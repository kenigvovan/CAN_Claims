﻿using claims.src.messages;
using claims.src.part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class ClaimsChatCommands: BaseCommand
    {
        public static TextCommandResult onCommandCC(TextCommandCallingArgs args)
        {
            if (claims.dataStorage.hasCityByPlayerGUID(args.Caller.Player.PlayerUID))
            {
                claims.dataStorage.getPlayerChatDict()[args.Caller.Player.PlayerUID] = ClaimsChatType.CITY;
                return TextCommandResult.Success("City channel.");
            }
            return TextCommandResult.Error("");
        }
        public static TextCommandResult onCommandLC(TextCommandCallingArgs args)
        {
            claims.dataStorage.getPlayerChatDict()[args.Caller.Player.PlayerUID] = ClaimsChatType.LOCAL;
            return TextCommandResult.Success("Local channel.");
        }

        public static TextCommandResult onCommandGC(TextCommandCallingArgs args)
        {
            claims.dataStorage.getPlayerChatDict()[args.Caller.Player.PlayerUID] = ClaimsChatType.NONE;
            return TextCommandResult.Success("Global channel.");
        }
    }
}
