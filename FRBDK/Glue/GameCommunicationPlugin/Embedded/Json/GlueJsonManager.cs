using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using GlueControl.Dtos;
using Newtonsoft.Json;

namespace GlueCommunication.Json
{
    internal partial class GlueJsonManager
    {
        public static readonly GlueJsonManager Instance = new GlueJsonManager();

        internal const string TYPE_SCREEN = "Screen";
        internal const string TYPE_ENTITY = "Entity";
        internal const string TYPE_GLUE = "Glue";

        private object _lock = new object();
        private Dictionary<string, Dictionary<string, JsonManager>> _managers = new Dictionary<string, Dictionary<string, JsonManager>>();

        public GlueJsonManager()
        {
            _managers.Add(TYPE_GLUE, new Dictionary<string, JsonManager>());
            _managers.Add(TYPE_SCREEN, new Dictionary<string, JsonManager>());
            _managers.Add(TYPE_ENTITY, new Dictionary<string, JsonManager>());
        }

        internal JsonManager Get(string type, string name)
        {
            lock (_lock)
            {
                if (!_managers.ContainsKey(type) || !_managers[type].ContainsKey(name))
                    return null;

                return _managers[type][name];
            }
        }

        public void Add(string type, string key)
        {
            lock (_lock)
            {
                if (!_managers.ContainsKey(type))
                    throw new Exception($"Type {type} is invalid");

                _managers[type].Add(key, new JsonManager());
            }
        }

        internal async Task ProcessUpdatePacket(GameConnectionManager.Packet packet)
        {
            await Task.Run(() =>
            {
                var data = JToken.Parse(packet.Payload);

                var type = data["Type"].Value<string>();
                var name = data["Name"].Value<string>();
                var patch = data["Patch"].Value<string>();
                if (type != null && name != null && patch != null)
                {
                    var container = JsonConvert.DeserializeObject<JsonManager.PatchContainer>(patch);
                    JsonManager mgr;
                    lock (_lock)
                    {
                        mgr = Get(type, name);

                        if (mgr == null)
                        {
                            Add(type, name);
                            mgr = Get(type, name);
                        }
                    }

                    mgr.UpdateJson(container).Wait();
                }
            });
        }
    }
}