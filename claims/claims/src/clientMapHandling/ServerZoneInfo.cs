using claims.src.part.structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.clientMapHandling
{
    public class ServerZoneInfo
    {
        public long timestamp;
        public HashSet<Plot> zonePlots = new HashSet<Plot>();
    }
}
