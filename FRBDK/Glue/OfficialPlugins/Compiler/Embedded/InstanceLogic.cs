{CompilerDirectives}
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

        public ShapeCollection ShapesAddedAtRuntime = new ShapeCollection();

        public FlatRedBall.Math.PositionedObjectList<Sprite> SpritesAddedAtRuntime = new FlatRedBall.Math.PositionedObjectList<Sprite>();


        // this is to prevent multiple objects from having the same name in the same frame:
        static long NewIndex = 0;

        #endregion

        public object HandleCreateInstanceCommandFromGlue(Models.NamedObjectSave deserialized)
        {

            PositionedObject newPositionedObject = null;
            object newObject = null;

            if (deserialized.SourceType == GlueControl.Models.SourceType.Entity)
            {
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(deserialized.SourceClassType);
                newPositionedObject = factory?.CreateNew() as FlatRedBall.PositionedObject;
            }
            else if (deserialized.SourceType == GlueControl.Models.SourceType.FlatRedBallType)
            {
                switch (deserialized.SourceClassType)
                {
                    case "FlatRedBall.Math.Geometry.AxisAlignedRectangle":
                        var aaRect = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddAxisAlignedRectangle(aaRect);
                            ShapesAddedAtRuntime.Add(aaRect);
                        }
                        newPositionedObject = aaRect;

                        break;
                    case "FlatRedBall.Math.Geometry.Circle":
                        var circle = new FlatRedBall.Math.Geometry.Circle();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddCircle(circle);
                            ShapesAddedAtRuntime.Add(circle);
                        }
                        newPositionedObject = circle;
                        break;
                    case "FlatRedBall.Math.Geometry.Polygon":
                        var polygon = new FlatRedBall.Math.Geometry.Polygon();
                        if (deserialized.AddToManagers)
                        {
                            ShapeManager.AddPolygon(polygon);
                            ShapesAddedAtRuntime.Add(polygon);
                        }
                        newPositionedObject = polygon;
                        break;
                    case "FlatRedBall.Sprite":
                        var sprite = new FlatRedBall.Sprite();
                        if(deserialized.AddToManagers)
                        {
                            SpriteManager.AddSprite(sprite);
                            SpritesAddedAtRuntime.Add(sprite);
                        }
                        newPositionedObject = sprite;

                        break;
                }
            }
            if (newPositionedObject != null)
            {
                newPositionedObject.Name = deserialized.InstanceName;
                newPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                newPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
                newPositionedObject.CreationSource = "Glue"; // Glue did make this, so do this so the game can select it

                foreach (var instruction in deserialized.InstructionSaves)
                {
                    
                    AssignVariable(newPositionedObject, instruction);

                }
            }

            newObject = newPositionedObject;

            return newObject;
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

        string GetNameFor(string itemType)
        {
            var newName = $"{itemType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}_{NewIndex}";
            NewIndex++;

            return newName;
        }

        private static void SendAndEnqueue(Dtos.AddObjectDto addObjectDto)
        {
            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            if (currentScreen is Screens.EntityViewingScreen entityViewingScreen)
            {
                addObjectDto.ElementName = entityViewingScreen.CurrentEntity.GetType().FullName;
            }
            else
            {
                addObjectDto.ElementName = currentScreen.GetType().FullName;
            }

            GlueControlManager.Self.SendToGlue(addObjectDto);

            GlueControlManager.Self.EnqueueToOwner(
                nameof(Dtos.AddObjectDto) + ":" + Newtonsoft.Json.JsonConvert.SerializeObject(addObjectDto), addObjectDto.ElementName);
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


            if (ShapeManager.AutomaticallyUpdatedShapes.Contains(newRectangle))
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
                AddValue(addObjectDto, nameof(newSprite.TextureAddressMode), nameof(Microsoft.Xna.Framework.Graphics.TextureAddressMode), (int)newSprite.TextureAddressMode);
            }

            #endregion

            SendAndEnqueue(addObjectDto);

            return newSprite;
        }

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

        private void AssignVariable(PositionedObject instance, FlatRedBall.Content.Instructions.InstructionSave instruction)
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
