// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Xml;
using System.Globalization;

namespace SilverSim.Scripting.Lsl
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public abstract partial class Script : ScriptInstance, IScriptState
    {
        private readonly ILog m_Log = LogManager.GetLogger("LSLSCRIPT");
        private ObjectPart m_Part;
        readonly ObjectPartInventoryItem m_Item;
        readonly NonblockingQueue<IScriptEvent> m_Events = new NonblockingQueue<IScriptEvent>();
        internal List<DetectInfo> m_Detected = new List<DetectInfo>();
        readonly Dictionary<string, ILSLState> m_States = new Dictionary<string, ILSLState>();
        private ILSLState m_CurrentState;
        public int StartParameter;
        internal RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();
        private double m_ExecutionTime;
        protected bool UseMessageObjectEvent;
        internal RwLockedList<UUID> m_RequestedURLs = new RwLockedList<UUID>();

        public readonly Timer Timer = new Timer();
        public int LastTimerEventTick;
        public bool UseForcedSleep = true;
        public double ForcedSleepFactor = 1;
        public double CurrentTimerInterval;
        internal List<Action<ScriptInstance>> ScriptRemoveDelegates;
        internal List<Action<ScriptInstance, List<object>>> SerializationDelegates;
        internal Dictionary<string, Action<ScriptInstance, List<object>>> DeserializationDelegates;
        static internal readonly Dictionary<Type, Action<Script, IScriptEvent>> StateEventHandlers = new Dictionary<Type, Action<Script, IScriptEvent>>();
        readonly object m_Lock = new object();

        bool m_HasTouchEvent;
        bool m_HasMoneyEvent;
        bool m_HaveQueuedTimerEvent;

        public override bool HasTouchEvent { get { return m_HasTouchEvent; } }
        public override bool HasMoneyEvent { get { return m_HasMoneyEvent; } }

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            lock (m_Lock)
            {
                if (!m_HaveQueuedTimerEvent)
                {
                    PostEvent(new TimerEvent());
                }
                LastTimerEventTick = Environment.TickCount;
                Timer.Interval = CurrentTimerInterval * 1000;
            }
        }

        public void SetTimerEvent(double interval, double elapsed = 0f)
        {
            lock (m_Lock)
            {
                CurrentTimerInterval = interval;
                if (interval < 0.01)
                {
                    Timer.Enabled = false;
                }
                else
                {
                    Timer.Enabled = false;
                    LastTimerEventTick = Environment.TickCount;
                    Timer.Interval = (interval - elapsed) * 1000;
                    CurrentTimerInterval = interval;
                    Timer.Enabled = true;
                }
            }
        }

        public override double ExecutionTime
        {
            get
            {
                lock(m_Lock)
                {
                    return m_ExecutionTime;
                }
            }
            set
            {
                lock(m_Lock)
                {
                    m_ExecutionTime = value;
                }
            }
        }

        public void ForcedSleep(double secs)
        {
            if (UseForcedSleep)
            {
                Sleep(secs * ForcedSleepFactor);
            }
        }

        public void AddState(string name, ILSLState state)
        {
            m_States.Add(name, state);
        }

        double m_MinEventDelay;
        public double MinEventDelay
        {
            get
            {
                return m_MinEventDelay;
            }
            set
            {
                m_MinEventDelay = (value < 0) ? 0 : value;
            }
        }


        readonly TransactionedState m_TransactionedState = new TransactionedState();

        class TransactionedState
        {
            readonly Dictionary<string, object> Variables = new Dictionary<string, object>();
            string CurrentState = string.Empty;
            ObjectPartInventoryItem.PermsGranterInfo PermsGranter = new ObjectPartInventoryItem.PermsGranterInfo();
            readonly List<object> PluginSerialization = new List<object>();
            double MinEventDelay;
            public UUID AssetID = UUID.Zero;
            public UUID ItemID = UUID.Zero;
            readonly object m_TransactionLock = new object();
            IScriptEvent[] Events = new IScriptEvent[0];

            public TransactionedState()
            {

            }

            public void UpdateFromScript(Script script)
            {
                IScriptEvent[] events = script.m_Events.ToArray();
                lock (m_TransactionLock)
                {
                    Events = events;
                    AssetID = script.Item.AssetID;
                    ItemID = script.Item.ID;
                    MinEventDelay = script.MinEventDelay;
                    CurrentState = "default";
                    foreach (KeyValuePair<string, ILSLState> kvp in script.m_States)
                    {
                        if (kvp.Value == script.m_CurrentState)
                        {
                            CurrentState = kvp.Key;
                        }
                    }
                    PermsGranter = script.Item.PermsGranter;
                    foreach (FieldInfo fi in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (!fi.Name.StartsWith("var_"))
                        {
                            continue;
                        }
                        if (fi.FieldType == typeof(int) ||
                            fi.FieldType == typeof(Vector3) ||
                            fi.FieldType == typeof(double) ||
                            fi.FieldType == typeof(Quaternion) ||
                            fi.FieldType == typeof(UUID))
                        {
                            Variables[fi.Name.Substring(4)] = fi.GetValue(this);
                        }
                        else if (fi.FieldType == typeof(LSLKey))
                        {
                            Variables[fi.Name.Substring(4)] = new LSLKey(fi.GetValue(this).ToString());
                        }
                        else if (fi.FieldType == typeof(string))
                        {
                            Variables[fi.Name.Substring(4)] = (string)fi.GetValue(this);
                        }
                        else if (fi.FieldType == typeof(AnArray))
                        {
                            Variables[fi.Name.Substring(4)] = new AnArray((AnArray)fi.GetValue(this));
                        }
                    }

                    PluginSerialization.Clear();
                    foreach (Action<ScriptInstance, List<object>> serializer in script.SerializationDelegates)
                    {
                        serializer(script, PluginSerialization);
                    }
                }
            }

            void ListToXml(XmlTextWriter writer, string name, AnArray array)
            {
                writer.WriteStartElement("Variable");
                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("type", "list");
                foreach (IValue val in array)
                {
                    Type valtype = val.GetType();
                    if (valtype == typeof(Integer))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                        writer.WriteValue(val.AsInt);
                        writer.WriteEndElement();
                    }
                    else if (valtype == typeof(Real))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                        double v = (Real)val;
                        writer.WriteValue(v);
                        writer.WriteEndElement();
                    }
                    else if (valtype == typeof(Quaternion))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                        writer.WriteValue(val.ToString());
                        writer.WriteEndElement();
                    }
                    else if (valtype == typeof(Vector3))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                        writer.WriteValue(val.ToString());
                        writer.WriteEndElement();
                    }
                    else if (valtype == typeof(AString))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString");
                        writer.WriteValue(val.ToString());
                        writer.WriteEndElement();
                    }
                    else if (valtype == typeof(UUID) || valtype == typeof(LSLKey))
                    {
                        writer.WriteStartElement("ListItem");
                        writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key");
                        writer.WriteValue(val.ToString());
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }

            public void ToXml(XmlTextWriter writer, Script script)
            {
                lock (m_TransactionLock)
                {
                    writer.WriteStartElement("State");
                    writer.WriteAttributeString("UUID", ItemID.ToString());
                    writer.WriteAttributeString("Asset", AssetID.ToString());
                    writer.WriteAttributeString("Engine", "XEngine");
                    {
                        writer.WriteStartElement("ScriptState");
                        {
                            writer.WriteStartElement("State");
                            {
                                writer.WriteValue(CurrentState);
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Running");
                            {
                                writer.WriteValue(script.IsRunning);
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("StartParameter");
                            {
                                writer.WriteValue(script.StartParameter);
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Variables");
                            foreach (KeyValuePair<string, object> kvp in Variables)
                            {
                                string varName = kvp.Key;
                                object varValue = kvp.Value;
                                Type varType = varValue.GetType();
                                if (varType == typeof(int))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                                }
                                else if (varType == typeof(double))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                                }
                                else if (varType == typeof(Vector3))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                                }
                                else if (varType == typeof(Quaternion))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                                }
                                else if (varType == typeof(UUID) || varType == typeof(LSLKey))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key");
                                }
                                else if (varType == typeof(string))
                                {
                                    writer.WriteStartElement("Variable");
                                    writer.WriteAttributeString("name", varName);
                                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString");
                                }
                                else if (varType == typeof(AnArray))
                                {
                                    ListToXml(writer, varName, (AnArray)varValue);
                                    continue;
                                }
                                else
                                {
                                    continue;
                                }
                                writer.WriteValue(varValue.ToString());
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Queue");
                            foreach(IScriptEvent ev in Events)
                            {
                                Action<Script, IScriptEvent, XmlTextWriter> serializer;
                                if(EventSerializers.TryGetValue(ev.GetType(), out serializer))
                                {
                                    serializer(script, ev, writer);
                                }
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Permissions");
                            {
                                ObjectPartInventoryItem.PermsGranterInfo grantInfo = PermsGranter;
                                writer.WriteNamedValue("mask", (uint)grantInfo.PermsMask);
                                writer.WriteNamedValue("granter", grantInfo.PermsGranter.ID);
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Plugins");
                            foreach (object o in PluginSerialization)
                            {
                                writer.WriteTypedValue("ListItem", o);
                            }
                            writer.WriteEndElement();
                            writer.WriteNamedValue("MinEventDelay", MinEventDelay);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
            }
        }

        public void ToXml(XmlTextWriter writer)
        {
            m_TransactionedState.ToXml(writer, this);
        }

        public byte[] ToDbSerializedState()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
                {
                    ToXml(writer);
                }
                return ms.ToArray();
            }
        }

        public Script(ObjectPart part, ObjectPartInventoryItem item, bool forcedSleepDefault)
        {
            UseForcedSleep = forcedSleepDefault;
            m_Part = part;
            m_Item = item;
            m_TransactionedState.ItemID = item.ID;
            m_TransactionedState.AssetID = item.AssetID;
            Timer.Elapsed += OnTimerEvent;
            /* we replace the loaded script state with ours */
            m_Item.ScriptState = this;
            m_Part.OnUpdate += OnPrimUpdate;
            m_Part.OnPositionChange += OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate += OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange += OnGroupPositionUpdate;
        }

        public void LoadScriptState(SavedScriptState state)
        {
            /* we have to integrate the loaded script state */
            Type scriptType = GetType();

            /* initialize variables */
            foreach (KeyValuePair<string, object> kvp in state.Variables)
            {
                FieldInfo fi = scriptType.GetField(kvp.Key);
                if (fi == null)
                {
                    m_Log.ErrorFormat("Restoring variable {0} failed", kvp.Key);
                }
                else if (fi.IsLiteral || fi.IsInitOnly || fi.FieldType != kvp.Value.GetType())
                {
                    continue;
                }
                fi.SetValue(this, kvp.Value);
            }

            /* initialize state */
            ILSLState script_state;
            if (m_States.TryGetValue(state.CurrentState, out script_state))
            {
                SetCurrentState(script_state);
            }

            /* queue deserialization */
            foreach (SavedScriptState.EventParams ep in state.EventData)
            {
                IScriptEvent ev;
                if (TryTranslateEventParams(ep, out ev))
                {
                    if(ev is TimerEvent)
                    {
                        m_HaveQueuedTimerEvent = true;
                    }
                    m_Events.Enqueue(ev);
                }
            }

            /* min event delay */
            MinEventDelay = state.MinEventDelay;

            /* start parameter */
            StartParameter = state.StartParameter;

            /* initialize plugin data */
            int pluginpos = 0;
            int pluginlen = state.PluginData.Count;
            try
            {
                while (pluginpos + 1 < pluginlen)
                {
                    int len = (int)state.PluginData[pluginpos + 1];
                    if (len + 2 + pluginpos > pluginlen)
                    {
                        break;
                    }
                    string type = (string)state.PluginData[pluginpos];
                    Action<ScriptInstance, List<object>> del;
                    if(len > 0 && DeserializationDelegates.TryGetValue(type, out del))
                    {
                        del(this, state.PluginData.GetRange(pluginpos + 2, len));
                    }
                    pluginpos += 2 + len;
                }
            }
            catch(Exception e)
            {
                StringBuilder disp = new StringBuilder();
                int pos = 0;
                foreach(object o in state.PluginData)
                {
                    disp.Append((pos++).ToString() + ":" + o.ToString());
                    disp.Append(" ");
                }
                m_Log.WarnFormat("Deserialization of state failed at position {0}: {1}\n=> {2}: {3}\n{4}", pluginpos, disp.ToString(), e.GetType().FullName, e.Message, e.StackTrace);
                throw;
            }


            IsRunning = state.IsRunning;

            lock(this) /* really needed to prevent aborting here */
            {
                m_TransactionedState.UpdateFromScript(this);
            }
        }

        private void OnPrimPositionUpdate(IObject part)
        {

        }

        private void OnGroupPositionUpdate(IObject group)
        {

        }

        public abstract void ResetVariables();

        public void ForcedSleep(int forcedSleepMs)
        {
            if(UseForcedSleep)
            {
                System.Threading.Thread.Sleep(forcedSleepMs);
            }
        }

        private void OnPrimUpdate(ObjectPart part, UpdateChangedFlags flags)
        {
            ChangedEvent.ChangedFlags changedflags = (ChangedEvent.ChangedFlags)(uint)flags;
            if (changedflags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = changedflags;
                PostEvent(e);
            }
        }

        private void OnGroupUpdate(ObjectGroup group, UpdateChangedFlags flags)
        {
            ChangedEvent.ChangedFlags changedflags = (ChangedEvent.ChangedFlags)(uint)flags;
            if (changedflags != 0)
            {
                ChangedEvent e = new ChangedEvent();
                e.Flags = changedflags;
                PostEvent(e);
            }
        }

        public override ObjectPart Part
        {
            get
            {
                return m_Part;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override ObjectPartInventoryItem Item 
        {
            get
            {
                return m_Item;
            }
        }

        public override void PostEvent(IScriptEvent e)
        {
            if (IsRunning && !IsAborting)
            {
                if(e is TimerEvent)
                {
                    m_HaveQueuedTimerEvent = true;
                }
                m_Events.Enqueue(e);
                Part.ObjectGroup.Scene.ScriptThreadPool.PostScript(this);
            }
        }

        public override void Reset()
        {
            if (IsRunning && !IsAborting)
            {
                m_Events.Enqueue(new ResetScriptEvent());
                Part.ObjectGroup.Scene.ScriptThreadPool.PostScript(this);
            }
        }

        public override bool IsRunning { get; set; }

        public override void Remove()
        {
            Timer.Elapsed -= OnTimerEvent;
            m_Part.OnUpdate -= OnPrimUpdate;
            m_Part.OnPositionChange -= OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate -= OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange -= OnGroupPositionUpdate;
            if(null != ScriptRemoveDelegates)
            {
                /* call remove delegates */
                foreach(Action<ScriptInstance> del in ScriptRemoveDelegates)
                {
                    del(this);
                }
            }
            IsRunning = false;
            m_HaveQueuedTimerEvent = false;
            m_Events.Clear();
            m_States.Clear();
            m_Part = null;
        }

        readonly RwLockedDictionary<string, MethodInfo> m_CurrentStateMethods = new RwLockedDictionary<string, MethodInfo>();

        public override void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions)
        {
            ObjectPartInventoryItem thisItem = Item;
            ObjectPart thisPart = Part;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = thisItem.PermsGranter;
            if (permissionsKey == grantinfo.PermsGranter.ID && grantinfo.PermsGranter != UUI.Unknown)
            {
                IAgent agent;
                if(!thisPart.ObjectGroup.Scene.Agents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                   return;
                }
                agent.RevokePermissions(thisPart.ID, thisItem.ID, (~permissions) & (grantinfo.PermsMask));
                grantinfo.PermsMask &= (~permissions);
                thisItem.PermsGranter = (ScriptPermissions.None == grantinfo.PermsMask) ?
                    null :
                    grantinfo;
            }
        }

        private void LogInvokeException(string name, Exception e)
        {
            string state_name = m_CurrentState.GetType().FullName;
            state_name = state_name.Substring(1 + state_name.LastIndexOf('.'));
            if (e.InnerException != null)
            {
                e = e.InnerException;
            }

            string objectName;
            UUID objectID;
            try
            {
                objectName = Part.ObjectGroup.Name;
            }
            catch
            {
                objectName = "<Unknown>";
            }

            try
            {
                objectID = Part.ObjectGroup.ID;
            }
            catch
            {
                objectID = UUID.Zero;
            }
            if (e is InvalidProgramException || e is MethodAccessException || e is CallDepthLimitViolationException)
            {
                m_Log.ErrorFormat("Stopping script {5} (asset {6}) in {7} ({8}) [{9} ({10})]\nWithin state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.GetType().FullName, e.Message, e.StackTrace,
                    Item.Name, Item.AssetID.ToString(), Part.Name, Part.ID.ToString(), objectName, objectID.ToString());
                IsRunning = false;
            }
            else
            {
                m_Log.ErrorFormat("Script {5} (asset {6}) in {7} ({8}) [{9} ({10})]\nWithin state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.GetType().FullName, e.Message, e.StackTrace,
                    Item.Name, Item.AssetID.ToString(), Part.Name, Part.ID.ToString(), objectName, objectID.ToString());
            }
        }

        public override bool IsLinkMessageReceiver
        {
            get
            {
                return m_CurrentStateMethods.ContainsKey("link_message");
            }
        }

        static public void InvokeStateEvent(Script script, string name, object[] param)
        {
            script.InvokeStateEventReal(name, param);
        }

        void InvokeStateEvent(string name, params object[] param)
        {
            InvokeStateEventReal(name, param);
        }

        int m_RecursionCount;
        static int m_CallDepthLimit = 40;
        static public int CallDepthLimit
        {
            get
            {
                return m_CallDepthLimit;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                m_CallDepthLimit = value;
            }
        }

        public void IncCallDepthCount()
        {
            if(++m_RecursionCount > m_CallDepthLimit)
            {
                throw new CallDepthLimitViolationException();
            }
        }

        public void DecCallDepthCount()
        {
            --m_RecursionCount;
        }

        public void ResetCallDepthCount()
        {
            m_RecursionCount = 0;
        }

        void SetCurrentState(ILSLState state)
        {
            m_CurrentState = state;
            m_CurrentStateMethods.Clear();
            m_HasTouchEvent = HasStateEvent("touch") || HasStateEvent("touch_start") || HasStateEvent("touch_end");
            m_HasMoneyEvent = HasStateEvent("money");
            lock(this)
            {
                /* lock(this) needed here to prevent aborting in wrong place */
                Part.UpdateScriptFlags();
            }
        }

        bool HasStateEvent(string name)
        {
            MethodInfo mi;
            if (!m_CurrentStateMethods.TryGetValue(name, out mi))
            {
                mi = m_CurrentState.GetType().GetMethod(name);
                m_CurrentStateMethods.Add(name, mi);
            }
            return mi != null;
        }

        internal void InvokeStateEventReal(string name, object[] param)
        {
            MethodInfo mi;
            if (!m_CurrentStateMethods.TryGetValue(name, out mi))
            {
                mi = m_CurrentState.GetType().GetMethod(name);
                m_CurrentStateMethods.Add(name, mi);
            }

            if (null != mi)
            {
                try
                {
                    mi.Invoke(m_CurrentState, param);
                }
                catch (ChangeStateException)
                {
                    throw;
                }
                catch (ResetScriptException)
                {
                    throw;
                }
                catch (TargetInvocationException e)
                {
                    Type innerType = e.InnerException.GetType();
                    if (innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException))
                    {
                        throw;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch(CallDepthLimitViolationException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(new LocalizedScriptMessage(this, "FunctionCallDepthLimitViolation", "Function call depth limit violation"));
                }
                catch (TargetParameterCountException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (TargetException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NullReferenceException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch(ArgumentException e)
                {
#if DEBUG
                    LogInvokeException(name, e);
#endif
                    ShoutError(e.Message);
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        static string MapTypeToString(Type t)
        {
            if(typeof(string) == t)
            {
                return "string";
            }
            else if(typeof(int) == t)
            {
                return "integer";
            }
            else if(typeof(Quaternion) == t)
            {
                return "rotation";
            }
            else if(typeof(AnArray) == t)
            {
                return "list";
            }
            else if(typeof(Vector3) == t)
            {
                return "vector";
            }
            else if(typeof(double) == t)
            {
                return "float";
            }
            else if(typeof(LSLKey) == t)
            {
                return "key";
            }
            else
            {
                return "???";
            }
        }

        void ShoutUnimplementedException(NotImplementedException e)
        {
            MethodBase mb = e.TargetSite;
            APILevelAttribute apiLevel = (APILevelAttribute)Attribute.GetCustomAttribute(mb, typeof(APILevelAttribute));
            if (apiLevel != null)
            {
                string methodName = mb.Name;
                if (!string.IsNullOrEmpty(apiLevel.Name))
                {
                    methodName = apiLevel.Name;
                }

                StringBuilder funcSignature = new StringBuilder("Script called unimplemented function " + methodName + "(");

                ParameterInfo[] pi = mb.GetParameters();
                for (int i = 1; i < pi.Length; ++i)
                {
                    if (i > 1)
                    {
                        funcSignature.Append(", ");
                    }
                    funcSignature.Append(MapTypeToString(pi[i].ParameterType));
                }
                funcSignature.Append(")");

                ShoutError(funcSignature.ToString());
            }
        }

        public override IScriptState ScriptState
        {
            get
            {
                return this;
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public override void ProcessEvent()
        {
            IScriptEvent evgot;
            try
            {
                evgot = m_Events.Dequeue();
            }
            catch
            {
                return;
            }
            int exectime;
            float execfloat;
            int startticks = Environment.TickCount;
            bool executeStateEntry = false;
            bool executeStateExit = false;
            bool executeScriptReset = false;
            ILSLState newState = m_CurrentState;

            if(evgot is TimerEvent)
            {
                m_HaveQueuedTimerEvent = false;
            }

            do
            {
                if (newState == null)
                {
                    newState = m_States["default"];
                    executeStateEntry = true;
                    executeStateExit = false;
                }
                else if (newState != m_CurrentState)
                {
                    executeStateExit = true;
                    executeStateEntry = true;
                }

                #region Script Reset
                if (executeScriptReset)
                {
                    executeScriptReset = false;
                    executeStateExit = false;
                    executeStateEntry = true;
                    TriggerOnStateChange();
                    TriggerOnScriptReset();
                    m_Events.Clear();
                    lock (m_Lock)
                    {
                        m_ExecutionTime = 0f;
                    }
                    SetCurrentState(m_States["default"]);
                    StartParameter = 0;
                    ResetVariables();
                    startticks = Environment.TickCount;
                }
                #endregion

                #region State Exit
                bool executedStateExit = executeStateExit;
                try
                {
                    if (executeStateExit)
                    {
                        executeStateExit = false;
                        startticks = Environment.TickCount;
                        InvokeStateEvent("state_exit");
                    }
                }
                catch (ResetScriptException)
                {
                    executeScriptReset = true;
                    continue;
                }
                catch(ChangeStateException e)
                {
                    ShoutError("Script error! state change used in state_exit");
                    LogInvokeException("state_exit", e);
                }
                catch (TargetInvocationException e)
                {
                    Type eType = e.InnerException.GetType();
                    if (eType == typeof(ResetScriptEvent))
                    {
                        executeScriptReset = true;
                        continue;
                    }
                    else if (eType == typeof(ChangeStateException))
                    {
                        ShoutError("Script error! state change used in state_exit");
                        LogInvokeException("state_exit", e);
                    }
                }
                finally
                {
                    if (executedStateExit)
                    {
                        exectime = Environment.TickCount - startticks;
                        execfloat = exectime / 1000f;
                        lock (m_Lock)
                        {
                            m_ExecutionTime += execfloat;
                        }
                    }
                }
                if (executedStateExit)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        m_TransactionedState.UpdateFromScript(this);
                    }
                }
                #endregion

                #region State Entry
                bool executedStateEntry = executeStateEntry;
                try
                {
                    if (executeStateEntry)
                    {
                        executeStateEntry = false;
                        SetCurrentState(newState);
                        startticks = Environment.TickCount;
                        if (evgot != null && evgot.GetType() == typeof(ResetScriptEvent))
                        {
                            evgot = null;
                        }
                        InvokeStateEvent("state_entry");
                    }
                }
                catch (ResetScriptException)
                {
                    if (m_States["default"] == m_CurrentState)
                    {
                        ShoutError("Script error! llResetScript used in default.state_entry");
                        return;
                    }
                    else
                    {
                        executeScriptReset = true;
                        continue;
                    }
                }
                catch(ChangeStateException e)
                {
                    if (m_CurrentState != m_States[e.NewState])
                    {
                        /* if state is equal, it simply aborts the event execution */
                        newState = m_States[e.NewState];
                        executeStateExit = true;
                        executeStateEntry = true;

                        lock (this) /* really needed to prevent aborting here */
                        {
                            m_TransactionedState.UpdateFromScript(this);
                        }
                        continue;
                    }
                }
                catch(TargetInvocationException e)
                {
                    Type eType = e.InnerException.GetType();
                    if (eType == typeof(ResetScriptEvent))
                    {
                        executeScriptReset = true;
                        continue;
                    }
                    else if (eType == typeof(ChangeStateException))
                    {
                        ChangeStateException innerException = (ChangeStateException)e.InnerException;
                        if (m_CurrentState != m_States[innerException.NewState])
                        {
                            /* if state is equal, it simply aborts the event execution */
                            newState = m_States[innerException.NewState];
                            executeStateExit = true;
                            executeStateEntry = true;

                            lock (this) /* really needed to prevent aborting here */
                            {
                                m_TransactionedState.UpdateFromScript(this);
                            }
                            continue;
                        }
                    }
                }
                finally
                {
                    if (executedStateEntry)
                    {
                        exectime = Environment.TickCount - startticks;
                        execfloat = exectime / 1000f;
                        lock (m_Lock)
                        {
                            m_ExecutionTime += execfloat;
                        }
                    }
                }

                if(executedStateEntry)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        m_TransactionedState.UpdateFromScript(this);
                    }
                }
                #endregion

                #region Event Logic
                bool eventExecuted = false;
                try
                {
                    IScriptEvent ev = evgot;
                    evgot = null;
                    if (null != ev)
                    {
                        eventExecuted = true;
                        startticks = Environment.TickCount;
                        Type evt = ev.GetType();
                        Action<Script, IScriptEvent> evtDelegate;
                        if (StateEventHandlers.TryGetValue(evt, out evtDelegate))
                        {
                            IScriptDetectedEvent detectedEv = ev as IScriptDetectedEvent;
                            if(detectedEv != null)
                            {
                                m_Detected = detectedEv.Detected;
                            }
                            evtDelegate(this, ev);
                        }
                    }
                }
                catch (ResetScriptException)
                {
                    executeScriptReset = true;
                    continue;
                }
                catch (ChangeStateException e)
                {
                    if (m_CurrentState != m_States[e.NewState])
                    {
                        /* if state is equal, it simply aborts the event execution */
                        newState = m_States[e.NewState];
                        executeStateExit = true;
                        executeStateEntry = true;
                    }
                }
                catch (TargetInvocationException e)
                {
                    Type eType = e.InnerException.GetType();
                    if (eType == typeof(ResetScriptEvent))
                    {
                        executeScriptReset = true;
                        continue;
                    }
                    else if (eType == typeof(ChangeStateException))
                    {
                        ChangeStateException innerException = (ChangeStateException)e.InnerException;
                        if (m_CurrentState != m_States[innerException.NewState])
                        {
                            /* if state is equal, it simply aborts the event execution */
                            newState = m_States[innerException.NewState];
                            executeStateExit = true;
                            executeStateEntry = true;

                            lock (this) /* really needed to prevent aborting here */
                            {
                                m_TransactionedState.UpdateFromScript(this);
                            }
                            continue;
                        }
                    }
                }
                finally
                {
                    if (eventExecuted)
                    {
                        exectime = Environment.TickCount - startticks;
                        execfloat = exectime / 1000f;
                        lock (m_Lock)
                        {
                            m_ExecutionTime += execfloat;
                        }
                    }
                }
                #endregion

                lock (this) /* really needed to prevent aborting here */
                {
                    m_TransactionedState.UpdateFromScript(this);
                }
            } while (executeStateEntry || executeStateExit || executeScriptReset);
        }

        public override bool HasEventsPending
        { 
            get
            {
                return m_Events.Count != 0;
            }
        }

        internal void OnListen(ListenEvent ev)
        {
            PostEvent(ev);
        }

        sealed class AtRegionMessageLocalization : IListenEventLocalization
        {
            readonly IListenEventLocalization m_SubMessage;
            readonly string m_RegionName;
            public AtRegionMessageLocalization(IListenEventLocalization subMessage, string regionName)
            {
                m_SubMessage = subMessage;
                m_RegionName = regionName;
            }

            public string Localize(ListenEvent le, CultureInfo currentCulture)
            {
                return string.Format(this.GetLanguageString(currentCulture, "ShoutErrorAtRegion0", "At region {0}:") + "\n", m_RegionName) +
                    m_SubMessage != null ? m_SubMessage.Localize(le, currentCulture) : le.Message;
            }
        }

        public override void ShoutError(string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = 0x7FFFFFFF; /* DEBUG_CHANNEL */
            ev.Type = ListenEvent.ChatType.Region;
            ChatServiceInterface chatService;
            lock (m_Lock)
            {
                ObjectPart part = Part;
                ObjectGroup objGroup = part.ObjectGroup;
                ev.Message = message;
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                ev.OwnerID = objGroup.Owner.ID;
                ev.GlobalPosition = objGroup.GlobalPosition;
                ev.ID = objGroup.ID;
                ev.Name = objGroup.Name;
                ev.Localization = new AtRegionMessageLocalization(null, objGroup.Scene.Name);
                chatService = objGroup.Scene.GetService<ChatServiceInterface>();
            }
            if (null != chatService)
            {
                chatService.Send(ev);
            }
        }

        public override void ShoutError(IListenEventLocalization localizedMessage)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = 0x7FFFFFFF; /* DEBUG_CHANNEL */
            ev.Type = ListenEvent.ChatType.Region;
            ChatServiceInterface chatService;
            lock (m_Lock)
            {
                ObjectPart part = Part;
                ObjectGroup objGroup = part.ObjectGroup;
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                ev.OwnerID = objGroup.Owner.ID;
                ev.GlobalPosition = objGroup.GlobalPosition;
                ev.ID = objGroup.ID;
                ev.Name = objGroup.Name;
                ev.Localization = new AtRegionMessageLocalization(localizedMessage, objGroup.Scene.Name);
                ev.Message = ev.Localization.Localize(ev, null);
                chatService = objGroup.Scene.GetService<ChatServiceInterface>();
            }
            if (null != chatService)
            {
                chatService.Send(ev);
            }
        }

        static bool TryTranslateEventParams(SavedScriptState.EventParams ep, out IScriptEvent res)
        {
            res = null;
            Func<SavedScriptState.EventParams, IScriptEvent> deserializer;
            if (EventDeserializers.TryGetValue(ep.EventName, out deserializer))
            {
                res = deserializer(ep);
            }
            return res != null;
        }

        static IScriptEvent ObjectRezDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                ObjectRezEvent ev = new ObjectRezEvent();
                ev.ObjectID = new UUID(ep.Params[0].ToString());
                return ev;
            }
            return null;
        }

        static void ObjectRezSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            ObjectRezEvent ev = (ObjectRezEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "object_rez");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ObjectID);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent EmailDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 5)
            {
                EmailEvent ev = new EmailEvent();
                ev.Time = ep.Params[0].ToString();
                ev.Address = ep.Params[1].ToString();
                ev.Subject = ep.Params[2].ToString();
                ev.Message = ep.Params[3].ToString();
                ev.NumberLeft = (int)ep.Params[4];
                return ev;
            }
            return null;
        }

        static void EmailSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            EmailEvent ev = (EmailEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "email");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Time);
                writer.WriteTypedValue("Param", ev.Address);
                writer.WriteTypedValue("Param", ev.Subject);
                writer.WriteTypedValue("Param", ev.Message);
                writer.WriteTypedValue("Param", ev.NumberLeft);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent RuntimePermissionsDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                RuntimePermissionsEvent ev = new RuntimePermissionsEvent();
                ev.Permissions = (ScriptPermissions)(int)ep.Params[0];
                if (ep.Params.Count > 1)
                {
                    ev.PermissionsKey = new UUI(ep.Params[1].ToString());
                }
                return ev;
            }
            return null;
        }

        static void RuntimePermissionsSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            RuntimePermissionsEvent ev = (RuntimePermissionsEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "run_time_permissions");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", (int)ev.Permissions);
                writer.WriteTypedValue("Param", ev.PermissionsKey.ID);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent LinkMessageDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                LinkMessageEvent ev = new LinkMessageEvent();
                ev.SenderNumber = (int)ep.Params[0];
                ev.Number = (int)ep.Params[1];
                ev.Data = ep.Params[2].ToString();
                ev.Id = ep.Params[3].ToString();
                return ev;
            }
            return null;
        }

        static void LinkMessageSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            LinkMessageEvent ev = (LinkMessageEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "link_message");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.SenderNumber);
                writer.WriteTypedValue("Param", ev.Number);
                writer.WriteTypedValue("Param", ev.Data);
                writer.WriteTypedValue("Param", ev.Id);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent RemoteDataDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 6)
            {
                RemoteDataEvent ev = new RemoteDataEvent();
                ev.Type = (int)ep.Params[0];
                ev.Channel = new UUID(ep.Params[1].ToString());
                ev.MessageID = new UUID(ep.Params[2].ToString());
                ev.Sender = ep.Params[3].ToString();
                ev.IData = (int)ep.Params[4];
                ev.SData = ep.Params[5].ToString();
                return ev;
            }
            return null;
        }

        static void RemoteDataSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            RemoteDataEvent ev = (RemoteDataEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "remote_data");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Type);
                writer.WriteTypedValue("Param", ev.Channel);
                writer.WriteTypedValue("Param", ev.MessageID);
                writer.WriteTypedValue("Param", ev.Sender);
                writer.WriteTypedValue("Param", ev.IData);
                writer.WriteTypedValue("Param", ev.SData);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent TransactionResultDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                TransactionResultEvent ev = new TransactionResultEvent();
                ev.TransactionID = ep.Params[0].ToString();
                ev.Success = (int)ep.Params[1] != 0;
                ev.ReplyData = ep.Params[2].ToString();
                return ev;
            }
            return null;
        }

        static void TransactionResultSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            TransactionResultEvent ev = (TransactionResultEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "transaction_result");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.TransactionID);
                writer.WriteTypedValue("Param", ev.Success);
                writer.WriteTypedValue("Param", ev.ReplyData);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent ObjectMessageDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                MessageObjectEvent ev = new MessageObjectEvent();
                ev.ObjectID = new UUID(ep.Params[0].ToString());
                ev.Data = ep.Params[1].ToString();
                return ev;
            }
            return null;
        }

        static void ObjectMessageSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            MessageObjectEvent ev = (MessageObjectEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", script.UseMessageObjectEvent ? "object_message" : "dataserver");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ObjectID);
                writer.WriteTypedValue("Param", ev.Data);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent DataserverDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                DataserverEvent ev = new DataserverEvent();
                ev.QueryID = new UUID(ep.Params[0].ToString());
                ev.Data = ep.Params[1].ToString();
                return ev;
            }
            return null;
        }

        static void DataserverSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            DataserverEvent ev = (DataserverEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "dataserver");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.QueryID);
                writer.WriteTypedValue("Param", ev.Data);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent HttpResponseDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                HttpResponseEvent ev = new HttpResponseEvent();
                ev.RequestID = new UUID(ep.Params[0].ToString());
                ev.Status = (int)ep.Params[1];
                ev.Metadata = (AnArray)ep.Params[2];
                ev.Body = ep.Params[3].ToString();
                return ev;
            }
            return null;
        }

        static void HttpResponseSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            HttpResponseEvent ev = (HttpResponseEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "http_response");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.RequestID);
                writer.WriteTypedValue("Param", ev.Status);
                writer.WriteTypedValue("Param", ev.Metadata);
                writer.WriteTypedValue("Param", ev.Body);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent ListenDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                ListenEvent ev = new ListenEvent();
                ev.Channel = (int)ep.Params[0];
                ev.Name = ep.Params[1].ToString();
                ev.ID = new UUID(ep.Params[2].ToString());
                ev.Message = ep.Params[3].ToString();
                return ev;
            }
            return null;
        }

        static void ListenSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            ListenEvent ev = (ListenEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "listen");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Channel);
                writer.WriteTypedValue("Param", ev.Name);
                writer.WriteTypedValue("Param", ev.ID);
                writer.WriteTypedValue("Param", ev.Message);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent OnRezDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                OnRezEvent ev = new OnRezEvent();
                ev.StartParam = (int)ep.Params[0];
                return ev;
            }
            return null;
        }

        static void OnRezSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            OnRezEvent ev = (OnRezEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "on_rez");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.StartParam);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent AttachDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                AttachEvent ev = new AttachEvent();
                ev.ObjectID = new UUID(ep.Params[0].ToString());
                return ev;
            }
            return null;
        }

        static void AttachSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            AttachEvent ev = (AttachEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "attach");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ObjectID);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent ChangedDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                ChangedEvent ev = new ChangedEvent();
                ev.Flags = (ChangedEvent.ChangedFlags)(int)ep.Params[0];
                return ev;
            }
            return null;
        }

        static void ChangedSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            ChangedEvent ev = (ChangedEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "changed");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", (int)ev.Flags);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent MoneyDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                MoneyEvent ev = new MoneyEvent();
                ev.ID = new UUID(ep.Params[0].ToString());
                ev.Amount = (int)ep.Params[1];
                return ev;
            }
            return null;
        }

        static void MoneySerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            MoneyEvent ev = (MoneyEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "money");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ID);
                writer.WriteTypedValue("Param", ev.Amount);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent LandCollisionStartDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                LandCollisionEvent ev = new LandCollisionEvent();
                ev.Type = LandCollisionEvent.CollisionType.Start;
                ev.Position = (Vector3)ep.Params[0];
                return ev;
            }
            return null;
        }

        static void LandCollisionSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            LandCollisionEvent ev = (LandCollisionEvent)iev;
            writer.WriteStartElement("Item");
            {
                switch(ev.Type)
                {
                    case LandCollisionEvent.CollisionType.Start:
                        writer.WriteAttributeString("event", "land_collision_start");
                        break;
                    case LandCollisionEvent.CollisionType.Continuous:
                        writer.WriteAttributeString("event", "land_collision");
                        break;
                    case LandCollisionEvent.CollisionType.End:
                        writer.WriteAttributeString("event", "land_collision_end");
                        break;
                    default:
                        break;
                }
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Position);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent LandCollisionDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                LandCollisionEvent ev = new LandCollisionEvent();
                ev.Type = LandCollisionEvent.CollisionType.Continuous;
                ev.Position = (Vector3)ep.Params[0];
                return ev;
            }
            return null;
        }

        static IScriptEvent LandCollisionEndDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                LandCollisionEvent ev = new LandCollisionEvent();
                ev.Type = LandCollisionEvent.CollisionType.End;
                ev.Position = (Vector3)ep.Params[0];
                return ev;
            }
            return null;
        }

        static IScriptEvent ControlDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                ControlEvent ev = new ControlEvent();
                ev.AgentID = new UUID(ep.Params[0].ToString());
                ev.Level = (int)ep.Params[1];
                ev.Flags = (int)ep.Params[2];
                return ev;
            }
            return null;
        }

        static void ControlSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            ControlEvent ev = (ControlEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "control");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.AgentID);
                writer.WriteTypedValue("Param", ev.Level);
                writer.WriteTypedValue("Param", ev.Flags);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent AtTargetDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                AtTargetEvent ev = new AtTargetEvent();
                ev.Handle = (int)ep.Params[0];
                ev.TargetPosition = (Vector3)ep.Params[1];
                ev.OurPosition = (Vector3)ep.Params[2];
                return ev;
            }
            return null;
        }

        static void AtTargetSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            AtTargetEvent ev = (AtTargetEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "at_target");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Handle);
                writer.WriteTypedValue("Param", ev.TargetPosition);
                writer.WriteTypedValue("Param", ev.OurPosition);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static IScriptEvent AtRotTargetDeserializer(SavedScriptState.EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                AtRotTargetEvent ev = new AtRotTargetEvent();
                ev.Handle = (int)ep.Params[0];
                ev.TargetRotation = (Quaternion)ep.Params[1];
                ev.OurRotation = (Quaternion)ep.Params[2];
                return ev;
            }
            return null;
        }

        static void AtRotTargetSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            AtRotTargetEvent ev = (AtRotTargetEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "at_rot_target");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.Handle);
                writer.WriteTypedValue("Param", ev.TargetRotation);
                writer.WriteTypedValue("Param", ev.OurRotation);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static void NoParamSerializer(IScriptEvent iev, string name, XmlTextWriter writer)
        {
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", name);
                writer.WriteStartElement("Params");
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static void DetectedSerializer(List<DetectInfo> di, string name, XmlTextWriter writer)
        {
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", name);
                writer.WriteStartElement("Params");
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                foreach(DetectInfo d in di)
                {
                    writer.WriteStartElement("Object");
                    writer.WriteAttributeString("pos", d.GrabOffset.ToString());
                    writer.WriteAttributeString("linkNum", d.LinkNumber.ToString());
                    writer.WriteAttributeString("group", d.Group.ToString());
                    writer.WriteAttributeString("name", d.Name);
                    writer.WriteAttributeString("owner", d.Owner.ToString());
                    writer.WriteAttributeString("position", d.Position.ToString());
                    writer.WriteAttributeString("rotation", d.Rotation.ToString());
                    writer.WriteAttributeString("type", ((int)d.ObjType).ToString());
                    writer.WriteAttributeString("velocity", d.Velocity.ToString());

                    /* for whatever reason, OpenSim does not serialize the following */
                    writer.WriteAttributeString("touchst", d.TouchST.ToString());
                    writer.WriteAttributeString("touchuv", d.TouchUV.ToString());
                    writer.WriteAttributeString("touchbinormal", d.TouchBinormal.ToString());
                    writer.WriteAttributeString("touchpos", d.TouchPosition.ToString());
                    writer.WriteAttributeString("touchface", d.TouchFace.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        static void TouchSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            TouchEvent ev = (TouchEvent)iev;
            switch(ev.Type)
            {
                case TouchEvent.TouchType.Start:
                    DetectedSerializer(ev.Detected, "touch_start", writer);
                    break;
                case TouchEvent.TouchType.Continuous:
                    DetectedSerializer(ev.Detected, "touch", writer);
                    break;
                case TouchEvent.TouchType.End:
                    DetectedSerializer(ev.Detected, "touch_end", writer);
                    break;
                default:
                    break;
            }
        }

        static void CollisionSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            CollisionEvent ev = (CollisionEvent)iev;
            switch (ev.Type)
            {
                case CollisionEvent.CollisionType.Start:
                    DetectedSerializer(ev.Detected, "collision_start", writer);
                    break;
                case CollisionEvent.CollisionType.Continuous:
                    DetectedSerializer(ev.Detected, "collision", writer);
                    break;
                case CollisionEvent.CollisionType.End:
                    DetectedSerializer(ev.Detected, "collision_end", writer);
                    break;
                default:
                    break;
            }
        }

        static IScriptEvent HttpRequestDeserializer(SavedScriptState.EventParams ep)
        {
            if(ep.Params.Count >= 3)
            {
                HttpRequestEvent ev = new HttpRequestEvent();
                ev.RequestID = UUID.Parse(ep.Params[0].ToString());
                ev.Method = ep.Params[1].ToString();
                ev.Body = ep.Params[2].ToString();
                return ev;
            }
            return null;
        }

        static void HttpRequestSerializer(Script script, IScriptEvent iev, XmlTextWriter writer)
        {
            HttpRequestEvent ev = (HttpRequestEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "http_request");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.RequestID);
                writer.WriteTypedValue("Param", ev.Method);
                writer.WriteTypedValue("Param", ev.Body);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        #region Event to function handlers
        static readonly Dictionary<Type, Action<Script, IScriptEvent, XmlTextWriter>> EventSerializers = new Dictionary<Type, Action<Script, IScriptEvent, XmlTextWriter>>();
        static readonly Dictionary<string, Func<SavedScriptState.EventParams, IScriptEvent>> EventDeserializers = new Dictionary<string, Func<SavedScriptState.EventParams, IScriptEvent>>();

        static Script()
        {
            EventDeserializers.Add("http_request", HttpRequestDeserializer);
            EventSerializers.Add(typeof(HttpRequestEvent), HttpRequestSerializer);
            EventDeserializers.Add("land_collision_start", LandCollisionStartDeserializer);
            EventSerializers.Add(typeof(LandCollisionEvent), LandCollisionSerializer);
            EventDeserializers.Add("land_collision", LandCollisionDeserializer);
            EventDeserializers.Add("land_collision_end", LandCollisionEndDeserializer);
            EventDeserializers.Add("at_target", AtTargetDeserializer);
            EventSerializers.Add(typeof(AtTargetEvent), AtTargetSerializer);
            EventDeserializers.Add("at_rot_target", AtRotTargetDeserializer);
            EventSerializers.Add(typeof(AtRotTargetEvent), AtRotTargetSerializer);
            EventDeserializers.Add("control", ControlDeserializer);
            EventSerializers.Add(typeof(ControlEvent), ControlSerializer);
            EventDeserializers.Add("object_rez", ObjectRezDeserializer);
            EventSerializers.Add(typeof(ObjectRezEvent), ObjectRezSerializer);
            EventDeserializers.Add("email", EmailDeserializer);
            EventSerializers.Add(typeof(EmailEvent), EmailSerializer);
            EventDeserializers.Add("run_time_permissions", RuntimePermissionsDeserializer);
            EventSerializers.Add(typeof(RuntimePermissionsEvent), RuntimePermissionsSerializer);
            EventDeserializers.Add("link_message", LinkMessageDeserializer);
            EventSerializers.Add(typeof(LinkMessageEvent), LinkMessageSerializer);
            EventDeserializers.Add("remote_data", RemoteDataDeserializer);
            EventSerializers.Add(typeof(RemoteDataEvent), RemoteDataSerializer);
            EventDeserializers.Add("transaction_result", TransactionResultDeserializer);
            EventSerializers.Add(typeof(TransactionResultEvent), TransactionResultSerializer);
            EventDeserializers.Add("dataserver", DataserverDeserializer);
            EventSerializers.Add(typeof(DataserverEvent), DataserverSerializer);
            EventDeserializers.Add("object_message", ObjectMessageDeserializer);
            EventSerializers.Add(typeof(MessageObjectEvent), ObjectMessageSerializer);
            EventDeserializers.Add("http_response", HttpResponseDeserializer);
            EventSerializers.Add(typeof(HttpResponseEvent), HttpResponseSerializer);
            EventDeserializers.Add("listen", ListenDeserializer);
            EventSerializers.Add(typeof(ListenEvent), ListenSerializer);
            EventDeserializers.Add("sensor", delegate(SavedScriptState.EventParams ep)
            {
                SensorEvent ev = new SensorEvent();
                ev.Detected = ep.Detected;
                return ev;
            });
            EventSerializers.Add(typeof(SensorEvent), delegate (Script script, IScriptEvent iev, XmlTextWriter writer)
            {
                SensorEvent ev = (SensorEvent)iev;
                DetectedSerializer(ev.Detected, "sensor", writer);
            });
            EventDeserializers.Add("on_rez", OnRezDeserializer);
            EventSerializers.Add(typeof(OnRezEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "on_rez", writer);
            });
            EventDeserializers.Add("attach", AttachDeserializer);
            EventSerializers.Add(typeof(AttachEvent), AttachSerializer);
            EventDeserializers.Add("changed", ChangedDeserializer);
            EventSerializers.Add(typeof(ChangedEvent), ChangedSerializer);
            EventDeserializers.Add("money", MoneyDeserializer);
            EventSerializers.Add(typeof(MoneyEvent), MoneySerializer);
            EventDeserializers.Add("no_sensor", delegate (SavedScriptState.EventParams ep)
            {
                return new NoSensorEvent();
            });
            EventSerializers.Add(typeof(NoSensorEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "no_sensor", writer);
            });
            EventDeserializers.Add("timer", delegate (SavedScriptState.EventParams ep)
            {
                return new TimerEvent();
            });
            EventSerializers.Add(typeof(TimerEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "timer", writer);
            });
            EventDeserializers.Add("touch_start", delegate (SavedScriptState.EventParams ep)
            {
                TouchEvent ev = new TouchEvent();
                ev.Type = TouchEvent.TouchType.Start;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventSerializers.Add(typeof(TouchEvent), TouchSerializer);
            EventDeserializers.Add("touch", delegate (SavedScriptState.EventParams ep)
            {
                TouchEvent ev = new TouchEvent();
                ev.Type = TouchEvent.TouchType.Continuous;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventDeserializers.Add("touch_end", delegate (SavedScriptState.EventParams ep)
            {
                TouchEvent ev = new TouchEvent();
                ev.Type = TouchEvent.TouchType.End;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventDeserializers.Add("collision_start", delegate (SavedScriptState.EventParams ep)
            {
                CollisionEvent ev = new CollisionEvent();
                ev.Type = CollisionEvent.CollisionType.Start;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventSerializers.Add(typeof(CollisionEvent), CollisionSerializer);
            EventDeserializers.Add("collision", delegate (SavedScriptState.EventParams ep)
            {
                CollisionEvent ev = new CollisionEvent();
                ev.Type = CollisionEvent.CollisionType.Continuous;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventDeserializers.Add("collision_end", delegate (SavedScriptState.EventParams ep)
            {
                CollisionEvent ev = new CollisionEvent();
                ev.Type = CollisionEvent.CollisionType.End;
                ev.Detected = ep.Detected;
                return ev;
            });
            EventDeserializers.Add("not_at_target", delegate (SavedScriptState.EventParams ep)
            {
                return new NotAtTargetEvent();
            });
            EventSerializers.Add(typeof(NotAtTargetEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "not_at_target", writer);
            });
            EventDeserializers.Add("moving_start", delegate (SavedScriptState.EventParams ep)
            {
                return new MovingStartEvent();
            });
            EventSerializers.Add(typeof(MovingStartEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "moving_start", writer);
            });
            EventDeserializers.Add("moving_end", delegate (SavedScriptState.EventParams ep)
            {
                return new MovingEndEvent();
            });
            EventSerializers.Add(typeof(MovingEndEvent), delegate (Script script, IScriptEvent ev, XmlTextWriter writer)
            {
                NoParamSerializer(ev, "moving_end", writer);
            });

            #region Default state event handlers
            StateEventHandlers.Add(typeof(AtRotTargetEvent), delegate(Script script, IScriptEvent ev)
            {
                AtRotTargetEvent e = (AtRotTargetEvent)ev;
                script.InvokeStateEvent("at_rot_target", e.TargetRotation, e.OurRotation);
            });

            StateEventHandlers.Add(typeof(AttachEvent), delegate(Script script, IScriptEvent ev)
            {
                AttachEvent e = (AttachEvent)ev;
                script.InvokeStateEvent("attach", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(AtTargetEvent), delegate(Script script, IScriptEvent ev)
            {
                AtTargetEvent e = (AtTargetEvent)ev;
                script.InvokeStateEvent("at_target", e.Handle, e.TargetPosition, e.OurPosition);
            });

            StateEventHandlers.Add(typeof(ChangedEvent), delegate(Script script, IScriptEvent ev)
            {
                ChangedEvent e = (ChangedEvent)ev;
                script.InvokeStateEvent("changed", (int)e.Flags);
            });

            StateEventHandlers.Add(typeof(CollisionEvent), HandleCollision);

            StateEventHandlers.Add(typeof(DataserverEvent), delegate(Script script, IScriptEvent ev)
            {
                DataserverEvent e = (DataserverEvent)ev;
                script.InvokeStateEvent("dataserver", new LSLKey(e.QueryID), e.Data);
            });

            StateEventHandlers.Add(typeof(MessageObjectEvent), HandleMessageObject);

            StateEventHandlers.Add(typeof(EmailEvent), delegate(Script script, IScriptEvent ev)
            {
                EmailEvent e = (EmailEvent)ev;
                script.InvokeStateEvent("email", e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
            });

            StateEventHandlers.Add(typeof(HttpResponseEvent), delegate(Script script, IScriptEvent ev)
            {
                HttpResponseEvent e = (HttpResponseEvent)ev;
                script.InvokeStateEvent("http_response", new LSLKey(e.RequestID), e.Status, e.Metadata, e.Body);
            });

            StateEventHandlers.Add(typeof(LandCollisionEvent), HandleLandCollision);

            StateEventHandlers.Add(typeof(LinkMessageEvent), delegate(Script script, IScriptEvent ev)
            {
                LinkMessageEvent e = (LinkMessageEvent)ev;
                script.InvokeStateEvent("link_message", e.SenderNumber, e.Number, e.Data, new LSLKey(e.Id));
            });

            StateEventHandlers.Add(typeof(ListenEvent), delegate(Script script, IScriptEvent ev)
            {
                ListenEvent e = (ListenEvent)ev;
                script.InvokeStateEvent("listen", e.Channel, e.Name, new LSLKey(e.ID), e.Message);
            });

            StateEventHandlers.Add(typeof(MoneyEvent), delegate(Script script, IScriptEvent ev)
            {
                MoneyEvent e = (MoneyEvent)ev;
                script.InvokeStateEvent("money", e.ID, e.Amount);
            });

            StateEventHandlers.Add(typeof(MovingStartEvent), delegate(Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("moving_start");
            });

            StateEventHandlers.Add(typeof(MovingEndEvent), delegate(Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("moving_end");
            });

            StateEventHandlers.Add(typeof(NoSensorEvent), delegate(Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("no_sensor");
            });

            StateEventHandlers.Add(typeof(NotAtRotTargetEvent), delegate(Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("not_at_rot_target");
            });

            StateEventHandlers.Add(typeof(NotAtTargetEvent), delegate (Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("not_at_target");
            });

            StateEventHandlers.Add(typeof(ObjectRezEvent), delegate (Script script, IScriptEvent ev)
            {
                ObjectRezEvent e = (ObjectRezEvent)ev;
                script.InvokeStateEvent("object_rez", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(OnRezEvent), delegate (Script script, IScriptEvent ev)
            {
                OnRezEvent e = (OnRezEvent)ev;
                script.StartParameter = new Integer(e.StartParam);
                script.InvokeStateEvent("on_rez", e.StartParam);
            });

            StateEventHandlers.Add(typeof(PathUpdateEvent), delegate (Script script, IScriptEvent ev)
            {
                PathUpdateEvent e = (PathUpdateEvent)ev;
                script.InvokeStateEvent("path_update", e.Type, e.Reserved);
            });

            StateEventHandlers.Add(typeof(RemoteDataEvent), delegate (Script script, IScriptEvent ev)
            {
                RemoteDataEvent e = (RemoteDataEvent)ev;
                script.InvokeStateEvent("remote_data", e.Type, new LSLKey(e.Channel), new LSLKey(e.MessageID), e.Sender, e.IData, e.SData);
            });

            StateEventHandlers.Add(typeof(ResetScriptEvent), delegate (Script script, IScriptEvent ev)
            {
                throw new ResetScriptException();
            });

            StateEventHandlers.Add(typeof(ItemSoldEvent), delegate (Script script, IScriptEvent ev)
            {
                ItemSoldEvent e = (ItemSoldEvent)ev;
                script.InvokeStateEvent("item_sold", e.Agent.FullName, new LSLKey(e.Agent.ID), e.ObjectName, new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(SensorEvent), HandleSensor);
            StateEventHandlers.Add(typeof(RuntimePermissionsEvent), HandleRuntimePermissions);
            StateEventHandlers.Add(typeof(TouchEvent), HandleTouch);
            StateEventHandlers.Add(typeof(TimerEvent), delegate (Script script, IScriptEvent ev)
            {
                script.InvokeStateEvent("timer");
            });
            #endregion
        }

        static void HandleCollision(Script script, IScriptEvent ev)
        {
            switch (((CollisionEvent)ev).Type)
            {
                case CollisionEvent.CollisionType.Start:
                    script.InvokeStateEvent("collision_start", script.m_Detected.Count);
                    break;

                case CollisionEvent.CollisionType.End:
                    script.InvokeStateEvent("collision_end", script.m_Detected.Count);
                    break;

                case CollisionEvent.CollisionType.Continuous:
                    script.InvokeStateEvent("collision", script.m_Detected.Count);
                    break;

                default:
                    break;
            }
        }

        static void HandleMessageObject(Script script, IScriptEvent ev)
        {
            MessageObjectEvent e = (MessageObjectEvent)ev;
            if (script.UseMessageObjectEvent)
            {
                script.InvokeStateEvent("object_message", new LSLKey(e.ObjectID), e.Data);
            }
            else
            {
                script.InvokeStateEvent("dataserver", new LSLKey(e.ObjectID), e.Data);
            }
        }

        static void HandleLandCollision(Script script, IScriptEvent ev)
        {
            LandCollisionEvent e = (LandCollisionEvent)ev;
            switch (e.Type)
            {
                case LandCollisionEvent.CollisionType.Start:
                    script.InvokeStateEvent("land_collision_start", e.Position);
                    break;

                case LandCollisionEvent.CollisionType.End:
                    script.InvokeStateEvent("land_collision_end", e.Position);
                    break;

                case LandCollisionEvent.CollisionType.Continuous:
                    script.InvokeStateEvent("land_collision", e.Position);
                    break;

                default:
                    break;
            }
        }

        static void HandleSensor(Script script, IScriptEvent ev)
        {
            SensorEvent e = (SensorEvent)ev;
            script.InvokeStateEvent("sensor", script.m_Detected.Count);
        }

        static void HandleRuntimePermissions(Script script, IScriptEvent ev)
        {
            RuntimePermissionsEvent e = (RuntimePermissionsEvent)ev;
            if (e.PermissionsKey != script.Item.Owner)
            {
                e.Permissions &= ~(ScriptPermissions.Debit | ScriptPermissions.SilentEstateManagement | ScriptPermissions.ChangeLinks);
            }
            if (e.PermissionsKey != script.Item.Owner)
            {
#warning Add group support here (also allowed are group owners)
                e.Permissions &= ~ScriptPermissions.ReturnObjects;
            }
            if (script.Item.IsGroupOwned)
            {
                e.Permissions &= ~ScriptPermissions.Debit;
            }

            ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
            grantinfo.PermsGranter = e.PermissionsKey;
            grantinfo.PermsMask = (ScriptPermissions)e.Permissions;
            script.Item.PermsGranter = grantinfo;
            script.InvokeStateEvent("run_time_permissions", (ScriptPermissions)e.Permissions);
        }

        static void HandleTouch(Script script, IScriptEvent ev)
        {
            TouchEvent e = (TouchEvent)ev;
            switch (e.Type)
            {
                case TouchEvent.TouchType.Start:
                    script.InvokeStateEvent("touch_start", script.m_Detected.Count);
                    break;

                case TouchEvent.TouchType.End:
                    script.InvokeStateEvent("touch_end", script.m_Detected.Count);
                    break;

                case TouchEvent.TouchType.Continuous:
                    script.InvokeStateEvent("touch", script.m_Detected.Count);
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Threat Level System

        public class Permissions
        {
            public readonly RwLockedList<UUI> Creators = new RwLockedList<UUI>();
            public readonly RwLockedList<UUI> Owners = new RwLockedList<UUI>();
            public bool IsAllowedForParcelOwner;
            public bool IsAllowedForParcelGroupMember;
            public bool IsAllowedForEstateOwner;
            public bool IsAllowedForEstateManager;
            public bool IsAllowedForRegionOwner;
            public bool IsAllowedForEveryone;

            public Permissions()
            {

            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        public enum ThreatLevelType : uint
        {
            None = 0,
            Nuisance = 1,
            VeryLow = 2,
            Low = 3,
            Moderate = 4,
            High = 5,
            VeryHigh = 6,
            Severe = 7
        }

        public const ThreatLevelType DefaultThreatLevel = ThreatLevelType.Low;

        public static readonly RwLockedDictionaryAutoAdd<UUID, ThreatLevelType> ThreatLevels = new RwLockedDictionaryAutoAdd<UUID, ThreatLevelType>(delegate () { return DefaultThreatLevel; });

        public static readonly RwLockedDictionaryAutoAdd<string,
            RwLockedDictionaryAutoAdd<UUID, Permissions>> OSSLPermissions = new RwLockedDictionaryAutoAdd<string, RwLockedDictionaryAutoAdd<UUID, Permissions>>(delegate ()
            {
                return new RwLockedDictionaryAutoAdd<UUID, Permissions>(
                    delegate ()
                    {
                        return new Permissions();
                    });
            });

        bool TryOSSLAllowed(
            SceneInterface scene,
            ParcelInfo pInfo,
            ObjectGroup objgroup,
            UUI creator,
            UUI owner,
            Permissions perms)
        {
            if (perms.IsAllowedForEveryone)
            {
                return true;
            }

            if (perms.Creators.Contains(creator))
            {
                return true;
            }

            if (perms.Owners.Contains(owner))
            {
                return true;
            }

            if (perms.IsAllowedForRegionOwner && scene.Owner.EqualsGrid(owner))
            {
                return true;
            }

            if (null != pInfo && (perms.IsAllowedForParcelOwner || perms.IsAllowedForParcelGroupMember))
            {
                if (owner.EqualsGrid(pInfo.Owner) && perms.IsAllowedForParcelOwner)
                {
                    return true;
                }

                GroupsServiceInterface groupsService = scene.GroupsService;
                if (groupsService != null && perms.IsAllowedForParcelGroupMember &&
                    groupsService.Members.ContainsKey(owner, pInfo.Group, owner))
                {
                    return true;
                }
            }

            if (perms.IsAllowedForEstateOwner &&
                objgroup.Scene.Owner.EqualsGrid(owner))
            {
                return true;
            }

            if (perms.IsAllowedForEstateManager &&
                scene.IsEstateManager(owner))
            {
                return true;
            }

            return false;
        }

        public void CheckThreatLevel(string name, ThreatLevelType level)
        {
            Permissions perms;
            ObjectPart part = Part;
            ObjectGroup objgroup = part.ObjectGroup;
            SceneInterface scene = objgroup.Scene;
            ObjectPart rootPart = objgroup.RootPart;
            UUI creator = rootPart.Creator;
            UUI owner = objgroup.Owner;
            ParcelInfo pInfo;

            if((int)level <= (int)ThreatLevels[scene.ID])
            {
                return;
            }
            else if((int)level <= (int)ThreatLevels[UUID.Zero])
            {
                return;
            }

            if (!scene.Parcels.TryGetValue(rootPart.GlobalPosition, out pInfo))
            {
                pInfo = null;
            }

            RwLockedDictionaryAutoAdd<UUID, Permissions> functionPerms;

            if (OSSLPermissions.TryGetValue(name, out functionPerms))
            {
                if (functionPerms.TryGetValue(scene.ID, out perms) &&
                    TryOSSLAllowed(scene, pInfo, objgroup, creator, owner, perms))
                {
                    return;
                }
                if (functionPerms.TryGetValue(UUID.Zero, out perms) &&
                    TryOSSLAllowed(scene, pInfo, objgroup, creator, owner, perms))
                {
                    return;
                }
            }

            throw new InvalidOperationException(string.Format("Function {0} not allowed", name));
        }
        #endregion
    }
}
