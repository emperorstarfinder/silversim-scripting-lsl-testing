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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Inventory;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public sealed partial class BaseApi
    {
        [APIExtension(APIExtension.Properties, "script")]
        [APIDisplayName("script")]
        [APIAccessibleMembers]
        public class OwnScriptAccessor
        {
            private readonly ScriptInstance m_Instance;

            public OwnScriptAccessor(ScriptInstance instance)
            {
                m_Instance = instance;
            }

            public LSLKey Owner
            {
                get
                {
                    lock (m_Instance)
                    {
                        return m_Instance.Item.Owner.ID;
                    }
                }
            }

            public string Name
            {
                get
                {
                    lock (m_Instance)
                    {
                        return m_Instance.Item.Name;
                    }
                }
            }

            public int StartParameter
            {
                get
                {
                    lock (m_Instance)
                    {
                        return ((Script)m_Instance).StartParameter;
                    }
                }
            }

            public double MinEventDelay
            {
                get
                {
                    lock (m_Instance)
                    {
                        return ((Script)m_Instance).MinEventDelay;
                    }
                }
                set
                {
                    lock (m_Instance)
                    {
                        ((Script)m_Instance).MinEventDelay = value;
                    }
                }
            }
        }

        /* retrieves own script */
        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Script")]
        public OwnScriptAccessor GetScriptProperty(ScriptInstance instance) => new OwnScriptAccessor(instance);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Reset")]
        public void ResetScript(OwnScriptAccessor accessor)
        {
            throw new ResetScriptException(); /* exception triggers state change code */
        }

        [APIExtension(APIExtension.Properties, "otherscript")]
        [APIDisplayName("otherscript")]
        [APIAccessibleMembers]
        public class OtherScriptAccessor
        {
            private readonly ScriptInstance m_Instance;
            private readonly ScriptInstance m_OtherInstance;

            public OtherScriptAccessor(ScriptInstance instance, ScriptInstance otherinstance)
            {
                m_Instance = instance;
                m_OtherInstance = otherinstance;
            }

            public LSLKey Owner
            {
                get
                {
                    lock (m_Instance)
                    {
                        return m_OtherInstance.Item.Owner.ID;
                    }
                }
            }

            public string Name
            {
                get
                {
                    lock (m_Instance)
                    {
                        return m_OtherInstance.Item.Name;
                    }
                }
            }

            public int IsRunning
            {
                get
                {
                    lock (m_Instance)
                    {
                        return ((Script)m_OtherInstance).IsRunning.ToLSLBoolean();
                    }
                }
                set
                {
                    lock (m_Instance)
                    {
                        ((Script)m_OtherInstance).IsRunning = value != 0;
                    }
                }
            }

            public void Reset()
            {
                lock (m_Instance)
                {
                    m_OtherInstance.PostEvent(new ResetScriptEvent());
                }
            }

            public OtherScriptAccessor this[string name]
            {
                get
                {
                    lock (m_Instance)
                    {
                        ObjectPartInventoryItem item;
                        ScriptInstance si;
                        if (m_Instance.Part.Inventory.TryGetValue(name, out item))
                        {
                            si = item.ScriptInstance;
                            if (item.InventoryType != InventoryType.LSL)
                            {
                                throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item {1} is not a script", "Script[]", name);
                            }
                            else if (si == null)
                            {
                                throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotACompiledScript", "{0}: Inventory item {1} is not a compiled script.", "Script[]", name);
                            }
                            else
                            {
                                return new OtherScriptAccessor(m_Instance, si);
                            }
                        }
                        else
                        {
                            throw new LocalizedScriptErrorException(this, "Function0Script1DoesNotExist", "{0}: Script {1} does not exist", "Script[]", name);
                        }
                    }
                }
            }
        }

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Reset")]
        public void ResetScript(OtherScriptAccessor accessor) => accessor.Reset();
    }
}
