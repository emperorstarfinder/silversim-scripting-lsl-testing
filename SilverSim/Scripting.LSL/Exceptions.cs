// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    [Serializable]
    public class ResetScriptException : Exception
    {
        public ResetScriptException()
        {

        }
    }

    [Serializable]
    public class ChangeStateException : Exception
    {
        public string NewState { get; private set; }
        public ChangeStateException(string newstate)
        {
            NewState = newstate;
        }
    }
}
