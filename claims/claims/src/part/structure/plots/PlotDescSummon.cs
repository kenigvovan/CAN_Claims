using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure.plots
{
    public class PlotDescSummon: PlotDesc
    {
        Vec3d summonPoint;
        public PlotDescSummon(Vec3d summonPoint)
        {
            this.summonPoint = summonPoint;
        }

        public PlotDescSummon()
        {
        }
        public Vec3d getSummonCoords()
        {
            return summonPoint;
        }
        public void setSummonCoords(Vec3d vec)
        {
            this.summonPoint = vec;
        }
        public string getSummonPoint()
        {
            return summonPoint.X.ToString() + "#" + summonPoint.Y.ToString() + "#" + summonPoint.Z.ToString();
        }
        public void fromStringPoint(string val)
        {
            string[] strs = val.Split('#');
            summonPoint = new Vec3d(double.Parse(strs[0]), double.Parse(strs[1]), double.Parse(strs[2]));
        }
    }
}
