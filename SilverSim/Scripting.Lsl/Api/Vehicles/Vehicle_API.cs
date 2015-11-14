// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Vehicles
{
    [ScriptApiName("Vehicle")]
    [LSLImplementation]
    public class VehicleApi : IScriptApi, IPlugin
    {
        public VehicleApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, "llSetVehicleFlags")]
        public void SetVehicleFlags(ScriptInstance instance, int flags)
        {
            instance.Part.ObjectGroup.SetVehicleFlags = (VehicleFlags)flags;
        }

        [APILevel(APIFlags.LSL, "llRemoveVehicleFlags")]
        public void RemoveVehicleFlags(ScriptInstance instance, int flags)
        {
            instance.Part.ObjectGroup.ClearVehicleFlags = (VehicleFlags)flags;
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = 32;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = 33;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = 35;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_MOTOR_TIMESCALE = 34;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_BANKING_EFFICIENCY = 38;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_BANKING_MIX = 39;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_BANKING_TIMESCALE = 40;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_BUOYANCY = 27;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_HOVER_HEIGHT = 24;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_HOVER_EFFICIENCY = 25;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_HOVER_TIMESCALE = 26;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = 28;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_DEFLECTION_TIMESCALE = 29;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = 31;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_MOTOR_TIMESCALE = 30;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = 36;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = 37;

        [APILevel(APIFlags.LSL, "llSetVehicleFloatParam")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public void SetVehicleFloatParam(ScriptInstance instance, int param, double value)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                switch (param)
                {
                    case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularFrictionTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                        thisGroup[VehicleVectorParamId.AngularMotorDirection] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearFrictionTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_DIRECTION:
                        thisGroup[VehicleVectorParamId.LinearMotorDirection] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_OFFSET:
                        thisGroup[VehicleVectorParamId.LinearMotorOffset] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.AngularDeflectionEfficiency] = value;
                        break;

                    case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.AngularDeflectionTimescale] = value;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleFloatParamId.AngularMotorDecayTimescale] = value;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleFloatParamId.AngularMotorTimescale] = value;
                        break;

                    case VEHICLE_BANKING_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.BankingEfficiency] = value;
                        break;

                    case VEHICLE_BANKING_MIX:
                        thisGroup[VehicleFloatParamId.BankingMix] = value;
                        break;

                    case VEHICLE_BANKING_TIMESCALE:
                        thisGroup[VehicleFloatParamId.BankingTimescale] = value;
                        break;

                    case VEHICLE_BUOYANCY:
                        thisGroup[VehicleFloatParamId.Buoyancy] = value;
                        break;

                    case VEHICLE_HOVER_HEIGHT:
                        thisGroup[VehicleFloatParamId.HoverHeight] = value;
                        break;

                    case VEHICLE_HOVER_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.HoverEfficiency] = value;
                        break;

                    case VEHICLE_HOVER_TIMESCALE:
                        thisGroup[VehicleFloatParamId.HoverTimescale] = value;
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.LinearDeflectionEfficiency] = value;
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.LinearDeflectionTimescale] = value;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleFloatParamId.LinearMotorDecayTimescale] = value;
                        break;

                    case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleFloatParamId.LinearMotorTimescale] = value;
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.VerticalAttractionEfficiency] = value;
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.VerticalAttractionTimescale] = value;
                        break;

                    default:
                        break;
                }
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_REFERENCE_FRAME = 44;

        [APILevel(APIFlags.LSL, "llSetVehicleRotationParam")]
        public void SetVehicleRotationParam(ScriptInstance instance, int param, Quaternion rot)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                switch (param)
                {
                    case VEHICLE_REFERENCE_FRAME:
                        thisGroup[VehicleRotationParamId.ReferenceFrame] = rot;
                        break;

                    default:
                        break;
                }
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_NONE = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_SLED = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_CAR = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_BOAT = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_AIRPLANE = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_TYPE_BALLOON = 5;

        [APILevel(APIFlags.LSL, "llSetVehicleType")]
        public void SetVehicleType(ScriptInstance instance, int type)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                switch (type)
                {
                    case VEHICLE_TYPE_NONE:
                        thisGroup.VehicleType = VehicleType.None;
                        break;

                    case VEHICLE_TYPE_SLED:
                        thisGroup.VehicleType = VehicleType.Sled;
                        break;

                    case VEHICLE_TYPE_CAR:
                        thisGroup.VehicleType = VehicleType.Car;
                        break;

                    case VEHICLE_TYPE_BOAT:
                        thisGroup.VehicleType = VehicleType.Boat;
                        break;

                    case VEHICLE_TYPE_AIRPLANE:
                        thisGroup.VehicleType = VehicleType.Airplane;
                        break;

                    case VEHICLE_TYPE_BALLOON:
                        thisGroup.VehicleType = VehicleType.Balloon;
                        break;

                    default:
                        instance.ShoutError("Invalid vehicle type");
                        break;
                }
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_FRICTION_TIMESCALE = 17;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_ANGULAR_MOTOR_DIRECTION = 19;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_FRICTION_TIMESCALE = 16;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_MOTOR_DIRECTION = 18;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int VEHICLE_LINEAR_MOTOR_OFFSET = 20;

        [APILevel(APIFlags.LSL, "llSetVehicleVectorParam")]
        public void SetVehicleVectorParam(ScriptInstance instance, int param, Vector3 vec)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                switch (param)
                {
                    case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularFrictionTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                        thisGroup[VehicleVectorParamId.AngularMotorDirection] = vec;
                        break;

                    case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearFrictionTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DIRECTION:
                        thisGroup[VehicleVectorParamId.LinearMotorDirection] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_OFFSET:
                        thisGroup[VehicleVectorParamId.LinearMotorOffset] = vec;
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
