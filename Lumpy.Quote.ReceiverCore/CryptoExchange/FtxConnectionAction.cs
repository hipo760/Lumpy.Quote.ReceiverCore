using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Lumpy.Quote.ReceiverCore.CryptoExchange
{
    public class FtxConnectionAction:ExchangeWsConnectAction
    {
        public FtxConnectionAction(ILogger log, string exchangeHost) : base(log, exchangeHost)
        {
        }

        public override Task Request(string request)
        {
            throw new System.NotImplementedException();
        }

        public override bool CheckConnection()
        {
            var pongSubscribe = DataBroker
                .Where(s => s == "{'type': 'pong'}")
                .Subscribe(s =>
                {

                });

            return base.CheckConnection();
        }
    }
}