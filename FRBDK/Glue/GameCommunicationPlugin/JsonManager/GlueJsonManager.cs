using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameJsonCommunicationPlugin.JsonManager
{
    internal class GlueJsonManager
    {
        private Dictionary<string, JsonManager> _jsonScreenManagers = new Dictionary<string, JsonManager>();
        private Dictionary<string, JsonManager> _jsonEntityManagers = new Dictionary<string, JsonManager>();

        internal JsonManager GetScreen(string name)
        {
            _jsonScreenManagers.TryGetValue(name, out JsonManager manager);

            return manager;
        }

        private JsonManager _jsonManagerGlueProjectSave = null;

        public void AddScreen(string key, string json)
        {
            _jsonScreenManagers.Add(key, new JsonManager(JToken.Parse(json)));
        }

        internal JsonManager GetEntity(string name)
        {
            _jsonEntityManagers.TryGetValue(name, out JsonManager manager);

            return manager;
        }

        public void AddEntity(string key, string json)
        {
            _jsonEntityManagers.Add(key, new JsonManager(JToken.Parse(json)));
        }

        public JsonManager GetGlueProjectSave()
        {
            return _jsonManagerGlueProjectSave;
        }

        internal void SetGlueProjectSave(string json)
        {
            _jsonManagerGlueProjectSave = new JsonManager(JToken.Parse(json));
        }

        internal bool ContainsEntity(string entityName)
        {
            return _jsonEntityManagers.ContainsKey(entityName);
        }

        internal bool ContainsScreen(string screenName)
        {
            return _jsonScreenManagers.ContainsKey(screenName);
        }
    }
}
