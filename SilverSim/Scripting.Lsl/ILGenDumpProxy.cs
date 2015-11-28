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
#if DEBUG
    class ILGenDumpProxy
    {
        ILGenerator m_ILGen;
        public readonly StreamWriter Writer;
        List<Label> m_DefinedLabels = new List<Label>();

        public ILGenDumpProxy(ILGenerator ilgen, StreamWriter textWriter)
        {
            m_ILGen = ilgen;
            Writer = textWriter;
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
            Writer.WriteLine("BeginCatchBlock(typeof({0}))", exceptionType.FullName);
            m_ILGen.BeginCatchBlock(exceptionType);
        }

        public void BeginExceptFilterBlock()
        {
            Writer.WriteLine("BeginExceptFilterBlock()");
            m_ILGen.BeginExceptFilterBlock();
        }

        public Label BeginExceptionBlock()
        {
            Writer.WriteLine("BeginExceptionBlock()");
            return m_ILGen.BeginExceptionBlock();
        }

        public void BeginFaultBlock()
        {
            Writer.WriteLine("BeginFaultBlock()");
            m_ILGen.BeginFaultBlock();
        }

        public void BeginFinallyBlock()
        {
            Writer.WriteLine("BeginFinallyBlock()");
            m_ILGen.BeginFinallyBlock();
        }

        public void BeginScope()
        {
            Writer.WriteLine("BeginScope()");
            m_ILGen.BeginScope();
        }

        public LocalBuilder DeclareLocal(Type localType)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType);
            Writer.WriteLine("DeclareLocal(typeof({0})) = {1}", localType.FullName, lb.LocalIndex);
            return lb;
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType, pinned);
            Writer.WriteLine("DeclareLocal(typeof({0}), {1}) = {2}", localType.FullName, pinned.ToString(), lb.LocalIndex);
            return lb;
        }

        public Label DefineLabel()
        {
            Label lb = m_ILGen.DefineLabel();
            Writer.WriteLine("DefineLabel() = {0}", lb.ToString());
            m_DefinedLabels.Add(lb);
            return lb;
        }

        public void Emit(OpCode opcode)
        {
            Writer.WriteLine("Emit(OpCodes.{0})", opcode.ToString());
            m_ILGen.Emit(opcode);
        }

        public void Emit(OpCode opcode, byte arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, ConstructorInfo con)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), con.ToString());
            m_ILGen.Emit(opcode, con);
        }

        public void Emit(OpCode opcode, double arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), field.Name);
            m_ILGen.Emit(opcode, field);
        }

        public void Emit(OpCode opcode, float arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, int arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, Label label)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), label.ToString());
            m_ILGen.Emit(opcode, label);
        }


        public void Emit(OpCode opcode, Label[] labels)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, [])", opcode.ToString());
            m_ILGen.Emit(opcode, labels);
        }


        public void Emit(OpCode opcode, LocalBuilder local)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), local.LocalIndex);
            m_ILGen.Emit(opcode, local);
        }

        public void Emit(OpCode opcode, long arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), meth.ToString());
            m_ILGen.Emit(opcode, meth);
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, short arg)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), signature.ToString());
            m_ILGen.Emit(opcode, signature);
        }

        public void Emit(OpCode opcode, string str)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, \"{1}\")", opcode.ToString(), str);
            m_ILGen.Emit(opcode, str);
        }

        public void Emit(OpCode opcode, Type cls)
        {
            Writer.WriteLine("Emit(OpCodes.{0}, typeof({1}))", opcode.ToString(), cls.FullName);
            m_ILGen.Emit(opcode, cls);
        }

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, [])", opcode.ToString(), methodInfo.ToString());
            m_ILGen.EmitCall(opcode, methodInfo, optionalParameterTypes);
        }

        public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        {
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...])", opcode.ToString(), unmanagedCallConv.ToString(), returnType.FullName);
            m_ILGen.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
        }

        public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...], [...])", opcode.ToString(), callingConvention.ToString(), returnType.FullName);
            m_ILGen.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        }

        public void EmitWriteLine(FieldInfo fld)
        {
            Writer.WriteLine("EmitWriteLine({0})", fld.ToString());
            m_ILGen.EmitWriteLine(fld);
        }

        public void EmitWriteLine(LocalBuilder localBuilder)
        {
            Writer.WriteLine("EmitWriteLine({0})", localBuilder.ToString());
            m_ILGen.EmitWriteLine(localBuilder);
        }

        public void EmitWriteLine(string value)
        {
            Writer.WriteLine("EmitWriteLine({0})", value);
            m_ILGen.EmitWriteLine(value);
        }

        public void EndExceptionBlock()
        {
            Writer.WriteLine("EndExceptionBlock()");
            m_ILGen.EndExceptionBlock();
        }

        public void EndScope()
        {
            Writer.WriteLine("EndScope()");
            m_ILGen.EndScope();
        }

        public void MarkLabel(Label loc)
        {
            m_DefinedLabels.Remove(loc);
            Writer.WriteLine("MarkLabel({0})", loc.ToString());
            m_ILGen.MarkLabel(loc);
        }

        public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
            m_ILGen.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
        }

        public void ThrowException(Type excType)
        {
            Writer.WriteLine("ThrowException(typeof({0}))", excType.FullName);
            m_ILGen.ThrowException(excType);
        }

        public void UsingNamespace(string usingNamespace)
        {
            Writer.WriteLine("UsingNamespace(\"{0}\")", usingNamespace);
            m_ILGen.UsingNamespace(usingNamespace);
        }
    }
#endif
}
