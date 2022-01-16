{CompilerDirectives}
using FlatRedBall;
using FlatRedBall.Gui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall.Graphics;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using FlatRedBall.Utilities;

using GlueControl.Dtos;
using GlueControl.Editing;
using GlueControl.Runtime;
using GlueControl.Models;

namespace GlueControl
{
    public class InstanceLogic
    {
        #region Objects added at runtime 
        public List<ShapeCollection> ShapeCollectionsAddedAtRuntime = new List<ShapeCollection>();

        public ShapeCollection ShapesAddedAtRuntime = new ShapeCollection();

        public FlatRedBall.Math.PositionedObjectList<Sprite> SpritesAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Sprite>();
        public FlatRedBall.Math.PositionedObjectList<Text> TextsAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Text>();

        public List<IDestroyable> DestroyablesAddedAtRuntime = new List<IDestroyable>();

        // Do we want to support entire categories at runtime? For now just states, but we'll have to review this at some point
        // if we want to allow entire categories added at runtime. The key is the game type (GameNamespace.Entities.EntityName)
        public Dictionary<string, List<StateSaveCategory>> StatesAddedAtRuntime = new Dictionary<string, List<StateSaveCategory>>();

        public Dictionary<string, List<CustomVariable>> CustomVariablesAddedAtRuntime = new Dictionary<string, List<CustomVariable>>();


        public List<IList> ListsAddedAtRuntime = new List<IList>();

#if HasGum
        public List<Gum.Wireframe.GraphicalUiElement> GumObjectsAddedAtRuntime = new List<Gum.Wireframe.GraphicalUiElement>();
        public List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper> GumWrappersAddedAtRuntime = new List<GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper>();
#endif

        #endregion

        #region Fields/Properties

        static InstanceLogic self;
        public static InstanceLogic Self
        {
            get
            {
                if (self == null)
                {
                    self = new InstanceLogic();
                }
                return self;
            }
        }



        /// <summary>
        /// A dictionary of custom elements where the key is the full name of the type that
        /// would exist if the code were generated (such as "ProjectNamespace.Entities.MyEntity")
        /// </summary>
        public Dictionary<string, GlueElement> CustomGlueElements = new Dictionary<string, GlueElement>();


        // this is to prevent multiple objects from having the same name in the same frame:
        static long NewIndex = 0;

        #endregion

        #region Create Instance from Glue

        public object HandleCreateInstanceCommandFromGlue(Dtos.AddObjectDto dto, int currentAddObjectIndex, PositionedObject forcedItem = null)
        {
            //var glueName = dto.ElementName;
            // this comes in as the game name not glue name
            var elementGameType = dto.ElementNameGame; // CommandReceiver.GlueToGameElementName(glueName);
            var ownerType = this.GetType().Assembly.GetType(elementGameType);
            GlueElement ownerElement = null;
            if (CustomGlueElements.ContainsKey(elementGameType))
            {
                ownerElement = CustomGlueElements[elementGameType];
            }

