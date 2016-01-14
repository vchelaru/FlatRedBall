using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface IStateChange : IPlugin
    {
        void ReactToStateNameChange(IElement element, string oldName, string newName);
        void ReactToStateRemoved(IElement element, string stateName);
    }
}
