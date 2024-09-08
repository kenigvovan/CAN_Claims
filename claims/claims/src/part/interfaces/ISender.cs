using claims.src.delayed.invitations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.part.interfaces
{
    public interface ISender
    {
        List<Invitation> getSentInvitations();
        void deleteSentInvitation(Invitation invitation);
        void addSentInvitation(Invitation invitation);
        int getMaxSentInvitations();
        string getNameSender();
    }
}
