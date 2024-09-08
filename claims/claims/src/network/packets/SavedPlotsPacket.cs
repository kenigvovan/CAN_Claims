using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.network.packets
{
    [ProtoContract]
    public class SavedPlotsPacket
    {
        [ProtoMember(1)]
        public PacketsContentEnum type;
        [ProtoMember(2)]
        public string data;
    }
}
