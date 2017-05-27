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

#pragma warning disable IDE0018, RCS1029, IDE0019

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    [CompilerUsesRunAndCollectMode]
    [Description("LSL Compiler")]
    [ServerParam("LSL.CallDepthLimit", ParameterType = typeof(uint), DefaultValue = 40)]
    [ScriptEngineName("lsl")]
    [PluginName("ScriptEngine")]
    public partial class LSLCompiler : IScriptCompiler, IPlugin, IPluginSubFactory, IServerParamListener
    {
        internal struct ApiMethodInfo
        {
            public string FunctionName;
            public IScriptApi Api;
            public MethodInfo Method;

            public ApiMethodInfo(string functionName, IScriptApi api, MethodInfo method)
            {
                FunctionName = functionName;
                Api = api;
                Method = method;
            }
        }

        [ServerParam("LSL.CallDepthLimit")]
        public void SetCallDepthLimit(UUID regionID, string value)
        {
            int val;
            if(UUID.Zero == regionID && int.TryParse(value, out val) && val > 0)
            {
                Script.CallDepthLimit = val;
            }
        }

        internal sealed class ApiInfo
        {
            public sealed class PropertyInfo
            {
                public ApiMethodInfo Getter;
                public ApiMethodInfo Setter;
                public readonly Type PropertyType;

                public PropertyInfo(Type type)
                {
                    PropertyType = type;
                }
            }

            public Dictionary<string, List<ApiMethodInfo>> Methods = new Dictionary<string, List<ApiMethodInfo>>();
            public Dictionary<string, FieldInfo> Constants = new Dictionary<string, FieldInfo>();
            public Dictionary<string, MethodInfo> EventDelegates = new Dictionary<string, MethodInfo>();
            public Dictionary<string, Type> Types = new Dictionary<string, Type>();
            public Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
            public Dictionary<Type, Dictionary<Type, MethodInfo>> Typecasts = new Dictionary<Type, Dictionary<Type, MethodInfo>>();

            public void Add(ApiInfo info)
            {
                foreach(KeyValuePair<string, List<ApiMethodInfo>> mInfo in info.Methods)
                {
                    if(!Methods.ContainsKey(mInfo.Key))
                    {
                        Methods[mInfo.Key] = new List<ApiMethodInfo>(mInfo.Value);
                    }
                    else
                    {
                        Methods[mInfo.Key].AddRange(mInfo.Value);
                    }
                }
                foreach(KeyValuePair<string, FieldInfo> kvp in info.Constants)
                {
                    Constants.Add(kvp.Key, kvp.Value);
                }
                foreach(KeyValuePair<string, MethodInfo> kvp in info.EventDelegates)
                {
                    EventDelegates.Add(kvp.Key, kvp.Value);
                }
                foreach (KeyValuePair<string, Type> kvp in info.Types)
                {
                    Types.Add(kvp.Key, kvp.Value);
                }
                foreach(KeyValuePair<string, PropertyInfo> kvp in info.Properties)
                {
                    Properties.Add(kvp.Key, kvp.Value);
                }
                foreach(KeyValuePair<Type, Dictionary<Type, MethodInfo>> kvp in info.Typecasts)
                {
                    Typecasts.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private static readonly ILog m_Log = LogManager.GetLogger("LSL COMPILER");
        internal List<IScriptApi> m_Apis = new List<IScriptApi>();

        internal Dictionary<APIFlags, ApiInfo> m_ApiInfos = new Dictionary<APIFlags, ApiInfo>();
        internal Dictionary<string, ApiInfo> m_ApiExtensions = new Dictionary<string, ApiInfo>();

        private readonly List<Action<ScriptInstance>> m_StateChangeDelegates = new List<Action<ScriptInstance>>();
        private readonly List<Action<ScriptInstance>> m_ScriptResetDelegates = new List<Action<ScriptInstance>>();
        private readonly List<Action<ScriptInstance>> m_ScriptRemoveDelegates = new List<Action<ScriptInstance>>();
        private readonly Dictionary<string, Action<ScriptInstance, List<object>>> m_ScriptDeserializeDelegates = new Dictionary<string, Action<ScriptInstance, List<object>>>();
        private readonly List<Action<ScriptInstance, List<object>>> m_ScriptSerializeDelegates = new
            List<Action<ScriptInstance, List<object>>>();
        private readonly List<string> m_ReservedWords = new List<string>();
        private readonly List<char> m_SingleOps = new List<char>();
        private readonly List<char> m_MultiOps = new List<char>();
        private readonly List<char> m_NumericChars = new List<char>();
        private readonly List<char> m_OpChars = new List<char>();

        public enum OperatorType
        {
            Unknown,
            RightUnary,
            LeftUnary,
            Binary
        }

        static internal readonly double NegativeZero;

        static LSLCompiler()
        {
            NegativeZero *= -1.0;
        }

        public LSLCompiler()
        {
            m_ApiInfos.Add(APIFlags.ASSL, new ApiInfo());
            m_ApiInfos.Add(APIFlags.LSL, new ApiInfo());
            m_ApiInfos.Add(APIFlags.OSSL, new ApiInfo());

            m_ReservedWords.Add("if");
            m_ReservedWords.Add("while");
            m_ReservedWords.Add("jump");
            m_ReservedWords.Add("for");
            m_ReservedWords.Add("do");
            m_ReservedWords.Add("return");
            m_ReservedWords.Add("state");
            m_ReservedWords.Add("event");

            m_MultiOps.Add('+');
            m_MultiOps.Add('-');
            m_MultiOps.Add('*');
            m_MultiOps.Add('/');
            m_MultiOps.Add('%');
            m_MultiOps.Add('<');
            m_MultiOps.Add('>');
            m_MultiOps.Add('=');
            m_MultiOps.Add('&');
            m_MultiOps.Add('|');
            m_MultiOps.Add('^');
            m_MultiOps.Add('!');

            m_SingleOps.Add('~');
            m_SingleOps.Add('.');
            m_SingleOps.Add(',');
            m_SingleOps.Add('@');

            m_NumericChars.Add('.');
            m_NumericChars.Add('A');
            m_NumericChars.Add('B');
            m_NumericChars.Add('C');
            m_NumericChars.Add('D');
            m_NumericChars.Add('E');
            m_NumericChars.Add('F');
            m_NumericChars.Add('a');
            m_NumericChars.Add('b');
            m_NumericChars.Add('c');
            m_NumericChars.Add('d');
            m_NumericChars.Add('e');
            m_NumericChars.Add('f');
            m_NumericChars.Add('x');
            m_NumericChars.Add('+');
            m_NumericChars.Add('-');

            m_OpChars = new List<char>();
            m_OpChars.AddRange(m_MultiOps);
            m_OpChars.AddRange(m_SingleOps);
        }

        public void AddPlugins(ConfigurationLoader loader)
        {
            loader.AddPlugin("LSLHTTP", new LSLHTTP());
            loader.AddPlugin("LSLHttpClient", new LSLHTTPClient_RequestQueue(loader.Scenes));
            foreach (Type type in GetType().Assembly.GetTypes())
            {
                if (typeof(IScriptApi).IsAssignableFrom(type))
                {
                    var scriptApiAttr = Attribute.GetCustomAttribute(type, typeof(ScriptApiNameAttribute)) as ScriptApiNameAttribute;
                    Attribute impTagAttr = Attribute.GetCustomAttribute(type, typeof(LSLImplementationAttribute));
                    if (impTagAttr != null && scriptApiAttr != null)
                    {
                        var factory = (IPlugin)Activator.CreateInstance(type);
                        loader.AddPlugin("LSL_API_" + scriptApiAttr.Name, factory);
                    }
                }
            }
        }

        private void CollectApis(ConfigurationLoader loader)
        {
            foreach (IScriptApi api in loader.GetServicesByValue<IScriptApi>())
            {
                Type apiType = api.GetType();
                Attribute attr = Attribute.GetCustomAttribute(apiType, typeof(LSLImplementationAttribute));
                if (attr != null && !m_Apis.Contains(api))
                {
                    if ((apiType.Attributes & TypeAttributes.Public) == 0)
                    {
                        m_Log.FatalFormat("LSLImplementation derived {0} is not set to public", apiType.FullName);
                    }
                    else
                    {
                        m_Apis.Add(api);
                    }
                }
            }
        }

        internal static readonly Dictionary<string, Type> KnownSerializationTypes = new Dictionary<string, Type>();

        private void CollectApiTypecasts()
        {
            foreach(Type t in m_ValidTypes.Keys)
            {
                if (Attribute.GetCustomAttribute(t, typeof(ImplementsCustomTypecastsAttribute)) != null)
                {
                    foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (mi.Name != "op_Explicit" && mi.Name != "op_Implicit")
                        {
                            continue;
                        }

                        Type fromType = mi.GetParameters()[0].ParameterType;
                        Type toType = mi.ReturnType;
                        if (fromType != t && toType != t)
                        {
                            continue;
                        }

                        var typecastLevelAttrs = Attribute.GetCustomAttributes(mi, typeof(APILevelAttribute)) as APILevelAttribute[];
                        var typecastExtensionAttrs = Attribute.GetCustomAttributes(mi, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];

                        foreach (APILevelAttribute attr in typecastLevelAttrs)
                        {
                            foreach (KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                            {
                                if ((kvp.Key & attr.Flags) != 0)
                                {
                                    Dictionary<Type, MethodInfo> toDict;
                                    if(!kvp.Value.Typecasts.TryGetValue(fromType, out toDict))
                                    {
                                        toDict = new Dictionary<Type, MethodInfo>();
                                        kvp.Value.Typecasts.Add(fromType, toDict);
                                    }
                                    toDict.Add(toType, mi);
                                }
                            }
                        }

                        foreach (APIExtensionAttribute attr in typecastExtensionAttrs)
                        {
                            ApiInfo apiInfo;
                            string extensionName = attr.Extension.ToLower();
                            if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                            {
                                apiInfo = new ApiInfo();
                                m_ApiExtensions.Add(extensionName, apiInfo);
                            }

                            Dictionary<Type, MethodInfo> toDict;
                            if (!apiInfo.Typecasts.TryGetValue(fromType, out toDict))
                            {
                                toDict = new Dictionary<Type, MethodInfo>();
                                apiInfo.Typecasts.Add(fromType, toDict);
                            }
                            toDict.Add(toType, mi);
                        }
                    }
                }

            }
        }

        private void CollectApiTypes(IScriptApi api)
        {
            foreach (Type t in api.GetType().GetNestedTypes())
            {
                if((!t.IsClass && !t.IsValueType))
                {
                    continue;
                }

                var apiLevelAttrs = Attribute.GetCustomAttributes(t, typeof(APILevelAttribute)) as APILevelAttribute[];
                var apiExtensionAttrs = Attribute.GetCustomAttributes(t, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                if (apiLevelAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                {
                    var apiDisplayNameAttr = Attribute.GetCustomAttribute(t, typeof(APIDisplayNameAttribute)) as APIDisplayNameAttribute;
                    if (apiDisplayNameAttr != null)
                    {
                        foreach (APILevelAttribute attr in apiLevelAttrs)
                        {
                            string typeName = apiDisplayNameAttr.DisplayName;
                            if (string.IsNullOrEmpty(typeName))
                            {
                                typeName = t.Name;
                            }
                            foreach (KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                            {
                                if ((kvp.Key & attr.Flags) != 0)
                                {
                                    kvp.Value.Types.Add(typeName, t);
                                    m_ValidTypes[t] = typeName;
                                }
                            }
                        }
                        foreach (APIExtensionAttribute attr in apiExtensionAttrs)
                        {
                            string typeName = apiDisplayNameAttr.DisplayName;
                            if (string.IsNullOrEmpty(typeName))
                            {
                                typeName = t.Name;
                            }

                            ApiInfo apiInfo;
                            string extensionName = attr.Extension.ToLower();
                            if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                            {
                                apiInfo = new ApiInfo();
                                m_ApiExtensions.Add(extensionName, apiInfo);
                            }
                            apiInfo.Types.Add(typeName, t);
                            m_ValidTypes[t] = typeName;
                            KnownSerializationTypes[t.FullName] = t;
                        }
                    }
                    else
                    {
                        m_Log.DebugFormat("Type {0} does not have APIDisplayName attribute", t.Name);
                    }
                }
            }
        }

        private void CollectApiConstants(IScriptApi api)
        {
            foreach (FieldInfo f in api.GetType().GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if ((f.Attributes & FieldAttributes.Static) != 0 &&
                    ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0))
                {
                    if (IsValidType(f.FieldType))
                    {
                        var apiLevelAttrs = Attribute.GetCustomAttributes(f, typeof(APILevelAttribute)) as APILevelAttribute[];
                        var apiExtensionAttrs = Attribute.GetCustomAttributes(f, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                        if (apiLevelAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                        {
                            foreach (APILevelAttribute attr in apiLevelAttrs)
                            {
                                string constName = attr.Name;
                                if (string.IsNullOrEmpty(constName))
                                {
                                    constName = f.Name;
                                }
                                foreach (KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                                {
                                    if ((kvp.Key & attr.Flags) != 0)
                                    {
                                        kvp.Value.Constants.Add(constName, f);
                                    }
                                }
                            }
                            foreach (APIExtensionAttribute attr in apiExtensionAttrs)
                            {
                                string constName = attr.Name;
                                if (string.IsNullOrEmpty(constName))
                                {
                                    constName = f.Name;
                                }

                                ApiInfo apiInfo;
                                string extensionName = attr.Extension.ToLower();
                                if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                                {
                                    apiInfo = new ApiInfo();
                                    m_ApiExtensions.Add(extensionName, apiInfo);
                                }
                                apiInfo.Constants.Add(constName, f);
                            }
                        }
                    }
                    else
                    {
                        var apiLevelAttrs = Attribute.GetCustomAttributes(f, typeof(APILevelAttribute)) as APILevelAttribute[];
                        var apiExtensionAttrs = Attribute.GetCustomAttributes(f, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                        if (apiLevelAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                        {
                            m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                        }
                    }
                }
            }
        }

        private void CollectApiEvents(IScriptApi api)
        {
            foreach (Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
            {
                var stateEventAttr = (StateEventDelegateAttribute)Attribute.GetCustomAttribute(t, typeof(StateEventDelegateAttribute));
                if (stateEventAttr != null)
                {
                    var apiLevelAttrs = Attribute.GetCustomAttributes(t, typeof(APILevelAttribute)) as APILevelAttribute[];
                    var apiExtensionAttrs = Attribute.GetCustomAttributes(t, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                    MethodInfo mi = t.GetMethod("Invoke");
                    if (apiLevelAttrs.Length == 0 && apiExtensionAttrs.Length == 0)
                    {
                        m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has StateEventDelegate attribute. APILevel or APIExtension attribute missing.",
                            mi.Name,
                            mi.DeclaringType.FullName);
                    }

                    /* validate parameters */
                    ParameterInfo[] pi = mi.GetParameters();
                    bool eventValid = true;

                    for (int i = 0; i < pi.Length; ++i)
                    {
                        if (!IsValidType(pi[i].ParameterType))
                        {
                            eventValid = false;
                            m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. Parameter '{2}' does not have LSL compatible type '{3}'.",
                                mi.Name,
                                mi.DeclaringType.FullName,
                                pi[i].Name,
                                pi[i].ParameterType.FullName);
                        }
                    }
                    if (mi.ReturnType != typeof(void))
                    {
                        eventValid = false;
                        m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. Return value is not void. Found: '{2}'",
                            mi.Name,
                            mi.DeclaringType.FullName,
                            mi.ReturnType.FullName);
                    }

                    if (eventValid)
                    {
                        foreach (APILevelAttribute apiLevelAttr in apiLevelAttrs)
                        {
                            string funcName = apiLevelAttr.Name;
                            if (string.IsNullOrEmpty(funcName))
                            {
                                funcName = mi.Name;
                            }

                            foreach (KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                            {
                                if ((kvp.Key & apiLevelAttr.Flags) != 0)
                                {
                                    kvp.Value.EventDelegates.Add(funcName, mi);
                                }
                            }
                        }
                        foreach (APIExtensionAttribute apiExtensionAttr in apiExtensionAttrs)
                        {
                            string funcName = apiExtensionAttr.Name;
                            if (string.IsNullOrEmpty(funcName))
                            {
                                funcName = mi.Name;
                            }

                            ApiInfo apiInfo;
                            string extensionName = apiExtensionAttr.Extension.ToLower();
                            if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                            {
                                apiInfo = new ApiInfo();
                                m_ApiExtensions.Add(extensionName, apiInfo);
                            }
                            apiInfo.EventDelegates.Add(funcName, mi);
                        }
                    }
                }
                else if (stateEventAttr != null)
                {
                    MethodInfo mi = t.GetMethod("Invoke");
                    m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. APILevel attribute missing.",
                        mi.Name,
                        mi.DeclaringType.FullName);
                }
            }
        }

        private void CollectApiAddMethod(ApiInfo apiInfo, IAPIDeclaration funcNameAttr, IScriptApi api, MethodInfo m)
        {
            string funcName = funcNameAttr.Name;
            if (string.IsNullOrEmpty(funcName))
            {
                funcName = m.Name;
            }

            switch (funcNameAttr.UseAs)
            {
                case APIUseAsEnum.Function:
                    List<ApiMethodInfo> methodList;
                    if (!apiInfo.Methods.TryGetValue(funcName, out methodList))
                    {
                        methodList = new List<ApiMethodInfo>();
                        apiInfo.Methods.Add(funcName, methodList);
                    }
                    methodList.Add(new ApiMethodInfo(funcName, api, m));
                    break;

                case APIUseAsEnum.Getter:
                    if (m.GetParameters().Length != 1 || m.ReturnType == typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Function is not a valid getter.",
                            m.Name,
                            m.DeclaringType.FullName,
                            m.ReturnType.FullName);
                    }
                    else
                    {
                        ApiInfo.PropertyInfo prop;
                        var apiMethod = new ApiMethodInfo(funcName, api, m);
                        if (!apiInfo.Properties.TryGetValue(funcName, out prop))
                        {
                            apiInfo.Properties[funcName] = new ApiInfo.PropertyInfo(m.ReturnType)
                            {
                                Getter = apiMethod
                            };
                        }
                        else if (prop.PropertyType != m.ReturnType)
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Getter return type mismatch.",
                                m.Name,
                                m.DeclaringType.FullName,
                                m.ReturnType.FullName);
                        }
                        else
                        {
                            prop.Getter = apiMethod;
                        }
                    }
                    break;

                case APIUseAsEnum.Setter:
                    if (m.GetParameters().Length != 2 || m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Function is not a valid setter.",
                            m.Name,
                            m.DeclaringType.FullName,
                            m.ReturnType.FullName);
                    }
                    else
                    {
                        ApiInfo.PropertyInfo prop;
                        var apiMethod = new ApiMethodInfo(funcName, api, m);
                        ParameterInfo param1 = m.GetParameters()[1];
                        if (!apiInfo.Properties.TryGetValue(funcName, out prop))
                        {
                            apiInfo.Properties[funcName] = new ApiInfo.PropertyInfo(param1.ParameterType)
                            {
                                Setter = apiMethod
                            };
                        }
                        else if (prop.PropertyType != param1.ParameterType)
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension  attribute. Setter parameter type mismatch.",
                                m.Name,
                                m.DeclaringType.FullName,
                                m.ReturnType.FullName);
                        }
                        else
                        {
                            prop.Getter = apiMethod;
                        }
                    }
                    break;
            }
        }

        private void CollectApiFunctionsAndDelegates(IScriptApi api)
        {
            foreach (MethodInfo m in api.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                var funcNameAttrs = Attribute.GetCustomAttributes(m, typeof(APILevelAttribute)) as APILevelAttribute[];
                var apiExtensionAttrs = Attribute.GetCustomAttributes(m, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                if (funcNameAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length >= 1 &&
                        pi[0].ParameterType.Equals(typeof(ScriptInstance)))
                    {
                        /* validate parameters */
                        bool methodValid = true;
                        if ((m.Attributes & MethodAttributes.Static) != 0)
                        {
                            methodValid = false;
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Method is declared static.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        for (int i = 1; i < pi.Length; ++i)
                        {
                            if (!IsValidType(pi[i].ParameterType))
                            {
                                methodValid = false;
                                m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Parameter '{2}' does not have LSL compatible type '{3}'.",
                                    m.Name,
                                    m.DeclaringType.FullName,
                                    pi[i].Name,
                                    pi[i].ParameterType.FullName);
                            }
                        }
                        if (!IsValidType(m.ReturnType))
                        {
                            methodValid = false;
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel or APIExtension attribute. Return value does not have LSL compatible type '{2}'.",
                                m.Name,
                                m.DeclaringType.FullName,
                                m.ReturnType.FullName);
                        }

                        if (methodValid)
                        {
                            foreach (APILevelAttribute funcNameAttr in funcNameAttrs)
                            {
                                foreach(KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                                {
                                    if((kvp.Key & funcNameAttr.Flags) != 0)
                                    {
                                        CollectApiAddMethod(kvp.Value, funcNameAttr, api, m);
                                    }
                                }
                            }
                            foreach (APIExtensionAttribute funcNameAttr in apiExtensionAttrs)
                            {
                                ApiInfo apiInfo;
                                string extensionName = funcNameAttr.Extension.ToLower();
                                if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                                {
                                    apiInfo = new ApiInfo();
                                    m_ApiExtensions.Add(extensionName, apiInfo);
                                }

                                CollectApiAddMethod(apiInfo, funcNameAttr, api, m);
                            }
                        }
                    }
                }

                Attribute attr = Attribute.GetCustomAttribute(m, typeof(ExecutedOnStateChangeAttribute));
                if (attr != null)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length != 1)
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Parameter count does not match.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Return type is not void.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (pi[0].ParameterType != typeof(ScriptInstance))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Parameter type is not ScriptInstance.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else
                    {
                        m_StateChangeDelegates.Add((Action<ScriptInstance>)Delegate.CreateDelegate(typeof(Action<ScriptInstance>), (m.Attributes & MethodAttributes.Static) != 0 ? null : api, m));
                    }
                }

                attr = Attribute.GetCustomAttribute(m, typeof(ExecutedOnScriptResetAttribute));
                if (attr != null)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length != 1)
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Parameter count does not match.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Return type is not void.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (pi[0].ParameterType != typeof(ScriptInstance))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Parameter type is not ScriptInstance.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else
                    {
                        m_ScriptResetDelegates.Add((Action<ScriptInstance>)Delegate.CreateDelegate(typeof(Action<ScriptInstance>), (m.Attributes & MethodAttributes.Static) != 0 ? null : api, m));
                    }
                }

                attr = Attribute.GetCustomAttribute(m, typeof(ExecutedOnScriptRemoveAttribute));
                if (attr != null)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length != 1)
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptRemove. Parameter count does not match.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptRemove. Return type is not void.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (pi[0].ParameterType != typeof(ScriptInstance))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptRemove. Parameter type is not ScriptInstance.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else
                    {
                        m_ScriptRemoveDelegates.Add(
                            (Action<ScriptInstance>)Delegate.CreateDelegate(
                                typeof(Action<ScriptInstance>),
                                (m.Attributes & MethodAttributes.Static) != 0 ? null : api,
                                m));
                    }
                }

                attr = Attribute.GetCustomAttribute(m, typeof(ExecutedOnSerializationAttribute));
                if (attr != null)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length != 2)
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnSerialization. Parameter count does not match.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnSerialization. Return type is not void.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (pi[0].ParameterType != typeof(ScriptInstance) ||
                        pi[1].ParameterType != typeof(List<object>))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnSerialization. Wrong parameter types.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else
                    {
                        m_ScriptSerializeDelegates.Add(
                            (Action<ScriptInstance, List<object>>)Delegate.CreateDelegate(
                                typeof(Action<ScriptInstance, List<object>>),
                                (m.Attributes & MethodAttributes.Static) != 0 ? null : api,
                                m));
                    }
                }

                var deserializeattr = Attribute.GetCustomAttribute(m, typeof(ExecutedOnDeserializationAttribute)) as ExecutedOnDeserializationAttribute;
                if (deserializeattr != null)
                {
                    ParameterInfo[] pi = m.GetParameters();
                    if (pi.Length != 2)
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnDeserialization. Parameter count does not match.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (m.ReturnType != typeof(void))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnDeserialization. Return type is not void.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else if (pi[0].ParameterType != typeof(ScriptInstance) ||
                        pi[1].ParameterType != typeof(List<object>))
                    {
                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnDeserialization. Wrong parameter types.",
                            m.Name,
                            m.DeclaringType.FullName);
                    }
                    else
                    {
                        m_ScriptDeserializeDelegates.Add(deserializeattr.Name,
                            (Action<ScriptInstance, List<object>>)Delegate.CreateDelegate(
                                typeof(Action<ScriptInstance, List<object>>),
                                (m.Attributes & MethodAttributes.Static) != 0 ? null : api,
                                m));
                    }
                }
            }
        }

        private void CollectApiEventTranslations(IScriptApi api)
        {
            foreach (FieldInfo f in api.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                var listattr = Attribute.GetCustomAttribute(f, typeof(TranslatedScriptEventsInfoAttribute)) as TranslatedScriptEventsInfoAttribute;
                if (listattr == null ||
                    f.FieldType != typeof(Type[]))
                {
                    continue;
                }
                Type[] typeList = (f.Attributes & FieldAttributes.Static) != 0 ?
                    (Type[])f.GetValue(null) :
                    (Type[])f.GetValue(api);
                foreach (Type evt in typeList)
                {
                    var eventAttr = Attribute.GetCustomAttribute(evt, typeof(TranslatedScriptEventAttribute)) as TranslatedScriptEventAttribute;
                    if (eventAttr != null && evt.GetInterfaces().Contains(typeof(IScriptEvent)))
                    {
                        /* translatable parameters */
                        var parameters = new SortedDictionary<int, object>();
                        bool notUsable = false;

                        foreach(FieldInfo fi in evt.GetFields(BindingFlags.Instance | BindingFlags.Public))
                        {
                            var paramAttr = Attribute.GetCustomAttribute(fi, typeof(TranslatedScriptEventParameterAttribute)) as TranslatedScriptEventParameterAttribute;

                            if (paramAttr != null)
                            {
                                if (parameters.ContainsKey(paramAttr.ParameterNumber))
                                {
                                    m_Log.DebugFormat("Invalid ScriptEvent type {0} encountered with duplicate parameter definitions {1} for field {2}",
                                        evt.FullName,
                                        paramAttr.ParameterNumber,
                                        fi.Name);
                                    notUsable = true;
                                    break;
                                }
                                else if (!IsValidType(fi.FieldType))
                                {
                                    m_Log.DebugFormat("Invalid ScriptEvent type {0} encountered field {1} having unsupported type {2}",
                                        evt.FullName,
                                        fi.Name,
                                        fi.FieldType.FullName);
                                    notUsable = true;
                                    break;
                                }
                                parameters.Add(paramAttr.ParameterNumber, fi);
                            }
                        }

                        foreach(PropertyInfo pi in evt.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            var paramAttr = Attribute.GetCustomAttribute(pi, typeof(TranslatedScriptEventParameterAttribute)) as TranslatedScriptEventParameterAttribute;

                            if (paramAttr != null)
                            {
                                if (parameters.ContainsKey(paramAttr.ParameterNumber))
                                {
                                    m_Log.DebugFormat("Invalid ScriptEvent type {0} encountered with duplicate parameter definitions {1} for property {2}",
                                        evt.FullName,
                                        paramAttr.ParameterNumber,
                                        pi.Name);
                                    notUsable = true;
                                    break;
                                }
                                else if(!IsValidType(pi.PropertyType))
                                {
                                    m_Log.DebugFormat("Invalid ScriptEvent type {0} encountered property {1} having unsupported type {2}",
                                        evt.FullName,
                                        pi.Name,
                                        pi.PropertyType.FullName);
                                    notUsable = true;
                                    break;
                                }
                                parameters.Add(paramAttr.ParameterNumber, pi);
                            }
                        }

                        int paramcount = 0;
                        foreach(int key in parameters.Keys)
                        {
                            if(key != paramcount)
                            {
                                m_Log.DebugFormat("Invalid ScriptEvent type {0} encountered with duplicate parameter number definition is not valid (missing {1})",
                                    evt.FullName,
                                    paramcount);
                                notUsable = true;
                                break;
                            }
                            ++paramcount;
                        }

                        if(notUsable)
                        {
                            continue;
                        }

                        var dynMethod = new DynamicMethod("Translate_" + eventAttr.EventName,
                            typeof(void),
                            new Type[2] { typeof(Script), typeof(IScriptEvent) },
                            typeof(Script).Module);
                        ILGenerator ilgen = dynMethod.GetILGenerator();

                        /* cast IScriptEvent to actual type */
                        ilgen.Emit(OpCodes.Ldarg_1);
                        ilgen.Emit(OpCodes.Castclass, evt);
                        LocalBuilder eventlb = ilgen.DeclareLocal(evt);
                        ilgen.Emit(OpCodes.Stloc, eventlb);

                        /* create object[] array */
                        LocalBuilder lb = ilgen.DeclareLocal(typeof(object[]));
                        ilgen.Emit(OpCodes.Ldc_I4, paramcount);
                        ilgen.Emit(OpCodes.Newarr, typeof(object));
                        ilgen.Emit(OpCodes.Stloc, lb);

                        /* collect parameters into object[] array */
                        foreach(KeyValuePair<int, object> kvp in parameters)
                        {
                            ilgen.Emit(OpCodes.Ldloc, lb);
                            ilgen.Emit(OpCodes.Ldc_I4, kvp.Key);
                            Type retType = null;
                            FieldInfo fi;
                            PropertyInfo pi;

                            if ((fi = kvp.Value as FieldInfo) != null)
                            {
                                ilgen.Emit(OpCodes.Ldloc, eventlb);
                                ilgen.Emit(OpCodes.Ldfld, fi);
                                retType = fi.FieldType;
                            }
                            else if((pi = kvp.Value as PropertyInfo) != null)
                            {
                                ilgen.Emit(OpCodes.Ldloc, eventlb);
                                ilgen.Emit(OpCodes.Call, pi.GetGetMethod());
                                retType = pi.PropertyType;
                            }
                            else
                            {
                                continue;
                            }

                            if (!retType.IsByRef)
                            {
                                ilgen.Emit(OpCodes.Box, retType);
                            }
                            ilgen.Emit(OpCodes.Stelem_Ref);
                        }

                        /* parameters for function */
                        ilgen.Emit(OpCodes.Ldarg_0);
                        ilgen.Emit(OpCodes.Ldstr, eventAttr.EventName);
                        ilgen.Emit(OpCodes.Ldloc, lb);

                        ilgen.Emit(OpCodes.Call, typeof(Script).GetMethod("InvokeStateEvent", new Type[] { typeof(Script), typeof(string), typeof(object[]) }));
                        ilgen.Emit(OpCodes.Ret);

                        Script.StateEventHandlers.Add(evt, dynMethod.CreateDelegate(typeof(Action<Script, IScriptEvent>)) as Action<Script, IScriptEvent>);
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            CollectApis(loader);

            #region API Collection
            foreach (IScriptApi api in m_Apis)
            {
                CollectApiTypes(api);
            }

            m_ValidTypes[typeof(int)] = "integer";
            m_ValidTypes[typeof(long)] = "long";
            m_ValidTypes[typeof(double)] = "float";
            m_ValidTypes[typeof(string)] = "string";
            m_ValidTypes[typeof(Quaternion)] = "rotation";
            m_ValidTypes[typeof(Vector3)] = "vector";
            m_ValidTypes[typeof(LSLKey)] = "key";
            m_ValidTypes[typeof(AnArray)] = "list";
            m_ValidTypes[typeof(void)] = "void";

            CollectApiTypecasts();

            foreach (IScriptApi api in m_Apis)
            {
                CollectApiConstants(api);
                CollectApiEvents(api);
                CollectApiEventTranslations(api);
                CollectApiFunctionsAndDelegates(api);
            }
            #endregion

            GenerateLSLSyntaxFile();

            CompilerRegistry.ScriptCompilers["lsl"] = this;
            CompilerRegistry.ScriptCompilers["XEngine"] = this; /* we won't be supporting anything beyond LSL compatibility */
        }

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null)
        {
            Preprocess(shbangs, reader, linenumber, cultureInfo);
        }

        private void WriteIndent(TextWriter writer, int indent)
        {
            while(indent-- > 0)
            {
                writer.Write("    ");
            }
        }

        private void WriteIndented(TextWriter writer, string s, ref int oldIndent)
        {
            if (s == "[")
            {
                writer.WriteLine("\n");
                ++oldIndent;
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                WriteIndent(writer, oldIndent);
            }
            else if (s == "{")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                ++oldIndent;
                WriteIndent(writer, oldIndent);
            }
            else if (s == "]")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                --oldIndent;
                WriteIndent(writer, oldIndent);
            }
            else if ( s == "}")
            {
                --oldIndent;
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
                writer.WriteLine(s);
                WriteIndent(writer, oldIndent);
            }
            else if(s == "\n")
            {
                writer.WriteLine("\n");
                WriteIndent(writer, oldIndent);
            }
            else if(s == ";")
            {
                writer.WriteLine(";");
                WriteIndent(writer, oldIndent);
            }
            else
            {
                writer.Write(s + " ");
            }
        }

        private void WriteIndented(TextWriter writer, List<string> list, ref int oldIndent)
        {
            foreach(string s in list)
            {
                WriteIndented(writer, s, ref oldIndent);
            }
        }

        public void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null)
        {
            CompileState cs = Preprocess(shbangs, reader, linenumber, cultureInfo);
            /* rewrite script */
            int indent = 0;
            using (TextWriter writer = new StreamWriter(s, new UTF8Encoding(false)))
            {
                #region Write Variables
                foreach (KeyValuePair<string, Type> kvp in cs.m_VariableDeclarations)
                {
                    LineInfo li;
                    WriteIndented(writer, cs.MapType(kvp.Value), ref indent);
                    WriteIndented(writer, kvp.Key, ref indent);
                    if (cs.m_VariableInitValues.TryGetValue(kvp.Key, out li))
                    {
                        WriteIndented(writer, "=", ref indent);
                        WriteIndented(writer, li.Line, ref indent);
                    }
                    WriteIndented(writer, ";", ref indent);
                }
                WriteIndented(writer, "\n", ref indent);
                #endregion

                #region Write functions
                foreach(KeyValuePair<string, List<FunctionInfo>> kvp in cs.m_Functions)
                {
                    foreach (FunctionInfo funcInfo in kvp.Value)
                    {
                        foreach (LineInfo li in funcInfo.FunctionLines)
                        {
                            WriteIndented(writer, li.Line, ref indent);
                            if (li.Line[li.Line.Count - 1] != "{" && li.Line[li.Line.Count - 1] != ";" && li.Line[li.Line.Count - 1] != "}")
                            {
                                ++indent;
                                WriteIndented(writer, "\n", ref indent);
                                --indent;
                            }
                        }
                    }
                }
                #endregion

                #region Write states
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> kvp in cs.m_States)
                {
                    if (kvp.Key != "default")
                    {
                        WriteIndented(writer, "state", ref indent);
                    }
                    WriteIndented(writer, kvp.Key, ref indent);
                    WriteIndented(writer, "{", ref indent);

                    foreach (KeyValuePair<string, List<LineInfo>> eventfn in kvp.Value)
                    {
                        int tempindent = 0;
                        foreach (LineInfo li in eventfn.Value)
                        {
                            WriteIndented(writer, li.Line, ref indent);
                            if (li.Line[li.Line.Count - 1] != "{" && li.Line[li.Line.Count - 1] != ";" && li.Line[li.Line.Count - 1] != "}")
                            {
                                ++tempindent;
                                indent += tempindent;
                                WriteIndented(writer, "\n", ref indent);
                                indent -= tempindent;
                            }
                            else
                            {
                                tempindent = 0;
                            }
                        }
                    }
                    WriteIndented(writer, "}", ref indent);
                }
                #endregion
                writer.Flush();
            }
        }

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1, CultureInfo cultureInfo = null)
        {
            CompileState compileState = Preprocess(shbangs, reader, lineNumber, cultureInfo);
            return PostProcess(compileState, appDom, assetID, compileState.ForcedSleepDefault, AssemblyBuilderAccess.RunAndCollect);
        }

        public void CompileToDisk(string filename, AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1, CultureInfo cultureInfo = null)
        {
            CompileToDisk(filename, appDom, user, shbangs, assetID, reader, false, lineNumber, cultureInfo);
        }

        public void CompileToDisk(string filename, AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, bool emitDebugSymbols, int lineNumber = 1, CultureInfo cultureInfo = null)
        {
            CompileState compileState = Preprocess(shbangs, reader, lineNumber, cultureInfo);
            compileState.EmitDebugSymbols = emitDebugSymbols;
            var scriptAssembly = (LSLScriptAssembly)PostProcess(compileState, appDom, assetID, compileState.ForcedSleepDefault, AssemblyBuilderAccess.RunAndSave, filename);
            if(scriptAssembly == null)
            {
                throw new CompilerException();
            }
            var builder = (AssemblyBuilder)scriptAssembly.Assembly;
            if(builder == null)
            {
                throw new CompilerException();
            }
            builder.Save(filename);
        }
    }
}
