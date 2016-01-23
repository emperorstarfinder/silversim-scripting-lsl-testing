// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum APIFlags : uint
    {
        None = 0,
        LSL = 1 << 0,
        OSSL = 1 << 1,
        ASSL = 1 << 2,
        Any = 0xFFFFFFFF
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    public sealed class APILevelAttribute : Attribute
    {
        public APIFlags Flags { get; private set; }
        public string Name { get; private set; }

        public APILevelAttribute(APIFlags flags, string name = "")
        {
            Flags = flags;
            Name = name;
        }
    }

    public static class APIExtension
    {
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string LightShare = "LightShare";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string WindLight_New = "WindLight";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string WindLight_Aurora = "WindLight_Aurora";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string Admin = "Admin";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string InWorldz = "InWorldz";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string ExtendedVehicle = "ExtendedVehicle";
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const string AdvancedPhysics = "AdvancedPhysics";
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    public sealed class APIExtensionAttribute : Attribute
    {
        public string Extension { get; private set; }
        public string Name { get; private set; }

        public APIExtensionAttribute(string extension, string name = "")
        {
            Extension = extension;
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ForcedSleepAttribute : Attribute
    {
        public double Seconds { get; private set; }
        public ForcedSleepAttribute(double seconds)
        {
            Seconds = seconds;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnStateChangeAttribute : Attribute
    {
        public ExecutedOnStateChangeAttribute()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnScriptResetAttribute : Attribute
    {
        public ExecutedOnScriptResetAttribute()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnScriptRemoveAttribute : Attribute
    {
        public ExecutedOnScriptRemoveAttribute()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnSerializationAttribute : Attribute
    {
        public string Name { get; set; }
        public ExecutedOnSerializationAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnDeserializationAttribute : Attribute
    {
        public string Name { get; set; }
        public ExecutedOnDeserializationAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LSLImplementationAttribute : Attribute
    {
        public LSLImplementationAttribute()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate, Inherited = false)]
    public sealed class StateEventDelegateAttribute : Attribute
    {
        public StateEventDelegateAttribute()
        {
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class EnergyUsageAttribute : Attribute
    {
        public double Energy { get; private set; }

        public EnergyUsageAttribute(double energy)
        {
            Energy = energy;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class TranslatedScriptEventsInfoAttribute : Attribute
    {
        public TranslatedScriptEventsInfoAttribute()
        {

        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TranslatedScriptEventAttribute : Attribute
    {
        public string EventName { get; private set; }

        public TranslatedScriptEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class TranslatedScriptEventParameterAttribute : Attribute
    {
        public int ParameterNumber { get; private set; }
        
        public TranslatedScriptEventParameterAttribute(int parameterNumber)
        {
            ParameterNumber = parameterNumber;
        }
    }
}
