// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
#if DEBUG
        void DumpFunctionLines(StreamWriter dumpILGen, List<LineInfo> lines, int indentinit = 0, string indentBase = "")
        {
            int indent = indentinit;
            bool closebrace = false;
            string indent_header = indentBase;

            closebrace = false;

            foreach (LineInfo line in lines)
            {
                if (line.Line[0] == "}")
                {
                    if (indent > 0)
                    {
                        --indent;
                        indent_header = indent_header.Substring(0, indent_header.Length - 2);
                    }
                    closebrace = true;
                }
                else
                {
                    if (closebrace)
                    {
                        dumpILGen.WriteLine("_____: ");
                    }
                    closebrace = false;
                }
                if (line.Line[line.Line.Count - 1] == "{")
                {
                    if (line.Line.Count > 1)
                    {
                        dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.LineNumber) + indent_header + string.Join(" ", line.Line.GetRange(0, line.Line.Count - 1)));
                    }
                    dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.LineNumber) + indent_header + "{");
                    ++indent;
                    indent_header += "  ";
                }
                else
                {
                    dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.LineNumber) + indent_header + string.Join(" ", line.Line));
                }
            }
        }
#endif

        IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID, bool forcedSleepDefault)
        {
#if DEBUG
            Directory.CreateDirectory("../data/dumps");
            using (StreamWriter dumpILGen = new StreamWriter("../data/dumps/ILGendump_" + assetID.ToString() + ".txt", false, Encoding.UTF8))
            {

                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
                    LineInfo initargs;

                    if (compileState.m_VariableInitValues.TryGetValue(variableKvp.Key, out initargs))
                    {
                        dumpILGen.WriteLine(string.Format("_____: {0} {1} = {2};", MapType(variableKvp.Value), variableKvp.Key, string.Join(" ", initargs.Line)));
                    }
                    else
                    {
                        dumpILGen.WriteLine(string.Format("_____: {0} {1};", MapType(variableKvp.Value), variableKvp.Key));
                    }
                }

                dumpILGen.WriteLine("_____: ");

                foreach (KeyValuePair<string, List<FunctionInfo>> functionKvp in compileState.m_Functions)
                {
                    foreach (FunctionInfo funcInfo in functionKvp.Value)
                    {
                        DumpFunctionLines(dumpILGen, funcInfo.FunctionLines);

                        dumpILGen.WriteLine("_____: ");
                    }
                }

                bool first = true;
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> stateKvp in compileState.m_States)
                {
                    if (stateKvp.Key == "default")
                    {
                        dumpILGen.WriteLine("_____: default");
                        dumpILGen.WriteLine("_____: {");
                    }
                    else
                    {
                        dumpILGen.WriteLine(string.Format("_____: state {0}", stateKvp.Key));
                        dumpILGen.WriteLine("_____: {");
                    }

                    foreach (KeyValuePair<string, List<LineInfo>> eventKvp in stateKvp.Value)
                    {
                        if (!first)
                        {
                            dumpILGen.WriteLine("_____: ");
                        }
                        DumpFunctionLines(dumpILGen, eventKvp.Value, 1, "  ");
                    }
                    dumpILGen.WriteLine("_____: }");
                }
                dumpILGen.WriteLine("");

                dumpILGen.WriteLine("********************************************************************************");
#endif
                string assetAssemblyName = "Script." + assetID.ToString().Replace('-', '_');
                AssemblyName aName = new AssemblyName(assetAssemblyName);
                AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
                ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, compileState.EmitDebugSymbols);

#region Create Script Container
#if DEBUG
                dumpILGen.WriteLine("DefineType({0})", assetAssemblyName + ".Script");
#endif
                TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
                Dictionary<string, object> typeLocals;
                Dictionary<string, object> typeLocalsInited;
                foreach (IScriptApi api in m_Apis)
                {
                    ScriptApiNameAttribute apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiNameAttribute));
                    FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
                    compileState.m_ApiFieldInfo.Add(apiAttr.Name, fb);
                }


#if DEBUG
                dumpILGen.WriteLine("DefineConstructor(new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) })");
