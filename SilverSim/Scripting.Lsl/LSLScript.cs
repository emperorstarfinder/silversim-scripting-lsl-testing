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

#pragma warning disable RCS1029, IDE0018, IDE0019

using log4net;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Api.ByteString;
using SilverSim.Scripting.Lsl.Api.Primitive;
using SilverSim.Scripting.Lsl.Event;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl
{
    public abstract partial class Script : ScriptInstance, IScriptState
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LSLSCRIPT");
        private ObjectPart m_Part;
        private readonly NonblockingQueue<IScriptEvent> m_Events = new NonblockingQueue<IScriptEvent>();
        internal List<DetectInfo> m_Detected = new List<DetectInfo>();
        private readonly Dictionary<string, ILSLState> m_States = new Dictionary<string, ILSLState>();
        private ILSLState m_CurrentState;
        public int StartParameter;
        internal RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();
        private double m_ExecutionTime;
        protected bool UseMessageObjectEvent;
        internal RwLockedList<UUID> m_RequestedURLs = new RwLockedList<UUID>();

        public readonly System.Timers.Timer Timer = new System.Timers.Timer();
        public long LastTimerEventTick;
        public bool UseForcedSleep = true;
        public double ForcedSleepFactor = 1;
        public double CurrentTimerInterval;
        internal List<Action<ScriptInstance>> ScriptRemoveDelegates;
        internal List<Action<ScriptInstance, List<object>>> SerializationDelegates;
        internal Dictionary<string, Action<ScriptInstance, List<object>>> DeserializationDelegates;
        static internal readonly Dictionary<Type, Action<Script, IScriptEvent>> StateEventHandlers = new Dictionary<Type, Action<Script, IScriptEvent>>();
        private readonly object m_Lock = new object();
        protected bool m_UsesSinglePrecision;
        public bool UsesSinglePrecision => m_UsesSinglePrecision;
        public static readonly TimeProvider TimeSource = TimeProvider.StopWatch;

        private long m_ExecutionStartedAt = TimeSource.TickCount;

        public string GetCurrentState()
        {
            foreach(KeyValuePair<string, ILSLState> kvp in m_States)
            {
                if(kvp.Value == m_CurrentState)
                {
                    return kvp.Key;
                }
            }
            return string.Empty;
        }

        public double GetAndResetTime()
        {
            long newvalue = TimeSource.TickCount;
            long oldvalue = Interlocked.Exchange(ref m_ExecutionStartedAt, newvalue);
            return (ulong)TimeSource.TicksElapsed(newvalue, oldvalue) / (double)TimeSource.Frequency;
        }

        public double GetTime()
        {
            return (ulong)TimeSource.TicksElapsed(TimeSource.TickCount, m_ExecutionStartedAt) / (double)TimeSource.Frequency;
        }

        private bool m_HasTouchEvent;
        private bool m_HasMoneyEvent;
        private bool m_HaveQueuedTimerEvent;
        private bool m_HasLandCollisionEvent;
        private bool m_HasLandCollisionStartEvent;
        private bool m_HasLandCollisionEndEvent;
        private bool m_HasCollisionEvent;
        private bool m_HasCollisionStartEvent;
        private bool m_HasCollisionEndEvent;

        public override bool HasTouchEvent => m_HasTouchEvent;
        public override bool HasMoneyEvent => m_HasMoneyEvent;

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            lock (m_Lock)
            {
                if (!m_HaveQueuedTimerEvent)
                {
                    PostEvent(new TimerEvent());
                }
                Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
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
                    Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
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

        private ReferenceBoxed<double> m_MinEventDelay = 0;
        public double MinEventDelay
        {
            get { return m_MinEventDelay; }
            set { m_MinEventDelay = (value < 0) ? 0 : value; }
        }

        private readonly ScriptStates.ScriptState m_TransactionedState = new ScriptStates.ScriptState();

        private void UpdateScriptState()
        {
            var state = new ScriptStates.ScriptState
            {
                EventData = m_Events.ToArray(),
                AssetID = Item.AssetID,
                ItemID = Item.ID,
                MinEventDelay = MinEventDelay,
                CurrentState = "default",
                UseMessageObjectEvent = UseMessageObjectEvent,
                PermsGranter = Item.PermsGranter
            };
            foreach (KeyValuePair<string, ILSLState> kvp in m_States)
            {
                if (kvp.Value == m_CurrentState)
                {
                    state.CurrentState = kvp.Key;
                }
            }
            foreach (FieldInfo fi in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!fi.Name.StartsWith("var_"))
                {
                    continue;
                }
                if (fi.FieldType == typeof(int) ||
                    fi.FieldType == typeof(Vector3) ||
                    fi.FieldType == typeof(double) ||
                    fi.FieldType == typeof(long) ||
                    fi.FieldType == typeof(Quaternion) ||
                    fi.FieldType == typeof(UUID))
                {
                    state.Variables[fi.Name.Substring(4)] = fi.GetValue(this);
                }
                else if (fi.FieldType == typeof(LSLKey))
                {
                    state.Variables[fi.Name.Substring(4)] = new LSLKey(fi.GetValue(this).ToString());
                }
                else if (fi.FieldType == typeof(string))
                {
                    state.Variables[fi.Name.Substring(4)] = (string)fi.GetValue(this);
                }
                else if (fi.FieldType == typeof(AnArray))
                {
                    state.Variables[fi.Name.Substring(4)] = new AnArray((AnArray)fi.GetValue(this));
                }
            }

            foreach (Action<ScriptInstance, List<object>> serializer in SerializationDelegates)
            {
                serializer(this, state.PluginData);
            }
        }

        public void ToXml(XmlTextWriter writer)
        {
            m_TransactionedState.ToXml(writer);
        }

        public byte[] ToDbSerializedState()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = ms.UTF8XmlTextWriter())
                {
                    ToXml(writer);
                }
                return ms.ToArray();
            }
        }

        protected Script(ObjectPart part, ObjectPartInventoryItem item, bool forcedSleepDefault)
        {
            UseForcedSleep = forcedSleepDefault;
            m_Part = part;
            Item = item;
            m_TransactionedState.ItemID = item.ID;
            m_TransactionedState.AssetID = item.AssetID;
            Timer.Elapsed += OnTimerEvent;
            /* we replace the loaded script state with ours */
            Item.ScriptState = this;
            m_Part.OnUpdate += OnPrimUpdate;
            m_Part.OnPositionChange += OnPrimPositionUpdate;
            m_Part.ObjectGroup.OnUpdate += OnGroupUpdate;
            m_Part.ObjectGroup.OnPositionChange += OnGroupPositionUpdate;
        }

        public void LoadScriptState(ILSLScriptState state)
        {
            /* we have to integrate the loaded script state */
            Type scriptType = GetType();

            /* initialize variables */
            foreach (KeyValuePair<string, object> kvp in state.Variables)
            {
                FieldInfo fi = scriptType.GetField("var_" + kvp.Key, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                Type loadType = kvp.Value.GetType();
                if (fi == null)
                {
                    m_Log.ErrorFormat("Restoring variable {0} failed for {1}", kvp.Key, scriptType.FullName);
                    continue;
                }
                else if (fi.IsLiteral || fi.IsInitOnly)
                {
                    continue;
                }
                if (fi.FieldType == typeof(LSLKey) && loadType == typeof(string))
                {
                    fi.SetValue(this, new LSLKey((string)kvp.Value));
                }
                else if (fi.FieldType == loadType)
                {
                    fi.SetValue(this, kvp.Value);
                    MethodInfo initMi = fi.FieldType.GetMethod("RestoreFromSerialization", new Type[] { typeof(ScriptInstance) });
                    if (initMi != null)
                    {
                        initMi.Invoke(kvp.Value, new object[] { this });
                    }
                }
            }

            /* initialize state */
            ILSLState script_state;
            if (m_States.TryGetValue(state.CurrentState, out script_state))
            {
                SetCurrentState(script_state);
            }

            /* queue deserialization */
            foreach (IScriptEvent ev in state.EventData)
            {
                m_Events.Enqueue(ev);
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
                var disp = new StringBuilder();
                int pos = 0;
                foreach(object o in state.PluginData)
                {
                    disp.Append((pos++).ToString()).Append(":").Append(o.ToString()).Append(" ");
                }
                m_Log.WarnFormat("Deserialization of state failed at position {0}: {1}\n=> {2}: {3}\n{4}", pluginpos, disp.ToString(), e.GetType().FullName, e.Message, e.StackTrace);
                throw;
            }

            IsRunning = state.IsRunning;

#if DEBUG
            m_Log.DebugFormat("Set script to IsRunning={0} from IsRunning={1}", IsRunning, state.IsRunning);
#endif

            lock(this) /* really needed to prevent aborting here */
            {
                UpdateScriptState();
            }
        }

        private void OnPrimPositionUpdate(IObject part)
        {
        }

        private void OnGroupPositionUpdate(IObject group)
        {
        }

        public abstract void ResetVariables();

        private void OnPrimUpdate(ObjectPart part, UpdateChangedFlags flags)
        {
            var changedflags = (ChangedEvent.ChangedFlags)(uint)flags;
            if (changedflags != 0)
            {
                PostEvent(new ChangedEvent
                {
                    Flags = changedflags
                });
            }
        }

        private void OnGroupUpdate(ObjectGroup group, UpdateChangedFlags flags)
        {
            var changedflags = (ChangedEvent.ChangedFlags)(uint)flags;
            if (changedflags != 0)
            {
                PostEvent(new ChangedEvent
                {
                    Flags = changedflags
                });
            }
        }

        public override ObjectPart Part => m_Part;

        public override ObjectPartInventoryItem Item { get; }

        public override void PostEvent(IScriptEvent e)
        {
            if (IsRunning && !IsAborting)
            {
                CollisionEvent cev;
                LandCollisionEvent lcev;
                if(e is TimerEvent)
                {
                    m_HaveQueuedTimerEvent = true;
                }
                else if((cev = e as CollisionEvent) != null)
                {
                    switch (cev.Type)
                    {
                        case CollisionEvent.CollisionType.Start:
                            if (!m_HasCollisionStartEvent)
                            {
                                return;
                            }
                            break;

                        case CollisionEvent.CollisionType.Continuous:
                            if (!m_HasCollisionEvent)
                            {
                                return;
                            }
                            break;

                        case CollisionEvent.CollisionType.End:
                            if (!m_HasCollisionEndEvent)
                            {
                                return;
                            }
                            break;
                    }
                }
                else if((lcev = e as LandCollisionEvent) != null)
                {
                    switch(lcev.Type)
                    {
                        case LandCollisionEvent.CollisionType.Start:
                            if(!m_HasLandCollisionStartEvent)
                            {
                                return;
                            }
                            break;

                        case LandCollisionEvent.CollisionType.Continuous:
                            if(!m_HasLandCollisionEvent)
                            {
                                return;
                            }
                            break;

                        case LandCollisionEvent.CollisionType.End:
                            if(!m_HasLandCollisionEndEvent)
                            {
                                return;
                            }
                            break;
                    }
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

        public sealed class StartScriptEvent : IScriptEvent
        {
        }

        public override void Start(int startparam = 0)
        {
            if (!IsRunning && !IsAborting)
            {
                StartParameter = startparam;
                IsRunning = true;
                m_Events.Enqueue(new StartScriptEvent());
                Part.ObjectGroup.Scene.ScriptThreadPool.PostScript(this);
            }
        }

        public override bool IsRunning { get; set; }

        public override void Remove()
        {
            Timer.Stop();
            Timer.Elapsed -= OnTimerEvent;
            try
            {
                m_Part.OnUpdate -= OnPrimUpdate;
                m_Part.OnPositionChange -= OnPrimPositionUpdate;
                m_Part.ObjectGroup.OnUpdate -= OnGroupUpdate;
                m_Part.ObjectGroup.OnPositionChange -= OnGroupPositionUpdate;
            }
            catch
            {
                /* ignore intentionally */
            }
            if(ScriptRemoveDelegates != null)
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

        private readonly RwLockedDictionary<string, MethodInfo> m_CurrentStateMethods = new RwLockedDictionary<string, MethodInfo>();

        public override void RevokePermissions(UUID permissionsKey, ScriptPermissions permissions)
        {
            ObjectPartInventoryItem thisItem = Item;
            ObjectPart thisPart = Part;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = thisItem.PermsGranter;
            if (permissionsKey == grantinfo.PermsGranter.ID && grantinfo.PermsGranter != UGUI.Unknown)
            {
                IAgent agent;
                if(!thisPart.ObjectGroup.Scene.Agents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                   return;
                }
                agent.RevokePermissions(thisPart.ID, thisItem.ID, (~permissions) & (grantinfo.PermsMask));
                grantinfo.PermsMask &= ~permissions;
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

        bool m_IsLinkMessageReceiver;
        public override bool IsLinkMessageReceiver => m_IsLinkMessageReceiver;

        static public void InvokeStateEvent(Script script, string name, object[] param)
        {
            script.InvokeStateEventReal(name, param);
        }

        private void InvokeStateEvent(string name, params object[] param)
        {
            InvokeStateEventReal(name, param);
        }

        private int m_RecursionCount;
        private static int m_CallDepthLimit = 40;
        static public int CallDepthLimit
        {
            get { return m_CallDepthLimit; }
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

        private void SetCurrentState(ILSLState state)
        {
            m_CurrentState = state;
            m_CurrentStateMethods.Clear();

            m_HasTouchEvent = HasStateEvent("touch") || HasStateEvent("touch_start") || HasStateEvent("touch_end");
            m_HasMoneyEvent = HasStateEvent("money");
            m_IsLinkMessageReceiver = HasStateEvent("link_message");
            m_HasLandCollisionEvent = HasStateEvent("land_collision");
            m_HasLandCollisionStartEvent = HasStateEvent("land_collision_start");
            m_HasLandCollisionEndEvent = HasStateEvent("land_collision_end");
            m_HasCollisionEvent = HasStateEvent("collision");
            m_HasCollisionStartEvent = HasStateEvent("collision_start");
            m_HasCollisionEndEvent = HasStateEvent("collision_end");
        }

        private bool HasStateEvent(string name)
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

            if (mi != null)
            {
                IncrementScriptEventCounter();
                try
                {
                    foreach(object p in param)
                    {
                        if(p == null)
                        {
                            var sb = new StringBuilder("Call failure at ");
                            sb.Append(name + "(");
                            bool first = true;
                            foreach (object pa in param)
                            {
                                if (!first)
                                {
                                    sb.Append(",");
                                }
                                first = false;
                                if (pa == null)
                                {
                                    sb.Append("null");
                                }
                                else if (pa is LSLKey || pa is string)
                                {
                                    sb.Append("\"" + pa + "\"");
                                }
                                else if (pa is char)
                                {
                                    sb.Append("\'" + pa + "\'");
                                }
                                else
                                {
                                    sb.Append(pa.ToString());
                                }
                            }
                            sb.Append(")");
                            m_Log.Debug(sb);
                            return;
                        }
                    }

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
                    if(innerType == typeof(NotImplementedException))
                    {
                        ShoutUnimplementedException(e.InnerException as NotImplementedException);
                        return;
                    }
                    else if(innerType == typeof(DeprecatedFunctionCalledException))
                    {
                        ShoutDeprecatedException(e.InnerException as DeprecatedFunctionCalledException);
                        return;
                    }
                    else if(innerType == typeof(HitSandboxLimitException))
                    {
                        ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                    }
                    else if (innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException) ||
                        innerType == typeof(LocalizedScriptErrorException) ||
                        innerType == typeof(DivideByZeroException))
                    {
                        throw e.InnerException;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (DeprecatedFunctionCalledException e)
                {
                    ShoutDeprecatedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch(HitSandboxLimitException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                }
                catch (CallDepthLimitViolationException e)
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
                catch(ScriptWorkerInterruptionException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        internal void InvokeRpcEventReal(string name, object[] param)
        {
            var paratypes = new List<Type>();
            object[] realparams = new object[param.Length];
            for(int i = 0; i < param.Length; ++i)
            {
                object p = param[i];
                Type pType = p.GetType();
                paratypes.Add(pType);
                realparams[i] = p;
                MethodInfo restoreMethod = pType.GetMethod("RestoreFromSerialization", new Type[] { typeof(ScriptInstance) });
                if (restoreMethod != null)
                {
                    ConstructorInfo cInfo = pType.GetConstructor(new Type[] { pType });
                    p = cInfo.Invoke(new object[] { p });
                    restoreMethod.Invoke(p, new object[] { this });
                }
                else if (pType == typeof(AnArray) || Attribute.GetCustomAttribute(pType, typeof(APICloneOnAssignmentAttribute)) != null)
                {
                    ConstructorInfo cInfo = pType.GetConstructor(new Type[] { pType });
                    p = cInfo.Invoke(new object[] { p });
                }
            }
            Type scriptType = GetType();

            MethodInfo mi = scriptType.GetMethod("rpcfn_" + name, paratypes.ToArray());

            if (mi != null)
            {
                if(mi.GetCustomAttribute(typeof(RpcLinksetExternalCallAllowedAttribute)) == null)
                {
                    if(!(Part?.ObjectGroup?.ContainsKey(RpcRemoteKey.AsUUID) ?? false))
                    {
                        /* ignore RPC from outside if not enabled */
                        return;
                    }
                }
                else if(mi.GetCustomAttribute(typeof(RpcLinksetExternalCallEveryoneAttribute)) == null)
                {
                    /* lockout foreign callers */
                    ObjectPart otherPart = null;
                    if (!(Part?.ObjectGroup?.Scene?.Primitives.TryGetValue(RpcRemoteKey.AsUUID, out otherPart) ?? false))
                    {
                        return;
                    }
                    if (mi.GetCustomAttribute(typeof(RpcLinksetExternalCallSameGroupAttribute)) != null &&
                        otherPart.Group.Equals(Part.Group))
                    {
                        /* same group is allowed */
                    }
                    else if (!otherPart.Owner.EqualsGrid(Part.Owner))
                    {
                        return;
                    }
                }
                IncrementScriptEventCounter();
                try
                {
                    foreach (object p in param)
                    {
                        if (p == null)
                        {
                            var sb = new StringBuilder("Call failure at rpc ");
                            sb.Append(name + "(");
                            bool first = true;
                            foreach (object pa in param)
                            {
                                if (!first)
                                {
                                    sb.Append(",");
                                }
                                first = false;
                                if (pa == null)
                                {
                                    sb.Append("null");
                                }
                                else if (pa is LSLKey || pa is string)
                                {
                                    sb.Append("\"" + pa + "\"");
                                }
                                else if (pa is char)
                                {
                                    sb.Append("\'" + pa + "\'");
                                }
                                else
                                {
                                    sb.Append(pa.ToString());
                                }
                            }
                            sb.Append(")");
                            m_Log.Debug(sb);
                            return;
                        }
                    }

                    mi.Invoke(this, realparams);
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
                    if (innerType == typeof(NotImplementedException))
                    {
                        ShoutUnimplementedException(e.InnerException as NotImplementedException);
                        return;
                    }
                    else if (innerType == typeof(DeprecatedFunctionCalledException))
                    {
                        ShoutDeprecatedException(e.InnerException as DeprecatedFunctionCalledException);
                        return;
                    }
                    else if (innerType == typeof(HitSandboxLimitException))
                    {
                        ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                    }
                    else if (innerType == typeof(ChangeStateException) ||
                        innerType == typeof(ResetScriptException) ||
                        innerType == typeof(LocalizedScriptErrorException) ||
                        innerType == typeof(DivideByZeroException))
                    {
                        throw e.InnerException;
                    }
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (NotImplementedException e)
                {
                    ShoutUnimplementedException(e);
                }
                catch (DeprecatedFunctionCalledException e)
                {
                    ShoutDeprecatedException(e);
                }
                catch (InvalidProgramException e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
                catch (HitSandboxLimitException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "HitSandboxLimit", "Hit Sandbox Limit"));
                }
                catch (CallDepthLimitViolationException e)
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
                catch (ArgumentException e)
                {
#if DEBUG
                    LogInvokeException(name, e);
#endif
                    ShoutError(e.Message);
                }
                catch (ScriptWorkerInterruptionException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogInvokeException(name, e);
                    ShoutError(e.Message);
                }
            }
        }

        private static string MapTypeToString(Type t)
        {
            if(typeof(string) == t)
            {
                return "string";
            }
            else if(typeof(int) == t)
            {
                return "integer";
            }
            else if(typeof(char) == t)
            {
                return "char";
            }
            else if(typeof(long) == t)
            {
                return "long";
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

        private void ShoutDeprecatedException(DeprecatedFunctionCalledException e)
        {
            MethodBase mb = e.TargetSite;
            var apiLevel = (APILevelAttribute)Attribute.GetCustomAttribute(mb, typeof(APILevelAttribute));
            if (apiLevel != null)
            {
                string methodName = mb.Name;
                if (!string.IsNullOrEmpty(apiLevel.Name))
                {
                    methodName = apiLevel.Name;
                }

                var funcSignature = new StringBuilder(methodName + "(");

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

                ShoutError(new LocalizedScriptMessage(apiLevel, "ScriptCalledDeprecatedFunction0", "Script called deprecated function {0}", funcSignature.ToString()));
            }
        }

        private void ShoutUnimplementedException(NotImplementedException e)
        {
            MethodBase mb = e.TargetSite;
            var apiLevel = (APILevelAttribute)Attribute.GetCustomAttribute(mb, typeof(APILevelAttribute));
            if (apiLevel != null)
            {
                string methodName = mb.Name;
                if (!string.IsNullOrEmpty(apiLevel.Name))
                {
                    methodName = apiLevel.Name;
                }

                var funcSignature = new StringBuilder(methodName + "(");

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

                ShoutError(new LocalizedScriptMessage(apiLevel, "ScriptCalledUnimplementedFunction0", "Script called unimplemented function {0}", funcSignature.ToString()));
            }
        }

        public override IScriptState ScriptState => this;

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
            long startticks = TimeSource.TickCount;
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
                    startticks = TimeSource.TickCount;
                }
                #endregion

                #region State Exit
                bool executedStateExit = executeStateExit;
                try
                {
                    if (executeStateExit)
                    {
                        executeStateExit = false;
                        startticks = TimeSource.TickCount;
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
                    ShoutError(new LocalizedScriptMessage(e, "ScriptErrorStateChangeUsedInStateExit", "Script error! state change used in state_exit"));
                    LogInvokeException("state_exit", e);
                }
                catch(LocalizedScriptErrorException e)
                {
                    ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                }
                catch(DivideByZeroException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                }
                finally
                {
                    if (executedStateExit)
                    {
                        lock (m_Lock)
                        {
                            m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                        }
                    }
                }
                if (executedStateExit)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        UpdateScriptState();
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
                        startticks = TimeSource.TickCount;
                        if (evgot != null && evgot.GetType() == typeof(ResetScriptEvent))
                        {
                            evgot = null;
                        }
                        lock (this)
                        {
                            /* lock(this) needed here to prevent aborting in wrong place */
                            Part.UpdateScriptFlags();
                        }

                        InvokeStateEvent("state_entry");
                    }
                }
                catch (ResetScriptException e)
                {
                    if (m_States["default"] == m_CurrentState)
                    {
                        ShoutError(new LocalizedScriptMessage(e.Message, "llResetScriptUsedInDefaultStateEntry", "Script error! llResetScript used in default.state_entry"));
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
                            UpdateScriptState();
                        }
                        continue;
                    }
                }
                catch (LocalizedScriptErrorException e)
                {
                    ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                }
                catch (DivideByZeroException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                }
                finally
                {
                    if (executedStateEntry)
                    {
                        lock (m_Lock)
                        {
                            m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                        }
                    }
                }

                if(executedStateEntry)
                {
                    lock (this) /* really needed to prevent aborting here */
                    {
                        UpdateScriptState();
                    }
                }
                #endregion

                #region Event Logic
                bool eventExecuted = false;
                try
                {
                    IScriptEvent ev = evgot;
                    evgot = null;
                    if (ev != null)
                    {
                        eventExecuted = true;
                        startticks = Environment.TickCount;
                        Type evt = ev.GetType();
                        Action<Script, IScriptEvent> evtDelegate;
                        if (StateEventHandlers.TryGetValue(evt, out evtDelegate))
                        {
                            var detectedEv = ev as IScriptDetectedEvent;
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
                catch (LocalizedScriptErrorException e)
                {
                    ShoutError(new LocalizedScriptMessage(e.NlsRefObject, e.NlsId, e.NlsDefMsg, e.NlsParams));
                }
                catch (DivideByZeroException)
                {
                    ShoutError(new LocalizedScriptMessage(this, "DivisionByZeroEncountered", "Division by zero encountered"));
                }
                finally
                {
                    if (eventExecuted)
                    {
                        lock (m_Lock)
                        {
                            m_ExecutionTime += TimeSource.TicksToSecs(TimeSource.TicksElapsed(TimeSource.TickCount, startticks));
                        }
                    }
                }
                #endregion

                lock (this) /* really needed to prevent aborting here */
                {
                    UpdateScriptState();
                }
            } while (executeStateEntry || executeStateExit || executeScriptReset);
        }

        public override bool HasEventsPending => m_Events.Count != 0;

        internal void OnListen(ListenEvent ev)
        {
            /* do not receive own messages */
            if (ev.ID != Part?.ID)
            {
                PostEvent(ev);
            }
        }

        private sealed class AtRegionMessageLocalization : IListenEventLocalization
        {
            private readonly IListenEventLocalization m_SubMessage;
            private readonly string m_RegionName;
            public string LinkName { get; set; }
            public int LinkNumber { get; set; }
            public string ScriptName { get; set; }
            public int LineNumber { get; set; }

            public AtRegionMessageLocalization(IListenEventLocalization subMessage, string regionName)
            {
                m_SubMessage = subMessage;
                m_RegionName = regionName;
                if (subMessage != null)
                {
                    LinkName = subMessage.LinkName;
                    LinkNumber = subMessage.LinkNumber;
                    ScriptName = subMessage.ScriptName;
                    LineNumber = subMessage.LineNumber;
                }
            }

            public string Localize(ListenEvent le, CultureInfo currentCulture)
            {
                if (!string.IsNullOrEmpty(ScriptName))
                {
                    return string.Format(this.GetLanguageString(currentCulture, "ShoutErrorAtRegion0LinkName1Num2Script3Line4",
                        "At region {0}:\nLink: {1} ({2})\nScript location: {3}:{4}:") + "\n", m_RegionName, LinkName, LinkNumber, ScriptName, LineNumber) +
                        (m_SubMessage != null ? m_SubMessage.Localize(le, currentCulture) : le.Message);
                }
                else
                {
                    return string.Format(this.GetLanguageString(currentCulture, "ShoutErrorAtRegion0", "At region {0}:") + "\n", m_RegionName) +
                        (m_SubMessage != null ? m_SubMessage.Localize(le, currentCulture) : le.Message);
                }
            }
        }

        public int LineNumber;

        public override void ShoutError(string message)
        {
            var ev = new ListenEvent
            {
                Channel = 0x7FFFFFFF, /* DEBUG_CHANNEL */
                Type = ListenEvent.ChatType.DebugChannel
            };
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
                ev.Localization = new AtRegionMessageLocalization(null, objGroup.Scene.Name)
                {
                    LinkName = part.Name,
                    LinkNumber = part.LinkNumber,
                    ScriptName = Item.Name,
                    LineNumber = LineNumber - 1
                };
                ev.Message = ev.Localization.Localize(ev, null);
                chatService = objGroup.Scene.GetService<ChatServiceInterface>();
            }
#if DEBUG
            m_Log.DebugFormat("Sending message to DEBUG_CHANNEL for {1} at {2}: {0}", ev.Message, ev.Name, ev.GlobalPosition.ToString());
#endif
            chatService?.Send(ev);
        }

        public override void ShoutError(IListenEventLocalization localizedMessage)
        {
            var ev = new ListenEvent
            {
                Channel = 0x7FFFFFFF, /* DEBUG_CHANNEL */
                Type = ListenEvent.ChatType.DebugChannel
            };
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
                ev.Localization = new AtRegionMessageLocalization(localizedMessage, objGroup.Scene.Name)
                {
                    LinkName = part.Name,
                    LinkNumber = part.LinkNumber,
                    ScriptName = Item.Name,
                    LineNumber = LineNumber - 1
                };
                ev.Message = ev.Localization.Localize(ev, null);
                chatService = objGroup.Scene.GetService<ChatServiceInterface>();
            }
#if DEBUG
            m_Log.DebugFormat("Sending localized message to DEBUG_CHANNEL for {1} at {2}: {0}", ev.Message, ev.Name, ev.GlobalPosition.ToString());
#endif
            chatService?.Send(ev);
        }


        #region Event to function handlers

        static Script()
        {
            #region Default state event handlers
            StateEventHandlers.Add(typeof(AtRotTargetEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AtRotTargetEvent)ev;
                script.InvokeStateEvent("at_rot_target", e.Handle, e.TargetRotation, e.OurRotation);
            });

            StateEventHandlers.Add(typeof(AttachEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AttachEvent)ev;
                script.InvokeStateEvent("attach", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(AtTargetEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (AtTargetEvent)ev;
                script.InvokeStateEvent("at_target", e.Handle, e.TargetPosition, e.OurPosition);
            });

            StateEventHandlers.Add(typeof(ChangedEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ChangedEvent)ev;
                script.InvokeStateEvent("changed", (int)e.Flags);
            });

            StateEventHandlers.Add(typeof(CollisionEvent), HandleCollision);

            StateEventHandlers.Add(typeof(DataserverEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (DataserverEvent)ev;
                script.InvokeStateEvent("dataserver", new LSLKey(e.QueryID), e.Data);
            });

            StateEventHandlers.Add(typeof(MessageObjectEvent), HandleMessageObject);

            StateEventHandlers.Add(typeof(EmailEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (EmailEvent)ev;
                script.InvokeStateEvent("email", e.Time, e.Address, e.Subject, e.Message, e.NumberLeft);
            });

            StateEventHandlers.Add(typeof(HttpResponseEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (HttpResponseEvent)ev;
                if (e.UsesByteArray)
                {
                    script.InvokeStateEvent("http_binary_response", new LSLKey(e.RequestID), e.Status, e.Metadata, new ByteArrayApi.ByteArray(e.Body));
                }
                else
                {
                    script.InvokeStateEvent("http_response", new LSLKey(e.RequestID), e.Status, e.Metadata, e.Body.FromUTF8Bytes());
                }
            });

            StateEventHandlers.Add(typeof(HttpRequestEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (HttpRequestEvent)ev;
                if (e.UsesByteArray)
                {
                    script.InvokeStateEvent("http_binary_request", new LSLKey(e.RequestID), e.Method, new ByteArrayApi.ByteArray(e.Body));
                }
                else
                {
                    script.InvokeStateEvent("http_request", new LSLKey(e.RequestID), e.Method, e.Body.FromUTF8Bytes());
                }
            });

            StateEventHandlers.Add(typeof(LandCollisionEvent), HandleLandCollision);

            StateEventHandlers.Add(typeof(LinkMessageEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (LinkMessageEvent)ev;
                script.InvokeStateEvent("link_message", e.SenderNumber, e.Number, e.Data, new LSLKey(e.Id));
            });

            StateEventHandlers.Add(typeof(ListenEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ListenEvent)ev;
                script.InvokeStateEvent("listen", e.Channel, e.Name, new LSLKey(e.ID), e.Message);
            });

            StateEventHandlers.Add(typeof(MoneyEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (MoneyEvent)ev;
                script.InvokeStateEvent("money", e.ID, e.Amount);
            });

            StateEventHandlers.Add(typeof(MovingStartEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("moving_start"));

            StateEventHandlers.Add(typeof(MovingEndEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("moving_end"));

            StateEventHandlers.Add(typeof(NoSensorEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("no_sensor"));

            StateEventHandlers.Add(typeof(NotAtRotTargetEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("not_at_rot_target"));

            StateEventHandlers.Add(typeof(NotAtTargetEvent), (Script script, IScriptEvent ev) => script.InvokeStateEvent("not_at_target"));

            StateEventHandlers.Add(typeof(ObjectRezEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (ObjectRezEvent)ev;
                script.InvokeStateEvent("object_rez", new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(OnRezEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (OnRezEvent)ev;
                script.StartParameter = new Integer(e.StartParam);
                script.InvokeStateEvent("on_rez", e.StartParam);
            });

            StateEventHandlers.Add(typeof(PathUpdateEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (PathUpdateEvent)ev;
                script.InvokeStateEvent("path_update", e.Type, e.Reserved);
            });

            StateEventHandlers.Add(typeof(RemoteDataEvent), (Script script, IScriptEvent ev) =>
            {
                var e = (RemoteDataEvent)ev;
                script.InvokeStateEvent("remote_data", e.Type, new LSLKey(e.Channel), new LSLKey(e.MessageID), e.Sender, e.IData, e.SData);
            });

            StateEventHandlers.Add(typeof(ResetScriptEvent), (Script script, IScriptEvent ev) =>
#pragma warning disable RCS1021 // Simplify lambda expression.
            {
                throw new ResetScriptException();
            });
#pragma warning restore RCS1021 // Simplify lambda expression.

            StateEventHandlers.Add(typeof(ItemSoldEvent), (script, ev) =>
            {
                var e = (ItemSoldEvent)ev;
                UGUIWithName agent = script.Part?.ObjectGroup?.Scene?.AvatarNameService?.ResolveName(e.Agent) ?? (UGUIWithName)e.Agent;
                script.InvokeStateEvent("item_sold", agent.FullName, new LSLKey(e.Agent.ID), e.ObjectName, new LSLKey(e.ObjectID));
            });

            StateEventHandlers.Add(typeof(SensorEvent), (script, ev) => script.InvokeStateEvent("sensor", script.m_Detected.Count));
            StateEventHandlers.Add(typeof(RuntimePermissionsEvent), HandleRuntimePermissions);
            StateEventHandlers.Add(typeof(ExperiencePermissionsEvent), HandleExperiencePermissions);
            StateEventHandlers.Add(typeof(ExperiencePermissionsDeniedEvent), HandleExperiencePermissionsDenied);
            StateEventHandlers.Add(typeof(TouchEvent), HandleTouch);
            StateEventHandlers.Add(typeof(TimerEvent), (script, ev) => script.InvokeStateEvent("timer"));
            StateEventHandlers.Add(typeof(RpcScriptEvent), HandleRpcScriptEvent);
            StateEventHandlers.Add(typeof(ControlEvent), (script, ev) =>
            {
                var e = (ControlEvent)ev;
                script.InvokeStateEvent("control", new LSLKey(e.AgentID), e.Level, e.Flags);
            });
            #endregion
        }

        private static void HandleCollision(Script script, IScriptEvent ev)
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
            }
        }

        private static void HandleMessageObject(Script script, IScriptEvent ev)
        {
            var e = (MessageObjectEvent)ev;
            if (script.UseMessageObjectEvent)
            {
                script.InvokeStateEvent("object_message", new LSLKey(e.ObjectID), e.Data);
            }
            else
            {
                script.InvokeStateEvent("dataserver", new LSLKey(e.ObjectID), e.Data);
            }
        }

        private static void HandleLandCollision(Script script, IScriptEvent ev)
        {
            var e = (LandCollisionEvent)ev;
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
            }
        }

        private static void HandleRuntimePermissions(Script script, IScriptEvent ev)
        {
            var e = (RuntimePermissionsEvent)ev;
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

            script.Item.PermsGranter = new ObjectPartInventoryItem.PermsGranterInfo
            {
                PermsGranter = e.PermissionsKey,
                PermsMask = e.Permissions
            };
            script.InvokeStateEvent("run_time_permissions", (int)e.Permissions);
        }

        private static void HandleExperiencePermissions(Script script, IScriptEvent ev)
        {
            var e = (ExperiencePermissionsEvent)ev;

            script.Item.PermsGranter = new ObjectPartInventoryItem.PermsGranterInfo
            {
                PermsGranter = e.PermissionsKey,
                PermsMask = ScriptPermissions.TakeControls | ScriptPermissions.TriggerAnimation | ScriptPermissions.Attach | ScriptPermissions.TrackCamera | ScriptPermissions.ControlCamera | ScriptPermissions.Teleport
            };
            script.InvokeStateEvent("experience_permissions", new LSLKey(e.PermissionsKey.ID));
        }

        private static void HandleExperiencePermissionsDenied(Script script, IScriptEvent ev)
        {
            var e = (ExperiencePermissionsDeniedEvent)ev;
            script.InvokeStateEvent("experience_permissions_denied", new LSLKey(e.AgentId.ID), e.Reason);
        }

        public string RpcRemoteScriptName { get; private set; }
        public int RpcRemoteLinkNumber { get; private set; }
        public LSLKey RpcRemoteKey { get; private set; }
        public LSLKey RpcRemoteScriptKey { get; private set; }

        private static void HandleRpcScriptEvent(Script script, IScriptEvent ev)
        {
            var e = (RpcScriptEvent)ev;
            script.RpcRemoteKey = e.SenderKey;
            script.RpcRemoteLinkNumber = e.SenderLinkNumber;
            script.RpcRemoteScriptName = e.SenderScriptName;
            script.RpcRemoteScriptKey = e.SenderScriptKey;
            script.InvokeRpcEventReal(e.FunctionName, e.Parameters);
            script.RpcRemoteKey = UUID.Zero;
            script.RpcRemoteLinkNumber = -1;
            script.RpcRemoteScriptName = string.Empty;
            script.RpcRemoteScriptKey = UUID.Zero;
        }

        private static void HandleTouch(Script script, IScriptEvent ev)
        {
            var e = (TouchEvent)ev;
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
            }
        }
        #endregion

        #region Threat Level System

        public class Permissions
        {
            public readonly RwLockedList<UGUI> Creators = new RwLockedList<UGUI>();
            public readonly RwLockedList<UGUI> Owners = new RwLockedList<UGUI>();
            public bool IsAllowedForParcelOwner;
            public bool IsAllowedForParcelGroupMember;
            public bool IsAllowedForEstateOwner;
            public bool IsAllowedForEstateManager;
            public bool IsAllowedForRegionOwner;
            public bool IsAllowedForEveryone;
        }

        public const ThreatLevel DefaultThreatLevel = ThreatLevel.Low;

        public static readonly RwLockedDictionaryAutoAdd<UUID, ThreatLevel> ThreatLevels = new RwLockedDictionaryAutoAdd<UUID, ThreatLevel>(() => DefaultThreatLevel);

        public static readonly RwLockedDictionaryAutoAdd<string,
            RwLockedDictionaryAutoAdd<UUID, Permissions>> OSSLPermissions = new RwLockedDictionaryAutoAdd<string, RwLockedDictionaryAutoAdd<UUID, Permissions>>(() =>
            {
                return new RwLockedDictionaryAutoAdd<UUID, Permissions>(
                    () => new Permissions());
            });

        private bool TryOSSLAllowed(
            SceneInterface scene,
            ParcelInfo pInfo,
            ObjectGroup objgroup,
            UGUI creator,
            UGUI owner,
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

            if (pInfo != null && (perms.IsAllowedForParcelOwner || perms.IsAllowedForParcelGroupMember))
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

        private void InvokeRpcCall(ObjectPart part, string scriptname, RpcScriptEvent ev)
        {
            ObjectPartInventoryItem item;
            if(string.IsNullOrEmpty(scriptname))
            {
                part.PostEvent(ev);
            }
            else if(part.Inventory.TryGetValue(scriptname, out item))
            {
                item.ScriptInstance?.PostEvent(ev);
            }
        }

        protected void InvokeRpcCall(LSLKey key, string scriptname, RpcScriptEvent ev)
        {
            ObjectPart thisPart = Part;
            ObjectGroup thisGroup = thisPart?.ObjectGroup;
            SceneInterface scene = thisGroup?.Scene;
            if (scene == null)
            {
                return;
            }

            ev.SenderLinkNumber = thisPart.LinkNumber;
            ev.SenderKey = thisPart.ID;
            ev.SenderScriptName = Item.Name;

            ObjectPart part;
            if (thisGroup.TryGetValue(key.AsUUID, out part))
            {
                ev.SenderScriptKey = Item.AssetID; /* same linkset, propagate script key */
            }
            else if(scene.Primitives.TryGetValue(key.AsUUID, out part))
            {
                ev.SenderScriptKey = UUID.Zero; /* no outside comms of script key */
            }
            else
            {
                return;
            }
            InvokeRpcCall(part, scriptname, ev);
        }

        protected void InvokeRpcCall(string linkname, string scriptname, RpcScriptEvent ev)
        {
            ObjectPart thisPart = Part;
            ObjectGroup objgroup = thisPart?.ObjectGroup;
            if (objgroup == null)
            {
                return;
            }

            ev.SenderLinkNumber = thisPart.LinkNumber;
            ev.SenderKey = thisPart.ID;
            ev.SenderScriptName = Item.Name;
            ev.SenderScriptKey = Item.AssetID;

            foreach (ObjectPart part in objgroup.Values)
            {
                if(part.Name == linkname)
                {
                    InvokeRpcCall(part, scriptname, ev);
                }
            }
        }

        protected void InvokeRpcCall(int linknumber, string scriptname, RpcScriptEvent ev)
        {
            ObjectPart thisPart = Part;
            ObjectGroup objgroup = thisPart?.ObjectGroup;
            if(objgroup == null)
            {
                return;
            }

            ev.SenderLinkNumber = thisPart.LinkNumber;
            ev.SenderKey = thisPart.ID;
            ev.SenderScriptName = Item.Name;
            ev.SenderScriptKey = Item.AssetID;

            switch(linknumber)
            {
                case PrimitiveApi.LINK_SET:
                    foreach (ObjectPart part in objgroup.Values)
                    {
                        InvokeRpcCall(part, scriptname, ev);
                    }
                    break;

                case PrimitiveApi.LINK_ALL_OTHERS:
                    foreach (ObjectPart part in objgroup.Values)
                    {
                        if (part != thisPart)
                        {
                            InvokeRpcCall(part, scriptname, ev);
                        }
                    }
                    break;

                case PrimitiveApi.LINK_ALL_CHILDREN:
                    ObjectPart rootPart = objgroup.RootPart;
                    foreach (ObjectPart part in objgroup.Values)
                    {
                        if (part != rootPart)
                        {
                            InvokeRpcCall(part, scriptname, ev);
                        }
                    }
                    break;

                case PrimitiveApi.LINK_THIS:
                    InvokeRpcCall(Part, scriptname, ev);
                    break;

                default:
                    {
                        if (linknumber == PrimitiveApi.LINK_UNLINKED_ROOT)
                        {
                            linknumber = PrimitiveApi.LINK_ROOT;
                        }
                        ObjectPart part;
                        if (objgroup.TryGetValue(linknumber, out part))
                        {
                            InvokeRpcCall(part, scriptname, ev);
                        }
                    }
                    break;
            }
        }

        public void CheckThreatLevel(string name, ThreatLevel level)
        {
            Permissions perms;
            ObjectPart part = Part;
            ObjectGroup objgroup = part.ObjectGroup;
            SceneInterface scene = objgroup.Scene;
            ObjectPart rootPart = objgroup.RootPart;
            UGUI creator = rootPart.Creator;
            UGUI owner = objgroup.Owner;
            ParcelInfo pInfo;

            ThreatLevel threatLevel;

            if ((ThreatLevels.TryGetValue(scene.ID, out threatLevel) || ThreatLevels.TryGetValue(UUID.Zero, out threatLevel)) &&
                (int)threatLevel >= (int)level)
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

            throw new LocalizedScriptErrorException(this, "Function0NotAllowed", "Function {0} not allowed", name);
        }
        #endregion
    }
}
