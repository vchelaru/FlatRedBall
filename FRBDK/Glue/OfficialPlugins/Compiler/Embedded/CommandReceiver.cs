#define SupportsEditMode
#define IncludeSetVariable
using EditModeProject.GlueControl.Dtos;
using EditModeProject.GlueControl.Editing;
using Microsoft.Xna.Framework;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditModeProject.GlueControl
{
    static class CommandReceiver
    {
        #region Supporting Methods/Properties

        static System.Reflection.MethodInfo[] AllMethods;

        static CommandReceiver()
        {
            AllMethods = typeof(CommandReceiver).GetMethods(
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic)
                .ToArray();
        }

        public static string Receive(string message)
        {
            string dtoTypeName = null;
            string dtoSerialized = null;
            if (message.Contains(":"))
            {
                dtoSerialized = message.Substring(message.IndexOf(":") + 1);
                dtoTypeName = message.Substring(0, message.IndexOf(":"));
            }
            else
            {
                throw new Exception();
            }

            var matchingMethod =
                AllMethods
                .FirstOrDefault(item =>
                {
                    if (item.Name == nameof(HandleDto))
                    {
                        var parameters = item.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.Name == dtoTypeName;
                    }
                    return false;
                });

            if(matchingMethod == null)
            {
                throw new InvalidOperationException(
                    $"Could not find a HandleDto method for type {dtoTypeName}");
            }

            var dtoType = matchingMethod.GetParameters()[0].ParameterType;

            var dto = JsonConvert.DeserializeObject(dtoSerialized, dtoType);

            var response = ReceiveDto(dto);

            if (response != null)
            {
                return JsonConvert.SerializeObject(response);
            }
            else
            {
                return null;
            }
        }

        public static object ReceiveDto(object dto)
        {
            var type = dto.GetType();

            var method = AllMethods
                .FirstOrDefault(item =>
                {
                    if (item.Name == nameof(HandleDto))
                    {
                        var parameters = item.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == type;
                    }
                    return false;
                });


            object toReturn = null;

            if (method != null)
            {
                toReturn = method.Invoke(null, new object[] { dto });
            }

            return toReturn;
        }

        private static bool GetIfMatchesCurrentScreen(string elementName, out System.Type ownerType, out Screen currentScreen)
        {
            var ownerTypeName = "EditModeProject." + elementName.Replace("\\", ".");

            ownerType = typeof(CommandReceiver).Assembly.GetType(ownerTypeName);
            currentScreen = ScreenManager.CurrentScreen;
            var currentScreenType = currentScreen.GetType();

            return currentScreenType == ownerType || ownerType?.IsAssignableFrom(currentScreenType) == true;
        }

        static Dictionary<string, Vector3> CameraPositions = new Dictionary<string, Vector3>();

        #endregion

        private static GlueVariableSetDataResponse HandleDto(GlueVariableSetData dto)
        {
            GlueVariableSetDataResponse valueToReturn = null;
#if IncludeSetVariable

            valueToReturn = GlueControl.Editing.VariableAssignmentLogic.SetVariable(dto);
#endif

            return valueToReturn;
        }

        private static void HandleDto(SelectObjectDto selectObjectDto)
        {
            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(selectObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);

            var elementNameGlue = selectObjectDto.ElementName;
            string ownerTypeName = GlueToGameElementName(elementNameGlue);
            ownerType = typeof(CommandReceiver).Assembly.GetType(ownerTypeName);

            bool isOwnerScreen = false;

            if (matchesCurrentScreen)
            {
                Editing.EditingManager.Self.Select(selectObjectDto.ObjectName);
                Editing.EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
                isOwnerScreen = true;
            }
            else
            {
                // it's a different screen. See if we can select that screen:
                CameraPositions[currentScreen.GetType().FullName] = Camera.Main.Position;

                if (ownerType != null && typeof(Screen).IsAssignableFrom(ownerType))
                {
#if SupportsEditMode

                    void AssignSelection(Screen screen)
                    {
                        // Select this even if it's null so the EditingManager deselects 
                        EditingManager.Self.Select(selectObjectDto.ObjectName);
                        GlueControlManager.Self.ReRunAllGlueToGameCommands();
                        screen.ScreenDestroy += HandleScreenDestroy;
                        if (CameraPositions.ContainsKey(screen.GetType().FullName))
                        {
                            Camera.Main.Position = CameraPositions[screen.GetType().FullName];
                        }
                        ScreenManager.ScreenLoaded -= AssignSelection;
                    }
                    ScreenManager.ScreenLoaded += AssignSelection;

                    ScreenManager.CurrentScreen.MoveToScreen(ownerType);

                    isOwnerScreen = true;
                    EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingScreen;
#endif
                }
            }

            if (!isOwnerScreen)
            {
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType) ||
                    InstanceLogic.Self.CustomGlueElements.ContainsKey(ownerTypeName);

                if (isEntity)
                {
                    var isAlreadyViewingThisEntity = ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen" &&
                        SpriteManager.ManagedPositionedObjects.Count > 0 &&
                        SpriteManager.ManagedPositionedObjects[0].GetType() == ownerType;

                    if (!isAlreadyViewingThisEntity)
                    {
#if SupportsEditMode

                        void CreateEntityInstance(Screen screen)
                        {
                            //var instance = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]) as IDestroyable;
                            var instance = InstanceLogic.Self.CreateEntity(elementNameGlue) as IDestroyable;
                            (screen as Screens.EntityViewingScreen).CurrentEntity = instance;
                            var instanceAsPositionedObject = (PositionedObject)instance;
                            instanceAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                            instanceAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                            ScreenManager.ScreenLoaded -= CreateEntityInstance;

                            EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingEntity;

                            Camera.Main.X = 0;
                            Camera.Main.Y = 0;
                            Camera.Main.Detach();

                            GlueControlManager.Self.ReRunAllGlueToGameCommands();
                            screen.ScreenDestroy += HandleScreenDestroy;

                            EditingManager.Self.Select(selectObjectDto.ObjectName);
                        }
                        ScreenManager.ScreenLoaded += CreateEntityInstance;

                        ScreenManager.CurrentScreen.MoveToScreen(typeof(Screens.EntityViewingScreen));
#endif
                    }
                    else
                    {
                        EditingManager.Self.Select(selectObjectDto.ObjectName);
                    }
                }
            }
        }

        public static string GlueToGameElementName(string elementName)
        {
            return "EditModeProject." + elementName.Replace("\\", ".");
        }

        private static void HandleScreenDestroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();
        }

        private static AddObjectDtoResponse HandleDto(AddObjectDto dto)
        {
            AddObjectDtoResponse valueToReturn = new AddObjectDtoResponse();
#if IncludeSetVariable

            var createdObject = GlueControl.InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(dto);
            valueToReturn.WasObjectCreated = createdObject != null;
#endif
            return valueToReturn;
        }

        private static void HandleDto(SetEditMode setEditMode)
        {
            var value = setEditMode.IsInEditMode;
#if SupportsEditMode
            FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
            FlatRedBall.Gui.GuiManager.Cursor.RequiresGameWindowInFocus = !value;

            if (value)
            {
                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;
                // user may go into edit mode after moving through a level and wouldn't want it to restart fully....or would they? What if they
                // want to change the Player start location. Need to think that through...

                void HandleScreenLoaded(Screen newScreen)
                {
                    GlueControlManager.Self.ReRunAllGlueToGameCommands();
                    newScreen.ScreenDestroy += HandleScreenDestroy;

                    FlatRedBall.Screens.ScreenManager.ScreenLoaded -= HandleScreenLoaded;
                }

                FlatRedBall.Screens.ScreenManager.ScreenLoaded += HandleScreenLoaded;

                screen?.RestartScreen(reloadContent: true, applyRestartVariables: true);
            }
#endif
        }

        private static void HandleDto(SetCameraPositionDto dto)
        {
            Camera.Main.Position = dto.Position;
        }

        private static MoveObjectToContainerDtoResponse HandleDto(MoveObjectToContainerDto dto)
        {
            var toReturn = new MoveObjectToContainerDtoResponse();

            var matchesCurrentScreen = GetIfMatchesCurrentScreen(
                dto.ElementName, out System.Type ownerType, out Screen currentScreen);
            if (matchesCurrentScreen)
            {
                toReturn.WasObjectMoved = GlueControl.Editing.MoveObjectToContainerLogic.TryMoveObjectToContainer(
                    dto.ObjectName, dto.ContainerName, EditingManager.Self.ElementEditingMode);
            }
            else
            {
                // we don't know if it can be moved. We'll assume it can, and when that screen is loaded, it will re-run that and...if it 
                // fails, then I guess we'll figure out a way to communicate back to Glue that it needs to restart. Actually this may never
                // happen because moving objects is done in the current screen, but I gues it's technically a possibility so I'll leave this
                // comment here.
            }

            return toReturn;
        }

        private static RemoveObjectDtoResponse HandleDto(RemoveObjectDto removeObjectDto)
        {
            RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();
            response.DidScreenMatch = false;
            response.WasObjectRemoved = false;

            bool matchesCurrentScreen =
                GetIfMatchesCurrentScreen(removeObjectDto.ElementName, out System.Type ownerType, out Screen currentScreen);

            if (matchesCurrentScreen)
            {
                response.DidScreenMatch = true;
                var isEditingEntity =
                    ScreenManager.CurrentScreen?.GetType() == typeof(Screens.EntityViewingScreen);
                var editingMode = isEditingEntity
                    ? GlueControl.Editing.ElementEditingMode.EditingEntity
                    : GlueControl.Editing.ElementEditingMode.EditingScreen;

                var available = GlueControl.Editing.SelectionLogic.GetAvailableObjects(editingMode)
                        .FirstOrDefault(item => item.Name == removeObjectDto.ObjectName);

                if (available is IDestroyable asDestroyable)
                {
                    asDestroyable.Destroy();
                    response.WasObjectRemoved = true;
                }
                else if (available is AxisAlignedRectangle rectangle)
                {
                    ShapeManager.Remove(rectangle);
                    response.WasObjectRemoved = true;
                }
                else if (available is Circle circle)
                {
                    ShapeManager.Remove(circle);
                    response.WasObjectRemoved = true;
                }
                else if (available is Polygon polygon)
                {
                    ShapeManager.Remove(polygon);
                    response.WasObjectRemoved = true;
                }
                else if (available is Sprite sprite)
                {
                    SpriteManager.RemoveSprite(sprite);
                    response.WasObjectRemoved = true;
                }

                if(!response.WasObjectRemoved)
                {
                    // see if there is a collision relationship with this name
                    var matchingCollisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(
                        item => item.Name == removeObjectDto.ObjectName);

                    if(matchingCollisionRelationship != null)
                    {
                        CollisionManager.Self.Relationships.Remove(matchingCollisionRelationship);
                        response.WasObjectRemoved = true;
                    }
                }
            }
            return response;
        }

        private static void HandleDto(CreateNewEntityDto createNewEntityDto)
        {
            var entitySave = createNewEntityDto.EntitySave;

            // convert the entity save name (which is the glue name) to a type name:
            string elementName = GlueToGameElementName(entitySave.Name);


            InstanceLogic.Self.CustomGlueElements[elementName] = entitySave;
        }
    }
}
