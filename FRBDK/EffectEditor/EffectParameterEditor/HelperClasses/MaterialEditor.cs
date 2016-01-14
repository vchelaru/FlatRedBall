using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Content.Model;
using FlatRedBall;
using System.IO;

namespace EffectParameterEditor.HelperClasses
{
    // Stores a material for a part
    public class PartMaterial
    {
        #region Fields

        public ModelMeshPart MeshPart;
        public Effect DefaultEffect;
        public EffectParameterListSave DefaultParameters;

        public string EffectName;
        public string ParametersName;

        public Effect Effect;
        public EffectParameterListSave Parameters;

        #endregion

        #region Constructor

        public PartMaterial(ModelMeshPart part)
        {
            MeshPart = part;
            DefaultEffect = part.Effect;
            DefaultParameters = EffectParameterListSave.FromEffect(part.Effect);

            SetDefaults();
        }

        #endregion

        #region Methods

        public void SetDefaults()
        {
            EffectName = string.Empty;
            ParametersName = string.Empty;

            Effect = DefaultEffect;
            Parameters = DefaultParameters;
        }

        #endregion
    }

    public class MaterialEditor
    {
        #region Fields

        // Dictionaries - associate short names with file names
        Dictionary<string, string> mEffectFilenames = new Dictionary<string, string>();
        Dictionary<string, string> mParametersFilenames = new Dictionary<string, string>();

        // Short names are used below here

        // Dictionaries - associate names with items
        Dictionary<string, Effect> mEffectDictionary = new Dictionary<string, Effect>();
        Dictionary<string, EffectParameterListSave> mParametersDictionary = new Dictionary<string, EffectParameterListSave>();

        // Parts
        List<PartMaterial> mParts = new List<PartMaterial>();

        // Current model
        Model mModel;

        // Invisible effect
        public Effect InvisibleEffect;

        // Associative lists
        Dictionary<string, List<string>> mEffectParameters = new Dictionary<string, List<string>>(); // all parameter lists for an effect

        #endregion

        #region Properties

        public int PartCount
        {
            get { return mParts.Count; }
        }

        public List<string> EffectNames
        {
            get { return new List<string>(mEffectDictionary.Keys); }
        }

        public List<PartMaterial> Parts
        {
            get { return mParts; }
        }

        #endregion

        #region Constructor / Initialization

        public MaterialEditor(Model model)
        {
            SetModel(model);
        }

        #endregion

        #region Methods

        #region Private methods

        private void SetModel(Model model)
        {
            // Store model
            mModel = model;

            // Load parts
            foreach (ModelMesh mesh in mModel.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Add part
                    mParts.Add(new PartMaterial(part));
                }
            }
        }

        #region Helper Methods

        private string ShortName(string fileName)
        {
            return Path.GetFileName(fileName);
        }

        private bool HasParameters(string effectShortName)
        {
            // Check if the parameters exist
            if (!mEffectParameters.ContainsKey(effectShortName)) return false;

            // Check if there are any parameters
            if (mEffectParameters[effectShortName].Count == 0) return false;

            // If we didn't fail, then there are parameters stored for this effect
            return true;
        }

        #endregion

        #endregion

        #region Public Methods

        #region Highlighting

        public void HighlightPart(int partIndex)
        {
            if (InvisibleEffect != null)
            {
                for (int i = 0; i < mParts.Count; i++)
                {
                    mParts[i].MeshPart.Effect = (i == partIndex) ? mParts[i].Effect : InvisibleEffect;
                }
            }
        }

        public void ShowAllParts()
        {
            foreach (PartMaterial part in mParts)
            {
                part.MeshPart.Effect = part.Effect;
            }
        }

        #endregion

        #region Get Lists

        public List<string> ParameterNames(string effectShortName)
        {
            if (!mEffectParameters.ContainsKey(effectShortName)) return new List<string>();
            else
            {
                return mEffectParameters[effectShortName];
            }
        }

        #endregion

        #region Set part information

        public void SetPartEffect(int partIndex, string effectShortName)
        {
            PartMaterial part = mParts[partIndex];
            Effect effect = mEffectDictionary[effectShortName].Clone(FlatRedBallServices.GraphicsDevice);

            // Store effect
            part.EffectName = effectShortName;
            part.Effect = effect;
            part.MeshPart.Effect = effect;

            // Load effect default parameters (or add non-saved parameters if none exist)
            if (HasParameters(effectShortName))
            {
                string parametersShortName = mEffectParameters[effectShortName][0];
                SetPartParameters(partIndex, parametersShortName);
            }
            else
            {
                part.ParametersName = string.Empty;
                part.Parameters = EffectParameterListSave.FromEffect(effect);
            }
        }

