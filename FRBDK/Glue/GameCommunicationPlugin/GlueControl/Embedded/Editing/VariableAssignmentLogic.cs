{CompilerDirectives}

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
using GlueControl.Dtos;
using {ProjectNamespace};
using FlatRedBall.Forms.Controls;
using GlueControl.Models;
using System.Runtime.CompilerServices;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Utilities;

namespace GlueControl.Editing
{
    public static class VariableAssignmentLogic
    {
        public static GlueVariableSetDataResponse SetVariable(GlueVariableSetData data, PositionedObject forcedItem = null)
        {
            object variableValue = ConvertVariableValue(data);

            var response = new GlueVariableSetDataResponse();

            try
            {
                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;

                var elementGameType = data.InstanceOwnerGameType;
                var ownerGameType = typeof(VariableAssignmentLogic).Assembly.GetType(data.InstanceOwnerGameType);
                Models.GlueElement ownerElement = null;
                if (InstanceLogic.Self.CustomGlueElements.ContainsKey(elementGameType))
                {
                    ownerElement = InstanceLogic.Self.CustomGlueElements[elementGameType];
                }
                else if (!string.IsNullOrEmpty(data.InstanceOwnerGameType))
                {
                    var glueType = CommandReceiver.GameElementTypeToGlueElement(data.InstanceOwnerGameType);
                    ownerElement = Managers.ObjectFinder.Self.GetElement(glueType);
                }


                var isStatic = false;

                string strippedVariableName = null;

                if (ownerElement != null)
                {
                    // strip off "this"
                    var nameWithoutThis = data.VariableName;
                    if (nameWithoutThis.StartsWith("this."))
                    {
                        nameWithoutThis = nameWithoutThis.Substring("this.".Length);
                    }
                    var startsWith = nameWithoutThis.StartsWith(data.InstanceOwnerGameType);

                    if (startsWith)
                    {
                        //                                                                                   + 1 to handle the '.'
                        strippedVariableName = nameWithoutThis.Substring(data.InstanceOwnerGameType.Length + 1);
                        var customVariable = ownerElement.CustomVariables.Find(item => item.Name == strippedVariableName);

                        isStatic = customVariable?.IsShared == true;
                    }
                }


                if (isStatic)
                {
                    var reflectedElementGameType = typeof(Game1).Assembly.GetType(elementGameType);
                    if (reflectedElementGameType != null)
                    {
                        var property = reflectedElementGameType.GetProperty(strippedVariableName);
                        if (property != null)
                        {
                            property.SetValue(null, variableValue);
                        }
                        else
                        {
                            var field = reflectedElementGameType.GetField(strippedVariableName);
                            field?.SetValue(null, variableValue);
                        }
                    }
                }
                else
                {
                    // The variable could be
                    // * Set on an entity, such as changing the Radius on a collision circle.
                    // * Set on an instance, such as setting the X position of an enemy in a Screen.
                    // If forcedItem is null, that means that we are re-running all variable assignments,
                    // which happens whenever a screen is restarted. 

                    var setOnEntity =
                        (ownerGameType != null && typeof(PositionedObject).IsAssignableFrom(ownerGameType))
                        ||
                        ownerElement is Models.EntitySave;

                    if (setOnEntity)
                    {
                        var variableNameOnObjectInInstance = data.VariableName.Substring("this.".Length);
                        if (forcedItem != null)
                        {
                            if (CommandReceiver.DoTypesMatch(forcedItem, data.InstanceOwnerGameType, ownerGameType))
                            {
                                screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, forcedItem);
                            }
                        }
                        else
                        {
                            var splitVariable = data.VariableName.Split('.');
                            var variableName = splitVariable.Last();

                            // Loop through all objects in the SpriteManager. If we are viewing a single 
                            // entity in the entity screen, then this will only loop 1 time and will set 1 value.
                            // If we are in a screen where multiple instances of the entity are around, then we set the 
                            // value on all instances
                            foreach (var item in SpriteManager.ManagedPositionedObjects)
                            {
                                if (CommandReceiver.DoTypesMatch(item, data.InstanceOwnerGameType, ownerGameType))
                                {
                                    //var targetInstance = GetTargetInstance(data, ref variableValue, screen);
                                    object targetInstance = null;
                                    // If there's 2 variables, then it's like "this.Width"
                                    if (setOnEntity && splitVariable.Length == 2)
                                    {
                                        targetInstance = item;
                                    }
                                    else
                                    {
                                        targetInstance = screen.GetInstance(splitVariable[1] + ".Whatever", item);
                                        if (targetInstance != null && !(targetInstance is INameable))
                                        {
                                            // wrap it
                                            targetInstance = new NameableWrapper() { Name = splitVariable[1], ContainedObject = targetInstance };
                                        }
                                    }

                                    SetValueOnObjectInElement(variableValue, response, screen, splitVariable[1], variableName, targetInstance as INameable);
                                    //SetValueOnObjectInScreen(variableNameOnObjectInInstance, variableValue, item);
                                    //screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, item);
                                }
                            }
                            response.WasVariableAssigned = true;
                        }
                    }
                    // See comment by setOnEntity about why we check for forcedItem.
                    else if (forcedItem == null)
                    {
                        var elementNameGlue = string.Join("\\", data.InstanceOwnerGameType.Split('.').Skip(1).ToArray());
                        if (CommandReceiver.GetIfMatchesCurrentScreen(elementNameGlue))
                        {
                            variableValue = SetValueOnObjectInScreen(data, variableValue, response, screen);
                        }
                        else
                        {
                            // it's not the current screen, so we don't know if it will assign, but we'll tell Glue "yes" so it doesn't restart
                            response.WasVariableAssigned = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                response.Exception = ExceptionWithJson(e, data);

                response.WasVariableAssigned = false;
            }
            return response;
        }

        static string ExceptionWithJson(Exception e, object data)
        {
            return $"{e.ToString()}\n{data?.GetType().Name}:\n{JsonConvert.SerializeObject(data, Formatting.Indented)}";
        }


        private static object SetValueOnObjectInScreen(GlueVariableSetData data, object variableValue, GlueVariableSetDataResponse response, FlatRedBall.Screens.Screen screen)
        {
            response.WasVariableAssigned = false;
            var splitVariable = data.VariableName.Split('.');

            try
            {
                // If this is set on an instance it might be "this.InstanceName.VariableName"
                // If it's set on the screen itself it might be "this.ScreenVariableName"
                // In either case, the variable name is the last
                var variableName = splitVariable.Last();

                var targetInstance = GetTargetInstance(data, ref variableValue, screen);

                SetValueOnObjectInElement(variableValue, response, screen, splitVariable[1], variableName, targetInstance);

            }
            catch (Exception e)
            {
                response.WasVariableAssigned = false;
                response.Exception = ExceptionWithJson(e, data);
            }


            return variableValue;
        }

        private static void SetValueOnObjectInElement(object variableValue, GlueVariableSetDataResponse response, FlatRedBall.Screens.Screen screen, string instanceName, string variableName, INameable targetInstance)
        {
            var shouldSuppressVariable = EditingManager.Self.GetIfShouldSuppressVariableAssignment(variableName, targetInstance);
            ////////////////////////Early Out/////////////////
            if (shouldSuppressVariable)
            {
                return;
            }
            /////////////////////End Early Out////////////////

            var didAttemptToAssign = false;

            #region "Entire CollisionRelationship" on CollisionRelationship

            if (targetInstance is CollisionRelationship && variableName == "Entire CollisionRelationship")
            {
                response.WasVariableAssigned = TryAssignCollisionRelationship(instanceName,
                    JsonConvert.DeserializeObject<Models.NamedObjectSave>(variableValue as string));
                didAttemptToAssign = true;
            }

            #endregion

            #region "Entire TileShapeCollection" on TileShapeCollection 

            if (!didAttemptToAssign && targetInstance is FlatRedBall.TileCollisions.TileShapeCollection && variableName == "Entire TileShapeCollection")
            {
                FlatRedBall.TileCollisions.TileShapeCollection foundTileShapeCollection;
                response.WasVariableAssigned = TryAssignTileShapeCollection(instanceName,
                    JsonConvert.DeserializeObject<Models.NamedObjectSave>(variableValue as string), out foundTileShapeCollection);

                if (response.WasVariableAssigned)
                {
                    var matchingNos = EditingManager.Self.CurrentNamedObjects.FirstOrDefault(item => item.InstanceName == foundTileShapeCollection?.Name);
                    if (matchingNos != null)
                    {
                        // force re-selection to update visibility:
                        EditingManager.Self.Select((NamedObjectSave)null);
                        EditingManager.Self.Select(matchingNos);
                    }
                }
                didAttemptToAssign = true;
            }

            #endregion

            #region IList variables

            if (!didAttemptToAssign && targetInstance is IList)
            {
                didAttemptToAssign = variableName == "SortAxis" ||
                    variableName == "IsSortListEveryFrameChecked" ||
                    variableName == "PartitionWidthHeight";

                response.WasVariableAssigned = didAttemptToAssign;
            }

            #endregion

            #region IncludeInICollidable on Shape

            if (!didAttemptToAssign && variableName == "IncludeInICollidable")
            {
                ShapeCollection shapeCollection = null;
                ICollidable parent = null;
                if (targetInstance is AxisAlignedRectangle rectangle)
                {
                    parent = rectangle.Parent as ICollidable;
                    shapeCollection = parent?.Collision;

                    if (shapeCollection != null)
                    {
                        if (variableValue as bool? == true)
                        {
                            if (shapeCollection.AxisAlignedRectangles.Contains(rectangle) == false)
                            {
                                shapeCollection.Add(rectangle);
                            }
                        }
                        else
                        {
                            if (shapeCollection.AxisAlignedRectangles.Contains(rectangle))
                            {
                                shapeCollection.AxisAlignedRectangles.Remove(rectangle);
                            }
                        }
                    }
                    didAttemptToAssign = true;
                    response.WasVariableAssigned = didAttemptToAssign;
                }
                else if (targetInstance is Circle circle)
                {
                    parent = circle.Parent as ICollidable;
                    shapeCollection = parent?.Collision;
                    if (shapeCollection != null)
                    {
                        if (variableValue as bool? == true)
                        {
                            if (shapeCollection.Circles.Contains(circle) == false)
                            {
                                shapeCollection.Add(circle);
                            }
                        }
                        else
                        {
                            if (shapeCollection.Circles.Contains(circle))
                            {
                                shapeCollection.Circles.Remove(circle);
                            }
                        }
                    }
                    didAttemptToAssign = true;
                    response.WasVariableAssigned = didAttemptToAssign;
                }
                else if (targetInstance is Polygon polygon)
                {
                    parent = polygon.Parent as ICollidable;
                    shapeCollection = parent?.Collision;
                    if (shapeCollection != null)
                    {
                        if (variableValue as bool? == true)
                        {
                            if (shapeCollection.Polygons.Contains(polygon) == false)
                            {
                                shapeCollection.Add(polygon);
                            }
                        }
                        else
                        {
                            if (shapeCollection.Polygons.Contains(polygon))
                            {
                                shapeCollection.Polygons.Remove(polygon);
                            }
                        }
                    }
                    didAttemptToAssign = true;
                    response.WasVariableAssigned = didAttemptToAssign;
                }
            }

            #endregion

            #region Polygon.Points

            if (!didAttemptToAssign && variableName == "Points" && targetInstance is Polygon targetPolygon)
            {
                List<Point> points = variableValue as List<Point>;
                if (variableValue is List<Microsoft.Xna.Framework.Vector2> asVectors)
                {
                    points = asVectors.Select(item => new Point(item.X, item.Y)).ToList();

                }
                if (points != null)
                {
                    targetPolygon.Points = points;
                    didAttemptToAssign = true;
                    response.WasVariableAssigned = didAttemptToAssign;
                }
            }

            #endregion

            #region Path.Path

            if (!didAttemptToAssign && variableName == "Path" && targetInstance is FlatRedBall.Math.Paths.Path asPath)
            {
                asPath.FromJson(variableValue as string);
                didAttemptToAssign = true;
                response.WasVariableAssigned = true;
            }

            #endregion

            if (!didAttemptToAssign)
            {
                targetInstance = targetInstance ?? screen.GetInstanceRecursive(variableName) as INameable;
                if (targetInstance == null)
                {
                    response.WasVariableAssigned = screen.ApplyVariable(variableName, variableValue);
                }
                else
                {
                    variableName = TryConvertVariableNameToExposedVariableName(variableName, targetInstance);

                    object effectiveTarget = targetInstance;
                    if (targetInstance is NameableWrapper nameableWrapper)
                    {
                        effectiveTarget = nameableWrapper.ContainedObject;
                    }

                    response.WasVariableAssigned = screen.ApplyVariable(variableName, variableValue, effectiveTarget);
                }

                if (response.WasVariableAssigned && targetInstance is PositionedObject targetAsPositionedObject)
                {
                    // make sure we 0-out velocity or acceleration, which could get set as a consequence of setting a variable (such as setting the movement values on a platformer)
                    targetAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    targetAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                }

                didAttemptToAssign = true;
            }
        }

        private static string TryConvertVariableNameToExposedVariableName(string variableName, INameable targetInstance)
        {
            var targetInstanceType = targetInstance.GetType().FullName;
            if (InstanceLogic.Self.CustomVariablesAddedAtRuntime.ContainsKey(targetInstanceType))
            {
                var variablesForThisType = InstanceLogic.Self.CustomVariablesAddedAtRuntime[targetInstanceType];

                var customVariable = variablesForThisType.FirstOrDefault(item => item.Name == variableName);

                if (customVariable != null)
                {
                    variableName = customVariable.SourceObject + "." + customVariable.SourceObjectProperty;
                }
            }

            return variableName;
        }

        private static FlatRedBall.Utilities.INameable GetTargetInstance(GlueVariableSetData data, ref object variableValue, FlatRedBall.Screens.Screen screen)
        {
            var splitVariable = data.VariableName.Split('.');

            FlatRedBall.Utilities.INameable targetInstance = null;
            // this searches for a name. we need to force it on a type too so newly-added objects can have their variables set....

            // Needs to be greater than 2.
            // If it's "this.SomeVariable" then there's no instance
            // If it's "this.InstanceName.SomeInstanceVariable" then it's > 2 and there is an instance
            if (splitVariable[0] == "this" && splitVariable.Length > 2)
            {
                // If it's "this.InstanceName.SomeVariable" then we just grab the middle "InstanceName"
                // However, it's possible that Glue sends thigns like "this.InstanceName.SubInstance.VariableName", so 
                // we need to pass in "InstanceName.SubInstance"
                var middleVars = splitVariable.Skip(1).Take(splitVariable.Length - 2).ToArray();
                var middleVarsAsString = string.Join(".", middleVars);
                targetInstance = GetRuntimeInstanceRecursively(screen, middleVarsAsString);
            }

            if (targetInstance != null && splitVariable[2] == "Points" && variableValue is List<Microsoft.Xna.Framework.Vector2> vectorList)
            {
                variableValue = vectorList.Select(item => new FlatRedBall.Math.Geometry.Point(item.X, item.Y)).ToList();
            }

            return targetInstance;
        }

        private static FlatRedBall.Utilities.INameable GetRuntimeInstanceRecursively(FlatRedBall.Screens.Screen screen, string objectAndSubObjects, object owner = null)
        {
            if (objectAndSubObjects.Contains(".") == false)
            {
                // no recurisve search necessary:
                return GetRuntimeInstance(screen, objectAndSubObjects, owner);
            }
            else
            {
                var directInstanceName = objectAndSubObjects.Substring(0, objectAndSubObjects.IndexOf("."));
                var remainder = objectAndSubObjects.Substring(directInstanceName.Length + 1);

                var foundInstance = GetRuntimeInstance(screen, directInstanceName, owner);

                return GetRuntimeInstanceRecursively(screen, remainder, foundInstance);
            }
        }

        private static FlatRedBall.Utilities.INameable GetRuntimeInstance(FlatRedBall.Screens.Screen screen, string objectName, object owner = null)
        {
            FlatRedBall.Utilities.INameable targetInstance = null;

            if (owner is PositionedObject ownerAsPositionedObject)
            {
                targetInstance = ownerAsPositionedObject.Children.FindByName(objectName);
            }

            // This is the most likely to find (and end all other checks) so let's do that first.
            if (targetInstance == null)
            {
                targetInstance = SpriteManager.ManagedPositionedObjects.FirstOrDefault(item =>
                    item.Parent == null && item.Name == objectName);

            }

            if (targetInstance == null)
            {
                targetInstance = ShapeManager.VisibleRectangles.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == objectName);
            }


            if (targetInstance == null)
            {
                targetInstance = ShapeManager.VisibleCircles.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == objectName);
            }

            if (targetInstance == null)
            {
                targetInstance = ShapeManager.VisiblePolygons.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == objectName);
            }

