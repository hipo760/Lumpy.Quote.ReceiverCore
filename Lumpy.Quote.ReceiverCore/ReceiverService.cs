using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Lumpy.Lib.Common.Broker;
using Serilog;

namespace Lumpy.Quote.ReceiverCore
{
    public class ReceiverService
    {
        private ILogger _log;
        private Dictionary<string, IExchangeDataFeed> _exchangeReceiverDict;
        public DataEventBroker<QuoteSubscription> QuoteSubscribeEvent { get; set; }

        public ReceiverService(ILogger log, IEnumerable<IExchangeDataFeed> exchangeList)
        {
            _log = log;
            InitExchange(exchangeList);

        }
        public Task AddExchangeSymbols(QuoteSubscription quoteSub)
        {
            return Task.Run(() =>
            {
                if (!_exchangeReceiverDict.ContainsKey(quoteSub.Exchange)) return;
                QuoteSubscribeEvent.Publish(quoteSub);
            });
        }

        private void InitExchange(IEnumerable<IExchangeDataFeed> exchangeList)
        {
            _exchangeReceiverDict = 
                exchangeList
                .GroupBy(e => e)
                .Select(x => x.FirstOrDefault())
                .Where(x=>x!=null)
                .ToDictionary(e=>e.Name,e=>e);

            foreach (var exFeed in _exchangeReceiverDict)
            {
                QuoteSubscribeEvent.Subscribe(o => exFeed.Value.OnSymbolUpdate(o));
            }
        }


    }
}