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

using System;

namespace SilverSim.Scripting.Lsl
{
    [Flags]
    public enum APIFlags : uint
    {
        None = 0,
        LSL = 1 << 0,
        OSSL = 1 << 1,
        ASSL = 1 << 2,
        Any = 0xFFFFFFFF
    }

    public enum APIUseAsEnum
    {
        Function = 0,
        Getter = 1,
        Setter = 2,
        MemberFunction = 3
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CheckFunctionPermissionAttribute : Attribute
    {
        public string FunctionName { get; }
        public CheckFunctionPermissionAttribute(string functionName = "")
        {
            FunctionName = functionName;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ImplementsCustomTypecastsAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowKeyOnlyEnumerationOnKeyValuePairAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcLinksetExternalCallAllowedAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcLinksetExternalCallSameGroupAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcLinksetExternalCallEveryoneAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IsPureAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowExplicitTypecastsBeImplicitToStringAttribute : Attribute
    {
        public int[] ParameterNumbers { get; }
        public AllowExplicitTypecastsBeImplicitToStringAttribute(params int[] parameternumbers)
        {
            ParameterNumbers = parameternumbers;
        }
    }

    public interface IAPIDeclaration
    {
        string Name { get; }
        APIUseAsEnum UseAs { get; }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class APILevelAttribute : Attribute, IAPIDeclaration
    {
        public APIFlags Flags { get; }
        public string Name { get; }
        public APIUseAsEnum UseAs { get; }

        public APILevelAttribute(APIFlags flags, string name = "")
        {
            Flags = flags;
            Name = name;
            UseAs = APIUseAsEnum.Function;
        }

        public APILevelAttribute(APIFlags flags, APIUseAsEnum useAs, string name = "")
        {
            Flags = flags;
            Name = name;
            UseAs = useAs;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class APIDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public APIDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class APIAccessibleMembersAttribute : Attribute
    {
        public string[] Members { get; }

        public APIAccessibleMembersAttribute(params string[] members)
        {
            Members = members;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class APICloneOnAssignmentAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class APIIsVariableTypeAttribute : Attribute
    {
    }

    public static class APIExtension
    {
        public const string ByteArray = "ByteArray";
        public const string LightShare = "LightShare";
        public const string WindLight_New = "WindLight";
        public const string WindLight_Aurora = "WindLight_Aurora";
        public const string Admin = "Admin";
        public const string InWorldz = "InWorldz";
        public const string ExtendedVehicle = "ExtendedVehicle";
        public const string AdvancedPhysics = "AdvancedPhysics";
        public const string ExtendedTypecasts = "ExtendedTypecasts";
        public const string StateVariables = "StateVariables";
        public const string BreakContinue = "BreakContinue";
        public const string SwitchBlock = "Switch";
        public const string Selling = "Selling";
        public const string Pathfinding = "Pathfinding";
        public const string LongInteger = "LongInteger";
        public const string Properties = "Properties";
        public const string AgentInventory = "AgentInventory";
        public const string MemberFunctions = "MemberFunctions";
        public const string Structs = "Structs";
        public const string CharacterType = "char";
        public const string Extern = "extern";
        public const string RezAsync = "RezAsync";
        public const string InheritEvents = "InheritEvents";
        public const string PureFunctions = "PureFunctions";
        public const string Const = "Const";
        public const string DateTime = "DateTime";
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class APIExtensionAttribute : Attribute, IAPIDeclaration
    {
        public string Extension { get; }
        public string Name { get; }
        public APIUseAsEnum UseAs { get; }

        public APIExtensionAttribute(string extension, string name = "")
        {
            Extension = extension;
            Name = name;
            UseAs = APIUseAsEnum.Function;
        }

        public APIExtensionAttribute(string extension, APIUseAsEnum useAs, string name = "")
        {
            Extension = extension;
            Name = name;
            UseAs = useAs;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ForcedSleepAttribute : Attribute
    {
        public double Seconds { get; }
        public ForcedSleepAttribute(double seconds)
        {
            Seconds = seconds;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnStateChangeAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnScriptResetAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnScriptRemoveAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnSerializationAttribute : Attribute
    {
        public string Name { get; }
        public ExecutedOnSerializationAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExecutedOnDeserializationAttribute : Attribute
    {
        public string Name { get; }
        public ExecutedOnDeserializationAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LSLImplementationAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate, Inherited = false)]
    public sealed class StateEventDelegateAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class EnergyUsageAttribute : Attribute
    {
        public double Energy { get; }

        public EnergyUsageAttribute(double energy)
        {
            Energy = energy;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class TranslatedScriptEventsInfoAttribute : Attribute
    {
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TranslatedScriptEventAttribute : Attribute
    {
        public string EventName { get; }
        public bool IsSerialized { get; }

        public TranslatedScriptEventAttribute(string eventName)
        {
            EventName = eventName;
            IsSerialized = true;
        }

        public TranslatedScriptEventAttribute(string eventName, bool isSerialized)
        {
            EventName = eventName;
            IsSerialized = isSerialized;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class TranslatedScriptEventParameterAttribute : Attribute
    {
        public int ParameterNumber { get; }

        public TranslatedScriptEventParameterAttribute(int parameterNumber)
        {
            ParameterNumber = parameterNumber;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class TranslatedScriptEventDetectedInfoAttribute : Attribute
    {
    }
}
