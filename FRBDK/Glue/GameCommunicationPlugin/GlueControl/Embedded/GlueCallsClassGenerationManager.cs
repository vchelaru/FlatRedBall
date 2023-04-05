using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GlueControl.Dtos;
using GlueControl.Managers;
using GlueControl.Models;
using Newtonsoft.Json.Linq;

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

        public class CallPropertyParameters
        {
            public bool ReturnToPropertyType {  get; set; }
        }

        public static async Task<object> ConvertToMethodCallToGame(MethodInfo method, Dictionary<string, GlueParameters> parameters, CallMethodParameters callMethodParameters)
        {
            var methodParms = method.GetParameters();

            var convertedParms = new List<object>();
            var correctTypeForParms = new Dictionary<string, string>();

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
                    convertedParms.Add(ConvertItem(parm.ParameterType, parameters[parm.Name].Value, parameters[parm.Name].Dependencies));
                }

                if(parm.ParameterType == typeof(object))
                {
                    correctTypeForParms.Add(parm.Name, parameters[parm.Name].Value.GetType().ToString());
                }
            }

            object returnValue;
            if (callMethodParameters.EchoToGame)
            {
                returnValue = await SendMethodCallToGameWithEcho(method.Name, convertedParms.ToArray(), correctTypeForParms);
            }
            else
            {
                returnValue = await SendMethodCallToGame(method.Name, convertedParms.ToArray(), correctTypeForParms);
            }

            if(method.ReturnType != null && method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var subType = method.ReturnType.GetGenericArguments()[0];
                
                if(subType.IsGenericType && subType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ConvertResponseList(subType.GetGenericArguments()[0], returnValue as JArray);
                }
                else
                {
                    return ConvertResponseItem(subType, (returnValue as JObject).ToObject(subType));
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Converts and sends the property assignment to the game. 
        /// </summary>
        /// <param name="propertyName">The property name, such as CurrentNamedObject.</param>
        /// <param name="propertyType">The type of the property being assigned, such as typeof(NamedObjectSave)</param>
        /// <param name="parameter">The value to assign, such a the current NamedObjectSave.</param>
        /// <param name="callPropertyParameters">Parameters defining the behavior of the call, such as whether the response from Glue is returned back to the game.</param>
        /// <returns></returns>
        public static async Task<object> ConvertToPropertyCallToGame(string propertyName, Type propertyType, GlueParameters parameter, CallPropertyParameters callPropertyParameters)
        {
            GlueParameters parm = parameter;
            object convertedParm;

            bool ShouldHandleAsGeneric()
            {
                var toReturn = propertyType.IsGenericType;

                if (toReturn)
                {
                    toReturn =
                        propertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                        propertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
                        propertyType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) ||
                        propertyType.GetGenericTypeDefinition() == typeof(IList<>);
                }
                return toReturn;
            }

            if (propertyType.IsPrimitive)
            {
                convertedParm = parm.Value;
            }
            else if (ShouldHandleAsGeneric())
            {
                convertedParm = ConvertList(propertyType.GetGenericArguments()[0], (IEnumerable<object>)parm.Value, parm.Dependencies);
            }
            else
            {
                convertedParm = ConvertItem(propertyType, parm.Value, parm.Dependencies);
            }

            object returnValue = await SendPropertyToGame(propertyName, convertedParm);

            if (callPropertyParameters.ReturnToPropertyType)
            {
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ConvertResponseList(propertyType.GetGenericArguments()[0], returnValue as JArray);
                }
                else
                {
                    return ConvertResponseItem(propertyType, (returnValue as JObject).ToObject(propertyType));
                }
            }

            return returnValue;
        }

        private static object ConvertResponseList(Type type, JArray responseAsJArray)
        {
            IList returnList = null;

            foreach(var item in responseAsJArray)
            {
                var convertedItem = ConvertResponseItem(type, item.ToObject(type));

                if (returnList == null)
                    returnList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(convertedItem.GetType()));

                returnList.Add(convertedItem);
            }

            if (returnList == null)
                return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));

            return returnList;
        }

        private static object ConvertResponseItem(Type type, object item)
        {
            if (!item.GetType().IsGenericType || item.GetType().GetGenericTypeDefinition() != typeof(GeneralResponse<>))
            {
                throw new Exception("Response not wrapped in General Response wrapper");
            }

            if(type.GetGenericArguments()[0] == typeof(NamedObjectSave))
            {
                var generalResponse = (GeneralResponse<NamedObjectSave>)item;

                if(generalResponse.Data != null)
                {
                    generalResponse.Data.FixAllTypes();
                }

                return generalResponse;
            }
            else
            {
                throw new Exception("Response Type not implemented");
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

            if (returnList == null)
                return new List<object>();

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
            else if(type == typeof(NamedObjectSave))
            {
                var typedItem = (NamedObjectSave)item;
                return NamedObjectSaveReference.From(typedItem, (GlueElement)dependencies["nosOwner"]);
            }
            else if(type == typeof(GlueElement))
            {
                var typedItem = (GlueElement)item;
                return GlueElementReference.From(typedItem);
            }
            else
            {
                return item;
            }
        }

        private static Task<object> SendMethodCallToGame(string caller, object[] parameters, Dictionary<string, string> correctTypeForParameters) =>
            SendMethodCallToGame(dto: new GluxCommandDto(), caller: caller, parameters: parameters, correctTypeForParameters: correctTypeForParameters);

        private static Task<object> SendMethodCallToGameWithEcho(string caller, object[] parameters, Dictionary<string, string> correctTypeForParameters) =>
            SendMethodCallToGame(dto: new GluxCommandDto() { EchoToGame = true }, caller: caller, parameters: parameters, correctTypeForParameters: correctTypeForParameters);

        private static async Task<object> SendMethodCallToGame(FacadeCommandBase dto, string caller, object[] parameters, Dictionary<string, string> correctTypeForParameters)
        {
            dto.Method = caller;
            foreach (var parameter in parameters)
            {
                dto.Parameters.Add(parameter);
            }
            foreach (var parameter in correctTypeForParameters)
            {
                dto.CorrectTypeForParameters.Add(parameter.Key, parameter.Value);
            }

            var objectResponse = await GlueControlManager.Self.SendToGlue(dto);
            return objectResponse;
        }

        private static async Task<object> SendPropertyToGame(string caller, object value)
        {
            var dto = new GlueStateDto();
            dto.SetPropertyName = caller;
            dto.Parameters.Add(value);

            return await GlueControlManager.Self.SendToGlue(dto);
        }
    }

    public class NosVariableAssignment
    {
        public NamedObjectSave NamedObjectSave;
        public string VariableName;
        public object Value;
    }
}
