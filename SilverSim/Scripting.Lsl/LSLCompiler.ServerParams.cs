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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Scripting.Lsl
{
    [ServerParamStartsWith("OSSL.")]
    partial class LSLCompiler : IServerParamAnyListener
    {
        private void AddServerParam(Dictionary<string, ServerParamAttribute> resList, ServerParamAttribute attr)
        {
            resList[attr.ParameterName] = attr;
        }

        private void AddServerParamBlock(Dictionary<string, ServerParamAttribute> resList, string name)
        {
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".AllowedCreators") { Description = "Defines allowed creators for " + name });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".AllowedOwners") { Description = "Defines allowed owners for " + name });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEstateOwnerAllowed") { ParameterType = typeof(bool) });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEstateManagerAllowed") { ParameterType = typeof(bool) });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsRegionOwnerAllowed") { ParameterType = typeof(bool) });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsParcelOwnerAllowed") { ParameterType = typeof(bool) });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsParcelGroupMemberAllowed") { ParameterType = typeof(bool) });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEveryoneAllowed") { ParameterType = typeof(bool) });
        }

        public IReadOnlyDictionary<string, ServerParamAttribute> ServerParams
        {
            get
            {
                var resList = new Dictionary<string, ServerParamAttribute>();
                AddServerParam(resList, new ServerParamAttribute("OSSL.ThreatLevel") { Description = "Defines threat level" });
                foreach(IScriptApi api in m_Apis)
                {
                    Type instanceType = api.GetType();
                    foreach(MethodInfo mi in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if(Attribute.GetCustomAttribute(mi, typeof(ThreatLevelRequiredAttribute)) != null)
                        {
                            foreach (APILevelAttribute attr in Attribute.GetCustomAttributes(mi, typeof(APILevelAttribute)))
                            {
                                AddServerParamBlock(resList, string.IsNullOrEmpty(attr.Name) ? mi.Name : attr.Name);
                            }
                            foreach (APIExtensionAttribute attr in Attribute.GetCustomAttributes(mi, typeof(APIExtensionAttribute)))
                            {
                                AddServerParamBlock(resList, string.IsNullOrEmpty(attr.Name) ? mi.Name : attr.Name);
                            }
                        }
                    }
                }

                return resList;
            }
        }

        private void ShowPermissionsReq(HttpRequest req, Map jsondata)
        {
            var resdata = new AnArray();
            foreach (IScriptApi api in m_Apis)
            {
                Type instanceType = api.GetType();
                foreach (MethodInfo mi in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute(mi, typeof(ThreatLevelRequiredAttribute)) != null)
                    {
                        foreach (APILevelAttribute attr in Attribute.GetCustomAttributes(mi, typeof(APILevelAttribute)))
                        {
                            resdata.Add(new Map
                            {
                                { "name", string.IsNullOrEmpty(attr.Name) ? mi.Name : attr.Name }
                            });
                        }
                        foreach (APIExtensionAttribute attr in Attribute.GetCustomAttributes(mi, typeof(APIExtensionAttribute)))
                        {
                            resdata.Add(new Map
                            {
                                { "name", string.IsNullOrEmpty(attr.Name) ? mi.Name : attr.Name }
                            });
                        }
                    }
                }
            }
        }

        public void TriggerParameterUpdated(UUID regionID, string parametername, string value)
        {
            if (!parametername.StartsWith("OSSL."))
            {
                /* ignore anything that is not OSSL. */
                return;
            }

            if(parametername == "OSSL.ThreatLevel")
            {
                switch(value.ToLower())
                {
                    case "":
                        Script.ThreatLevels[regionID] = Script.DefaultThreatLevel;
                        break;

                    case "none":
                        Script.ThreatLevels[regionID] = ThreatLevel.None;
                        break;

                    case "nuisance":
                        Script.ThreatLevels[regionID] = ThreatLevel.Nuisance;
                        break;

                    case "verylow":
                        Script.ThreatLevels[regionID] = ThreatLevel.VeryLow;
                        break;

                    case "low":
                        Script.ThreatLevels[regionID] = ThreatLevel.Low;
                        break;

                    case "moderate":
                        Script.ThreatLevels[regionID] = ThreatLevel.Moderate;
                        break;

                    case "high":
                        Script.ThreatLevels[regionID] = ThreatLevel.High;
                        break;

                    case "veryhigh":
                        Script.ThreatLevels[regionID] = ThreatLevel.VeryHigh;
                        break;

                    case "severe":
                        Script.ThreatLevels[regionID] = ThreatLevel.Severe;
                        break;

                    default:
                        break;
                }
                return;
            }

            string[] parts = parametername.Split('.');
            string[] list;
            List<UUI> uuilist;

            if (parts.Length < 3)
            {
                return;
            }

            //Parameter format
            //OSSL.<FunctionName>.AllowedCreators
            //OSSL.<FunctionName>.AllowedOwners
            //OSSL.<FunctionName>.IsEstateOwnerAllowed
            //OSSL.<FunctionName>.IsEstateManagerAllowed
            //OSSL.<FunctionName>.IsRegionOwnerAllowed
            //OSSL.<FunctionName>.IsParcelOwnerAllowed
            //OSSL.<FunctionName>.IsParcelGroupMemberAllowed
            //OSSL.<FunctionName>.IsEveryoneAllowed
            bool boolval;
            Script.Permissions perms = Script.OSSLPermissions[parts[1]][regionID];
            switch (parts[2])
            {
                case "AllowedCreators":
                    list = value.Split(',');
                    uuilist = new List<UUI>();
                    foreach(string entry in list)
                    {
                        UUI uui;
                        if(UUI.TryParse(entry, out uui) && !uuilist.Contains(uui))
                        {
                            uuilist.Add(uui);
                        }
                    }

                    foreach(UUI uui in perms.Creators)
                    {
                        if(!uuilist.Contains(uui))
                        {
                            perms.Creators.Remove(uui);
                        }
                    }

                    foreach (UUI uui in uuilist)
                    {
                        perms.Creators.AddIfNotExists(uui);
                    }
                    break;

                case "AllowedOwners":
                    list = value.Split(',');
                    uuilist = new List<UUI>();
                    foreach (string entry in list)
                    {
                        UUI uui;
                        if (UUI.TryParse(entry, out uui) && !uuilist.Contains(uui))
                        {
                            uuilist.Add(uui);
                        }
                    }

                    foreach (UUI uui in perms.Owners)
                    {
                        if (!uuilist.Contains(uui))
                        {
                            perms.Owners.Remove(uui);
                        }
                    }

                    foreach (UUI uui in uuilist)
                    {
                        perms.Owners.AddIfNotExists(uui);
                    }
                    break;

                case "IsEstateOwnerAllowed":
                    if(bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForEstateOwner = boolval;
                    }
                    break;

                case "IsEstateManagerAllowed":
                    if (bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForEstateManager = boolval;
                    }
                    break;

                case "IsRegionOwnerAllowed":
                    if(bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForRegionOwner = boolval;
                    }
                    break;

                case "IsParcelOwnerAllowed":
                    if(bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForParcelOwner = boolval;
                    }
                    break;

                case "IsParcelGroupMemberAllowed":
                    if (bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForParcelGroupMember = boolval;
                    }
                    break;

                case "IsEveryoneAllowed":
                    if (bool.TryParse(value, out boolval))
                    {
                        Script.OSSLPermissions[parts[1]][regionID].IsAllowedForEveryone = boolval;
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
