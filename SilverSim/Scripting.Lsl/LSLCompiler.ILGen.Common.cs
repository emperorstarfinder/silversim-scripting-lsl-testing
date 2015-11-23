// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {

        public sealed class LSLScriptAssembly : IScriptAssembly
        {
            public readonly Assembly Assembly;
            readonly Type m_ScriptType;
            readonly Dictionary<string, Type> m_StateTypes;
            readonly bool m_ForcedSleep;

            public LSLScriptAssembly(Assembly assembly, Type script, Dictionary<string, Type> stateTypes, bool forcedSleep)
            {
                Assembly = assembly;
                m_ScriptType = script;
                m_StateTypes = stateTypes;
                m_ForcedSleep = forcedSleep;
            }

            public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item)
            {
                ConstructorInfo scriptconstructor = m_ScriptType.
                    GetConstructor(new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) });
                Script m_Script = (Script)scriptconstructor.Invoke(new object[3] { objpart, item, m_ForcedSleep });

                foreach (KeyValuePair<string, Type> t in m_StateTypes)
                {
                    ConstructorInfo info = t.Value.GetConstructor(new Type[1] { m_ScriptType });
                    object[] param = new object[1];
                    param[0] = m_Script;
                    m_Script.AddState(t.Key, (ILSLState)info.Invoke(param));
                }

                return m_Script;
            }
        }

        internal sealed class ILParameterInfo
        {
            public int Position;
            public Type ParameterType;

            public ILParameterInfo(Type type, int position)
            {
                ParameterType = type;
                Position = position;
            }
        }

        internal sealed class ILLabelInfo
        {
            public Label Label;
            public bool IsDefined;
            public List<int> UsedInLines = new List<int>();

            public ILLabelInfo(Label label, bool isDefined)
            {
                Label = label;
                IsDefined = isDefined;
            }
        }

        #region LSL Integer Overflow
        /* special functions for converts
         * 
         * Integer Overflow
         * The compiler treats integers outside the range -2147483648 to 2147483647 somewhat strangely. No compile time warning or error is generated. (If the following explanation, doesn't make sense to you don't worry -- just know to avoid using numbers outside the valid range in your script.)

         * - For an integer outside the range -2147483648 to 2147483647, the absolute value of the number is reduced to fall in the range 0 to 4294967295 (0xFFFFFFFF).
         * - This number is then parsed as an unsigned 32 bit integer and cast to the corresponding signed integer.
         * - If the value in the script had a negative sign, the sign of the internal representation is switched.
         * - The net effect is that very large positive numbers get mapped to -1 and very large negative numbers get mapped to 1.
         */

        public static int ConvToInt(double v)
        {
            try
            {
                return (int)v;
            }
            catch
            {
                if (v > 0)
                {
                    try
                    {
                        return (int)((uint)v);
                    }
                    catch
                    {
                        return -1;
                    }
                }
                else
                {
                    try
                    {
                        return (int)-((uint)v);
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static int ConvToInt(string v)
        {
            if (v.ToLower().StartsWith("0x"))
            {
                try
                {
                    return (int)uint.Parse(v.Substring(2), NumberStyles.HexNumber);
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                try
                {
                    return int.Parse(v);
                }
                catch
                {
                    try
                    {
                        if (v.StartsWith("-"))
                        {
                            try
                            {
                                return -((int)uint.Parse(v.Substring(1)));
                            }
                            catch
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            try
                            {
                                return (int)uint.Parse(v.Substring(1));
                            }
                            catch
                            {
                                return -1;
                            }
                        }
                    }
                    catch
                    {
                        /* LSL specifics here */
                        return v.StartsWith("-") ? 1 : -1;
                    }
                }
            }
        }

        public static int LSL_IntegerMultiply(int a, int b)
        {
            long c = (long)a * b;
            if(c >= UInt32.MaxValue)
            {
                c = -1;
            }
            else if(c <= -(long)UInt32.MaxValue)
            {
                c = 1;
            }
            else if(c > Int32.MaxValue && c < UInt32.MaxValue)
            {
                c = (int)(uint)c;
            }
            else if(c < Int32.MinValue && c < -(long)UInt32.MaxValue)
            {
                c = -(int)(uint)c;
            }
            return (int)c;
        }

        public static int LSL_IntegerDivision(int a, int b)
        {
            return (a == -2147483648 && b == -1) ?
                -2147483648 :
                a / b;
        }

        public static int LSL_IntegerModulus(int a, int b)
        {
            return (a == -2147483648 && b == -1) ?
                0 :
                a / b;
        }
        #endregion

        #region Preprocessor for concatenated string constants
        void CollapseStringConstants(List<string> args)
        {
            for (int pos = 1; pos < args.Count - 2; ++pos)
            {
                if (args[pos] == "+" && args[pos - 1].StartsWith("\"") && args[pos + 1].StartsWith("\""))
                {
                    args[pos - 1] = args[pos - 1] + args[pos + 1];
                    args.RemoveAt(pos);
                    args.RemoveAt(pos);
                    --pos;
                }
            }
        }
        #endregion

        void CombineTypecastArguments(List<string> args)
        {
            for (int pos = 0; pos < args.Count - 2; ++pos)
            {
                if (args[pos] == "(" && m_Typecasts.Contains(args[pos + 1]) && args[pos + 2] == ")")
                {
                    args[pos] = "(" + args[pos + 1] + ")";
                    args.RemoveAt(pos + 1);
                    args.RemoveAt(pos + 1);
                }
            }
        }

        /*  Process important things before solving operators */
        void PreprocessLine(List<string> args)
        {
            CombineTypecastArguments(args);
            CollapseStringConstants(args);
        }

        #region Type validation and string representation
        internal static bool IsValidType(Type t)
        {
            if (t == typeof(string)) return true;
            if (t == typeof(int)) return true;
            if (t == typeof(double)) return true;
            if (t == typeof(LSLKey)) return true;
            if (t == typeof(Quaternion)) return true;
            if (t == typeof(Vector3)) return true;
            if (t == typeof(AnArray)) return true;
            if (t == typeof(void)) return true;
            return false;
        }
        internal static string MapType(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "integer";
            if (t == typeof(double)) return "float";
            if (t == typeof(LSLKey)) return "key";
            if (t == typeof(Quaternion)) return "rotation";
            if (t == typeof(Vector3)) return "vector";
            if (t == typeof(AnArray)) return "list";
            if (t == typeof(void)) return "void";
            return "???";
        }
        #endregion

        #region Typecasting IL Generator
        internal static void ProcessImplicitCasts(
            CompileState compileState,
            Type toType, 
            Type fromType, 
            int lineNumber)
        {
            if (fromType == toType)
            {

            }
            else if (toType == typeof(void))
            {
            }
            else if (fromType == typeof(string) && toType == typeof(LSLKey))
            {

            }
            else if (fromType == typeof(LSLKey) && toType == typeof(string))
            {

            }
            else if (fromType == typeof(int) && toType == typeof(double))
            {

            }
            else if (toType == typeof(AnArray))
            {

            }
            else if (toType == typeof(bool))
            {

            }
            else if(null == fromType)
            {
                throw new CompilerException(lineNumber, "Internal Error! fromType is not set");
            }
            else if (null == toType)
            {
                throw new CompilerException(lineNumber, "Internal Error! toType is not set");
            }
            else if (!IsValidType(fromType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", fromType.FullName));
            }
            else if (!IsValidType(toType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", toType.FullName));
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("Unsupported implicit typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
            ProcessCasts(compileState, toType, fromType, lineNumber);
        }

        public static double ParseStringToDouble(string input)
        {
            double v;
            if(!Double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                v = 0;
            }
            return v;
        }

        public static Vector3 ParseStringToVector(string input)
        {
            Vector3 v;
            if(!Vector3.TryParse(input, out v))
            {
                v = Vector3.Zero;
            }
            return v;
        }

        public static Quaternion ParseStringToQuaternion(string input)
        {
            Quaternion q;
            if(!Quaternion.TryParse(input, out q))
            {
                q = Quaternion.Identity;
            }
            return q;
        }

        internal static void ProcessCasts(
            CompileState compileState,
            Type toType,
            Type fromType,
            int lineNumber)
        {
            /* value is on stack before */
            if (toType == fromType)
            {
            }
            else if (toType == typeof(void))
            {
                compileState.ILGen.Emit(OpCodes.Pop);
            }
            else if (fromType == typeof(void))
            {
                throw new CompilerException(lineNumber, string.Format("function does not return anything"));
            }
            else if (toType == typeof(LSLKey))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("function does not return anything"));
                }
            }
            else if (toType == typeof(string))
            {
                if (fromType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(string).GetMethod("ToString", Type.EmptyTypes));
                }
                else if (fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(double).GetMethod("ToString", Type.EmptyTypes));
                }
                else if (fromType == typeof(Vector3))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(Vector3).GetMethod("ToString", Type.EmptyTypes));
                }
                else if (fromType == typeof(Quaternion))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(Quaternion).GetMethod("ToString", Type.EmptyTypes));
                }
                else if (fromType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("ToString", Type.EmptyTypes));
                }
                else if (fromType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(int))
            {
                /* yes, we need special handling for conversion of string to integer or float to integer. (see section about Integer Overflow) */
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else if (fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(bool))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, 0f);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("Count").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(Quaternion))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(Vector3))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(Vector3).GetProperty("Length").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, 0f);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(double))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToDouble", new Type[] { typeof(string) }));
                }
                else if (fromType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Conv_R8);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Vector3))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToVector", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Quaternion))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToQuaternion", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(AnArray))
            {
                if (fromType == typeof(string) || fromType == typeof(int) || fromType == typeof(double))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { fromType }));
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.EndScope();
                }
                else if (fromType == typeof(Vector3) || fromType == typeof(Quaternion) || fromType == typeof(LSLKey))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.EndScope();
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
        }
        #endregion

        #region Variable Access IL Generator
        internal static Type GetVarType(
            object v)
        {
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            ilpi = v as ILParameterInfo;
            if (null != ilpi)
            {
                return ilpi.ParameterType;
            }

            lb = v as LocalBuilder;
            if (null != lb)
            {
                return lb.LocalType;
            }

            fb = v as FieldBuilder;
            if (null != fb)
            {
                return fb.FieldType;
            }

            fi = v as FieldInfo;
            if (null != fi)
            {
                return fi.FieldType;
            }

            throw new NotSupportedException();
        }

        static Type EmitFieldRead(
            CompileState compileState,
            FieldInfo fi)
        {
            if ((fi.Attributes & FieldAttributes.Literal) != 0)
            {
                if (fi.FieldType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I4, (int)fi.GetValue(null));
                }
                else if (fi.FieldType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)fi.GetValue(null));
                }
                else if(fi.FieldType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Ldstr, (string)fi.GetValue(null));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if ((fi.Attributes & FieldAttributes.Static) != 0)
            {
                compileState.ILGen.Emit(OpCodes.Ldsfld, fi);
            }
            else
            {
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldfld, fi);
            }
            return fi.FieldType;
        }

        internal static Type GetVarToStack(
            CompileState compileState,
            object v)
        {
            Type retType;
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            if (null != (ilpi = v as ILParameterInfo))
            {
                compileState.ILGen.Emit(OpCodes.Ldarg, ilpi.Position);
                retType = ilpi.ParameterType;
            }
            else if (null != (lb = v as LocalBuilder))
            {
                compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                retType = lb.LocalType;
            }
            else if (null != (fb = v as FieldBuilder))
            {
                retType = EmitFieldRead(
                    compileState,
                    fb);
            }
            else if (null != (fi = v as FieldInfo))
            {
                retType = EmitFieldRead(
                    compileState,
                    fi);
            }
            else
            {
                throw new NotSupportedException();
            }
            if (retType == typeof(AnArray))
            {
                /* list has deep copying */
                compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { retType }));
            }
            return retType;
        }

        internal static void SetVarFromStack(
            CompileState compileState,
            object v,
            int lineNumber)
        {
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            ilpi = v as ILParameterInfo;
            if (null != ilpi)
            {
                compileState.ILGen.Emit(OpCodes.Starg, ilpi.Position);
                return;
            }

            lb = v as LocalBuilder;
            if (null != lb)
            {
                compileState.ILGen.Emit(OpCodes.Stloc, lb);
                return;
            }

            fb = v as FieldBuilder;
            if (null != fb)
            {
                if ((fb.Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
                compileState.ILGen.BeginScope();
                LocalBuilder swapLb = compileState.ILGen.DeclareLocal(fb.FieldType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                compileState.ILGen.Emit(OpCodes.Stfld, fb);
                compileState.ILGen.EndScope();
                return;
            }

            fi = v as FieldInfo;
            if (null != fi)
            {
                if ((fi.Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
                compileState.ILGen.BeginScope();
                LocalBuilder swapLb = compileState.ILGen.DeclareLocal(fi.FieldType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                compileState.ILGen.Emit(OpCodes.Stfld, fi);
                compileState.ILGen.EndScope();
                return;
            }

            throw new NotSupportedException();
        }
        #endregion

        #region Constants collector for IL Generator
        Dictionary<string, object> AddConstants(CompileState compileState, TypeBuilder typeBuilder, ILGenerator ilgen)
        {
            Dictionary<string, object> localVars = new Dictionary<string, object>();
            foreach(KeyValuePair<string, FieldInfo> kvp in compileState.ApiInfo.Constants)
            {
                FieldInfo f = kvp.Value;
                if ((f.Attributes & FieldAttributes.Static) != 0)
                {
                    if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                    {
                        localVars[kvp.Key] = f;
                    }
                    else
                    {
                        m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", kvp.Key, f.Attributes.ToString());
                    }
                }

            }
            return localVars;
        }
        #endregion
    }
}
