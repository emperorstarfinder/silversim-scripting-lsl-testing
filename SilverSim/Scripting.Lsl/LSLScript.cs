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
        public double CurrentTimerInterval = 0f;
        internal List<Action<ScriptInstance>> ScriptRemoveDelegates;
        internal List<Action<ScriptInstance, List<object>>> SerializationDelegates;

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
                    {
                        
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

        public void LoadScriptState(SavedScriptState state, Dictionary<string, Action<ScriptInstance, List<object>>> deserializationDelegates)
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

#warning Implement queue deserialization

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
                if(deserializationDelegates.TryGetValue(type, out del))
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

        private void InvokeStateEvent(string name, params object[] param)
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
                if(evt == typeof(AtRotTargetEvent))
                {
                    AtRotTargetEvent e = (AtRotTargetEvent)ev;
                    InvokeStateEvent("at_rot_target", e.TargetRotation, e.OurRotation);
                }
                else if(evt == typeof(AttachEvent))
                {
                    AttachEvent e = (AttachEvent)ev;
                    InvokeStateEvent("attach", new LSLKey(e.ObjectID));
                }
                else if(evt == typeof(AtTargetEvent))
                {
                    AtTargetEvent e = (AtTargetEvent)ev;
                    InvokeStateEvent("at_target", e.Handle, e.TargetPosition, e.OurPosition);
                }
                else if(evt == typeof(ChangedEvent))
                {
                    ChangedEvent e = (ChangedEvent)ev;
                    InvokeStateEvent("changed", (int)e.Flags);
                }
                else if(evt == typeof(CollisionEvent))
                {
                        m_Detected = ((CollisionEvent)ev).Detected;
                        switch (((CollisionEvent)ev).Type)
                        {
                            case CollisionEvent.CollisionType.Start:
                                InvokeStateEvent("collision_start", m_Detected.Count);
                                break;

                            case CollisionEvent.CollisionType.End:
                                InvokeStateEvent("collision_end", m_Detected.Count);
                                break;

                            case CollisionEvent.CollisionType.Continuous:
                                InvokeStateEvent("collision", m_Detected.Count);
                                break;

                            default:
                                break;
                        }
                }
                else if(evt == typeof(DataserverEvent))
                {
                    DataserverEvent e = (DataserverEvent)ev;
                    InvokeStateEvent("dataserver", new LSLKey(e.QueryID), e.Data);
                }
                else if(evt == typeof(MessageObjectEvent))
                {
                    MessageObjectEvent e = (MessageObjectEvent)ev;
                    if (UseMessageObjectEvent)
                    {
                        InvokeStateEvent("object_message", new LSLKey(e.ObjectID), e.Data);
                    }
                    else
                    {
                        InvokeStateEvent("dataserver", new LSLKey(e.ObjectID), e.Data);
                    }
                }
                else if(evt == typeof(EmailEvent))
                {
                    EmailEvent e = (EmailEvent)ev;
                    InvokeStateEvent("email", e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
                }
                else if(evt == typeof(HttpRequestEvent))
                {
                    HttpRequestEvent e = (HttpRequestEvent)ev;
                    InvokeStateEvent("http_request", new LSLKey(e.RequestID), e.Method, e.Body);
                }

                else if(evt == typeof(HttpResponseEvent))
                {
                    HttpResponseEvent e = (HttpResponseEvent)ev;
                    InvokeStateEvent("http_response", new LSLKey(e.RequestID), e.Status, e.Metadata, e.Body);
                }
                else if(evt == typeof(LandCollisionEvent))
                {
                    LandCollisionEvent e = (LandCollisionEvent)ev;
                    switch (e.Type)
                    {
                        case LandCollisionEvent.CollisionType.Start:
                            InvokeStateEvent("land_collision_start", e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.End:
                            InvokeStateEvent("land_collision_end", e.Position);
                            break;

                        case LandCollisionEvent.CollisionType.Continuous:
                            InvokeStateEvent("land_collision", e.Position);
                            break;

                        default:
                            break;
                    }
                }
                else if(evt == typeof(LinkMessageEvent))
                {
                    LinkMessageEvent e = (LinkMessageEvent)ev;
                    InvokeStateEvent("link_message", e.SenderNumber, e.Number, e.Data, new LSLKey(e.Id));
                }
                else if(evt == typeof(ListenEvent))
                {
                    ListenEvent e = (ListenEvent)ev;
                    InvokeStateEvent("listen", e.Channel, e.Name, new LSLKey(e.ID), e.Message);
                }
                else if(evt == typeof(MoneyEvent))
                {
                    MoneyEvent e = (MoneyEvent)ev;
                    InvokeStateEvent("money", e.ID, e.Amount);
                }
                else if(evt == typeof(MovingEndEvent))
                {
                    InvokeStateEvent("moving_end");
                }
                else if(evt == typeof(MovingStartEvent))
                {
                    InvokeStateEvent("moving_start");
                }
                else if(evt == typeof(NoSensorEvent))
                {
                    InvokeStateEvent("no_sensor");
                }
                else if(evt == typeof(NotAtRotTargetEvent))
                {
                    InvokeStateEvent("not_at_rot_target");
                }
                else if(evt == typeof(NotAtTargetEvent))
                {
                    InvokeStateEvent("not_at_target");
                }
                else if(evt == typeof(ObjectRezEvent))
                {
                    ObjectRezEvent e = (ObjectRezEvent)ev;
                    InvokeStateEvent("object_rez", new LSLKey(e.ObjectID));
                }
                else if(evt == typeof(OnRezEvent))
                {
                    OnRezEvent e = (OnRezEvent)ev;
                    StartParameter = new Integer(e.StartParam);
                    InvokeStateEvent("on_rez", e.StartParam);
                }
                else if(evt == typeof(PathUpdateEvent))
                {
                    PathUpdateEvent e = (PathUpdateEvent)ev;
                    InvokeStateEvent("path_update", e.Type, e.Reserved);
                }
                else if(evt == typeof(RemoteDataEvent))
                {
                    RemoteDataEvent e = (RemoteDataEvent)ev;
                    InvokeStateEvent("remote_data", e.Type, new LSLKey(e.Channel), new LSLKey(e.MessageID), e.Sender, e.IData, e.SData);
                }
                else if(evt == typeof(ResetScriptEvent))
                {
                    throw new ResetScriptException();
                }

                else if(evt == typeof(RuntimePermissionsEvent))
                {
                    RuntimePermissionsEvent e = (RuntimePermissionsEvent)ev;
                    if (e.PermissionsKey != Item.Owner)
                    {
                        e.Permissions &= ~(ScriptPermissions.Debit | ScriptPermissions.SilentEstateManagement | ScriptPermissions.ChangeLinks);
                    }
                    if (e.PermissionsKey != Item.Owner)
                    {
#warning Add group support here (also allowed are group owners)
                        e.Permissions &= ~ScriptPermissions.ReturnObjects;
                    }
                    if (Item.IsGroupOwned)
                    {
                        e.Permissions &= ~ScriptPermissions.Debit;
                    }

                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = new ObjectPartInventoryItem.PermsGranterInfo();
                    grantinfo.PermsGranter = e.PermissionsKey;
                    grantinfo.PermsMask = (ScriptPermissions)e.Permissions;
                    Item.PermsGranter = grantinfo;
                    InvokeStateEvent("run_time_permissions", (ScriptPermissions)e.Permissions);
                }
                else if(evt == typeof(SensorEvent))
                {
                    SensorEvent e = (SensorEvent)ev;
                    m_Detected = e.Data;
                    InvokeStateEvent("sensor", m_Detected.Count);
                }
                else if(evt == typeof(TouchEvent))
                {
                    TouchEvent e = (TouchEvent)ev;
                    m_Detected = e.Detected;
                    switch (e.Type)
                    {
                        case TouchEvent.TouchType.Start:
                            InvokeStateEvent("touch_start", m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.End:
                            InvokeStateEvent("touch_end", m_Detected.Count);
                            break;

                        case TouchEvent.TouchType.Continuous:
                            InvokeStateEvent("touch", m_Detected.Count);
                            break;

                        default:
                            break;
                    }
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
    }
}
