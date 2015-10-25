// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    [Flags]
    public enum APIFlags : uint
    {
        None = 0,
        LSL = 1 << 0,
        OSSL = 1 << 1,
        ASSL = 1 << 2,
        //LightShare = 1 << 1,
        //ASSL_Admin = 1 << 4,
        //WindLight_Aurora = 1 << 5,
        //WindLight_New = 1 << 6,
        Any = 0xFFFFFFFF
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    public sealed class APILevel : Attribute
    {
        public APIFlags Flags { get; private set; }
        public string Name { get; private set; }

        public const string KeepCsName = "";

        public APILevel(APIFlags flags, string name)
        {
            Flags = flags;
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    public sealed class APIExtension : Attribute
    {
        public string Extension { get; private set; }
        public string Name { get; private set; }

        public const string KeepCsName = "";

        public const string LightShare = "LightShare";
        public const string WindLight_New = "WindLight";
        public const string WindLight_Aurora = "WindLight_Aurora";
        public const string Admin = "Admin";

        public APIExtension(string extension, string name)
        {
            Extension = extension;
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ForcedSleep : Attribute
    {
        public double Seconds { get; private set; }
        public ForcedSleep(double seconds)
        {
            Seconds = seconds;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnStateChange : Attribute
    {
        public ExecutedOnStateChange()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnScriptReset : Attribute
    {
        public ExecutedOnScriptReset()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LSLImplementation : Attribute
    {
        public LSLImplementation()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate, Inherited = false)]
    public sealed class StateEventDelegate : Attribute
    {
        public StateEventDelegate()
        {
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class EnergyUsage : Attribute
    {
        public double Energy { get; private set; }

        public EnergyUsage(double energy)
        {
            Energy = energy;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
    public sealed class LSLTooltip : Attribute
    {
        public string Tooltip { get; private set; }

        public LSLTooltip(string tooltip)
        {
            Tooltip = tooltip.Replace("\n", "\\n");
        }
    }
}
