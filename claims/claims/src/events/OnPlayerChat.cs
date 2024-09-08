using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using System.Text.RegularExpressions;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace claims.src.events
{
    public class OnPlayerChat
    {
        public static void onPlayerChat(IServerPlayer player, int channelId, ref string message, ref string data, BoolRef consumed)
        {

            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            ClaimsChatType chat;
            claims.dataStorage.getPlayerChatDict().TryGetValue(player.PlayerUID, out chat);
            if (chat == ClaimsChatType.LOCAL)
            {
                MessageHandler.sendLocalMsg(player.Entity.ServerPos.XYZ, message);
                consumed.value = true;
                return;
            }

            if (channelId != claims.dataStorage.getModChatGroup().Uid && chat == ClaimsChatType.NONE)
            {
                if (playerInfo.hasCity())
                {
                    //message = "<font color=#FFFFFF><strong>PREFIX</strong></font>" + message;
                    Match somePrefix = Regex.Match(message, "<font(.+)/font>");
                    
                    Match onlyMsg = Regex.Match(message, "> (.+)");
                   // var i = somePrefix.Value;
                    message = (somePrefix.Success 
                            ? somePrefix.Value
                            : "")
                        + StringFunctions.setStringColor("[", ColorsClaims.WHITE)
                           + StringFunctions.setBold(StringFunctions.setStringColor(StringFunctions.replaceUnderscore(playerInfo.City.GetPartName()), claims.config.CITY_COLOR_NAME))
                           + StringFunctions.setStringColor("]", ColorsClaims.WHITE) +
                           playerInfo.getNameForChat() + " " + onlyMsg.Groups[1].Value;
                }
                else
                {

                }
                //consumed.value = true;
                return;
            }

            if (chat == ClaimsChatType.CITY)
            {
                MessageHandler.sendMsgInCity(playerInfo.City, message, false);
                consumed.value = true;
            }      
            
            if(channelId == claims.dataStorage.getModChatGroup()?.Uid)
            {
                consumed.value = true;
                return;
            }
        }
    }
}
