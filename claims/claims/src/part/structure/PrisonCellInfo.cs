using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure
{
    public class PrisonCellInfo
    {
        List<PlayerInfo> prisonedPlayers = new List<PlayerInfo>();
        Vec3i spawnPostion;
        public PrisonCellInfo(Vec3i pos)
        {
            spawnPostion = pos;
        }
        public PrisonCellInfo()
        {

        }

        public List<PlayerInfo> getPlayerInfos()
        {
            return prisonedPlayers;
        }
        public Vec3i getSpawnPosition()
        {
            return spawnPostion;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(spawnPostion.X.ToString()).Append(",").Append(spawnPostion.Y.ToString()).Append(",").Append(spawnPostion.Z.ToString());
            sb.Append(":");
            foreach(PlayerInfo playerInfo in prisonedPlayers)
            {
                sb.Append(playerInfo.Guid);
                if(!playerInfo.Equals(prisonedPlayers.Last()))
                {
                    sb.Append(',');
                }
            }
            return sb.ToString();
        }
        public void fromString(string input)
        {
            //coords:list of uid
            string [] splited =  input.Split(':');

            string [] spawn = splited[0].Split(',');
            spawnPostion = new Vec3i(int.Parse(spawn[0]), int.Parse(spawn[1]), int.Parse(spawn[2]));
            if(splited[1].Length == 0)
            {
                return;
            }

            string [] uids = splited[1].Split(',');
            foreach (string uid in uids) 
            {
                if (uid.Length == 0)
                    continue;
                if(claims.dataStorage.getPlayerByUid(uid, out PlayerInfo player))
                    prisonedPlayers.Add(player);
            }
        }
    }
}
