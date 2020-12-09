using System;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Serilog;
using Stateless;

namespace Lumpy.Quote.ReceiverCore
{
    public enum QuoteSubscribeTrigger
    {
        Subscribe,
        SubscribeSuccess,
        SubscribeTimeout,
        Unsubscribe,
        UnsubscribeTimeout,
        UnsubscribeSuccess
    }
    public static class QuoteSubscribeHelper
    {
        public static QuoteSubscribeTrigger ToTrigger(this QuoteSubscribeCommand command)
        {
            return command switch
            {
                QuoteSubscribeCommand.Subscribe => QuoteSubscribeTrigger.Subscribe,
                QuoteSubscribeCommand.Unsubscribe => QuoteSubscribeTrigger.Unsubscribe,
                _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
            };
        }
    }

    public class QuoteScriptionStateMachine
    {
        private readonly ILogger _log;
        private readonly IQuoteConnectionAction _quoteConnectionAction;
        private static readonly  object TriggerLock = new object();
        protected StateMachine<QuoteSubscribeState, QuoteSubscribeTrigger> StateMachine;
        public QuoteScriptionStateMachine(ILogger log, IQuoteConnectionAction quoteConnectionAction)
        {
            _log = log;
            _quoteConnectionAction = quoteConnectionAction;
            StateMachine = new StateMachine<QuoteSubscribeState, QuoteSubscribeTrigger>(QuoteSubscribeState.Unsubscribed);
            
            ConfigState();
        }

        private void ConfigState()
        {

            _subscribeSymbolTrigger =
                StateMachine.SetTriggerParameters<string>(QuoteSubscribeTrigger.Subscribe);
            _unsubscribeSymbolTrigger =
                StateMachine.SetTriggerParameters<string>(QuoteSubscribeTrigger.Unsubscribe);

            StateMachine.Configure(QuoteSubscribeState.Unsubscribed)
                .Permit(QuoteSubscribeTrigger.Subscribe, QuoteSubscribeState.Subscribing);
            StateMachine.Configure(QuoteSubscribeState.Subscribing)
                .OnEntryFromAsync(_subscribeSymbolTrigger,
                    async symbol => await _quoteConnectionAction.SendSubscribeSymbolRequest(symbol))
                .Permit(QuoteSubscribeTrigger.SubscribeSuccess, QuoteSubscribeState.Subscribed)
                .Permit(QuoteSubscribeTrigger.SubscribeTimeout, QuoteSubscribeState.Unsubscribed);
            StateMachine.Configure(QuoteSubscribeState.Subscribed)
                .Permit(QuoteSubscribeTrigger.Unsubscribe, QuoteSubscribeState.Unsubscribing);
            StateMachine.Configure(QuoteSubscribeState.Unsubscribing)
                .OnEntryFromAsync(_unsubscribeSymbolTrigger,
                    async symbol => await _quoteConnectionAction.SendUnsubscribeSymbolRequest(symbol))
                .Permit(QuoteSubscribeTrigger.UnsubscribeSuccess, QuoteSubscribeState.Unsubscribed)
                .Permit(QuoteSubscribeTrigger.UnsubscribeTimeout, QuoteSubscribeState.Subscribed);
        }

        private StateMachine<QuoteSubscribeState, QuoteSubscribeTrigger>.TriggerWithParameters<string> _subscribeSymbolTrigger;
        private StateMachine<QuoteSubscribeState, QuoteSubscribeTrigger>.TriggerWithParameters<string> _unsubscribeSymbolTrigger;

        public void Subscribe(string symbol)
        {
            lock (TriggerLock)
            {
                if (StateMachine.CanFire(QuoteSubscribeTrigger.Subscribe))
                {
                    StateMachine.Fire(_subscribeSymbolTrigger, symbol);
                }
            }
        }
        public void Unsubscribe(string symbol)
        {
            lock (TriggerLock)
            {
                if (StateMachine.CanFire(QuoteSubscribeTrigger.Unsubscribe))
                {
                    StateMachine.Fire(_unsubscribeSymbolTrigger, symbol);
                }
            }
        }
    }
}