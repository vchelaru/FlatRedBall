using FlatRedBall;
using FlatRedBall.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl
{
    public class InstanceLogic
    {
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

        public void Update()
        {

        }


        public FlatRedBall.PositionedObject CreateEntity(string entityType, float x, float y)
        {
            var newName = $"{entityType}Auto{TimeManager.CurrentTime.ToString().Replace(".", "_")}";

            var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(entityType);
            var cursor = GuiManager.Cursor;
            var toReturn = factory.CreateNew(x, y) as FlatRedBall.PositionedObject;
            toReturn.Name = newName;

            var nos = new Models.NamedObjectSave();
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

            GlueControlManager.Self.SendCommandToGlue($"AddObject:{Newtonsoft.Json.JsonConvert.SerializeObject(nos)}");

            return toReturn;
        }
    }
}
