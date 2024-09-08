using claims.src.auxialiry.innerclaims;
using claims.src.perms;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.auxialiry.claimAreas
{
    public class ClaimArea
    {
        /*
         { 0, "use"},
         { 1, "build" },
         { 2, "attack" }};*/
        [ProtoMember(1)]
        public Vec3i pos1;
        [ProtoMember(2)]
        public Vec3i pos2;
        [ProtoMember(3)]
        public bool[] permissionsFlags = new bool[4] { false, false, false, false };
        public bool pvpFlag, fireFlag, blastFlag;
        public int MinX => Math.Min(pos1.X, pos2.X);

        public int MinY => Math.Min(pos1.Y, pos2.Y);

        public int MinZ => Math.Min(pos1.Z, pos2.Z);

        public int MaxX => Math.Max(pos1.X, pos2.X);

        public int MaxY => Math.Max(pos1.Y, pos2.Y);

        public int MaxZ => Math.Max(pos1.Z, pos2.Z);
        public bool Intersects(ClaimArea with)
        {
            if (with.MaxX < MinX || with.MinX > MaxX)
            {
                return false;
            }

            if (with.MaxY < MinY || with.MinY > MaxY)
            {
                return false;
            }

            if (with.MaxZ >= MinZ)
            {
                return with.MinZ <= MaxZ;
            }

            return false;
        }
        public bool Contains(Vec3d pos)
        {
            if (pos.X >= (double)MinX && pos.X < (double)MaxX && pos.Y >= (double)MinY && pos.Y < (double)MaxY && pos.Z >= (double)MinZ)
            {
                return pos.Z < (double)MaxZ;
            }

            return false;
        }

        public bool Contains(int x, int y, int z)
        {
            if (x >= MinX && x < MaxX && y >= MinY && y < MaxY && z >= MinZ)
            {
                return z < MaxZ;
            }

            return false;
        }

        public bool Contains(BlockPos pos)
        {
            if (pos.X >= MinX && pos.X <= MaxX && pos.Y >= MinY && pos.Y <= MaxY && pos.Z >= MinZ)
            {
                return pos.Z < MaxZ;
            }

            return false;
        }

        public ClaimArea()
        {

        }
        public ClaimArea(Vec3i pos1, Vec3i pos2, bool[] permissionsFlags)
        {
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.permissionsFlags[0] = permissionsFlags[0];
            this.permissionsFlags[1] = permissionsFlags[1];
            this.permissionsFlags[2] = permissionsFlags[2];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pos1.X).Append(",").Append(pos1.Y).Append(",").Append(pos1.Z).Append(":").Append(pos2.X).Append(",").Append(pos2.Y).Append(",").Append(pos2.Z).Append(":").Append(permissionsFlags[0] ? "1" : "0").Append(",").
                Append(permissionsFlags[1] ? "1" : "0").Append(",").Append(permissionsFlags[2] ? "1" : "0").Append(":");
            return sb.ToString();
        }
    }
}
