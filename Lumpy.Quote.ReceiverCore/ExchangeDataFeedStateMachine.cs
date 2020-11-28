using System;
using Htf.Schemas.V1.Grpc.Service.Quote;
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
        ResetTimeer,
        EmptySubscription,
        DeathConnection,
        Reconnect,
        Disconnect,
        Clear,
        IsClose,
        Close
    }

    public class ExchangeDataFeedStateMachine
    {
        private StateMachine<ExchangeConnectionState, ExchangeConnectTrigger> _state;

        private IExchangeDataFeed _exchangeDataFeed;

        public ExchangeDataFeedStateMachine(IExchangeDataFeed exchangeDataFeed)
        {
            _exchangeDataFeed = exchangeDataFeed;
            _state = new StateMachine<ExchangeConnectionState, ExchangeConnectTrigger>(ExchangeConnectionState.None);

            ConfigState();
        }

        private void ConfigState()
        {
            _state
                .Configure(ExchangeConnectionState.None)
                .Permit(ExchangeConnectTrigger.Connect, ExchangeConnectionState.Connecting);

            _state
                .Configure(ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.Fail, ExchangeConnectionState.ConnectingFailed)
                .Permit(ExchangeConnectTrigger.ConnectTaskDone, ExchangeConnectionState.Connected);

            _state.Configure(ExchangeConnectionState.ConnectingFailed)
                .SubstateOf(ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.Retry, ExchangeConnectionState.Connecting)
                .Permit(ExchangeConnectTrigger.StopRetry, ExchangeConnectionState.Disconnected);

            _state
                .Configure(ExchangeConnectionState.Connected)
                .Permit(ExchangeConnectTrigger.Close, ExchangeConnectionState.Closing)
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting)
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking)
                .Permit(ExchangeConnectTrigger.RestoreSubscriptions, ExchangeConnectionState.SubscriptionChecking);


            _state
                .Configure(ExchangeConnectionState.SubscriptionChecking)
                .SubstateOf(ExchangeConnectionState.Connected)
                .Permit(ExchangeConnectTrigger.ResetTimeer, ExchangeConnectionState.SubscriptionRestored)
                .Permit(ExchangeConnectTrigger.EmptySubscription, ExchangeConnectionState.Connected);

            _state
                .Configure(ExchangeConnectionState.SubscriptionRestored)
                .SubstateOf(ExchangeConnectionState.SubscriptionChecking)
                .Permit(ExchangeConnectTrigger.Listen, ExchangeConnectionState.Receving);

            _state
                .Configure(ExchangeConnectionState.Receving)
                .SubstateOf(ExchangeConnectionState.SubscriptionRestored)
                .Permit(ExchangeConnectTrigger.OnExchangeDataExpired, ExchangeConnectionState.Timeout);

            _state
                .Configure(ExchangeConnectionState.Timeout)
                .Permit(ExchangeConnectTrigger.CheckConnection, ExchangeConnectionState.ConnectionChecking);

            _state
                .Configure(ExchangeConnectionState.ConnectionChecking)
                .Permit(ExchangeConnectTrigger.DeathConnection, ExchangeConnectionState.ConnectionLost)
                .Permit(ExchangeConnectTrigger.ConnectionAlive, ExchangeConnectionState.SubscriptionChecking);

            _state
                .Configure(ExchangeConnectionState.ConnectionLost)
                .Permit(ExchangeConnectTrigger.Reconnect, ExchangeConnectionState.Reconnecting);


        }
    }
}