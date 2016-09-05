// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Runtime.Serialization;

namespace SilverSim.Scripting.Lsl
{
    [Serializable]
    public class ResetScriptException : Exception
    {
        public ResetScriptException()
        {

        }

        public ResetScriptException(string message)
            : base(message)
        {

        }

        protected ResetScriptException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public ResetScriptException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class CallDepthLimitViolationException : Exception
    {
        public CallDepthLimitViolationException()
            : base("Function call depth limit violation")
        {

        }

        public CallDepthLimitViolationException(string message)
            : base(message)
        {

        }

        protected CallDepthLimitViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public CallDepthLimitViolationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class ChangeStateException : Exception
    {
        public string NewState { get { return Message; } }
        public ChangeStateException(string message)
            : base(message)
        {
        }

        public ChangeStateException()
        {

        }

        protected ChangeStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public ChangeStateException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
