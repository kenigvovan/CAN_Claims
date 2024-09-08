using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure
{
    public class Prison : Part
    {
        List<PrisonCellInfo> prisonCells = new List<PrisonCellInfo>();
        City city;
        Plot plot;
        public Prison(string val, string guid) : base(val, guid)
        {
        }
        public void setPlot(Plot plot)
        {
            this.plot = plot;
        }
        public Vec3i getRandomRespawnPoint()
        {
            return prisonCells[claims.dataStorage.r.Next() % prisonCells.Count].getSpawnPosition();
        }
        public Plot getPlot()
        {
            return plot;
        }
        public void setCity(City city)
        {
            this.city = city;   
        }
        public City getCity()
        {
            return city;
        }
        public void addPrisonCell(PrisonCellInfo prisonCellInfo)
        {
            prisonCells.Add(prisonCellInfo);
        }
        public void removePrisonCell(int val)
        {
            prisonCells.Remove(prisonCells[val]);
        }
        public List<PrisonCellInfo> getPrisonCells()
        {
            return prisonCells; 
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //one cell info/ second cell info
            foreach(var cell in prisonCells)
            {
                sb.Append(cell.ToString());
                if(!cell.Equals(prisonCells.Last()))
                {
                    sb.Append("|");
                }
            }
            return sb.ToString();
        }
        public void fromString(string input)
        {
            string[] cells = input.Split('|');
            foreach(var cell in cells)
            {
                if (cell.Length == 0)
                    continue;
                PrisonCellInfo newCell = new PrisonCellInfo();
                newCell.fromString(cell);
                prisonCells.Add(newCell);
            }
        }
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().savePrison(this, update);
        }
    }
}
