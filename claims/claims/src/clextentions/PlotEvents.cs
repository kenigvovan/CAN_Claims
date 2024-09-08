using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.events;
using claims.src.part;
using claims.src.part.structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.clextentions
{
    public class PlotEvents
    {
        public static Dictionary<string, long> lastTimePlayerAskedForPlotsAround;
             
        public static void updatedPlotHandlerUnclaimed(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            int chX = tree.GetInt("chX");
            int chZ = tree.GetInt("chZ");
            
            PlotStateHandling.broadcastPlotUnclaimedInZone(chX, chZ);
        }
        public static void updatedPlotHandlerClaimed(string eventName, ref EnumHandling handling, IAttribute data)
        {
            TreeAttribute tree = data as TreeAttribute;
            int chX = tree.GetInt("chX");
            int chZ = tree.GetInt("chZ");
            
            if(claims.dataStorage.getPlot(new PlotPosition(chX, chZ), out var plot))
            {
                PlotStateHandling.broadcastPlotClaimedInZone(plot);
            }           
        }
    }
}
