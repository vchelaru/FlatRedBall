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
    #region Classes

    public class NosVariableAssignment
    {
        public NamedObjectSave NamedObjectSave;
        public string VariableName;
        public object Value;
    }

    #endregion

    internal class GluxCommands : GlueCommandsStateBase
    {
        // nosOwner is needed until we have support for ObjectFinder, which requires the full GlueProjectSave
        public async Task<GeneralResponse<NamedObjectSave>> CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement nosOwner, GlueElement targetElement,
            bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            // convert nos and target element to references
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var targetElementReference = GlueElementReference.From(targetElement);

            var response = await SendMethodCallToGame(nameof(CopyNamedObjectIntoElement),
                nosReference,
                targetElementReference,
                performSaveAndGenerateCode,
                updateUi);

            var responseAsJObject = response as JObject;
            var generalResponse = responseAsJObject.ToObject<GeneralResponse<NamedObjectSave>>();

            if (generalResponse.Data != null)
            {
                generalResponse.Data.FixAllTypes();
            }

            return generalResponse;
        }

        public async Task<List<GeneralResponse<NamedObjectSave>>> CopyNamedObjectListIntoElement(List<NamedObjectSave> nosList, GlueElement nosOwner, GlueElement targetElement,
            bool performSaveAndGenerateCode = true, bool updateUi = true)
        {
            List<NamedObjectSaveReference> namedReferenceList = new List<NamedObjectSaveReference>();
            foreach (var nos in nosList)
            {
                var reference = NamedObjectSaveReference.From(nos, nosOwner);
                namedReferenceList.Add(reference);
            }

            var targetElementReference = GlueElementReference.From(targetElement);

            var response = await SendMethodCallToGame(nameof(CopyNamedObjectListIntoElement),
                namedReferenceList,
                targetElementReference,
                performSaveAndGenerateCode,
                updateUi);

            var responseAsJArray = response as JArray;
            List<GeneralResponse<NamedObjectSave>> listToReturn = new List<GeneralResponse<NamedObjectSave>>();

            foreach (var jobject in responseAsJArray)
            {
                var generalResponse = jobject.ToObject<GeneralResponse<NamedObjectSave>>();

                if (generalResponse.Data != null)
                {
                    generalResponse.Data.FixAllTypes();
                }

                listToReturn.Add(generalResponse);
            }
            return listToReturn;
        }

        public async Task SetVariableOn(NamedObjectSave nos, GlueElement nosOwner, string memberName, object value, bool performSaveAndGenerateCode = true,
            bool updateUi = true, bool recordUndo = true, bool echoToGame = false)
        {
            var nosReference = NamedObjectSaveReference.From(nos, nosOwner);

            var typedValue = TypedParameter.FromValue(value);

            if (echoToGame)
            {
                await SendMethodCallToGameWithEcho(nameof(SetVariableOn), nosReference, memberName, typedValue, performSaveAndGenerateCode, updateUi, recordUndo);
            }
            else
            {
                await SendMethodCallToGame(nameof(SetVariableOn), nosReference, memberName, typedValue, performSaveAndGenerateCode, updateUi, recordUndo);
            }
        }

        public async Task SetVariableOnList(List<NosVariableAssignment> nosVariableAssignments, GlueElement nosOwner,
            bool performSaveAndGenerateCode = true,
            bool updateUi = true,
            bool recordUndo = true,
            bool echoToGame = false)
        {
            List<NosReferenceVariableAssignment> nosReferenceVariableAssignments = new List<NosReferenceVariableAssignment>();
            foreach (var assignment in nosVariableAssignments)
            {
                var referenceAssignment = new NosReferenceVariableAssignment
                {
                    NamedObjectSave = NamedObjectSaveReference.From(assignment.NamedObjectSave, nosOwner),
                    VariableName = assignment.VariableName,
                    Value = TypedParameter.FromValue(assignment.Value)
                };

                nosReferenceVariableAssignments.Add(referenceAssignment);
            }

            if (echoToGame)
            {
                await SendMethodCallToGameWithEcho(nameof(SetVariableOnList), nosReferenceVariableAssignments, performSaveAndGenerateCode, updateUi, recordUndo);
            }
            else
            {
                await SendMethodCallToGame(nameof(SetVariableOnList), nosReferenceVariableAssignments, performSaveAndGenerateCode, updateUi, recordUndo);
            }
        }

        public Task SaveGlux(TaskExecutionPreference taskExecutionPreference = TaskExecutionPreference.Asap) =>
            SendMethodCallToGame(nameof(SaveGlux), taskExecutionPreference);



        private Task<object> SendMethodCallToGame(string caller = null, params object[] parameters) =>
            SendMethodCallToGame(new GluxCommandDto(), caller, parameters);


        private Task<object> SendMethodCallToGameWithEcho(string caller = null, params object[] parameters) =>
            SendMethodCallToGame(new GluxCommandDto() { EchoToGame = true }, caller, parameters);
    }
}
