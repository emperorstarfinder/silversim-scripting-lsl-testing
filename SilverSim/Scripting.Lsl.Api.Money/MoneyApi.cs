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

#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using SilverSim.Types.Script;
using System;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.Money
{
    [ScriptApiName("Money")]
    [Description("LSL Money API")]
    [PluginName("LSL_Money")]
    [LSLImplementation]
    public class MoneyApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "transaction_result")]
        [StateEventDelegate]
        public delegate void State_transaction_result(LSLKey id, int success, string data);

        private delegate void TransferMoneyDelegate(UUID transactionID, UUI sourceid,
            UUI destinationid, int amount, ScriptInstance instance);

        private bool TransferMoney(UUI sourceid, UUI destinationid, int amount, ScriptInstance instance)
        {
            EconomyServiceInterface sourceservice = null;
            EconomyServiceInterface destinationservice = null;
            bool success = false;
            if (sourceservice != null &&
                destinationservice != null &&
                destinationid != UUI.Unknown)
            {
                try
                {
                    SceneInterface scene = instance.Part.ObjectGroup.Scene;
                    var transaction = new ObjectPaysTransaction(
                        scene.GridPosition,
                        scene.ID,
                        scene.Name)
                    {
                        ObjectName = instance.Part.Name,
                        ObjectID = instance.Part.ID,
                    };
                    sourceservice.TransferMoney(sourceid, destinationid, transaction, amount, () => { });
                    success = true;
                }
                catch
                {
                    /* error intentionally ignored sine ev.Success holds the result status */
                }
            }
            return success;
        }

        public class TransferMoneyData
        {
            public UUID TransactionID;
            public UUI SourceID;
            public UUI DestinationID;
            public int Amount;
            public ScriptInstance Instance;
        }

        private void TransferMoney(object o)
        {
            var data = (TransferMoneyData)o;
            var ev = new TransactionResultEvent
            {
                Success = false,
                TransactionID = data.TransactionID
            };
            try
            {
                ev.Success = TransferMoney(data.SourceID, data.DestinationID, data.Amount, data.Instance);
            }
            catch
            {
                /* content intentionally ignored */
            }

            data.Instance.PostEvent(ev);
        }

        [APILevel(APIFlags.LSL, "llGiveMoney")]
        public int GiveMoney(ScriptInstance instance, LSLKey destination, int amount)
        {
            lock (instance)
            {
                UUI destinationuui;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                    grantinfo.PermsGranter != instance.Part.Owner ||
                    amount < 0 ||
                    !instance.Part.ObjectGroup.Scene.AvatarNameService.TryGetValue(destination.AsUUID, out destinationuui))
                {
                    return 0;
                }

                TransferMoney(grantinfo.PermsGranter, destinationuui, amount, instance);
            }
            return 0;
        }

        [APILevel(APIFlags.LSL, "llTransferLindenDollars")]
        public LSLKey TransferLindenDollars(ScriptInstance instance, LSLKey destination, int amount)
        {
            lock (instance)
            {
                UUI destinationuui;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                    grantinfo.PermsGranter != instance.Part.Owner ||
                    amount < 0 ||
                    !instance.Part.ObjectGroup.Scene.AvatarNameService.TryGetValue(destination.AsUUID, out destinationuui))
                {
                    return UUID.Zero;
                }

                UUID transactionid = UUID.Random;

                var data = new TransferMoneyData
                {
                    SourceID = grantinfo.PermsGranter,
                    DestinationID = destinationuui,
                    Amount = amount,
                    Instance = instance,
                    TransactionID = transactionid
                };

                ThreadPool.QueueUserWorkItem(TransferMoney, data);
                return transactionid;
            }
        }
    }
}
