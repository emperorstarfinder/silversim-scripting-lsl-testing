using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL
{
    public static class ExtensionMethods
    {
        public static int ToLSLBoolean(this bool v)
        {
            return v ? 1 : 0;
        }
    }
}
