using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GlueControl.Models;

namespace GlueControl.Managers
{
    internal class GluxCommands : GlueCommandsStateBase
    {
        // nosOwner is needed until we have support for ObjectFinder, which requires the full GlueProjectSave
        public async Task<GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement nosOwner, GlueElement targetElement,
            bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            // convert nos and target element to references
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var targetElementReference = new GlueElementReference();
            targetElementReference.ElementNameGlue = targetElement.Name;
            var response = await SendMethodCallToGame(nameof(CopyNamedObjectIntoElement),
                nosReference,
                targetElementReference,
                performSaveAndGenerateCode,
                updateUi);

            var responseAsJObject = response as JObject;
            responseAsJObject.ToObject<GeneralResponse<NamedObjectSave>>();
            var generalResponse = responseAsJObject.ToObject<GeneralResponse<NamedObjectSave>>();

            if (generalResponse.Data != null)
            {
                generalResponse.Data.FixAllTypes();
            }

            return generalResponse;
        }

        public async Task SetVariableOn(NamedObjectSave nos, GlueElement nosOwner, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true)
        {
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var typedValue = TypedParameter.FromValue(value);

            await SendMethodCallToGame(nameof(SetVariableOn), nosReference, memberName, typedValue, performSaveAndGenerateCode, updateUi);
        }

        public Task SaveGlux(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap) => 
            SendMethodCallToGame(nameof(SaveGlux), taskExecutionPreference);



        private Task<object> SendMethodCallToGame(string caller = null, params object[] parameters) =>
            SendMethodCallToGame(new GluxCommandDto(), caller, parameters);
    }
}
