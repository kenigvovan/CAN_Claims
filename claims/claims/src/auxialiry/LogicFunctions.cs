using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace claims.src.auxialiry
{
    public static class LogicFunctions
    {
        //check if input string == 'on', 'off' or something else and return true, false, null in the same order of variants
        public static bool? IsOnOffNone(string val)
        {
            if(val.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if(val.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return null;
        }
    }
}
