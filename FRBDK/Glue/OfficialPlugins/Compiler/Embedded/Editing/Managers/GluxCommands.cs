using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GlueControl.Managers
{
    internal class GluxCommands
    {
        // nosOwner is needed until we have support for ObjectFinder, which requires the full GlueProjectSave
        public async Task<GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement nosOwner, GlueElement targetElement, bool save = true)
        {
            // convert nos and target element to references
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var targetElementReference = new GlueElementReference();
            targetElementReference.ElementNameGlue = targetElement.Name;
            var response = await SendToGame(nameof(CopyNamedObjectIntoElement), nosReference, targetElementReference, save);

            var responseAsJObject = response as JObject;
            responseAsJObject.ToObject<GeneralResponse<NamedObjectSave>>();
            var generalResponse = responseAsJObject.ToObject<GeneralResponse<NamedObjectSave>>();

            return generalResponse;
        }

        public async Task SetVariableOn(NamedObjectSave nos, GlueElement nosOwner, string memberName, object value)
        {
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var typedParameter = TypedParameter.FromValue(value);

            await SendToGame(nameof(SetVariableOn), nosReference, memberName, typedParameter);
        }


        private async Task<object> SendToGame(string caller = null, params object[] parameters)
        {
            var dto = new GluxCommandDto();
            dto.Method = caller;
            dto.Parameters.AddRange(parameters);

            var objectResponse = await GlueControlManager.Self.SendToGlue(dto);
            return objectResponse;
        }
    }
}
