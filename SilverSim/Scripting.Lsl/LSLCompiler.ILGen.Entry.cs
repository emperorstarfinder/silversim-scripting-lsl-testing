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
using SilverSim.Scripting.Common;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
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
                        dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.FirstTokenLineNumber) + indent_header + string.Join(" ", line.Line.GetRange(0, line.Line.Count - 1)));
                    }
                    dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.FirstTokenLineNumber) + indent_header + "{");
                    ++indent;
                    indent_header += "  ";
                }
                else
                {
                    dumpILGen.WriteLine(string.Format("{0,5:d}: ", line.FirstTokenLineNumber) + indent_header + string.Join(" ", line.Line));
                }
            }
        }

        private bool DebugDiagnosticOutput { get; set; }

        private IScriptAssembly PostProcess(CompileState compileState, AppDomain appDom, UUID assetID, bool forcedSleepDefault, AssemblyBuilderAccess access, string filename = "")
        {
            StreamWriter dumpILGen = null;
            try
            {
                if(DebugDiagnosticOutput)
                {
                    Directory.CreateDirectory("../data/dumps");
                    dumpILGen = new StreamWriter("../data/dumps/ILGendump_" + assetID.ToString() + ".txt", false, Encoding.UTF8);
                }

                if (dumpILGen != null)
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
                }

                string assetAssemblyName = "Script." + assetID.ToString().Replace('-', '_');
                var aName = new AssemblyName(assetAssemblyName);
                AssemblyBuilder ab = appDom.DefineDynamicAssembly(aName, access);

                if (compileState.EmitDebugSymbols)
                {
                    Type daType = typeof(DebuggableAttribute);
                    ConstructorInfo daCtor = daType.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
                    var daBuilder = new CustomAttributeBuilder(daCtor, new object[]
                    {
                        DebuggableAttribute.DebuggingModes.DisableOptimizations |
                        DebuggableAttribute.DebuggingModes.Default
                    });
                    ab.SetCustomAttribute(daBuilder);
                }

                ModuleBuilder mb = (access == AssemblyBuilderAccess.RunAndCollect) ?
                    ab.DefineDynamicModule(aName.Name, compileState.EmitDebugSymbols) :
                    ab.DefineDynamicModule(aName.Name, filename, compileState.EmitDebugSymbols);

                if(compileState.EmitDebugSymbols)
                {
                    compileState.DebugDocument = mb.DefineDocument(Path.GetFullPath($"../data/dumps/{assetID}.lsl"),
                        SymDocumentType.Text,
                        Guid.Empty,
                        Guid.Empty);
                }

                #region Create Script Container
                if (dumpILGen != null)
                {
                    dumpILGen.WriteLine("********************************************************************************");
                    dumpILGen.WriteLine("DefineType({0})", assetAssemblyName + ".Script");
                }
                TypeBuilder scriptTypeBuilder = mb.DefineType(assetAssemblyName + ".Script", TypeAttributes.Public, typeof(Script));
                Dictionary<string, object> typeLocals;
                Dictionary<string, object> typeLocalsInited;
                foreach (IScriptApi api in m_Apis)
                {
                    var apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(api.GetType(), typeof(ScriptApiNameAttribute));
                    FieldBuilder fb = scriptTypeBuilder.DefineField(apiAttr.Name, api.GetType(), FieldAttributes.Static | FieldAttributes.Public);
                    compileState.m_ApiFieldInfo.Add(apiAttr.Name, fb);
                }

                if (dumpILGen != null)
                {
                    dumpILGen.WriteLine("********************************************************************************");
                    dumpILGen.WriteLine("DefineConstructor(new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) })");
                }
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

                if(compileState.LanguageExtensions.UseMessageObjectEvent)
                {
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldc_I4_1);
                    script_ilgen.Emit(OpCodes.Stfld, typeof(Script).GetField("UseMessageObjectEvent", BindingFlags.Instance | BindingFlags.NonPublic));
                }

                if(compileState.LanguageExtensions.AllowEmptyDialogList)
                {
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldc_I4_1);
                    script_ilgen.Emit(OpCodes.Stfld, typeof(Script).GetField("m_AllowEmptyDialogList", BindingFlags.Instance | BindingFlags.NonPublic));
                }

                if(compileState.LanguageExtensions.InheritEventsOnStateChange)
                {
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldc_I4_1);
                    script_ilgen.Emit(OpCodes.Stfld, typeof(Script).GetField("InheritEventsOnStateChange", BindingFlags.Instance | BindingFlags.NonPublic));
                }

                foreach(string name in compileState.m_NamedTimers)
                {
                    script_ilgen.Emit(OpCodes.Ldarg_0);
                    script_ilgen.Emit(OpCodes.Ldstr, name);
                    script_ilgen.Emit(OpCodes.Call, typeof(Script).GetMethod("RegisterNamedTimer", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null));
                }

                MethodBuilder reset_func = scriptTypeBuilder.DefineMethod("ResetVariables", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
                ILGenerator reset_ilgen = reset_func.GetILGenerator();
                #endregion

                var stateTypes = new Dictionary<string, Type>();

                /* Collect static globals */
                typeLocalsInited = AddConstants(compileState);

                #region Struct generation
                foreach (KeyValuePair<string, Dictionary<string, LineInfo>> kvp in compileState.m_Structs)
                {
                    if (dumpILGen != null)
                    {
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("DefineType(\"{0}\")", assetAssemblyName + ".Struct_" + kvp.Key);
                    }
                    TypeBuilder structTypeBuilder = mb.DefineType(assetAssemblyName + ".Struct_" + kvp.Key, TypeAttributes.Public, typeof(object));
                    structTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(APIDisplayNameAttribute).GetConstructor(new Type[] { typeof(string) }), new object[] { kvp.Key }));
                    structTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(APICloneOnAssignmentAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
                    structTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(APIAccessibleMembersAttribute).GetConstructor(new Type[] { typeof(string[]) }), new object[] { new string[0] }));
                    structTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(APIIsVariableTypeAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
                    structTypeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

                    ConstructorBuilder cb = structTypeBuilder.DefineTypeInitializer();
                    var typecb_ILGen = new ILGenDumpProxy(
                        cb.GetILGenerator(),
                        compileState.DebugDocument,
                        dumpILGen);
                    typecb_ILGen.Emit(OpCodes.Ret);

                    cb = structTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                    var defcb_ILGen = new ILGenDumpProxy(
                        cb.GetILGenerator(),
                        compileState.DebugDocument,
                        dumpILGen);
                    cb = structTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { structTypeBuilder });
                    var copycb_ILGen = new ILGenDumpProxy(
                        cb.GetILGenerator(),
                        compileState.DebugDocument,
                        dumpILGen);

                    foreach (KeyValuePair<string, LineInfo> variableKvp in kvp.Value)
                    {
                        Type varType;
                        if(!compileState.TryGetValidVarType(variableKvp.Value.Line[0], out varType))
                        {
                            throw new CompilerException(variableKvp.Value.Line[0].LineNumber, "Internal Error");
                        }
                        FieldBuilder structField = structTypeBuilder.DefineField(variableKvp.Key, varType, FieldAttributes.Public);
                        /* default value */
                        if (varType == typeof(string))
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else if (varType == typeof(double))
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else if (varType == typeof(long))
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldc_I8, (long)0);
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else if (varType == typeof(int))
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldc_I4_0);
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else if (varType == typeof(Quaternion))
                        {
                            FieldInfo sfld = typeof(Quaternion).GetField("Identity");
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldsfld, sfld);
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else if (varType.IsValueType)
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Ldflda, structField);
                            defcb_ILGen.Emit(OpCodes.Initobj, varType);
                        }
                        else
                        {
                            defcb_ILGen.Emit(OpCodes.Ldarg_0);
                            defcb_ILGen.Emit(OpCodes.Newobj, compileState.GetDefaultConstructor(varType));
                            defcb_ILGen.Emit(OpCodes.Stfld, structField);
                        }

                        if(varType.IsValueType)
                        {
                            copycb_ILGen.Emit(OpCodes.Ldarg_0);
                            copycb_ILGen.Emit(OpCodes.Ldarg_1);
                            copycb_ILGen.Emit(OpCodes.Ldfld, structField);
                            copycb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                        else
                        {
                            copycb_ILGen.Emit(OpCodes.Ldarg_0);
                            copycb_ILGen.Emit(OpCodes.Ldarg_1);
                            copycb_ILGen.Emit(OpCodes.Ldfld, structField);
                            if (compileState.IsCloneOnAssignment(varType))
                            {
                                copycb_ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(varType));
                            }
                            copycb_ILGen.Emit(OpCodes.Stfld, structField);
                        }
                    }
                    defcb_ILGen.Emit(OpCodes.Ret);
                    copycb_ILGen.Emit(OpCodes.Ret);
                    compileState.m_StructTypes.Add(kvp.Key, structTypeBuilder.CreateType());
                }
                #endregion

                #region Globals generation
                AddProperties(compileState, typeLocalsInited);
                foreach (KeyValuePair<string, Type> variableKvp in compileState.m_VariableDeclarations)
                {
                    if (dumpILGen != null)
                    {
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("DefineField(\"{0}\", typeof({1}))", variableKvp.Key, variableKvp.Value.FullName);
                    }
                    FieldAttributes fieldAttr = FieldAttributes.Public;
                    if(compileState.m_VariableConstantDeclarations.ContainsKey(variableKvp.Key))
                    {
                        fieldAttr |= FieldAttributes.InitOnly;
                    }

                    FieldBuilder fb = compileState.LanguageExtensions.EnableStateVariables ?
                        scriptTypeBuilder.DefineField("var_glob_" + variableKvp.Key, variableKvp.Value, fieldAttr) :
                        scriptTypeBuilder.DefineField("var_" + variableKvp.Key, variableKvp.Value, fieldAttr);
                    compileState.m_VariableFieldInfo[variableKvp.Key] = fb;
                    typeLocalsInited[variableKvp.Key] = fb;
                }
                foreach(KeyValuePair<string, Dictionary<string, Type>> stateVariableKvp in compileState.m_StateVariableDeclarations)
                {
                    foreach (KeyValuePair<string, Type> variableKvp in stateVariableKvp.Value)
                    {
                        if (dumpILGen != null)
                        {
                            dumpILGen.WriteLine("********************************************************************************");
                            dumpILGen.WriteLine("State[{2}].DefineField(\"{0}\", typeof({1}))", variableKvp.Key, variableKvp.Value.FullName, stateVariableKvp.Key);
                        }
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
                var script_ILGen = new ILGenDumpProxy(
                    script_ilgen,
                    compileState.DebugDocument,
                    dumpILGen);
                var reset_ILGen = new ILGenDumpProxy(
                    reset_ilgen,
                    compileState.DebugDocument,
                    dumpILGen);
                compileState.ILGen = script_ILGen;
                foreach(KeyValuePair<string, FieldBuilder> kvp in compileState.m_VariableFieldInfo)
                {
                    if (compileState.ILGen.HaveDebugOut)
                    {
                        compileState.ILGen.WriteLine("-- Init var " + kvp.Key);
                    }
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
                            ConstructorInfo cInfo = compileState.GetDefaultConstructor(fb.FieldType);
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
                            ConstructorInfo cInfo = compileState.GetDefaultConstructor(fb.FieldType);
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
                        expressionTree = LineToExpressionTree(compileState, initargs.Line, typeLocals.Keys, compileState.CurrentCulture);

                        if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree, varName, initargs.FirstTokenLineNumber))
                        {
                            dumpILGen?.WriteLine("-- Init var " + varName);

                            compileState.ILGen = script_ILGen;
                            compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            ResultIsModifiedEnum modified = ProcessExpression(
                                compileState,
                                fb.FieldType,
                                expressionTree,
                                typeLocals);
                            if (modified == ResultIsModifiedEnum.Yes)
                            {
                                /* skip operation as it is modified */
                            }
                            else if (fb.FieldType == typeof(AnArray) || compileState.IsCloneOnAssignment(fb.FieldType))
                            {
                                /* keep LSL semantics valid */
                                compileState.ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(fb.FieldType));
                            }
                            compileState.ILGen.Emit(OpCodes.Stfld, fb);

                            if(!fb.IsInitOnly)
                            {
                                compileState.ILGen = reset_ILGen;
                                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                                modified = ProcessExpression(
                                    compileState,
                                    fb.FieldType,
                                    expressionTree,
                                    typeLocals);
                                if (modified == ResultIsModifiedEnum.Yes)
                                {
                                    /* skip operation as it is modified */
                                }
                                else if (fb.FieldType == typeof(AnArray) || compileState.IsCloneOnAssignment(fb.FieldType))
                                {
                                    /* keep LSL semantics valid */
                                    compileState.ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(fb.FieldType));
                                }
                                compileState.ILGen.Emit(OpCodes.Stfld, fb);
                            }
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
                        List<TokenInfo> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        string functionName = functionDeclaration[1];
                        int functionStart = 3;
                        bool isRpcAccessibleFromOtherLinkset = false;
                        bool isRpcAccessibleByEveryone = false;
                        bool isRpcAccessibleByGroup = false;
                        string functionPrefix = "fn_";

                        if(functionDeclaration[0] == "timer" && compileState.LanguageExtensions.EnableNamedTimers)
                        {
                            if (functionDeclaration[1] == "(")
                            {
                                if(functionDeclaration[3] != ")")
                                {
                                    throw new CompilerException(functionDeclaration[3].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidFunctionDeclaration", "Invalid function declaration"));
                                }
                                functionName = functionDeclaration[2];
                                functionStart = 6;
                            }

                            functionPrefix = "timerfn_";
                            returnType = typeof(void);
                        }
                        else if (functionDeclaration[0] == "extern" && compileState.LanguageExtensions.EnableExtern)
                        {
                            if (functionDeclaration[functionDeclaration.Count - 1] == ";")
                            {
                                functionStart = 2;
                                while (functionDeclaration[functionStart] != ")")
                                {
                                    ++functionStart;
                                }
                                ++functionStart;
                                functionName = functionDeclaration[functionStart++];
                                if (functionDeclaration[functionStart] == "=")
                                {
                                    functionStart += 2;
                                }
                                ++functionStart;
                            }
                            else
                            {
                                if (functionDeclaration[1] == "(")
                                {
                                    /* parse flags */
                                    int externPos = 1;
                                    while (functionDeclaration[externPos++] != ")")
                                    {
                                        switch(functionDeclaration[externPos])
                                        {
                                            case "public":
                                                isRpcAccessibleFromOtherLinkset = true;
                                                break;

                                            case "private":
                                                isRpcAccessibleFromOtherLinkset = false;
                                                break;

                                            case "everyone":
                                                isRpcAccessibleByEveryone = true;
                                                isRpcAccessibleByGroup = false;
                                                break;

                                            case "owner":
                                                isRpcAccessibleByEveryone = false;
                                                isRpcAccessibleByGroup = false;
                                                break;

                                            case "group":
                                                isRpcAccessibleByGroup = true;
                                                isRpcAccessibleByEveryone = false;
                                                break;

                                            default:
                                                throw new CompilerException(functionDeclaration[externPos].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidFunctionDeclaration", "Invalid function declaration"));
                                        }
                                        ++externPos;
                                    }
                                    functionName = functionDeclaration[externPos];
                                    functionStart = externPos + 2;
                                }

                                functionPrefix = "rpcfn_";
                            }
                            returnType = typeof(void);
                        }
                        else if (!compileState.ApiInfo.Types.TryGetValue(functionDeclaration[0], out returnType))
                        {
                            functionName = functionDeclaration[0];
                            functionStart = 2;
                            returnType = typeof(void);
                        }
                        if(functionDeclaration[functionStart] == "this" &&
                            functionDeclaration[functionStart + 1] != ")")
                        {
                            /* special designator for custom member methods */
                            ++functionStart;
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
                                throw new CompilerException(functionDeclaration[functionStart - 1].LineNumber, "Internal Error");
                            }
                            paramTypes.Add(paramType);
                            paramName.Add(functionDeclaration[functionStart++]);
                        }

                        if (dumpILGen != null)
                        {
                            dumpILGen.WriteLine("********************************************************************************");
                            dumpILGen.WriteLine("DefineMethod(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, string.Join(",", from p in paramTypes select p.FullName));
                        }
                        method = scriptTypeBuilder.DefineMethod(functionPrefix + functionName, MethodAttributes.Public, returnType, paramTypes.ToArray());
                        if(isRpcAccessibleFromOtherLinkset)
                        {
                            CustomAttributeBuilder attrBuilder = new CustomAttributeBuilder(typeof(RpcLinksetExternalCallAllowedAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                            method.SetCustomAttribute(attrBuilder);
                        }
                        if (isRpcAccessibleByEveryone)
                        {
                            CustomAttributeBuilder attrBuilder = new CustomAttributeBuilder(typeof(RpcLinksetExternalCallEveryoneAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                            method.SetCustomAttribute(attrBuilder);
                        }
                        if(isRpcAccessibleByGroup)
                        {
                            CustomAttributeBuilder attrBuilder = new CustomAttributeBuilder(typeof(RpcLinksetExternalCallSameGroupAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
                            method.SetCustomAttribute(attrBuilder);
                        }
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

                        Type returnType;
                        List<TokenInfo> functionDeclaration = funcInfo.FunctionLines[0].Line;
                        string functionName = functionDeclaration[1];
                        int functionStart = 3;
                        bool isExternCall = false;
                        List<TokenInfo> externDefinition = null;
                        bool requiresSerializable = functionDeclaration[0] == "extern";
                        string aliasedFunctionName = string.Empty;

                        if (functionDeclaration[0] == "timer" && compileState.LanguageExtensions.EnableNamedTimers)
                        {
                            if (functionDeclaration[1] == "(")
                            {
                                if (functionDeclaration[3] != ")")
                                {
                                    throw new CompilerException(functionDeclaration[3].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidFunctionDeclaration", "Invalid function declaration"));
                                }
                                functionName = functionDeclaration[2];
                                functionStart = 6;
                            }

                            returnType = typeof(void);
                        }
                        else if (requiresSerializable && compileState.LanguageExtensions.EnableExtern)
                        {
                            returnType = typeof(void);
                            isExternCall = functionDeclaration[functionDeclaration.Count - 1] == ";";
                            if(isExternCall || functionDeclaration[1] == "(")
                            {
                                functionStart = 2;
                                externDefinition = new List<TokenInfo>();
                                while (functionDeclaration[functionStart] != ")")
                                {
                                    externDefinition.Add(functionDeclaration[functionStart++]);
                                    if (functionDeclaration[functionStart] == ",")
                                    {
                                        ++functionStart;
                                    }
                                }
                                ++functionStart;
                                functionName = functionDeclaration[functionStart++];
                                aliasedFunctionName = functionName;
                                if(functionDeclaration[functionStart] == "=")
                                {
                                    aliasedFunctionName = functionDeclaration[++functionStart];
                                    ++functionStart;
                                }
                                ++functionStart;
                            }
                        }
                        else if(!compileState.ApiInfo.Types.TryGetValue(functionDeclaration[0], out returnType))
                        {
                            functionName = functionDeclaration[0];
                            functionStart = 2;
                            returnType = typeof(void);
                        }

                        if(functionDeclaration[functionStart] == "this" &&
                            functionDeclaration[functionStart + 1] != ")")
                        {
                            /* member capable function */
                            ++functionStart;
                            List<FunctionInfo> memberFunctions;
                            if(!compileState.m_MemberFunctions.TryGetValue(functionKvp.Key, out memberFunctions))
                            {
                                memberFunctions = new List<FunctionInfo>();
                                compileState.m_MemberFunctions.Add(functionKvp.Key, memberFunctions);
                            }
                            memberFunctions.Add(funcInfo);
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
                                throw new CompilerException(functionDeclaration[functionStart - 1].LineNumber, "Internal Error");
                            }
                            if(requiresSerializable && 
                                (paramType != typeof(AnArray) && paramType != typeof(LSLKey) && paramType.GetCustomAttribute(typeof(SerializableAttribute)) == null))
                            {
                                throw new CompilerException(functionDeclaration[functionStart - 1].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "RpcCallParameterTypeMustBeSerializable", "RPC call parameter type must be serializable."));
                            }
                            paramTypes.Add(paramType);
                            paramName.Add(functionDeclaration[functionStart++]);
                        }

                        if (dumpILGen != null)
                        {
                            dumpILGen.WriteLine("********************************************************************************");
                            dumpILGen.WriteLine("GenerateMethodIL.Begin(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, string.Join(",", from p in paramTypes select p.FullName));
                        }

                        var method_ilgen = new ILGenDumpProxy(
                            method.GetILGenerator(),
                            compileState.DebugDocument,
                            dumpILGen);
                        typeLocals = new Dictionary<string, object>(typeLocalsInited);
                        if (isExternCall)
                        {
                            /* generate special call process */
                            ProcessExternFunction(compileState, scriptTypeBuilder, method_ilgen, externDefinition, aliasedFunctionName, paramTypes, functionDeclaration[0].LineNumber, typeLocals);
                        }
                        else
                        {
                            ProcessFunction(compileState, scriptTypeBuilder, null, method, method_ilgen, funcInfo.FunctionLines, typeLocals);
                        }

                        dumpILGen?.WriteLine("GenerateMethodIL.End(\"{0}\", returnType=typeof({1}), new Type[] {{{2}}})", functionName, returnType.FullName, string.Join(",", from p in paramTypes select p.FullName));
                    }
                }
                #endregion

                #region State compilation
                if(!compileState.m_States.ContainsKey("default"))
                {
                    throw new CompilerException(1, this.GetLanguageString(compileState.CurrentCulture, "NoDefaultStateDefined", "No default state defined."));
                }
                foreach (KeyValuePair<string, Dictionary<string, List<LineInfo>>> stateKvp in compileState.m_States)
                {
                    FieldBuilder fb;
                    if (dumpILGen != null)
                    {
                        dumpILGen.WriteLine("********************************************************************************");
                        dumpILGen.WriteLine("DefineState(\"{0}\")", stateKvp.Key);
                    }
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
                            compileState.DebugDocument,
                            dumpILGen);
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
                        if (dumpILGen != null)
                        {
                            dumpILGen.WriteLine("********************************************************************************");
                            dumpILGen.WriteLine("DefineEvent(\"{0}\")", eventKvp.Key);
                        }
                        MethodBuilder eventbuilder = state.DefineMethod(
                            eventKvp.Key,
                            MethodAttributes.Public,
                            typeof(void),
                            paramtypes);
                        var event_ilgen = new ILGenDumpProxy(
                            eventbuilder.GetILGenerator(),
                            compileState.DebugDocument,
                            dumpILGen);
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
                        dumpILGen?.WriteLine("DefineEvent.ILGenEnd(\"{0}\")", eventKvp.Key);
                    }

                    stateTypes.Add(stateKvp.Key, state.CreateType());
                }
                #endregion

                script_ilgen.Emit(OpCodes.Ldarg_0);
                script_ilgen.Emit(OpCodes.Call, typeof(Script).GetMethod("UpdateScriptState", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
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
            }
            finally
            {
                dumpILGen?.Dispose();
            }
        }

        private void StateEntryInitVars(string stateName, CompileState compileState, Dictionary<string, object> typeLocalIn)
        {
            compileState.ILGen.WriteLine("StateEntryInitVars: begin");
            Dictionary<string, FieldBuilder> stateVars;
            Dictionary<string, LineInfo> stateVarInitValues;
            if(!compileState.m_StateVariableFieldInfo.TryGetValue(stateName, out stateVars))
            {
                compileState.ILGen.WriteLine("StateEntryInitVars: end");
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
                        expressionTree = LineToExpressionTree(compileState, initargs.Line, typeLocals.Keys, compileState.CurrentCulture);
                    }
                    catch (Exception e)
                    {
                        throw new CompilerException(initargs.FirstTokenLineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "InitValueOfVariable0HasSyntaxError", "Init value of state variable {0} has syntax error. {1}\n{2}"), varName, e.Message, e.StackTrace));
                    }

                    if (AreAllVarReferencesSatisfied(compileState, varIsInited, expressionTree, varName, initargs.FirstTokenLineNumber))
                    {
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("-- Init state var " + varName);
                        }
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                        ResultIsModifiedEnum modified = ProcessExpression(
                            compileState,
                            fb.FieldType,
                            expressionTree,
                            typeLocals);
                        if (modified == ResultIsModifiedEnum.Yes)
                        {
                            /* skip operation as it is modified */
                        }
                        else if (fb.FieldType == typeof(AnArray) || compileState.IsCloneOnAssignment(fb.FieldType))
                        {
                            /* keep LSL semantics valid */
                            compileState.ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(fb.FieldType));
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
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Ldc_I8, 0L);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Ldstr, string.Empty);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Vector3))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    FieldInfo sfld = typeof(Vector3).GetField("Zero");
                    compileState.ILGen.Emit(OpCodes.Ldsfld, sfld);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(Quaternion))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    FieldInfo sfld = typeof(Quaternion).GetField("Identity");
                    compileState.ILGen.Emit(OpCodes.Ldsfld, sfld);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if(fb.FieldType.IsClass)
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    ConstructorInfo ci = fb.FieldType.GetConstructor(Type.EmptyTypes);
                    compileState.ILGen.Emit(OpCodes.Newobj, ci);
                    compileState.ILGen.Emit(OpCodes.Stfld, fb);

                    varIsInited.Add(varName);
                }
                else if (fb.FieldType.IsValueType)
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    compileState.ILGen.Emit(OpCodes.Ldflda, fb);
                    compileState.ILGen.Emit(OpCodes.Initobj, fb.FieldType);

                    varIsInited.Add(varName);
                }
            }
            compileState.ILGen.WriteLine("StateEntryInitVars: end");
        }

        private bool AreAllVarReferencesSatisfied(CompileState cs, List<string> initedVars, Tree expressionTree, string varToInit, int lineNumber)
        {
            foreach (Tree st in expressionTree.SubTree)
            {
                if (!AreAllVarReferencesSatisfied(cs, initedVars, st, varToInit, lineNumber))
                {
                    return false;
                }
                else if ((st.Type == Tree.EntryType.Variable || st.Type == Tree.EntryType.Unknown) &&
                    cs.m_VariableDeclarations.ContainsKey(st.Entry) &&
                    !initedVars.Contains(st.Entry))
                {
                    if(st.Entry == varToInit)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(cs.CurrentCulture, "Variable0HasACircularReference", "Variable '{0}' references itself on initialization."), st.Entry));
                    }
                    return !cs.m_VariableInitValues.ContainsKey(st.Entry);
                }
            }
            return true;
        }
    }
}
