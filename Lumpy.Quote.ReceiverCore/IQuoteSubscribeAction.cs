using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Serilog;

namespace Lumpy.Quote.ReceiverCore
{
    public interface IQuoteSubscribeAction
    {
        void OnQuoteSubscribeRequest(QuoteSubscribeRequest request);
    }
    public class QuoteSubscribeAction:IQuoteSubscribeAction
    {
        private ILogger _log;
        private Dictionary<string, HashSet<int>> _symbolSubsciptionDict;

        public QuoteSubscribeAction(ILogger log)
        {
            _log = log;
            _symbolSubsciptionDict = new Dictionary<string, HashSet<int>>();
        }


        public void OnQuoteSubscribeRequest(QuoteSubscribeRequest request)
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

        private void OnSubscribeRequest(QuoteSubscribeRequest request)
        {
            if (_symbolSubsciptionDict.ContainsKey(request.Symbol))
            {
                throw new NotImplementedException();
            }
        }

        private void OnUnsubscribeRequest(QuoteSubscribeRequest request)
        {
            throw new NotImplementedException();
        }
    }
}