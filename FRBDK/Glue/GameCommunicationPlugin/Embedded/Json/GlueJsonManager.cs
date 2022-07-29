using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using GlueControl.Dtos;

namespace GlueCommunication.Json
{
    internal partial class GlueJsonManager
    {
        public static GlueJsonManager Instance { get; private set; }

        static GlueJsonManager()
        {
            Instance = new GlueJsonManager();
        }

        private const string PacketType_JsonUpdate = "JsonUpdate";

        private Dictionary<string, JsonManager> _jsonScreenManagers = new Dictionary<string, JsonManager>();
        private Dictionary<string, JsonManager> _jsonEntityManagers = new Dictionary<string, JsonManager>();
        private JsonManager _jsonManagerGlueProjectSave = null;
        private JsonManager _jsonManagerEditState = new JsonManager(JObject.Parse("{}"));

        public event Action<string> HandleUpdatedSelection;
        public event Action<GameConnectionManager.Packet> SendPacket;
        public event Func<GameConnectionManager.Packet, Task<GameConnectionManager.Packet>> SendPacketWithResponse;

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

        public Task ProcessUpdatePacket(GameConnectionManager.Packet packet)
        {
            return Task.Run(() =>
            {
                var jObj = JObject.Parse(packet.Payload);

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

                SendPacket(new GameConnectionManager.Packet
                {
                    InResponseTo = packet.Id,
                    PacketType = "Response",
                    Payload = ""
                });
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
    }
}