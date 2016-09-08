// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace SilverSim.Scripting.Lsl
{
    public class ILGenDumpProxy
    {
        ILGenerator m_ILGen;
#if DEBUG
        public readonly StreamWriter Writer;
#endif
        List<Label> m_DefinedLabels = new List<Label>();
        public bool GeneratedRetAtLast;

        public ILGenDumpProxy(ILGenerator ilgen
#if DEBUG
            , StreamWriter textWriter
#endif
            )
        {
            m_ILGen = ilgen;
#if DEBUG
            Writer = textWriter;
#endif
        }

        public int ILOffset 
        { 
            get
            {
                return m_ILGen.ILOffset;
            }
        }

        public void BeginCatchBlock(Type exceptionType)
        {
#if DEBUG
            Writer.WriteLine("BeginCatchBlock(typeof({0}))", exceptionType.FullName);
#endif
            m_ILGen.BeginCatchBlock(exceptionType);
        }

        public void BeginExceptFilterBlock()
        {
#if DEBUG
            Writer.WriteLine("BeginExceptFilterBlock()");
#endif
            m_ILGen.BeginExceptFilterBlock();
        }

        public Label BeginExceptionBlock()
        {
#if DEBUG
            Writer.WriteLine("BeginExceptionBlock()");
#endif
            return m_ILGen.BeginExceptionBlock();
        }

        public void BeginFaultBlock()
        {
#if DEBUG
            Writer.WriteLine("BeginFaultBlock()");
#endif
            m_ILGen.BeginFaultBlock();
        }

        public void BeginFinallyBlock()
        {
#if DEBUG
            Writer.WriteLine("BeginFinallyBlock()");
#endif
            m_ILGen.BeginFinallyBlock();
        }

        public void BeginScope()
        {
#if DEBUG
            Writer.WriteLine("BeginScope()");
#endif
            m_ILGen.BeginScope();
        }

        public LocalBuilder DeclareLocal(Type localType)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType);
#if DEBUG
            Writer.WriteLine("DeclareLocal(typeof({0})) = {1}", localType.FullName, lb.LocalIndex);
#endif
            return lb;
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType, pinned);
#if DEBUG
            Writer.WriteLine("DeclareLocal(typeof({0}), {1}) = {2}", localType.FullName, pinned.ToString(), lb.LocalIndex);
#endif
            return lb;
        }

        public Label DefineLabel()
        {
            Label lb = m_ILGen.DefineLabel();
#if DEBUG
            Writer.WriteLine("DefineLabel() = {0}", lb.ToString());
#endif
            m_DefinedLabels.Add(lb);
            return lb;
        }

        public void Emit(OpCode opcode)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0})", opcode.ToString());
#endif
            m_ILGen.Emit(opcode);
            GeneratedRetAtLast = opcode == OpCodes.Ret;
        }

        public void Emit(OpCode opcode, byte arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, ConstructorInfo con)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), con.ToString());
#endif
            m_ILGen.Emit(opcode, con);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, double arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), field.Name);
#endif
            m_ILGen.Emit(opcode, field);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, float arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, int arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, Label label)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), label.ToString());
#endif
            m_ILGen.Emit(opcode, label);
            GeneratedRetAtLast = false;
        }


        public void Emit(OpCode opcode, Label[] labels)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, [])", opcode.ToString());
#endif
            m_ILGen.Emit(opcode, labels);
            GeneratedRetAtLast = false;
        }


        public void Emit(OpCode opcode, LocalBuilder local)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), local.LocalIndex);
#endif
            m_ILGen.Emit(opcode, local);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, long arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), meth.ToString());
#endif
            m_ILGen.Emit(opcode, meth);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, short arg)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
#endif
            m_ILGen.Emit(opcode, arg);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), signature.ToString());
#endif
            m_ILGen.Emit(opcode, signature);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, string str)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, \"{1}\")", opcode.ToString(), str);
#endif
            m_ILGen.Emit(opcode, str);
            GeneratedRetAtLast = false;
        }

        public void Emit(OpCode opcode, Type cls)
        {
#if DEBUG
            Writer.WriteLine("Emit(OpCodes.{0}, typeof({1}))", opcode.ToString(), cls.FullName);
#endif
            m_ILGen.Emit(opcode, cls);
            GeneratedRetAtLast = false;
        }

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
#if DEBUG
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, [])", opcode.ToString(), methodInfo.ToString());
#endif
            m_ILGen.EmitCall(opcode, methodInfo, optionalParameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        {
#if DEBUG
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...])", opcode.ToString(), unmanagedCallConv.ToString(), returnType.FullName);
#endif
            m_ILGen.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
#if DEBUG
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...], [...])", opcode.ToString(), callingConvention.ToString(), returnType.FullName);
#endif
            m_ILGen.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
            GeneratedRetAtLast = false;
        }

        public void EmitWriteLine(FieldInfo fld)
        {
#if DEBUG
            Writer.WriteLine("EmitWriteLine({0})", fld.ToString());
#endif
            m_ILGen.EmitWriteLine(fld);
        }

        public void EmitWriteLine(LocalBuilder localBuilder)
        {
#if DEBUG
            Writer.WriteLine("EmitWriteLine({0})", localBuilder.ToString());
#endif
            m_ILGen.EmitWriteLine(localBuilder);
        }

        public void EmitWriteLine(string value)
        {
#if DEBUG
            Writer.WriteLine("EmitWriteLine({0})", value);
#endif
            m_ILGen.EmitWriteLine(value);
        }

        public void EndExceptionBlock()
        {
#if DEBUG
            Writer.WriteLine("EndExceptionBlock()");
#endif
            m_ILGen.EndExceptionBlock();
        }

        public void EndScope()
        {
#if DEBUG
            Writer.WriteLine("EndScope()");
#endif
            m_ILGen.EndScope();
        }

        public void MarkLabel(Label loc)
        {
            m_DefinedLabels.Remove(loc);
#if DEBUG
            Writer.WriteLine("MarkLabel({0})", loc.ToString());
#endif
            m_ILGen.MarkLabel(loc);
            GeneratedRetAtLast = false;
        }

        public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
            m_ILGen.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
        }

        public void ThrowException(Type excType)
        {
#if DEBUG
            Writer.WriteLine("ThrowException(typeof({0}))", excType.FullName);
#endif
            m_ILGen.ThrowException(excType);
            GeneratedRetAtLast = false;
        }

        public void UsingNamespace(string usingNamespace)
        {
#if DEBUG
            Writer.WriteLine("UsingNamespace(\"{0}\")", usingNamespace);
#endif
            m_ILGen.UsingNamespace(usingNamespace);
        }
    }
}
