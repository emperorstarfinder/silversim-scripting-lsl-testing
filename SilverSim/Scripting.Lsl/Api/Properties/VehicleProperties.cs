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

#pragma warning disable IDE0018

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.Vehicles;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("VehicleProperties")]
    [Description("Vehicle Properties API")]
    public class VehicleProperties : IPlugin, IScriptApi
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

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

            protected VehicleBaseData(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
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
            public VehicleData(ScriptInstance instance) : base(instance)
            {
            }

            public int Type
            {
                get { return With((ObjectGroup g) => (int)g.VehicleType); }
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
            return new VehicleData(instance);
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
