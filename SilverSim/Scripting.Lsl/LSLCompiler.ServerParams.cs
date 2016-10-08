// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        void AddServerParam(Dictionary<string, ServerParamAttribute> resList, ServerParamAttribute attr)
        {
            resList[attr.ParameterName] = attr;
        }

        void AddServerParamBlock(Dictionary<string, ServerParamAttribute> resList, string name)
        {
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".AllowedCreators") { Description = "Defines allowed creators for " + name });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".AllowedOwners") { Description = "Defines allowed owners for " + name });
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEstateOwnerAllowed"));
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEstateManagerAllowed"));
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsRegionOwnerAllowed"));
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsParcelOwnerAllowed"));
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsParcelGroupMemberAllowed"));
            AddServerParam(resList, new ServerParamAttribute("OSSL." + name + ".IsEveryoneAllowed"));
        }

        public IDictionary<string, ServerParamAttribute> ServerParams
        {
            get
            {
                Dictionary<string, ServerParamAttribute> resList = new Dictionary<string, ServerParamAttribute>();
                AddServerParam(resList, new ServerParamAttribute("OSSL.ThreatLevel") { Description = "Defines threat level" });
                foreach(IScriptApi api in m_Apis)
                {
                    Type instanceType = api.GetType();
                    MethodInfo[] mis = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                    foreach(MethodInfo mi in mis)
                    {
                        if(Attribute.GetCustomAttribute(mi, typeof(ThreatLevelUsedAttribute)) != null)
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
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.None;
                        break;

                    case "nuisance":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.Nuisance;
                        break;

                    case "verylow":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.VeryLow;
                        break;

                    case "low":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.Low;
                        break;

                    case "moderate":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.Moderate;
                        break;

                    case "high":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.High;
                        break;

                    case "veryhigh":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.VeryHigh;
                        break;

                    case "severe":
                        Script.ThreatLevels[regionID] = Script.ThreatLevelType.Severe;
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
