using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Threading.Tasks;
using Htf.Schemas.V1.Fbs.Market;
using Htf.Schemas.V1.Service.Quote;
using Lumpy.Lib.Common.Broker;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IExchangeDataFeed
    {
        public string Name { get; set; }
        public QuoteSubscriptions Symbols { get; set; }
        public ExchangeConnectStatus ExchangeConnectStatus { get; set; }
        public bool ContainSymbol(string symbol);
        public Task Connect();  
        public Task Close();
        public Task Reconnect(TimeSpan interval);
        public Task OnSymbolUpdate(QuoteSubscription quoteSub);
        
    }
}