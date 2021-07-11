{CompilerDirectives}
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Screens;

using {ProjectNamespace}.GlueControl.Models;


namespace {ProjectNamespace}.GlueControl
{
    public class InstanceLogic
    {
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

        public List<ShapeCollection> ShapeCollectionsAddedAtRuntime = new List<ShapeCollection>();
        public ShapeCollection ShapesAddedAtRuntime = new ShapeCollection();

        public FlatRedBall.Math.PositionedObjectList<Sprite> SpritesAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Sprite>();

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
            if(CustomGlueElements.ContainsKey(elementGameType))
            {
                ownerElement = CustomGlueElements[elementGameType];
            }

            var addedToEntity =
                (ownerType != null && typeof(PositionedObject).IsAssignableFrom(ownerType))
                ||
                ownerElement != null && ownerElement is EntitySave;

            if(addedToEntity)
            {
                if(forcedItem != null)
                {
                    if (CommandReceiver.DoTypesMatch(forcedItem, elementGameType))
                    {
                        HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, forcedItem);
                    }
                }
                else
                {
                    // need to loop through every object and see if it is an instance of the entity type, and if so, add this object to it
                    for(int i = 0; i < SpriteManager.ManagedPositionedObjects.Count; i++)
                    {
                        var item = SpriteManager.ManagedPositionedObjects[i];
                        if(CommandReceiver.DoTypesMatch(item, elementGameType))
                        {
                            HandleCreateInstanceCommandFromGlueInner(dto, currentAddObjectIndex, item);
                        }
                    }
                }
            }
            else if(forcedItem == null && ScreenManager.CurrentScreen.GetType().FullName == elementGameType)
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
                newPositionedObject = CreateEntity(deserialized);

                var sourceClassTypeGame = CommandReceiver.GlueToGameElementName(deserialized.SourceClassType);

