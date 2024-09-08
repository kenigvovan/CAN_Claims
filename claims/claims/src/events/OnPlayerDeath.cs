using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src.events
{
    public class OnPlayerDeath
    {
        public static void Event_OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {

             claims.dataStorage.getPlayerByUid(byPlayer.PlayerUID, out PlayerInfo playerInfo);

            if (damageSource != null && damageSource.SourceEntity != null && damageSource.SourceEntity is EntityPlayer)
            {
                claims.dataStorage.getPlayerByName(damageSource.SourceEntity.GetName(), out PlayerInfo attackerPlayerInfo);
                tryToPrison(damageSource.SourceEntity, byPlayer, playerInfo, attackerPlayerInfo);
            }


            // If player is jailed send them to their jailspawn.
            if (playerInfo != null && playerInfo.isPrisoned())
            {
                var tmp = playerInfo.PrisonedIn.getRandomRespawnPoint();
                byPlayer.SetSpawnPosition(new PlayerSpawnPos(tmp.X, tmp.Y, tmp.Z));
            }
        }
        public static void tryToPrison(Entity attacker, IServerPlayer killed, PlayerInfo playerInfoKilled, PlayerInfo playerInfoAttacker)
        {
            IServerPlayer attackPlayer = null;
            if(attacker is EntityPlayer)
            {
                attackPlayer = (attacker as EntityPlayer).Player as IServerPlayer;
            }
            else if(attacker is EntityProjectile)
            {
                if((attacker as EntityProjectile).FiredBy is EntityPlayer)
                {
                    attackPlayer = ((attacker as EntityProjectile).FiredBy as EntityPlayer) as IServerPlayer;
                }
            }
            if (attackPlayer == null)
                return;
            claims.dataStorage.getPlot(PlotPosition.fromEntityyPos(killed.Entity.ServerPos), out Plot plotKilled); 
            if(plotKilled == null)
            {
                return;
            }
            if(plotKilled.hasCity() && plotKilled.getCity().isCitizen(playerInfoAttacker))
            {
                if (playerInfoAttacker.City.hasPrison())
                    playerInfoKilled.PrisonedIn = playerInfoAttacker.City.getRandomPrison();
            }
        }
    }
}
