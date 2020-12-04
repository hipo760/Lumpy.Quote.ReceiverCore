using System.Threading.Tasks;
using Lumpy.Lib.Common.Connection.Exchange;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Lumpy.Quote.ReceiverCore.CryptoExchange
{
    public class BinanceQuoteAction: IQuoteConnectionAction
    {
        public BinanceConnectionAction(ILogger log, string exchangeUrl) : base(log, exchangeUrl){}
        
        public override Task Request(string symbol) =>
            Task.Run(() =>
            {
                var request = new JObject()
                {
                    {"method", "SUBSCRIBE"},
                    {"params", new JArray {$"{symbol}@aggTrade"}},
                    {"id", 101}
                };
                var json = request.ToString(Formatting.None);
                Log.Verbose("Send Request: {req}", json);
                RxWsClient.RequestBroker.OnNext(json);
            });
    }
}