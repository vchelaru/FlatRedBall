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

        public static string Receive(string message, Func<object, bool> runPredicate = null)
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

            if (matchingMethod == null)
            {
                throw new InvalidOperationException(
                    $"Could not find a HandleDto method for type {dtoTypeName}");
            }

            var dtoType = matchingMethod.GetParameters()[0].ParameterType;

            var dto = JsonConvert.DeserializeObject(dtoSerialized, dtoType);

            if (runPredicate == null || runPredicate(dto))
            {
                var response = ReceiveDto(dto);

                if (response != null)
                {
                    return JsonConvert.SerializeObject(response);
                }
            }
            return null;
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

        // todo - move this to some type manager
        public static bool DoTypesMatch(PositionedObject positionedObject, string qualifiedTypeName, Type possibleType = null)
        {
            if (possibleType == null)
            {
                possibleType = typeof(CommandReceiver).Assembly.GetType(qualifiedTypeName);
            }

            if (positionedObject.GetType() == possibleType)
            {
                return true;
            }
            else if (positionedObject is GlueControl.Runtime.DynamicEntity dynamicEntity)
            {
                return dynamicEntity.EditModeType == qualifiedTypeName;
            }
            else
            {
                // here we need to do reflection to get the EditModeType, but that's not implemented yet.
                // This is needed for inherited entities
                return false;
            }
        }

        #endregion

        #region Message Queue

        /// <summary>
        /// Stores all commands that have been sent from Glue to game 
        /// which should always be re-run.
        /// </summary>
        public static Queue<object> GlobalGlueToGameCommands = new Queue<object>();
        public static Dictionary<string, Queue<object>> ScreenSpecificGlueToGameCommands =
            new Dictionary<string, Queue<object>>();

        public static void EnqueueToOwner(object dto, string owner)
        {
            if (string.IsNullOrEmpty(owner))
            {
                GlobalGlueToGameCommands.Enqueue(dto);
            }
            else
            {
                var ownerType = typeof(GlueControlManager).Assembly.GetType(owner);
                var isEntity = typeof(PositionedObject).IsAssignableFrom(ownerType) ||
                    InstanceLogic.Self.CustomGlueElements.ContainsKey(owner);
                if (isEntity)
                {
                    // If it's on an entity, then it needs to be applied globally
                    GlobalGlueToGameCommands.Enqueue(dto);
                }
                else
                {
                    EnqueueScreenSpecificMessage(dto, owner);
                }
            }
        }



        private static void EnqueueScreenSpecificMessage(object dto, string owner)
        {
            Queue<object> queue = null;
            if (ScreenSpecificGlueToGameCommands.ContainsKey(owner))
            {
                queue = ScreenSpecificGlueToGameCommands[owner];
            }
            else
            {
                queue = new Queue<object>();
                ScreenSpecificGlueToGameCommands.Add(owner, queue);
            }
            queue.Enqueue(dto);
        }



        #endregion

        #region Set Variable

        private static GlueVariableSetDataResponse HandleDto(GlueVariableSetData dto)
        {
            GlueVariableSetDataResponse response = null;

            var shouldEnqueue = true;
            if (dto.AssignOrRecordOnly == AssignOrRecordOnly.Assign)
            {
                response = GlueControl.Editing.VariableAssignmentLogic.SetVariable(dto);
                shouldEnqueue = response.WasVariableAssigned;
            }
            else
            {
                // If it's a record-only, then we'll always want to enqueue it
                // need to change the record only back to assign so future re-runs will assign
                dto.AssignOrRecordOnly = AssignOrRecordOnly.Assign;
                shouldEnqueue = true;
            }
            if (shouldEnqueue)
            {
                EnqueueToOwner(dto, dto.InstanceOwner);
            }
            return response;
        }

        #endregion

        #region Select Object
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
                        DoTypesMatch(SpriteManager.ManagedPositionedObjects[0], ownerTypeName, ownerType);

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

        #endregion

        #region Rename

        public static string GlueToGameElementName(string elementName)
        {
            return "EditModeProject." + elementName.Replace("\\", ".");
        }

        #endregion

        #region Destroy Screen

        private static void HandleScreenDestroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();
        }

        #endregion

        #region Add Object

        private static AddObjectDtoResponse HandleDto(AddObjectDto dto)
        {
            AddObjectDtoResponse valueToReturn = new AddObjectDtoResponse();
#if IncludeSetVariable

            var createdObject = GlueControl.InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(dto);
            valueToReturn.WasObjectCreated = createdObject != null;
#endif
            CommandReceiver.EnqueueToOwner(dto, dto.ElementNameGame);
            return valueToReturn;
        }

        #endregion

        #region Edit vs Play

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

        #endregion

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

            CommandReceiver.EnqueueToOwner(dto, dto.ElementName);


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

                if (!response.WasObjectRemoved)
                {
                    // see if there is a collision relationship with this name
                    var matchingCollisionRelationship = CollisionManager.Self.Relationships.FirstOrDefault(
                        item => item.Name == removeObjectDto.ObjectName);

                    if (matchingCollisionRelationship != null)
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

        private static void HandleDto(RestartScreenDto dto)
        {
            var screen = ScreenManager.CurrentScreen;
            screen.RestartScreen(true);
        }

        private static void HandleDto(ReloadGlobalContentDto dto)
        {
            GlobalContent.Reload(GlobalContent.GetFile(dto.StrippedGlobalContentFileName));
        }

        private static void HandleDto(TogglePauseDto dto)
        {
            var screen = ScreenManager.CurrentScreen;

            if (screen.IsPaused)
            {
                screen.UnpauseThisScreen();
            }
            else
            {
                screen.PauseThisScreen();
            }
        }

        private static void HandleDto(AdvanceOneFrameDto dto)
        {
            var screen = ScreenManager.CurrentScreen;

            screen.UnpauseThisScreen();
            var delegateInstruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
            {
                screen.PauseThisScreen();
            });
            delegateInstruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + .001;

            FlatRedBall.Instructions.InstructionManager.Instructions.Add(delegateInstruction);
        }

        private static void HandleDto(SetSpeedDto dto)
        {
            FlatRedBall.TimeManager.TimeFactor = dto.SpeedPercentage / 100.0f;
        }
    }
}
