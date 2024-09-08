﻿using claims.src.auxialiry;
using claims.src.delayed;
using claims.src.messages;
using claims.src.part;
using claims.src.part.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class AcceptCommand: BaseCommand
    {
        //FOR PLAYERS INVITATIONS
        public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            claims.dataStorage.getPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if(playerInfo == null)
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
            foreach(var invitation in playerInfo.getReceivedInvitations())
            {
                if((invitation.getSender() as City).GetPartName().Equals(args[0]))
                {
                    invitation.accept();
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }     
        public static TextCommandResult onAcceptPlotGroup(TextCommandCallingArgs args)
        {
            //FROM ONE CITY PLAYER CAN GET ONLY ONE INVITATION TO GROUP AT THE TIME
            claims.dataStorage.getPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("");
            }
            if (args.LastArg == null)
            {
                MessageHandler.sendMsgToPlayer(args.Caller.Player as IServerPlayer, StringFunctions.makeFeasibleStringFromNames(
                        StringFunctions.getNamesOfCitiesFromInvitations(Lang.Get("claims:you_have_plotsgroup_invites"), playerInfo.groupInvitations),
                    ','));
                return TextCommandResult.Success();
            }
            
            foreach (var invitation in playerInfo.groupInvitations)
            {
                if (invitation.Sender.GetPartName().Equals(args[0]))
                {
                    invitation.Receiver.groupInvitations.Remove(invitation);
                    invitation.Sender.groupInvitations.Remove(invitation);
                    invitation.accept();
                    return TextCommandResult.Success();
                }
            }
            return TextCommandResult.Success();
        }
        public static TextCommandResult onLeavePlotGroup(TextCommandCallingArgs args)
        {
            claims.dataStorage.getPlayerByUid(args.Caller.Player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return TextCommandResult.Error("");
            }
            if (args.LastArg == null)
            {
                List<string> names = new List<string>
                {
                    Lang.Get("claims:your_plotsgroups")
                };
                foreach (var it in claims.dataStorage.getCityPlotsGroupsDict().Values)
                {
                    if(it.PlayersList.Contains(playerInfo))
                    {
                        names.Add(it.City.GetPartName() + ":" + it.GetPartName());
                    }
                }
                return TextCommandResult.Success(StringFunctions.makeFeasibleStringFromNames(names, ','));
            }
            if(!((string)args.LastArg).Contains(":"))
            {
                return TextCommandResult.Success("claims:wrong_format");
            }
            string[] splitted = ((string)args.LastArg).Split(':');
            string cityName = Filter.filterName(splitted[0]);
            if (cityName.Length == 0 || !Filter.checkForBlockedNames(cityName))
            {
                return TextCommandResult.Success("claims:no_city_found");
            }

            claims.dataStorage.getCityByName(cityName, out City targetCity);

            if(targetCity == null)
            {
                return TextCommandResult.Success("claims:no_city_found");
            }
            string groupName = Filter.filterName(splitted[1]);
            if (groupName.Length == 0 || !Filter.checkForBlockedNames(groupName))
            {
                return TextCommandResult.Success("claims:invlaid_group_name");
            }
            CityPlotsGroup searchedGroup = null;
            foreach (CityPlotsGroup group in targetCity.getCityPlotsGroups())
            {
                if (group.GetPartName().Equals(groupName))
                {
                    searchedGroup = group;
                    break;
                }
            }

            if (searchedGroup == null)
            {
                return TextCommandResult.Success("claims:no_such_group");
            }

            if(!searchedGroup.PlayersList.Contains(playerInfo))
            {
                return TextCommandResult.Success("claims:you_are_not_in_the_group");
            }
            searchedGroup.PlayersList.Remove(playerInfo);
            searchedGroup.saveToDatabase();
            return SuccessWithParams("claims:you_left_plotsgroup", new object[] { searchedGroup.GetPartName() });
        }
    }
}
