{CompilerDirectives}

using EditModeProject.GlueControl.Dtos;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FlatRedBall.Math.Collision;
using System.Collections;


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

                        if(!response.WasVariableAssigned)
                        {
                            if(splitVariable[2] == "Entire CollisionRelationship")
                            {
                                response.WasVariableAssigned = TryAssignCollisionRelationship(splitVariable[1],
                                    JsonConvert.DeserializeObject< Models.NamedObjectSave>(data.VariableValue));
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

        private static bool TryAssignCollisionRelationship(string relationshipName, Models.NamedObjectSave namedObject)
        {
            var handled = false;

            var collisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(item => item.Name == relationshipName);

            if(collisionRelationship != null)
            {
                T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);

                //DelegateCollision
                var collisionType = Get<int>("CollisionType");

                var firstMass = Get<float>("FirstCollisionMass");
                var secondMass = Get<float>("SecondCollisionMass");
                var elasticity = Get<float>("CollisionElasticity");

                var firstObjectName = Get<string>("FirstCollisionName");
                var secondObjectName = Get<string>("SecondCollisionName");

                object firstObject = null;
                object secondObject = null;

                var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;

                currentScreen.GetInstance($"{firstObjectName}.Unused", currentScreen, out _, out firstObject);
                currentScreen.GetInstance($"{secondObjectName}.Unused", currentScreen, out _, out secondObject);

                var isFirstList = firstObject is IList;
                var isSecondList = secondObject is IList;
                var isSecondShapeCollection = secondObject is ShapeCollection;

                var existingRelationshipTypeName = collisionRelationship.GetType().FullName;

                Type desiredRelationshipType = null;

                Type GetStandardCollisionRelationshipType()
                {
                    if(isFirstList && isSecondList)
                    {
                        return typeof(ListVsListRelationship<,>)
                            .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType().GenericTypeArguments[0]);
                    }
                    else if(isFirstList && isSecondShapeCollection)
                    {
                        return typeof(ListVsShapeCollectionRelationship<>)
                            .MakeGenericType(firstObject.GetType().GenericTypeArguments[0]);
                    }
                    else if(isFirstList)
                    {
                        return typeof(ListVsPositionedObjectRelationship<,>)
                            .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType());
                    }
                    else if(isSecondList)
                    {
                        return typeof(PositionedObjectVsListRelationship<,>)
                            .MakeGenericType(firstObject.GetType(), secondObject.GetType().GenericTypeArguments[0]);
                    }
                    else if(isSecondShapeCollection)
                    {
                        return typeof(PositionedObjectVsShapeCollection<>)
                            .MakeGenericType(firstObject.GetType());
                    }
                    else
                    {
                        return typeof(PositionedObjectVsPositionedObjectRelationship<,>)
                            .MakeGenericType(firstObject.GetType(), secondObject.GetType());
                    }
                }

                // This uses the Glue CollisionPlugin's CollisionType with the following values:
                switch (collisionType)
                {
                    case 0:
                        //NoPhysics = 0,
                        collisionRelationship.SetEventOnlyCollision();
                        desiredRelationshipType = GetStandardCollisionRelationshipType();
                        handled = true;
                        break;
                    case 1:
                        //MoveCollision = 1,
                        collisionRelationship.SetMoveCollision(firstMass, secondMass);
                        desiredRelationshipType = GetStandardCollisionRelationshipType();
                        handled = true;
                        break;
                    case 2:
                        //BounceCollision = 2,
                        collisionRelationship.SetBounceCollision(firstMass, secondMass, elasticity);
                        desiredRelationshipType = GetStandardCollisionRelationshipType();
                        handled = true;
                        break;
                    case 3:
                        //PlatformerSolidCollision = 3,
                    case 4:
                        //PlatformerCloudCollision = 4,

                        if (isFirstList && isSecondList)
                        {
                            desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateListVsListRelationship<,>)
                                .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType().GenericTypeArguments[0]);
                        }
                        else if (isFirstList)
                        {
                            desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<,>)
                                .MakeGenericType(firstObject.GetType().GenericTypeArguments[0], secondObject.GetType());
                        }
                        else if (isSecondList)
                        {
                            desiredRelationshipType = typeof(FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<,>)
                                .MakeGenericType(firstObject.GetType(), secondObject.GetType().GenericTypeArguments[0]);
                        }

                        break;
                    case 5:
                        break;
                }

                var needsToBeRecreated = desiredRelationshipType != collisionRelationship.GetType();

                //var needsToBeRecreated = shouldBeDelegate != isDelegate;

            }

            return handled;
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
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = float.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0f;
                    }
                    break;
                case "int":
                case nameof(Int32):
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = int.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0;
                    }
                    break;
                case "bool":
                case nameof(Boolean):
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = bool.Parse(variableValue.ToLowerInvariant());
                    }
                    else
                    {
                        convertedValue = false;
                    }
                    break;
                case "double":
                case nameof(Double):
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = double.Parse(variableValue);
                    }
                    else
                    {
                        convertedValue = 0.0;
                    }
                    break;
                case "Microsoft.Xna.Framework.Color":
                    if(!string.IsNullOrWhiteSpace(variableValue))
                    {
                        convertedValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(variableValue).GetValue(null);
                    }
                    else
                    {
                        // do we default to white? that's default for shapes
                        convertedValue = Microsoft.Xna.Framework.Color.White;
                    }
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
