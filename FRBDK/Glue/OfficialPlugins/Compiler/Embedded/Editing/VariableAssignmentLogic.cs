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

            try
            {

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

                    // We really don't know if it was assigned yet since the Screen isn't returning true or false, but
                    // we don't want it to always be false because not returning true prevents all assignment from being 
                    // responded to by glue
                    response.WasVariableAssigned = true;
                    // eventually ApplyVariable should return true/false
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

                        if(!response.WasVariableAssigned)
                        {
                            var sprite = SpriteManager.AutomaticallyUpdatedSprites.FirstOrDefault(item =>
                                item.Parent == null &&
                                item.Name == splitVariable[1]);

                            if(sprite != null)
                            {
                                screen.ApplyVariable(splitVariable[2], variableValue, sprite);
                                response.WasVariableAssigned = true;
                            }
                        }
                    }
                    if (!response.WasVariableAssigned)
                    {
                        try
                        {
                            response.WasVariableAssigned = screen.ApplyVariable(data.VariableName, variableValue);
                        }
                        catch (Exception e)
                        {
                            response.Exception = e.ToString(); ;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                response.Exception = e.ToString();
                response.WasVariableAssigned = false;
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
                case "Texture2D":
                case "Microsoft.Xna.Framework.Graphics.Texture2D":
                    variableValue = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(
                        data.VariableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                    break;
                case "FlatRedBall.Graphics.Animation.AnimationChainList":
                case "AnimationChainList":
                    variableValue = FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>(
                        data.VariableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                    break;

            }

            return variableValue;
        }
    }
}
