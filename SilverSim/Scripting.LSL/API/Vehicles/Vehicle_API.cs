// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetVehicleFlags(ScriptInstance instance, int flags)
        {
            instance.Part.ObjectGroup.SetVehicleFlags = (VehicleFlags)flags;
        }

        [APILevel(APIFlags.LSL, "llRemoveVehicleFlags")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void RemoveVehicleFlags(ScriptInstance instance, int flags)
        {
            instance.Part.ObjectGroup.ClearVehicleFlags = (VehicleFlags)flags;
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = 32;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = 33;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = 35;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_MOTOR_TIMESCALE = 34;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_BANKING_EFFICIENCY = 38;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_BANKING_MIX = 39;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_BANKING_TIMESCALE = 40;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_BUOYANCY = 27;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_HOVER_HEIGHT = 24;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_HOVER_EFFICIENCY = 25;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_HOVER_TIMESCALE = 26;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = 28;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_DEFLECTION_TIMESCALE = 29;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = 31;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_MOTOR_TIMESCALE = 30;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = 36;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = 37;

        [APILevel(APIFlags.LSL, "llSetVehicleFloatParam")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetVehicleFloatParam(ScriptInstance instance, int param, double value)
        {
            switch(param)
            {
                case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleVectorParamId.AngularFrictionTimescale] = new Vector3(value);
                    break;

                case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                    instance.Part.ObjectGroup[VehicleVectorParamId.AngularMotorDirection] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearFrictionTimescale] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_MOTOR_DIRECTION:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearMotorDirection] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_MOTOR_OFFSET:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearMotorOffset] = new Vector3(value);
                    break;

                case VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.AngularDeflectionEfficiency] = value;
                    break;

                case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.AngularDeflectionTimescale] = value;
                    break;

                case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.AngularMotorDecayTimescale] = value;
                    break;

                case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.AngularMotorTimescale] = value;
                    break;

                case VEHICLE_BANKING_EFFICIENCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.BankingEfficiency] = value;
                    break;

                case VEHICLE_BANKING_MIX:
                    instance.Part.ObjectGroup[VehicleFloatParamId.BankingMix] = value;
                    break;

                case VEHICLE_BANKING_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.BankingTimescale] = value;
                    break;

                case VEHICLE_BUOYANCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.Buoyancy] = value;
                    break;

                case VEHICLE_HOVER_HEIGHT:
                    instance.Part.ObjectGroup[VehicleFloatParamId.HoverHeight] = value;
                    break;

                case VEHICLE_HOVER_EFFICIENCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.HoverEfficiency] = value;
                    break;

                case VEHICLE_HOVER_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.HoverTimescale] = value;
                    break;

                case VEHICLE_LINEAR_DEFLECTION_EFFICIENCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.LinearDeflectionEfficiency] = value;
                    break;

                case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.LinearDeflectionTimescale] = value;
                    break;

                case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.LinearMotorDecayTimescale] = value;
                    break;

                case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.LinearMotorTimescale] = value;
                    break;

                case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                    instance.Part.ObjectGroup[VehicleFloatParamId.VerticalAttractionEfficiency] = value;
                    break;

                case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleFloatParamId.VerticalAttractionTimescale] = value;
                    break;

                default:
                    break;
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_REFERENCE_FRAME = 44;

        [APILevel(APIFlags.LSL, "llSetVehicleRotationParam")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetVehicleRotationParam(ScriptInstance instance, int param, Quaternion rot)
        {
            switch(param)
            {
                case VEHICLE_REFERENCE_FRAME:
                    instance.Part.ObjectGroup[VehicleRotationParamId.ReferenceFrame] = rot;
                    break;

                default:
                    break;
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_NONE = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_SLED = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_CAR = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_BOAT = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_AIRPLANE = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_TYPE_BALLOON = 5;

        [APILevel(APIFlags.LSL, "llSetVehicleType")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetVehicleType(ScriptInstance instance, int type)
        {
            switch(type)
            {
                case VEHICLE_TYPE_NONE:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.None;
                    break;

                case VEHICLE_TYPE_SLED:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.Sled;
                    break;

                case VEHICLE_TYPE_CAR:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.Car;
                    break;

                case VEHICLE_TYPE_BOAT:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.Boat;
                    break;

                case VEHICLE_TYPE_AIRPLANE:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.Airplane;
                    break;

                case VEHICLE_TYPE_BALLOON:
                    instance.Part.ObjectGroup.VehicleType = VehicleType.Balloon;
                    break;

                default:
                    instance.ShoutError("Invalid vehicle type");
                    break;
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_FRICTION_TIMESCALE = 17;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_ANGULAR_MOTOR_DIRECTION = 19;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_FRICTION_TIMESCALE = 16;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_MOTOR_DIRECTION = 18;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int VEHICLE_LINEAR_MOTOR_OFFSET = 20;

        [APILevel(APIFlags.LSL, "llSetVehicleVectorParam")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetVehicleVectorParam(ScriptInstance instance, int param, Vector3 vec)
        {
            switch(param)
            {
                case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleVectorParamId.AngularFrictionTimescale] = vec;
                    break;
                
                case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                    instance.Part.ObjectGroup[VehicleVectorParamId.AngularMotorDirection] = vec;
                    break;
                
                case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearFrictionTimescale] = vec;
                    break;
                
                case VEHICLE_LINEAR_MOTOR_DIRECTION:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearMotorDirection] = vec;
                    break;

                case VEHICLE_LINEAR_MOTOR_OFFSET:
                    instance.Part.ObjectGroup[VehicleVectorParamId.LinearMotorOffset] = vec;
                    break;

            }
        }
    }
}
