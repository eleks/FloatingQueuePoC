namespace FloatingQueue.Server.Services.Proxy
{
    public interface IInternalQueueServiceProxy : IInternalQueueService, IManualProxy {}

    public interface IManualProxy
    {
        void Open();
        void Close();
    }
}
