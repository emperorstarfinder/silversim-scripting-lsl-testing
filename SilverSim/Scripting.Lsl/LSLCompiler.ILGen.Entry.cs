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

#pragma warning disable RCS1029, IDE0018

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
#if DEBUG
        private void DumpFunctionLines(StreamWriter dumpILGen, List<LineInfo> lines, int indentinit = 0, string indentBase = "")
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

        private IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID, bool forcedSleepDefault, AssemblyBuilderAccess access, string filename = "")
        {
#if DEBUG
            Directory.CreateDirectory("../data/dumps");
            using (var dumpILGen = new StreamWriter("../data/dumps/ILGendump_" + assetID.ToString() + ".txt", false, Encoding.UTF8))
            {
                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
                    LineInfo initargs;

                    if (compileState.m_VariableInitValues.TryGetValue(variableKvp.Key, out initargs))
                    {
                        dumpILGen.WriteLine(string.Format("_____: {0} {1} = {2};", compileState.MapType(variableKvp.Value), variableKvp.Key, string.Join(" ", initargs.Line)));
                    }
                    else
                    {
                        dumpILGen.WriteLine(string.Format("_____: {0} {1};", compileState.MapType(variableKvp.Value), variableKvp.Key));
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
                        first = false;
                        DumpFunctionLines(dumpILGen, eventKvp.Value, 1, "  ");
                    }
                    dumpILGen.WriteLine("_____: }");
                }
                dumpILGen.WriteLine("");

                dumpILGen.WriteLine("********************************************************************************");
#endif
                string assetAssemblyName = "Script." + assetID.ToString().Replace('-', '_');
                AssemblyName aName = new AssemblyName(assetAssemblyName);
                AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, access);
                ModuleBuilder mb = (access == AssemblyBuilderAccess.RunAndCollect) ?
                    ab.DefineDynamicModule(aName.Name, compileState.EmitDebugSymbols) :
                    ab.DefineDynamicModule(aName.Name, filename, compileState.EmitDebugSymbols);

                if (compileState.EmitDebugSymbols)
                {
                    compileState.DebugDocument = mb.DefineDocument(assetID.ToString() + ".lsl",
                        SymDocumentType.Text,
                        Guid.Empty,
                        Guid.Empty);
                }

                #region Create Script Container
#if DEBUG
                dumpILGen.WriteLine("********************************************************************************");
                dumpILGen.WriteLine("DefineType({0})", assetAssemblyName + ".Script");
#endif
                TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
                Dictionary<string, object> typeLocals;
                Dictionary<string, object> typeLocalsInited;
                foreach (IScriptApi api in m_Apis)
                {
                    var apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiNameAttribute));
                    FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
                    compileState.m_ApiFieldInfo.Add(apiAttr.Name, fb);
                }

#if DEBUG
                dumpILGen.WriteLine("********************************************************************************");
                dumpILGen.WriteLine("DefineConstructor(new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) })");
