using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArrowDataConversion;
using FlatRedBall.Content.Scene;
using NUnit.Framework;

namespace ArrowUnitTests.DataConversion
{
    [TestFixture]
    public class SpriteSaveConverterTests
    {
        [Test]
        public void TestConversion()
        {
            SpriteSave spriteSave = new SpriteSave();
            SpriteSaveConverter converter = new SpriteSaveConverter();

            var nos = converter.SpriteSaveToNamedObjectSave(spriteSave);

            if (nos.InstructionSaves.Count != 0)
            {
                throw new Exception("A default SpriteSave should have no properties");
            }

            spriteSave.X = 4;
            nos = converter.SpriteSaveToNamedObjectSave(spriteSave);
            if (nos.InstructionSaves.Count != 1)
            {
                throw new Exception("A SpriteSave with non-zero X should have 1 property");
            }

            spriteSave.Texture = "Folder/filename.png";
            nos = converter.SpriteSaveToNamedObjectSave(spriteSave);
            if (nos.GetCustomVariable("Texture").Value as string != "filename")
            {
                throw new Exception("NOS's are not getting the right file name out of SpriteSaves");
            }
        }
    }
}
