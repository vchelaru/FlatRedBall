using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArrowDataConversion;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Content.Scene;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using NUnit.Framework;

namespace ArrowUnitTests.DataConversion
{
    [TestFixture]
    public class ArrowProjectToGlueProjectConverterTests
    {
        [Test]
        public void TestConversion()
        {
            ArrowProjectSave project = new ArrowProjectSave();

            SpriteSave spriteSave = new SpriteSave();
            spriteSave.X = 3;
            spriteSave.Y = 4;
            spriteSave.Name = "SpriteInstance";
            spriteSave.Texture = "Entities/FirstElement/redball.BMP";
            

            ArrowElementSave element = new ArrowElementSave();
            element.Name = "FirstElement";
            element.Sprites.Add(spriteSave);
            project.Elements.Add(element);


            element = new ArrowElementSave();
            element.Name = "ContainerOfFirstElement";
            element.Sprites.Add(spriteSave);
            project.Elements.Add(element);

            ArrowElementInstance instance = new ArrowElementInstance();
            instance.Name = "FirstElementInstance";
            instance.Type = "FirstElement";
            instance.SetVariable("X", 4);
            instance.SetVariable("Y", 5);
            element.ElementInstances.Add(instance);



            ArrowProjectToGlueProjectConverter converter = new ArrowProjectToGlueProjectConverter();

            GlueProjectSave gps = converter.ToGlueProjectSave(project);

            EntitySave firstElementEntity = gps.Entities.FirstOrDefault(item => item.Name == "Entities/FirstElement");
            EntitySave containerOfFirstElement = gps.Entities.FirstOrDefault(item => item.Name == "Entities/ContainerOfFirstElement");
            if (firstElementEntity.Name.StartsWith("Entities/") == false)
            {
                throw new Exception("Entity names must start with \"Entities/\"");
            }

            if (firstElementEntity.ReferencedFiles.Count == 0)
            {
                throw new Exception("The Entity should automatically contain a ReferencedFile for the redball file");
            }

            if (containerOfFirstElement.NamedObjects.FirstOrDefault(item => item.InstanceName == "FirstElementInstance") == null)
            {
                throw new Exception("The entity should contain a NOS for another element, but it doesn't");
            }

            string gluxString;

            FileManager.XmlSerialize(gps, out gluxString);

            string aroxString;
            FileManager.XmlSerialize(project, out aroxString);

        }
    }
}
