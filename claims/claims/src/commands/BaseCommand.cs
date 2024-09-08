using claims.src.part.structure;
using claims.src.part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;
using claims.src.rights;
using Vintagestory.API.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace claims.src.commands
{
    public class BaseCommand
    {      
        public static bool getBoolFromString(string str)
        {
            if (str.Equals("on"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool isOwnerOfPlotMayorAdmin(Plot plot, PlayerInfo playerInfo, IServerPlayer player)
        {
            if (player.Role.Code.Equals("admin"))
            {
                return true;
            }
            if (plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo))
            {
                return true;
            }
            else if (playerInfo.hasCity() && plot.hasCity() && playerInfo.City.Equals(plot.getCity()) && plot.getCity().isMayor(playerInfo))
            {
                return true;
            }
            return false;
        }
        public static bool CheckForPlayerPermissions(IServerPlayer player, EnumPlayerPermissions[] anyPermission)
        {
            if (claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                foreach (var permission in anyPermission)
                {
                    if (playerInfo.PlayerPermissionsHandler.HasPermission(permission))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static TextCommandResult SuccessWithParams(string msg, object[] msgParams)
        {
            return new TextCommandResult
            {
                Status = EnumCommandStatus.Success,
                MessageParams = msgParams,
                StatusMessage = msg
            };
        }
    }
}
