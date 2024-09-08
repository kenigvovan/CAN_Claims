using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.part.structure
{
    public class CityClaim
    {
        List<Cuboid> cuboids = new List<Cuboid>();

        public CityClaim()
        {

        }

        public List<Cuboid> getCuboids()
        {
            return cuboids;
        }
    }
}
