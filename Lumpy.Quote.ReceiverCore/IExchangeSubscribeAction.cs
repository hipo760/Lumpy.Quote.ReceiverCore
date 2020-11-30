using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;

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
}