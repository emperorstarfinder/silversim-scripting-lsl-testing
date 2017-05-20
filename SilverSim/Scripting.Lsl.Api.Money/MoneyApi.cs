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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;

namespace SilverSim.Scripting.Lsl.Api.Money
{
    [ScriptApiName("Money")]
    [Description("LSL Money API")]
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

        delegate void TransferMoneyDelegate(UUID transactionID, UUI sourceid, 
            UUI destinationid, int amount, ScriptInstance instance);

        void TransferMoney(UUID transactionID, UUI sourceid,
            UUI destinationid, int amount, ScriptInstance instance)
        {
            EconomyServiceInterface sourceservice = null;
            EconomyServiceInterface destinationservice = null;
            var ev = new TransactionResultEvent()
            {
                Success = false,
                TransactionID = transactionID
            };
            if (sourceservice == null ||
                destinationservice == null ||
                destinationid == UUI.Unknown)
            {
                if (instance != null)
                {
                    instance.PostEvent(ev);
                }
            }
            else
            {
                try
                {
                    sourceservice.ChargeAmount(sourceid, EconomyServiceInterface.TransactionType.ObjectPays, amount,
                        () => destinationservice.IncreaseAmount(destinationid, EconomyServiceInterface.TransactionType.ObjectPays, amount));
                    ev.Success = true;
                }
                catch
                {
                    /* error intentionally ignored sine ev.Success holds the result status */
                }
                if (instance != null)
                {
                    instance.PostEvent(ev);
                }
            }
        }

        void TransferMoneyEnd(IAsyncResult ar)
        {
            var result = (AsyncResult)ar;
            var caller = (TransferMoneyDelegate)result.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void InvokeTransferMoney(UUID transactionID, UUI sourceid,
            UUI destinationid, int amount, ScriptInstance instance)
        {
            TransferMoneyDelegate d = TransferMoney;
            d.BeginInvoke(transactionID, sourceid, destinationid, amount, instance, TransferMoneyEnd, this);
        }

        [APILevel(APIFlags.LSL, "llGiveMoney")]
        public int GiveMoney(ScriptInstance instance, LSLKey destination, int amount)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                grantinfo.PermsGranter != instance.Part.Owner ||
                amount < 0)
            {
                return 0;
            }
            return 0;
        }

        [APILevel(APIFlags.LSL, "llTransferLindenDollars")]
        public LSLKey TransferLindenDollars(ScriptInstance instance, LSLKey destination, int amount)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                grantinfo.PermsGranter != instance.Part.Owner ||
                amount < 0)
            {
                return UUID.Zero;
            }
            return UUID.Zero;
        }
    }
}
