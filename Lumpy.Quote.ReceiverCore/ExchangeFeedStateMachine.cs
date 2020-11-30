using System;
using System.Threading.Tasks;
using Htf.Schemas.V1.Grpc.Service.Quote;
using Serilog;
using Stateless;

namespace Lumpy.Quote.ReceiverCore
{
    public static class ExchangeConnectionHelper
    {
        public static ExchangeConnectTrigger ToTrigger(this ExhangeConnectCommand command)
        {
            switch (command)
            {
                case ExhangeConnectCommand.Connect:
                    return ExchangeConnectTrigger.Connect;
                case ExhangeConnectCommand.Disconnect:
                    return ExchangeConnectTrigger.Disconnect;
                case ExhangeConnectCommand.Reconnect:
                    return ExchangeConnectTrigger.Reconnect;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }
    }
    public enum ExchangeConnectTrigger
    {
        Connect,
        Fail,
        Retry,
        StopRetry,
        ConnectTaskDone,
        RestoreSubscriptions,
        Listen,
        OnExchangeDataExpired,
        CheckConnection,
        ConnectionAlive,
        ResetTimer,
        EmptySubscription,
        DeathConnection,
        Reconnect,
        Disconnect,
        Clear
    }

    public class ExchangeConnectionStateMachine
    {
        public StateMachine<ExchangeConnectionState, ExchangeConnectTrigger> StateMachine { get; }

        private ILogger _log;
        private bool _isReconnecting = false;

        public ExchangeConnectionStateMachine(ILogger log)
        {
            _log = log;
            StateMachine = new StateMachine<ExchangeConnectionState, ExchangeConnectTrigger>(ExchangeConnectionState.Connected);
            ConfigState();
        }
        private void ConfigState()
        {
            StateMachine
                .Configure(ExchangeConnectionState.None)
                .Permit(ExchangeConnectTrigger.Connect, ExchangeConnectionState.Connecting);

            StateMachine
                .Configure(ExchangeConnectionState.Connecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[==ExchangeConnectionState.Connecting==] Entry");
                    Task.Delay(TimeSpan.FromSeconds(2)).Wait();
                    _log.Verbose("[==ExchangeConnectionState.Connecting==] ConnetTaskDonce, fire trigger.");
                    StateMachine.Fire(ExchangeConnectTrigger.ConnectTaskDone);
                })
                .Permit(ExchangeConnectTrigger.Fail, ExchangeConnectionState.ConnectingFailed)
                .Permit(ExchangeConnectTrigger.ConnectTaskDone, ExchangeConnectionState.Connected);

            StateMachine.Configure(ExchangeConnectionState.ConnectingFailed)
                .SubstateOf(ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.Retry, ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.StopRetry, ExchangeConnectionState.Disconnected);

            StateMachine
                .Configure(ExchangeConnectionState.Connected)
                .Permit(ExchangeConnectTrigger.Disconnect, ExchangeConnectionState.Disconnecting)
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting)
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking)
                .Permit(ExchangeConnectTrigger.RestoreSubscriptions, ExchangeConnectionState.SubscriptionChecking);


            StateMachine
                .Configure(ExchangeConnectionState.SubscriptionChecking)
                .SubstateOf(ExchangeConnectionState.Connected)
                .Permit(ExchangeConnectTrigger.ResetTimer, ExchangeConnectionState.SubscriptionRestored)
                .Permit(ExchangeConnectTrigger.EmptySubscription, ExchangeConnectionState.Connected);

            StateMachine
                .Configure(ExchangeConnectionState.SubscriptionRestored)
                .SubstateOf(ExchangeConnectionState.SubscriptionChecking)
                .Permit(ExchangeConnectTrigger.Listen, ExchangeConnectionState.Receving);

            StateMachine
                .Configure(ExchangeConnectionState.Receving)
                .SubstateOf(ExchangeConnectionState.SubscriptionRestored)
                .Permit(ExchangeConnectTrigger.OnExchangeDataExpired, ExchangeConnectionState.Timeout);

            StateMachine
                .Configure(ExchangeConnectionState.Timeout)
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking);

            StateMachine
                .Configure(ExchangeConnectionState.ConnectionChecking)
                .Permit(ExchangeConnectTrigger.DeathConnection, ExchangeConnectionState.ConnectionLost)
                .Permit(ExchangeConnectTrigger.ConnectionAlive, ExchangeConnectionState.SubscriptionChecking);

            StateMachine
                .Configure(ExchangeConnectionState.ConnectionLost)
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting);

            StateMachine
                .Configure(ExchangeConnectionState.Reconnecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[==Reconnecting==] Entry");
                    StateMachine.Fire(ExchangeConnectTrigger.Disconnect);
                })
                .OnExit(() => { _isReconnecting = false;})
               .Permit(ExchangeConnectTrigger.Disconnect, ExchangeConnectionState.Disconnecting);

            StateMachine
                .Configure(ExchangeConnectionState.Disconnecting)
                .OnEntry(() =>
                {
                    _log.Verbose("[==Disconnecting==] Entry");
                    _log.Verbose("[==Disconnecting==] Clear all connection. Fire trigger");
                    StateMachine.Fire(ExchangeConnectTrigger.Clear);
                })
                .Permit(ExchangeConnectTrigger.Clear, ExchangeConnectionState.Disconnected);

            StateMachine
                .Configure(ExchangeConnectionState.Disconnected)
                .SubstateOf(ExchangeConnectionState.Disconnecting)
                .OnEntry( () =>
                {
                    _log.Verbose("[==Disconnected==] Entry");
                    if (!StateMachine.IsInState(ExchangeConnectionState.Reconnecting)) return;
                    _log.Verbose("[==Disconnected==] In reconnect state, Fire connect trigger.");
                    StateMachine.Fire(ExchangeConnectTrigger.Connect);
                })
                .Permit(ExchangeConnectTrigger.Connect,ExchangeConnectionState.Connected);
        }
    }
}