using Newtonsoft.Json.Linq;
using System.Linq;

namespace GlueCommunication.Json
{
    internal class ScreenJsonContainer : JsonContainer
    {
        public ScreenJsonContainer(JToken json) : base(json)
        {
        }

        public void UpdateVariableOnInstance(string instanceName, string variableName, object value)
        {
            var element = Json.SelectToken($"$..NamedObjects[?(@.InstanceName == '{instanceName}')]");

            if(element != null)
            {
                //Update Instruction Save
                var instructionSave = element.SelectToken($"$..InstructionSaves[?(@.Member == '{variableName}')]");
                var prop = instructionSave.Children().OfType<JProperty>().Where(item => item.Name == "Value").FirstOrDefault();
                prop.Value = JToken.FromObject(value);
            }
        }
    }
}
