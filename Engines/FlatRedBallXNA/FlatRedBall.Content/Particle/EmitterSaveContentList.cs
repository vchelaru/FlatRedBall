using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using System.Xml.Serialization;

namespace FlatRedBall.Content.Particle
{
    [XmlRoot("ArrayOfEmitterSave")]
    public class EmitterSaveContentList
    {

        [XmlElement("EmitterSave")]
        public List<EmitterSaveContent> emitters;

        public FlatRedBall.Math.CoordinateSystem CoordinateSystem;

        // This is included so that the EmitterSaveList and EmitterSaveContentList match members
        [XmlIgnore]
        public string Name
        {
            get;
            set;
        }

        [XmlIgnore]
        public EmitterSaveContent this[int key]
        {
            get
            {
                return emitters[key];
            }

            set
            {
                emitters[key] = value;
            }
        }

        public static EmitterSaveContentList FromFile(string fileName)
        {
            EmitterSaveContentList emitterSaveContentListToReturn =
                FileManager.XmlDeserialize<EmitterSaveContentList>(fileName);

            string directory = FileManager.GetDirectory(fileName);

            string oldRelativeDirectory = FileManager.RelativeDirectory;

            FileManager.RelativeDirectory = directory;

            foreach (EmitterSaveContent es in emitterSaveContentListToReturn.emitters)
            {
                if (FileManager.IsRelative(es.ParticleBlueprint.Texture))
                {
                    es.ParticleBlueprint.Texture =
                        FileManager.MakeAbsolute(es.ParticleBlueprint.Texture);
                }

            }

            FileManager.RelativeDirectory = oldRelativeDirectory;

            return emitterSaveContentListToReturn;
        }
        /*
        public void Save(string fileName)
        {
            MakeAssetsRelative(fileName);


            FileManager.XmlSerialize(this, fileName);
        }*/

       /* public EmitterList ToEmitterList(string contentManagerName)
        {
            EmitterList emitterList = new EmitterList();

            foreach (EmitterSave es in this)
            {
                emitterList.Add(es.ToEmitter(contentManagerName));
            }

            return emitterList;
        }*/

        /*
        private void MakeAssetsRelative(string fileName)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;

            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }
            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);

            foreach (EmitterSave es in this)
            {
                es.ParticleBlueprint.Texture = FileManager.MakeRelative(es.ParticleBlueprint.Texture);
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;


        }*/






    }
}
