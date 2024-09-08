﻿using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class PlotCommand: BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult HereInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:plot_not_claimed");
            }

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (!isOwnerOfPlotMayorAdmin(plot, playerInfo, player))
            {
                return TextCommandResult.Error("");
            }
            return TextCommandResult.Success(string.Join("", plot.getStatus())); 
        }
        public static TextCommandResult plotBorders(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (((string)args.LastArg).Equals("on", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = true;
                PlotPosition.makeChunkHighlight(claims.sapi.World, player);
                return TextCommandResult.Success();
            }
            else if (((string)args.LastArg).Equals("off", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = false;
                PlotPosition.clearChunkHighlight(claims.sapi.World, player);
                return TextCommandResult.Success();
            }
            return TextCommandResult.Success();
        }
        /*==============================================================================================*/
        /*=====================================CLAIM====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult PlotClaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            if (plot.hasPlotOwner())
            {
                return TextCommandResult.Error("claims:plot_has_owner_already");
            }

            if (!plot.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }
            if (plot.hasCityPlotsGroup())
            {
                return TextCommandResult.Error("claims:has_plot_group");
            }
            if (plot.getPrice() > (double)claims.economyHandler.getBalance(playerInfo.Guid))
            {
                return TextCommandResult.Error("claims:not_enough_money");
            }
            if (plot.getType() == PlotType.EMBASSY || (playerInfo.hasCity() && plot.getCity().Equals(playerInfo.City)))
            {
                //Save price localy or do not move after we change plot price
                decimal savedPrice = (decimal)plot.getPrice();

                if(claims.economyHandler.depositFromAToB(playerInfo.MoneyAccountName, plot.getCity().MoneyAccountName, (decimal)plot.getPrice()).ResultState 
                    == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                {
                    plot.setPrice(-1);
                    plot.getPermsHandler().setPerm(playerInfo.PermsHandler);
                    plot.setPlotOwner(playerInfo);
                    playerInfo.PlayerPlots.Add(plot);
                    plot.saveToDatabase();
                    playerInfo.saveToDatabase();
                    UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                    return SuccessWithParams("claims:plot_has_been_claimed_by_player_paid", new object[] { savedPrice });
                }
                else
                {
                    return TextCommandResult.Error("claims:economy_money_transaction_error");
                }
            }
            return TextCommandResult.Success();
        }
        public static TextCommandResult PlotUnclaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("claims:no_such_player");
            }

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Error("claims:no_city_here");
            }

            if ((plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo)) || plot.getCity().isMayor(playerInfo))
            {
                if (plot.hasCityPlotsGroup())
                {
                    return TextCommandResult.Error("claims:has_plots_group");
                }
                plot.setPlotOwner(null);
                playerInfo.PlayerPlots.Remove(plot);
                playerInfo.saveToDatabase();
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
                plot.saveToDatabase();
                return TextCommandResult.Success("claims:plot_has_been_unclaimed_by_player");
            }
            else
            {
                return TextCommandResult.Error("claims:no_plots_here");
            }
        }
        /*==============================================================================================*/
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult SetPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            Plot plotHere;
            if (!HelperFunctionSetFlag(player, out plotHere, tcr))
            {
                return tcr;
            }
            //flag is the same
            if (!plotHere.getPermsHandler().setPvp((string)args.LastArg))
            {
                return tcr;
            }
            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            Plot plotHere;
            if (!HelperFunctionSetFlag(player, out plotHere, tcr))
            {
                return tcr;
            }

            if (!plotHere.getPermsHandler().setFire((string)args.LastArg))
            {
                return tcr;
            }

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            Plot plotHere;
            if (!HelperFunctionSetFlag(player, out plotHere, tcr))
            {
                return tcr;
            }

            if (!plotHere.getPermsHandler().setBlast((string)args.LastArg))
            {
                return tcr;
            }

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult SetName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Success("claims:no_city_here");
            }
            if ((plot.getCity().Equals(playerInfo.City) && !(plot.getPlotOwner()?.Equals(playerInfo) ?? false) && !(playerInfo.City?.isMayor(playerInfo) ?? true)))
            {
                return TextCommandResult.Success("claims:not_your_city");
            }

            if ((!plot.getCity().Equals(playerInfo.City) && !plot.getPlotOwner().Equals(playerInfo)))
            {
                return TextCommandResult.Success("claims:not_your_city");
            }
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_plot_name");
            }
            if (!plot.SetPartName(name))
            {
                return TextCommandResult.Success();
            }

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plot.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            plot.saveToDatabase();
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            int tax = (int)args.LastArg;

            if (tax < 0)
            {
                return TextCommandResult.Success("claims:not_negative");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                return TextCommandResult.Success("claims:plot_not_claimed");
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            if (!plotHere.hasCity())
            {
                return TextCommandResult.Success("claims:no_city_here");
            }

            if (!plotHere.getCity().Equals(playerInfo.City))
            {
                return TextCommandResult.Success("claims:player_should_be_in_same_city");
            }

            if (!plotHere.setCustomTax(tax))
            {
                return TextCommandResult.Success();
            }

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
            plotHere.saveToDatabase();
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;


            if (PlotInfo.nameToPlotType.ContainsKey((string)args.LastArg))
            {
                PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
                claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
                if (plotHere == null)
                {
                    return TextCommandResult.Success("claims:plot_not_claimed");
                }

                claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    return TextCommandResult.Success("claims:no_such_player");
                }

                if (!plotHere.hasCity())
                {
                    return TextCommandResult.Success("claims:no_city_here");
                }

                if (!plotHere.getCity().Equals(playerInfo.City))
                {
                    return TextCommandResult.Success("claims:player_should_be_in_same_city");
                }

                TextCommandResult tcr = new TextCommandResult();
                tcr.Status = EnumCommandStatus.Success;
                if (!plotHere.setNewType(tcr, (string)args.LastArg, player))
                {
                    return tcr;
                }
                claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plotHere.getPos());
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plotHere.getPos());
                UsefullPacketsSend.SendCurrentPlotUpdate(player, plotHere);
                return tcr;
            }
            else
            {
                return TextCommandResult.Error("");
            }
        }
        public static TextCommandResult SetPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success();
            }

            if (args.RawArgs.Length < 3)
            {
                return TextCommandResult.Success();
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (!isOwnerOfPlotMayorAdmin(plot, playerInfo, player))
            {
                return TextCommandResult.Success();
            }

            plot.getPermsHandler().setAccessPerm(args.RawArgs);

            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plot.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            plot.saveToDatabase();
            claims.dataStorage.clearCacheForPlayersInPlot(plot);
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success();
        }
        public static TextCommandResult SetForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Success("claims:no_city_here");
            }

            if (!plot.getCity().Equals(playerInfo.City))
            {
                return TextCommandResult.Success("claims:not_your_city");
            }
            int price = (int)args.LastArg;

            if (price < 0)
            {
                return TextCommandResult.Success("claims:try_pos");
            }
            plot.setPrice(price);
            plot.saveToDatabase();
            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plot.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return SuccessWithParams("claims:plot_is_for_sale", new object[] { price });
        }
        public static TextCommandResult SetNotForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Success("claims:no_such_player");
            }

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return TextCommandResult.Success("claims:no_plots_here");
            }
            if (!plot.hasCity())
            {
                return TextCommandResult.Success("claims:no_city_here");
            }
            if (!plot.getCity().Equals(playerInfo.City))
            {
                return TextCommandResult.Success("claims:not_your_city");
            }

            plot.setPrice(-1);
            plot.saveToDatabase();
            claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plot.getPos());
            claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            UsefullPacketsSend.SendCurrentPlotUpdate(player, plot);
            return TextCommandResult.Success("claims:plot_is_not_for_sale");
        }

        /*==============================================================================================*/
        /*=====================================HELPERS==================================================*/
        /*==============================================================================================*/
        public static bool HelperFunctionSetFlag(IServerPlayer player, out Plot plotHere, TextCommandResult tcr)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out plotHere);
            if (plotHere == null)
            {
                tcr.StatusMessage = "claims:plot_not_claimed";
                return false;
            }

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return false;
            }

            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return false;
            }
            if (!plotHere.getCity().getPlayerInfos().Contains(playerInfo) && (!plotHere.hasPlotOwner() || !plotHere.getPlotOwner().Equals(playerInfo)))
            {
                tcr.StatusMessage = "claims:no_city_here";
                return false;
            }
            return true;
        }


        /*public static TextCommandResult plotMsgs(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo2);
            switch((string)args.LastArg)
            {
                case "0":
                    playerInfo2.showPlotMovement = EnumShowPlotMovement.SHOW_NONE;
                    return tcr;
                case "1":
                    playerInfo2.showPlotMovement = EnumShowPlotMovement.SHOW_MESSAGE;
                    return tcr;
                case "2":
                    playerInfo2.showPlotMovement = EnumShowPlotMovement.SHOW_HUD;
                    return tcr;
                case "3":
                    playerInfo2.showPlotMovement = EnumShowPlotMovement.SHOW_MESSAGE_HUD;
                    return tcr;
            }
            return tcr;
        }
        public static TextCommandResult plotBorders(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (((string)args.LastArg).Equals("on", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = true;
                PlotPosition.makeChunkHighlight(claims.sapi.World, player);
                return tcr;
            }
            else if (((string)args.LastArg).Equals("off", StringComparison.OrdinalIgnoreCase) && playerInfo != null)
            {
                playerInfo.showBorders = false;
                PlotPosition.clearChunkHighlight(claims.sapi.World, player);
                return tcr;
            }
            return tcr;
        }
        public static TextCommandResult processInnerClaim(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            if (args.RawArgs.Length == 0)
            {
                return tcr;
            }
            if (!RightsHandler.hasRight(player, PermConstants.PLOT_INNER_CLAIM))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_right_for_that_command"));
                return tcr;
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_not_claimed"));
                return tcr;
            }
            if(plotHere.getType() != PlotType.TAVERN)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:only_for_tavern"));
                return tcr;
            }
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (!(plotHere.hasPlotOwner() && plotHere.getPlotOwner().Equals(playerInfo))
                && !plotHere.getCity().Equals(playerInfo.City))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_owner_or_mayor_needed"));
                return tcr;
            }
 

            //CHECKS FOR RIGHTS
            if (args.RawArgs[0].Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                if(!claims.dataStorage.getInnerClaimRecord(player.PlayerUID, out InnerClaimRecord innerClaimRecord))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_record"));
                    return tcr;
                }
                if(innerClaimRecord.pos1 == null || innerClaimRecord.pos2 == null)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:need_both_points"));
                    return tcr;
                }
                claims.dataStorage.getPlot(new PlotPosition(innerClaimRecord.plotCoords), out Plot plot);
                if(plot == null)
                {
                    claims.dataStorage.tryRemoveClaimRecord(player.PlayerUID);
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_plot_here"));
                    return tcr;
                }

                if(!(plot.getPlotDesc() is PlotDescTavern))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:wrong_plot_type"));
                    return tcr;
                }
                if((plot.getPlotDesc() as PlotDescTavern).innerClaims.Count > claims.config.MAX_NUMBER_INNER_CLAIM_PER_TAVERN)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:too_much_inner_claims"));
                    return tcr;
                }
                foreach(var it in (plot.getPlotDesc() as PlotDescTavern).innerClaims)
                {
                    if(it.Intersects(new InnerClaim(innerClaimRecord.pos1, innerClaimRecord.pos2)))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:intersects_with_existing_inner_claim"));
                        return tcr;
                    }
                }

                (plot.getPlotDesc() as PlotDescTavern).addNewInnerClaim(innerClaimRecord.pos1, innerClaimRecord.pos2);
                claims.dataStorage.tryRemoveClaimRecord(player.PlayerUID);
                plot.saveToDatabase();
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:inner_claim_was_added"));
                return tcr;
                //WE HAVE 2 POINTS - TRY CREATE
            }
            else if (args.RawArgs[0].Equals("start", StringComparison.OrdinalIgnoreCase))
            {
                if(!claims.dataStorage.getInnerClaimRecord(player.PlayerUID, out InnerClaimRecord innerClaimRecord))
                {
                    InnerClaimRecord tmpAdd = new InnerClaimRecord();
                    claims.dataStorage.addClaimRecord(player.PlayerUID, tmpAdd);
                    tmpAdd.plotCoords = new Vec2i((int)player.Entity.ServerPos.X / 16, (int)player.Entity.ServerPos.Z / 16);                  
                }
                claims.dataStorage.getInnerClaimRecord(player.PlayerUID, out InnerClaimRecord tmp);
                if(player.CurrentBlockSelection == null)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_selection"));
                    return tcr;
                }

                if (player.CurrentBlockSelection.Position.X / 16 == tmp.plotCoords.X
                    && player.CurrentBlockSelection.Position.Z / 16 == tmp.plotCoords.Y)
                {
                    tmp.pos1 = new Vec3i();
                    tmp.pos1.X = player.CurrentBlockSelection.Position.X;
                    tmp.pos1.Y = player.CurrentBlockSelection.Position.Y;
                    tmp.pos1.Z = player.CurrentBlockSelection.Position.Z;
                }
                else
                {
                    tmp.plotCoords.X = player.CurrentBlockSelection.Position.X/16;
                    tmp.plotCoords.Y = player.CurrentBlockSelection.Position.Z / 16;
                    tmp.pos1.X = player.CurrentBlockSelection.Position.X;
                    tmp.pos1.Y = player.CurrentBlockSelection.Position.Y;
                    tmp.pos1.Z = player.CurrentBlockSelection.Position.Z;
                    tmp.pos2 = null;
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_was_changed"));
                }
                reapplyHighlightRecord(player, tmp);
                return tcr;
                //IF NO RECORD - CREATE
                //POS1 ADD
            }
            else if (args.RawArgs[0].Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                if (!claims.dataStorage.getInnerClaimRecord(player.PlayerUID, out InnerClaimRecord innerClaimRecord))
                {
                    InnerClaimRecord tmpAdd = new InnerClaimRecord();
                    claims.dataStorage.addClaimRecord(player.PlayerUID, tmpAdd);
                    tmpAdd.plotCoords = new Vec2i((int)player.Entity.ServerPos.X / 16, (int)player.Entity.ServerPos.Z / 16);
                }
                claims.dataStorage.getInnerClaimRecord(player.PlayerUID, out InnerClaimRecord tmp);
                if(player.CurrentBlockSelection == null)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_selection"));
                    return tcr;
                }
                if ((player.CurrentBlockSelection.Position.X / 16 == tmp.plotCoords.X)
                    && (player.CurrentBlockSelection.Position.Z / 16 == tmp.plotCoords.Y)
                    )
                {
                    tmp.pos2 = new Vec3i();                 
                    tmp.pos2.X = player.CurrentBlockSelection.Position.X;
                    tmp.pos2.Y = player.CurrentBlockSelection.Position.Y;
                    tmp.pos2.Z = player.CurrentBlockSelection.Position.Z;
                }
                else
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:out_of_plot"));
                    return tcr;
                }
                reapplyHighlightRecord(player, tmp);
                return tcr;
                //IF NO RECORD - CREATE
                //POS2 ADD
            }
            else if (args.RawArgs[0].Equals("delete", StringComparison.OrdinalIgnoreCase))
            {

                if (args.RawArgs.Length < 2)
                {
                    return tcr;
                }
                try
                {
                    int claimNumber = int.Parse(args.RawArgs[1]);
                    if (claimNumber < 0)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                    if((plotHere.getPlotDesc() as PlotDescTavern).innerClaims.Count - 1 < claimNumber)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                    (plotHere.getPlotDesc() as PlotDescTavern).innerClaims.RemoveAt(claimNumber);
                    plotHere.saveToDatabase();
                    return tcr;
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                //BY NUMBER
            }
            else if (args.RawArgs[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                MessageHandler.sendMsgToPlayer(player, (plotHere.getPlotDesc() as PlotDescTavern).innerClaimsToString());
                return tcr;
                //NUMBER. RIGHTS. MEMBERS.
            }
            else if (args.RawArgs[0].Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                if(args.RawArgs.Length < 3)
                {
                    return tcr;
                }
                try
                {
                    int claimNumber = int.Parse(args.RawArgs[1]);
                    if(claimNumber < 0)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                    string name = Filter.filterName(args.RawArgs[2]);
                    if (!claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayerInfo))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                        return tcr;
                    }
                    else
                    {
                        
                        if((plotHere.getPlotDesc() as PlotDescTavern).innerClaims.Count - 1 < claimNumber)
                        {
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                            return tcr;
                        }
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].membersUids.Add(targetPlayerInfo.Guid);
                        plotHere.saveToDatabase();
                        return tcr;
                    }
                }catch(FormatException e)
                {
                    return tcr;
                }

                //NUMBER PLAYER'S NAME
            }
            else if (args.RawArgs[0].Equals("kick", StringComparison.OrdinalIgnoreCase))
            {
                if (args.RawArgs.Length < 3)
                {
                    return tcr;
                }
                try
                {
                    int claimNumber = int.Parse(args.RawArgs[1]);
                    if (claimNumber < 0)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                    string name = Filter.filterName(args.RawArgs[2]);
                    if (!claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayerInfo))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                        return tcr;
                    }
                    else
                    {

                        if ((plotHere.getPlotDesc() as PlotDescTavern).innerClaims.Count - 1 < claimNumber)
                        {
                            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                            return tcr;
                        }
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].membersUids.Remove(targetPlayerInfo.Guid);
                        plotHere.saveToDatabase();
                        return tcr;
                    }
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                //NUMBER PLAYER'S NAME
            }
            else if (args.RawArgs[0].Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                if (args.RawArgs.Length < 2)
                {
                    return tcr;
                }
                try
                {
                    int claimNumber = int.Parse(args.RawArgs[1]);
                    if (claimNumber < 0)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }                  

                    if ((plotHere.getPlotDesc() as PlotDescTavern).innerClaims.Count - 1 < claimNumber)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                    reapplyHighlightInnerClaim(player, (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber]);
                    return tcr;
                    
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                //NUMBER
            }
            else if (args.RawArgs[0].Equals("set", StringComparison.OrdinalIgnoreCase)) 
            {
                if(args.RawArgs.Length < 4)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:need_claim_number_flag_value"));
                    return tcr;
                }
                int claimNumber = 0;
                try
                {
                    claimNumber = int.Parse(args.RawArgs[1]);
                    if (claimNumber < 0)
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                        return tcr;
                    }
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                if ((plotHere.getPlotDesc() as PlotDescTavern).innerClaims.Count - 1 < claimNumber)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:not_valid_number"));
                    return tcr;
                }
                if(args.RawArgs[2].Equals("build"))
                {
                    if(args.RawArgs[3].Equals("on"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[1] = true;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "build", "on"));
                    }
                    else if (args.RawArgs[3].Equals("off"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[1] = false;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "build", "off"));
                    }
                }
                else if (args.RawArgs[2].Equals("use"))
                {
                    if (args.RawArgs[3].Equals("on"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[0] = false;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "use", "on"));
                    }
                    else if (args.RawArgs[3].Equals("off"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[0] = false;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "use", "off"));
                    }
                }
                else if (args.RawArgs[2].Equals("attack"))
                {
                    if (args.RawArgs[3].Equals("on"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[2] = true;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "attack", "on"));
                    }
                    else if(args.RawArgs[3].Equals("off"))
                    {
                        (plotHere.getPlotDesc() as PlotDescTavern).innerClaims[claimNumber].permissionsFlags[2] = false;
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:flag_now_has_value", "attack", "off"));
                    }
                }

                plotHere.saveToDatabase();
                return tcr;
            }
            return tcr;
        }
        public static void reapplyHighlightRecord(IServerPlayer player, InnerClaimRecord innerClaimRecord)
        {
            if (innerClaimRecord.pos1 == null || innerClaimRecord.pos2 == null)
            {
                return;
            }
            List<BlockPos> bList = new List<BlockPos>();
            List<int> colors = new List<int>();
            colors.Add(ColorUtil.ToRgba(64, 255, 255, 0));
            var bp1 = new BlockPos(innerClaimRecord.pos1.X, innerClaimRecord.pos1.Y, innerClaimRecord.pos1.Z);
            var bp2 = new BlockPos(innerClaimRecord.pos2.X, innerClaimRecord.pos2.Y, innerClaimRecord.pos2.Z);
            if (innerClaimRecord.pos1.X > innerClaimRecord.pos2.X)
            {
                bp1.Add(1, 0, 0);
            }
            else
            {
                bp2.Add(1, 0, 0);
            }
            bList.Add(bp1);
            
            if (innerClaimRecord.pos1.Z > innerClaimRecord.pos2.Z)
            {
                bp1.Add(0, 0, 1); 
            }
            else
            {
                bp2.Add(0, 0, 1);
            }

            if (innerClaimRecord.pos1.Y > innerClaimRecord.pos2.Y)
            {
                bp1.Add(0, 1, 0);
            }
            else
            {
                bp2.Add(0, 1, 0);
            }

            bList.Add(bp2);
            claims.sapi.World.HighlightBlocks(player as IPlayer, 61, bList, colors, shape: EnumHighlightShape.Cube);
        }
        public static void reapplyHighlightInnerClaim(IServerPlayer player, InnerClaim innerClaim)
        {
            if (innerClaim.pos1 == null || innerClaim.pos2 == null)
            {
                return;
            }
            List<BlockPos> bList = new List<BlockPos>();
            List<int> colors = new List<int> ();
            colors.Add(ColorUtil.ToRgba(64, 255, 255, 0));
            var bp1 = new BlockPos(innerClaim.pos1.X, innerClaim.pos1.Y, innerClaim.pos1.Z);
            var bp2 = new BlockPos(innerClaim.pos2.X, innerClaim.pos2.Y, innerClaim.pos2.Z);
            if (innerClaim.pos1.X > innerClaim.pos2.X)
            {
                bp1.Add(1, 0, 0);
            }
            else
            {
                bp2.Add(1, 0, 0);
            }
            bList.Add(bp1);

            if (innerClaim.pos1.Z > innerClaim.pos2.Z)
            {
                bp1.Add(0, 0, 1);
            }
            else
            {
                bp2.Add(0, 0, 1);
            }

            if (innerClaim.pos1.Y > innerClaim.pos2.Y)
            {
                bp1.Add(0, 1, 0);
            }
            else
            {
                bp2.Add(0, 1, 0);
            }

            bList.Add(bp2);
            claims.sapi.World.HighlightBlocks(player, 61, bList, colors, shape: EnumHighlightShape.Cubes);
        }
        public static void processPlotSet(IServerPlayer player, CmdArgs args, TextCommandResult res)
        {
            if (args.Length == 0)
            {
                return;
            }


            else if (args[0].Equals("type", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:need_type"));
                    return;
                }
            }
            
            else if (args[0].Equals("fire", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }

                if (!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_such_player"));
                    return;
                }
                PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
                if (!claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_not_claimed"));
                    return;
                }
                if (!RightsHandler.hasRight(player, PermConstants.PLOT_SET_ALL))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_right_for_that_command"));
                    return;
                }

                if (!plotHere.hasCity())
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                    return;
                }
                if (!plotHere.getCity().getPlayerInfos().Contains(playerInfo) && (!plotHere.hasPlotOwner() || !plotHere.getPlotOwner().Equals(playerInfo)))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    plotHere.getPermsHandler().setFire(false);
                    plotHere.saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    plotHere.getPermsHandler().setFire(true);
                    plotHere.saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("blast", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_not_claimed"));
                    return;
                }
                PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
                claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
                if (plotHere == null)
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                    return;
                }
                if (!RightsHandler.hasRight(player, PermConstants.PLOT_SET_ALL))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:you_dont_have_right_for_that_command"));
                    return;
                }

                if (!plotHere.hasCity())
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                    return;
                }
                if (!plotHere.getCity().getPlayerInfos().Contains(playerInfo) && (!plotHere.hasPlotOwner() || !plotHere.getPlotOwner().Equals(playerInfo)))
                {
                    MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    plotHere.getPermsHandler().setBlast(false);
                    plotHere.saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    plotHere.getPermsHandler().setBlast(true);
                    plotHere.saveToDatabase();
                    return;
                }
            }

        }
      
       
      
      
       
       
        
       
       
        
              */
    }
}
