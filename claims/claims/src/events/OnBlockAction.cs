﻿using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client;
using Vintagestory.Common;
using Vintagestory.Server;

namespace claims.src.events
{
    public class OnBlockAction
    {
        public static bool Event_OnBlockUse(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }
            if (!canBlockUse(byPlayer, blockSel))
            {
                return false;
            }
            return true;
        }
        public static bool Event_OnBlockDestroy(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant)
        {
            claimant = "";
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                claimant = "claims";
                return true;
            }
            if (!canBlockDestroy(byPlayer, blockSel, out claimant))
            {               
                return false;
            }
            return true;
        }
        public static bool checkInnerClaimPerm(PermType permType, string uid, Plot plot, BlockSelection blockSel)
        {
            foreach(var it in (plot.getPlotDesc() as PlotDescTavern).innerClaims)
            {
                if(it.Contains(blockSel.Position))
                {
                    return it.permissionsFlags[(int)permType];
                }
            }
            return false;
        }
        public static bool checkInnerClaimPerm(PermType permType, string uid, Plot plot, Vec3d blockSel)
        {
            foreach (var it in (plot.getPlotDesc() as PlotDescTavern).innerClaims)
            {
                if (it.Contains(blockSel))
                {
                    return it.permissionsFlags[(int)permType];
                }
            }
            return false;
        }
        private static bool CheckLandClaimPresence(Vec3d pos)
        {
            long key = claims.sapi.WorldManager.MapRegionIndex2D((int)pos.X / claims.sapi.WorldManager.RegionSize, (int)pos.Z / claims.sapi.WorldManager.RegionSize);
            if (!((ServerMain)claims.sapi.World).WorldMap.LandClaimByRegion.ContainsKey(key))
            {
                return false;
            }

            foreach (LandClaim item in ((ServerMain)claims.sapi.World).WorldMap.LandClaimByRegion[key])
            {
                if (item.PositionInside(pos))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool canBlockDestroy(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant)
        {
            claims.dataStorage.getPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            claimant = "";
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromBlockPos(blockSel.Position), out Plot plot);
            if (plot == null)
            {
                /*if (claims.config.NO_ACCESS_WITH_FOR_NOT_CLAIMED_AREA)
                {
                    return CheckLandClaimPresence(blockSel.Position.ToVec3d());
                }*/
                return true;
            }
            claimant = "claims";
            PlotPosition currentPosPlayer = new PlotPosition(blockSel.Position.X, blockSel.Position.Z);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                //todo
                //MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + currentPosPlayer.getPos().ToString() + " equals " + playerInfo.PlayerCache.getLastLocation().getPos().ToString());
                if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].HasValue)
                {
                    if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].Value)
                    {
                        //todo
                        //MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + playerInfo.PlayerCache.getLastLocation().getPos().ToString() + " value is true");
                        return true;
                    }
                    else
                    {
                        if (plot.getType() == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                        else
                            return false;
                    }
                }
            }
            //todo
           // MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + currentPosPlayer.getPos().ToString() + " set as currentposplayer ");
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM);
                    //todo
                   // MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + " for stranger in plot " + plot.getPos().ToString() + " value is " + b.ToString());
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
            }
            return false;
        }        
        public static bool canBlockUse(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            claims.dataStorage.getPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromBlockPos(blockSel.Position), out Plot plot);
            if(plot == null)
            {
                /*if (claims.config.NO_ACCESS_WITH_FOR_NOT_CLAIMED_AREA)
                {
                    return CheckLandClaimPresence(blockSel.Position.ToVec3d());
                }*/
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ(blockSel.Position.X, blockSel.Position.Z);
            if(currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if(playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                {
                    if(playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value)
                    {
                        return true;
                    }
                    else
                    {
                        if (plot.getType() == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                        else
                            return false;
                    }
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {
                
                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                    }
                    return b;
            }
            return false;   

        }
        public static bool canBlockUse(IServerPlayer byPlayer, Vec3d vec3)
        {
            claims.dataStorage.getPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromXZ((int)vec3.X, (int)vec3.Z), out Plot plot);
            if (plot == null)
            {
                /*if (claims.config.NO_ACCESS_WITH_FOR_NOT_CLAIMED_AREA)
                {
                    return CheckLandClaimPresence(vec3);
                }*/
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ((int)vec3.X, (int)vec3.Z);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                {
                    return playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value;
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    return b;
            }
            return false;

        }       
        public static bool canAttackAnimals(IServerPlayer byPlayer, Vec3d pos)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }
            claims.dataStorage.getPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return false;
            }
            claims.dataStorage.getClaimedPlots().TryGetValue(PlotPosition.fromXZ((int)pos.X, (int)pos.Z), out Plot plot);
            if (plot == null)
            {
                /*if (claims.config.NO_ACCESS_WITH_FOR_NOT_CLAIMED_AREA)
                {
                    return CheckLandClaimPresence(pos);
                }*/
                return true;
            }
            PlotPosition currentPosPlayer = PlotPosition.fromXZ((int)pos.X, (int)pos.Z);
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].HasValue)
                {
                    if(playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].Value)
                    {
                        return true;
                    }
                    else
                    {
                        if (plot.getType() == PlotType.TAVERN)
                            return checkInnerClaimPerm(PermType.ATTACK_ANIMALS_PERM, playerInfo.Guid, plot, pos);
                        else
                            return false;
                    }
                }
            }
            playerInfo.PlayerCache.setPlotPosition(currentPosPlayer);
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }
                    return b;
                case PlotRelation.COMRADE    :
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }
                    return b;
            }
            return false;
        }
        public static PlotRelation getPlotRelationForPlayerInfo(PlayerInfo playerInfo, PlotPosition pos, Plot plot)
        {            
            if((plot.hasCity() && plot.getCity().isMayor(playerInfo)))
            {
                return PlotRelation.MANAGABLE_OWNER;
            }

            if (plot.hasPlotGroup() && plot.getPlotGroup().PlayersList.Contains(playerInfo))
            {
                return PlotRelation.GROUP_MEMBER;
            }

            if (plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo))
            {
                return PlotRelation.PLOT_OWNER;
            }
            if(plot.hasCity() && plot.getCity().getCityCitizens().Contains(playerInfo))
            {
                return PlotRelation.CITIZEN;
            }
            if(plot.hasPlotOwner() && plot.getPlotOwner().Friends.Contains(playerInfo))
            {
                return PlotRelation.COMRADE;
            }
            return PlotRelation.STRANGER;
        }
        public static bool canBlockDestroyWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            PlotPosition currentPosPlayer = plot.plotPosition;
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].HasValue)
                {
                    return playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM].Value;
                }
            }
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    /*if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }*/
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.BUILD_AND_DESTROY_PERM);
                    //todo
                    // MessageHandler.sendDebugMsg(byPlayer.PlayerName + " " + " for stranger in plot " + plot.getPos().ToString() + " value is " + b.ToString());
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                     }*/
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                     }*/
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.BUILD_AND_DESTROY_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.BUILD_AND_DESTROY_PERM] = b;
                    /*if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.BUILD_AND_DESTROY_PERM, playerInfo.Guid, plot, blockSel);
                    }*/
                    return b;
            }
            return false;
        }
        public static bool canBlockUseWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            PlotPosition currentPosPlayer = plot.plotPosition;
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].HasValue)
                {
                    return playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM].Value;             
                }
            }          
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                     }*/
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    /*if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                    }*/
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                     }*/
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.USE_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.USE_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, blockSel);
                     }*/
                    return b;
            }
            return false;

        }
        public static bool canAttackAnimalsWithOutCacheUpdate(PlayerInfo playerInfo, Plot plot)
        {
            PlotPosition currentPosPlayer = plot.plotPosition;
            if (currentPosPlayer.Equals(playerInfo.PlayerCache.getLastLocation()))
            {
                if (playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].HasValue)
                {
                    return playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM].Value;                   
                }
            }
            bool b;
            switch (getPlotRelationForPlayerInfo(playerInfo, currentPosPlayer, plot))
            {

                case PlotRelation.PLOT_OWNER:
                case PlotRelation.MANAGABLE_OWNER:
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = true;
                    return true;
                case PlotRelation.CITIZEN:
                    b = plot.getPermsHandler().getPerm(PermGroup.CITIZEN, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    /*if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }*/
                    return b;
                case PlotRelation.STRANGER:
                    b = plot.getPermsHandler().getPerm(PermGroup.STRANGER, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                     }*/
                    return b;
                case PlotRelation.GROUP_MEMBER:
                    b = plot.getPlotGroup().PermsHandler.getPerm(PermGroup.CITIZEN, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    /*if (!b && plot.getType() == PlotType.TAVERN)
                    {
                        return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                    }*/
                    return b;
                case PlotRelation.COMRADE:
                    b = plot.getPermsHandler().getPerm(PermGroup.COMRADE, PermType.ATTACK_ANIMALS_PERM);
                    playerInfo.PlayerCache.getCache()[(int)PermType.ATTACK_ANIMALS_PERM] = b;
                    /* if (!b && plot.getType() == PlotType.TAVERN)
                     {
                         return checkInnerClaimPerm(PermType.USE_PERM, playerInfo.Guid, plot, pos);
                     }*/
                    return b;
            }
            return false;
        }
    }
}
