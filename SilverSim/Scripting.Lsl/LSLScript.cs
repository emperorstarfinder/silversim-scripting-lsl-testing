// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Timers;
using System.Xml;
using ThreadedClasses;

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

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            lock (this)
            {
                PostEvent(new TimerEvent());
                LastTimerEventTick = Environment.TickCount;
                Timer.Interval = CurrentTimerInterval;
            }
        }

        public void SetTimerEvent(double interval, double elapsed = 0f)
        {
            CurrentTimerInterval = interval;
            if(interval < 0.01)
            {
                Timer.Enabled = false;
            }
            else
            {
                Timer.Enabled = false;
                LastTimerEventTick = Environment.TickCount;
                Timer.Interval = interval - elapsed;
                CurrentTimerInterval = interval;
                Timer.Enabled = true;
            }
        }

        public override double ExecutionTime
        {
            get
            {
                lock(this) return m_ExecutionTime;
            }
            set
            {
                lock(this) m_ExecutionTime = value;
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

        void ListToXml(XmlTextWriter writer, string name, AnArray array)
        {
            writer.WriteStartElement("Variable");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("type", "list");
            foreach(IValue val in array)
            {
                Type valtype = val.GetType();
                if(valtype == typeof(Integer))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                    writer.WriteValue(val.AsInt);
                    writer.WriteEndElement();
                }
                else if(valtype == typeof(Real))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                    double v = (Real)val;
                    writer.WriteValue(v);
                    writer.WriteEndElement();
                }
                else if(valtype == typeof(Quaternion))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                    writer.WriteValue(val.ToString());
                    writer.WriteEndElement();
                }
                else if(valtype == typeof(Vector3))
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

        public void ToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("State");
            writer.WriteStartAttribute("UUID");
            writer.WriteValue(Item.ID);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("Asset");
            writer.WriteValue(Item.AssetID);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("Engine");
            writer.WriteValue("XEngine");
            writer.WriteEndAttribute();
            {
                writer.WriteStartElement("ScriptState");
                {
                    string current_state = "default";
                    foreach (KeyValuePair<string, ILSLState> kvp in m_States)
                    {
                        if (kvp.Value == m_CurrentState)
                        {
                            current_state = kvp.Key;
                        }
                    }
                    writer.WriteStartElement("State");
                    {
                        writer.WriteValue(current_state);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Running");
                    {
                        writer.WriteValue(IsRunning);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Variables");
                    foreach(FieldInfo fi in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    { 
                        if(!fi.Name.StartsWith("var_"))
                        {
                            continue;
                        }
                        if(fi.FieldType == typeof(int))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                        }
                        else if(fi.FieldType == typeof(double))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                        }
                        else if (fi.FieldType == typeof(Vector3))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                        }
                        else if (fi.FieldType == typeof(Quaternion))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                        }
                        else if (fi.FieldType == typeof(UUID) || fi.FieldType == typeof(LSLKey))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key");
                        }
                        else if(fi.FieldType == typeof(string))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", fi.Name.Substring(4));
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString");
                        }
                        else if(fi.FieldType == typeof(AnArray))
                        {
                            ListToXml(writer, fi.Name, (AnArray)fi.GetValue(this));
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                        writer.WriteValue(fi.GetValue(this).ToString());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Queue");
                    {

                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Plugins");
                    List<object> serializationData = new List<object>();
                    foreach(Action<ScriptInstance, List<object>> serializer in SerializationDelegates)
                    {
                        serializer(this, serializationData);
                    }
                    foreach(object o in serializationData)
                    {
                        writer.WriteTypedValue("ListItem", o);
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public Script(ObjectPart part, ObjectPartInventoryItem item, bool forcedSleepDefault)
        {
            UseForcedSleep = forcedSleepDefault;
            m_Part = part;
            m_Item = item;
            Timer.Elapsed += OnTimerEvent;
            /* we replace the loaded script state with ours */
            m_Item.ScriptState = this;
            m_Part.OnUpdate += OnPrimUpdate;
            m_Part.OnPositionChange += OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate += OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange += OnGroupPositionUpdate;
        }

        static bool TryTranslateEventParams(SavedScriptState.EventParams ep, out IScriptEvent res)
        {
            switch(ep.EventName)
            {
                case "object_rez":
                    if(ep.Params.Count >= 1)
                    {
                        ObjectRezEvent ev = new ObjectRezEvent();
                        ev.ObjectID = new UUID(ep.Params[0].ToString());
                        res = ev;
                        return true;
                    }
                    break;

                case "email":
                    if(ep.Params.Count >= 5)
                    {
                        EmailEvent ev = new EmailEvent();
                        ev.Time = ep.Params[0].ToString();
                        ev.Address = ep.Params[1].ToString();
                        ev.Subject = ep.Params[2].ToString();
                        ev.Message = ep.Params[3].ToString();
                        ev.NumberLeft = (int)ep.Params[4];
                        res = ev;
                        return true;
                    }
                    break;

                case "run_time_permissions":
                    if(ep.Params.Count >= 1)
                    {
                        RuntimePermissionsEvent ev = new RuntimePermissionsEvent();
                        ev.Permissions = (ScriptPermissions)(int)ep.Params[0];
                        if(ep.Params.Count > 1)
                        {
                            ev.PermissionsKey = new UUI(ep.Params[1].ToString());
                        }
                        res = ev;
                        return true;
                    }
                    break;

                case "link_message":
                    if(ep.Params.Count >= 4)
                    {
                        LinkMessageEvent ev = new LinkMessageEvent();
                        ev.SenderNumber = (int)ep.Params[0];
                        ev.Number = (int)ep.Params[1];
                        ev.Data = ep.Params[2].ToString();
                        ev.Id = ep.Params[3].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "remote_data":
                    if(ep.Params.Count >= 6)
                    {
                        RemoteDataEvent ev = new RemoteDataEvent();
                        ev.Type = (int)ep.Params[0];
                        ev.Channel = new UUID(ep.Params[1].ToString());
                        ev.MessageID = new UUID(ep.Params[2].ToString());
                        ev.Sender = ep.Params[3].ToString();
                        ev.IData = (int)ep.Params[4];
                        ev.SData = ep.Params[5].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "transaction_result":
                    if(ep.Params.Count >= 3)
                    {
                        TransactionResultEvent ev = new TransactionResultEvent();
                        ev.TransactionID = ep.Params[0].ToString();
                        ev.Success = (int)ep.Params[1] != 0;
                        ev.ReplyData = ep.Params[2].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "dataserver":
                    if(ep.Params.Count >= 2)
                    {
                        DataserverEvent ev = new DataserverEvent();
                        ev.QueryID = new UUID(ep.Params[0].ToString());
                        ev.Data = ep.Params[1].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "http_response":
                    if(ep.Params.Count >= 4)
                    {
                        HttpResponseEvent ev = new HttpResponseEvent();
                        ev.RequestID = new UUID(ep.Params[0].ToString());
                        ev.Status = (int)ep.Params[1];
                        ev.Metadata = (AnArray)ep.Params[2];
                        ev.Body = ep.Params[3].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "listen":
                    if(ep.Params.Count >= 4)
                    {
                        ListenEvent ev = new ListenEvent();
                        ev.Channel = (int)ep.Params[0];
                        ev.Name = ep.Params[1].ToString();
                        ev.ID = new UUID(ep.Params[2].ToString());
                        ev.Message = ep.Params[3].ToString();
                        res = ev;
                        return true;
                    }
                    break;

                case "no_sensor":
                    res = new NoSensorEvent();
                    return true;

                case "sensor":
                    {
                        SensorEvent ev = new SensorEvent();
                        ev.Data = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "timer":
                    res = new TimerEvent();
                    return true;

                case "on_rez":
                    if(ep.Params.Count >= 1)
                    {
                        OnRezEvent ev = new OnRezEvent();
                        ev.StartParam = (int)ep.Params[0];
                        res = ev;
                        return true;
                    }
                    break;

                case "attach":
                    if(ep.Params.Count >= 1)
                    {
                        AttachEvent ev = new AttachEvent();
                        ev.ObjectID = new UUID(ep.Params[0].ToString());
                        res = ev;
                        return true;
                    }
                    break;

                case "changed":
                    if(ep.Params.Count >= 1)
                    {
                        ChangedEvent ev = new ChangedEvent();
                        ev.Flags = (ChangedEvent.ChangedFlags)(int)ep.Params[0];
                        res = ev;
                        return true;
                    }
                    break;

                case "touch_start":
                    {
                        TouchEvent ev = new TouchEvent();
                        ev.Type = TouchEvent.TouchType.Start;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "touch":
                    {
                        TouchEvent ev = new TouchEvent();
                        ev.Type = TouchEvent.TouchType.Continuous;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "touch_end":
                    {
                        TouchEvent ev = new TouchEvent();
                        ev.Type = TouchEvent.TouchType.End;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "money":
                    if(ep.Params.Count >= 2)
                    {
                        MoneyEvent ev = new MoneyEvent();
                        ev.ID = new UUID(ep.Params[0].ToString());
                        ev.Amount = (int)ep.Params[1];
                        res = ev;
                        return true;
                    }
                    break;

                case "collision_start":
                    {
                        CollisionEvent ev = new CollisionEvent();
                        ev.Type = CollisionEvent.CollisionType.Start;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "collision":
                    {
                        CollisionEvent ev = new CollisionEvent();
                        ev.Type = CollisionEvent.CollisionType.Continuous;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "collision_end":
                    {
                        CollisionEvent ev = new CollisionEvent();
                        ev.Type = CollisionEvent.CollisionType.End;
                        ev.Detected = ep.Detected;
                        res = ev;
                        return true;
                    }

                case "land_collision_start":
                    if(ep.Params.Count >= 1)
                    {
                        LandCollisionEvent ev = new LandCollisionEvent();
                        ev.Type = LandCollisionEvent.CollisionType.Start;
                        ev.Position = (Vector3)ep.Params[0];
                        res = ev;
                        return true;
                    }
                    break;

                case "land_collision":
                    if (ep.Params.Count >= 1)
                    {
                        LandCollisionEvent ev = new LandCollisionEvent();
                        ev.Type = LandCollisionEvent.CollisionType.Continuous;
                        ev.Position = (Vector3)ep.Params[0];
                        res = ev;
                        return true;
                    }
                    break;

                case "land_collision_end":
                    if (ep.Params.Count >= 1)
                    {
                        LandCollisionEvent ev = new LandCollisionEvent();
                        ev.Type = LandCollisionEvent.CollisionType.End;
                        ev.Position = (Vector3)ep.Params[0];
                        res = ev;
                        return true;
                    }
                    break;

                case "control":
                    if (ep.Params.Count >= 3)
                    {
                        ControlEvent ev = new ControlEvent();
                        ev.AgentID = new UUID(ep.Params[0].ToString());
                        ev.Level = (int)ep.Params[1];
                        ev.Flags = (int)ep.Params[2];
                        res = ev;
                        return true;
                    }
                    break;

                case "at_target":
                    if(ep.Params.Count >= 3)
                    {
                        AtTargetEvent ev = new AtTargetEvent();
                        ev.Handle = (int)ep.Params[0];
                        ev.TargetPosition = (Vector3)ep.Params[1];
                        ev.OurPosition = (Vector3)ep.Params[2];
                        res = ev;
                        return true;
                    }
                    break;

                case "not_at_target":
                    res = new NotAtTargetEvent();
                    return true;

                case "at_rot_target":
                    if(ep.Params.Count >= 3)
                    {
                        AtRotTargetEvent ev = new AtRotTargetEvent();
                        ev.Handle = (int)ep.Params[0];
                        ev.TargetRotation = (Quaternion)ep.Params[1];
                        ev.OurRotation = (Quaternion)ep.Params[2];
                        res = ev;
                        return true;
                    }
                    break;

                case "moving_start":
                    res = new MovingStartEvent();
                    return true;

                case "moving_end":
                    res = new MovingEndEvent();
                    return true;

                default:
                    break;
            }

            res = null;
            return false;
        }

        public void LoadScriptState(SavedScriptState state)
        {
            /* we have to integrate the loaded script state */
            Type scriptType = GetType();

            /* initialize variables */
            foreach (KeyValuePair<string, object> kvp in state.Variables)
            {
                FieldInfo fi = scriptType.GetField(kvp.Key);
                if(fi == null)
                {
                    m_Log.ErrorFormat("Restoring variable {0} failed", kvp.Key);
                }
                else if(fi.IsLiteral || fi.IsInitOnly || fi.FieldType != kvp.Value.GetType())
                {
                    continue;
                }
                fi.SetValue(this, kvp.Value);
            }

            /* initialize state */
            ILSLState script_state;
            if (m_States.TryGetValue(state.CurrentState, out script_state))
            {
                m_CurrentState = script_state;
                m_CurrentStateMethods.Clear();
            }

            /* queue deserialization */
            foreach(SavedScriptState.EventParams ep in state.EventData)
            {
                IScriptEvent ev;
                if(TryTranslateEventParams(ep, out ev))
                {
                    m_Events.Enqueue(ev);
                }
            }

            /* initialize plugin data */
            int pluginpos = 0;
            int pluginlen = state.PluginData.Count;
            while(pluginpos + 1 < pluginlen)
            {
                int len = (int)state.PluginData[pluginpos + 1];
                if (len + 2 + pluginpos > pluginlen)
                {
                    break;
                }
                string type = (string)state.PluginData[0];
                Action<ScriptInstance, List<object>> del;
                if(DeserializationDelegates.TryGetValue(type, out del))
                {
                    del(this, state.PluginData.GetRange(pluginpos + 2, len));
                }
                pluginpos += 2 + len;
            }
            IsRunning = state.IsRunning;
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
                m_Log.FatalFormat("Within state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.InnerException.GetType().FullName, e.InnerException.Message, e.InnerException.StackTrace);
            }
            else
            {
                m_Log.FatalFormat("Within state {0} event {1}:\nException {2} at script execution: {3}\n{4}",
                    state_name, name,
                    e.GetType().FullName, e.Message, e.StackTrace);
            }
        }

        public override bool IsLinkMessageReceiver
        {
            get
            {
                return m_CurrentStateMethods.ContainsKey("link_message");
            }
        }

        static internal void InvokeStateEvent(Script script, string name, object[] param)
        {
            script.InvokeStateEventReal(name, param);
        }

        void InvokeStateEvent(string name, params object[] param)
        {
            InvokeStateEventReal(name, param);
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
                catch (TargetInvocationException e)
                {
                    LogInvokeException(name, e);
                    throw;
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    throw;
                }
                catch (TargetParameterCountException e)
                {
                    LogInvokeException(name, e);
                    throw;
                }
                catch (TargetException e)
                {
                    LogInvokeException(name, e);
                    throw;
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

                string funcSignature = methodName + "(";

                ParameterInfo[] pi = mb.GetParameters();
                for (int i = 1; i < pi.Length; ++i)
                {
                    if (i > 1)
                    {
                        funcSignature += ", ";
                    }
                    funcSignature = MapTypeToString(pi[i].ParameterType);
                }
                funcSignature += ")";

                ShoutError("Script called unimplemented function " + funcSignature);
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public override void ProcessEvent()
        {
            IScriptEvent ev;
            int exectime;
            float execfloat;
            int startticks = Environment.TickCount;
            try
            {
                ev = m_Events.Dequeue();
                if(m_CurrentState == null)
                {
                    m_CurrentState = m_States["default"];
                    InvokeStateEvent("state_entry");
                    if (ev.GetType() == typeof(ResetScriptEvent))
                    {
                        return;
                    }
                }
            }
            catch(NotImplementedException e)
            {
                ShoutUnimplementedException(e);
                return;
            }
            catch (ResetScriptException)
            {
                ShoutError("Script error! llResetScript used in default.state_entry");
                return;
            }
            catch (Exception e)
            {
                ShoutError(e.Message);
                return;
            }
            finally
            {
                exectime = Environment.TickCount - startticks;
                execfloat = exectime / 1000f;
                lock (this)
                {
                    m_ExecutionTime += execfloat;
                }
            }

            startticks = Environment.TickCount;
            try
            {
                Type evt = ev.GetType();
                Action<Script, IScriptEvent> evtDelegate;
                if(StateEventHandlers.TryGetValue(evt, out evtDelegate))
                {
                    evtDelegate(this, ev);
                }
            }
            catch (ResetScriptException)
            {
                TriggerOnStateChange();
                TriggerOnScriptReset();
                m_Events.Clear();
                lock(this)
                {
                    m_ExecutionTime = 0f;
                }
                m_CurrentState = m_States["default"];
                m_CurrentStateMethods.Clear();
                StartParameter = 0;
                ResetVariables();
                startticks = Environment.TickCount;
                InvokeStateEvent("state_entry");
            }
            catch(System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch(ChangeStateException e)
            {
                if (m_CurrentState != m_States[e.NewState])
                {
                    /* if state is equal, it simply aborts the event execution */
                    TriggerOnStateChange();
                    m_Events.Clear();
                    InvokeStateEvent("state_exit");
                    m_CurrentState = m_States[e.NewState];
                    m_CurrentStateMethods.Clear();
                    InvokeStateEvent("state_entry");
                }
            }
            catch (NotImplementedException e)
            {
                ShoutUnimplementedException(e);
            }
            catch (Exception e)
            {
                ShoutError(e.Message);
            }
            exectime = Environment.TickCount - startticks;
            execfloat = exectime / 1000f;
            lock(this)
            {
                m_ExecutionTime += execfloat;
            }
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


        public override void ShoutError(string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = 0x7FFFFFFF; /* DEBUG_CHANNEL */
            ev.Type = ListenEvent.ChatType.Shout;
            ChatServiceInterface chatService;
            lock (this)
            {
                ObjectPart part = Part;
                ObjectGroup objGroup = part.ObjectGroup;
                ev.Message = "At region " + objGroup.Scene.Name + ":\n" + message;
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                ev.OwnerID = objGroup.Owner.ID;
                ev.GlobalPosition = objGroup.GlobalPosition;
                ev.ID = objGroup.ID;
                ev.Name = objGroup.Name;
                chatService = objGroup.Scene.GetService<ChatServiceInterface>();
            }
            if (null != chatService)
            {
                chatService.Send(ev);
            }
        }

        #region Event to function handlers

        static Script()
        {
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

            StateEventHandlers.Add(typeof(SensorEvent), HandleSensor);
            StateEventHandlers.Add(typeof(RuntimePermissionsEvent), HandleRuntimePermissions);
            StateEventHandlers.Add(typeof(TouchEvent), HandleTouch);
        }

        static void HandleCollision(Script script, IScriptEvent ev)
        {
            script.m_Detected = ((CollisionEvent)ev).Detected;
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
            script.m_Detected = e.Data;
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
            script.m_Detected = e.Detected;
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
    }
}