            if (targetInstance == null)
            {
                targetInstance = SpriteManager.AutomaticallyUpdatedSprites.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == objectName);
            }

            if (targetInstance == null)
            {
                targetInstance = FlatRedBall.Graphics.TextManager.AutomaticallyUpdatedTexts.FirstOrDefault(item =>
                    item.Parent == null &&
                    item.Name == objectName);
            }

            if (targetInstance == null)
            {
                targetInstance = CollisionManager.Self.Relationships.FirstOrDefault(item =>
                    item.Name == objectName);
            }

            if (targetInstance == null)
            {
                targetInstance = InstanceLogic.Self.ListsAddedAtRuntime.FirstOrDefault(item =>
                    (item as FlatRedBall.Utilities.INameable).Name == objectName) as INameable;
            }

            if (targetInstance == null)
            {
                targetInstance = InstanceLogic.Self.ShapeCollectionsAddedAtRuntime.FirstOrDefault(item =>
                    item.Name == objectName);
            }

            // This was originally how we got instances, but this adds a ton of overhead when dealing with screens
            // which have lots of variable assignments. Instead, we just rely on the calls above and it makes performance
            // way better. Specifically, with this code in, large copy paste blocks make the app unusable.
            // Update - but there are things like Paths which do not exist in any manager, but which may be edited, so we 
            // still need to have this in. Maybe we can make copy/paste faster some other way without removing this....
            if (targetInstance == null)
            {
                object foundObject;
                screen.GetInstance(objectName, screen, out _, out foundObject);
                targetInstance = foundObject as INameable;
            }

