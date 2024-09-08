﻿using claims.src.auxialiry;
using claims.src.delayed.cooldowns;
using claims.src.part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace claims.src.delayed.teleportation
{
    public class TeleportationHandler
    {
        public static List<TeleportationInfo> teleportationsList = new List<TeleportationInfo>();

        public static bool addTeleportation(TeleportationInfo teleportationInfo)
        {
            foreach (var it in teleportationsList)
            {
                if (it.getTargetPlayer().Guid.Equals(teleportationInfo.getTargetPlayer().Guid))
                {
                    teleportationsList.Remove(it);
                    break;
                }
            }
            teleportationInfo.getTargetPlayer().AwaitForTeleporation = true;
            teleportationsList.Add(teleportationInfo);
            return true;
        }
        public static bool removeTeleportation(PlayerInfo playerInfo)
        {
            foreach (var it in teleportationsList)
            {
                if (it.getTargetPlayer().Guid.Equals(playerInfo.Guid))
                {
                    it.getTargetPlayer().AwaitForTeleporation = false;
                    teleportationsList.Remove(it);
                    return true;
                }
            }
            return false;
        }
        public static bool hasTeleportation(PlayerInfo playerInfo)
        {
            foreach (var it in teleportationsList)
            {
                if (it.getTargetPlayer().Guid.Equals(playerInfo.Guid))
                {
                    return true;
                }
            }
            return false;
        }
        public static void UpdateTeleportations()
        {
            if (teleportationsList == null || teleportationsList.Count == 0)
            {
                return;
            }
            foreach (var it in teleportationsList.ToArray())
            {
                long timeNow = TimeFunctions.getEpochSeconds();
                if (it.getTimeStamp() < timeNow)
                {
                    IServerPlayer player = claims.sapi.World.PlayerByUid(it.getTargetPlayer().Guid) as IServerPlayer;
                    if (player != null)
                    {
                        player.Entity.TeleportToDouble(it.getTargetPoint().X, it.getTargetPoint().Y, it.getTargetPoint().Z);
                    }
                    CooldownHandler.addCooldown(it.getTargetPlayer(), new CooldownInfo(TimeFunctions.getEpochSeconds() + claims.config.SECONDS_SUMMON_COOLDOWN, CooldownType.SUMMON));

                    it.getTargetPlayer().AwaitForTeleporation = false;
                    teleportationsList.Remove(it);
                }
            }
        }
    }
}
