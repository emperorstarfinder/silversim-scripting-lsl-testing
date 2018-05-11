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


using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Vehicles
{
    public sealed partial class VehicleApi
    {

        public abstract class VehicleBaseData
        {
            protected readonly WeakReference<ScriptInstance> WeakInstance;

            protected T With<T>(Func<ObjectGroup, T> getter)
            {
                ScriptInstance instance;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        return getter(instance.Part.ObjectGroup);
                    }
                }
                return default(T);
            }

            protected void With<T>(Action<ObjectGroup, T> setter, T value)
            {
                ScriptInstance instance;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        setter(instance.Part.ObjectGroup, value);
                    }
                }
            }

            protected T With<T>(Func<ScriptInstance, T> getter, T defvalue)
            {
                ScriptInstance instance;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        return getter(instance);
                    }
                }
                return defvalue;
            }

            protected VehicleBaseData()
            {
            }

            protected VehicleBaseData(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_angular")]
        [APIDisplayName("vehicle_angular")]
        [APIAccessibleMembers]
        public class VehicleAngularData : VehicleBaseData
        {
            public VehicleAngularData()
            {
            }

            public VehicleAngularData(ScriptInstance instance) : base(instance)
            {
            }

            public Vector3 FrictionTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularFrictionTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularFrictionTimescale] = v, value); }
            }

            public Vector3 MotorDirection
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorDirection]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorDirection] = v, value); }
            }

            public Vector3 MotorDecayTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorDecayTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorDecayTimescale] = v, value); }
            }

            public Vector3 MotorTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorTimescale] = v, value); }
            }

            public Vector3 MotorAccelPosTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorAccelPosTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorAccelPosTimescale] = v, value); }
            }

            public Vector3 MotorDecelPosTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorDecelPosTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorDecelPosTimescale] = v, value); }
            }

            public Vector3 MotorAccelNegTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorAccelNegTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorAccelNegTimescale] = v, value); }
            }

            public Vector3 MotorDecelNegTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularMotorDecelNegTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularMotorDecelNegTimescale] = v, value); }
            }

            public Vector3 WindEfficiency
            {
                get { return With((g) => g[VehicleVectorParamId.AngularWindEfficiency]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularWindEfficiency] = v, value); }
            }

            public Vector3 DeflectionEfficiency
            {
                get { return With((g) => g[VehicleVectorParamId.AngularDeflectionEfficiency]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularDeflectionEfficiency] = v, value); }
            }

            public Vector3 DeflectionTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.AngularDeflectionTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.AngularDeflectionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_linear")]
        [APIDisplayName("vehicle_linear")]
        [APIAccessibleMembers]
        public class VehicleLinearData : VehicleBaseData
        {
            public VehicleLinearData()
            {
            }

            public VehicleLinearData(ScriptInstance instance) : base(instance)
            {
            }

            public Vector3 FrictionTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearFrictionTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearFrictionTimescale] = v, value); }
            }

            public Vector3 MotorDirection
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorDirection]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorDirection] = v, value); }
            }

            public Vector3 MotorOffset
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorOffset]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorOffset] = v, value); }
            }

            public Vector3 MotorDecayTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorDecayTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorDecayTimescale] = value, value); }
            }

            public Vector3 MotorTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorTimescale] = v, value); }
            }

            public Vector3 MotorAccelPosTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorAccelPosTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorAccelPosTimescale] = v, value); }
            }

            public Vector3 MotorDecelPosTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorDecelPosTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorDecelPosTimescale] = v, value); }
            }

            public Vector3 MotorAccelNegTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorAccelNegTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorAccelNegTimescale] = v, value); }
            }

            public Vector3 MotorDecelNegTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearMotorDecelNegTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearMotorDecelNegTimescale] = v, value); }
            }

            public Vector3 WindEfficiency
            {
                get { return With((g) => g[VehicleVectorParamId.LinearWindEfficiency]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearWindEfficiency] = v, value); }
            }

            public Vector3 DeflectionEfficiency
            {
                get { return With((g) => g[VehicleVectorParamId.LinearDeflectionEfficiency]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearDeflectionEfficiency] = v, value); }
            }

            public Vector3 DeflectionTimescale
            {
                get { return With((g) => g[VehicleVectorParamId.LinearDeflectionTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.LinearDeflectionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_banking")]
        [APIDisplayName("vehicle_banking")]
        [APIAccessibleMembers]
        public class VehicleBankingData : VehicleBaseData
        {
            public VehicleBankingData()
            {

            }

            public VehicleBankingData(ScriptInstance instance) : base(instance)
            {
            }

            public double Efficiency
            {
                get { return With((g) => g[VehicleFloatParamId.BankingEfficiency]); }
                set { With((g, v) => g[VehicleFloatParamId.BankingEfficiency] = v, value); }
            }

            public double Mix
            {
                get { return With((g) => g[VehicleFloatParamId.BankingMix]); }
                set { With((g, v) => g[VehicleFloatParamId.BankingMix] = v, value); }
            }

            public double Timescale
            {
                get { return With((g) => g[VehicleFloatParamId.BankingTimescale]); }
                set { With((g, v) => g[VehicleFloatParamId.BankingTimescale] = v, value); }
            }

            public double Azimuth
            {
                get { return With((g) => g[VehicleFloatParamId.BankingAzimuth]); }
                set { With((g, v) => g[VehicleFloatParamId.BankingAzimuth] = v, value); }
            }

            public double InvertedModifier
            {
                get { return With((g) => g[VehicleFloatParamId.InvertedBankingModifier]); }
                set { With((g, v) => g[VehicleFloatParamId.InvertedBankingModifier] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_hover")]
        [APIDisplayName("vehicle_hover")]
        [APIAccessibleMembers]
        public class VehicleHoverData : VehicleBaseData
        {
            public VehicleHoverData()
            {

            }

            public VehicleHoverData(ScriptInstance instance) : base(instance)
            {
            }

            public double Buoyancy
            {
                get { return With((g) => g[VehicleFloatParamId.Buoyancy]); }
                set { With((g, v) => g[VehicleFloatParamId.Buoyancy] = v, value); }
            }

            public double Height
            {
                get { return With((g) => g[VehicleFloatParamId.HoverHeight]); }
                set { With((g, v) => g[VehicleFloatParamId.HoverHeight] = v, value); }
            }

            public double Efficiency
            {
                get { return With((g) => g[VehicleFloatParamId.HoverEfficiency]); }
                set { With((g, v) => g[VehicleFloatParamId.HoverEfficiency] = v, value); }
            }

            public double Timescale
            {
                get { return With((g) => g[VehicleFloatParamId.HoverTimescale]); }
                set { With((g, v) => g[VehicleFloatParamId.HoverTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_verticalattraction")]
        [APIDisplayName("vehicle_verticalattraction")]
        [APIAccessibleMembers]
        public class VehicleVerticalAttractionData : VehicleBaseData
        {
            public VehicleVerticalAttractionData()
            {
            }

            public VehicleVerticalAttractionData(ScriptInstance instance) : base(instance)
            {
            }

            public Vector3 Efficiency
            {
                get { return With((g) => g[VehicleVectorParamId.VerticalAttractionEfficiency]); }
                set { With((g, v) => g[VehicleVectorParamId.VerticalAttractionEfficiency] = v, value); }
            }

            public Vector3 Timescale
            {
                get { return With((g) => g[VehicleVectorParamId.VerticalAttractionTimescale]); }
                set { With((g, v) => g[VehicleVectorParamId.VerticalAttractionTimescale] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle_mouselook")]
        [APIDisplayName("vehicle_mouselook")]
        [APIAccessibleMembers]
        public class VehicleMouselookData : VehicleBaseData
        {
            public VehicleMouselookData()
            {
            }

            public VehicleMouselookData(ScriptInstance instance) : base(instance)
            {
            }

            public double Azimuth
            {
                get { return With((g) => g[VehicleFloatParamId.MouselookAzimuth]); }
                set { With((g, v) => g[VehicleFloatParamId.MouselookAzimuth] = v, value); }
            }

            public double Altitude
            {
                get { return With((g) => g[VehicleFloatParamId.MouselookAltitude]); }
                set { With((g, v) => g[VehicleFloatParamId.MouselookAltitude] = v, value); }
            }
        }

        [APIExtension(APIExtension.Properties, "vehicle")]
        [APIDisplayName("vehicle")]
        [APIAccessibleMembers]
        public class VehicleData : VehicleBaseData
        {
            public VehicleData(ScriptInstance instance) : base(instance)
            {
            }

            public int Type
            {
                get { return With((g) => (int)g.VehicleType); }
                set
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        lock (instance)
                        {
                            ObjectGroup thisGroup = instance.Part.ObjectGroup;
                            switch (value)
                            {
                                case VehicleApi.VEHICLE_TYPE_NONE:
                                    thisGroup.VehicleType = VehicleType.None;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_SLED:
                                    thisGroup.VehicleType = VehicleType.Sled;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_CAR:
                                    thisGroup.VehicleType = VehicleType.Car;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_BOAT:
                                    thisGroup.VehicleType = VehicleType.Boat;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_AIRPLANE:
                                    thisGroup.VehicleType = VehicleType.Airplane;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_BALLOON:
                                    thisGroup.VehicleType = VehicleType.Balloon;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_MOTORCYCLE:
                                    thisGroup.VehicleType = VehicleType.Motorcycle;
                                    break;

                                case VehicleApi.VEHICLE_TYPE_SAILBOAT:
                                    thisGroup.VehicleType = VehicleType.Sailboat;
                                    break;

                                default:
                                    instance.ShoutError(new LocalizedScriptMessage(this, "InvalidVehicleType", "Invalid vehicle type"));
                                    break;
                            }
                        }
                    }
                }
            }

            public int Flags
            {
                get { return With((g) => (int)g.VehicleFlags); }
                set { With((g, v) => g.VehicleFlags = (VehicleFlags)v, value); }
            }

            public Quaternion ReferenceFrame
            {
                get { return With((g) => g[VehicleRotationParamId.ReferenceFrame]); }
                set { With((g, v) => g[VehicleRotationParamId.ReferenceFrame] = v, value); }
            }

            public double MouselookAzimuth
            {
                get { return With((g) => g[VehicleFloatParamId.MouselookAzimuth]); }
                set { With((g, v) => g[VehicleFloatParamId.MouselookAzimuth] = v, value); }
            }

            public double MouselookAltitude
            {
                get { return With((g) => g[VehicleFloatParamId.MouselookAltitude]); }
                set { With((g, v) => g[VehicleFloatParamId.MouselookAltitude] = v, value); }
            }

            public double DisableMotorsAbove
            {
                get { return With((g) => g[VehicleFloatParamId.DisableMotorsAbove]); }
                set { With((g, v) => g[VehicleFloatParamId.DisableMotorsAbove] = v, value); }
            }

            public double DisableMotorsAfter
            {
                get { return With((g) => g[VehicleFloatParamId.DisableMotorsAfter]); }
                set { With((g, v) => g[VehicleFloatParamId.DisableMotorsAfter] = v, value); }
            }

            public VehicleLinearData Linear =>
                With((instance) => new VehicleLinearData(instance), new VehicleLinearData());

            public VehicleAngularData Angular =>
                With((instance) => new VehicleAngularData(instance), new VehicleAngularData());

            public VehicleBankingData Banking =>
                With((instance) => new VehicleBankingData(instance), new VehicleBankingData());

            public VehicleHoverData Hover =>
                With((instance) => new VehicleHoverData(instance), new VehicleHoverData());

            public VehicleVerticalAttractionData VerticalAttract =>
                With((instance) => new VehicleVerticalAttractionData(instance), new VehicleVerticalAttractionData());

            public VehicleMouselookData Mouselook =>
                With((instance) => new VehicleMouselookData(instance), new VehicleMouselookData());
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Vehicle")]
        public VehicleData GetVehicle(ScriptInstance instance)
        {
            return new VehicleData(instance);
        }
    }
}
