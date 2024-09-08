using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace claims.src.agreement
{
    public class Agreement
    {
        Thread onAgree;
        CancellationTokenSource source;
        string playerUID;

        public Agreement(Thread onAgree, string playerUID)
        {
            this.onAgree = onAgree;
            this.playerUID = playerUID;
        }
        public string getPlayerUid()
        {
            return playerUID;
        }
        public Thread getOnAgree()
        {
            return onAgree;
        }
        public CancellationTokenSource getToken()
        {
            return source;
        }
        public void setTokenSource(CancellationTokenSource token)
        {
            this.source = token;
        }
    }
}
