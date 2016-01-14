using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.Graphics;
using FlatRedBall.IO;


namespace FlatRedBall.Content.Particle
{
    [ContentImporter(".emix", 
        DisplayName="EmitterList - FlatRedBall",
        DefaultProcessor="EmitterProcessor")]
    class EmitterImporter : ContentImporter<EmitterSaveContentList>
    {
        public override EmitterSaveContentList Import(string fileName, ContentImporterContext context)
        {

            EmitterSaveContentList emitterSaveList = null;

            try
            {
                emitterSaveList = EmitterSaveContentList.FromFile(fileName);
            }
            catch (Exception e)
            {
                string message = "Error trying to process the file " + fileName + " in the content pipeline\n\n" +
                    "Inner exception:\n\n" + e.ToString();

                throw new Exception(message);
            }


            emitterSaveList.Name = fileName;
            
            return emitterSaveList;
        }
    }
}
