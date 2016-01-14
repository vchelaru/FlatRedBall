using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Particle;

namespace ParticleEditorControls.Managers
{
    public class ProjectManager : Singleton<ProjectManager>
    {
        EmitterSaveList mEmitterSaveList;
        string mLastLoaded;

        public string FileName
        {
            get { return mLastLoaded; }
        }

        public EmitterSaveList EmitterSaveList
        {
            get
            {
                return mEmitterSaveList;
            }
        }

        public void Initialize()
        {

        }

        public void Load(string fileName)
        {
            // Not sure why this check is here - we want to be able to load and reload all the time
            //if (mEmitterSaveList != null)
            //{
            //    System.Diagnostics.Debugger.Break();
            //    throw new Exception();
            //}

            mEmitterSaveList = EmitterSaveList.FromFile(fileName);
            foreach (var emitter in mEmitterSaveList.emitters)
            {
                TryUpdateEmitterTextures(emitter);
            }
            
            mLastLoaded = fileName;
            TreeViewManager.Self.RefreshTreeView();
        }

        /// <summary>
        /// Handles updating old particle files that stored textures in
        /// the particle blueprint object. If the emitterSave has a 
        /// texture but the emissionSettings does not, it will
        /// copy the texture from emitterSave to emissionSettings
        /// </summary>
        /// <param name="emitterSave">The emitter save to check.</param>
        private void TryUpdateEmitterTextures(EmitterSave emitterSave)
        {
            if (emitterSave != null &&
                emitterSave.ParticleBlueprint != null &&
                !string.IsNullOrEmpty(emitterSave.ParticleBlueprint.Texture) &&
                string.IsNullOrEmpty(emitterSave.EmissionSettings.Texture))
            {
                emitterSave.EmissionSettings.Texture = emitterSave.ParticleBlueprint.Texture;
            }
        }



        internal void SaveLastLoaded()
        {
            if (string.IsNullOrEmpty(mLastLoaded) ||
                mEmitterSaveList == null)
            {
                throw new Exception("There is no loaded Emitter");
            }

            mEmitterSaveList.Save(mLastLoaded);
        }
    }
}
