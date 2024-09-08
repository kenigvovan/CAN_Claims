using claims.src.gui.playerGui.structures;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.network.packets
{
    [ProtoContract]
    public class PlayerGuiRelatedInfoPacket
    {
        [ProtoMember(1)]
        public Dictionary<EnumPlayerRelatedInfo, string> playerGuiRelatedInfoDictionary = new Dictionary<EnumPlayerRelatedInfo, string>();
    }
}
