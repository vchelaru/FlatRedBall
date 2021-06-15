using {ProjectNamespace}.GlueControl.Dtos;
using FlatRedBall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Editing
{
    public static class VariableAssignmentLogic
    {
        public static void SetVariable(GlueVariableSetData data)
        {

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;


            object variableValue = data.VariableValue;

            switch (data.Type)
            {
                case "float":
                case nameof(Single):
                    variableValue = float.Parse(data.VariableValue);
                    break;
                case "int":
                case nameof(Int32):
                    variableValue = int.Parse(data.VariableValue);
                    break;
                case "bool":
                case nameof(Boolean):
                    variableValue = bool.Parse(data.VariableValue);
                    break;
                case "double":
                case nameof(Double):
                    variableValue = double.Parse(data.VariableValue);
                    break;
                case "Microsoft.Xna.Framework.Color":
                    variableValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(data.VariableValue).GetValue(null);
                    break;
            }

            var ownerType = typeof(VariableAssignmentLogic).Assembly.GetType(data.InstanceOwner);
            var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType);
            if (isEntity)
            {
                foreach (var item in SpriteManager.ManagedPositionedObjects)
                {
                    if (ownerType.IsAssignableFrom(item.GetType()))
                    {
                        var variableName = data.VariableName.Substring("this.".Length);
                        screen.ApplyVariable(variableName, variableValue, item);
                    }
                }
            }
            else
            {
                screen.ApplyVariable(data.VariableName, variableValue);
            }
        }
    }
}
