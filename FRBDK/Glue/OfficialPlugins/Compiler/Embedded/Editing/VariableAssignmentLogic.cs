{CompilerDirectives}

using EditModeProject.GlueControl.Dtos;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Editing
{
    public static class VariableAssignmentLogic
    {
        public static GlueVariableSetDataResponse SetVariable(GlueVariableSetData data)
        {
            object variableValue = ConvertVariableValue(data);

            var response = new GlueVariableSetDataResponse();

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;

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
                response.WasVariableAssigned = false;
                var splitVariable = data.VariableName.Split('.');
                if (splitVariable[0] == "this" && splitVariable.Length > 1)
                {

                    var aarect = ShapeManager.VisibleRectangles.FirstOrDefault(item =>
                        item.Parent == null &&
                        item.Name == splitVariable[1]);
                    if (aarect != null)
                    {
                        screen.ApplyVariable(splitVariable[2], variableValue, aarect);
                        response.WasVariableAssigned = true;
                    }

                    if (!response.WasVariableAssigned)
                    {
                        var circle = ShapeManager.VisibleCircles.FirstOrDefault(item =>
                            item.Parent == null &&
                            item.Name == splitVariable[1]);
                        if (circle != null)
                        {
                            screen.ApplyVariable(splitVariable[2], variableValue, circle);
                            response.WasVariableAssigned = true;
                        }
                    }

                    if (!response.WasVariableAssigned)
                    {
                        var polygon = ShapeManager.VisiblePolygons.FirstOrDefault(item =>
                            item.Parent == null &&
                            item.Name == splitVariable[1]);

                        if (polygon != null)
                        {
                            screen.ApplyVariable(splitVariable[2], variableValue, polygon);
                            response.WasVariableAssigned = true;
                        }
                    }

                }
                if (!response.WasVariableAssigned)
                {
                    try
                    {
                        screen.ApplyVariable(data.VariableName, variableValue);
                        // if no exception, assume it worked?
                        response.WasVariableAssigned = true;
                    }
                    catch (Exception e)
                    {
                        response.Exception = e.ToString(); ;
                    }
                }
            }
            return response;
        }

        private static object ConvertVariableValue(GlueVariableSetData data)
        {
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
                    variableValue = bool.Parse(data.VariableValue.ToLowerInvariant());
                    break;
                case "double":
                case nameof(Double):
                    variableValue = double.Parse(data.VariableValue);
                    break;
                case "Microsoft.Xna.Framework.Color":
                    variableValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(data.VariableValue).GetValue(null);
                    break;
            }

            return variableValue;
        }
    }
}
