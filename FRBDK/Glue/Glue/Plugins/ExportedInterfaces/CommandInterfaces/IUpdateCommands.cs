using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IUpdateCommands
    {
        void Update(GlueElement element);
        void Update(string containerName, NamedObjectSave namedObjectSave);
        void Update(string containerName, CustomVariable customVariable);
        void Update(string containerName, StateSave stateSave);
        void Update(string containerName, StateSaveCategory stateSaveCategory);

        void Add(GlueElement element);
        void Add(string containerName, NamedObjectSave namedObjectSave);
        void Add(string containerName, CustomVariable customVariable);
        void Add(string containerName, StateSave stateSave);
        void Add(string containerName, StateSaveCategory stateSaveCategory);


        void Remove(GlueElement element);
        void Remove(string containerName, NamedObjectSave namedObjectSave);
        void Remove(string containerName, CustomVariable customVariable);
        void Remove(string containerName, StateSave stateSave);
        void Remove(string containerName, StateSaveCategory stateSaveCategory);
    }
}
