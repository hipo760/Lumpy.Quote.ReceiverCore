using System;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Stateless;
using Stateless.Graph;

namespace Lumpy.Quote.ReceiverCore.CryptoExchange
{
    

    public class CryptoExchangeDataFeed:IExchangeDataFeed
    {
        

        public CryptoExchangeDataFeed()
        {
            
            

        }

        public string ExchangeName { get; set; }
        public string ExchangeHost { get; set; }
        public QuoteSubscriptions Symbols { get; set; }
        public ExchangeConnectionState ExchangeConnectStatus { get; set; }
        public bool ContainSymbol(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task Connect()
        {
            throw new NotImplementedException();
        }

        public Task Close()
        {
            throw new NotImplementedException();
        }

        public Task Reconnect(TimeSpan interval)
        {
            throw new NotImplementedException();
        }

        public Task OnSymbolUpdate(QuoteSubscription quoteSub)
        {
            throw new NotImplementedException();
        }
    }
}