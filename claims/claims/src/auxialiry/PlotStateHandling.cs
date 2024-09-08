﻿using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.auxialiry
{
    public class PlotStateHandling
    {
        //Send all players in plot's zone info about the newly claimed plot
        public static void broadcastPlotClaimedInZone(Plot plot)
        {

            Vec2i zone = plot.plotPosition.getPos().Copy();
            zone.X = zone.X / claims.config.ZONE_PLOTS_LENGTH;
            zone.Y = zone.Y / claims.config.ZONE_PLOTS_LENGTH;
            
            foreach (var player in claims.sapi.World.AllOnlinePlayers)
            {
                if (zone.X != (int)(player.Entity.ServerPos.X / claims.config.ZONE_BLOCKS_LENGTH) 
                    || zone.Y != (int)(player.Entity.ServerPos.Z / claims.config.ZONE_BLOCKS_LENGTH))
                {
                    continue;
                }
                claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    return;
                }
                var tmpPlot = new SavedPlotInfo((int)plot.getPrice(), plot.getPermsHandler().pvpFlag,
                        player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockDestroyWithOutCacheUpdate(playerInfo, plot),
                        player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canBlockUseWithOutCacheUpdate(playerInfo, plot),
                        player.WorldData.CurrentGameMode == EnumGameMode.Creative || OnBlockAction.canAttackAnimalsWithOutCacheUpdate(playerInfo, plot),
                           plot.getCity().GetPartName(), plot.GetPartName(),
                           plot.hasCityPlotsGroup()
                               ? plot.getPlotGroup().GetPartName()
                               : "",
                           plot.getType() == PlotType.TAVERN 
                               ? plot.GetClientInnerClaimFromDefault(playerInfo)
                               : null);
                string serializedPlots = JsonConvert.SerializeObject(new Tuple<Vec2i, SavedPlotInfo>(plot.getPos(), tmpPlot));

                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.ADD_SINGLE_PLOT,
                    data = serializedPlots

                }, (IServerPlayer)player);
            }

        }
        //Send all players in plot's zone info about the unclaimed plot
        public static void broadcastPlotUnclaimedInZone(int x, int z)
        {
            Vec2i zone = new Vec2i(x / claims.config.ZONE_PLOTS_LENGTH, z / claims.config.ZONE_PLOTS_LENGTH);
            foreach (var player in claims.sapi.World.AllOnlinePlayers)
            {
                if (zone.X != (int)(player.Entity.ServerPos.X / claims.config.ZONE_BLOCKS_LENGTH) 
                    || zone.Y != (int)(player.Entity.ServerPos.Z / claims.config.ZONE_BLOCKS_LENGTH))
                {
                    continue;
                }
                claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
                if (playerInfo == null)
                {
                    return;
                }
                var tmpPlot = new SavedPlotInfo(0, false,
                           false,
                           false,
                           false,
                           null, null, null, null);
                string serializedPlots = JsonConvert.SerializeObject(new Tuple<Vec2i, SavedPlotInfo> (new Vec2i(x, z), tmpPlot));

                claims.serverChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.REMOVE_SINGLE_PLOT,
                    data = serializedPlots

                }, (IServerPlayer)player);
            }

        }
    }
}
