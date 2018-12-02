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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;

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
        public override int StartParameter { get; set; }
        internal RwLockedDictionary<int, ChatServiceInterface.Listener> m_Listeners = new RwLockedDictionary<int, ChatServiceInterface.Listener>();
        private double m_ExecutionTime;
        protected bool UseMessageObjectEvent;
        protected bool InheritEventsOnStateChange;
        protected bool m_AllowExperienceDataServerEventsToAllScripts;
        internal RwLockedList<UUID> m_RequestedURLs = new RwLockedList<UUID>();

        public bool AllowExperienceDataServerEventsToAllScripts => m_AllowExperienceDataServerEventsToAllScripts;
        public bool UseForcedSleep = true;
        public double ForcedSleepFactor = 1;
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
        public override bool HasCollisionEvent => m_HasCollisionEvent || m_HasCollisionStartEvent || m_HasCollisionEndEvent;
        public override bool HasLandCollisionEvent => m_HasLandCollisionEvent || m_HasLandCollisionStartEvent || m_HasLandCollisionEndEvent;

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

        private ScriptStates.ScriptState m_TransactionedState = new ScriptStates.ScriptState();

        protected void UpdateScriptState()
        {
            var state = new ScriptStates.ScriptState
            {
                EventData = m_Events.ToArray(),
                AssetID = Item.AssetID,
                ItemID = Item.ID,
                MinEventDelay = MinEventDelay,
                CurrentState = "default",
                UseMessageObjectEvent = UseMessageObjectEvent, /* make script state know about that */
                PermsGranter = Item.PermsGranter,
                IsRunning = IsRunning
            };
            foreach (KeyValuePair<string, ILSLState> kvp in m_States)
            {
                if (kvp.Value == m_CurrentState)
                {
                    state.CurrentState = kvp.Key;
                }
            }
            FieldInfo[] fieldInfos = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (!fi.Name.StartsWith("var_") || fi.IsInitOnly)
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

            if (SerializationDelegates != null)
            {
                foreach (Action<ScriptInstance, List<object>> serializer in SerializationDelegates)
                {
                    serializer(this, state.PluginData);
                }
            }
            m_TransactionedState = state;
        }

        public void ToXml(XmlTextWriter writer, bool primaryFormatOnly = false)
        {
            m_TransactionedState.ToXml(writer, primaryFormatOnly);
        }

        public byte[] ToDbSerializedState() => m_TransactionedState.ToDbSerializedState();

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

        public override bool IsRunning
        {
            get
            {
                return m_TransactionedState.IsRunning;
            }

            set
            {
                m_TransactionedState.IsRunning = value;
            }
        }

        public override void Remove()
        {
            Timer.Stop();
            Timer.Elapsed -= OnTimerEvent;
            StopAllNamedTimers();
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
                    try
                    {
                        del(this);
                    }
                    catch(Exception e)
                    {
                        m_Log.Debug("Exception at Remove()", e);
                    }
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

        bool m_IsLinkMessageReceiver;
        public override bool IsLinkMessageReceiver => m_IsLinkMessageReceiver;

        private int m_RecursionCount;
        private static int m_CallDepthLimit = 200;
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

        public override IScriptState ScriptState => this;

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

        public void CheckThreatLevel(string name)
        {
            Permissions perms;
            ObjectPart part = Part;
            ObjectGroup objgroup = part.ObjectGroup;
            SceneInterface scene = objgroup.Scene;
            ObjectPart rootPart = objgroup.RootPart;
            UGUI creator = rootPart.Creator;
            UGUI owner = objgroup.Owner;
            ParcelInfo pInfo;

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
