using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace claims.src.part
{
    public class Cuboid
    {
        public int X1;
        public int Y1;
        public int Z1;
        public int X2;
        public int Y2;
        public int Z2;

        HashSet<string> addedPlayersUIDS = new HashSet<string>();
        public Dictionary<string, EnumBlockAccessFlags> PermittedPlayerUids = new Dictionary<string, EnumBlockAccessFlags>();
        public Cuboid()
        {
 
        }
    }
}