            var addedToEntity =
                (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                ||
                ownerElement != null && ownerElement is EntitySave;

            if (addedToEntity)
            {
                if (forcedItem != null)
                {
                    if (CommandReceiver.DoTypesMatch(forcedItem, elementGameType))
                    {
                        HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, forcedItem);
                    }
                }
                else
                {
                    // need to loop through every object and see if it is an instance of the entity type, and if so, add this object to it
                    for (int i = 0; i < SpriteManager.ManagedPositionedObjects.Count; i++)
                    {
                        var item = SpriteManager.ManagedPositionedObjects[i];
                        if (CommandReceiver.DoTypesMatch(item, elementGameType))
                        {
                            HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, item);
                        }
                    }
                }
            }
            else if (forcedItem == null &&
                (ScreenManager.CurrentScreen.GetType().FullName == elementGameType || ownerType?.IsAssignableFrom(ScreenManager.CurrentScreen.GetType()) == true))
            {
                // it's added to the base screen, so just add it to null
                HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, null);
            }
            return dto;
        }

        private object HandleCreateInstanceCommandFromGlueInner(Models.NamedObjectSave deserialized, int currentAddObjectIndex, PositionedObject owner)
        {
            // The owner is the
            // PositionedObject which
            // owns the newly-created instance
            // from the NamedObjectSave. Note that
            // if the owner is a DynamicEntity, it will
            // automatically remove any attached objects; 
            // however, if it is not, the objects still need
            // to be removed by the Glue control system, so we 
            // are going to add them to the ShapesAddedAtRuntime

            PositionedObject newPositionedObject = null;
            object newObject = null;

            if (deserialized.SourceType == GlueControl.Models.SourceType.Entity)
            {
                newPositionedObject = CreateEntity(deserialized, currentAddObjectIndex);
            }
            else if (deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType &&
                deserialized.IsCollisionRelationship())
            {
                newObject = TryCreateCollisionRelationship(deserialized);
            }
            else if (deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType)
            {
                switch (deserialized.SourceClassType)
                {
                    case "FlatRedBall.Math.Geometry.AxisAlignedRectangle":
                    case "AxisAlignedRectangle":
                        {
                            var aaRect = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddAxisAlignedRectangle(aaRect);
                                ShapesAddedAtRuntime.Add(aaRect);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(aaRect);
                            }
                            newPositionedObject = aaRect;
                        }

                        break;
                    case "FlatRedBall.Math.Geometry.Circle":
                    case "Circle":
                        {
                            var circle = new FlatRedBall.Math.Geometry.Circle();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddCircle(circle);
                                ShapesAddedAtRuntime.Add(circle);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(circle);
                            }
                            newPositionedObject = circle;
                        }
                        break;
                    case "FlatRedBall.Math.Geometry.Polygon":
                    case "Polygon":
                        {
                            var polygon = new FlatRedBall.Math.Geometry.Polygon();
                            if (deserialized.AddToManagers)
                            {
                                ShapeManager.AddPolygon(polygon);
                                ShapesAddedAtRuntime.Add(polygon);
                            }
                            if (owner is ICollidable asCollidable && deserialized.IncludeInICollidable)
                            {
                                asCollidable.Collision.Add(polygon);
                            }
                            newPositionedObject = polygon;
                        }
                        break;
                    case "FlatRedBall.Sprite":
                    case "Sprite":
                        var sprite = new FlatRedBall.Sprite();
                        if (deserialized.AddToManagers)
                        {
                            SpriteManager.AddSprite(sprite);
                            SpritesAddedAtRuntime.Add(sprite);
                        }
                        newPositionedObject = sprite;

                        break;
                    case "Text":
                    case "FlatRedBall.Graphics.Text":
                        var text = new FlatRedBall.Graphics.Text();
                        text.Font = TextManager.DefaultFont;
                        text.SetPixelPerfectScale(Camera.Main);
                        if (deserialized.AddToManagers)
                        {
                            TextManager.AddText(text);
                            TextsAddedAtRuntime.Add(text);
                        }
                        newPositionedObject = text;
                        break;
                    case "FlatRedBall.Math.Geometry.ShapeCollection":
                    case "ShapeCollection":
                        var shapeCollection = new ShapeCollection();
                        ShapeCollectionsAddedAtRuntime.Add(shapeCollection);
                        newObject = shapeCollection;
                        break;
                    case "FlatRedBall.Math.PositionedObjectList<T>":
                        newObject = CreatePositionedObjectList(deserialized);
                        break;
                }

                if (newObject == null)
                {
                    newObject = TryCreateGumObject(deserialized, owner);
                }
            }
            if (newPositionedObject != null)
            {
                newObject = newPositionedObject;

                if (owner != null)
                {
                    newPositionedObject.AttachTo(owner);
                }
            }
            if (newObject != null)
            {
                AssignVariablesOnNewlyCreatedObject(deserialized, newObject);
            }

            return newObject;
        }

        private object TryCreateGumObject(NamedObjectSave deserialized, PositionedObject owner)
        {
#if HasGum
            var type = this.GetType().Assembly.GetType(deserialized.SourceClassType);
            var isGum = type != null && typeof(Gum.Wireframe.GraphicalUiElement).IsAssignableFrom(type);

            if (isGum)
            {
                var oldLayoutSuspended = global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended;
                global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = true;
                var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });
                var newGumObjectInstance = 
                    constructor.Invoke(new object[] { true, true }) as Gum.Wireframe.GraphicalUiElement;

                global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = oldLayoutSuspended;
                newGumObjectInstance.UpdateFontRecursive();
                newGumObjectInstance.UpdateLayout();

                // eventually support layered, but not for now.....
                newGumObjectInstance.AddToManagers(RenderingLibrary.SystemManagers.Default, null);

                if (owner != null)
                {
                    var wrapperForAttachment = new GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper(owner, newGumObjectInstance);
                    FlatRedBall.SpriteManager.AddPositionedObject(wrapperForAttachment);
                    wrapperForAttachment.Name = deserialized.InstanceName;
                    //gumAttachmentWrappers.Add(wrapperForAttachment);
                    GumWrappersAddedAtRuntime.Add(wrapperForAttachment);
                }
                GumObjectsAddedAtRuntime.Add(newGumObjectInstance);

                return newGumObjectInstance;
            }