        public void SetPartParameters(int partIndex, string parametersShortName)
        {
            PartMaterial part = mParts[partIndex];
            EffectParameterListSave list = mParametersDictionary[parametersShortName];

            // Store parameters and apply them to effect
            part.ParametersName = parametersShortName;
            part.Parameters = list;
            list.ApplyTo(part.Effect);
        }

        public void SetPartDefaults(int partIndex)
        {
            mParts[partIndex].SetDefaults();
        }

        public void SetPartDefaultParameters(int partIndex)
        {
            PartMaterial part = mParts[partIndex];

            part.ParametersName = string.Empty;
            part.Parameters = part.DefaultParameters;
            part.Parameters.ApplyTo(part.Effect);
        }

        public void UpdatePartParameters(int partIndex)
        {
            PartMaterial part = mParts[partIndex];

            part.Parameters.CopyFromEffect(part.Effect);
        }

        #endregion

        #region File Adding Methods

        public string AddEffect(string fileName)
        {
            return AddEffect(fileName, -1);
        }
        public string AddEffect(string fileName, int partIndex)
        {
            // Load the effect
            Effect effect = FlatRedBallServices.Load<Effect>(fileName);

            // Store the effect's short name
            string shortName = ShortName(fileName);
            mEffectFilenames.Add(shortName, fileName);

            // Store the effect
            mEffectDictionary.Add(shortName, effect);
            
            // Create a list of parameters
            mEffectParameters.Add(shortName, new List<string>());

            // Set effect if needed
            if (partIndex >= 0)
            {
                SetPartEffect(partIndex, shortName);
            }

            // Return the short name
            return shortName;
        }

        public string AddParameters(string fileName, string effectShortName)
        {
            return AddParameters(fileName, effectShortName, -1);
        }
        public string AddParameters(string fileName, string effectShortName, int partIndex)
        {
            if (!mEffectParameters.ContainsKey(effectShortName))
            {
                throw new ArgumentException("The specified effect has not been stored, can not add parameters");
            }

            // Load the parameters
            EffectParameterListSave list = EffectParameterListSave.FromFile(fileName);

            // Store the short name
            string shortName = ShortName(fileName);
            mParametersFilenames.Add(shortName, fileName);

            // Store the parameters
            mParametersDictionary.Add(shortName, list);

            // Add to the effect's list
            mEffectParameters[effectShortName].Add(shortName);

            // Set parameters if needed
            if (partIndex >= 0)
            {
                SetPartParameters(partIndex, shortName);
            }

            // Return the short name
            return shortName;
        }

        #endregion

        #region Save/Load Material

        public void SaveMaterial(string fileName)
        {
            MaterialSave material = new MaterialSave();

            foreach (PartMaterial part in mParts)
            {
                // Add to effect
                material.EffectFiles.Add(mEffectFilenames[part.EffectName]);
                material.EffectParameterFiles.Add(mParametersFilenames[part.ParametersName]);

                // Save parameters
                mParametersDictionary[part.ParametersName].Save(mParametersFilenames[part.ParametersName]);
            }

            material.FileName = fileName;

            material.Save(fileName);
        }

        public void LoadMaterial(string fileName)
        {
            MaterialSave material = MaterialSave.FromFile(fileName);

            if (material.EffectFiles.Count != mParts.Count)
            {
                throw new System.FormatException("The material " + Path.GetFileName(fileName) + " has " +
                    material.EffectFiles.Count + ", which is not equal to the number of mesh parts on the current model (" +
                    mParts.Count + ").  Make sure the current model is the same that the material was saved for.");
            }
            else
            {
                // Load all effects
                for (int i = 0; i < material.EffectFiles.Count; i++)
                {
                    string effectShortName = AddEffect(material.EffectFiles[i]);
                    AddParameters(material.EffectParameterFiles[i], effectShortName);
                }

                // Set all effects
                for (int i = 0; i < material.EffectFiles.Count; i++)
                {
                    SetPartEffect(i, ShortName(material.EffectFiles[i]));
                    SetPartParameters(i, ShortName(material.EffectParameterFiles[i]));
                }
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