#endif
                var script_cb_params = new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) };
                ConstructorBuilder script_cb = scriptTypeBuilder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    script_cb_params);
                ILGenerator script_ilgen = script_cb.GetILGenerator();
                {
                    ConstructorInfo typeConstructor = typeof(Script).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, script_cb_params, null);
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldarg_1);
                    script_ilgen.Emit(OpCodes.Ldarg_2);
                    script_ilgen.Emit(forcedSleepDefault ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    script_ilgen.Emit(OpCodes.Call, typeConstructor);
                }

                if(compileState.UsesSinglePrecision)
                {
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldc_I4_1);
                    script_ilgen.Emit(OpCodes.Stfld, typeof(Script).GetField("m_UsesSinglePrecision", BindingFlags.Instance | BindingFlags.NonPublic));
                }

                MethodBuilder reset_func = scriptTypeBuilder.DefineMethod("ResetVariables", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
                ILGenerator reset_ilgen = reset_func.GetILGenerator();
#endregion

                var stateTypes = new Dictionary<string, Type>();

#region Globals generation
                typeLocalsInited = AddConstants(compileState);
                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
#if DEBUG
                    dumpILGen.WriteLine("********************************************************************************");
                    dumpILGen.WriteLine("DefineField(\"{0}\", typeof({1}))", variableKvp.Key, variableKvp.Value.FullName);
#endif
                    FieldBuilder fb = compileState.LanguageExtensions.EnableStateVariables ?
                        scriptTypeBuilder.DefineField("var_glob_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public) :
                        scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                    compileState.m_VariableFieldInfo[variableKvp.Key] = fb;
                    typeLocalsInited[variableKvp.Key] = fb;
                }
                foreach(KeyValuePair<string, Dictionary<string, Type>> stateVariableKvp in compileState.m_StateVariableDeclarations)
                {
                    foreach (KeyValuePair<string, Type> variableKvp in stateVariableKvp.Value)
                    {
#if DEBUG
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("State[{2}].DefineField(\"{0}\", typeof({1}))", variableKvp.Key, variableKvp.Value.FullName, stateVariableKvp.Key);
#endif
                        FieldBuilder fb = scriptTypeBuilder.DefineField("var_state_" + stateVariableKvp.Key + "_" + variableKvp.Key, variableKvp.Value, FieldAttributes.Public);
                        if(!compileState.m_StateVariableFieldInfo.ContainsKey(stateVariableKvp.Key))
                        {
                            compileState.m_StateVariableFieldInfo.Add(stateVariableKvp.Key, new Dictionary<string, FieldBuilder>());
                        }
                        compileState.m_StateVariableFieldInfo[stateVariableKvp.Key][variableKvp.Key] = fb;
                    }
                }

                typeLocals = new Dictionary<string, object>(typeLocalsInited);

                var varIsInited = new List<string>();
                var varsToInit = new List<string>(compileState.m_VariableInitValues.Keys);

                compileState.ScriptTypeBuilder = scriptTypeBuilder;
                compileState.StateTypeBuilder = null;
                var script_ILGen = new ILGenDumpProxy(script_ilgen,
                    compileState.DebugDocument
#if DEBUG
                    ,dumpILGen
#endif
                    );
                var reset_ILGen = new ILGenDumpProxy(reset_ilgen,
                    compileState.DebugDocument
#if DEBUG
                    , dumpILGen
#endif
                    );
                compileState.ILGen = script_ILGen;
                foreach(KeyValuePair<string, FieldBuilder> kvp in compileState.m_VariableFieldInfo)
                {
#if DEBUG
                    compileState.ILGen.Writer.WriteLine("-- Init var " + kvp.Key);
#endif
                    FieldBuilder fb = kvp.Value;
                    if (!compileState.m_VariableInitValues.ContainsKey(kvp.Key))
                    {
                        if(fb.FieldType == typeof(string))
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
                        else if (fb.FieldType == typeof(long))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldc_I8, 0L);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldc_I8, 0L);
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
                        else if (fb.FieldType == typeof(Quaternion))
                        {
                            FieldInfo sfld = typeof(Quaternion).GetField("Identity");
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldsfld, sfld);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldsfld, sfld);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if (fb.FieldType.IsValueType)
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldflda, fb);
                            script_ILGen.Emit(OpCodes.Initobj, fb.FieldType);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldflda, fb);
                            reset_ILGen.Emit(OpCodes.Initobj, fb.FieldType);
                        }
                        else
                        {
                            ConstructorInfo cInfo = fb.FieldType.GetConstructor(Type.EmptyTypes);
                            if (cInfo == null)
                            {
                                throw new ArgumentException("Unexpected type " + fb.FieldType.FullName);
                            }
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Newobj, cInfo);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Newobj, cInfo);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                    }
                }

                /* init state variables with default values */
                foreach(KeyValuePair<string, Dictionary<string, FieldBuilder>> stateVarKvp in compileState.m_StateVariableFieldInfo)
                {
                    foreach(KeyValuePair<string, FieldBuilder> kvp in stateVarKvp.Value)
                    {
                        FieldBuilder fb = kvp.Value;
                        if (fb.FieldType == typeof(string))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if (fb.FieldType == typeof(double))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if (fb.FieldType == typeof(long))
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldc_I8, (long)0);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldc_I8, (long)0);
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
                        else if (fb.FieldType == typeof(Quaternion))
                        {
                            FieldInfo sfld = typeof(Quaternion).GetField("Identity");
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldsfld, sfld);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldsfld, sfld);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
                        else if(fb.FieldType.IsValueType)
                        {
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Ldflda, fb);
                            script_ILGen.Emit(OpCodes.Initobj, fb.FieldType);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Ldflda, fb);
                            reset_ILGen.Emit(OpCodes.Initobj, fb.FieldType);
                        }
                        else
                        {
                            ConstructorInfo cInfo = fb.FieldType.GetConstructor(Type.EmptyTypes);
                            if (cInfo == null)
                            {
                                throw new ArgumentException("Unexpected type " + fb.FieldType.FullName);
                            }
                            script_ILGen.Emit(OpCodes.Ldarg_0);
                            script_ILGen.Emit(OpCodes.Newobj, cInfo);
                            script_ILGen.Emit(OpCodes.Stfld, fb);

                            reset_ILGen.Emit(OpCodes.Ldarg_0);
                            reset_ILGen.Emit(OpCodes.Newobj, cInfo);
                            reset_ILGen.Emit(OpCodes.Stfld, fb);
                        }
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
                            expressionTree = LineToExpressionTree(compileState, initargs.Line, typeLocals.Keys, initargs.LineNumber, compileState.CurrentCulture);
                        }
                        catch (Exception e)
                        {
                            throw CompilerException(initargs, string.Format(this.GetLanguageString(compileState.CurrentCulture, "InitValueOfVariable0HasSyntaxError", "Init value of variable {0} has syntax error. {1}\n{2}"), varName, e.Message, e.StackTrace));
                        }

                        if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree))
                        {
#if DEBUG
                            compileState.ILGen.Writer.WriteLine("-- Init var " + varName);
#endif

                            compileState.ILGen = script_ILGen;
                            compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            ResultIsModifiedEnum modified = ProcessExpression(
                                compileState,
                                fb.FieldType,
                                expressionTree,
                                initargs.LineNumber,
                                typeLocals);
                            if (modified == ResultIsModifiedEnum.Yes)
                            {
                                /* skip operation as it is modified */
                            }
                            else if (fb.FieldType == typeof(AnArray) || Attribute.GetCustomAttribute(fb.FieldType, typeof(APICloneOnAssignmentAttribute)) != null)
                            {
                                /* keep LSL semantics valid */
                                compileState.ILGen.Emit(OpCodes.Newobj, fb.FieldType.GetConstructor(new Type[] { fb.FieldType }));
                            }
                            compileState.ILGen.Emit(OpCodes.Stfld, fb);

                            compileState.ILGen = reset_ILGen;
                            compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            modified = ProcessExpression(
                                compileState,
                                fb.FieldType,
                                expressionTree,
                                initargs.LineNumber,
                                typeLocals);
                            if (modified == ResultIsModifiedEnum.Yes)
                            {
                                /* skip operation as it is modified */
                            }
                            else if (fb.FieldType == typeof(AnArray) || Attribute.GetCustomAttribute(fb.FieldType, typeof(APICloneOnAssignmentAttribute)) != null)
                            {
                                /* keep LSL semantics valid */
                                compileState.ILGen.Emit(OpCodes.Newobj, fb.FieldType.GetConstructor(new Type[] { fb.FieldType }));
                            }
                            compileState.ILGen.Emit(OpCodes.Stfld, fb);

                            varIsInited.Add(varName);
                        }
                        else
                        {
                            /* push back that var. We got it too early. */
                            varsToInit.Add(varName);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Variable without init value encountered " + varName);
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
                        Type returnType;
                        List<string> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        string functionName = functionDeclaration[1];
                        int functionStart = 3;

                        if(!compileState.ApiInfo.Types.TryGetValue(functionDeclaration[0], out returnType))
                        {
                            functionName = functionDeclaration[0];
                            functionStart = 2;
                            returnType = typeof(void);
                        }
                        var paramTypes = new List<Type>();
                        var paramName = new List<string>();
                        while (functionDeclaration[functionStart] != ")")
                        {
                            if (functionDeclaration[functionStart] == ",")
                            {
                                ++functionStart;
                            }

                            Type paramType;
                            if(!compileState.TryGetValidVarType(functionDeclaration[functionStart++], out paramType))
                            {
                                throw CompilerException(funcInfo.FunctionLines[0], "Internal Error");
                            }
                            paramTypes.Add(paramType);
                            paramName.Add(functionDeclaration[functionStart++]);
                        }

#if DEBUG
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("DefineMethod(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, paramTypes.ToArray().ToString());
#endif
                        method = scriptTypeBuilder.DefineMethod("fn_" + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                        var paramSignature = new KeyValuePair<string, Type>[paramTypes.Count];
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
                        MethodBuilder method = funcInfo.Method;

#if DEBUG
                        Type returnType;
                        List<string> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        string functionName = functionDeclaration[1];
                        int functionStart = 3;

                        if(!compileState.ApiInfo.Types.TryGetValue(functionDeclaration[0], out returnType))
                        {
                            functionName = functionDeclaration[0];
                            functionStart = 2;
                            returnType = typeof(void);
                        }
                        var paramTypes = new List<Type>();
                        var paramName = new List<string>();
                        while (functionDeclaration[functionStart] != ")")
                        {
                            if (functionDeclaration[functionStart] == ",")
                            {
                                ++functionStart;
                            }

                            Type paramType;
                            if(!compileState.TryGetValidVarType(functionDeclaration[functionStart++], out paramType))
                            {
                                throw CompilerException(funcInfo.FunctionLines[0], "Internal Error");
                            }
                            paramTypes.Add(paramType);
                            paramName.Add(functionDeclaration[functionStart++]);
                        }

                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("GenerateMethodIL.Begin(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, paramTypes.ToArray().ToString());
#endif

                        var method_ilgen = new ILGenDumpProxy(method.GetILGenerator(),
                            compileState.DebugDocument
#if DEBUG
                            , dumpILGen
#endif
                            );
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, funcInfo.FunctionLines, typeLocals);

#if DEBUG
                        dumpILGen.WriteLine("GenerateMethodIL.End(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, paramTypes.ToArray().ToString());
#endif
                    }
                }
#endregion

#region State compilation
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> stateKvp in compileState.m_States)
                {
                    FieldBuilder fb;
#if DEBUG
                    dumpILGen.WriteLine("********************************************************************************");
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

                    if(!stateKvp.Value.ContainsKey("state_entry"))
                    {
                        MethodBuilder eventbuilder = state.DefineMethod(
                            "state_entry",
                            MethodAttributes.Public,
                            typeof(void),
                            Type.EmptyTypes);
                        var event_ilgen = new ILGenDumpProxy(
                            eventbuilder.GetILGenerator(),
                            compileState.DebugDocument
#if DEBUG
                            , dumpILGen
#endif
                            );
                        compileState.ILGen = event_ilgen;
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        StateEntryInitVars(stateKvp.Key, compileState, typeLocals);
                        event_ilgen.Emit(OpCodes.Ret);
                    }

                    foreach (KeyValuePair<string, List<LineInfo>> eventKvp in stateKvp.Value)
                    {
                        MethodInfo d = compileState.ApiInfo.EventDelegates[eventKvp.Key];
                        ParameterInfo[] pinfo = d.GetParameters();
                        var paramtypes = new Type[pinfo.Length];
                        for (int pi = 0; pi < pinfo.Length; ++pi)
                        {
                            paramtypes[pi] = pinfo[pi].ParameterType;
                        }
#if DEBUG
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("DefineEvent(\"{0}\")", eventKvp.Key);
#endif
                        MethodBuilder eventbuilder = state.DefineMethod(
                            eventKvp.Key,
                            MethodAttributes.Public,
                            typeof(void),
                            paramtypes);
                        var event_ilgen = new ILGenDumpProxy(eventbuilder.GetILGenerator(),
                            compileState.DebugDocument
#if DEBUG
                            , dumpILGen
#endif
                            );
                        compileState.ILGen = event_ilgen;
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        Dictionary<string, FieldBuilder> stateVarDict;

                        if (compileState.m_StateVariableFieldInfo.TryGetValue(stateKvp.Key, out stateVarDict))
                        {
                            foreach(KeyValuePair<string, FieldBuilder> fbKvp in stateVarDict)
                            {
                                typeLocals[fbKvp.Key] = fbKvp.Value;
                            }
                        }

                        if (eventKvp.Key == "state_entry")
                        {
                            StateEntryInitVars(stateKvp.Key, compileState, typeLocals);
                        }

                        event_ilgen.Emit(OpCodes.Ldarg_0);
                        event_ilgen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                        event_ilgen.Emit(OpCodes.Call, typeof(Script).GetMethod("ResetCallDepthCount"));

                        ProcessFunction(compileState, scriptTypeBuilder, state, eventbuilder, event_ilgen, eventKvp.Value, typeLocals);
#if DEBUG
                        dumpILGen.WriteLine("DefineEvent.ILGenEnd(\"{0}\")", eventKvp.Key);
#endif
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

                if (access == AssemblyBuilderAccess.RunAndCollect)
                {
                    foreach (IScriptApi api in m_Apis)
                    {
                        var apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiNameAttribute));
                        FieldInfo info = t.GetField(apiAttr.Name, BindingFlags.Static | BindingFlags.Public);
                        info.SetValue(null, api);
                    }
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

        private void StateEntryInitVars(string stateName, CompileState compileState, Dictionary<string, object> typeLocalIn)
        {
            Dictionary<string, FieldBuilder> stateVars;
            Dictionary<string, LineInfo> stateVarInitValues;
            if(!compileState.m_StateVariableFieldInfo.TryGetValue(stateName, out stateVars))
            {
                return;
            }
            if(!compileState.m_StateVariableInitValues.TryGetValue(stateName, out stateVarInitValues))
            {
                stateVarInitValues = new Dictionary<string, LineInfo>();
            }

            var varsToInit = new List<string>(stateVars.Keys);
            var varIsInited = new List<string>();
            var typeLocals = new Dictionary<string, object>(typeLocalIn);

            while (varsToInit.Count != 0)
            {
                string varName = varsToInit[0];
                varsToInit.RemoveAt(0);

                FieldBuilder fb = stateVars[varName];
                LineInfo initargs;

                if (stateVarInitValues.TryGetValue(varName, out initargs))
                {
                    Tree expressionTree;
                    try
                    {
                        expressionTree = LineToExpressionTree(compileState, initargs.Line, typeLocals.Keys, initargs.LineNumber, compileState.CurrentCulture);
                    }
                    catch (Exception e)
                    {
                        throw CompilerException(initargs, string.Format(this.GetLanguageString(compileState.CurrentCulture, "InitValueOfVariable0HasSyntaxError", "Init value of state variable {0} has syntax error. {1}\n{2}"), varName, e.Message, e.StackTrace));
                    }

                    if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree))
                    {
#if DEBUG
                        compileState.ILGen.Writer.WriteLine("-- Init state var " + varName);
#endif
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                        ResultIsModifiedEnum modified = ProcessExpression(
                            compileState,
                            fb.FieldType,
                            expressionTree,
                            initargs.LineNumber,
                            typeLocals);
                        if (modified == ResultIsModifiedEnum.Yes)
                        {
                            /* skip operation as it is modified */
                        }
                        else if (fb.FieldType == typeof(AnArray) || Attribute.GetCustomAttribute(fb.FieldType, typeof(APICloneOnAssignmentAttribute)) != null)
                        {
                            /* keep LSL semantics valid */
                            compileState.ILGen.Emit(OpCodes.Newobj, fb.FieldType.GetConstructor(new Type[] { fb.FieldType }));
                        }
                        compileState.ILGen.Emit(OpCodes.Stfld, fb);

                        varIsInited.Add(varName);
                    }
                    else
                    {
                        /* push back that var. We got it too early. */
                        varsToInit.Add(varName);
                    }
                }
                else if (fb.FieldType == typeof(long))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I8, 0L);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Ldstr, string.Empty);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Vector3))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Quaternion))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
            }
        }

        private bool AreAllVarReferencesSatisfied(CompileState cs, List<string> initedVars, Tree expressionTree)
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
                    return !cs.m_VariableInitValues.ContainsKey(st.Entry);
                }
            }
            return true;
        }
    }
}
