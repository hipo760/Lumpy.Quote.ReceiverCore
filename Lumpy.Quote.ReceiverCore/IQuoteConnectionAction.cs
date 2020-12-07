using System;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Lumpy.Lib.Common.Connection.Exchange;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IQuoteConnectionAction:IExchangeConnectionAction
    {
        Task SendSubscribeSymbolRequest(string symbol);
        Task SendUnsubscribeSymbolRequest(string symbol);
        //Task<bool> CheckSymbolSubscription(string symbol);
        IObservable<(string symbol,QuoteSubscribeState state)> SymbolSubscriptionEvent { get; }
        IObservable<string> QuoteDataBroker { get; }
    }
}