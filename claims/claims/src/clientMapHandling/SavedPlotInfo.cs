﻿using claims.src.auxialiry.innerclaims;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.clientMapHandling
{
    [ProtoContract]
    public class SavedPlotInfo
    {
        //if not for sale then it is -1
        [ProtoMember(1)]
        public int price;
        [ProtoMember(2)]
        public bool PvPIsOn;
        [ProtoMember(3)]
        public bool buildFlag;
        [ProtoMember(4)]
        public bool useFlag;
        [ProtoMember(5)]
        public bool attackAnimalsFlag;
        [ProtoMember(6)]
        public string cityName;
        [ProtoMember(7)]
        public string plotName;
        [ProtoMember(8)]
        public string groupName;
        [ProtoMember(9)]
        public List<ClientInnerClaim> clientInnerClaims;

        public SavedPlotInfo()
        {

        }
        public SavedPlotInfo(int price, bool pvPIsOn, bool buildFlag, bool useFlag, bool attackAnimalsFlag,
            string cityName, string plotName, string groupName, List<ClientInnerClaim> clientInnerClaims)
        {
            this.price = price;
            this.PvPIsOn = pvPIsOn;
            this.buildFlag = buildFlag;
            this.useFlag = useFlag;
            this.attackAnimalsFlag = attackAnimalsFlag;
            this.cityName = cityName;
            this.plotName = plotName;
            this.groupName = groupName;
            this.clientInnerClaims = clientInnerClaims;
        }
    }
}
