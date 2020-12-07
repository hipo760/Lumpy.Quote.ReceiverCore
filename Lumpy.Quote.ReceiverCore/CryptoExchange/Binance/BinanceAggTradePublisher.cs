using System;

namespace Lumpy.Quote.ReceiverCore
{
    public class BinanceAggTradePublisher:ITickPublisher
    {
        private IQuoteConnectionAction _quoteConnectionAction;

        public BinanceAggTradePublisher(IQuoteConnectionAction quoteConnectionAction)
        {
            _quoteConnectionAction = quoteConnectionAction;
        }

        public void OnNewTick(string tick)
        {
            throw new NotImplementedException();
        }
    }
}