                for(int i = 0; i < currentAddObjectIndex; i++)
                {
                    var dto = CommandReceiver.GlobalGlueToGameCommands[i];
                    if(dto is Dtos.AddObjectDto addObjectDtoRerun)
                    {
                        HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, currentAddObjectIndex, newPositionedObject);
                    }
                    else if(dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                    {
                        GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, newPositionedObject);
                    }
                }
            }
            else if(deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType &&
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
                        var aaRect = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddAxisAlignedRectangle(aaRect);
                            ShapesAddedAtRuntime.Add(aaRect);
                        }
                        newPositionedObject = aaRect;

                        break;
                    case "FlatRedBall.Math.Geometry.Circle":
                    case "Circle":
                        var circle = new FlatRedBall.Math.Geometry.Circle();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddCircle(circle);
                            ShapesAddedAtRuntime.Add(circle);
                        }
                        newPositionedObject = circle;
                        break;
                    case "FlatRedBall.Math.Geometry.Polygon":
                    case "Polygon":
                        var polygon = new FlatRedBall.Math.Geometry.Polygon();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddPolygon(polygon);
                            ShapesAddedAtRuntime.Add(polygon);
                        }
                        newPositionedObject = polygon;
                        break;
                    case "FlatRedBall.Sprite":
                    case "Sprite":
                        var sprite = new FlatRedBall.Sprite();
                        if(deserialized.AddToManagers)
                        {
                            SpriteManager.AddSprite(sprite);
                            SpritesAddedAtRuntime.Add(sprite);
                        }
                        newPositionedObject = sprite;

                        break;
                    case "FlatRedBall.Math.Geometry.ShapeCollection":
                    case "ShapeCollection":
                        var shapeCollection = new ShapeCollection();
                        ShapeCollectionsAddedAtRuntime.Add(shapeCollection);
                        newObject = shapeCollection;
                        break;
                }
            }
            if(newPositionedObject != null)
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

        private object TryCreateCollisionRelationship(Models.NamedObjectSave deserialized)
        {
            var type = Editing.VariableAssignmentLogic.GetDesiredRelationshipType(deserialized, out object firstObject, out object secondObject);
            if(type == null)
            {
                return null;
            }
            else
            {
                object toReturn = null;
                // can we make a new one here?
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    List<object> parameters = new List<object>();
                    if(firstObject != null)
                    {
                        parameters.Add(firstObject);
                    }
                    if(secondObject != null)
                    {
                        parameters.Add(secondObject);
                    }
                    var collisionRelationship =
                        constructor.Invoke(parameters.ToArray()) as FlatRedBall.Math.Collision.CollisionRelationship;
                    toReturn = collisionRelationship;
                    FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(collisionRelationship);
                }
                return toReturn;
            }
        }


        public PositionedObject CreateEntity(Models.NamedObjectSave deserialized)
        {
            var entityNameGlue = deserialized.SourceClassType;
            return CreateEntity(entityNameGlue);
        }

        public PositionedObject CreateEntity(string entityNameGlue)
        {
            var entityNameGame = CommandReceiver.GlueToGameElementName(entityNameGlue);

            if (CustomGlueElements.ContainsKey(entityNameGame))
            {
                var dynamicEntityInstance = new Runtime.DynamicEntity();
                dynamicEntityInstance.EditModeType = entityNameGame;
                SpriteManager.AddPositionedObject(dynamicEntityInstance);
                return dynamicEntityInstance;
            }
            else
            {
                PositionedObject newPositionedObject;
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityNameGlue);
                if(factory != null)
                {
                    newPositionedObject = factory?.CreateNew() as FlatRedBall.PositionedObject;
                }
                else
                {
                    // just instantiate it using reflection?
                    newPositionedObject = this.GetType().Assembly.CreateInstance(entityNameGame)
                         as PositionedObject;
                    //newPositionedObject = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                }
                return newPositionedObject;
            }
        }

        private void AssignVariablesOnNewlyCreatedObject(Models.NamedObjectSave deserialized, object newObject)
        {
            if (newObject is FlatRedBall.Utilities.INameable asNameable)
            {
                asNameable.Name = deserialized.InstanceName;
            }
            if(newObject is PositionedObject asPositionedObject)
            {
                asPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                asPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                asPositionedObject.CreationSource = "Glue"; // Glue did make this, so do this so the game can select it
            }

            foreach (var instruction in deserialized.InstructionSaves)
            {
                AssignVariable(newObject, instruction);
            }
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

            CommandReceiver.EnqueueToOwner(addObjectDto, addObjectDto.ElementNameGame);
        }

        #region Create Instance from Game

        private string GetNameFor(string itemType)
        {
            var newName = $"{itemType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}_{NewIndex}";
            NewIndex++;

            return newName;
        }

        private void AddFloatValue(Dtos.AddObjectDto addObjectDto, string name, float value)
        {
            AddValue(addObjectDto, name, "float", value);
        }

        private void AddStringValue(Dtos.AddObjectDto addObjectDto, string name, string value)
        {
            AddValue(addObjectDto, name, "string", value);
        }

        private void AddValue(Dtos.AddObjectDto addObjectDto, string name, string type, object value)
        {
            addObjectDto.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
            {
                Member = name,
                Type = type,
                Value = value
            });
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityType, float x, float y)
        {
            var newName = GetNameFor(entityType);

            var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityType);

            var cursor = GuiManager.Cursor;
            var toReturn = factory.CreateNew(x, y) as FlatRedBall.PositionedObject;
            toReturn.Name = newName;

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = $"Entities\\{entityType}";

            AddFloatValue(addObjectDto, "X", x);
            AddFloatValue(addObjectDto, "Y", y);

            #endregion

            SendAndEnqueue(addObjectDto);

            return toReturn;
        }

        public Circle HandleCreateCircleByGame(Circle originalCircle)
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

        public AxisAlignedRectangle HandleCreateAxisAlignedRectangleByGame(AxisAlignedRectangle originalRectangle)
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

        public Polygon HandleCreatePolygonByGame(Polygon originalPolygon)
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
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Math.Geometry.Polygon";

            AddFloatValue(addObjectDto, "X", newPolygon.X);
            AddFloatValue(addObjectDto, "Y", newPolygon.Y);

            AddValue(addObjectDto, "Points", typeof(List<Point>).ToString(),
                Newtonsoft.Json.JsonConvert.SerializeObject(newPolygon.Points.ToList()));

            #endregion

            SendAndEnqueue(addObjectDto);

            return newPolygon;
        }

        public Sprite HandleCreateSpriteByName(Sprite originalSprite)
        {
            var newSprite = originalSprite.Clone();
            var newName = GetNameFor("Sprite");

            newSprite.Name = newName;

            if(SpriteManager.AutomaticallyUpdatedSprites.Contains(originalSprite))
            {
                SpriteManager.AddSprite(newSprite);
            }
            InstanceLogic.Self.SpritesAddedAtRuntime.Add(newSprite);

            #region Create the AddObjectDto for the new object

            var addObjectDto = new Dtos.AddObjectDto();
            addObjectDto.InstanceName = newName;
            addObjectDto.SourceType = Models.SourceType.FlatRedBallType;
            // todo - need to eventually include sub namespaces for entities in folders
            addObjectDto.SourceClassType = "FlatRedBall.Sprite";

            AddFloatValue(addObjectDto, "X", newSprite.X);
            AddFloatValue(addObjectDto, "Y", newSprite.Y);
            if(newSprite.TextureScale > 0)
            {
                AddFloatValue(addObjectDto, nameof(newSprite.TextureScale), newSprite.TextureScale);
            }
            else
            {
                AddFloatValue(addObjectDto, nameof(newSprite.Width), newSprite.Width);
                AddFloatValue(addObjectDto, nameof(newSprite.Height), newSprite.Height);
            }


            if(newSprite.Texture != null)
            {
                // Texture must be assigned before pixel values.
                AddValue(addObjectDto, "Texture", typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName, 
                    newSprite.Texture.Name);

                // Glue uses the pixel coords, but we can check the coordinates more easily
                if(newSprite.LeftTextureCoordinate != 0)
                {
                    AddFloatValue(addObjectDto, nameof(newSprite.LeftTexturePixel), newSprite.LeftTexturePixel);
                }
                if(newSprite.TopTextureCoordinate != 0)
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
            if(newSprite.AnimationChains?.Name != null)
            {
                AddValue(addObjectDto, "AnimationChains", typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName,
                    newSprite.AnimationChains.Name);
            }
            if (!string.IsNullOrEmpty(newSprite.CurrentChainName))
            {
                AddStringValue(addObjectDto, "CurrentChainName", newSprite.CurrentChainName);
            }
            if(newSprite.TextureAddressMode != Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp)
            {
                AddValue(addObjectDto, nameof(newSprite.TextureAddressMode), 
                    nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode), (int)newSprite.TextureAddressMode);
            }

            // do we want to consider animated sprites? Does it matter?
            // An animation could flip this and that would incorrectly set
            // that value on Glue but if it's animated that would get overwritten anyway, so maybe it's no biggie?
            if(newSprite.FlipHorizontal != false)
            {
                AddValue(addObjectDto, nameof(newSprite.FlipHorizontal), 
                    "bool", newSprite.FlipHorizontal);
            }
            if (newSprite.FlipVertical != false)
            {
                AddValue(addObjectDto, nameof(newSprite.FlipVertical),
                    "bool", newSprite.FlipVertical);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newSprite;
        }

        #endregion

        #region Delete Instance from Game
        
        public void DeleteInstanceByGame(PositionedObject positionedObject)
        {
            // Vic June 27, 2021
            // this sends a command to Glue to delete the object, but doesn't
            // actually delete it in game until Glue tells the game to get rid
            // of it. Is that okay? it's a little slower, but it works. Maybe at
            // some point in the future I'll find a reason why it needs to be immediate.
            var name = positionedObject.Name;

            var dto = new Dtos.RemoveObjectDto();
            dto.ObjectName = positionedObject.Name;

            GlueControlManager.Self.SendToGlue(dto);
        }

        #endregion

        private void AssignVariable(object instance, FlatRedBall.Content.Instructions.InstructionSave instruction)
        {
            string variableName = instruction.Member;
            object variableValue = instruction.Value;

            if (instruction.Type == "float")
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
            else if(instruction.Type == typeof(FlatRedBall.Graphics.Animation.AnimationChainList).FullName ||
                instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName)
            {
                if(variableValue is string asString && !string.IsNullOrWhiteSpace(asString))
                {
                    variableValue = Editing.VariableAssignmentLogic.ConvertStringToType(instruction.Type, asString);
                }
            }
            else if(instruction.Type == typeof(Microsoft.Xna.Framework.Graphics.TextureAddressMode).Name)
            {
                if(variableValue is int asInt)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asInt;
                }
                if (variableValue is long asLong)
                {
                    variableValue = (Microsoft.Xna.Framework.Graphics.TextureAddressMode)asLong;
                }
            }

            FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(instance, variableName, variableValue);
        }

        public void DestroyDynamicallyAddedInstances()
        {
            for(int i = ShapesAddedAtRuntime.AxisAlignedRectangles.Count-1; i > -1; i--)
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

            for(int i = SpritesAddedAtRuntime.Count - 1; i > -1; i--)
            {
                SpriteManager.RemoveSprite(SpritesAddedAtRuntime[i]);
            }
        }
    }
}
