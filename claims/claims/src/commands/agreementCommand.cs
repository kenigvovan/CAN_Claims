using claims.src.agreement;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class agreementCommand: BaseCommand
    {
        public static TextCommandResult onCommand(TextCommandCallingArgs args)
        {
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            if (AgreementHandler.agreeFor(args.Caller.Player as IServerPlayer))
            {

            }
            else
            {
                tcr.StatusMessage = "claims:no_agreements_awaits";
                return tcr;
            }
            return tcr;
        }
    }
}
