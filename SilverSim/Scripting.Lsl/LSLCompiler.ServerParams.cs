// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl
{
    [ServerParamStartsWith("OSSL.")]
    partial class LSLCompiler : IServerParamAnyListener
    {
        public IDictionary<string, ServerParamType> ServerParams
        {
            get
            {
                Dictionary<string, ServerParamType> resList = new Dictionary<string, ServerParamType>();
                resList.Add("OSSL.ThreatLevel", ServerParamType.GlobalAndRegion);
                //Parameter format
                //OSSL.<FunctionName>.AllowedCreators
                //OSSL.<FunctionName>.AllowedOwners
                //OSSL.<FunctionName>.IsEstateOwnerAllowed
                //OSSL.<FunctionName>.IsEstateManagerAllowed
                //OSSL.<FunctionName>.IsRegionOwnerAllowed
                //OSSL.<FunctionName>.IsParcelOwnerAllowed
                //OSSL.<FunctionName>.IsParcelGroupMemberAllowed
                //OSSL.<FunctionName>.IsEveryoneAllowed
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
