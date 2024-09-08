﻿using caneconomy.src.implementations.VirtualMoney;
using claims.src.auxialiry;
using claims.src.part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class MoneyCommands: BaseCommand
    {
        public static TextCommandResult OnCityBalance(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }

            return SuccessWithParams("claims:economy_virtual_city_balance", new object[] { claims.economyHandler.getBalance(playerInfo.City.MoneyAccountName)});
        }

        public static TextCommandResult OnCityWithdraw(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            int toWithdraw = (int)args.Parsers[0].GetValue();

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }

            if(claims.economyHandler.getBalance(playerInfo.City.MoneyAccountName) < toWithdraw)
            {
                return TextCommandResult.Success("claims:economy_virtual_city_not_enough_money");
            }

            if(claims.economyHandler.withdraw(playerInfo.City.MoneyAccountName, (decimal)toWithdraw).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                VirtualMoneyEconomyHandler.GiveCurrencyItemsToPlayer(player, toWithdraw);
                UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_BALANCE);
                return SuccessWithParams("claims:economy_virtual_city_withdrawn", new object[] { toWithdraw });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_withdraw_error");
        }

        public static TextCommandResult OnCityDeposit(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:economy_no_city");
            }

            decimal collectedValue = VirtualMoneyEconomyHandler.TakeCurrencyItemsFromPlayerActiveSlot(player);
            if(collectedValue > 0)
            {
                claims.economyHandler.deposit(claims.config.CITY_ACCOUNT_STRING_PREFIX + playerInfo.City.Guid, (decimal)collectedValue);
                UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_BALANCE);
                return SuccessWithParams("claims:economy_virtual_city_deposited", new object[] { collectedValue });
            }
            return TextCommandResult.Success("claims:economy_virtual_city_deposit_error");
        }
    }
}
