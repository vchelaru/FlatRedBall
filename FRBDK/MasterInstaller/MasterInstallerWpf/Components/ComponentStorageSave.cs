using System;

namespace MasterInstaller.Components
{
    [Serializable]
    public class ComponentStorageSave
    {
        public SerializableDictionary<string, object> Settings = new SerializableDictionary<string, object>();
    }
}
