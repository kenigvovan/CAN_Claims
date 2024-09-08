﻿using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Config;

namespace claims.src.delayed.invitations
{
    public class Invitation : IGetStatus
    {
        ISender sender;
        IReceiver receiver;
        long timeoutStamp;

        Thread onApproval;
        Thread onDissent;

        public Invitation(ISender sender, IReceiver receiver, long timeoutStamp, Thread onApproval, Thread onDissent)
        {
            this.sender = sender;
            this.receiver = receiver;
            this.timeoutStamp = timeoutStamp;
            this.onApproval = onApproval;
            this.onDissent = onDissent;
        }

        public ISender getSender()
        {
            return sender;
        }
        public IReceiver getReceiver()
        {
            return receiver;
        }
        public long getTimeStamp()
        {
            return timeoutStamp;
        }
        public void accept()
        {
            InvitationHandler.removeInvitationIfExists(sender, receiver);
            Task.Run(() => onApproval.Start());
            //this.onApproval.Start();            
        }
        public void deny()
        {
            InvitationHandler.removeInvitationIfExists(sender, receiver);
            onDissent.Start();
        }
        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> outStrings = new List<string>();
            outStrings.Add(sender.getNameSender() + " --+ \n");
            outStrings.Add(receiver.getNameReceiver() + " +-- \n");
            outStrings.Add(Lang.Get("claims:will_expire_invitation", TimeFunctions.getDateFromEpochSeconds(timeoutStamp)));
            return outStrings;
        }
    }
}
