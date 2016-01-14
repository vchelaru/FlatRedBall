using System;
using RemotingHelper;

namespace InteractiveInterface
{
    public class RegisterInterface : MarshalByRefObject, IRegisterCallback<IInteractiveInterface>
    {
        public void RegisterCallback(IInteractiveInterface callback)
        {
            InteractiveConnection.Callback = callback;
        }
    }
}
