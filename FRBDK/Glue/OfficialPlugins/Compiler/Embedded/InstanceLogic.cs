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

        static HashSet<string> floatVariables = new HashSet<string>
        {
            "X",
            "Y",
            "Z",
            "Width",
            "Height",
            "TextureScale",
            "Radius"
        };

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
                    var variableName = instruction.Member;
                    var variableValue = instruction.Value;

                    AssignVariable(newPositionedObject, variableName, variableValue);

                }
            }

            newObject = newPositionedObject;

            return newObject;
        }

        public FlatRedBall.PositionedObject CreateInstanceByGame(string entityType, float x, float y)
        {
            var newName = $"{entityType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}";

            var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityType);
            
            var cursor = GuiManager.Cursor;
            var toReturn = factory.CreateNew(x, y) as FlatRedBall.PositionedObject;
            toReturn.Name = newName;

            var nos = new Dtos.AddObjectDto();
            nos.InstanceName = newName;
            nos.SourceType = Models.SourceType.Entity;
            // todo - need to eventually include sub namespaces
            nos.SourceClassType = $"Entities\\{entityType}";
            nos.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
            {
                Member = "X",
                Type = "float",
                Value = x
            });

            nos.InstructionSaves.Add(new FlatRedBall.Content.Instructions.InstructionSave
            {
                Member = "Y",
                Type = "float",
                Value = y
            });

            var currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            if(currentScreen is Screens.EntityViewingScreen entityViewingScreen)
            {
                nos.ElementName = entityViewingScreen.CurrentEntity.GetType().FullName;
            }
            else
            {
                nos.ElementName = currentScreen.GetType().FullName;
            }

            GlueControlManager.Self.SendToGlue(nos);

            GlueControlManager.Self.EnqueueToOwner(
                nameof(Dtos.AddObjectDto) + ":" + Newtonsoft.Json.JsonConvert.SerializeObject(nos), nos.ElementName);

            return toReturn;
        }

        public void DeleteInstanceByGame(PositionedObject positionedObject)
        {
            var name = positionedObject.Name;

            var dto = new Dtos.RemoveObjectDto();
            dto.ObjectName = positionedObject.Name;

            GlueControlManager.Self.SendToGlue(dto);
        }

        private void AssignVariable(PositionedObject instance, string variableName, object variableValue)
        {
            var shouldBeFloat = floatVariables.Contains(variableName);

            if (shouldBeFloat)
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

            FlatRedBall.Instructions.Reflection.LateBinder.SetValueStatic(instance, variableName, variableValue);
        }

        public void DestroyShapes()
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
        }
    }
}
