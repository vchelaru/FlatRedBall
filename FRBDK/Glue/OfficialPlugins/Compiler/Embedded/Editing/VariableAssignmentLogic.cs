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
                            response.WasVariableAssigned = screen.ApplyVariable(splitVariable[2], variableValue, aarect);
                        }

                        if (!response.WasVariableAssigned)
                        {
                            var circle = ShapeManager.VisibleCircles.FirstOrDefault(item =>
                                item.Parent == null &&
                                item.Name == splitVariable[1]);
                            if (circle != null)
                            {
                                response.WasVariableAssigned = screen.ApplyVariable(splitVariable[2], variableValue, circle);
                            }
                        }

                        if (!response.WasVariableAssigned)
                        {
                            var polygon = ShapeManager.VisiblePolygons.FirstOrDefault(item =>
                                item.Parent == null &&
                                item.Name == splitVariable[1]);

                            if (polygon != null)
                            {
                                response.WasVariableAssigned = screen.ApplyVariable(splitVariable[2], variableValue, polygon);
                            }
                        }

                        if(!response.WasVariableAssigned)
                        {
                            var sprite = SpriteManager.AutomaticallyUpdatedSprites.FirstOrDefault(item =>
                                item.Parent == null &&
                                item.Name == splitVariable[1]);

                            if(sprite != null)
                            {
                                response.WasVariableAssigned = screen.ApplyVariable(splitVariable[2], variableValue, sprite);
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
            var type = data.Type;
            object variableValue = ConvertStringToType(type, data.VariableValue);

            return variableValue;
        }
    
        public static object ConvertStringToType(string type, string variableValue)
        {
            object convertedValue = variableValue;
            switch (type)
            {
                case "float":
                case nameof(Single):
                    convertedValue = float.Parse(variableValue);
                    break;
                case "int":
                case nameof(Int32):
                    convertedValue = int.Parse(variableValue);
                    break;
                case "bool":
                case nameof(Boolean):
                    convertedValue = bool.Parse(variableValue.ToLowerInvariant());
                    break;
                case "double":
                case nameof(Double):
                    convertedValue = double.Parse(variableValue);
                    break;
                case "Microsoft.Xna.Framework.Color":
                    convertedValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(variableValue).GetValue(null);
                    break;
                case "Texture2D":
                case "Microsoft.Xna.Framework.Graphics.Texture2D":
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(
                            variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                    }
                    else
                    {
                        convertedValue = (Microsoft.Xna.Framework.Graphics.Texture2D)null;
                    }
                    break;
                case "FlatRedBall.Graphics.Animation.AnimationChainList":
                case "AnimationChainList":
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>(
                            variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                    }
                    else
                    {
                        convertedValue = (FlatRedBall.Graphics.Animation.AnimationChainList)null;
                    }
                    break;
                case nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode):
                    if(int.TryParse(variableValue, out int parsedInt))
                    {
                        convertedValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)parsedInt;
                    }
                    break;

            }

            return convertedValue;
        }
    }
}
