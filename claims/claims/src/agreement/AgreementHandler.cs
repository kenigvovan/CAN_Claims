﻿using claims.src.messages;
using claims.src.part;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace claims.src.agreement
{

	//FOR PLAYER AGREE TO CREATE NEW CITY, VILLAGE, ALLIANCE
    public class AgreementHandler
    {
        public static ConcurrentDictionary<string, Agreement> agreements = new ConcurrentDictionary<string, Agreement>();

        public static void addNewAgreementOrReplace(Agreement agreement)
        {
            if(agreements.TryGetValue(agreement.getPlayerUid(), out _))
            {
                agreements.TryRemove(agreement.getPlayerUid(), out _);
            }
            agreements.TryAdd(agreement.getPlayerUid(), agreement);

			CancellationTokenSource source = new CancellationTokenSource();
			CancellationToken token = source.Token;

			var tokenSource2 = new CancellationTokenSource();
			CancellationToken ct = tokenSource2.Token;
			agreement.setTokenSource(tokenSource2);
			var t = Task.Run(async delegate
			{
				await Task.Delay(claims.config.AGREEMENT_TIMEOUT_SECONDS * 1000, source.Token);
				ct.ThrowIfCancellationRequested();
				string uid = agreement.getPlayerUid();
				if (agreements.TryGetValue(uid, out _))
				{
					agreements.TryRemove(uid, out _);
					MessageHandler.sendMsgToPlayer(claims.sapi.World.PlayerByUid(uid) as IServerPlayer, Lang.Get("claims:agreement_timeout"));
				}
			}, tokenSource2.Token);
		}

		public static bool agreeFor(IServerPlayer player)
        {
			if(agreements.TryRemove(player.PlayerUID, out Agreement agreement))
            {
				agreement.getToken().Cancel();
				claims.sapi.Event.RegisterCallback((dt =>
				{
					agreement.getOnAgree().Start();					
				}), 0);
				return true;
			}
			return false;
        }
    }
}