#endif
                Type[] script_cb_params = new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) };
                ConstructorBuilder script_cb = scriptTypeBuilder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    script_cb_params);
                ILGenerator script_ilgen = script_cb.GetILGenerator();
                {
                    ConstructorInfo typeConstructor = typeof(Script).GetConstructor(script_cb_params);
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldarg_1);
                    script_ilgen.Emit(OpCodes.Ldarg_2);
                    script_ilgen.Emit(forcedSleepDefault ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    script_ilgen.Emit(OpCodes.Call, typeConstructor);
                }

                MethodBuilder reset_func = scriptTypeBuilder.DefineMethod("ResetVariables", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
                ILGenerator reset_ilgen = reset_func.GetILGenerator();
#endregion

                Dictionary<string, Type> stateTypes = new Dictionary<string, Type>();

#region Globals generation
                typeLocalsInited = AddConstants(compileState, scriptTypeBuilder, script_ilgen);
                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
#if DEBUG
                    dumpILGen.WriteLine("DefineField(\"{0}\", typeof({1}))", variableKvp.Key, variableKvp.Value.FullName);
#endif
                    FieldBuilder fb = scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                    compileState.m_VariableFieldInfo[variableKvp.Key] = fb;
                    typeLocalsInited[variableKvp.Key] = fb;
                }
                typeLocals = new Dictionary<string, object>(typeLocalsInited);

                List<string> varIsInited = new List<string>();
                List<string> varsToInit = new List<string>(compileState.m_VariableInitValues.Keys);

                compileState.ScriptTypeBuilder = scriptTypeBuilder;
                compileState.StateTypeBuilder = null;
#if DEBUG
                ILGenDumpProxy script_ILGen = new ILGenDumpProxy(script_ilgen, dumpILGen);
                ILGenDumpProxy reset_ILGen = new ILGenDumpProxy(reset_ilgen, dumpILGen);
                compileState.ILGen = script_ILGen;
#else
                compileState.ILGen = script_ilgen;
                ILGenerator script_ILGen = script_ilgen;
                ILGenerator reset_ILGen = reset_ilgen;
