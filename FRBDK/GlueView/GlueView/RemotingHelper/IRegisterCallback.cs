namespace RemotingHelper
{
    public interface IRegisterCallback<T>
    {
        void RegisterCallback(T callback);
    }
}
