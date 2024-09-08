using claims.src.auxialiry;
using claims.src.part.structure.plots;
using claims.src.part.structure;
using claims.src.part;
using claims.src.perms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.gui.playerGui.structures
{
    public class CurrentPlotInfo
    {
        public string PlotName { get; set; }
        public string OwnerName { get; set; }
        public PlotType PlotType { get; set; }       
        public Vec2i PlotPosition {  get; set; }
        public double CustomTax { get; set; } = 0;
        public double Price { get; set; } = -1;
        public PermsHandler PermsHandler {  get; set; }
        public bool ExtraBought { get; set; }

        public CurrentPlotInfo(string plotName, string ownerName, PlotType plotType, double customTax,
            double price, PermsHandler permsHandler, bool extraBoungt, Vec2i plotPosition)
        {
            PlotName = plotName;
            OwnerName = ownerName;
            PlotType = plotType;
            CustomTax = customTax;
            Price = price;
            PlotPosition = plotPosition;
            PermsHandler = permsHandler;
            ExtraBought = extraBoungt;
        }

        public CurrentPlotInfo()
        {
            PermsHandler = new PermsHandler();
            PlotPosition = new Vec2i();
        }
    }
}
