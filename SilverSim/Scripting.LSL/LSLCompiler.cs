// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Scripting.LSL.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    [CompilerUsesRunAndCollectMode]
    public partial class LSLCompiler : IScriptCompiler, IPlugin, IPluginSubFactory
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

        internal class ApiInfo
        {
            public Dictionary<string, List<ApiMethodInfo>> Methods = new Dictionary<string, List<ApiMethodInfo>>();
            public Dictionary<string, FieldInfo> Constants = new Dictionary<string, FieldInfo>();
            public Dictionary<string, MethodInfo> EventDelegates = new Dictionary<string, MethodInfo>();

            public ApiInfo()
            {

            }

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
            }
        }

        private static readonly ILog m_Log = LogManager.GetLogger("LSL COMPILER");
        internal List<IScriptApi> m_Apis = new List<IScriptApi>();
        //Dictionary<string, APIFlags> m_Constants = new Dictionary<string, APIFlags>();

        //internal Dictionary<string, List<KeyValuePair<IScriptApi, MethodInfo>>> m_Methods = new Dictionary<string, List<KeyValuePair<IScriptApi, MethodInfo>>>();

        internal Dictionary<APIFlags, ApiInfo> m_ApiInfos = new Dictionary<APIFlags, ApiInfo>();
        internal Dictionary<string, ApiInfo> m_ApiExtensions = new Dictionary<string, ApiInfo>();

        //Dictionary<string, MethodInfo> m_EventDelegates = new Dictionary<string, MethodInfo>();
        List<Script.StateChangeEventDelegate> m_StateChangeDelegates = new List<ScriptInstance.StateChangeEventDelegate>();
        List<Script.ScriptResetEventDelegate> m_ScriptResetDelegates = new List<ScriptInstance.ScriptResetEventDelegate>();
        List<string> m_ReservedWords = new List<string>();
        //internal Dictionary<string, APIFlags> m_MethodNames = new Dictionary<string, APIFlags>();
        List<char> m_SingleOps = new List<char>();
        List<char> m_MultiOps = new List<char>();
        List<char> m_NumericChars = new List<char>();
        List<char> m_OpChars = new List<char>();
        Resolver m_Resolver;

        sealed class LineInfo
        {
            public readonly List<string> Line;
            public readonly int LineNumber;

            public LineInfo(List<string> line, int lineNo)
            {
                Line = line;
                LineNumber = lineNo;
            }
        }

        enum ControlFlowType
        {
            Entry,
            UnconditionalBlock,
            If,
            Else,
            ElseIf,
            For,
            DoWhile,
            While
        }

        sealed class ControlFlowElement
        {
            public bool IsExplicitBlock;
            public bool PopNextImplicit;
            public ControlFlowType Type;
            public Label? LoopLabel;
            public Label? EndOfControlFlowLabel;
            public Label? EndOfIfFlowLabel;
            //public bool EndOfIfLabelDefined;

            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, Label eoiflabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label? looplabel, Label eofclabel, Label eoiflabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
            }
        }

        sealed class CompileState
        {
            public ApiInfo ApiInfo = new ApiInfo();
            public bool ForcedSleepDefault;
            public bool EmitDebugSymbols;
            public Dictionary<string, MethodBuilder> m_FunctionInfo = new Dictionary<string, MethodBuilder>();
            public Dictionary<string, KeyValuePair<Type, KeyValuePair<string, Type>[]>> m_FunctionSignature = new Dictionary<string, KeyValuePair<Type, KeyValuePair<string, Type>[]>>();
            public Dictionary<string, Type> m_VariableDeclarations = new Dictionary<string, Type>();
            public Dictionary<string, FieldBuilder> m_VariableFieldInfo = new Dictionary<string, FieldBuilder>();
            public Dictionary<string, LineInfo> m_VariableInitValues = new Dictionary<string, LineInfo>();
            public List<List<string>> m_LocalVariables = new List<List<string>>();
            public Dictionary<string, List<LineInfo>> m_Functions = new Dictionary<string, List<LineInfo>>();
            public Dictionary<string, Dictionary<string, List<LineInfo>>> m_States = new Dictionary<string, Dictionary<string, List<LineInfo>>>();
            public FieldBuilder InstanceField;
            public Dictionary<string, FieldBuilder> m_ApiFieldInfo = new Dictionary<string, FieldBuilder>();
            List<ControlFlowElement> m_ControlFlowStack = new List<ControlFlowElement>();
            public Dictionary<Label, KeyValuePair<int, string>> m_UnnamedLabels = new Dictionary<Label, KeyValuePair<int, string>>();
            public ControlFlowElement LastBlock;

            public void InitControlFlow()
            {
                m_ControlFlowStack.Clear();
                m_UnnamedLabels.Clear();
                LastBlock = null;
                PushControlFlow(new ControlFlowElement(ControlFlowType.Entry, true));
            }

            public void FinishControlFlowChecks()
            {
                Dictionary<int, string> messages = new Dictionary<int, string>();
                foreach(KeyValuePair<int, string> kvp in m_UnnamedLabels.Values)
                {
                    messages[kvp.Key] = string.Format("Internal Error! Undefined local label for {0}", kvp.Value);
                }
                if (messages.Count != 0)
                {
                    throw new CompilerException(messages);
                }
            }

            public void PushControlFlow(ControlFlowElement e)
            {
                m_ControlFlowStack.Insert(0, e);
                LastBlock = null;
            }

            public string GetControlFlowInfo(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                switch(m_ControlFlowStack[0].Type)
                {
                    case ControlFlowType.Entry: return "function entry";
                    case ControlFlowType.If: return "if";
                    case ControlFlowType.Else: return "else";
                    case ControlFlowType.ElseIf: return "else if";
                    case ControlFlowType.For: return "for";
                    case ControlFlowType.DoWhile: return "do ... while";
                    case ControlFlowType.While: return "while";
                    default: throw new ArgumentException(m_ControlFlowStack[0].Type.ToString());
                }
            }

            public bool IsImplicitControlFlow(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                return !m_ControlFlowStack[0].IsExplicitBlock;
            }

            public void PopControlFlowImplicit(ILGenerator ilgen, int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ilgen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                if(m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else if(!m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if(null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (elem.Type == ControlFlowType.Else)
                        {
                            if(!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                            }
                            ilgen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                        }
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if(!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }
            }

            public void PopControlFlowImplicits(ILGenerator ilgen, int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    if(!m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value))
                    {
                        throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                    }
                    ilgen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else while (!m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (elem.Type == ControlFlowType.Else)
                        {
                            if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                            }
                            ilgen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                        }
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if(!m_UnnamedLabels.Remove(LastBlock.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", LastBlock.Type.ToString()));
                        }
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }
            }

            public ControlFlowElement PopControlFlowExplicit(ILGenerator ilgen, int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ilgen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                while (m_ControlFlowStack.Count != 0 && !m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                        }
                        ilgen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if(!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (elem.Type == ControlFlowType.Else)
                        {
                            if(!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                            }
                            ilgen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                        }
                        else
                        {
                            ilgen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                        }
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if(!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ilgen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                        elem.EndOfControlFlowLabel = null;
                    }
                    return elem;
                }
            }

            public CompileState()
            {

            }
        }

        public LSLCompiler()
        {
            m_ApiInfos.Add(APIFlags.ASSL, new ApiInfo());
            m_ApiInfos.Add(APIFlags.LSL, new ApiInfo());
            m_ApiInfos.Add(APIFlags.OSSL, new ApiInfo());

            m_ReservedWords.Add("integer");
            m_ReservedWords.Add("vector");
            m_ReservedWords.Add("list");
            m_ReservedWords.Add("float");
            m_ReservedWords.Add("string");
            m_ReservedWords.Add("key");
            m_ReservedWords.Add("rotation");
            m_ReservedWords.Add("if");
            m_ReservedWords.Add("while");
            m_ReservedWords.Add("jump");
            m_ReservedWords.Add("for");
            m_ReservedWords.Add("do");
            m_ReservedWords.Add("return");
            m_ReservedWords.Add("state");
            m_ReservedWords.Add("void");
            m_ReservedWords.Add("quaternion");

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
            m_SingleOps.Add('(');
            m_SingleOps.Add(')');
            m_SingleOps.Add('[');
            m_SingleOps.Add(']');
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
            loader.AddPlugin("LSLHttpClient", new LSLHTTPClient_RequestQueue());
            Type[] types = GetType().Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (typeof(IScriptApi).IsAssignableFrom(type))
                {
                    System.Attribute scriptApiAttr = System.Attribute.GetCustomAttribute(type, typeof(ScriptApiName));
                    System.Attribute impTagAttr = System.Attribute.GetCustomAttribute(type, typeof(LSLImplementation));
                    if (null != impTagAttr && null != scriptApiAttr)
                    {
                        IPlugin factory = (IPlugin)Activator.CreateInstance(type);
                        loader.AddPlugin("LSL_API_" + ((ScriptApiName)scriptApiAttr).Name, factory);
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            List<IScriptApi> apis = loader.GetServicesByValue<IScriptApi>();
            foreach (IScriptApi api in apis)
            {
                System.Attribute attr = System.Attribute.GetCustomAttribute(api.GetType(), typeof(LSLImplementation));
                if(attr != null && !m_Apis.Contains(api))
                {
                    m_Apis.Add(api);
                }
            }

            foreach (IScriptApi api in apis)
            {
                #region Collect constants
                foreach (FieldInfo f in api.GetType().GetFields())
                {
                    if ((f.Attributes & FieldAttributes.Static) != 0)
                    {
                        if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                        {
                            if (IsValidType(f.FieldType))
                            {
                                APILevel[] apiLevelAttrs = System.Attribute.GetCustomAttributes(f, typeof(APILevel)) as APILevel[];
                                APIExtension[] apiExtensionAttrs = System.Attribute.GetCustomAttributes(f, typeof(APIExtension)) as APIExtension[];
                                if(apiLevelAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                                {
                                    foreach(APILevel attr in apiLevelAttrs)
                                    {
                                        string constName = attr.Name;
                                        if(string.IsNullOrEmpty(constName))
                                        {
                                            constName = f.Name;
                                        }
                                        foreach(KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                                        {
                                            if((kvp.Key & attr.Flags) != 0)
                                            {
                                                kvp.Value.Constants.Add(constName, f);
                                            }
                                        }
                                    }
                                    foreach(APIExtension attr in apiExtensionAttrs)
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
                                m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                            }
                        }
                    }
                }
                #endregion

                #region Collect event definitions
                foreach (Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
                {
                    StateEventDelegate stateEventAttr = (StateEventDelegate)System.Attribute.GetCustomAttribute(t, typeof(StateEventDelegate));
                    if (stateEventAttr != null)
                    {
                        APILevel[] apiLevelAttrs = System.Attribute.GetCustomAttributes(t, typeof(APILevel)) as APILevel[];
                        APIExtension[] apiExtensionAttrs = System.Attribute.GetCustomAttributes(t, typeof(APIExtension)) as APIExtension[];
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
                            foreach (APILevel apiLevelAttr in apiLevelAttrs)
                            {
                                string funcName = apiLevelAttr.Name;
                                if(string.IsNullOrEmpty(funcName))
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
                            foreach(APIExtension apiExtensionAttr in apiExtensionAttrs)
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
                    else if (null != stateEventAttr)
                    {
                        MethodInfo mi = t.GetMethod("Invoke");
                        m_Log.DebugFormat("Invalid delegate '{0}' in '{1}' has APILevel attribute. APILevel attribute missing.",
                            mi.Name,
                            mi.DeclaringType.FullName);
                    }
                }
                #endregion

                #region Collect API functions, reset delegates and state change delegates
                foreach (MethodInfo m in api.GetType().GetMethods())
                {
                    APILevel[] funcNameAttrs = System.Attribute.GetCustomAttributes(m, typeof(APILevel)) as APILevel[];
                    APIExtension[] apiExtensionAttrs = System.Attribute.GetCustomAttributes(m, typeof(APIExtension)) as APIExtension[];
                    if (funcNameAttrs.Length != 0 || apiExtensionAttrs.Length != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if (pi.Length >= 1)
                        {
                            if (pi[0].ParameterType.Equals(typeof(ScriptInstance)))
                            {
                                /* validate parameters */
                                bool methodValid = true;
                                if((m.Attributes & MethodAttributes.Static) != 0)
                                {
                                    methodValid = false;
                                    m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Method is declared static.",
                                        m.Name,
                                        m.DeclaringType.FullName);
                                }
                                for (int i = 1; i < pi.Length; ++i)
                                {
                                    if (!IsValidType(pi[i].ParameterType))
                                    {
                                        methodValid = false;
                                        m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Parameter '{2}' does not have LSL compatible type '{3}'.",
                                            m.Name,
                                            m.DeclaringType.FullName,
                                            pi[i].Name,
                                            pi[i].ParameterType.FullName);
                                    }
                                }
                                if (!IsValidType(m.ReturnType))
                                {
                                    methodValid = false;
                                    m_Log.DebugFormat("Invalid method '{0}' in '{1}' has APILevel attribute. Return value does not have LSL compatible type '{2}'.",
                                        m.Name,
                                        m.DeclaringType.FullName,
                                        m.ReturnType.FullName);
                                }

                                if (methodValid)
                                {
                                    foreach (APILevel funcNameAttr in funcNameAttrs)
                                    {
                                        string funcName = funcNameAttr.Name;
                                        if(string.IsNullOrEmpty(funcName))
                                        {
                                            funcName = m.Name;
                                        }
                                        foreach (KeyValuePair<APIFlags, ApiInfo> kvp in m_ApiInfos)
                                        {
                                            List<ApiMethodInfo> methodList;
                                            if (!kvp.Value.Methods.TryGetValue(funcName, out methodList))
                                            {
                                                kvp.Value.Methods.Add(funcName, methodList = new List<ApiMethodInfo>());
                                            }
                                            methodList.Add(new ApiMethodInfo(funcName, api, m));
                                        }
                                    }
                                    foreach(APIExtension funcNameAttr in apiExtensionAttrs)
                                    {
                                        string funcName = funcNameAttr.Name;
                                        if (string.IsNullOrEmpty(funcName))
                                        {
                                            funcName = m.Name;
                                        }

                                        ApiInfo apiInfo;
                                        string extensionName = funcNameAttr.Extension.ToLower();
                                        if (!m_ApiExtensions.TryGetValue(extensionName, out apiInfo))
                                        {
                                            apiInfo = new ApiInfo();
                                            m_ApiExtensions.Add(extensionName, apiInfo);
                                        }
                                        List<ApiMethodInfo> methodList;
                                        if (!apiInfo.Methods.TryGetValue(funcName, out methodList))
                                        {
                                            apiInfo.Methods.Add(funcName, methodList = new List<ApiMethodInfo>());
                                        }
                                        methodList.Add(new ApiMethodInfo(funcName, api, m));
                                    }
                                }
                            }
                        }
                    }

                    Attribute attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnStateChange));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if(pi.Length != 1)
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
                        else if(pi[0].ParameterType != typeof(ScriptInstance))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnStateChange. Parameter type is not ScriptInstance.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else
                        {
                            Delegate d = Delegate.CreateDelegate(typeof(Script.StateChangeEventDelegate), null, m);
                            m_StateChangeDelegates.Add((Script.StateChangeEventDelegate)d);
                        }
                    }

                    attr = System.Attribute.GetCustomAttribute(m, typeof(ExecutedOnScriptReset));
                    if (attr != null && (m.Attributes & MethodAttributes.Static) != 0)
                    {
                        ParameterInfo[] pi = m.GetParameters();
                        if(pi.Length != 1)
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
                        else if(pi[0].ParameterType != typeof(ScriptInstance))
                        {
                            m_Log.DebugFormat("Invalid method '{0}' in '{1}' has attribute ExecutedOnScriptReset. Parameter type is not ScriptInstance.",
                                m.Name,
                                m.DeclaringType.FullName);
                        }
                        else
                        {
                            Delegate d = Delegate.CreateDelegate(typeof(Script.ScriptResetEventDelegate), null, m);
                            m_ScriptResetDelegates.Add((Script.ScriptResetEventDelegate)d);
                        }
                    }
                }
                #endregion
            }

            List<Dictionary<string, Resolver.OperatorType>> operators = new List<Dictionary<string, Resolver.OperatorType>>();
            Dictionary<string, string> blockOps = new Dictionary<string, string>();
            blockOps.Add("(", ")");
            blockOps.Add("[", "]");

            Dictionary<string, Resolver.OperatorType> plist;
            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("(integer)", Resolver.OperatorType.LeftUnary);
            plist.Add("(float)", Resolver.OperatorType.LeftUnary);
            plist.Add("(string)", Resolver.OperatorType.LeftUnary);
            plist.Add("(list)", Resolver.OperatorType.LeftUnary);
            plist.Add("(key)", Resolver.OperatorType.LeftUnary);
            plist.Add("(vector)", Resolver.OperatorType.LeftUnary);
            plist.Add("(rotation)", Resolver.OperatorType.LeftUnary);
            plist.Add("(quaternion)", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("@", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.RightUnary);
            plist.Add("--", Resolver.OperatorType.RightUnary);
            plist.Add(".", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("++", Resolver.OperatorType.LeftUnary);
            plist.Add("--", Resolver.OperatorType.LeftUnary);
            plist.Add("+", Resolver.OperatorType.LeftUnary);
            plist.Add("-", Resolver.OperatorType.LeftUnary);
            plist.Add("!", Resolver.OperatorType.LeftUnary);
            plist.Add("~", Resolver.OperatorType.LeftUnary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("*", Resolver.OperatorType.Binary);
            plist.Add("/", Resolver.OperatorType.Binary);
            plist.Add("%", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("+", Resolver.OperatorType.Binary);
            plist.Add("-", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<<", Resolver.OperatorType.Binary);
            plist.Add(">>", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("<", Resolver.OperatorType.Binary);
            plist.Add("<=", Resolver.OperatorType.Binary);
            plist.Add(">", Resolver.OperatorType.Binary);
            plist.Add(">=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("==", Resolver.OperatorType.Binary);
            plist.Add("!=", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("^", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("|", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("&&", Resolver.OperatorType.Binary);
            plist.Add("||", Resolver.OperatorType.Binary);

            operators.Add(plist = new Dictionary<string, Resolver.OperatorType>());
            plist.Add("=", Resolver.OperatorType.Binary);
            plist.Add("+=", Resolver.OperatorType.Binary);
            plist.Add("-=", Resolver.OperatorType.Binary);
            plist.Add("*=", Resolver.OperatorType.Binary);
            plist.Add("/=", Resolver.OperatorType.Binary);
            plist.Add("%=", Resolver.OperatorType.Binary);

            m_Resolver = new Resolver(m_ReservedWords, operators, blockOps);

            GenerateLSLSyntaxFile();

            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["lsl"] = this;
            SilverSim.Scripting.Common.CompilerRegistry.ScriptCompilers["XEngine"] = this; /* we won't be supporting anything beyond LSL compatibility */
        }

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            Preprocess(user, shbangs, reader, linenumber);
        }

        void WriteIndent(TextWriter writer, int indent)
        {
            while(indent-- > 0)
            {
                writer.Write("    ");
            }
        }

        void WriteIndented(TextWriter writer, string s, ref int oldIndent)
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

        void WriteIndented(TextWriter writer, List<string> list, ref int oldIndent)
        {
            foreach(string s in list)
            {
                WriteIndented(writer, s, ref oldIndent);
            }
        }

        public void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            CompileState cs = Preprocess(user, shbangs, reader, linenumber);
            /* rewrite script */
            int indent = 0;
            using (TextWriter writer = new StreamWriter(s, new UTF8Encoding(false)))
            {
                #region Write Variables
                foreach (KeyValuePair<string, Type> kvp in cs.m_VariableDeclarations)
                {
                    LineInfo li;
                    WriteIndented(writer, MapType(kvp.Value), ref indent);
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
                foreach(KeyValuePair<string, List<LineInfo>> kvp in cs.m_Functions)
                {
                    foreach(LineInfo li in kvp.Value)
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

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = Preprocess(user, shbangs, reader, lineNumber);
            return PostProcess(compileState, appDom, assetID, compileState.ForcedSleepDefault);
        }
    }
}
