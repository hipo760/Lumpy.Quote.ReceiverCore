using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Serilog;

namespace Lumpy.Quote.ReceiverCore.CryptoExchange.Binance
{
    public class BinanceAggTradeSubscribeAction:IQuoteSubscribeAction
    {
        private readonly ILogger _log;
        //private readonly IQuoteConnectionAction _quoteConnectionAction;
         
        public Subject<QuoteSubscribeRequest> QuoteSubScriptionEvent { get; }
        //private readonly Dictionary<string, QuoteSubscribeState> _symbolSubsciptionDict;
        private static readonly object SymbolSubscriptionDictLock = new object();

        private readonly string _innerExchangeName;

        public BinanceAggTradeSubscribeAction(ILogger log, IQuoteConnectionAction quoteConnectionAction,string innerExchangeName)
        {
            _log = log;
            _quoteConnectionAction = quoteConnectionAction;
            _innerExchangeName = innerExchangeName;
            _symbolSubsciptionDict = new Dictionary<string, QuoteSubscribeState>();
            _quoteConnectionAction.SymbolSubscriptionEvent
                .Subscribe(symbolState => OnSymbolSubscriptionEvent(symbolState.symbol, symbolState.state));
        }

        private void OnSymbolSubscriptionEvent(string symbol,QuoteSubscribeState state)
        {
            lock (SymbolSubscriptionDictLock)
            {
                if (!_symbolSubsciptionDict.ContainsKey(symbol)) return;
                _symbolSubsciptionDict[symbol] = state;
            }
        }

        public void OnQuoteSubscribeRequest(QuoteSubscribeRequest request)
        {
            if (request.Exchange != _innerExchangeName) return;
            lock (SymbolSubscriptionDictLock)
            {
                
                
                switch (request.Command)
                {
                    case QuoteSubscribeCommand.Unsubscribe:
                        OnUnsubscribeRequest(request);
                        break;
                    case QuoteSubscribeCommand.Subscribe:
                        OnSubscribeRequest(request);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnSubscribeRequest(QuoteSubscribeRequest request)
        {
            if (!_symbolSubsciptionDict.ContainsKey(request.Symbol))
            {
                _symbolSubsciptionDict.Add(request.Symbol, QuoteSubscribeState.Unsubscribed);
                QuoteSubScriptionEvent.OnNext(request);
            }
            else if (_symbolSubsciptionDict[request.Symbol] != QuoteSubscribeState.Subscribed)
            {
                QuoteSubScriptionEvent.OnNext(request);
            }
        }
        private void OnUnsubscribeRequest(QuoteSubscribeRequest request)
        {
            if (!_symbolSubsciptionDict.ContainsKey(request.Symbol)) return;
            QuoteSubScriptionEvent.OnNext(request);
        }
    }
}