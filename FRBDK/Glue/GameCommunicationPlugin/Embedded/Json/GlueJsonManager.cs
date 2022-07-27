using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonDiffPatchDotNet.Formatters.JsonPatch;

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
        private JsonManager _jsonManagerGlueProjectSave = null;
        private JsonManager _jsonManagerEditState = new JsonManager(JObject.Parse("{}"));

        internal JsonManager GetScreen(string name)
        {
            _jsonScreenManagers.TryGetValue(name, out JsonManager manager);

            return manager;
        }

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

        public JsonManager GetEditState()
        {
            return _jsonManagerEditState;
        }

        public Task ProcessUpdatePacket(string packet)
        {
            return Task.Run(() =>
            {
                var jObj = JObject.Parse(packet);

                switch (jObj["Type"].Value<string>())
                {
                    case "EditState":
                        var editStateMgr = GetEditState();

                        var operations = editStateMgr.UpdateJson(JToken.Parse(jObj["Patch"].Value<string>()));

                        processOperations(operations);

                        break;
                    default:
                        throw new NotImplementedException();
                }
            });
        }

        private void processOperations(IList<Operation> operations)
        {
            bool doSelection = false;

            foreach (var operation in operations)
            {
                if (operation.Path.StartsWith("/SelectionDTO"))
                    doSelection = true;
            }

            if (doSelection)
            {
                var editStateMgr = GetEditState();
                var editStateJson = editStateMgr.GetCurrentUIJson();

                if (HandleUpdatedSelection != null)
                    HandleUpdatedSelection("SelectObjectDto:" + (editStateJson["SelectionDTO"]?.ToString() ?? ""));
            }
        }

        public event Action<string> HandleUpdatedSelection;
    }
}