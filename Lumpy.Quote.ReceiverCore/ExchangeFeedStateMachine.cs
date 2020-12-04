using System;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Serilog;
using Stateless;

namespace Lumpy.Quote.ReceiverCore
{
    public static class ExchangeConnectionHelper
    {
        public static ExchangeConnectTrigger ToTrigger(this ExhangeConnectCommand command)
        {
            return command switch
            {
                ExhangeConnectCommand.Connect => ExchangeConnectTrigger.Connect,
                ExhangeConnectCommand.Disconnect => ExchangeConnectTrigger.Disconnect,
                ExhangeConnectCommand.Reconnect => ExchangeConnectTrigger.Reconnect,
                ExhangeConnectCommand.CheckConnection => ExchangeConnectTrigger.CheckConnection,
                _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
            };
        }
    }
    public enum ExchangeConnectTrigger
    {
        Connect,
        Fail,
        Retry,
        StopRetry,
        ConnectSuccessful,
        OnSymbolEvent,
        UpdateSubscriptions,
        Listen,
        OnExchangeDataExpired,
        CheckConnection,
        ConnectionAlive,
        EmptySubscription,
        DeathConnection,
        Reconnect,
        Disconnect,
        Clear
    }

    public class ExchangeConnectionStateMachine
    {
        public StateMachine<ExchangeConnectionState, ExchangeConnectTrigger> StateMachine { get; }

        private readonly ILogger _log;
        private readonly IQuoteConnectionAction _quoteConnAction;
        private readonly IQuoteSubscribeAction _exSubAction;

        private bool _isReconnecting = false;
        private int _connectRetry = 0;
        private int _maxConnectRetry = 3;

        public ExchangeConnectionStateMachine(ILogger log)
        {
            _log = log;
            StateMachine = new StateMachine<ExchangeConnectionState, ExchangeConnectTrigger>(ExchangeConnectionState.None);
            ConfigState();
        }

        public ExchangeConnectionStateMachine(ILogger log, IQuoteConnectionAction quoteConnAction, IQuoteSubscribeAction exSubAction):this(log)
        {
            _log = log;
            _quoteConnAction = quoteConnAction;
            _exSubAction = exSubAction;
        }

