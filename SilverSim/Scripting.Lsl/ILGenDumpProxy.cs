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

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable RCS1163

namespace SilverSim.Scripting.Lsl
{
    public class ILGenDumpProxy
    {
        private readonly ILGenerator m_ILGen;
        private readonly StreamWriter Writer;
        private readonly List<Label> m_DefinedLabels = new List<Label>();
        public bool GeneratedRetAtLast;
        public ISymbolDocumentWriter DebugDocument;

        public sealed class NopNull
        {
        }

        public ILGenDumpProxy(
            ILGenerator ilgen,
            ISymbolDocumentWriter debugDocument,
            StreamWriter textWriter)
        {
            m_ILGen = ilgen;
            DebugDocument = debugDocument;
            Writer = textWriter;
        }

        public int ILOffset => m_ILGen.ILOffset;

        public bool HaveDebugOut => Writer != null;
        public void WriteLine(string fmt, params object[] p) => Writer?.WriteLine(fmt, p);

        public void BeginCatchBlock(Type exceptionType, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginCatchBlock(typeof({0}))\n    ---------- {1}:{3}=>{2}", exceptionType.FullName, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.BeginCatchBlock(exceptionType);
        }

        public void BeginExceptFilterBlock(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginExceptFilterBlock()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.BeginExceptFilterBlock();
        }

        public Label BeginExceptionBlock(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginExceptionBlock()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            return m_ILGen.BeginExceptionBlock();
        }

        public void BeginFaultBlock(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginFaultBlock()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.BeginFaultBlock();
        }

        public void BeginFinallyBlock(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginFinallyBlock()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.BeginFinallyBlock();
        }

        public void BeginScope(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("BeginScope()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.BeginScope();
        }

        public LocalBuilder DeclareLocal(Type localType, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType);
            Writer?.WriteLine("DeclareLocal(typeof({0})) = {1}\n    ---------- {2}:{4}=>{3}", localType.FullName, lb.LocalIndex, callerFilePath, callerMemberName, callerLineNumber);
            return lb;
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType, pinned);
            Writer?.WriteLine("DeclareLocal(typeof({0}), {1}) = {2}\n    ---------- {3}:{5}=>{4}", localType.FullName, pinned.ToString(), lb.LocalIndex, callerFilePath, callerMemberName, callerLineNumber);
            return lb;
        }

        public Label DefineLabel(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Label lb = m_ILGen.DefineLabel();
            Writer?.WriteLine("DefineLabel() = {0}\n    ---------- {1}:{3}=>{2}", lb.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_DefinedLabels.Add(lb);
            return lb;
        }

        public void Emit(OpCode opcode, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0})\n    ---------- {1}:{3}=>{2}", opcode.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode);
            GeneratedRetAtLast = opcode == OpCodes.Ret;
        }

        public void Emit(OpCode opcode, byte arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, ConstructorInfo con, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), con.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, con);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, double arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, FieldInfo field, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), field.Name, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, field);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, float arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, int arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, Label label, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), label.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, label);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, Label[] labels, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, [])\n    ---------- {1}:{3}=>{2}", opcode.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, labels);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, LocalBuilder local, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), local.LocalIndex, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, local);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, long arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, MethodInfo meth, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1}) \n    ---------- {2}:{4}=>{3}", opcode.ToString(), meth.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, meth);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, sbyte arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, short arg, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), arg, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, SignatureHelper signature, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, {1})\n    ---------- {2}:{4}=>{3}", opcode.ToString(), signature.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, signature);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, string str, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, \"{1}\")\n    ---------- {2}:{4}=>{3}", opcode.ToString(), str, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, str);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, Type cls, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("Emit(OpCodes.{0}, typeof({1}))\n    ---------- {2}:{4}=>{3}", opcode.ToString(), cls.FullName, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.Emit(opcode, cls);
            GeneratedRetAtLast = false;
        }

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitCall(OpCodes.{0}, {1}, [])\n    ---------- {2}:{4}=>{3}", opcode.ToString(), methodInfo.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitCall(opcode, methodInfo, optionalParameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...])\n    ---------- {3}:{5}=>{4}", opcode.ToString(), unmanagedCallConv.ToString(), returnType.FullName, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...], [...])\n    ---------- {3}:{5}=>{4}", opcode.ToString(), callingConvention.ToString(), returnType.FullName, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitWriteLine(FieldInfo fld, [CallerFilePath]string callerFilePath = null, NopNull nop = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitWriteLine({0})\n    ---------- {0}:{2}=>{1}", fld.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitWriteLine(fld);
        }

        public void EmitWriteLine(LocalBuilder localBuilder, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitWriteLine({0})\n    ---------- {0}:{2}=>{1}", localBuilder.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitWriteLine(localBuilder);
        }

        public void EmitWriteLine(string value, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EmitWriteLine({0})\n    ---------- {0}:{2}=>{1}", value, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EmitWriteLine(value);
        }

        public void EndExceptionBlock(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EndExceptionBlock()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EndExceptionBlock();
        }

        public void EndScope(NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("EndScope()\n    ---------- {0}:{2}=>{1}", callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.EndScope();
        }

        public void MarkLabel(Label loc, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            m_DefinedLabels.Remove(loc);
            Writer?.WriteLine("MarkLabel({0})\n    ---------- {1}:{3}=>{2}", loc.ToString(), callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.MarkLabel(loc);
            GeneratedRetAtLast = false;
        }

        public void MarkSequencePoint(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (DebugDocument != null)
            {
                m_ILGen.MarkSequencePoint(DebugDocument, startLine, startColumn, endLine, endColumn);
            }
        }

        public void ThrowException(Type excType, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("ThrowException(typeof({0}))\n    ---------- {1}:{3}=>{2}", excType.FullName, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.ThrowException(excType);
            GeneratedRetAtLast = false;
        }

        public void UsingNamespace(string usingNamespace, NopNull nop = null, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string callerMemberName = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            Writer?.WriteLine("UsingNamespace(\"{0}\")\n    ---------- {1}:{3}=>{2}", usingNamespace, callerFilePath, callerMemberName, callerLineNumber);
            m_ILGen.UsingNamespace(usingNamespace);
        }
    }
}
