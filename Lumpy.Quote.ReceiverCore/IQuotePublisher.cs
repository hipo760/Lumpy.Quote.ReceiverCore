namespace Lumpy.Quote.ReceiverCore
{
    public interface ITickPublisher
    {
        void OnNewTick(string tick);
    }
}