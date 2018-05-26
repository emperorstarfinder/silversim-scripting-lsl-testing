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

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Event;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.ScriptStates
{
    public partial class ScriptState
    {
        private static readonly Dictionary<Type, Func<IScriptEvent, EventParams>> EventSerializers = new Dictionary<Type, Func<IScriptEvent, EventParams>>();
        private static readonly Dictionary<string, Func<EventParams, IScriptEvent>> EventDeserializers = new Dictionary<string, Func<EventParams, IScriptEvent>>();

        public static bool TryTranslateEventParams(EventParams ep, out IScriptEvent res)
        {
            res = null;
            Func<EventParams, IScriptEvent> deserializer;
            if (EventDeserializers.TryGetValue(ep.EventName, out deserializer))
            {
                res = deserializer(ep);
            }
            return res != null;
        }

        public static bool TryTranslateEvent(IScriptEvent ev, out EventParams res)
        {
            res = null;
            Func<IScriptEvent, EventParams> serializer;
            if(EventSerializers.TryGetValue(ev.GetType(), out serializer))
            {
                res = serializer(ev);
            }
            return res != null;
        }

        static ScriptState()
        {
            EventDeserializers.Add("rpc", RpcDeserializer);
            EventSerializers.Add(typeof(RpcScriptEvent), RpcSerializer);
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
            EventDeserializers.Add("http_binary_response", HttpBinaryResponseDeserializer);
            EventSerializers.Add(typeof(HttpResponseEvent), HttpResponseSerializer);
            EventDeserializers.Add("listen", ListenDeserializer);
            EventSerializers.Add(typeof(ListenEvent), ListenSerializer);
            EventDeserializers.Add("sensor", (ep) => new SensorEvent { Detected = ep.Detected });
            EventSerializers.Add(typeof(SensorEvent), (iev) => DetectedSerializer(((SensorEvent)iev).Detected, "sensor"));
            EventDeserializers.Add("on_rez", OnRezDeserializer);
            EventSerializers.Add(typeof(OnRezEvent), (ev) => new EventParams { EventName = "on_rez" });
            EventDeserializers.Add("attach", AttachDeserializer);
            EventSerializers.Add(typeof(AttachEvent), AttachSerializer);
            EventDeserializers.Add("changed", ChangedDeserializer);
            EventSerializers.Add(typeof(ChangedEvent), ChangedSerializer);
            EventDeserializers.Add("money", MoneyDeserializer);
            EventSerializers.Add(typeof(MoneyEvent), MoneySerializer);
            EventDeserializers.Add("no_sensor", (ep) => new NoSensorEvent());
            EventSerializers.Add(typeof(NoSensorEvent), (ev) => new EventParams { EventName = "no_sensor" });
            EventDeserializers.Add("timer", (ep) => new TimerEvent());
            EventSerializers.Add(typeof(TimerEvent), (ev) => new EventParams { EventName = "timer" });
            EventDeserializers.Add("touch_start", (ep) => new TouchEvent { Type = TouchEvent.TouchType.Start, Detected = ep.Detected });
            EventSerializers.Add(typeof(TouchEvent), TouchSerializer);
            EventDeserializers.Add("touch", (ep) => new TouchEvent { Type = TouchEvent.TouchType.Continuous, Detected = ep.Detected });
            EventDeserializers.Add("touch_end", (ep) => new TouchEvent { Type = TouchEvent.TouchType.End, Detected = ep.Detected });
            EventDeserializers.Add("collision_start", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.Start, Detected = ep.Detected });
            EventSerializers.Add(typeof(CollisionEvent), CollisionSerializer);
            EventDeserializers.Add("collision", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.Continuous, Detected = ep.Detected });
            EventDeserializers.Add("collision_end", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.End, Detected = ep.Detected });
            EventDeserializers.Add("not_at_target", (ep) => new NotAtTargetEvent());
            EventSerializers.Add(typeof(NotAtTargetEvent), (ev) => new EventParams { EventName = "not_at_target" });
            EventDeserializers.Add("not_at_rot_target", (ep) => new NotAtRotTargetEvent());
            EventSerializers.Add(typeof(NotAtRotTargetEvent), (ev) => new EventParams { EventName = "not_at_rot_target" });
            EventDeserializers.Add("moving_start", (ep) => new MovingStartEvent());
            EventSerializers.Add(typeof(MovingStartEvent), (ev) => new EventParams { EventName = "moving_start" });
            EventDeserializers.Add("moving_end", (ep) => new MovingEndEvent());
            EventSerializers.Add(typeof(MovingEndEvent), (ev) => new EventParams { EventName = "moving_end" });
        }

        #region RPC

        private static EventParams RpcSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "rpc" };
            var ev = (RpcScriptEvent)iev;
            ep.Params.Add(ev.FunctionName);
            ep.Params.Add(ev.SenderKey);
            ep.Params.Add(ev.SenderLinkNumber);
            ep.Params.Add(ev.SenderScriptName);
            ep.Params.Add(ev.SenderScriptKey);
            ep.Params.AddRange(ev.Parameters);
            return ep;
        }

        private static IScriptEvent RpcDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 5)
            {
                var param = new List<object>();
                for (int i = 5; i < ep.Params.Count; ++i)
                {
                    param.Add(ep.Params[i]);
                }
                return new RpcScriptEvent
                {
                    FunctionName = ep.Params[0].ToString(),
                    SenderKey = (UUID)ep.Params[1],
                    SenderLinkNumber = (int)ep.Params[2],
                    SenderScriptName = ep.Params[3].ToString(),
                    SenderScriptKey = (UUID)ep.Params[4],
                    Parameters = param.ToArray()
                };
            }
            return null;
        }

        #endregion

        #region Detected

        private static EventParams DetectedSerializer(List<DetectInfo> di, string name)
        {
            var ep = new EventParams { EventName = name };
            ep.Detected.AddRange(di);
            return ep;
        }
        #endregion

        #region Collision

        private static EventParams CollisionSerializer(IScriptEvent iev)
        {
            var ev = (CollisionEvent)iev;
            switch (ev.Type)
            {
                case CollisionEvent.CollisionType.Start:
                    return DetectedSerializer(ev.Detected, "collision_start");
                case CollisionEvent.CollisionType.Continuous:
                    return DetectedSerializer(ev.Detected, "collision");
                case CollisionEvent.CollisionType.End:
                    return DetectedSerializer(ev.Detected, "collision_end");
            }
            return null;
        }
        #endregion

        #region Touch

        private static EventParams TouchSerializer(IScriptEvent iev)
        {
            var ev = (TouchEvent)iev;
            switch (ev.Type)
            {
                case TouchEvent.TouchType.Start:
                    return DetectedSerializer(ev.Detected, "touch_start");
                case TouchEvent.TouchType.Continuous:
                    return DetectedSerializer(ev.Detected, "touch");
                case TouchEvent.TouchType.End:
                    return DetectedSerializer(ev.Detected, "touch_end");
            }
            return null;
        }
        #endregion

        #region AtRotTarget
        private static IScriptEvent AtRotTargetDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                return new AtRotTargetEvent
                {
                    Handle = (int)ep.Params[0],
                    TargetRotation = (Quaternion)ep.Params[1],
                    OurRotation = (Quaternion)ep.Params[2]
                };
            }
            return null;
        }

        private static EventParams AtRotTargetSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "at_rot_target" };
            var ev = (AtRotTargetEvent)iev;
            ep.Params.Add(ev.Handle);
            ep.Params.Add(ev.TargetRotation);
            ep.Params.Add(ev.OurRotation);
            return ep;
        }
        #endregion

        #region AtTarget
        private static IScriptEvent AtTargetDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                return new AtTargetEvent
                {
                    Handle = (int)ep.Params[0],
                    TargetPosition = (Vector3)ep.Params[1],
                    OurPosition = (Vector3)ep.Params[2]
                };
            }
            return null;
        }

        private static EventParams AtTargetSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "at_target" };
            var ev = (AtTargetEvent)iev;
            ep.Params.Add(ev.Handle);
            ep.Params.Add(ev.TargetPosition);
            ep.Params.Add(ev.OurPosition);
            return ep;
        }
        #endregion

        #region control
        private static IScriptEvent ControlDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                return new ControlEvent
                {
                    AgentID = new UUID(ep.Params[0].ToString()),
                    Level = (ControlFlags)ep.Params[1],
                    Edge = (ControlFlags)ep.Params[2]
                };
            }
            return null;
        }

        private static EventParams ControlSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "control" };
            var ev = (ControlEvent)iev;
            ep.Params.Add(ev.AgentID);
            ep.Params.Add((int)ev.Level);
            ep.Params.Add((int)ev.Edge);
            return ep;
        }

        #endregion

        #region land_collision

        private static IScriptEvent LandCollisionStartDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new LandCollisionEvent
                {
                    Type = LandCollisionEvent.CollisionType.Start,
                    Position = (Vector3)ep.Params[0]
                };
            }
            return null;
        }

        private static EventParams LandCollisionSerializer(IScriptEvent iev)
        {
            var ev = (LandCollisionEvent)iev;
            EventParams ep = null;
            switch (ev.Type)
            {
                case LandCollisionEvent.CollisionType.Start:
                    ep = new EventParams { EventName = "land_collision_start" };
                    ep.Params.Add(ev.Position);
                    break;
                case LandCollisionEvent.CollisionType.Continuous:
                    ep = new EventParams { EventName = "land_collision" };
                    ep.Params.Add(ev.Position);
                    break;
                case LandCollisionEvent.CollisionType.End:
                    ep = new EventParams { EventName = "land_collision_end" };
                    ep.Params.Add(ev.Position);
                    break;
                default:
                    break;
            }
            return ep;
        }

        private static IScriptEvent LandCollisionDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new LandCollisionEvent
                {
                    Type = LandCollisionEvent.CollisionType.Continuous,
                    Position = (Vector3)ep.Params[0]
                };
            }
            return null;
        }

        private static IScriptEvent LandCollisionEndDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new LandCollisionEvent
                {
                    Type = LandCollisionEvent.CollisionType.End,
                    Position = (Vector3)ep.Params[0]
                };
            }
            return null;
        }
        #endregion

        #region money
        private static IScriptEvent MoneyDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                return new MoneyEvent
                {
                    ID = new UUID(ep.Params[0].ToString()),
                    Amount = (int)ep.Params[1]
                };
            }
            return null;
        }

        private static EventParams MoneySerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "money" };
            var ev = (MoneyEvent)iev;
            ep.Params.Add(ev.ID);
            ep.Params.Add(ev.Amount);
            return ep;
        }
        #endregion

        #region changed
        private static IScriptEvent ChangedDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new ChangedEvent
                {
                    Flags = (ChangedEvent.ChangedFlags)(int)ep.Params[0]
                };
            }
            return null;
        }

        private static EventParams ChangedSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "changed" };
            var ev = (ChangedEvent)iev;
            ep.Params.Add((int)ev.Flags);
            return ep;
        }
        #endregion

        #region attach
        private static IScriptEvent AttachDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new AttachEvent
                {
                    ObjectID = new UUID(ep.Params[0].ToString())
                };
            }
            return null;
        }

        private static EventParams AttachSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "attach" };
            var ev = (AttachEvent)iev;
            ep.Params.Add(ev.ObjectID);
            return ep;
        }
        #endregion

        #region on_rez
        private static IScriptEvent OnRezDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new OnRezEvent
                {
                    StartParam = (int)ep.Params[0]
                };
            }
            return null;
        }
        #endregion

        #region listen

        private static IScriptEvent ListenDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                return new ListenEvent
                {
                    Channel = (int)ep.Params[0],
                    Name = ep.Params[1].ToString(),
                    ID = new UUID(ep.Params[2].ToString()),
                    Message = ep.Params[3].ToString()
                };
            }
            return null;
        }

        private static EventParams ListenSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "listen" };
            var ev = (ListenEvent)iev;
            ep.Params.Add(ev.Channel);
            ep.Params.Add(ev.Name);
            ep.Params.Add(ev.ID);
            ep.Params.Add(ev.Message);
            return ep;
        }
        #endregion

        #region http_response

        private static IScriptEvent HttpResponseDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                return new HttpResponseEvent
                {
                    RequestID = new UUID(ep.Params[0].ToString()),
                    Status = (int)ep.Params[1],
                    Metadata = (AnArray)ep.Params[2],
                    Body = ep.Params[3].ToString().ToUTF8Bytes()
                };
            }
            return null;
        }

        private static IScriptEvent HttpBinaryResponseDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                return new HttpResponseEvent
                {
                    RequestID = new UUID(ep.Params[0].ToString()),
                    Status = (int)ep.Params[1],
                    Metadata = (AnArray)ep.Params[2],
                    UsesByteArray = true,
                    Body = Convert.FromBase64String(ep.Params[3].ToString())
                };
            }
            return null;
        }

        private static EventParams HttpResponseSerializer(IScriptEvent iev)
        {
            var ev = (HttpResponseEvent)iev;
            var ep = new EventParams { EventName = ev.UsesByteArray ? "http_binary_response" : "http_response" };
            ep.Params.Add(ev.RequestID);
            ep.Params.Add(ev.Status);
            ep.Params.Add(ev.Metadata);
            if(ev.UsesByteArray)
            {
                ep.Params.Add(Convert.ToBase64String(ev.Body));
            }
            else
            {
                ep.Params.Add(ev.Body.FromUTF8Bytes());
            }
            return ep;
        }
        #endregion

        #region dataserver

        private static IScriptEvent DataserverDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                return new DataserverEvent
                {
                    QueryID = new UUID(ep.Params[0].ToString()),
                    Data = ep.Params[1].ToString()
                };
            }
            return null;
        }

        private static EventParams DataserverSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "dataserver" };
            var ev = (DataserverEvent)iev;
            ep.Params.Add(ev.QueryID);
            ep.Params.Add(ev.Data);
            return ep;
        }
        #endregion

        #region object_message

        private static IScriptEvent ObjectMessageDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 2)
            {
                return new MessageObjectEvent
                {
                    ObjectID = new UUID(ep.Params[0].ToString()),
                    Data = ep.Params[1].ToString()
                };
            }
            return null;
        }

        internal static EventParams ObjectMessageSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "object_message" };
            var ev = (MessageObjectEvent)iev;
            ep.Params.Add(ev.ObjectID);
            ep.Params.Add(ev.Data);
            return ep;
        }

        internal static EventParams ObjectMessageDataserverSerializer(IScriptEvent iev)
        {
            EventParams ep = ObjectMessageSerializer(iev);
            ep.EventName = "dataserver";
            return ep;
        }
        #endregion

        #region transaction

        private static IScriptEvent TransactionResultDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                return new TransactionResultEvent
                {
                    TransactionID = ep.Params[0].ToString(),
                    Success = (int)ep.Params[1] != 0,
                    ReplyData = ep.Params[2].ToString()
                };
            }
            return null;
        }

        private static EventParams TransactionResultSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "transaction_result" };
            var ev = (TransactionResultEvent)iev;
            ep.Params.Add(ev.TransactionID);
            ep.Params.Add(ev.Success);
            ep.Params.Add(ev.ReplyData);
            return ep;
        }
        #endregion

        #region remote_data

        private static IScriptEvent RemoteDataDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 6)
            {
                return new RemoteDataEvent
                {
                    Type = (int)ep.Params[0],
                    Channel = new UUID(ep.Params[1].ToString()),
                    MessageID = new UUID(ep.Params[2].ToString()),
                    Sender = ep.Params[3].ToString(),
                    IData = (int)ep.Params[4],
                    SData = ep.Params[5].ToString()
                };
            }
            return null;
        }

        private static EventParams RemoteDataSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "remote_data" };
            var ev = (RemoteDataEvent)iev;
            ep.Params.Add(ev.Type);
            ep.Params.Add(ev.Channel);
            ep.Params.Add(ev.MessageID);
            ep.Params.Add(ev.Sender);
            ep.Params.Add(ev.IData);
            ep.Params.Add(ev.SData);
            return ep;
        }
        #endregion

        #region link_message
        private static IScriptEvent LinkMessageDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                return new LinkMessageEvent
                {
                    SenderNumber = (int)ep.Params[0],
                    Number = (int)ep.Params[1],
                    Data = ep.Params[2].ToString(),
                    Id = ep.Params[3].ToString()
                };
            }
            return null;
        }

        private static EventParams LinkMessageSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "link_message" };
            var ev = (LinkMessageEvent)iev;
            ep.Params.Add(ev.SenderNumber);
            ep.Params.Add(ev.Number);
            ep.Params.Add(ev.Data);
            ep.Params.Add(ev.Id);
            return ep;
        }
        #endregion

        #region runtime_permissions

        private static IScriptEvent RuntimePermissionsDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                var ev = new RuntimePermissionsEvent
                {
                    Permissions = (ScriptPermissions)(int)ep.Params[0]
                };
                if (ep.Params.Count > 1)
                {
                    ev.PermissionsKey = new UGUI(ep.Params[1].ToString());
                }
                return ev;
            }
            return null;
        }

        private static EventParams RuntimePermissionsSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "run_time_permissions" };
            var ev = (RuntimePermissionsEvent)iev;
            ep.Params.Add((int)ev.Permissions);
            ep.Params.Add(ev.PermissionsKey.ID);
            return ep;
        }
        #endregion

        #region email
        private static IScriptEvent EmailDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 5)
            {
                return new EmailEvent
                {
                    Time = ep.Params[0].ToString(),
                    Address = ep.Params[1].ToString(),
                    Subject = ep.Params[2].ToString(),
                    Message = ep.Params[3].ToString(),
                    NumberLeft = (int)ep.Params[4]
                };
            }
            return null;
        }

        private static EventParams EmailSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "email" };
            var ev = (EmailEvent)iev;
            ep.Params.Add(ev.Time);
            ep.Params.Add(ev.Address);
            ep.Params.Add(ev.Subject);
            ep.Params.Add(ev.Message);
            ep.Params.Add(ev.NumberLeft);
            return ep;
        }
        #endregion

        #region object_rez
        private static IScriptEvent ObjectRezDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 1)
            {
                return new ObjectRezEvent
                {
                    ObjectID = new UUID(ep.Params[0].ToString())
                };
            }
            return null;
        }

        private static EventParams ObjectRezSerializer(IScriptEvent iev)
        {
            var ep = new EventParams { EventName = "object_rez" };
            var ev = (ObjectRezEvent)iev;
            ep.Params.Add(ev.ObjectID);
            return ep;
        }
        #endregion
    }
}
