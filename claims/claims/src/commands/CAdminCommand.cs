﻿using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using claims.src.timers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class CAdminCommand : BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult triggerNextDay(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.sapi.Event.RegisterCallback((dt =>
            {
                new DayTimer().Run(false);
            }), 0);


            return tcr;
        }
        public static TextCommandResult triggerNextHour(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.sapi.Event.RegisterCallback((dt =>
            {
                new HourTimer().Run();
            }), 0);

            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================SET======================================================*/
        /*==============================================================================================*/
        public static TextCommandResult plotPermissions(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_command";
                return tcr;
            }

            plot.getPermsHandler().setAccessPerm(args.RawArgs);
            plot.saveToDatabase();
            claims.dataStorage.clearCacheForPlayersInPlot(plot);
            return tcr;
        }

        public static TextCommandResult plotPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setPvp((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setFire((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            plot.getPermsHandler().setBlast((string)args.LastArg);
            plot.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }
            if (PlotInfo.nameToPlotType.ContainsKey((string)args.LastArg))
            {
                plotHere.setNewType(tcr, (string)args.LastArg, player);
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:no_such_plot_type";
                return tcr;
            }
        }
        public static TextCommandResult plotFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }

            int tax = (int)args.LastArg;

            if (tax < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return tcr;
            }
            if (!plotHere.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }

            plotHere.setCustomTax(tax);
            plotHere.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult plotForSale(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plot);
            if (plot == null)
            {
                return tcr;
            }

            if (!plot.hasCity())
            {
                tcr.StatusMessage = "claims:no_city_here";
                return tcr;
            }

            int price = (int)args.LastArg;

            if (price < 0)
            {
                tcr.StatusMessage = "claims:try_pos";
                return tcr;
            }

            plot.setPrice(price);
            plot.saveToDatabase();
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================CITY=====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult cCreateCity(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                return tcr;
            }
            string newCityName = Filter.filterName((string)args.LastArg);

            if (newCityName.Length == 0 || !Filter.checkForBlockedNames(newCityName))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:invalid_new_city_name"));
                return tcr;
            }
            if (claims.dataStorage.cityExistsByName(newCityName))
            {
                return TextCommandResult.Error("claims:city_name_is_already_taken");
            }
            if (newCityName.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:city_name_is_too_long"));
                return tcr;
            }
            PartInits.initNewCity(null, currentPlotPosition, newCityName);
            return tcr;
        }
        public static TextCommandResult cityDelete(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            PartDemolition.demolishCity(city);

            return tcr;
        }
        public static TextCommandResult cityClaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            cCityClaim(player, null, city, tcr, true);
            return tcr;
        }
        public static TextCommandResult cityUnclaim(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName((string)args.LastArg);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            cCityUnclaim(player, null, city, tcr);
            return tcr;
        }
        public static TextCommandResult cCityPlayerKick(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 1)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            string targetPlayerName = Filter.filterName(args.RawArgs[1]);
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(targetPlayerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            if (city.isMayor(targetPlayer))
            {
                city.setMayor(null);
            }
            targetPlayer.clearCity();
            return tcr;
        }
        public static TextCommandResult cCityPlayerAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 1)
            {
                tcr.StatusMessage = "claims:need_player_name";
                return tcr;
            }

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            string targetPlayerName = Filter.filterName(args.RawArgs[1]);
            if (targetPlayerName.Length == 0 || !Filter.checkForBlockedNames(targetPlayerName))
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(targetPlayerName, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                tcr.StatusMessage = "claims:invalid_player_name";
                return tcr;
            }
            city.getCityCitizens().Add(targetPlayer);
            targetPlayer.setCity(city);
            city.saveToDatabase();
            targetPlayer.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult cCitySetName(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_value";
                return tcr;
            }

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.rename(args.RawArgs[1]);
            return tcr;
        }
        public static TextCommandResult citySetPvp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setPvp(args.RawArgs[1]);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetFire(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setFire((string)args.LastArg);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetBlast(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.getPermsHandler().setBlast(args.RawArgs[1]);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetOpenClosed(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.setCityOpenCloseState(args.RawArgs[1]);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetTechnical(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            city.setIsTechnicalCity(args.RawArgs[1], tcr);
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            string filteredName = Filter.filterName(args.RawArgs[0]);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            int fee = 0;
            try
            {
                fee = int.Parse(args.RawArgs[1]);
            }
            catch (FormatException e)
            {
                tcr.StatusMessage = "claims:need_number";
                return tcr;
            }
            if (fee < 0)
            {
                tcr.StatusMessage = "claims:not_negative";
                return tcr;
            }
            if (fee > claims.config.MAX_CITY_FEE)
            {
                fee = (int)claims.config.MAX_CITY_FEE;
            }

            city.fee = fee;
            city.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult citySetMayor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string filteredName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(filteredName, out City city);
            if (city == null)
            {
                tcr.StatusMessage = "claims:no_such_city";
                return tcr;
            }

            filteredName = Filter.filterName(args.Parsers[1].GetValue().ToString());
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                tcr.StatusMessage = "claims:invalid_name";
                return tcr;
            }
            claims.dataStorage.getPlayerByName(filteredName, out PlayerInfo playerInfo);

            if (playerInfo == null)
            {
                tcr.StatusMessage = "claims:no_such_player";
                return tcr;
            }
            if (playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:player_has_city";
                return tcr;
            }
            city.getPlayerInfos().Add(playerInfo);
            playerInfo.setCity(city);
            if (!city.isTechnicalCity())
            {
                PlayerInfo tmpPlayer = city.getMayor();
                city.getMayor().clearCity();
                city.getPlayerInfos().Remove(tmpPlayer);
                RightsHandler.reapplyRights(tmpPlayer);
            }
            city.setMayor(playerInfo);
            RightsHandler.reapplyRights(playerInfo);
            city.saveToDatabase();
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================WORLD=====================================================*/
        /*==============================================================================================*/
        public static TextCommandResult cWorldCommands(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                args.RawArgs.PopWord();
                worldSet(player, args.RawArgs, tcr);
            }
            else if (args.RawArgs[0].Equals("info", StringComparison.OrdinalIgnoreCase))
            {
                args.RawArgs.PopWord();
                worldInfo(player, args.RawArgs, tcr);
            }
            return tcr;
        }
        public static void worldInfo(IServerPlayer player, CmdArgs args, TextCommandResult tcr)
        {
            tcr.StatusMessage = string.Join("", claims.dataStorage.getWorldInfo().getStatus());
        }
        public static void worldSet(IServerPlayer player, CmdArgs args, TextCommandResult res)
        {
            //name, mayor, 
            if (args.Length < 2)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:need_value"));
                return;
            }

            if (args[0].Equals("blastew", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().blastEverywhere = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().blastEverywhere = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("pvpew", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().pvpEverywhere = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().pvpEverywhere = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("fireew", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().fireEverywhere = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().fireEverywhere = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("pvpfb", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().pvpForbidden = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().pvpForbidden = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("firefb", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().fireForbidden = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().fireForbidden = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
            else if (args[0].Equals("blastfb", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    return;
                }
                if (args[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().blastForbidden = true;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
                if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    claims.dataStorage.getWorldInfo().blastForbidden = false;
                    claims.dataStorage.getWorldInfo().saveToDatabase();
                    return;
                }
            }
        }
        public static TextCommandResult processBackup(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.sapi.Event.RegisterCallback((dt =>
            {
                claims.getModInstance().getDatabaseHandler().makeBackup(claims.config.MANUALLY_BACKUP_FILE_NAME);
            }), 0);
            return tcr;
        }
        /*==============================================================================================*/
        /*=====================================HELPER===================================================*/
        /*==============================================================================================*/
        public static void cCityUnclaim(IServerPlayer player, CmdArgs args, City city, TextCommandResult res)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere == null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_not_claimed"));
                return;
            }

            if (!plotHere.hasCity())
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:no_city_here"));
                return;
            }

            if (!plotHere.getCity().Equals(city))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:player_should_be_in_same_city"));
                return;
            }
            if (plotHere.getCity().getCityPlots().Count() == 1)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:last_city_plot"));
                return;
            }
            PartDemolition.demolishCityPlot(plotHere);
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_has_been_unclaimed", currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y));
        }
        public static void cCityClaim(IServerPlayer player, CmdArgs args, City city, TextCommandResult res, bool force = false)
        {
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z);
            claims.dataStorage.getPlot(currentPlotPosition, out Plot plotHere);
            if (plotHere != null)
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_already_claimed"));
                return;
            }
            plotHere = new Plot(currentPlotPosition);
            plotHere.setCity(city);
            if (!force && !claims.dataStorage.plotHasDistantEnoughFromOtherCities(plotHere))
            {
                MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:too_close_to_another_city"));
                return;
            }
            MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:plot_has_been_claimed", currentPlotPosition.getPos().X, currentPlotPosition.getPos().Y, 0));
            plotHere.setCity(city);
            plotHere.getPermsHandler().setPerm(city.getPermsHandler());
            plotHere.setPrice(-1);
            claims.dataStorage.addClaimedPlot(currentPlotPosition, plotHere);
            city.getCityPlots().Add(plotHere);
            city.saveToDatabase();
            plotHere.saveToDatabase();
            TreeAttribute tree = new TreeAttribute();
            tree.SetInt("chX", plotHere.getPos().X);
            tree.SetInt("chZ", plotHere.getPos().Y);
            tree.SetString("name", plotHere.getCity().GetPartName());
            claims.sapi.World.Api.Event.PushEvent("plotclaimed", tree);
            return;
        }
        /*public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            //player.Role.Privileges
            if (!player.Role.Code.Equals("admin"))        
            {             
                MessageHandler.sendMsgToPlayer(player, Lang.GetL(player.LanguageCode, "claims:you_dont_have_right_for_that_command"));
                return tcr;
            }
            if (args.RawArgs.Length == 0)
            {

            }

            string firstArg = args.RawArgs.PopWord().ToLower();

            return tcr;
        }
      
        public static TextCommandResult processSetCityPlotsColor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 2)
            {
                tcr.StatusMessage = "claims:need_name_and_color";
                return tcr;
            }

            if(!ColorHandling.tryFindColor(args.RawArgs[1], out int color))
            {
                tcr.StatusMessage = "claims:unknown_color";
                return tcr;
            }

            string cityName = Filter.filterName(args.RawArgs[0]);

            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                tcr.StatusMessage = "claims:invalid_city_name";
                return tcr;
            }
            claims.dataStorage.getCityByName(cityName, out City city);
            if (city == null)
            {
                return tcr;
            }
            city.trySetPlotColor(tcr, color, args.RawArgs[1]);
            return tcr;
        }
        public static TextCommandResult processClaimsColor(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs.Length < 3)
            {
                tcr.StatusMessage = "claims:need_name_and_color";
                return tcr;
            }

            ColorHandling.tryFindColor(args.RawArgs[2], out int color);

            if (args.RawArgs[0].Equals("city"))
            {
                string cityName = Filter.filterName(args.RawArgs[1]);

                if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
                {
                    tcr.StatusMessage = "claims:invalid_city_name";
                    return tcr;
                }
                claims.dataStorage.getCityByName(cityName, out City city);
                if (city == null)
                {
                    return tcr;
                }
                city.trySetPlotColor(tcr, color, args.RawArgs[2]);
                return tcr;
            }        
            return tcr;
        }
       
      
       
       
        public static TextCommandResult plotType(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.getPlot(PlotPosition.fromXZ((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Z), out Plot plotHere);
            if (plotHere == null)
            {
                return tcr;
            }
            if (PlotInfo.nameToPlotType.ContainsKey((string)args.LastArg))
            {
                plotHere.setNewType(tcr, (string)args.LastArg, player);
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:no_such_plot_type";
                return tcr;
            }
        }
       
       
       
        
        
        
      
        
        public static TextCommandResult cGlobalBank(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if (args.RawArgs[0].Equals("info", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (args.RawArgs[0].Equals("deposit", StringComparison.OrdinalIgnoreCase))
            {
                if(args.RawArgs.Length == 0)
                {
                    return tcr;
                }
                int toAdd = 0;
                try
                {
                    toAdd = int.Parse(args.RawArgs[1]);
                }catch(FormatException e)
                {
                    return tcr;
                }
                if(toAdd < 0)
                {
                    return tcr;
                }

                return tcr;
            }
            else if (args.RawArgs[0].Equals("withdraw", StringComparison.OrdinalIgnoreCase))
            {
                if (args.RawArgs.Length == 0)
                {
                    return tcr;
                }
                int toTake = 0;
                try
                {
                    toTake = int.Parse(args.RawArgs[1]);
                }
                catch (FormatException e)
                {
                    return tcr;
                }
                if (toTake < 0)
                {
                    return tcr;
                }

                return tcr;
            }
            return tcr;
        }

        
       
        public static TextCommandResult chestsInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            //MessageHandler.sendMsgToPlayer(player, claimsEconomyInterface.getAccountInfoAdmin());
            return tcr;
        }
       */
    }

}
