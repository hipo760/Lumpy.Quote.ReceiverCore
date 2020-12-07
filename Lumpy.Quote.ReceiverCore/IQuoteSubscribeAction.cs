using System.Reactive.Subjects;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IQuoteSubscribeAction
    {
        void OnQuoteSubscribeRequest(QuoteSubscribeRequest request);
    }
}