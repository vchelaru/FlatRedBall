using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GlueCommunication.Json
{
    internal class JsonManager
    {
        private JToken _currentJson;
        public JToken CurrentJson
        {
            get
            {
                //Clone Json so it can't be modified
                return _currentJson?.DeepClone() ?? JToken.Parse("{}");
            }
            private set
            {
                _currentJson = value;
            }
        }

        private JsonDiffPatch _jdp;
        private JToken _currentUIJson = null;
        private bool _currentUIJsonExpired = true;
        private JsonDeltaFormatter _jdf;
        private List<JToken> _uiPatches = new List<JToken>();

        public JsonManager(JToken json)
        {
            CurrentJson = json.DeepClone();
            _jdp = new JsonDiffPatch();
            _jdf = new JsonDeltaFormatter();
        }

        public JToken ApplyUIUpdate(JToken json)
        {
            var patch = _jdp.Diff(GetCurrentUIJson(), json);
            //Add patch to list
            _uiPatches.Add(patch);
            //Set flag to regenerate UI json
            _currentUIJsonExpired = true;

            return patch;
        }

        public JToken GetCurrentUIJson()
        {
            //Check if we need to regenerate current UI json
            if (_currentUIJsonExpired || _currentUIJson == null)
            {
                //Get base json
                var currentJson = CurrentJson;

                //Apply ui patches to base json
                foreach (var patch in _uiPatches)
                {
                    currentJson = _jdp.Patch(currentJson, patch);
                }

                return currentJson;
            }
            else
            {
                return _currentUIJson;
            }
        }

        public IList<Operation> UpdateJson(JToken patch)
        {
            //Save UI Json from before
            var _beforeUIJson = GetCurrentUIJson();

            //Expire UI Json so it will recreate
            _currentUIJsonExpired = true;
            //Update base level json with the update
            CurrentJson = _jdp.Patch(CurrentJson, patch);

            //Get the current UI json
            _currentUIJson = GetCurrentUIJson();
            //Clear out the UI patches
            _uiPatches.Clear();
            //Add a single UI patch that has the current differences
            ApplyUIUpdate(_currentUIJson);

            //Get operations needed
            return _jdf.Format(_jdp.Diff(_beforeUIJson, GetCurrentUIJson()));
        }
    }
}
