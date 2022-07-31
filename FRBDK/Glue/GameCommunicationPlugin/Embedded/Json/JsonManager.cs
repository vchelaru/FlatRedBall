using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GlueCommunication.Json
{
    internal class JsonManager
    {
        public class PatchContainer
        {
            public Guid InstanceId { get; set; }
            public long Version { get; set; }
            public string Type { get; set; }
            public string Data { get; set; }
        }

        public event Action<IList<Operation>> JsonUpdated;

        private JToken _currentJson;
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
        private JsonDeltaFormatter _jdf;
        private List<PatchContainer> _uiPatches = new List<PatchContainer>();
        private Guid _currentInstance = Guid.Empty;
        private long _lastVersion = 0;
        private bool _resyncRequired = false;

        public JsonManager()
        {
            _jdp = new JsonDiffPatch();
            _jdf = new JsonDeltaFormatter();
        }

        public async Task UpdateJson(PatchContainer patch)
        {
            await Task.Run(async () =>
            {
                var startTime = DateTime.Now;

                while(patch.Type == "Patch" && _currentInstance != patch.InstanceId && _lastVersion + 1 != patch.Version)
                {
                    await Task.Delay(10);

                    if (_resyncRequired)
                        return;

                    if ((DateTime.Now - startTime).TotalSeconds >= 10)
                    {
                        _resyncRequired = true;
                        return;
                    }
                }

                lock (_lock)
                {
                    if(patch.Type == "Full")
                    {
                        _resyncRequired = false;
                        var previousJson = _currentJson;
                        _currentJson = JToken.Parse(patch.Data);
                        _currentInstance = patch.InstanceId;
                        _lastVersion = patch.Version;

                        if(previousJson != null && JsonUpdated != null)
                        {
                            JsonUpdated(_jdf.Format(_jdp.Diff(previousJson, _currentJson)));
                        }
                    }else if(patch.Type == "Patch")
                    {
                        var previousJson = _currentJson;
                        var restoredPatch = Newtonsoft.Json.JsonConvert.DeserializeObject<JToken>(patch.Data);
                        System.Diagnostics.Debug.WriteLine("Before JSON");
                        System.Diagnostics.Debug.WriteLine(previousJson.ToString());
                        System.Diagnostics.Debug.WriteLine("Patch");
                        System.Diagnostics.Debug.WriteLine(restoredPatch.ToString());
                        _currentJson = _jdp.Patch(previousJson, restoredPatch);
                        _lastVersion = patch.Version;

                        if (JsonUpdated != null)
                        {
                            JsonUpdated(_jdf.Format(_jdp.Diff(previousJson, _currentJson)));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            });
        }
    }
}