#endif
                foreach(KeyValuePair<string, FieldBuilder> kvp in compileState.m_VariableFieldInfo)
                {
#if DEBUG
                    compileState.ILGen.Writer.WriteLine("-- Init var " + kvp.Key);
#endif
                    FieldBuilder fb = kvp.Value;
                    if (!compileState.m_VariableInitValues.ContainsKey(kvp.Key))
                    {
                        if(fb.FieldType == typeof(LSLKey))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                            reset_ILGen.Emit(OpCodes.Stfld, fb);

                        }
                        else if(fb.FieldType == typeof(string))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if(fb.FieldType == typeof(double))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if (fb.FieldType == typeof(int))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldc_I4_0);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldc_I4_0);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if (fb.FieldType == typeof(AnArray))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if(fb.FieldType != typeof(Vector3) && fb.FieldType != typeof(Quaternion))
                        {
                            throw new ArgumentException("Unexpected type " + fb.FieldType.FullName);
                        }
                    }

                    if (fb.FieldType == typeof(Vector3))
                    {
                        FieldInfo sfld = typeof(Vector3).GetField("Zero");
                        script_ILGen.Emit(OpCodes.Ldarg_0);
                        script_ILGen.Emit(OpCodes.Ldsfld, sfld);
                        script_ILGen.Emit(OpCodes.Stfld, fb);

                        reset_ILGen.Emit(OpCodes.Ldarg_0);
                        reset_ILGen.Emit(OpCodes.Ldsfld, sfld);
                        reset_ILGen.Emit(OpCodes.Stfld, fb);
                    }
                    else if (fb.FieldType == typeof(Quaternion))
                    {
                        FieldInfo sfld = typeof(Quaternion).GetField("Identity");
                        script_ILGen.Emit(OpCodes.Ldarg_0);
                        script_ilgen.Emit(OpCodes.Ldsfld, sfld);
                        script_ILGen.Emit(OpCodes.Stfld, fb);

                        reset_ILGen.Emit(OpCodes.Ldarg_0);
                        reset_ilgen.Emit(OpCodes.Ldsfld, sfld);
                        reset_ILGen.Emit(OpCodes.Stfld, fb);
                    }

                }

                while (varsToInit.Count != 0)
                {
                    string varName = varsToInit[0];
                    varsToInit.RemoveAt(0);

                    FieldBuilder fb = compileState.m_VariableFieldInfo[varName];
                    LineInfo initargs;

                    if (compileState.m_VariableInitValues.TryGetValue(varName, out initargs))
                    {
                        Tree expressionTree;
                        try
                        {
                            expressionTree = LineToExpressionTree(compileState, initargs.Line, typeLocals.Keys, initargs.LineNumber);
                        }
                        catch (Exception e)
                        {
                            throw CompilerException(initargs, string.Format("Init value of variable {0} has syntax error. {1}\n{2}", varName, e.Message, e.StackTrace));
                        }

                        if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree))
                        {
#if DEBUG
                            compileState.ILGen.Writer.WriteLine("-- Init var " + varName);
#endif

                            compileState.ILGen = script_ILGen;
                            compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            ProcessExpression(
                                compileState,
                                fb.FieldType,
                                expressionTree,
                                initargs.LineNumber,
                                typeLocals);
                            compileState.ILGen.Emit(OpCodes.Stfld, fb);

                            compileState.ILGen = reset_ILGen;
                            compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            ProcessExpression(
                                compileState,
                                fb.FieldType,
                                expressionTree,
                                initargs.LineNumber,
                                typeLocals);
                            compileState.ILGen.Emit(OpCodes.Stfld, fb);

                            varIsInited.Add(varName);
                        }
                        else
                        {
                            /* push back that var. We got it too early. */
                            varsToInit.Add(varName);
                        }
                    }
                    else if (fb.FieldType == typeof(int))
                    {
                        script_ilgen.Emit(OpCodes.Ldc_I4_0);
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Ldc_I4_0);
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(double))
                    {
                        script_ilgen.Emit(OpCodes.Ldc_R8, (double)0);
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Ldc_R8, (double)0);
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(string))
                    {
                        script_ilgen.Emit(OpCodes.Ldstr, string.Empty);
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Ldstr, string.Empty);
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(Vector3))
                    {
                        script_ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(Type.EmptyTypes));
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(Type.EmptyTypes));
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(Quaternion))
                    {
                        script_ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(Type.EmptyTypes));
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(Type.EmptyTypes));
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(AnArray))
                    {
                        script_ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else if (fb.FieldType == typeof(LSLKey))
                    {
                        script_ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                        script_ilgen.Emit(OpCodes.Stfld, fb);

                        reset_ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                        reset_ilgen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                }
#endregion

#region Function compilation
                /* we have to process the function definition first */
                foreach (KeyValuePair<string, List<FunctionInfo>> functionKvp in compileState.m_Functions)
                {
                    foreach (FunctionInfo funcInfo in functionKvp.Value)
                    {
                        MethodBuilder method;
                        Type returnType = typeof(void);
                        List<string> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        string functionName = functionDeclaration[1];
                        int functionStart = 3;

                        switch (functionDeclaration[0])
                        {
                            case "integer":
                                returnType = typeof(int);
                                break;

                            case "vector":
                                returnType = typeof(Vector3);
                                break;

                            case "list":
                                returnType = typeof(AnArray);
                                break;

                            case "float":
                                returnType = typeof(double);
                                break;

                            case "string":
                                returnType = typeof(string);
                                break;

                            case "key":
                                returnType = typeof(LSLKey);
                                break;

                            case "rotation":
                            case "quaternion":
                                returnType = typeof(Quaternion);
                                break;

                            case "void":
                                returnType = typeof(void);
                                break;

                            default:
                                functionName = functionDeclaration[0];
                                functionStart = 2;
                                break;
                        }
                        List<Type> paramTypes = new List<Type>();
                        List<string> paramName = new List<string>();
                        while (functionDeclaration[functionStart] != ")")
                        {
                            if (functionDeclaration[functionStart] == ",")
                            {
                                ++functionStart;
                            }
                            switch (functionDeclaration[functionStart++])
                            {
                                case "integer":
                                    paramTypes.Add(typeof(int));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "vector":
                                    paramTypes.Add(typeof(Vector3));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "list":
                                    paramTypes.Add(typeof(AnArray));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "float":
                                    paramTypes.Add(typeof(double));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "string":
                                    paramTypes.Add(typeof(string));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "key":
                                    paramTypes.Add(typeof(LSLKey));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                case "rotation":
                                case "quaternion":
                                    paramTypes.Add(typeof(Quaternion));
                                    paramName.Add(functionDeclaration[functionStart++]);
                                    break;

                                default:
                                    throw CompilerException(funcInfo.FunctionLines[0], "Internal Error");
                            }
                        }

#if DEBUG
                        dumpILGen.WriteLine("DefineMethod(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, paramTypes.ToArray().ToString());
#endif
                        method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                        KeyValuePair<string, Type>[] paramSignature = new KeyValuePair<string, Type>[paramTypes.Count];
                        for (int i = 0; i < paramTypes.Count; ++i)
                        {
                            paramSignature[i] = new KeyValuePair<string, Type>(paramName[i], paramTypes[i]);
                        }
                        funcInfo.Parameters = paramSignature;
                        funcInfo.ReturnType = returnType;
                        funcInfo.Method = method;
                    }
                }

                foreach (KeyValuePair<string, List<FunctionInfo>> functionKvp in compileState.m_Functions)
                {
                    foreach (FunctionInfo funcInfo in functionKvp.Value)
                    {
                        List<string> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        MethodBuilder method = funcInfo.Method;

#if DEBUG
                        ILGenDumpProxy method_ilgen = new ILGenDumpProxy(method.GetILGenerator(), dumpILGen);
#else
                    ILGenerator method_ilgen = method.GetILGenerator();
#endif
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, funcInfo.FunctionLines, typeLocals);
                        method_ilgen.Emit(OpCodes.Ret);
                    }
                }
#endregion

#region State compilation
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> stateKvp in compileState.m_States)
                {
                    FieldBuilder fb;
#if DEBUG
                    dumpILGen.WriteLine("DefineState(\"{0}\")", stateKvp.Key);
#endif
                    TypeBuilder state = mb.DefineType(aName.Name + ".State." + stateKvp.Key, TypeAttributes.Public, typeof(object));
                    state.AddInterfaceImplementation(typeof(ILSLState));
                    fb = state.DefineField("Instance", scriptTypeBuilder, FieldAttributes.Private | FieldAttributes.InitOnly);
                    compileState.InstanceField = fb;

                    ConstructorBuilder state_cb = state.DefineConstructor(
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                        CallingConventions.Standard,
                        new Type[1] { scriptTypeBuilder });
                    ILGenerator state_ilgen = state_cb.GetILGenerator();
                    ConstructorInfo typeConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
                    state_ilgen.Emit(OpCodes.Ldarg_0);
                    state_ilgen.Emit(OpCodes.Call, typeConstructor);
                    state_ilgen.Emit(OpCodes.Ldarg_0);
                    state_ilgen.Emit(OpCodes.Ldarg_1);
                    state_ilgen.Emit(OpCodes.Stfld, fb);

                    state_ilgen.Emit(OpCodes.Ret);

                    /* add the type initializers */
                    state_cb = state.DefineTypeInitializer();

                    state_ilgen = state_cb.GetILGenerator();
                    state_ilgen.Emit(OpCodes.Ret);

                    foreach (KeyValuePair<string, List<LineInfo>> eventKvp in stateKvp.Value)
                    {
                        MethodInfo d = compileState.ApiInfo.EventDelegates[eventKvp.Key];
                        ParameterInfo[] pinfo = d.GetParameters();
                        Type[] paramtypes = new Type[pinfo.Length];
                        for (int pi = 0; pi < pinfo.Length; ++pi)
                        {
                            paramtypes[pi] = pinfo[pi].ParameterType;
                        }
#if DEBUG
                        dumpILGen.WriteLine("DefineEvent(\"{0}\")", eventKvp.Key);
#endif
                        MethodBuilder eventbuilder = state.DefineMethod(
                            eventKvp.Key,
                            MethodAttributes.Public,
                            typeof(void),
                            paramtypes);
#if DEBUG
                        ILGenDumpProxy event_ilgen = new ILGenDumpProxy(eventbuilder.GetILGenerator(), dumpILGen);
#else
                        ILGenerator event_ilgen = eventbuilder.GetILGenerator();
#endif
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        ProcessFunction(compileState, scriptTypeBuilder, state, eventbuilder, event_ilgen, eventKvp.Value, typeLocals);
                    }

                    stateTypes.Add(stateKvp.Key, state.CreateType());
                }
