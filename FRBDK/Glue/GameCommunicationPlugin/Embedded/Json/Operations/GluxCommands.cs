using GlueControl;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueCommunication.Json.Operations
{
    internal class GluxCommands
    {
        public async Task SetVariableOnList(List<NosVariableAssignment> nosVariableAssignments, GlueElement nosOwner)
        {
            if(nosOwner.GetType() == typeof(GlueControl.Models.ScreenSave))
            {
                var jsonManager = GlueJsonManager.Instance.GetScreen(nosOwner.Name);
                var screenContainer = new ScreenJsonContainer(jsonManager.GetCurrentUIJson());

                foreach(var variable in nosVariableAssignments)
                {
                    screenContainer.UpdateVariableOnInstance(variable.NamedObjectSave.InstanceName, variable.VariableName, variable.Value);
                }

                jsonManager.ApplyUIUpdate(screenContainer.Json);
            }
        }
    }
}
