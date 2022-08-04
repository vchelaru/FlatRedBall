using System;
using System.Collections.Generic;
using System.Linq;

namespace GameJsonCommunicationPlugin.JsonManager
{
    internal class GlueJsonManager
    {
        internal const string TYPE_SCREEN = "Screen";
        internal const string TYPE_ENTITY = "Entity";
        internal const string TYPE_GLUE = "Glue";

        private Dictionary<string, Dictionary<string, JsonManager>> _managers = new Dictionary<string, Dictionary<string, JsonManager>>();

        public GlueJsonManager()
        {
            _managers.Add(TYPE_GLUE, new Dictionary<string, JsonManager>());
            _managers.Add(TYPE_SCREEN, new Dictionary<string, JsonManager>());
            _managers.Add(TYPE_ENTITY, new Dictionary<string, JsonManager>());
        }

        internal JsonManager Get(string type, string name)
        {
            if (!_managers.ContainsKey(type) || !_managers[type].ContainsKey(name))
                return null;

            return _managers[type][name];
        }

        public void Add(string type, string key)
        {
            if (!_managers.ContainsKey(type))
                throw new Exception($"Type {type} is invalid");

            _managers[type].Add(key, new JsonManager());
        }

        internal IList<ItemKey> GetAll()
        {
            var items = new List<ItemKey>();
            foreach(var type in _managers.Keys)
            {
                foreach(var name in _managers[type].Keys)
                {
                    items.Add(new ItemKey
                    {
                        Type = type,
                        Name = name
                    });
                }
            }
            return items;
        }

        internal class ItemKey
        {
            public string Type { get; internal set; }
            public string Name { get; internal set; }
        }
    }
}
