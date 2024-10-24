using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;

using System.Xml.Serialization;
using System.IO;

using FlatRedBall.Content.Scene;

using FlatRedBall.Graphics.Particle;

using FlatRedBall.IO;

using FlatRedBall.Math;
using System.Collections;

namespace FlatRedBall.Content.Particle
{
    [XmlRoot("ArrayOfEmitterSave")]
    public class EmitterSaveList : ISaveableContent
    {
        #region Fields

        [XmlElement("EmitterSave")]
        public List<EmitterSave> emitters;

        public FlatRedBall.Math.CoordinateSystem CoordinateSystem;



        // STOP!  If you add anything here, be sure to add it to EmitterSaveContentList

        #endregion

        #region Properties

        public EmitterSave this[int key]
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


        [XmlIgnore]
        public string Name
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructor

        public EmitterSaveList() 
        { 
            emitters = new List<EmitterSave>();

            // This is so that the default is LeftHanded when loading from .emix.
            // If this property is present in the XML this will get overwritten.
            // If the user is instantiating a EmitterSave to save an .emix from
            // FlatRedBall XNA, the Save method will overwrite this so that the coordinate
            // system is RightHanded.
            this.CoordinateSystem = FlatRedBall.Math.CoordinateSystem.LeftHanded;
        }

        #endregion

        #region Public Static "From" Methods

        [Obsolete]
        public static EmitterSaveList FromEmitterList(IEnumerable<Emitter> emittersToSave)
        {
            EmitterSaveList emitterSaveList = new EmitterSaveList();
            
            foreach (Emitter emitter in emittersToSave)
            {
                EmitterSave emitterSave = EmitterSave.FromEmitter(emitter);
                emitterSaveList.emitters.Add(emitterSave);
            }

            return emitterSaveList;
        }

        public static EmitterSaveList FromFile(string fileName)
        {


            EmitterSaveList emitterSaveListToReturn =
                FileManager.XmlDeserialize<EmitterSaveList>(fileName);

            emitterSaveListToReturn.Name = fileName;

            if (FileManager.IsRelative(emitterSaveListToReturn.Name))
            {
                emitterSaveListToReturn.Name = FileManager.MakeAbsolute(emitterSaveListToReturn.Name);
            }

            foreach (EmitterSave es in emitterSaveListToReturn.emitters)
            {
                es.FileName = fileName;
            }
            
            if (emitterSaveListToReturn.CoordinateSystem == CoordinateSystem.LeftHanded)
            {
                emitterSaveListToReturn.InvertZ();
                emitterSaveListToReturn.CoordinateSystem = CoordinateSystem.RightHanded;
            }

            return emitterSaveListToReturn;
        }

        #endregion

        public void InvertZ()
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                EmitterSave emitter = emitters[i];

                emitter.InvertHandedness();

            }

        }

        public void Save(string fileName)
        {
            CoordinateSystem = FlatRedBall.Math.CoordinateSystem.RightHanded;

            MakeAssetsRelative(fileName);

            FileManager.XmlSerialize(this, fileName);
        }

        /// <summary>
        /// Returns a List of files referenced by this.
        /// </summary>
        /// <param name="relativeType">Whether the files should be absolute or relative.</param>
        /// <returns>The list of referenced files.</returns>
		public List<string> GetReferencedFiles(RelativeType relativeType)
		{
            if (string.IsNullOrEmpty(this.Name))
            {
                throw new InvalidOperationException("The EmitterSaveList's " +
                    "Name is null.  You must set this to the file associated with " +
                    "this instance.");
            }

			List<string> referencedFiles = new List<string>();

			for (int i = 0; i < this.emitters.Count; i++)
			{
				EmitterSave emitterSave = this.emitters[i];
                if (!string.IsNullOrEmpty(emitterSave.EmissionSettings.Texture))
                {
                    referencedFiles.Add(emitterSave.EmissionSettings.Texture);
                }

                if (!string.IsNullOrEmpty(emitterSave.EmissionSettings.AnimationChains))
                {
                    referencedFiles.Add(emitterSave.EmissionSettings.AnimationChains);
                }

                // Justin Johnson, 04/2015 - Retired particle blueprint system
                //if (emitterSave.ParticleBlueprint != null)
                //{
                //    emitterSave.ParticleBlueprint.GetReferencedFiles(referencedFiles);
                //}
			}

			string directory = FileManager.GetDirectory(this.Name);

			if (relativeType == RelativeType.Absolute)
			{
				for (int i = 0; i < referencedFiles.Count; i++)
				{
                    if (FileManager.IsRelative(referencedFiles[i]))
                    {
                        referencedFiles[i] = directory + referencedFiles[i];
                    }
				}
			}

			return referencedFiles;
		}

        [Obsolete]
        public EmitterList ToEmitterList(string contentManagerName)
        {
			string oldRelativeDirectory = FileManager.RelativeDirectory;

			FileManager.RelativeDirectory = FileManager.GetDirectory(this.Name);

            EmitterList emitterList = new EmitterList();

            foreach (EmitterSave es in this.emitters)
            {
                emitterList.Add(es.ToEmitter(contentManagerName));
            }

			FileManager.RelativeDirectory = oldRelativeDirectory;

            return emitterList;
        }

        public void MakeAssetsRelative(string fileName)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;

            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }
            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);

            // Justin Johnson 04/2015: Retired particle blueprint
            // TODO: not sure if anything needs to replace this.
            foreach (EmitterSave es in this.emitters)
            {
                if (!string.IsNullOrEmpty(es.EmissionSettings.AnimationChains))
                {
                    es.EmissionSettings.AnimationChains = FileManager.MakeRelative(es.EmissionSettings.AnimationChains);
                }

                if (!string.IsNullOrEmpty(es.EmissionSettings.Texture))
                {
                    es.EmissionSettings.Texture = FileManager.MakeRelative(es.EmissionSettings.Texture);
                }

                //if (es.ParticleBlueprint != null)
                //{
                //    es.ParticleBlueprint.MakeRelative();
                //}
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;


        }

        public float GetEquilibriumParticleCount()
        {
            bool canHaveEquilibrium = true;

            foreach (EmitterSave es in this.emitters)
            {
                if (es.TimedEmission == false ||
                    (es.RemovalEvent != "Fadeout" && es.RemovalEvent != "Alpha0" && es.RemovalEvent != "Timed"))
                {
                    canHaveEquilibrium = false;
                }
            }

            if (canHaveEquilibrium)
            {
                float equilibriumValue = 0;
                foreach (EmitterSave es in this.emitters)
                {
                    equilibriumValue += es.GetEquilibriumParticleCount();
                }

                return equilibriumValue;
            }
            else
            {
                return -1;
            }

        }

        public int GetBurstParticleCount()
        {
            int value = 0;
            foreach (EmitterSave es in this.emitters)
            {
                value += es.NumberPerEmission;
            }
            return value;
        }

        #endregion
    }
}