#endregion

                script_ilgen.Emit(OpCodes.Ret);

#region Call type initializer
                {
                    script_cb = scriptTypeBuilder.DefineTypeInitializer();
                    script_ilgen = script_cb.GetILGenerator();
                    script_ilgen.Emit(OpCodes.Ret);
                }
                reset_ilgen.Emit(OpCodes.Ret);
#endregion

                mb.CreateGlobalFunctions();

#region Initialize static fields
                Type t = scriptTypeBuilder.CreateType();

                foreach (IScriptApi api in m_Apis)
                {
                    ScriptApiNameAttribute apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiNameAttribute));
                    FieldInfo info = t.GetField(apiAttr.Name, BindingFlags.Static | BindingFlags.Public);
                    info.SetValue(null, api);
                }
#endregion

                return new LSLScriptAssembly(ab, t, stateTypes, forcedSleepDefault, 
                    m_ScriptRemoveDelegates,
                    m_ScriptSerializeDelegates,
                    m_ScriptDeserializeDelegates);
#if DEBUG
            }
#endif
        }

        bool AreAllVarReferencesSatisfied(CompileState cs, List<string> initedVars, Tree expressionTree)
        {
            foreach (Tree st in expressionTree.SubTree)
            {
                if (!AreAllVarReferencesSatisfied(cs, initedVars, st))
                {
                    return false;
                }
                else if ((st.Type == Tree.EntryType.Variable || st.Type == Tree.EntryType.Unknown) &&
                    cs.m_VariableDeclarations.ContainsKey(st.Entry) &&
                    !initedVars.Contains(st.Entry))
                {
                    return (!cs.m_VariableInitValues.ContainsKey(st.Entry));
                }
            }
            return true;
        }
    }
}