            if (targetInstance == null)
            {
                var screenType = screen.GetType();
                while (targetInstance == null && screenType != null)
                {
                    var field = screenType.GetField(objectName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);
                    var foundObject = field?.GetValue(screen);
                    targetInstance = foundObject as INameable;

                    if (targetInstance == null)
                    {
                        screenType = screenType.BaseType;
                    }
                }
            }

            return targetInstance;
        }

        private static bool TryAssignCollisionRelationship(string relationshipName, Models.NamedObjectSave namedObject)
        {
            var handled = false;

            var collisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(item => item.Name == relationshipName);

            if (collisionRelationship != null)
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

                firstObject = GetRuntimeInstance(currentScreen, firstObjectName);
                secondObject = GetRuntimeInstance(currentScreen, secondObjectName);

                var isFirstList = firstObject is IList;
                var isSecondList = secondObject is IList;
                var isSecondShapeCollection = secondObject is ShapeCollection;

                var firstSubCollision = Get<string>("FirstSubCollisionSelectedItem");

                if (firstSubCollision == "<Entire Object>")
                {
                    firstSubCollision = null;
                }

                var secondSubCollision = Get<string>("SecondSubCollisionSelectedItem");

                if (secondSubCollision == "<Entire Object>")
                {
                    secondSubCollision = null;
                }

                var groupPlatformerVariableName = Get<string>("GroundPlatformerVariableName");
                var airPlatformerVariableName = Get<string>("AirPlatformerVariableName");
                var afterDoubleJumpPlatformerVariableName = Get<string>("AfterDoubleJumpPlatformerVariableName");


                var existingRelationshipTypeName = collisionRelationship.GetType().FullName;

                Type desiredRelationshipType = GetDesiredRelationshipType(namedObject);

                // This uses the Glue CollisionPlugin's CollisionType with the following values:
                switch (collisionType)
                {
                    case 0:
                        //NoPhysics = 0,
                        collisionRelationship.SetEventOnlyCollision();
                        handled = true;
                        break;
                    case 1:
                        //MoveCollision = 1,
                        collisionRelationship.SetMoveCollision(firstMass, secondMass);
                        handled = true;
                        break;
                    case 2:
                        //BounceCollision = 2,
                        collisionRelationship.SetBounceCollision(firstMass, secondMass, elasticity);
                        handled = true;
                        break;
                    case 3:
                        //PlatformerSolidCollision = 3,
                        // assume yes, will be no'd later
                        handled = true;
                        break;
                    case 4:
                        //PlatformerCloudCollision = 4,
                        handled = true;
                        break;
                    case 5:
                        break;
                }

                var doFirstSubsMatch =
                    firstSubCollision == collisionRelationship.FirstSubObjectName;
                var doSecondSubsMatch =
                    secondSubCollision == collisionRelationship.SecondSubObjectName;
                var doesFirstMatch =
                    firstObject == collisionRelationship.FirstAsObject;
                var doesSecondMatch =
                    secondObject == collisionRelationship.SecondAsObject;

                if (doFirstSubsMatch == false)
                {
                    handled = false;
                }
                else if (doSecondSubsMatch == false)
                {
                    handled = false;
                }

                if (doesFirstMatch == false)
                {
                    handled = false;
                }
                if (doesSecondMatch == false)
                {
                    handled = false;
                }

                var currentRelationshipType =
                    collisionRelationship.GetType();
                var needsToBeRecreated = desiredRelationshipType != currentRelationshipType;
                if (needsToBeRecreated)
                {
                    handled = false;
                }

                var needsDelegate = currentRelationshipType.Name.StartsWith("Delegate");
                if (needsDelegate)
                {
                    var hasDelegate = currentRelationshipType.GetField("CollisionFunction")
                        ?.GetValue(collisionRelationship) != null;

                    if (!hasDelegate)
                    {
                        handled = false;
                    }
                }

            }

            return handled;
        }

        private static bool TryAssignTileShapeCollection(string tileShapeCollectionName, Models.NamedObjectSave namedObject, out FlatRedBall.TileCollisions.TileShapeCollection foundTileShapeCollection)
        {
            var handled = false;

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            screen.GetInstance(namedObject.InstanceName, screen, out _, out object tileShapeCollectionAsObject);

            var tileShapeCollection = tileShapeCollectionAsObject as FlatRedBall.TileCollisions.TileShapeCollection;
            foundTileShapeCollection = tileShapeCollection;
            if (tileShapeCollection != null)
            {
                T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);
                void ClearShapeCollection()
                {
                    tileShapeCollection.Visible = false;
                    // What if this was added to the ShapeManager? New versions of generated code don't,
                    // so do we need to bother removing from ShapeManager?
                    tileShapeCollection.Rectangles.Clear();
                }

                var creationOptions = Get<int>("CollisionCreationOptions");

                var isVisible = (namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "Visible")?.Value as bool?) == true;

                var tileSize = Get<float>("CollisionTileSize");

                var leftFill = Get<float>("CollisionFillLeft");
                var topFill = Get<float>("CollisionFillTop");

                var remainderX = leftFill % tileSize;
                var remainderY = topFill % tileSize;

                var widthFill = Get<int>("CollisionFillWidth");
                var heightFill = Get<int>("CollisionFillHeight");

                switch (creationOptions)
                {
                    case 0: // Empty
                        ClearShapeCollection();
                        handled = true;
                        break;
                    case 1: // FillCompletely
                        ClearShapeCollection();

                        tileShapeCollection.GridSize = tileSize;
                        tileShapeCollection.LeftSeedX = remainderX;
                        tileShapeCollection.BottomSeedY = remainderY;
                        tileShapeCollection.SortAxis = FlatRedBall.Math.Axis.X;

                        for (int x = 0; x < widthFill; x++)
                        {
                            for (int y = 0; y < heightFill; y++)
                            {
                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - y * tileSize - tileSize / 2.0f);
                            }
                        }
                        if (isVisible)
                        {
                            tileShapeCollection.Visible = true;
                        }
                        handled = true;
                        break;
                    case 2: // BorderOutline
                        ClearShapeCollection();

                        tileShapeCollection.GridSize = tileSize;
                        tileShapeCollection.LeftSeedX = remainderX;
                        tileShapeCollection.BottomSeedY = remainderY;
                        tileShapeCollection.SortAxis = FlatRedBall.Math.Axis.X;

                        var borderOutlineType = Get<int>("BorderOutlineType");


                        if (borderOutlineType == 1 /*BorderOutlineType.InnerSize*/)
                        {
                            var innerWidth = Get<float>("InnerSizeWidth");

                            var innerHeight = Get<float>("InnerSizeHeight");

                            var additionalWidth = 2 * tileSize;
                            var additionalHeight = 2 * tileSize;

                            widthFill = FlatRedBall.Math.MathFunctions.RoundToInt(
                                (innerWidth + additionalWidth) / tileSize);
                            heightFill = FlatRedBall.Math.MathFunctions.RoundToInt(
                                (innerHeight + additionalHeight) / tileSize);


                        }

                        for (int x = 0; x < widthFill; x++)
                        {
                            if (x == 0 || x == widthFill - 1)
                            {
                                for (int y = 0; y < heightFill; y++)
                                {
                                    tileShapeCollection.AddCollisionAtWorld(
                                        leftFill + x * tileSize + tileSize / 2.0f,
                                        topFill - y * tileSize - tileSize / 2.0f);

                                }
                            }
                            else
                            {
                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - tileSize / 2.0f);

                                tileShapeCollection.AddCollisionAtWorld(
                                    leftFill + x * tileSize + tileSize / 2.0f,
                                    topFill - (heightFill - 1) * tileSize - tileSize / 2.0f);
                            }
                        }

                        if (isVisible)
                        {
                            tileShapeCollection.Visible = true;
                        }
                        handled = true;

                        break;
                    case 4: // FromType

                        ClearShapeCollection();

                        var mapName = Get<string>("SourceTmxName");
                        var typeName = Get<string>("CollisionTileTypeName");
                        var removeTiles = Get<bool>("RemoveTilesAfterCreatingCollision");
                        var isMerged = Get<bool>("IsCollisionMerged");
                        if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(typeName))
                        {
                            var map = screen.GetType().GetMethod("GetFile").Invoke(null, new object[] { mapName }) as
                                FlatRedBall.TileGraphics.LayeredTileMap;

                            if (map == null)
                            {
                                var mapAsObject = FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(screen, mapName);
                                map = mapAsObject as FlatRedBall.TileGraphics.LayeredTileMap;
                            }

                            if (map != null)
                            {
                                if (isMerged)
                                {
                                    FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddMergedCollisionFromTilesWithType(
                                        tileShapeCollection, map, typeName);
                                }
                                else
                                {
                                    FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFromTilesWithType(
                                        tileShapeCollection, map, typeName, removeTiles);
                                }
                                if (isVisible)
                                {
                                    tileShapeCollection.Visible = true;
                                }

                            }
                        }

                        handled = true;

                        break;
                }
            }

            return handled;
        }

        public static Type GetDesiredRelationshipType(Models.NamedObjectSave namedObject)
        {
            return GetDesiredRelationshipType(namedObject, out _, out _);
        }

        public static Type GetDesiredRelationshipType(Models.NamedObjectSave namedObject, out object firstObject, out object secondObject)
        {
            T Get<T>(string name) => GlueControl.Models.PropertySaveListExtensions.GetValue<T>(namedObject.Properties, name);
            var collisionType = Get<int>("CollisionType");

            var firstObjectName = Get<string>("FirstCollisionName");
            var secondObjectName = Get<string>("SecondCollisionName");

            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;

            firstObject = GetRuntimeInstance(currentScreen, firstObjectName);
            secondObject = GetRuntimeInstance(currentScreen, secondObjectName);

            var isFirstList = firstObject is IList;
            var isSecondList = secondObject is IList;
            var isSecondShapeCollection = secondObject is ShapeCollection;
            var isSecondTileShapeCollection = secondObject is FlatRedBall.TileCollisions.TileShapeCollection;

            Type desiredRelationshipType = null;

            var firstType = firstObject?.GetType();
            var secondType = secondObject?.GetType();

            Type GetStandardCollisionRelationshipType()
            {
                if (isFirstList && isSecondList)
                {
                    return typeof(ListVsListRelationship<,>)
                        .MakeGenericType(firstType.GenericTypeArguments[0], secondType.GenericTypeArguments[0]);
                }
                else if (isFirstList && isSecondShapeCollection)
                {
                    return typeof(ListVsShapeCollectionRelationship<>)
                        .MakeGenericType(firstType.GenericTypeArguments[0]);
                }
                else if (isFirstList && isSecondTileShapeCollection)
                {
                    return typeof(CollidableListVsTileShapeCollectionRelationship<>)
                        .MakeGenericType(firstType.GenericTypeArguments[0]);
                }
                else if (isFirstList)
                {
                    return typeof(ListVsPositionedObjectRelationship<,>)
                        .MakeGenericType(firstType.GenericTypeArguments[0], secondType);
                }
                else if (isSecondList)
                {
                    return typeof(PositionedObjectVsListRelationship<,>)
                        .MakeGenericType(firstType, secondType.GenericTypeArguments[0]);
                }
                else if (isSecondShapeCollection)
                {
                    return typeof(PositionedObjectVsShapeCollection<>)
                        .MakeGenericType(firstType);
                }
                else if (isSecondTileShapeCollection)
                {
                    if (isFirstList)
                    {
                        return typeof(CollidableVsTileShapeCollectionRelationship<>)
                            .MakeGenericType(firstType.GenericTypeArguments[0]);
                    }
                    else
                    {
                        return typeof(CollidableVsTileShapeCollectionRelationship<>)
                            .MakeGenericType(firstType);
                    }
                }
                else
                {
                    return typeof(PositionedObjectVsPositionedObjectRelationship<,>)
                        .MakeGenericType(firstType, secondType);
                }
            }

            // Get the type here:
            switch (collisionType)
            {
                case 0:
                //NoPhysics = 0,
                case 1:
                //MoveCollision = 1,
                case 2:
                    //BounceCollision = 2,
                    desiredRelationshipType = GetStandardCollisionRelationshipType();
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

            return desiredRelationshipType;
        }

        private static object ConvertVariableValue(GlueVariableSetData data)
        {
            var type = data.Type;
            object variableValue = ConvertStringToType(type, data.VariableValue, data.IsState, out string _);

            return variableValue;
        }

        static object GetFileFromUnqualifiedName(string unqualifiedName)
        {
            var file = GlobalContent.GetFile(unqualifiedName);

            if (file == null && FlatRedBall.Screens.ScreenManager.CurrentScreen != null)
            {
                var getFileMethod = FlatRedBall.Screens.ScreenManager.CurrentScreen.GetType().GetMethod("GetFile");


                if (getFileMethod != null)
                {
                    file = getFileMethod.Invoke(null, new object[] { unqualifiedName });
                }
            }

            // Or is it in the entity being viewed?
            if (file == null && FlatRedBall.Screens.ScreenManager.CurrentScreen is Screens.EntityViewingScreen entityViewingScreen)
            {
                var entity = entityViewingScreen.CurrentEntity;

                var getFileMethod = entity?.GetType().GetMethod("GetFile");

                if (getFileMethod != null)
                {
                    file = getFileMethod.Invoke(null, new object[] { unqualifiedName });
                }
            }

            return file;
        }

        public static object ConvertStringToType(string type, string variableValue, bool isState, out string conversionReport, bool convertFileNamesToObjects = true)
        {
            object convertedValue = variableValue;
            const string inWithSpaces = " in ";
            conversionReport = $"Attempting to convert string varaible to {type}, but was not able to handle the conversion";
            if (isState)
            {
                convertedValue = TryGetStateValue(type, variableValue);
            }
            else if (type == typeof(List<Microsoft.Xna.Framework.Vector2>).ToString() || type == "List<Vector2>")
            {
                convertedValue = JsonConvert.DeserializeObject<List<Microsoft.Xna.Framework.Vector2>>(variableValue);
            }
            else if (type == typeof(List<Point>).ToString() || type == "List<Point>")
            {
                convertedValue = JsonConvert.DeserializeObject<List<Point>>(variableValue);
            }
            else if (type == typeof(FlatRedBall.Graphics.BitmapFont).ToString() || type == "BitmapFont")
            {
                if (variableValue == null)
                {
                    convertedValue = FlatRedBall.Graphics.TextManager.DefaultFont;
                }
            }
            else if (variableValue?.Contains(inWithSpaces) == true)
            {
                conversionReport = $"Attempting to convert string to CSV type {type}";
                // It could be a CSV:
                var indexOfIn = variableValue.IndexOf(inWithSpaces);

                var startOfCsvName = indexOfIn + inWithSpaces.Length;

                var csvName = variableValue.Substring(startOfCsvName).Split('.')[0];

                var file = GetFileFromUnqualifiedName(csvName);

                if (file is IDictionary asDictionary)
                {
                    conversionReport += $"\nFound dictionary from {csvName}";
                    var itemInCsv = variableValue.Substring(0, indexOfIn);

                    if (asDictionary.Contains(itemInCsv))
                    {
                        convertedValue = asDictionary[itemInCsv];
                        conversionReport += $"\nFound entry in dictionary {itemInCsv}";
                    }
                    else
                    {
                        conversionReport += $"\nCould not find entry in dictionary {itemInCsv}";
                    }
                }
                else
                {
                    conversionReport += $"\nCould not find dictionary from name {csvName}";

                }

                if (convertedValue is string)
                {
                    conversionReport += $"\nConverted value is still string, was not able to convert";

                    // If we got here and it's a string, that's bad, so let's just set it to null
                    convertedValue = null;
                }
                else
                {
                    conversionReport += $"\nConverted value to {convertedValue?.GetType()}";
                }
            }
            else
            {
                switch (type)
                {
                    case "float":
                    case nameof(Single):
                    case "System.Single":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = float.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0f;
                        }
                        break;
                    case "float?":
                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = float.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = (float?)null;
                        }
                        break;

                    case "int":
                    case nameof(Int32):
                    case "System.Int32":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = int.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0;
                        }
                        break;

                    case "int?":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = int.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = (int?)null;
                        }

                        break;

                    case "bool":
                    case nameof(Boolean):
                    case "System.Boolean":

                        if (!string.IsNullOrWhiteSpace(variableValue))
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
                    case "System.Double":

                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = double.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0.0;
                        }
                        break;
                    case "decimal":
                    case nameof(Decimal):
                    case "System.Decimal":
                        if (!string.IsNullOrWhiteSpace(variableValue))
                        {
                            convertedValue = decimal.Parse(variableValue);
                        }
                        else
                        {
                            convertedValue = 0m;
                        }
                        break;
                    case "Microsoft.Xna.Framework.Color":
                    case nameof(Microsoft.Xna.Framework.Color):
                        if (!string.IsNullOrWhiteSpace(variableValue))
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
                        if (convertFileNamesToObjects)
                        {
                            if (!string.IsNullOrWhiteSpace(variableValue))
                            {
                                convertedValue = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(
                                    variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                            }
                            else
                            {
                                convertedValue = (Microsoft.Xna.Framework.Graphics.Texture2D)null;
                            }
                        }
                        break;
                    case "FlatRedBall.Graphics.Animation.AnimationChainList":
                    case "AnimationChainList":
                        if (convertFileNamesToObjects)
                        {
                            if (!string.IsNullOrWhiteSpace(variableValue))
                            {
                                // try unqualified first:
                                convertedValue = GetFileFromUnqualifiedName(variableValue);
                                if (convertedValue == null)
                                {
                                    convertedValue = FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>(
                                        variableValue, FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName);
                                }
                            }
                            else
                            {
                                convertedValue = (FlatRedBall.Graphics.Animation.AnimationChainList)null;
                            }
                        }
                        break;
                    case nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode):
                    case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                        convertedValue = ToEnum<Microsoft.Xna.Framework.Graphics.TextureAddressMode>(variableValue);
                        break;
                    case nameof(FlatRedBall.Graphics.ColorOperation):
                    case "FlatRedBall.Graphics.ColorOperation":
                        convertedValue = ToEnum<FlatRedBall.Graphics.ColorOperation>(variableValue);

                        break;
                    case nameof(FlatRedBall.Graphics.BlendOperation):
                    case "FlatRedBall.Graphics.BlendOperation":
                        convertedValue = ToEnum<FlatRedBall.Graphics.BlendOperation>(variableValue);

                        break;
                    case "HorizontalAlignment":
                        // assume FRB horizontal alignment. At some point we may have to differentiate:
                        convertedValue = ToEnum<FlatRedBall.Graphics.HorizontalAlignment>(variableValue);
                        break;
                    case "VerticalAlignment":
                        // assume FRB horizontal alignment. At some point we may have to differentiate:
                        convertedValue = ToEnum<FlatRedBall.Graphics.VerticalAlignment>(variableValue);
                        break;

                }
                T ToEnum<T>(string asString)
                {
                    if (int.TryParse(variableValue, out int parsedInt))
                    {
                        return (T)(object)parsedInt;
                    }
                    return default(T);
                }
            }

            return convertedValue;
        }


        private static object TryGetStateValue(string type, string variableValue)
        {
            Type stateType = TryGetStateType(type);

            var dictionary = stateType?.GetField("AllStates").GetValue(null) as IDictionary;

            if (dictionary != null && variableValue != null && dictionary.Contains(variableValue))
            {
                return dictionary[variableValue];
            }
            else
            {
                return null;
            }
        }


        public static Type TryGetStateType(string qualifiedTypeName)
        {
            // Note about fully-qualified state names
            // This code was originally written to support
            // only fully-qualified names. This was causing
            // errors because state types were not being recognized
            // as fully qualified. Vic thought - okay, why not make this
            // tolerate unqualified types? That seems like a good idea, right?
            // NOPE! Because state types can be used across entities, and if they
            // are not fully qualified, then setting one state may be mistakenly set
            // to another state, causing a crash. Instead, we must make sure that states
            // are always fully qualified. They must be fully qualified before reaching this
            // method, and this method should never tolerate unqualified type names.

            ///////// Early Out/////////////
            if (!qualifiedTypeName.Contains('.'))
            {
                return null;
            }
            ////////End Early Out/////////////

            var splitType = qualifiedTypeName.Split('.');

            qualifiedTypeName = string.Join(".", splitType.Take(splitType.Length - 1).ToArray()) + "+" +
                splitType.Last();

            var gameType = typeof(Game1).FullName.Split('.');
            if (splitType[0] != gameType[0])
            {
                // The qualifiedTypeName could be in one of two formats.
                // One is the Type.FullName which would appear as follows:
                // GameRootNamespace.Entities.EntityName.StateName
                // The Type.FullName variety is used when Glue prepends the 
                // namespace.
                // However, when we copy/paste in game, the NamedObject types
                // get copied and the NamedObject doesn't contain that, so we 
                // should tolerate that too. Infact, I question whether we even
                // need the root namespace prefix. By supporting both we can incrementally
                // refactor if that becomes the preferred way to do it.
                qualifiedTypeName = gameType[0] + '.' + qualifiedTypeName;
            }

            var stateType = typeof(VariableAssignmentLogic).Assembly.GetType(qualifiedTypeName);
            return stateType;
        }
    }
}
