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
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Vehicles
{
    [ScriptApiName("Vehicle")]
    [LSLImplementation]
    [Description("LSL Vehicle API")]
    public class VehicleApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
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

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = 32;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = 33;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = 35;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_TIMESCALE = 34;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_EFFICIENCY = 38;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_MIX = 39;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_TIMESCALE = 40;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BUOYANCY = 27;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_HEIGHT = 24;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_EFFICIENCY = 25;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_TIMESCALE = 26;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = 28;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_TIMESCALE = 29;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = 31;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_TIMESCALE = 30;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = 36;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = 37;
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
                        thisGroup[VehicleFloatParamId.AngularDeflectionEfficiency] = value;
                        break;

                    case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.AngularDeflectionTimescale] = value;
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
                        thisGroup[VehicleFloatParamId.LinearDeflectionEfficiency] = value;
                        break;

                    case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.LinearDeflectionTimescale] = value;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecayTimescale] = new Vector3(value, value, value);
                        break;

                    case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorTimescale] = new Vector3(value, value, value);
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                        thisGroup[VehicleFloatParamId.VerticalAttractionEfficiency] = value;
                        break;

                    case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                        thisGroup[VehicleFloatParamId.VerticalAttractionTimescale] = value;
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

                    default:
                        break;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
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

                    case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorDecayTimescale] = vec;
                        break;

                    case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.AngularMotorTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorDecayTimescale] = vec;
                        break;

                    case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                        thisGroup[VehicleVectorParamId.LinearMotorTimescale] = vec;
                        break;

                    default:
                        break;
                }
            }
        }

        public abstract class VehicleBaseData
        {
            protected readonly ScriptInstance Instance;

            protected T With<T>(Func<ObjectGroup, T> getter)
            {
                lock (Instance)
                {
                    return getter(Instance.Part.ObjectGroup);
                }
            }

            protected void With<T>(Action<ObjectGroup, T> setter, T value)
            {
                lock (Instance)
                {
                    setter(Instance.Part.ObjectGroup, value);
                }
            }

            protected VehicleBaseData(ScriptInstance instance)
            {
                Instance = instance;
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_angular")]
        [APIDisplayName("vehicle_angular")]
        [APIAccessibleMembers(
            "FrictionTimescale",
            "MotorDirection",
            "MotorDecayTimescale",
            "MotorTimescale",
            "WindEfficiency",
            "DeflectionEfficiency",
            "DeflectionTimescale")]
        public class VehicleAngularData : VehicleBaseData
        {
            public VehicleAngularData(ScriptInstance instance) : base(instance)
            {
            }

            public Vector3 FrictionTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.AngularFrictionTimescale]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.AngularFrictionTimescale] = v, value); }
            }

            public Vector3 MotorDirection
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.AngularMotorDirection]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.AngularMotorDirection] = v, value); }
            }

            public Vector3 MotorDecayTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.AngularMotorDecayTimescale]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.AngularMotorDecayTimescale] = v, value); }
            }

            public Vector3 MotorTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.AngularWindEfficiency]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.AngularMotorTimescale] = v, value); }
            }

            public Vector3 WindEfficiency
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.AngularWindEfficiency]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.AngularWindEfficiency] = v, value); }
            }

            public double DeflectionEfficiency
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.AngularDeflectionEfficiency]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.AngularDeflectionEfficiency] = v, value); }
            }

            public double DeflectionTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.AngularDeflectionTimescale]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.AngularDeflectionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_linear")]
        [APIDisplayName("vehicle_linear")]
        [APIAccessibleMembers(
            "FrictionTimescale",
            "MotorDirection",
            "MotorOffset",
            "MotorDecayTimescale",
            "MotorTimescale",
            "WindEfficiency",
            "DeflectionEfficiency",
            "DeflectionTimescale")]
        public class VehicleLinearData : VehicleBaseData
        {
            public VehicleLinearData(ScriptInstance instance) : base(instance)
            {
            }

            public Vector3 FrictionTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearFrictionTimescale]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearFrictionTimescale] = v, value); }
            }

            public Vector3 MotorDirection
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearMotorDirection]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearMotorDirection] = v, value); }
            }

            public Vector3 MotorOffset
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearMotorOffset]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearMotorOffset] = v, value); }
            }

            public Vector3 MotorDecayTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearMotorDecayTimescale]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearMotorDecayTimescale] = value, value); }
            }

            public Vector3 MotorTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearMotorTimescale]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearMotorTimescale] = v, value); }
            }

            public Vector3 WindEfficiency
            {
                get { return With((ObjectGroup g) => g[VehicleVectorParamId.LinearWindEfficiency]); }
                set { With((ObjectGroup g, Vector3 v) => g[VehicleVectorParamId.LinearWindEfficiency] = v, value); }
            }

            public double DeflectionEfficiency
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.LinearDeflectionEfficiency]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.LinearDeflectionEfficiency] = v, value); }
            }

            public double DeflectionTimescale
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.LinearDeflectionTimescale]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.LinearDeflectionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle")]
        [APIDisplayName("vehicle")]
        [APIAccessibleMembers(
            "Type",
            "ReferenceFrame",
            "MouselookAzimuth",
            "MouselookAltitude",
            "DisableMotorsAbove",
            "DisableMotorsAfter")]
        public class VehicleData : VehicleBaseData
        {
            private readonly VehicleApi m_Api;

            public VehicleData(VehicleApi api, ScriptInstance instance) : base(instance)
            {
                m_Api = api;
            }

            public int Type
            {
                get { return With((ObjectGroup g) => (int)g.VehicleType); }
                set { m_Api.SetVehicleType(Instance, value); }
            }

            public int Flags
            {
                get { return With((ObjectGroup g) => (int)g.VehicleFlags); }
                set { With((ObjectGroup g, int v) => g.VehicleFlags = (VehicleFlags)v, value); }
            }

            public Quaternion ReferenceFrame
            {
                get { return With((ObjectGroup g) => g[VehicleRotationParamId.ReferenceFrame]); }
                set { With((ObjectGroup g, Quaternion v) => g[VehicleRotationParamId.ReferenceFrame] = v, value); }
            }

            public double MouselookAzimuth
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.MouselookAzimuth]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.MouselookAzimuth] = v, value); }
            }

            public double MouselookAltitude
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.MouselookAltitude]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.MouselookAltitude] = v, value); }
            }

            public double DisableMotorsAbove
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.DisableMotorsAbove]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.DisableMotorsAbove] = v, value); }
            }

            public double DisableMotorsAfter
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.DisableMotorsAfter]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.DisableMotorsAfter] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_banking")]
        [APIDisplayName("vehicle_banking")]
        [APIAccessibleMembers(
            "Efficiency",
            "Mix",
            "Timescale",
            "Azimuth",
            "InvertedModifier")]
        public class VehicleBankingData : VehicleBaseData
        {
            public VehicleBankingData(ScriptInstance instance) : base(instance)
            {
            }

            public double Efficiency
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.BankingEfficiency]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.BankingEfficiency] = v, value); }
            }

            public double Mix
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.BankingMix]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.BankingMix] = v, value); }
            }

            public double Timescale
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.BankingTimescale]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.BankingTimescale] = v, value); }
            }

            public double Azimuth
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.BankingAzimuth]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.BankingAzimuth] = v, value); }
            }

            public double InvertedModifier
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.InvertedBankingModifier]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.InvertedBankingModifier] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_hover")]
        [APIDisplayName("vehicle_hover")]
        [APIAccessibleMembers(
            "Buoyancy",
            "Height",
            "Efficiency",
            "Timescale")]
        public class VehicleHoverData : VehicleBaseData
        {
            public VehicleHoverData(ScriptInstance instance) : base(instance)
            {
            }

            public double Buoyancy
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.Buoyancy]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.Buoyancy] = v, value); }
            }

            public double Height
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.HoverHeight]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.HoverHeight] = v, value); }
            }

            public double Efficiency
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.HoverEfficiency]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.HoverEfficiency] = v, value); }
            }

            public double Timescale
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.HoverTimescale]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.HoverTimescale] = v, value); }
            }
        }

        public class VehicleVerticalAttractionData : VehicleBaseData
        {
            public VehicleVerticalAttractionData(ScriptInstance instance) : base(instance)
            {
            }

            public double Efficiency
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.VerticalAttractionEfficiency]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.VerticalAttractionEfficiency] = v, value); }
            }

            public double Timescale
            {
                get { return With((ObjectGroup g) => g[VehicleFloatParamId.VerticalAttractionTimescale]); }
                set { With((ObjectGroup g, double v) => g[VehicleFloatParamId.VerticalAttractionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Vehicle")]
        public VehicleData GetVehicle(ScriptInstance instance)
        {
            return new VehicleData(this, instance);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "VehicleLinear")]
        public VehicleLinearData GetVehicleLinear(ScriptInstance instance)
        {
            return new VehicleLinearData(instance);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "VehicleAngular")]
        public VehicleAngularData GetVehicleAngular(ScriptInstance instance)
        {
            return new VehicleAngularData(instance);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "VehicleBanking")]
        public VehicleBankingData GetVehicleBanking(ScriptInstance instance)
        {
            return new VehicleBankingData(instance);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "VehicleHover")]
        public VehicleHoverData GetVehicleHover(ScriptInstance instance)
        {
            return new VehicleHoverData(instance);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "VehicleVerticalAttraction")]
        public VehicleVerticalAttractionData GetVehicleVerticalAttraction(ScriptInstance instance)
        {
            return new VehicleVerticalAttractionData(instance);
        }
    }
}
