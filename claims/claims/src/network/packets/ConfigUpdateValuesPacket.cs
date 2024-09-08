using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace claims.src.network.packets
{
    [ProtoContract]
    public class ConfigUpdateValuesPacket
    {
        [ProtoMember(1)]
        public double NewCityCost;
        [ProtoMember(2)]
        public double NewPlotClaimCost;
        [ProtoMember(3)]
        public OrderedDictionary<double, string> COINS_VALUES_TO_CODE;
        [ProtoMember(4)]
        public OrderedDictionary<int, double> ID_TO_COINS_VALUES;
        [ProtoMember(5)]
        public double CITY_NAME_CHANGE_COST;
        [ProtoMember(6)]
        public double CITY_BASE_CARE;
        [ProtoMember(7)]
        public int[] PLOTS_COLORS;
    }
}
