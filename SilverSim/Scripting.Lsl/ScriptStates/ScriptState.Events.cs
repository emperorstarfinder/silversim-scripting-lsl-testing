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
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SilverSim.Scripting.Lsl.ScriptStates
{
    public partial class ScriptState
    {
        private static readonly Dictionary<Type, Action<IScriptEvent, XmlTextWriter>> EventSerializers = new Dictionary<Type, Action<IScriptEvent, XmlTextWriter>>();
        private static readonly Dictionary<string, Func<EventParams, IScriptEvent>> EventDeserializers = new Dictionary<string, Func<EventParams, IScriptEvent>>();

        private static bool TryTranslateEventParams(EventParams ep, out IScriptEvent res)
        {
            res = null;
            Func<EventParams, IScriptEvent> deserializer;
            if (EventDeserializers.TryGetValue(ep.EventName, out deserializer))
            {
                res = deserializer(ep);
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
            EventSerializers.Add(typeof(HttpResponseEvent), HttpResponseSerializer);
            EventDeserializers.Add("listen", ListenDeserializer);
            EventSerializers.Add(typeof(ListenEvent), ListenSerializer);
            EventDeserializers.Add("sensor", (ep) => new SensorEvent { Detected = ep.Detected });
            EventSerializers.Add(typeof(SensorEvent), (iev, writer) =>
            {
                var ev = (SensorEvent)iev;
                DetectedSerializer(ev.Detected, "sensor", writer);
            });
            EventDeserializers.Add("on_rez", OnRezDeserializer);
            EventSerializers.Add(typeof(OnRezEvent), (ev, writer) => NoParamSerializer("on_rez", writer));
            EventDeserializers.Add("attach", AttachDeserializer);
            EventSerializers.Add(typeof(AttachEvent), AttachSerializer);
            EventDeserializers.Add("changed", ChangedDeserializer);
            EventSerializers.Add(typeof(ChangedEvent), ChangedSerializer);
            EventDeserializers.Add("money", MoneyDeserializer);
            EventSerializers.Add(typeof(MoneyEvent), MoneySerializer);
            EventDeserializers.Add("no_sensor", (ep) => new NoSensorEvent());
            EventSerializers.Add(typeof(NoSensorEvent), (ev, writer) => NoParamSerializer("no_sensor", writer));
            EventDeserializers.Add("timer", (ep) => new TimerEvent());
            EventSerializers.Add(typeof(TimerEvent), (ev, writer) => NoParamSerializer("timer", writer));
            EventDeserializers.Add("touch_start", (ep) => new TouchEvent { Type = TouchEvent.TouchType.Start, Detected = ep.Detected });
            EventSerializers.Add(typeof(TouchEvent), TouchSerializer);
            EventDeserializers.Add("touch", (ep) => new TouchEvent { Type = TouchEvent.TouchType.Continuous, Detected = ep.Detected });
            EventDeserializers.Add("touch_end", (ep) => new TouchEvent { Type = TouchEvent.TouchType.End, Detected = ep.Detected });
            EventDeserializers.Add("collision_start", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.Start, Detected = ep.Detected });
            EventSerializers.Add(typeof(CollisionEvent), CollisionSerializer);
            EventDeserializers.Add("collision", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.Continuous, Detected = ep.Detected });
            EventDeserializers.Add("collision_end", (ep) => new CollisionEvent { Type = CollisionEvent.CollisionType.End, Detected = ep.Detected });
            EventDeserializers.Add("not_at_target", (ep) => new NotAtTargetEvent());
            EventSerializers.Add(typeof(NotAtTargetEvent), (ev, writer) => NoParamSerializer("not_at_target", writer));
            EventDeserializers.Add("moving_start", (ep) => new MovingStartEvent());
            EventSerializers.Add(typeof(MovingStartEvent), (ev, writer) => NoParamSerializer("moving_start", writer));
            EventDeserializers.Add("moving_end", (ep) => new MovingEndEvent());
            EventSerializers.Add(typeof(MovingEndEvent), (ev, writer) => NoParamSerializer("moving_end", writer));
        }

        #region RPC

        private static void RpcSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (RpcScriptEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "rpc");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.FunctionName);
                writer.WriteTypedValue("Param", ev.SenderKey);
                writer.WriteTypedValue("Param", ev.SenderLinkNumber);
                writer.WriteTypedValue("Param", ev.SenderScriptName);
                writer.WriteTypedValue("Param", ev.SenderScriptKey);
                foreach (object o in ev.Parameters)
                {
                    writer.WriteTypedValue("Param", o);
                }
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
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


        private static void NoParamSerializer(string name, XmlTextWriter writer)
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

        #region Detected

        private static void DetectedSerializer(List<DetectInfo> di, string name, XmlTextWriter writer)
        {
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", name);
                writer.WriteStartElement("Params");
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                foreach (DetectInfo d in di)
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
        #endregion

        #region Collision

        private static void CollisionSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (CollisionEvent)iev;
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
            }
        }
        #endregion

        #region Touch

        private static void TouchSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (TouchEvent)iev;
            switch (ev.Type)
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
            }
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

        private static void AtRotTargetSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (AtRotTargetEvent)iev;
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

        private static void AtTargetSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (AtTargetEvent)iev;
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
        #endregion

        #region control
        private static IScriptEvent ControlDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 3)
            {
                return new ControlEvent
                {
                    AgentID = new UUID(ep.Params[0].ToString()),
                    Level = (int)ep.Params[1],
                    Flags = (int)ep.Params[2]
                };
            }
            return null;
        }

        private static void ControlSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (ControlEvent)iev;
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

        private static void LandCollisionSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (LandCollisionEvent)iev;
            writer.WriteStartElement("Item");
            {
                switch (ev.Type)
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

        private static void MoneySerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (MoneyEvent)iev;
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

        private static void ChangedSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (ChangedEvent)iev;
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

        private static void AttachSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (AttachEvent)iev;
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

        private static void ListenSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (ListenEvent)iev;
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
        #endregion

        #region http_response

        private static IScriptEvent HttpResponseDeserializer(EventParams ep)
        {
            if (ep.Params.Count >= 4)
            {
                bool isByteArray = ep.Params.Count > 4 && ep.Params[4].ToString() == "ByteArray";
                return new HttpResponseEvent
                {
                    RequestID = new UUID(ep.Params[0].ToString()),
                    Status = (int)ep.Params[1],
                    Metadata = (AnArray)ep.Params[2],
                    Body = isByteArray ? Convert.FromBase64String(ep.Params[3].ToString()) : ep.Params[3].ToString().ToUTF8Bytes()
                };
            }
            return null;
        }

        private static void HttpResponseSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (HttpResponseEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "http_response");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.RequestID);
                writer.WriteTypedValue("Param", ev.Status);
                writer.WriteTypedValue("Param", ev.Metadata);
                if (ev.UsesByteArray)
                {
                    writer.WriteTypedValue("Param", Convert.ToBase64String(ev.Body));
                    writer.WriteTypedValue("Param", "ByteArray");
                }
                else
                {
                    writer.WriteTypedValue("Param", ev.Body.FromUTF8Bytes());
                }
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
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

        private static void DataserverSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (DataserverEvent)iev;
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

        private static void ObjectMessageSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (MessageObjectEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "object_message");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ObjectID);
                writer.WriteTypedValue("Param", ev.Data);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private static void ObjectMessageDataserverSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (MessageObjectEvent)iev;
            writer.WriteStartElement("Item");
            {
                writer.WriteAttributeString("event", "dataserver");
                writer.WriteStartElement("Params");
                writer.WriteTypedValue("Param", ev.ObjectID);
                writer.WriteTypedValue("Param", ev.Data);
                writer.WriteEndElement();
                writer.WriteStartElement("Detected");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
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

        private static void TransactionResultSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (TransactionResultEvent)iev;
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

        private static void RemoteDataSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (RemoteDataEvent)iev;
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

        private static void LinkMessageSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (LinkMessageEvent)iev;
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

        private static void RuntimePermissionsSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (RuntimePermissionsEvent)iev;
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

        private static void EmailSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (EmailEvent)iev;
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

        private static void ObjectRezSerializer(IScriptEvent iev, XmlTextWriter writer)
        {
            var ev = (ObjectRezEvent)iev;
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
        #endregion
    }
}
