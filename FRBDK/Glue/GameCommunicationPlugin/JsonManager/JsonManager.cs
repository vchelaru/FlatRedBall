using JsonDiffPatchDotNet;
using Newtonsoft.Json.Linq;
using System;

namespace GameJsonCommunicationPlugin.JsonManager
{
    internal class JsonManager
    {
        public class PatchContainer
        {
            public Guid InstanceId { get; set; }
            public long Version { get; set; }
            public string Type { get; set; }
            public JToken Data { get; set; }
        }

        private JToken _currentJson = null;
        public JToken CurrentJson
        {
            get
            {
                var copy = _currentJson;
                //Clone Json so it can't be modified
                return copy?.DeepClone() ?? JToken.Parse("{}");
            }
            private set
            {
                _currentJson = value;
            }
        }

        private object _lock = new object();
        private JsonDiffPatch _jdp;
        private long _currentUIVersion = 0;
        private Guid _instanceId = Guid.NewGuid();

        public JsonManager()
        {
            _jdp = new JsonDiffPatch();
        }

        public PatchContainer UpdateJson(JToken newJson)
        {
            lock (_lock)
            {
                if(_currentJson == null)
                {
                    CurrentJson = newJson;
                    _currentUIVersion = 0;

                    return new PatchContainer
                    {
                        InstanceId = _instanceId,
                        Version = ++_currentUIVersion,
                        Type = "Full",
                        Data = newJson.ToString()
                    };
                }
                else
                {
                    var beforeJson = CurrentJson;
                    var patch = _jdp.Diff(beforeJson, newJson);
                    if (patch != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Before JSON");
                        System.Diagnostics.Debug.WriteLine(beforeJson.ToString());
                        CurrentJson = _jdp.Patch(beforeJson, patch);
                        System.Diagnostics.Debug.WriteLine("Patch");
                        System.Diagnostics.Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(patch));

                        if (newJson.ToString() != _currentJson.ToString())
                        {
                            System.Diagnostics.Debug.WriteLine("Match");
                        }

                        return new PatchContainer
                        {
                            InstanceId = _instanceId,
                            Version = ++_currentUIVersion,
                            Type = "Patch",
                            Data = Newtonsoft.Json.JsonConvert.SerializeObject(patch)
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        internal PatchContainer Reset(JToken newJson)
        {
            lock(_lock)
            {
                _instanceId = Guid.NewGuid();
                _currentUIVersion = 0;
                _currentJson = newJson.DeepClone();

                return new PatchContainer
                {
                    InstanceId = _instanceId,
                    Version = ++_currentUIVersion,
                    Type = "Full",
                    Data = _currentJson.ToString()
                };
            }
        }
    }
}
