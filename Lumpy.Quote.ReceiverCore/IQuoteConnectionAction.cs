using System;
using System.Threading.Tasks;
using Lumpy.Lib.Common.Connection.Exchange;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IQuoteConnectionAction:IExchangeConnectionAction
    {
        Task SendSymbolRequest(string request);
        IObservable<string> QuoteDataBroker { get; }
    }
}