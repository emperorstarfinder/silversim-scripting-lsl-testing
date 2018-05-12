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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Vehicles
{
    [ScriptApiName("Vehicle")]
    [LSLImplementation]
    [Description("LSL Vehicle API")]
    public sealed partial class VehicleApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        #region Vehicle flags
        [APILevel(APIFlags.LSL)]
        [APILevel(APIFlags.LSL, "VEHICLE_FLAG_NO_FLY_UP")]
        public const int VEHICLE_FLAG_NO_DEFLECTION_UP = 0x001;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_LIMIT_ROLL_ONLY = 0x002;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_HOVER_WATER_ONLY = 0x004;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_HOVER_TERRAIN_ONLY = 0x008;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_HOVER_GLOBAL_HEIGHT = 0x010;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_HOVER_UP_ONLY = 0x020;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_LIMIT_MOTOR_UP = 0x040;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_MOUSELOOK_STEER = 0x080;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_MOUSELOOK_BANK = 0x100;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_FLAG_CAMERA_DECOUPLED = 0x200;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_REACT_TO_CURRENTS = 0x10000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_REACT_TO_WIND = 0x20000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_LIMIT_MOTOR_DOWN = 0x40000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_TORQUE_WORLD_Z = 0x80000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_MOUSE_POINT_STEER = 0x100000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_MOUSE_POINT_BANK = 0x200000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_FLAG_STOP_MOVE_TO_TARGET_AT_END = 1 << 31;
        #endregion

        #region Vehicle types
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_SLED = 1;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_CAR = 2;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_BOAT = 3;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_AIRPLANE = 4;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_BALLOON = 5;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_TYPE_SAILBOAT = 10001;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_TYPE_MOTORCYCLE = 10002;
        #endregion

        #region Vehicle parameters
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_FRICTION_TIMESCALE = 17;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_DIRECTION = 19;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_FRICTION_TIMESCALE = 16;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_DIRECTION = 18;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_OFFSET = 20;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_HEIGHT = 24;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_EFFICIENCY = 25;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_TIMESCALE = 26;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BUOYANCY = 27;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = 28;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_TIMESCALE = 29;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_TIMESCALE = 30;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = 31;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = 32;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = 33;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_TIMESCALE = 34;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = 35;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = 36;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = 37;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_EFFICIENCY = 38;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_MIX = 39;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_TIMESCALE = 40;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_REFERENCE_FRAME = 44;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_MOUSELOOK_AZIMUTH = 11001;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_MOUSELOOK_ALTITUDE = 11002;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_BANKING_AZIMUTH = 11003;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_DISABLE_MOTORS_ABOVE = 11004;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_DISABLE_MOTORS_AFTER = 11005;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_INVERTED_BANKING_MODIFIER = 11006;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOTOR_ACCEL_POS_TIMESCALE = 13000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOTOR_DECEL_POS_TIMESCALE = 13001;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOTOR_ACCEL_NEG_TIMESCALE = 13002;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOTOR_DECEL_NEG_TIMESCALE = 13003;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOTOR_ACCEL_POS_TIMESCALE = 13100;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOTOR_DECEL_POS_TIMESCALE = 13101;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOTOR_ACCEL_NEG_TIMESCALE = 13102;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOTOR_DECEL_NEG_TIMESCALE = 13103;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOVE_TO_TARGET_EFFICIENCY = 14000;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOVE_TO_TARGET_TIMESCALE = 14001;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOVE_TO_TARGET_EPSILON = 14002;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_LINEAR_MOVE_TO_TARGET_MAX_OUTPUT = 14003;

        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOVE_TO_TARGET_EFFICIENCY = 14100;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOVE_TO_TARGET_TIMESCALE = 14101;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOVE_TO_TARGET_EPSILON = 14102;
        [APIExtension(APIExtension.ExtendedVehicle)]
        public const int VEHICLE_ANGULAR_MOVE_TO_TARGET_MAX_OUTPUT = 14103;
        #endregion

        [APILevel(APIFlags.LSL, "llSetVehicleFlags")]
        public void SetVehicleFlags(ScriptInstance instance, int flags)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.SetVehicleFlags((VehicleFlags)flags);
            }
        }

        [APILevel(APIFlags.LSL, "llRemoveVehicleFlags")]
        public void RemoveVehicleFlags(ScriptInstance instance, int flags)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.ClearVehicleFlags((VehicleFlags)flags);
            }
        }

        [APILevel(APIFlags.LSL, "llSetVehicleFloatParam")]
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
                        thisGroup[VehicleVectorParamId.AngularDeflectionEfficiency] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularDeflectionTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecayTimescale] = new Vector3(value, value, value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorTimescale] = new Vector3(value, value, value);
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
                        thisGroup[VehicleVectorParamId.LinearDeflectionEfficiency] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearDeflectionTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecayTimescale] = new Vector3(value, value, value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorTimescale] = new Vector3(value, value, value);
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.VerticalAttractionEfficiency] = new Vector3(value);
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.VerticalAttractionTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_MOUSELOOK_AZIMUTH:
                        thisGroup[VehicleFloatParamId.MouselookAzimuth] = value;
                        break;

                    case VEHICLE_MOUSELOOK_ALTITUDE:
                        thisGroup[VehicleFloatParamId.MouselookAltitude] = value;
                        break;

                    case VEHICLE_BANKING_AZIMUTH:
                        thisGroup[VehicleFloatParamId.BankingAzimuth] = value;
                        break;

                    case VEHICLE_DISABLE_MOTORS_ABOVE:
                        thisGroup[VehicleFloatParamId.DisableMotorsAbove] = value;
                        break;

                    case VEHICLE_DISABLE_MOTORS_AFTER:
                        thisGroup[VehicleFloatParamId.DisableMotorsAfter] = value;
                        break;

                    case VEHICLE_INVERTED_BANKING_MODIFIER:
                        thisGroup[VehicleFloatParamId.InvertedBankingModifier] = value;
                        break;

                    case VEHICLE_LINEAR_MOTOR_ACCEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorAccelPosTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecelPosTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_ACCEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorAccelNegTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecelNegTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_ACCEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorAccelPosTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecelPosTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_ACCEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorAccelNegTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecelNegTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetEfficiency] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_EPSILON:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetEpsilon] = new Vector3(value);
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_MAX_OUTPUT:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetMaxOutput] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetEfficiency] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetTimescale] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_EPSILON:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetEpsilon] = new Vector3(value);
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_MAX_OUTPUT:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetMaxOutput] = new Vector3(value);
                        break;

                    default:
                        break;
                }
            }
        }

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

                    case VEHICLE_TYPE_MOTORCYCLE:
                        thisGroup.VehicleType = VehicleType.Motorcycle;
                        break;

                    case VEHICLE_TYPE_SAILBOAT:
                        thisGroup.VehicleType = VehicleType.Sailboat;
                        break;

                    default:
                        instance.ShoutError(new LocalizedScriptMessage(this, "InvalidVehicleType", "Invalid vehicle type"));
                        break;
                }
            }
        }

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

                    case VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.AngularDeflectionEfficiency] = vec;
                        break;

                    case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularDeflectionTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecayTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.LinearDeflectionEfficiency] = vec;
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearDeflectionTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecayTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorTimescale] = vec;
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.VerticalAttractionEfficiency] = vec;
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                        thisGroup[VehicleVectorParamId.VerticalAttractionTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_ACCEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorAccelPosTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecelPosTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_ACCEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorAccelNegTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecelNegTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_ACCEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorAccelPosTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECEL_POS_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecelPosTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_ACCEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorAccelNegTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_DECEL_NEG_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecelNegTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetEfficiency] = vec;
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_EPSILON:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetEpsilon] = vec;
                        break;

                    case VEHICLE_LINEAR_MOVE_TO_TARGET_MAX_OUTPUT:
                        thisGroup[VehicleVectorParamId.LinearMoveToTargetMaxOutput] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_EFFICIENCY:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetEfficiency] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_EPSILON:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetEpsilon] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOVE_TO_TARGET_MAX_OUTPUT:
                        thisGroup[VehicleVectorParamId.AngularMoveToTargetMaxOutput] = vec;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
