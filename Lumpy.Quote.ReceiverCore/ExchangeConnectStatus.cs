namespace Lumpy.Quote.ReceiverCore
{
    public enum ExchangeConnectStatus
    {
        None,
        Connecting,
        Open,
        CloseSent,
        CloseReceived,
        Closed,
        Aborted,
    }
}