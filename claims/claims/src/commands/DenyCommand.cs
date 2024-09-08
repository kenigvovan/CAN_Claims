﻿using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.network.packets;
using claims.src.part;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class DenyCommand: BaseCommand
    {
        public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            claims.dataStorage.getPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("");
            }

            //try to accept the first and only one invite
            if (args.LastArg == null)
            {
                int invitationsCount = playerInfo.getReceivedInvitations().Count;
                if (invitationsCount == 1)
                {
                    playerInfo.getReceivedInvitations()[0].accept();
                }
                return TextCommandResult.Success();
            }

            //cityName arg is not null, we try to accept invite from that city
            foreach (var invitation in playerInfo.getReceivedInvitations())
            {
                if ((invitation.getSender() as City).GetPartName().Equals(args[0]))
                {
                    invitation.deny();
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CITY_INVITE_REMOVE, invitation.getSender().getNameSender() }
                    };


                    claims.serverChannel.SendPacket(
                            new PlayerGuiRelatedInfoPacket()
                            {
                                playerGuiRelatedInfoDictionary = collector
                            }
                            , args.Caller.Player as IServerPlayer);
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }
    }
}