        private void ConfigState()
        {
            StateMachine
                .Configure(ExchangeConnectionState.None)
                .Permit(ExchangeConnectTrigger.Connect, ExchangeConnectionState.Connecting);

            StateMachine
                .Configure(ExchangeConnectionState.Connecting)
                .OnEntryAsync(async () =>
                {
                    _log.Verbose("[Connecting] Entry");
                    await _quoteConnAction.Connect().ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            _log.Verbose("[Connecting] Connected, fire successful trigger.");
                            StateMachine.Fire(ExchangeConnectTrigger.ConnectSuccessful);
                        }
                        else
                        {
                            _log.Verbose("[Connecting] Failed, fire fail trigger.");
                            StateMachine.Fire(ExchangeConnectTrigger.Fail);
                        }
                    });
                })
                .Permit(ExchangeConnectTrigger.Fail, ExchangeConnectionState.ConnectingFailed)
                .Permit(ExchangeConnectTrigger.ConnectSuccessful, ExchangeConnectionState.Connected);

            StateMachine.Configure(ExchangeConnectionState.ConnectingFailed)
                .SubstateOf(ExchangeConnectionState.Connecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[ConnectingFailed] Entry");
                    StateMachine.Fire(
                        _connectRetry < _maxConnectRetry
                        ? ExchangeConnectTrigger.Retry
                        : ExchangeConnectTrigger.StopRetry);
                })
                .OnExit(() =>
                {
                    _log.Verbose("[ConnectingFailed] Exit");
                    _connectRetry += 1;
                    _log.Verbose("[ConnectingFailed] Retry {time} times.",_connectRetry);
                })
                .Permit(ExchangeConnectTrigger.Retry, ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.StopRetry, ExchangeConnectionState.None);

            StateMachine
                .Configure(ExchangeConnectionState.Connected)
                .OnEntry(() =>
                {
                    _log.Verbose("[Connected] Entry");
                    if (_exSubAction.RegisteredCount <= 0) return;
                    _exSubAction.RestoreSubscriptions();
                    StateMachine.Fire(ExchangeConnectTrigger.OnSymbolEvent);
                })
                .Permit(ExchangeConnectTrigger.Disconnect, ExchangeConnectionState.Disconnecting)
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting)
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking)
                .Permit(ExchangeConnectTrigger.OnSymbolEvent, ExchangeConnectionState.SubscriptionChecking);


            StateMachine
                .Configure(ExchangeConnectionState.SubscriptionChecking)
                .SubstateOf(ExchangeConnectionState.Connected)
                .OnEntry(() =>
                {
                    _exSubAction.CheckSubscriptions();
                    if (_exSubAction.RegisteredCount <= 0)
                    {
                        StateMachine.Fire(ExchangeConnectTrigger.EmptySubscription);
                        return;
                    }
                    StateMachine.Fire(ExchangeConnectTrigger.UpdateSubscriptions);
                })
                .Permit(ExchangeConnectTrigger.EmptySubscription, ExchangeConnectionState.Connected)
                .Permit(ExchangeConnectTrigger.UpdateSubscriptions, ExchangeConnectionState.SubscriptionUpdated);


            StateMachine
                .Configure(ExchangeConnectionState.SubscriptionUpdated)
                .SubstateOf(ExchangeConnectionState.SubscriptionChecking)
                .OnExit(() =>
                {
                    _exSubAction.ResetAllTimer();
                    StateMachine.Fire(ExchangeConnectTrigger.Listen);
                })
                .Permit(ExchangeConnectTrigger.Listen, ExchangeConnectionState.Receving);

            StateMachine
                .Configure(ExchangeConnectionState.Receving)
                .SubstateOf(ExchangeConnectionState.SubscriptionUpdated)
                .Permit(ExchangeConnectTrigger.OnExchangeDataExpired, ExchangeConnectionState.Timeout);

            StateMachine
                .Configure(ExchangeConnectionState.Timeout)
                .SubstateOf(ExchangeConnectionState.Receving)
                .OnEntry(()=>{StateMachine.Fire(ExchangeConnectTrigger.CheckConnection);})
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking);

            StateMachine
                .Configure(ExchangeConnectionState.ConnectionChecking)
                .OnEntry(() =>
                {
                    _log.Verbose("[ConnectionChecking] Entry");
                })
                .SubstateOf(ExchangeConnectionState.Timeout)
                .Permit(ExchangeConnectTrigger.DeathConnection, ExchangeConnectionState.ConnectionLost)
                .Permit(ExchangeConnectTrigger.ConnectionAlive, ExchangeConnectionState.SubscriptionChecking);

            StateMachine
                .Configure(ExchangeConnectionState.ConnectionLost)
                .OnEntry(() =>
                {
                    StateMachine.Fire(ExchangeConnectTrigger.Reconnect);
                })
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting);

            StateMachine
                .Configure(ExchangeConnectionState.Reconnecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[Reconnecting] Entry");
                    StateMachine.Fire(ExchangeConnectTrigger.Disconnect);
                })
                .OnExit(() => { _isReconnecting = false;})
               .Permit(ExchangeConnectTrigger.Disconnect, ExchangeConnectionState.Disconnecting);

            StateMachine
                .Configure(ExchangeConnectionState.Disconnecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[Disconnecting] Entry");
                    _log.Verbose("[Disconnecting] Clear all connection. Fire trigger");
                    StateMachine.Fire(ExchangeConnectTrigger.Clear);
                })
                .Permit(ExchangeConnectTrigger.Clear, ExchangeConnectionState.Disconnected);

            StateMachine
                .Configure(ExchangeConnectionState.Disconnected)
                .OnEntry( () =>
                {
                    _log.Verbose("[Disconnected] Entry");
                    if (!_isReconnecting) return;
                    _log.Verbose("[Disconnected] In reconnect state, Fire connect trigger.");
                    StateMachine.Fire(ExchangeConnectTrigger.Connect);
                })
                .OnExit(()=>_isReconnecting=false)
                .Permit(ExchangeConnectTrigger.Connect,ExchangeConnectionState.Connected);
        }
    }
}