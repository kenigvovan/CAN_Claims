﻿using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.delayed.cooldowns;
using claims.src.delayed.invitations;
using claims.src.delayed.teleportation;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using claims.src;
using claims.src.network.packets;
using claims.src.gui.playerGui.structures;
using Newtonsoft.Json;
using HarmonyLib;
using Vintagestory.Server;
using System.Reflection;

namespace claims.src.commands
{
    public class CityCommand: BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CityHere(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            if (!plotHere.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            return TextCommandResult.Success(string.Join("", plotHere.getCity().getStatus()));
        }
        public static TextCommandResult ProcessListCities(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            return SuccessWithParams("claims:cities_list", new object[] { StringFunctions.makeFeasibleStringFromNames(StringFunctions.getNamesOfCities("", claims.dataStorage.getCitiesList()), ' ') });
        }
        public static TextCommandResult CityInfo(TextCommandCallingArgs args)
        {
            string cityName = Filter.filterName((string)args.LastArg);
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                return TextCommandResult.Error("claims:invalid_city_name");
            }
            claims.dataStorage.getCityByName(cityName, out City city);

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            if (city == null)
            {
                return TextCommandResult.Success("claims:no_such_city");
            }
            else
            {
                return TextCommandResult.Success(string.Join("", city.getStatus(playerInfo)));
            }
        }
        public static TextCommandResult CreateNewCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            if (playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_already_have_city");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }

            string newCityName = Filter.filterName((string)args.LastArg);
            if (claims.dataStorage.cityExistsByName(newCityName))
            {
                return TextCommandResult.Error("claims:city_name_is_already_taken");
            }

            if (newCityName.Length == 0 || !Filter.checkForBlockedNames(newCityName))
            {
                return TextCommandResult.Error("claims:invalid_new_city_name");
            }
            if (newCityName.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                return TextCommandResult.Error("claims:city_name_is_too_long");
            }
            if (claims.economyHandler.getBalance(playerInfo.Guid) < (decimal)claims.config.NEW_CITY_COST)
            {
                return TextCommandResult.Error("claims:not_enough_for_new_city");
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherForNewCity(new Vec2i((int)player.Entity.ServerPos.X / 16, (int)player.Entity.ServerPos.Z / 16)))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city_new_city");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                new Thread(new ThreadStart(() =>
                {
                    claims.dataStorage.getPlot(currentPlotPosition, out plotHere);
                    if (playerInfo.hasCity() || plotHere != null)
                    {
                        return;
                    }
                    if (!claims.config.NEW_CITY_ONLY_BY_ITEM)
                    {
                        if (claims.economyHandler.withdraw(playerInfo.Guid, (decimal)claims.config.NEW_CITY_COST).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                        {
                            PartInits.initNewCity(playerInfo, currentPlotPosition, newCityName);
                        }
                    }
                    else
                    {
                        PartInits.initNewCity(playerInfo, currentPlotPosition, newCityName);
                    }
                })), player.PlayerUID));
            
            if (player != null)
            {
                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.AGREE_NEEDED_ON_NEW_CITY_CREATION,
                    data = newCityName

                }, player as IServerPlayer);
            }

            return SuccessWithParams("claims:help_agreement_new_city", new object[] { claims.config.AGREEMENT_COMMAND });
        }
        public static TextCommandResult DeleteCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                new Thread(new ThreadStart(() =>
                {
                    MessageHandler.sendGlobalMsg(Lang.Get("claims:city_has_been_demolished", city.getPartNameReplaceUnder(), playerInfo.getPartNameReplaceUnder()));

                    PartDemolition.demolishCity(city);
                })), player.PlayerUID));
            return SuccessWithParams("claims:help_agreement_delete_city", new object[] { claims.config.AGREEMENT_COMMAND });
        }
        /*==============================================================================================*/
        /*=====================================CLAIM====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult ClaimCityPlot(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;


            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }

            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }

            if (city.getCityPlots().Count >= Settings.getMaxNumberOfPlotForCity(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }

            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            if (claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere))
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            if (claims.economyHandler.getBalance(city.MoneyAccountName) < (decimal)claims.config.PLOT_CLAIM_PRICE)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }
            if (!CheckForAtleastOneClaimedPlotOnBorderSameCity(plotHere))
            {
                return TextCommandResult.Error("claims:should_be_on_the_border_with_another_claimed_plot");
            }
            
            if(claims.economyHandler.withdraw(city.MoneyAccountName, (decimal)claims.config.PLOT_CLAIM_PRICE).ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_BALANCE);
            plotHere.setCity(playerInfo.City);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.setPrice(-1);
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.PLOT_CLAIM_PRICE });
        }
        public static TextCommandResult UnclaimCityPlot(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            if (!plotHere.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }

            if (!plotHere.getCity().Equals(city))
            {
                return TextCommandResult.Error("claims:player_should_be_in_same_city");
            }
            if (plotHere.getCity().getCityPlots().Count() == 1)
            {
                return TextCommandResult.Error("claims:last_city_plot");
            }
            if (plotHere.extraBought)
            {
                city.Extrachunksbought--;
                city.saveToDatabase();
            }
            PartDemolition.demolishCityPlot(plotHere);

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasRemoved(plotHere.getPos());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            return SuccessWithParams("claims:plot_has_been_unclaimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y });
        }
        public static TextCommandResult ClaimOutpost(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (city.getCityPlots().Count >= Settings.getMaxNumberOfPlotForCity(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }
            if (claims.economyHandler.getBalance(city.MoneyAccountName) < (decimal)claims.config.OUTPOST_PLOT_COST)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }

            if(claims.economyHandler.withdraw(city.MoneyAccountName, (decimal)claims.config.OUTPOST_PLOT_COST).ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            plotHere.setCity(playerInfo.City);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.setPrice(-1);
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y });
        }
        public static TextCommandResult ProcessExtraPlot(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                return TextCommandResult.Error("claims:plot_already_claimed");
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);

            if (city.Extrachunksbought >= Settings.getMaxNumberOfExtraChunksBought(city))
            {
                return TextCommandResult.Error("claims:max_amount_claimed");
            }
            if (claims.economyHandler.getBalance(city.MoneyAccountName) < (decimal)claims.config.EXTRA_PLOT_COST)
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                return TextCommandResult.Error("claims:too_close_to_another_city");
            }

            if(claims.economyHandler.withdraw(city.MoneyAccountName, (decimal)claims.config.EXTRA_PLOT_COST).ResultState != caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.setPrice(-1);
            plotHere.extraBought = true;
            city.Extrachunksbought++;
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());

            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS);
            return SuccessWithParams("claims:plot_has_been_claimed", new object[] { (currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, claims.config.EXTRA_PLOT_COST) });
        }
        /*==============================================================================================*/
        /*=====================================INVITES==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult InviteToCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_city_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity != null)
            {
                return TextCommandResult.Success("claims:player_has_city_already");
            }
            if (InvitationHandler.addNewInvite(new Invitation(city, targetPlayer, TimeFunctions.getEpochSeconds() + claims.config.HOUR_TIMEOUT_INVITATION_CITY * 60 * 60,
                new Thread(new ThreadStart(() =>
                {
                    city.getCityCitizens().Add(targetPlayer);
                    targetPlayer.setCity(city);
                    city.saveToDatabase();
                    targetPlayer.saveToDatabase();
                    TreeAttribute tree = new TreeAttribute();
                    tree.SetString("cityname", city.GetPartName());
                    claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
                    UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(targetPlayer);
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
                })),
                new Thread(new ThreadStart(() =>
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:player_disagreed_with_invitation_to_city", targetPlayer.GetPartName(), city.GetPartName()));                  
                }))
                )))
            {
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_invited_to_city", city.GetPartName()));

                var targetIPlayer = claims.sapi.World.PlayerByUid(targetPlayer.Guid);
                if (targetIPlayer != null)
                {
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CITY_INVITE_ADD, JsonConvert.SerializeObject(new ClientToCityInvitation(city.GetPartName(),
                                                                 TimeFunctions.getEpochSeconds() + claims.config.HOUR_TIMEOUT_INVITATION_CITY * 60 * 60)) }
                    };


                    claims.serverChannel.SendPacket(
                            new PlayerGuiRelatedInfoPacket()
                            {
                                playerGuiRelatedInfoDictionary = collector
                            }
                            , targetIPlayer as IServerPlayer);
                }
                return SuccessWithParams("claims:invitation_to_city_was_sent", new object[] { targetPlayer.GetPartName() });
            }
            else
            {
                return TextCommandResult.Success("claims:player_already_invited_to_city");
            }
        }
        public static TextCommandResult CityKick(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity == null || !targetCity.Equals(city))
            {
                return TextCommandResult.Success("claims:player_should_be_in_same_city");
            }
            if (city.isMayor(targetPlayer))
            {
                return TextCommandResult.Success("claims:can_not_kick_mayor");
            }
            if (playerInfo.Equals(targetPlayer))
            {
                return TextCommandResult.Success("claims:can_not_kick_yourself");
            }
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_was_kicked", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_kicked_from_city"));
            targetPlayer.clearCity();
            TreeAttribute tree = new TreeAttribute();
            tree.SetString("cityname", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
            UsefullPacketsSend.SendPlayerRelatedInfoOnKickFromCity(targetPlayer);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
            return TextCommandResult.Success();
        }
        public static TextCommandResult CityLeave(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            tcr.Status = EnumCommandStatus.Error;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            if (city.isMayor(playerInfo))
            {
                return TextCommandResult.Success("claims:you_are_mayor");
            }
            playerInfo.clearCity();
            TreeAttribute tree = new TreeAttribute();
            tree.SetString("cityname", city.GetPartName());
            claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
            UsefullPacketsSend.SendPlayerRelatedInfoOnKickFromCity(playerInfo);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
            return SuccessWithParams("claims:player_left_city", new object[] { playerInfo.getPartNameReplaceUnder() });
        }
        public static TextCommandResult UninviteToCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
         
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            if (InvitationHandler.removeInvitationIfExists(city, targetPlayer))
            {
                Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CITY_INVITE_REMOVE, city.GetPartName() }
                    };


                claims.serverChannel.SendPacket(
                        new PlayerGuiRelatedInfoPacket()
                        {
                            playerGuiRelatedInfoDictionary = collector
                        }
                        , claims.sapi.World.PlayerByUid(targetPlayer.Guid) as IServerPlayer);
                return SuccessWithParams("claims:invitation_for_player_to_city_was_removed", new object[] { city.GetPartName(), targetPlayer.GetPartName() });
            }
            else
            {
                return TextCommandResult.Success("claims:no_invitation");
            }
        }
        public static TextCommandResult ShowInvitesSent(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (args.LastArg == null)
            {
                return TextCommandResult.Success(StringFunctions.getNthPageOf(playerInfo.City.getSentInvitations(), 1));
            }

            int page = (int)args.LastArg;

            var sentInvites = playerInfo.City.getSentInvitations();
            if (sentInvites.Count() < 1)
            {
                return TextCommandResult.Success("claims:no_invitations");
            }
            return TextCommandResult.Success(StringFunctions.getNthPageOf(playerInfo.City.getSentInvitations(), page));
        }
        public static TextCommandResult CityJoin(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:has_city_or_village");
            }
            string targetCity = Filter.filterName((string)args.LastArg);
            if (targetCity.Length == 0 || !Filter.checkForBlockedNames(targetCity))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getCityByName(targetCity, out City city);
            if (city == null)
            {
                return TextCommandResult.Success("claims:no_such_city");
            }

            if (!city.openCity)
            {
                return TextCommandResult.Success("claims:not_open_city");
            }
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_joined_city", playerInfo.getPartNameReplaceUnder()));
            city.getPlayerInfos().Add(playerInfo);
            playerInfo.setCity(city);
            city.saveToDatabase();
            playerInfo.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS);
            UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(playerInfo);
            return TextCommandResult.Success();
        }
        /*==============================================================================================*/
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CitySetInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Error("");
            }
            return TextCommandResult.Success(city.getPermsHandler().getStringForChat() + "\n");
        }
        public static TextCommandResult SetCityName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            City city = playerInfo.City;
            if (claims.economyHandler.getBalance(city.MoneyAccountName) < (decimal)claims.config.CITY_NAME_CHANGE_COST)
            {
                return TextCommandResult.Success("claims:not_enough_money");
            }
            

            if (city.rename((string)args.LastArg))
            {
                if (claims.economyHandler.withdraw(playerInfo.City.MoneyAccountName, (decimal)claims.config.CITY_NAME_CHANGE_COST).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                {
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(playerInfo.City.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_BALANCE);
                    return TextCommandResult.Success();
                }
            }
            
            return TextCommandResult.Error("");
        }
        public static TextCommandResult CitySetPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_do_not_have_city");
            }

            if (args.RawArgs.Length < 3)
            {
                return TextCommandResult.Success("claims:more_parameters_needed");
            }

            city.getPermsHandler().setAccessPerm(args.RawArgs);
            city.saveToDatabase();
            return SuccessWithParams("claims:for_group_perm_set_what", new object[] { args[0], args[1], args[2] });
        }
        public static TextCommandResult CitySetInvMsg(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            if (args.LastArg == null)
            {
                city.invMsg = "";
                city.saveToDatabase();
                return TextCommandResult.Success("claims:city_inv_msg_reset");
            }
            StringBuilder sb = new StringBuilder();
            int lenCounter = 0;

            string filteredName = Filter.filterNameWithSpaces((string)args.LastArg);

            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Success("claims:invalid_string");
            }
            lenCounter += filteredName.Length;
            if (lenCounter > claims.config.MAX_LENGTH_CITY_INV_MSG)
            {
                return TextCommandResult.Success("claims:inv_msg_too_long");
            }
            sb.Append(" ").Append(filteredName);

            city.invMsg = sb.ToString();
            city.saveToDatabase();
            //if empty reset to empty msg
            return TextCommandResult.Success("claims:city_inv_msg_set");
        }
        public static TextCommandResult CitySetPvP(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setPvp((string)args.LastArg);
            city.saveToDatabase();
            return SuccessWithParams("claims:pvp_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setFire((string)args.LastArg);
            city.saveToDatabase();
            return SuccessWithParams("claims:fire_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            city.getPermsHandler().setBlast((string)args.LastArg);
            city.saveToDatabase();
            return SuccessWithParams("claims:fire_flag_set_to", new object[] { (string)args.LastArg });
        }
        public static TextCommandResult CitySetCitizenPrefix(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            string[] playerName_title = ((string)args.LastArg).Split(' ');
            if (playerName_title.Length < 1)
            {
                return TextCommandResult.Error("claims:no_paramaters");
            }
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Error("claims:no_such_player_info");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }

            string filteredName = Filter.filterName(playerName_title[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Error("claims:invalid_name");
            }

            claims.dataStorage.getPlayerByName(filteredName, out PlayerInfo targetPlayer);

            if (targetPlayer == null)
            {
                return TextCommandResult.Error("claims:invalid_name");
            }
            if (playerName_title.Length < 2)
            {
                targetPlayer.Prefix = "";
                targetPlayer.saveToDatabase();
                return SuccessWithParams("claims:citizen_title_reset", new object[] { targetPlayer.GetPartName() });
            }
            filteredName = Filter.filterName(playerName_title[1]);
            //Length == 0 => empty title
            if (!Filter.checkForBlockedNames(filteredName))
            {
                return TextCommandResult.Error("claims:invalid_title");
            }
            if (filteredName.Length > claims.config.MAX_CITIZEN_TITLE_LENGTH)
            {
                return TextCommandResult.Error("claims:citizen_title_is_too_long");
            }

            targetPlayer.Prefix = filteredName;
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(targetPlayer.GetPartName(), EnumPlayerRelatedInfo.PLAYER_PREFIX);
            return TextCommandResult.Success();
        }
        public static TextCommandResult CitySetOpen(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }
            if(city.setCityOpenCloseState((string)args.LastArg))
            {
                city.saveToDatabase();
                return TextCommandResult.Success();
            }
            return TextCommandResult.Error("");
        }
        public static TextCommandResult CitySetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            City city = claims.dataStorage.getCityByPlayerGUID(player.PlayerUID);
            if (city == null)
            {
                return TextCommandResult.Success();
            }

            int fee = (int)args.LastArg;

            if (fee < 0)
            {
                return TextCommandResult.Success("claims:not_negative");
            }
            if (fee > claims.config.MAX_CITY_FEE)
            {
                fee = (int)claims.config.MAX_CITY_FEE;
            }

            city.fee = fee;
            city.saveToDatabase();
            return SuccessWithParams("claims:city_fee_set_to", new object[] { fee });
        }
        public static TextCommandResult CitySetMayor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            City city = playerInfo.City;
            if (city == null)
            {
                return TextCommandResult.Success("claims:you_dont_have_city");
            }
            if (!city.isMayor(playerInfo))
            {
                return TextCommandResult.Success("claims:you_dont_have_right_for_that_command");
            }

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            City targetCity = targetPlayer.City;
            if (targetCity == null || !targetCity.Equals(city))
            {
                return TextCommandResult.Success("claims:player_should_be_in_same_city");
            }

            city.setMayor(targetPlayer);
            RightsHandler.reapplyRights(playerInfo);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_now_is_a_mayor", targetPlayer.GetPartName()));
            playerInfo.saveToDatabase();
            targetPlayer.saveToDatabase();
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.MAYOR_NAME);
            return SuccessWithParams("claims:player_now_is_a_mayor", new object[] { targetPlayer.GetPartName() });
        }
        public static TextCommandResult CitySetPlotsColor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:no_city");
            }

            if (playerInfo.City.isMayor(playerInfo))
            {
                if (!ColorHandling.tryFindColor((string)args.LastArg, out int color))
                {
                    return TextCommandResult.Success("claims:unknown_color");
                }
                playerInfo.City.trySetPlotColor(color);
                return SuccessWithParams("claims:color_was_set_to", new object[] { (string)args.LastArg });
            }
            else
            {
                return TextCommandResult.Success("claims:only_for_mayor");
            }
        }
        public static TextCommandResult CitySetPlotsColorInt(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Success("claims:no_city");
            }

            if (playerInfo.City.isMayor(playerInfo))
            {
                int colorValue;
                try
                {
                    colorValue = int.Parse((string)args.LastArg);
                }
                catch(Exception e)
                {
                    return TextCommandResult.Success("claims:wrong_value");
                }
                playerInfo.City.trySetPlotColor(colorValue);
                return SuccessWithParams("claims:color_was_set_to", new object[] { (string)args.LastArg });
            }
            else
            {
                return TextCommandResult.Success("claims:only_for_mayor");
            }
        }
        /*==============================================================================================*/
        /*=====================================RANKS====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CityRankList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (args.LastArg == null)
            {
                return TextCommandResult.Success(StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:your_ranks"),
                    playerInfo.getCityTitles(),
                    ", "));
            }

            string targetNames = Filter.filterName((string)args.LastArg); ;
            if (targetNames.Length == 0 || !Filter.checkForBlockedNames(targetNames))
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.getPlayerByName(targetNames, out PlayerInfo targetPlayerInfo);
            if (targetPlayerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player_info");
            }
            if (playerInfo.hasCity() && playerInfo.City.Equals(targetPlayerInfo.City))
            {
                return TextCommandResult.Success(StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:player_ranks", targetPlayerInfo.GetPartName()),
                    targetPlayerInfo.getCityTitles(),
                    ", "));
            }
            return TextCommandResult.Success();

        }
        public static TextCommandResult CityRankAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (args.LastArg == null)
            {
                return tcr;
            }
            string[] rank_and_player_name = ((string)args.LastArg).Split(' ');
            if (rank_and_player_name.Length < 2)
            {
                return tcr;
            }
            City city = null;
            PlayerInfo targetPlayer = null;
            if (!HelperFunctionRank(player, rank_and_player_name[0], rank_and_player_name[1], out city, out targetPlayer, tcr))
            {
                return tcr;
            }
            if (targetPlayer.getCityTitles().Contains(rank_and_player_name[0]))
            {
                tcr.StatusMessage = "claims:player_already_has_rank";
                return tcr;
            }
            targetPlayer.addCityTitle(rank_and_player_name[0]);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:rank_added_to_player", targetPlayer.GetPartName(), rank_and_player_name[0]));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_got_now_rank", rank_and_player_name[0]));
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_CITY_TITLES);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);

            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult CityRankRemove(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (args.LastArg == null)
            {
                return tcr;
            }
            string[] rank_and_player_name = ((string)args.LastArg).Split(' ');
            if (rank_and_player_name.Length < 2)
            {
                return tcr;
            }

            City city = null;
            PlayerInfo targetPlayer = null;
            if (!HelperFunctionRank(player, rank_and_player_name[0], rank_and_player_name[1], out city, out targetPlayer, tcr))
            {
                return tcr;
            }
            if (!targetPlayer.getCityTitles().Contains(rank_and_player_name[0]))
            {
                tcr.StatusMessage = "claims:player_doesnt_have_this_title";
                return tcr;
            }
            targetPlayer.removeCityTitle(rank_and_player_name[0]);
            RightsHandler.reapplyRights(targetPlayer);
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:rank_was_deleted", rank_and_player_name[0], targetPlayer.GetPartName()));
            targetPlayer.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_CITY_TITLES);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(targetPlayer.Guid, EnumPlayerRelatedInfo.PLAYER_PERMISSIONS);
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.PlayerUID, EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS);
            return SuccessWithParams("claims:rank_removed_from_player", new object[] { rank_and_player_name[0], targetPlayer.GetPartName() });
        }
        /*==============================================================================================*/
        /*=====================================PRISON===================================================*/
        /*==============================================================================================*/
        public static TextCommandResult PrisonList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            City city = null;
            Plot plotHere = null;
            if (!HelperFunctionPrison(player, out city, out plotHere, tcr))
            {
                return tcr;
            }
            Prison prison = plotHere.getPrison();
            StringBuilder sb = new StringBuilder();

            int i = 0;
            foreach (var it in prison.getPrisonCells())
            {
                sb.Append(i.ToString()).Append(". ").Append(it.getSpawnPosition().ToString()).Append("\n");
                i++;
            }
            tcr.StatusMessage = sb.ToString();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult RemovePrisonCell(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            City city = null;
            Plot plotHere = null;
            if (!HelperFunctionPrison(player, out city, out plotHere, tcr))
            {
                return tcr;
            }

            if (plotHere.getPrison().getPrisonCells().Count == 1)
            {
                return TextCommandResult.Success("claims:last_cell");
            }
            int index = (int)args.LastArg;
            if (index < 0 || plotHere.getPrison().getPrisonCells().Count < index)
            {
                return TextCommandResult.Error("claims:need_number");
            }
            plotHere.getPrison().removePrisonCell(index);
            plotHere.saveToDatabase();
            plotHere.getPrison().saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult AddPrisonCell(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            City city = null;
            Plot plotHere = null;
            if (!HelperFunctionPrison(player, out city, out plotHere, tcr))
            {
                return tcr;
            }
            if (plotHere.getPrison().getPrisonCells().Count > claims.config.MAX_CELLS_PER_PRISON)
            {
                tcr.StatusMessage = "claims:too_much_cells";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }

            plotHere.getPrison().addPrisonCell(new PrisonCellInfo(new Vec3i((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Y, (int)player.Entity.ServerPos.Z)));
            plotHere.saveToDatabase();
            plotHere.getPrison().saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================HELPERS==================================================*/
        /*==============================================================================================*/
        public static bool CheckForAtleastOneClaimedPlotOnBorderSameCity(Plot plot)
        {
            PlotPosition cl = plot.plotPosition.Clone();
            cl.getPos().Add(-1, 0);
            Plot foundPlot;
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(2, 0);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(-1, 0);
            cl.getPos().Add(0, -1);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(0, 2);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool HelperFunctionRank(IServerPlayer player, string rankNamePassed, string playerName, out City city, out PlayerInfo targetPlayer, TextCommandResult tcr)
        {
            city = null;
            targetPlayer = null;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player_info"));
                return false;
            }

            city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            if (playerInfo.hasCity() && !playerInfo.City.Equals(city))
            {
                tcr.StatusMessage = "claims:player_should_be_in_same_city";
                return false;
            }

            string rankName = Filter.filterName(rankNamePassed);
            if (rankName.Length == 0 || !Filter.checkForBlockedNames(rankName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            if (!RightsHandler.ExistCityRank(rankName))
            {
                tcr.StatusMessage = "claims:no_such_city_rank";
                return false;
            }
            string targetPlayerName = Filter.filterName(playerName);
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            claims.dataStorage.getPlayerByName(targetPlayerName, out targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            return true;
        }
        public static bool HelperFunctionPrison(IServerPlayer player, out City city, out Plot plotHere, TextCommandResult tcr)
        {
            city = null;
            plotHere = null;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            city = playerInfo.City;
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:not_claimed";
                return false;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return false;
            }
            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return false;
            }
            if (!(plotHere.getType() == PlotType.PRISON))
            {
                tcr.StatusMessage = "claims:not_prison_here";
                return false;
            }
            return true;
        }
        /*
       
       
        
        public static TextCommandResult processSummonSet(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            City city;
            PlayerInfo playerInfo;
            if(!helperFunctionSummon(player, tcr, out city, out playerInfo))
            {
                return tcr;
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:plot_not_claimed";
                return tcr;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }
            if (!plotHere.getCity().Equals(city))
            {
                tcr.StatusMessage = "claims:not_same_city";
                return tcr;
            }
            if (plotHere.getType() != PlotType.SUMMON)
            {
                tcr.StatusMessage = "claims:need_summon_plot";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_SET_SUMMON))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
                (plotHere.getPlotDesc() as PlotDescSummon).setSummonCoords(player.Entity.ServerPos.XYZ.Clone());
            tcr.StatusMessage = "claims:summon_point_set_to";
            tcr.MessageParams = new object[] { player.Entity.ServerPos.XYZ.ToString() };
            tcr.Status = EnumCommandStatus.Success;
            return tcr;

        }
        public static TextCommandResult processSummonTeleport(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            City city;
            PlayerInfo playerInfo;
            if (!helperFunctionSummon(player, tcr, out city, out playerInfo))
            {
                return tcr;
            }
            int index = 0;
            if (args.LastArg != null)
            {
                index = (int)args.LastArg;
            }
            if (index < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }
            if (index >= city.summonPlots.Count)
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            long stamp = CooldownHandler.hasCooldown(playerInfo, CooldownType.SUMMON);
            if (stamp != 0)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:wait_before") + TimeFunctions.getHourFromEpochSeconds(stamp));
                return tcr;
            }
            Plot chosenPlot = null;
            int counter = 0;
            foreach (var it in city.summonPlots)
            {
                if (counter == index)
                {
                    chosenPlot = it;
                    break;
                }
                counter++;
            }
            if (chosenPlot == null)
            {
                return tcr;
            }
            if (claims.config.SUMMON_MIN_PLAYERS != 0 &&
                claims.sapi.World.GetPlayersAround((chosenPlot.getPlotDesc() as PlotDescSummon).getSummonCoords(),
                claims.config.SUMMON_HOR_RANGE,
                claims.config.SUMMON_VER_RANGE).Count() < claims.config.SUMMON_MIN_PLAYERS)
            {
                tcr.StatusMessage = "claims:need_more_players_for_summon";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }

            if (playerInfo.Account.getBalance() < claims.config.SUMMON_PAYMENT)
            {
                tcr.StatusMessage = "claims:not_enough_money";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            playerInfo.Account.withdraw(claims.config.SUMMON_PAYMENT, true);
            if (TeleportationHandler.addTeleportation(new TeleportationInfo(playerInfo,
                (chosenPlot.getPlotDesc() as PlotDescSummon).getSummonCoords(), true, TimeFunctions.getEpochSeconds() + claims.config.SECONDS_SUMMON_TIME)))
            {
                tcr.StatusMessage = "claims:you_will_be_summoned";
                tcr.MessageParams = new object[] { claims.config.SECONDS_SUMMON_TIME };
                tcr.Status = EnumCommandStatus.Success;
            }
            return tcr;
        }
        public static bool helperFunctionSummon(IServerPlayer player, TextCommandResult tcr, out City city, out PlayerInfo playerInfo)
        {
            city = null;
            playerInfo = null;

            if (!claims.config.SUMMON_ALLOWED)
            {
                tcr.StatusMessage = "claims:summon_is_not_allowed";
                return false;
            }
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }
            city = playerInfo.City;
            return true;
        }       
        public static TextCommandResult processSummonList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            if(!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            City city = playerInfo.City;
            tcr.StatusMessage = StringFunctions.getSummonPoints(city);
            tcr.Status = EnumCommandStatus.Success;
            return tcr; 
        }
        public static TextCommandResult processCityRankList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            if (args.LastArg == null)
            {
                MessageHandler.sendMsgToPlayer(player, StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:your_ranks"),
                    playerInfo.getCityTitles(),
                    ", "));
                return tcr;
            }

            string targetNames = Filter.filterName((string)args.LastArg);;
            if (targetNames.Length == 0 || !Filter.checkForBlockedNames(targetNames))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_RANK_SHOW_OTHER))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(targetNames, out PlayerInfo targetPlayerInfo);
            if (targetPlayerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            if (playerInfo.hasCity() && playerInfo.City.Equals(targetPlayerInfo.City))
            {
                MessageHandler.sendMsgToPlayer(player, StringFunctions.concatStringsWithPrefixAndDelim(
                    Lang.Get("claims:player_ranks", targetPlayerInfo.GetPartName()),
                    targetPlayerInfo.getCityTitles(),
                    ", "));
            }
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
            
        }

        public static TextCommandResult processCityCriminalList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            tcr.StatusMessage = "claims:criminals";
            tcr.MessageParams = new object[] { StringFunctions.makeStringPlayersName(city.getCriminals(), ',') };

            return tcr;
        }
        public static TextCommandResult processCityCriminalAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            City targetCity = null;
            PlayerInfo targetPlayer = null;
            if(args.LastArg == null)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }
            if(!helperFunctionCriminal(player, (string)args.LastArg, out targetCity, out targetPlayer, tcr))
            {
                return tcr;
            }
            if (city.getCriminals().Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:already_added_as_criminal";
                return tcr;
            }
            city.getCriminals().Add(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_has_been_added_to_criminals", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("criminals:you_were_added_criminals_in_city", city.GetPartName()));
            city.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult processCityCriminalRemove(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            City targetCity = null;
            PlayerInfo targetPlayer = null;
            if (args.LastArg == null)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }
            if (!helperFunctionCriminal(player, (string)args.LastArg, out targetCity, out targetPlayer, tcr))
            {
                return tcr;
            }
            if (!city.getCriminals().Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:not_criminal_here";
                return tcr;
            }
            city.getCriminals().Remove(targetPlayer);
            MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_has_been_removed_from_criminals", targetPlayer.GetPartName()));
            MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("criminals:you_were_removed_criminals_in_city", city.GetPartName()));
            city.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static bool helperFunctionCriminal(IServerPlayer player, string targetPlayerName, out City targetCity, out PlayerInfo targetPlayer, TextCommandResult tcr)
        {
            targetCity = null;
            targetPlayer = null;
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return false;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return false;
            }

            if (!RightsHandler.hasRight(player, PermConstants.CITY_ADD_CRIMINAL))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return false;
            }
            string name = Filter.filterName(targetPlayerName);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            claims.dataStorage.getPlayerByName(name, out targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return false;
            }
            targetCity = targetPlayer.City;
            if (targetCity != null && city.Equals(targetCity))
            {
                tcr.StatusMessage = "claims:kick_before_add";
                return false;
            }
            return true;
        }
        
        
        public static bool checkForAtleastOneClaimedPlotOnBorderSameCity(Plot plot, TextCommandResult res)
        {
            PlotPosition cl = plot.plotPosition.Clone();
            cl.getPos().Add(-1, 0);
            Plot foundPlot;
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if(foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(2, 0);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(-1, 0);
            cl.getPos().Add(0, -1);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            cl.getPos().Add(0, 2);
            if (claims.dataStorage.getPlot(cl, out foundPlot))
            {
                if (foundPlot.hasCity() && foundPlot.getCity().Equals(plot.getCity()))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static void processInviteList(IServerPlayer player, CmdArgs args, TextCommandResult res)
        {
            //RECEIVED
            //SENT
        }
        public static TextCommandResult PlotsGroupSet(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (!(args.Parsers[0].GetValue() as string).Equals("p", StringComparison.OrdinalIgnoreCase)
                && !(args.Parsers[0].GetValue() as string).Equals("permissions", StringComparison.OrdinalIgnoreCase))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:wrong_param"));
                return tcr;
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player_info"));
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_city"));
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_SET))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_right_for_that_command"));
                return tcr;
            }

            string name = Filter.filterName((args.Parsers[1].GetValue() as string));
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_group_name"));
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_group_found"));
                return tcr;
            }
            PermGroup permGroup;
            switch ((args.Parsers[2].GetValue() as string))
            {
                case "citizen":
                    permGroup = PermGroup.CITIZEN;
                    break;
                case "stranger":
                    permGroup = PermGroup.STRANGER;
                    break;
                case "ally":
                    permGroup = PermGroup.ALLY;
                    break;
                default:
                    return tcr;
            }
            PermType permType;
            switch ((args.Parsers[3].GetValue() as string))
            {
                case "use":
                    permType = PermType.USE_PERM;
                    break;
                case "build":
                    permType = PermType.BUILD_AND_DESTROY_PERM;
                    break;
                case "attack":
                    permType = PermType.ATTACK_ANIMALS_PERM;
                    break;
                default:
                    return tcr;
            }
            searchedGroup.getPermsHandler().setPerm(permGroup, permType, getBoolFromString((args.Parsers[4].GetValue() as string)));
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:for_plotsgroup_group_perm_set_what", name, permGroup, (args.Parsers[3].GetValue() as string), (args.Parsers[4].GetValue() as string)));
            searchedGroup.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult PlotsGroupListPlayers(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_LIST))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            if (!city.Equals(playerInfo.City))
            {
                tcr.StatusMessage = "claims:player_should_be_in_same_city";
                return tcr;
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            MessageHandler.sendMsgToPlayer(player, StringFunctions.makeFeasibleStringFromNames(
                StringFunctions.getNamesOfPartsForChat(
                    Lang.Get("claims:plots_group_members", searchedGroup.GetPartName()),
                    new List<Part>(searchedGroup.getPlayerInfos())), ','));
            tcr.Status = EnumCommandStatus.Success;
            return  tcr;
        }
        public static TextCommandResult PlotsGroupList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_LIST))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            if(!city.Equals(playerInfo.City))
            {
                tcr.StatusMessage = "claims:player_should_be_in_same_city";
                return tcr;
            }
            MessageHandler.sendMsgToPlayer(player,
                StringFunctions.makeFeasibleStringFromNames(
                    StringFunctions.getNamesOfPartsForChat(Lang.Get("claims:city_groups"),
                   new List<Part>(city.getCityPlotsGroups())), ','));
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        //CREATE NEW GROUP BY NAME
        public static TextCommandResult PlotsGroupCreate(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if(city.getCityPlotsGroups().Count() > 10)
            {
                tcr.StatusMessage = "claims:too_much_plot_groups";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_CREATE))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if(group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if(searchedGroup != null)
            {
                tcr.StatusMessage = "claims:group_already_exists";
                return tcr;
            }
            string newGuid = "";
            while(true)
            {
                Guid guid = Guid.NewGuid();
                if(claims.dataStorage.PlotsGroupExistsByGUID(guid.ToString()))
                {
                    continue;
                }
                else 
                {
                    newGuid = guid.ToString();
                    break;
                }
            }
            
            searchedGroup = new CityPlotsGroup(name, newGuid);
            tcr.StatusMessage = "claims:plotsgroup_was_created";
            tcr.MessageParams = new object [] { searchedGroup.GetPartName() };
            city.getCityPlotsGroups().Add(searchedGroup);
            claims.dataStorage.addPlotsGroup(searchedGroup);
            searchedGroup.setCity(city);
            city.saveToDatabase();
            searchedGroup.saveToDatabase(false);
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult PlotsGroupDelete(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_DELETE))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(name))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            tcr.StatusMessage = "claims:plotsgroup_was_deleted";
            tcr.MessageParams = new object[] { searchedGroup.GetPartName() };
            city.getCityPlotsGroups().Remove(searchedGroup);
            claims.dataStorage.removePlotsGroup(searchedGroup.Guid);
            city.saveToDatabase();
            claims.getModInstance().getDatabaseHandler().deleteFromDatabaseCityPlotGroup(searchedGroup);
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        //ADD PLAYER TO GROUP
        public static TextCommandResult PlotsGroupAddTo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_name_for_group_and_player";
                return tcr;
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_ADD))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            string groupName = Filter.filterName(args.RawArgs[0]);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            string playerName = Filter.filterName(args.RawArgs[1]);
            if (playerName.Length == 0 || !Filter.checkForBlockedNames(playerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(playerName, out PlayerInfo targetPlayer);
            if(targetPlayer == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                return tcr;
            }
            if(searchedGroup.getPlayerInfos().Contains(targetPlayer))
            {
                tcr.StatusMessage = "claims:already_in_the_group";
                return tcr;
            }
            if(CityPlotsGroupInvitationsHandler.addNewCityPlotGroupInvitation(new CityPlotsGroupInvitation(
                playerInfo.City, targetPlayer, TimeFunctions.getEpochSeconds() + claims.config.PLOT_GROUP_INVITATION_TIMEOUT * TimeFunctions.secondsInAnHour,
               new Thread(new ThreadStart(() =>
               {
                   if(searchedGroup == null)
                   {
                       return;
                   }
                   searchedGroup.getPlayerInfos().Add(targetPlayer);
                   searchedGroup.saveToDatabase();
               })),
               new Thread(new ThreadStart(() =>
               {
                   //TODO
               })),
               searchedGroup.GetPartName())))
            {
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_invited_to_group", city.getPartNameReplaceUnder(), playerInfo.getPartNameReplaceUnder()));
                tcr.StatusMessage = "claims:you_invited_player_to_group";
                tcr.MessageParams = new object[] { targetPlayer.GetPartName(), searchedGroup.GetPartName() };
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:you_already_invited_player_in_one_of_city_group";
                tcr.MessageParams = new object[] { targetPlayer.GetPartName() };
                return tcr;
            }
        }
        public static TextCommandResult PlotsGroupUnaddTo(TextCommandCallingArgs args)
        {
            //TODO
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;


            tcr.StatusMessage = "todo";
            return tcr;
        }
        public static TextCommandResult PlotsGroupKickFrom(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            if (args.RawArgs.Length < 2)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:need_name_for_group_and_player"));
                return tcr;
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player_info"));
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_city"));
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_KICK))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_right_for_that_command"));
                return tcr;
            }
            string groupName = Filter.filterName(args.RawArgs[0]);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_group_name"));
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_group_found"));
                return tcr;
            }
            string playerName = Filter.filterName(args.RawArgs[1]);
            if (playerName.Length == 0 || !Filter.checkForBlockedNames(playerName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_player_name"));
                return tcr;
            }
            claims.dataStorage.getPlayerByName(playerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                return tcr;
            }
            if (!searchedGroup.getPlayerInfos().Contains(targetPlayer))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_in_the_group"));
                return tcr;
            }
            searchedGroup.getPlayerInfos().Remove(targetPlayer);
            searchedGroup.saveToDatabase();

            tcr.Status = EnumCommandStatus.Success;
            return tcr;

        }
        public static TextCommandResult processPlotRemove(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            //ADD PLOTGROUPNAME

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_REMOVEPLOTS))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            claims.dataStorage.getPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out Plot plot);
            //NO CLAIMED PLOT HERE || VILLAGE HERE || PLOT NOT OURS
            if (plot == null || !plot.getCity().Equals(playerInfo.City) || !plot.hasCityPlotsGroup())
            {
                tcr.StatusMessage = "claims:cannot_remove_from_group";
                return tcr;
            }
            string groupName = Filter.filterName((string)args.LastArg);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }
            if(!plot.getPlotGroup().Equals(searchedGroup))
            {
                tcr.StatusMessage = "claims:different_groups";
                return tcr;
            }
            plot.setPlotGroup(null);
            plot.saveToDatabase();

            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult processPlotAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;

            //ADD PLOTGROUPNAME
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player_info";
                return tcr;
            }
            City city = playerInfo.City;
            if (city == null)
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.CITY_PLOTGROUP_ADDPLOTS))
            {
                tcr.StatusMessage = "claims:you_dont_have_right_for_that_command";
                return tcr;
            }
            claims.dataStorage.getPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out Plot plot);
            //NO CLAIMED PLOT HERE || VILLAGE HERE || PLOT NOT OURS
            if(plot == null || !plot.getCity().Equals(playerInfo.City))
            {
                tcr.StatusMessage = "claims:cannot_add_to_group";
                return tcr;
            }
            string groupName = Filter.filterName((string)args.LastArg);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                tcr.StatusMessage = "claims:invalid_group_name";
                return tcr;
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in city.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }
            if (searchedGroup == null)
            {
                tcr.StatusMessage = "claims:no_such_group_found";
                return tcr;
            }

            //DELETE OWNER, RECALCULATE HIS RIGHTS AND HIS COMRADES
            if(plot.hasPlotOwner())
            {
                PlayerInfo tmp = plot.getPlotOwner();
                plot.setPlotOwner(null);
                RightsHandler.reapplyRights(tmp);
                foreach(var it in tmp.Friends)
                {
                    RightsHandler.reapplyRights(it);
                }
            }
            plot.setPlotGroup(searchedGroup);
            plot.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }


      */
    }
}
