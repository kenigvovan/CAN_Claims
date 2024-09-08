using claims.src.auxialiry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.cityplotsgroups
{
    public class CityPlotsGroupInvitationsHandler
    {
        public static HashSet<CityPlotsGroupInvitation> cityPlotsGroupInvitations = new HashSet<CityPlotsGroupInvitation>();

        public static bool addNewCityPlotGroupInvitation(CityPlotsGroupInvitation invitation)
        {
            if (cityPlotsGroupInvitations.Add(invitation))
            {
                invitation.Receiver.groupInvitations.Add(invitation);
                invitation.Sender.groupInvitations.Add(invitation);
                return true;
            }
            return false;
        }
        public static HashSet<CityPlotsGroupInvitation> GetCityPlotsGroupInvitations()
        {
            return cityPlotsGroupInvitations;
        }
        public static void updateCityPlotsGroupInvitations()
        {
            long now = TimeFunctions.getEpochSeconds();
            foreach(CityPlotsGroupInvitation invitation in cityPlotsGroupInvitations.ToArray())
            {
                if(invitation.TimeStampFinished < now)
                {
                    invitation.Sender.groupInvitations.Remove(invitation);
                    invitation.Receiver.groupInvitations.Remove(invitation);
                    cityPlotsGroupInvitations.Remove(invitation);
                }
            }
        }
    }
}
