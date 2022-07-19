using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueCommunication.Json
{
    internal class GlueJsonManager
    {
        public static GlueJsonManager Instance { get; private set; }

        static GlueJsonManager()
        {
            Instance = new GlueJsonManager();
        }

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
    }
}
