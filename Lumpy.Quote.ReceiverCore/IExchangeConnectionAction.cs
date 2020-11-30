using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Lumpy.Lib.Common.Broker;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IExchangeSubscribeAction
    {
        public string ExchangeName { get; set; }
        public string ExchangeHost { get; set; }
        public Task Subscribe(string symbol);
        public Task Unsubscribe(string symbol);
        public QuoteSubscriptions Symbols { get; set; }
        public bool ContainSymbol(string symbol);
    }
    public interface IExchangeConnectionAction
    {
        public Task Connect();
        public Task Disconnect();
        public Task Reconnect();
        public Task CheckConnection();
    }
}