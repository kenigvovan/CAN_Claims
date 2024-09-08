using claims.src.delayed.invitations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.part.interfaces
{
    public interface IReceiver
    {
       List<Invitation> getReceivedInvitations();       
       void deleteReceivedInvitation(Invitation invitation);
       void addReceivedInvitation(Invitation invitation);
       int getMaxReceivedInvitations();
       string getNameReceiver();
    }
}