#endif
            return null;
        }

        private Object CreatePositionedObjectList(Models.NamedObjectSave namedObject)
        {
            var sourceClassGenericType = namedObject.SourceClassGenericType;

            var gameTypeName =
                CommandReceiver.GlueToGameElementName(sourceClassGenericType);

            var type = this.GetType().Assembly.GetType(gameTypeName);

            object newList = null;

            if (type == null)
            {
                // see if it's contained in the list of dynamic entities

                var isDynamicEntity = CustomGlueElements.ContainsKey(gameTypeName);
                if (isDynamicEntity)
                {
                    var list = new PositionedObjectList<DynamicEntity>();
                    ListsAddedAtRuntime.Add(list);
                    newList = list;
                }
                else
                {
                    var list = new PositionedObjectList<PositionedObject>();
                    ListsAddedAtRuntime.Add(list);
                    newList = list;
                }
            }
            else
            {
                var poList = typeof(PositionedObjectList<>).MakeGenericType(type);
                var list = poList.GetConstructor(new Type[0]).Invoke(new object[0]) as IList;
                ListsAddedAtRuntime.Add(list);
                newList = list;
            }
            return newList;
        }

        private object TryCreateCollisionRelationship(Models.NamedObjectSave deserialized)
        {
            var type = Editing.VariableAssignmentLogic.GetDesiredRelationshipType(deserialized, out object firstObject, out object secondObject);
            if (type == null)
            {
                return null;
            }
            else
            {
                object toReturn = null;
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    List<object> parameters = new List<object>();
                    if (firstObject != null)
                    {
                        parameters.Add(firstObject);
                    }
                    if (secondObject != null)
                    {
                        parameters.Add(secondObject);
                    }
                    var collisionRelationship =
                        constructor.Invoke(parameters.ToArray()) as FlatRedBall.Math.Collision.CollisionRelationship;
                    collisionRelationship.Partitions = FlatRedBall.Math.Collision.CollisionManager.Self.Partitions;
                    toReturn = collisionRelationship;
                    FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(collisionRelationship);
                }
                return toReturn;
            }
        }


        public PositionedObject CreateEntity(Models.NamedObjectSave deserialized, int currentAddObjectIndex)
        {
            var entityNameGlue = deserialized.SourceClassType;
            var newEntity = CreateEntity(CommandReceiver.GlueToGameElementName(entityNameGlue), currentAddObjectIndex);

            return newEntity;
        }

        public void ApplyEditorCommandsToNewEntity(PositionedObject newEntity, int currentAddObjectIndex = -1)
        {
            currentAddObjectIndex = currentAddObjectIndex > 0
                ? currentAddObjectIndex
                : CommandReceiver.GlobalGlueToGameCommands.Count;
            for (int i = 0; i < currentAddObjectIndex; i++)
            {
                var dto = CommandReceiver.GlobalGlueToGameCommands[i];
                if (dto is Dtos.AddObjectDto addObjectDtoRerun)
                {
                    HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, currentAddObjectIndex, newEntity);
                }
                else if (dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                {
                    GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, newEntity);
                }
                else if (dto is RemoveObjectDto removeObjectDtoRerun)
                {
                    HandleDeleteInstanceCommandFromGlue(removeObjectDtoRerun, newEntity);
                }
            }
        }

        public PositionedObject CreateEntity(string entityNameGameType, int currentAddObjectIndex = -1)
        {
            var containsKey =
                CustomGlueElements.ContainsKey(entityNameGameType);
            if (!containsKey && !string.IsNullOrWhiteSpace(entityNameGameType) && entityNameGameType.Contains('.') == false)
            {
                // It may not be qualified, which means it is coming from content that doesn't qualify - like Tiled
                entityNameGameType = CustomGlueElements.Keys.FirstOrDefault(item => item.Split('.').Last() == entityNameGameType);
                // Now that we've qualified, try again
                if (!string.IsNullOrWhiteSpace(entityNameGameType))
                {
                    containsKey =
                        CustomGlueElements.ContainsKey(entityNameGameType);
                }
            }

            PositionedObject newEntity = null;

            // This function may be given a qualified name like MyGame.Entities.MyEntity (if from Glue) 
            // or an unqualified name like MyEntity (if from Tiled). If from Tiled, then this code attempts
            // to fully qualify the entity name. This attempt to qualify may make the name null, so we need to
            // check and tolerate null.
            if (string.IsNullOrWhiteSpace(entityNameGameType))
            {
                newEntity = null;
            }
            else if (containsKey)
            {
                var dynamicEntityInstance = new Runtime.DynamicEntity();
                dynamicEntityInstance.EditModeType = entityNameGameType;
                SpriteManager.AddPositionedObject(dynamicEntityInstance);

                DestroyablesAddedAtRuntime.Add(dynamicEntityInstance);

                newEntity = dynamicEntityInstance;

                ApplyEditorCommandsToNewEntity(newEntity, currentAddObjectIndex);
            }
            else
            {
                PositionedObject newPositionedObject;
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityNameGameType);
                if (factory != null)
                {
                    newPositionedObject = factory?.CreateNew() as FlatRedBall.PositionedObject;
                }
                else
                {
                    // just instantiate it using reflection?
                    newPositionedObject = this.GetType().Assembly.CreateInstance(entityNameGameType)
                         as PositionedObject;
                    //newPositionedObject = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                }
                if (newPositionedObject != null && newPositionedObject is IDestroyable asDestroyable)
                {
                    DestroyablesAddedAtRuntime.Add(asDestroyable);
                }
                newEntity = newPositionedObject;

                if (factory == null)
                {
                    ApplyEditorCommandsToNewEntity(newEntity, currentAddObjectIndex);
                }
            }


            return newEntity;
        }

        private void AssignVariablesOnNewlyCreatedObject(Models.NamedObjectSave deserialized, object newObject)
        {
            if (newObject is FlatRedBall.Utilities.INameable asNameable)
            {
                asNameable.Name = deserialized.InstanceName;
            }
            if (newObject is PositionedObject asPositionedObject)
            {
                if (ScreenManager.IsInEditMode)
                {
                    asPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    asPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                }
                asPositionedObject.CreationSource = "Glue"; // Glue did make this, so do this so the game can select it
            }

            foreach (var instruction in deserialized.InstructionSaves)
            {
                AssignVariable(newObject, instruction, convertFileNamesToObjects: true);
            }
        }

        #endregion

        #region Delete Instance from Glue

        public RemoveObjectDtoResponse HandleDeleteInstanceCommandFromGlue(RemoveObjectDto removeObjectDto, PositionedObject forcedItem = null)
        {
            var elementNameGlue = removeObjectDto.ElementNameGlue;

            RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();
            response.DidScreenMatch = false;
            response.WasObjectRemoved = false;

            foreach (var objectName in removeObjectDto.ObjectNames)
            {
                HandleDeleteObject(forcedItem, elementNameGlue, objectName, response);
            }


            return response;
        }

        private void HandleDeleteObject(PositionedObject forcedItem, string elementNameGlue, string objectName, RemoveObjectDtoResponse response)
        {
            var elementGameType = CommandReceiver.GlueToGameElementName(elementNameGlue);

            var ownerType = this.GetType().Assembly.GetType(elementGameType);
            GlueElement ownerElement = null;
            if (CustomGlueElements.ContainsKey(elementGameType))
            {
                ownerElement = CustomGlueElements[elementGameType];
            }


            var removedFromEntity =
                (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                ||
                ownerElement != null && ownerElement is EntitySave;


            if (removedFromEntity)
            {
                if (forcedItem != null)
                {
                    if (CommandReceiver.DoTypesMatch(forcedItem, elementGameType))
                    {
                        var objectToDelete = forcedItem.Children.FindByName(objectName);
                        if (objectToDelete != null)
                        {
                            TryDeleteObject(response, objectToDelete);
                        }
                    }
                }
                foreach (var item in SpriteManager.ManagedPositionedObjects)
                {
                    if (CommandReceiver.DoTypesMatch(item, elementGameType, ownerType))
                    {
                        // try to remove this object from here...
                        //screen.ApplyVariable(variableNameOnObjectInInstance, variableValue, item);
                        var objectToDelete = item.Children.FindByName(objectName);

                        if (objectToDelete != null)
                        {
                            TryDeleteObject(response, objectToDelete);
                        }
                    }
                }
            }
            // see VariableAssignmentLogic.SetVariable by `var setOnEntity =` code for info on why we do this check
            else if (forcedItem == null)
            {
                bool matchesCurrentScreen =
                    (ScreenManager.CurrentScreen.GetType().FullName == elementGameType || ownerType?.IsAssignableFrom(ScreenManager.CurrentScreen.GetType()) == true);

                if (matchesCurrentScreen)
                {
                    response.DidScreenMatch = true;
                    var isEditingEntity =
                        ScreenManager.CurrentScreen?.GetType() == typeof(Screens.EntityViewingScreen);
                    var editingMode = isEditingEntity
                        ? GlueControl.Editing.ElementEditingMode.EditingEntity
                        : GlueControl.Editing.ElementEditingMode.EditingScreen;

                    var foundObject = GlueControl.Editing.SelectionLogic.GetAvailableObjects(editingMode)
                            .FirstOrDefault(item => item.Name == objectName);
                    TryDeleteObject(response, foundObject);

                    if (!response.WasObjectRemoved)
                    {
                        // see if there is a collision relationship with this name
                        var matchingCollisionRelationship = FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.FirstOrDefault(
                            item => item.Name == objectName);

                        if (matchingCollisionRelationship != null)
                        {
                            FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Remove(matchingCollisionRelationship);
                            response.WasObjectRemoved = true;
                        }
                    }
                }
            }
        }

        private static void TryDeleteObject(RemoveObjectDtoResponse removeResponse, PositionedObject objectToDelete)
        {
            if (objectToDelete is IDestroyable asDestroyable)
            {
                asDestroyable.Destroy();
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is AxisAlignedRectangle rectangle)
            {
                ShapeManager.Remove(rectangle);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Circle circle)
            {
                ShapeManager.Remove(circle);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Polygon polygon)
            {
                ShapeManager.Remove(polygon);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Sprite sprite)
            {
                SpriteManager.RemoveSprite(sprite);
                removeResponse.WasObjectRemoved = true;
            }
            else if (objectToDelete is Text text)
            {
                TextManager.RemoveText(text);
                removeResponse.WasObjectRemoved = true;
            }
#if HasGum
            else if (objectToDelete is GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper gumWrapper)
            {
                gumWrapper.GumObject.Destroy();
                gumWrapper.RemoveSelfFromListsBelongingTo();
            }
#endif
        }


        #endregion

        private static void SendAndEnqueue(Dtos.AddObjectDto addObjectDto)
        {
            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            if (currentScreen is Screens.EntityViewingScreen entityViewingScreen)
            {
                addObjectDto.ElementNameGame = entityViewingScreen.CurrentEntity.GetType().FullName;
            }
            else
            {
                addObjectDto.ElementNameGame = currentScreen.GetType().FullName;
            }

            GlueControlManager.Self.SendToGlue(addObjectDto);

            CommandReceiver.GlobalGlueToGameCommands.Add(addObjectDto);
        }

        #region Create Instance from Game

        private string GetNameFor(string itemType)
        {
            if (itemType.Contains('.'))
            {
                var lastDot = itemType.LastIndexOf('.');
                itemType = itemType.Substring(lastDot + 1);
            }
            var newName = $"{itemType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}_{NewIndex}";
            NewIndex++;

            return newName;
        }

        private void AddFloatValue(Dtos.AddObjectDto addObjectDto, string name, float value)
        {
            AddValueToDto(addObjectDto, name, "float", value);
        }

        private void AddStringValue(Dtos.AddObjectDto addObjectDto, string name, string value)
        {
            AddValueToDto(addObjectDto, name, "string", value);
        }

        private void AddValueToDto(Dtos.AddObjectDto addObjectDto, string name, string type, object value)
        {
            addObjectDto.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
            {
                Member = name,
                Type = type,
                Value = value
            });
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityGameType, PositionedObject original)
        {
            var newName = GetNameFor(entityGameType);

            var newInstance = CreateEntity(entityGameType);
            newInstance.X = original.X;
            newInstance.Y = original.Y;
            newInstance.Name = newName;


            // The NamedObjectSave could contain additional properties that are assigned
            // on the instance. We want to assign those:
            var glueElement = EditingManager.Self.CurrentGlueElement;
            // find the nos:
            var nosForCopiedObject = EditingManager.Self.CurrentGlueElement
                ?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == original.Name);

            if (nosForCopiedObject != null)
            {
                // loop through all the variables on the nos and apply them to the
                // copied runtime object *and* send those back to Glue so Glue can apply
                // them to the copied NOS
                // Note that if these values are on the NamedObject.InstructionSaves, then 
                // they have been explicitly set. Therefore, we don't have to do equality comparison
                // between the old and new value. If they're in the copied InstructionSaves, they should
                // be explicitly set on the new!
                foreach (var instructionInOriginal in nosForCopiedObject.InstructionSaves)
                {
                    var shouldSend =
                        instructionInOriginal.Member != "X" &&
                        instructionInOriginal.Member != "Y";

                    if (shouldSend)
                    {
                        object valueToSet = null;

                        try
                        {

                            // When applying values to the runtime, don't use the
                            // NamedObject, because that contains serialized values
                            // which may need to be converted back. Just use the value
                            // directly from the copy:
                            var originalRuntimeValue = LateBinder.GetValueStatic(
                                original, instructionInOriginal.Member);
                            valueToSet = originalRuntimeValue;
                        }
                        catch
                        {
                            // There are some properties (like paths on PathInstance) which
                            // can only be set, not gotten. Therefore, we should try/catch here
                            // and tolerate values that can't be obtained through a get call.
                            // If there is a failure, fall back to the instruction:

                            valueToSet = instructionInOriginal.Value;
                        }

                        // apply it on the copy
                        // Note - Glue keeps old
                        // variables around whenever
                        // a type changes or even when
                        // the type stays the same but a
                        // variable is removed from the defining
                        // type. Therefore, there could be orphan
                        // variables on the NamedObjectSave which don't
                        // exist on the class itself. Therefore, we need
                        // to tolerate MemberAccessExceptions:
                        try
                        {
                            LateBinder.SetValueStatic(
                                newInstance, instructionInOriginal.Member,
                                valueToSet);

                        }
                        catch (MemberAccessException)
                        {
                            // See above for an explanation on why this is okay
                        }

                    }
                }
            }

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.CopyOriginalName = original.Name;
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = CommandReceiver.GameElementTypeToGlueElement(entityGameType);

            AddFloatValue(addObjectDto, "X", original.X);
            AddFloatValue(addObjectDto, "Y", original.Y);

            var properties = newInstance.GetType().GetProperties();

            foreach (var newInstanceProperty in properties)
            {
                var didFailToGetProperty = false;

                object oldPropertyValue = null;
                object newPropertyValue = null;

                try
                {
                    oldPropertyValue = newInstanceProperty.GetValue(original);
                    newPropertyValue = newInstanceProperty.GetValue(newInstance);
                }
                catch
                {
                    didFailToGetProperty = true;
                }
                if (oldPropertyValue != newPropertyValue && !didFailToGetProperty)
                {
                    // they differ, so we should set and DTO it
                    // But how do we know what to set and what not to set? I think we should whitelist...

                    var shouldSet = oldPropertyValue != null;
                    var isState = false;
                    if (shouldSet)
                    {
                        // for now we'll only handle states, which have a + in the name. 
                        var fullName = newInstanceProperty.PropertyType.FullName;
                        isState = fullName.Contains("+");
                        shouldSet = isState;
                    }

                    if (shouldSet)
                    {
                        newInstanceProperty.SetValue(newInstance, oldPropertyValue);
                        var type = newInstanceProperty.PropertyType.Name;
                        var value = oldPropertyValue;

                        if (isState)
                        {
                            type = newInstanceProperty.PropertyType.FullName.Replace("+", ".");
                            var nameField = newInstanceProperty.PropertyType.GetField("Name");
                            if (nameField != null)
                            {
                                value = nameField.GetValue(value);
                            }
                        }

                        AddValueToDto(addObjectDto, newInstanceProperty.Name, type, value);
                    }
                }
            }

            if (nosForCopiedObject != null)
            {
                foreach (var instruction in nosForCopiedObject.InstructionSaves)
                {
                    if (instruction.Member != "X" && instruction.Member != "Y")
                    {
                        addObjectDto.InstructionSaves.Add(instruction);
                    }
                }
            }

            #endregion


            // do we need to add the new NOS to the current element? Or do we rely on the game to tell us that? Need to test/decide this....
            // Update - The game will eventually refresh this whenever the selection changes, but we do want to 
            var currentElement = EditingManager.Self.CurrentGlueElement;
            if (currentElement != null)
            {
                if (currentElement.NamedObjects.Contains(nosForCopiedObject))
                {
                    currentElement.NamedObjects.Add(addObjectDto);
                }
                else
                {
                    var container = currentElement.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(nosForCopiedObject));
                    if (container != null)
                    {
                        container.ContainedObjects.Add(addObjectDto);
                    }
                }
            }

            SendAndEnqueue(addObjectDto);

            return newInstance;
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityGameType, float x, float y)
        {
            var newName = GetNameFor(entityGameType);

            var toReturn = CreateEntity(entityGameType);
            toReturn.X = x;
            toReturn.Y = y;
            toReturn.Name = newName;

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = CommandReceiver.GameElementTypeToGlueElement(entityGameType);

            AddFloatValue(addObjectDto, "X", x);
            AddFloatValue(addObjectDto, "Y", y);

            //var fields = toReturn.GetType().GetFields();




            #endregion

            SendAndEnqueue(addObjectDto);

            return toReturn;
        }

        public Circle HandleCreateCircleByGame(Circle originalCircle, string copiedObjectName)
        {
            var newCircle = originalCircle.Clone();
            var newName = GetNameFor("Circle");

            newCircle.Visible = originalCircle.Visible;
            newCircle.Name = newName;

            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(newCircle))
            {
                ShapeManager.AddCircle(newCircle);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newCircle);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.CopyOriginalName = copiedObjectName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.Circle";

            AddFloatValue(addObjectDto, "X", newCircle.X);
            AddFloatValue(addObjectDto, "Y", newCircle.Y);
            AddFloatValue(addObjectDto, "Radius", newCircle.Radius);

            #endregion

            SendAndEnqueue(addObjectDto);

            return newCircle;
        }

        public AxisAlignedRectangle HandleCreateAxisAlignedRectangleByGame(AxisAlignedRectangle originalRectangle, string copiedObjectName)
        {
            var newRectangle = originalRectangle.Clone();
            var newName = GetNameFor("Rectangle");

            newRectangle.Visible = originalRectangle.Visible;
            newRectangle.Name = newName;


            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(originalRectangle))
            {
                ShapeManager.AddAxisAlignedRectangle(newRectangle);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newRectangle);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.CopyOriginalName = copiedObjectName;
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.AxisAlignedRectangle";

            AddFloatValue(addObjectDto, "X", newRectangle.X);
            AddFloatValue(addObjectDto, "Y", newRectangle.Y);
            AddFloatValue(addObjectDto, "Width", newRectangle.Width);
            AddFloatValue(addObjectDto, "Height", newRectangle.Height);

            #endregion

            SendAndEnqueue(addObjectDto);

            return newRectangle;
        }

        public Polygon HandleCreatePolygonByGame(Polygon originalPolygon, string copiedObjectName)
        {
            var newPolygon = originalPolygon.Clone();
            var newName = GetNameFor("Polygon");

            newPolygon.Visible = originalPolygon.Visible;
            newPolygon.Name = newName;

            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(originalPolygon))
            {
                ShapeManager.AddPolygon(newPolygon);
            }
            InstanceLogic.Self.ShapesAddedAtRuntime.Add(newPolygon);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.CopyOriginalName = copiedObjectName;
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.Polygon";

            AddFloatValue(addObjectDto, "X", newPolygon.X);
            AddFloatValue(addObjectDto, "Y", newPolygon.Y);

            AddValueToDto(addObjectDto, "Points", typeof(List<Point>).ToString(),
                Newtonsoft.Json.JsonConvert.SerializeObject(newPolygon.Points.ToList()));

            #endregion

            SendAndEnqueue(addObjectDto);

            return newPolygon;
        }

        public Sprite HandleCreateSpriteByName(Sprite originalSprite, string copiedObjectName)
        {
            var newSprite = originalSprite.Clone();
            var newName = GetNameFor("Sprite");

            newSprite.Name = newName;

            if (SpriteManager.AutomaticallyUpdatedSprites.Contains(originalSprite))
            {
                SpriteManager.AddSprite(newSprite);
            }
            InstanceLogic.Self.SpritesAddedAtRuntime.Add(newSprite);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.CopyOriginalName = copiedObjectName;
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            addObjectDto.SourceClassType = "FlatRedBall.Sprite";

            AddFloatValue(addObjectDto, "X", newSprite.X);
            AddFloatValue(addObjectDto, "Y", newSprite.Y);
            if (newSprite.TextureScale > 0)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.TextureScale), newSprite.TextureScale);
            }
            else
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Width), newSprite.Width);
                AddFloatValue(addObjectDto, nameof(newSprite.Height), newSprite.Height);
            }


            if (newSprite.Texture != null)
            {
                // Texture must be assigned before pixel values.
                AddValueToDto(addObjectDto, "Texture", typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName,
                    newSprite.Texture.Name);

                // Glue uses the pixel coords, but we can check the coordinates more easily
                if (newSprite.LeftTextureCoordinate != 0)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.LeftTexturePixel), newSprite.LeftTexturePixel);
                }
                if (newSprite.TopTextureCoordinate != 0)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.TopTexturePixel), newSprite.TopTexturePixel);
                }
                if (newSprite.RightTextureCoordinate != 1)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.RightTexturePixel), newSprite.RightTexturePixel);
                }
                if (newSprite.BottomTextureCoordinate != 1)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.BottomTexturePixel), newSprite.BottomTexturePixel);
                }
            }
            if (newSprite.AnimationChains?.Name != null)
            {
                AddValueToDto(addObjectDto, "AnimationChains", typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName,
                    newSprite.AnimationChains.Name);
            }
            if (!string.IsNullOrEmpty(newSprite.CurrentChainName))
            {
                AddStringValue(addObjectDto, "CurrentChainName", newSprite.CurrentChainName);
            }
            if (newSprite.TextureAddressMode != Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp)
            {
                AddValueToDto(addObjectDto, nameof(newSprite.TextureAddressMode),
                    nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode), (int)newSprite.TextureAddressMode);
            }
            if (newSprite.Red != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Red), newSprite.Red);
            }
            if (newSprite.Green != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Green), newSprite.Green);
            }
            if (newSprite.Blue != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Blue), newSprite.Blue);
            }
            if (newSprite.Alpha != 1.0f)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Alpha), newSprite.Alpha);
            }
            if (newSprite.ColorOperation != ColorOperation.Texture)
            {
                AddValueToDto(addObjectDto, nameof(newSprite.ColorOperation),
                    nameof(ColorOperation), (int)newSprite.ColorOperation);
            }
            if (newSprite.BlendOperation != BlendOperation.Regular)
            {
                AddValueToDto(addObjectDto, nameof(newSprite.BlendOperation),
                    nameof(BlendOperation), (int)newSprite.BlendOperation);
            }

            // do we want to consider animated sprites? Does it matter?
            // An animation could flip this and that would incorrectly set
            // that value on Glue but if it's animated that would get overwritten anyway, so maybe it's no biggie?
            if (newSprite.FlipHorizontal != false)
            {
                AddValueToDto(addObjectDto, nameof(newSprite.FlipHorizontal),
                    "bool", newSprite.FlipHorizontal);
            }
            if (newSprite.FlipVertical != false)
            {
                AddValueToDto(addObjectDto, nameof(newSprite.FlipVertical),
                    "bool", newSprite.FlipVertical);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newSprite;
        }

        public Text HandleCreateTextByName(Text originalText, string copiedObjectName)
        {
            var newText = originalText.Clone();
            var newName = GetNameFor("Text");

            newText.Name = newName;
            if (TextManager.AutomaticallyUpdatedTexts.Contains(originalText))
            {
                TextManager.AddText(newText);
            }
            InstanceLogic.Self.TextsAddedAtRuntime.Add(newText);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.CopyOriginalName = copiedObjectName;
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            addObjectDto.SourceClassType = typeof(FlatRedBall.Graphics.Text).FullName;

            AddFloatValue(addObjectDto, "X", newText.X);
            AddFloatValue(addObjectDto, "Y", newText.Y);

            AddValueToDto(addObjectDto, nameof(Text.DisplayText), "string", newText.DisplayText);

            AddValueToDto(addObjectDto, nameof(Text.HorizontalAlignment), nameof(HorizontalAlignment), (int)newText.HorizontalAlignment);
            AddValueToDto(addObjectDto, nameof(Text.VerticalAlignment), nameof(VerticalAlignment), (int)newText.VerticalAlignment);

            if (newText.Red != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Red), newText.Red);
            }
            if (newText.Green != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Green), newText.Green);
            }
            if (newText.Blue != 0.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Blue), newText.Blue);
            }
            if (newText.Alpha != 1.0f)
            {
                AddFloatValue(addObjectDto, nameof(newText.Alpha), newText.Alpha);
            }
            if (newText.ColorOperation != ColorOperation.Texture)
            {
                AddValueToDto(addObjectDto, nameof(newText.ColorOperation),
                    nameof(ColorOperation), (int)newText.ColorOperation);
            }
            if (newText.BlendOperation != BlendOperation.Regular)
            {
                AddValueToDto(addObjectDto, nameof(newText.BlendOperation),
                    nameof(BlendOperation), (int)newText.BlendOperation);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newText;
        }

        #endregion

        #region Delete Instance from Game

        public void DeleteInstancesByGame(List<INameable> instances)
        {
            // Vic June 27, 2021
            // this sends a command to Glue to delete the object, but doesn't
            // actually delete it in game until Glue tells the game to get rid
            // of it. Is that okay? it's a little slower, but it works. Maybe at
            // some point in the future I'll find a reason why it needs to be immediate.
            // Update - January 16, 2022
            // This does take a little bit of time, and we can make the game way more responsive
            // by batching.
            var dto = new Dtos.RemoveObjectDto();
            dto.ObjectNames = instances.Select(item => item.Name).ToList();

            GlueControlManager.Self.SendToGlue(dto);
        }

        #endregion

        public void AssignVariable(object instance, FlatRedBall.Content.Instructions.InstructionSave instruction, bool convertFileNamesToObjects)
        {
            string variableName = instruction.Member;
            object variableValue = instruction.Value;

            Type stateType = VariableAssignmentLogic.TryGetStateType(instruction.Type);

            var valueAsString = variableValue as string;

            if (variableValue is string)
            {
                // only convert this if the instance is 
                variableValue = VariableAssignmentLogic.ConvertStringToType(instruction.Type, valueAsString, stateType != null, convertFileNamesToObjects);
            }
            else if (stateType != null && variableValue is string && !string.IsNullOrWhiteSpace(valueAsString))
            {
                var fieldInfo = stateType.GetField(valueAsString);

                variableValue = fieldInfo.GetValue(null);
            }
            else if (instruction.Type == "int")
            {
                if (variableValue is long asLong)
                {
                    variableValue = (int)asLong;
                }
            }
            else if (instruction.Type == "int?")
            {
                if (variableValue is long asLong)
                {
                    variableValue = (int?)asLong;
                }
            }
            else if (instruction.Type == "float" || instruction.Type == "Single")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (float)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (float)asDouble;
                }
            }
            else if (instruction.Type == "float?")
            {
                if (variableValue is int asInt)
                {
                    variableValue = (float?)asInt;
                }
                else if (variableValue is double asDouble)
                {
                    variableValue = (float?)asDouble;
                }
            }
            else if (instruction.Type == typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName ||
                instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName)
            {
                if (convertFileNamesToObjects && variableValue is string asString && !string.IsNullOrWhiteSpace(asString))
                {
                    variableValue = Editing.VariableAssignmentLogic.ConvertStringToType(instruction.Type, asString, false);
                }
            }
            else if (instruction.Type == typeof(Microsoft.Xna.Framework.Color).FullName)
            {
                if (variableValue is string asString && !string.IsNullOrWhiteSpace(asString))
                {
                    variableValue = Editing.VariableAssignmentLogic.ConvertStringToType(instruction.Type, asString, false);
                }
            }
            else if (instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.TextureAddressMode).Name)
            {
                if (variableValue is int asInt)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asInt;
                }
                if (variableValue is long asLong)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asLong;
                }
            }

            try
            {
                FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(instance, variableName, variableValue);
            }
            catch (MemberAccessException)
            {
                // for info on why this exception is caught, search for MemberAccessException in this file
            }
        }

        public void DestroyDynamicallyAddedInstances()
        {
            for (int i = ShapesAddedAtRuntime.AxisAlignedRectangles.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.AxisAlignedRectangles[i]);
            }

            for (int i = ShapesAddedAtRuntime.Circles.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.Circles[i]);
            }

            for (int i = ShapesAddedAtRuntime.Polygons.Count - 1; i > -1; i--)
            {
                ShapeManager.Remove(ShapesAddedAtRuntime.Polygons[i]);
            }


            for (int i = SpritesAddedAtRuntime.Count - 1; i > -1; i--)
            {
                SpriteManager.RemoveSprite(SpritesAddedAtRuntime[i]);
            }

            for (int i = TextsAddedAtRuntime.Count - 1; i > -1; i--)
            {
                TextManager.RemoveText(TextsAddedAtRuntime[i]);
            }

            for (int i = DestroyablesAddedAtRuntime.Count - 1; i > -1; i--)
            {
                DestroyablesAddedAtRuntime[i].Destroy();
            }

            foreach (var list in ListsAddedAtRuntime)
            {
                for (int i = list.Count - 1; i > -1; i--)
                {
                    var positionedObject = list[i] as PositionedObject;
                    positionedObject.RemoveSelfFromListsBelongingTo();
                }
            }

#if HasGum

            for (int i = GumObjectsAddedAtRuntime.Count - 1; i > -1; i--)
            {
                GumObjectsAddedAtRuntime[i].Destroy();
            }
            for(int i = GumWrappersAddedAtRuntime.Count - 1; i > -1; i--)
            {
                GumWrappersAddedAtRuntime[i].RemoveSelfFromListsBelongingTo();
            }
            GumObjectsAddedAtRuntime.Clear();
            GumWrappersAddedAtRuntime.Clear();
#endif

            ShapesAddedAtRuntime.Clear();
            SpritesAddedAtRuntime.Clear();
            DestroyablesAddedAtRuntime.Clear();
            ListsAddedAtRuntime.Clear();
            TextsAddedAtRuntime.Clear();
        }
    }
}
