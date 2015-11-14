using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    class ILGenDumpProxy
    {
        ILGenerator m_ILGen;
        StreamWriter m_Writer;

        public ILGenDumpProxy(ILGenerator ilgen, StreamWriter textWriter)
        {
            m_ILGen = ilgen;
            m_Writer = textWriter;
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
            m_Writer.WriteLine("BeginCatchBlock(typeof({0}))", exceptionType.FullName);
            m_ILGen.BeginCatchBlock(exceptionType);
        }

        public void BeginExceptFilterBlock()
        {
            m_Writer.WriteLine("BeginExceptFilterBlock()");
            m_ILGen.BeginExceptFilterBlock();
        }

        public Label BeginExceptionBlock()
        {
            m_Writer.WriteLine("BeginExceptionBlock()");
            return m_ILGen.BeginExceptionBlock();
        }

        public void BeginFaultBlock()
        {
            m_Writer.WriteLine("BeginFaultBlock()");
            m_ILGen.BeginFaultBlock();
        }

        public void BeginFinallyBlock()
        {
            m_Writer.WriteLine("BeginFinallyBlock()");
            m_ILGen.BeginFinallyBlock();
        }

        public void BeginScope()
        {
            m_Writer.WriteLine("BeginScope()");
            m_ILGen.BeginScope();
        }

        public LocalBuilder DeclareLocal(Type localType)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType);
            m_Writer.WriteLine("DeclareLocal(typeof({0})) = {1}", localType.FullName, lb.LocalIndex);
            return lb;
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            LocalBuilder lb = m_ILGen.DeclareLocal(localType, pinned);
            m_Writer.WriteLine("DeclareLocal(typeof({0}), {1}) = {2}", localType.FullName, pinned.ToString(), lb.LocalIndex);
            return lb;
        }

        public Label DefineLabel()
        {
            Label lb = m_ILGen.DefineLabel();
            m_Writer.WriteLine("DefineLabel() = {0}", lb.ToString());
            return lb;
        }

        public void Emit(OpCode opcode)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0})", opcode.ToString());
            m_ILGen.Emit(opcode);
        }

        public void Emit(OpCode opcode, byte arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, ConstructorInfo con)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), con.ToString());
            m_ILGen.Emit(opcode, con);
        }

        public void Emit(OpCode opcode, double arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), field.Name);
            m_ILGen.Emit(opcode, field);
        }

        public void Emit(OpCode opcode, float arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, int arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, Label label)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), label.ToString());
            m_ILGen.Emit(opcode, label);
        }


        public void Emit(OpCode opcode, Label[] labels)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, [])", opcode.ToString());
            m_ILGen.Emit(opcode, labels);
        }


        public void Emit(OpCode opcode, LocalBuilder local)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), local.LocalIndex);
            m_ILGen.Emit(opcode, local);
        }

        public void Emit(OpCode opcode, long arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), meth.ToString());
            m_ILGen.Emit(opcode, meth);
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, short arg)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), arg);
            m_ILGen.Emit(opcode, arg);
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, {1})", opcode.ToString(), signature.ToString());
            m_ILGen.Emit(opcode, signature);
        }

        public void Emit(OpCode opcode, string str)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, \"{1}\")", opcode.ToString(), str);
            m_ILGen.Emit(opcode, str);
        }

        public void Emit(OpCode opcode, Type cls)
        {
            m_Writer.WriteLine("Emit(OpCodes.{0}, typeof({1}))", opcode.ToString(), cls.FullName);
            m_ILGen.Emit(opcode, cls);
        }

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            m_Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, [])", opcode.ToString(), methodInfo.ToString());
            m_ILGen.EmitCall(opcode, methodInfo, optionalParameterTypes);
        }

        public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        {
            m_Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...])", opcode.ToString(), unmanagedCallConv.ToString(), returnType.FullName);
            m_ILGen.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
        }

        public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            m_Writer.WriteLine("EmitCall(OpCodes.{0}, {1}, typeof({2}), [...], [...])", opcode.ToString(), callingConvention.ToString(), returnType.FullName);
            m_ILGen.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        }

        public void EmitWriteLine(FieldInfo fld)
        {
            m_Writer.WriteLine("EmitWriteLine({0})", fld.ToString());
            m_ILGen.EmitWriteLine(fld);
        }

        public void EmitWriteLine(LocalBuilder localBuilder)
        {
            m_Writer.WriteLine("EmitWriteLine({0})", localBuilder.ToString());
            m_ILGen.EmitWriteLine(localBuilder);
        }

        public void EmitWriteLine(string value)
        {
            m_Writer.WriteLine("EmitWriteLine({0})", value);
            m_ILGen.EmitWriteLine(value);
        }

        public void EndExceptionBlock()
        {
            m_Writer.WriteLine("EndExceptionBlock()");
            m_ILGen.EndExceptionBlock();
        }

        public void EndScope()
        {
            m_Writer.WriteLine("EndScope()");
            m_ILGen.EndScope();
        }

        public void MarkLabel(Label loc)
        {
            m_Writer.WriteLine("MarkLabel({0})", loc.ToString());
            m_ILGen.MarkLabel(loc);
        }

        public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
            m_ILGen.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
        }

        public void ThrowException(Type excType)
        {
            m_Writer.WriteLine("ThrowException(typeof({0}))", excType.FullName);
            m_ILGen.ThrowException(excType);
        }

        public void UsingNamespace(string usingNamespace)
        {
            m_Writer.WriteLine("UsingNamespace(\"{0}\")", usingNamespace);
            m_ILGen.UsingNamespace(usingNamespace);
        }
    }
}
