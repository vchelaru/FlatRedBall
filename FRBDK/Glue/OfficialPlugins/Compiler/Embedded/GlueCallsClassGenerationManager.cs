using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GlueControl.Dtos;
using GlueControl.Managers;
using GlueControl.Models;

namespace GlueControl
{
    internal static class GlueCallsClassGenerationManager
    {
        public class GlueParameters
        {
            public object Value { get; set; }
            public Dictionary<string, object> Dependencies { get; set; }
        }

        public class CallMethodParameters
        {
            public bool EchoToGame { get; set; }
        }

        public static async Task ConvertToMethodCallToGame(MethodBase method, Dictionary<string, GlueParameters> parameters, CallMethodParameters callMethodParameters)
        {
            var methodParms = method.GetParameters();

            var convertedParms = new List<object>();

            foreach (var parm in methodParms)
            {
                if (!parameters.ContainsKey(parm.Name))
                    continue;

                if (parm.ParameterType.IsPrimitive)
                {
                    convertedParms.Add(parameters[parm.Name].Value);
                }
                else if (parm.ParameterType.IsGenericType && parm.ParameterType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    convertedParms.Add(ConvertList(parm.ParameterType.GetGenericArguments()[0], (IEnumerable<object>)parameters[parm.Name].Value, parameters[parm.Name].Dependencies));
                }
                else
                {
                    convertedParms.Add(ConvertItem(parm.ParameterType, parameters[parm.Name], parameters[parm.Name].Dependencies));
                }
            }

            if (callMethodParameters.EchoToGame)
            {
                await SendMethodCallToGameWithEcho(method.Name, convertedParms.ToArray());
            }
            else
            {
                await SendMethodCallToGame(method.Name, convertedParms.ToArray());
            }
        }

        private static object ConvertList(Type type, IEnumerable<object> items, Dictionary<string, object> dependencies)
        {
            IList returnList = null;

            foreach (var item in items)
            {
                var convertedItem = ConvertItem(type, item, dependencies);

                if (returnList == null)
                    returnList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(convertedItem.GetType()));

                returnList.Add(convertedItem);
            }

            return returnList;
        }

        private static object ConvertItem(Type type, object item, Dictionary<string, object> dependencies)
        {
            if (type == typeof(NosVariableAssignment))
            {
                var typedItem = (NosVariableAssignment)item;
                return new NosReferenceVariableAssignment
                {
                    NamedObjectSave = NamedObjectSaveReference.From(typedItem.NamedObjectSave, (GlueElement)dependencies["nosOwner"]),
                    VariableName = typedItem.VariableName,
                    Value = TypedParameter.FromValue(typedItem.Value)
                };
            }
            else
            {
                return item;
            }
        }

        private static Task<object> SendMethodCallToGame(string caller = null, params object[] parameters) =>
            SendMethodCallToGame(new GluxCommandDto(), caller, parameters);


        private static Task<object> SendMethodCallToGameWithEcho(string caller = null, params object[] parameters) =>
            SendMethodCallToGame(new GluxCommandDto() { EchoToGame = true }, caller, parameters);

        private static async Task<object> SendMethodCallToGame(FacadeCommandBase dto, string caller = null, params object[] parameters)
        {
            dto.Method = caller;
            foreach (var parameter in parameters)
            {
                dto.Parameters.Add(parameter);
            }

            var objectResponse = await GlueControlManager.Self.SendToGlue(dto);
            return objectResponse;
        }
    }

    public class NosVariableAssignment
    {
        public NamedObjectSave NamedObjectSave;
        public string VariableName;
        public object Value;
    }
}
