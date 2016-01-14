using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using EffectEditor.EffectComponents.HLSLInformation;
using FlatRedBall.IO;

namespace EffectEditor.EffectComponents
{
    public struct EffectPassDefinition
    {
        #region Fields

        public string Name;

        public string VertexShaderName;
        public string PixelShaderName;

        public ShaderProfile VertexShaderProfile;
        public ShaderProfile PixelShaderProfile;

        #endregion

        #region Constructor

        public EffectPassDefinition(
            string name,
            string vertexShaderName, string pixelShaderName,
            ShaderProfile vertexShaderProfile, ShaderProfile pixelShaderProfile)
        {
            Name = name;
            VertexShaderName = vertexShaderName;
            PixelShaderName = pixelShaderName;
            VertexShaderProfile = vertexShaderProfile;
            PixelShaderProfile = pixelShaderProfile;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Name;
        }

        public string GetPassString()
        {
            string passString = String.Empty;

            passString += "    pass " + Name + Environment.NewLine;
            passString += "    {" + Environment.NewLine;
            passString += "        VertexShader = compile " +
                VertexShaderProfile.ToString().ToLower() + " " +
                VertexShaderName + "();" + Environment.NewLine;
            passString += "        PixelShader = compile " +
                PixelShaderProfile.ToString().ToLower() + " " +
                PixelShaderName + "();" + Environment.NewLine;
            passString += "    }";

            return passString;
        }

        #endregion
    }
    
    public struct EffectTechniqueDefinition
    {
        #region Fields

        public string Name;

        public List<EffectPassDefinition> Passes;

        #endregion

        #region Constructor

        public EffectTechniqueDefinition(string name,
            List<EffectPassDefinition> passes)
        {
            Name = name;
            Passes = new List<EffectPassDefinition>(passes);
        }

        public EffectTechniqueDefinition(string name)
        {
            Name = name;
            Passes = new List<EffectPassDefinition>();
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Name;
        }

        public string GetTechniqueString()
        {
            string techniqueString = String.Empty;

            techniqueString += "technique " + Name + Environment.NewLine;
            techniqueString += "{" + Environment.NewLine;

            foreach (EffectPassDefinition pass in Passes)
            {
                techniqueString += pass.GetPassString() + Environment.NewLine;
            }

            techniqueString += "};";

            return techniqueString;
        }

        #endregion
    }

    public class EffectDefinition
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The name of this effect
        /// </summary>
        #endregion
        public string Name;

        #region XML Docs
        /// <summary>
        /// The components used by this effect
        /// </summary>
        public List<string> Components;
        #endregion

        #region XML Docs
        /// <summary>
        /// Parameters available in this effect
        /// </summary>
        #endregion
        public List<EffectParameterDefinition> Parameters;

        #region XML Docs
        /// <summary>
        /// Vertex shader components
        /// </summary>
        #endregion
        public List<EffectComponent> VertexShaders;

        #region XML Docs
        /// <summary>
        /// Pixel shader components
        /// </summary>
        #endregion
        public List<EffectComponent> PixelShaders;

        #region XML Docs

        #endregion
        public List<EffectTechniqueDefinition> Techniques;

        #endregion

        #region Constructor / Initialization

        public EffectDefinition()
        {
            Name = String.Empty;
            Components = new List<string>();
            Parameters = new List<EffectParameterDefinition>();
            VertexShaders = new List<EffectComponent>();
            PixelShaders = new List<EffectComponent>();
            Techniques = new List<EffectTechniqueDefinition>();
        }

        public EffectDefinition(String name)
            : this()
        {
            Name = name;
        }

        #endregion

        #region Methods

        public string GetEffectCode()
        {
            String effectCode = String.Empty;

            #region Parameters

            // Output effect parameters
            foreach (EffectParameterDefinition param in Parameters)
            {
                effectCode += param.ToString() + @";" + Environment.NewLine;

                if (param.Type.Type == HlslType.Texture)
                {
                    // output a sampler as well
                    effectCode += @"sampler " + param.Name + @"Sampler = sampler_state" + Environment.NewLine;
                    effectCode += @"{" + Environment.NewLine;
                    effectCode += @"    Texture = (" + param.Name + @");" + Environment.NewLine;
                    effectCode += @"};" + Environment.NewLine;
                }
            }

            effectCode += Environment.NewLine;

            #endregion

            #region Components / shaders

            // Load components
            EffectComponent[] components = new EffectComponent[Components.Count];
            for (int i = 0; i < Components.Count; i++)
            {
                components[i] = EffectComponent.FromFile(Components[i]);
            }

            // Structs
            for (int i = 0; i < components.Length; i++)
            {
                effectCode += components[i].OutputStruct() + Environment.NewLine + Environment.NewLine;
            }

            for (int i = 0; i < VertexShaders.Count; i++)
            {
                effectCode += VertexShaders[i].OutputStruct() + Environment.NewLine + Environment.NewLine;
            }

            for (int i = 0; i < PixelShaders.Count; i++)
            {
                effectCode += PixelShaders[i].OutputStruct() + Environment.NewLine + Environment.NewLine;
            }

            // Functions
            for (int i = 0; i < components.Length; i++)
            {
                effectCode += components[i].FunctionString() + Environment.NewLine + Environment.NewLine;
            }

            for (int i = 0; i < VertexShaders.Count; i++)
            {
                effectCode += VertexShaders[i].FunctionString() + Environment.NewLine + Environment.NewLine;
            }

            for (int i = 0; i < PixelShaders.Count; i++)
            {
                effectCode += PixelShaders[i].FunctionString() + Environment.NewLine + Environment.NewLine;
            }

            #endregion

            #region Techniques

            foreach (EffectTechniqueDefinition technique in Techniques)
            {
                effectCode += technique.GetTechniqueString() + Environment.NewLine;
            }

            #endregion

            return effectCode;
        }

        #region XML Docs
        /// <summary>
        /// Saves the effect to file
        /// </summary>
        /// <param name="fileName">The name of the effect file</param>
        #endregion
        public void Save(string fileName)
        {
            string directory = FileManager.GetDirectory(fileName);

            // make components relative
            List<string> oldComponents = Components;
            Components = new List<string>();

            foreach (string component in oldComponents)
            {
                Components.Add(FileManager.MakeRelative(component, directory));
            }

            // Save the file
            FileManager.XmlSerialize<EffectDefinition>(this, fileName);

            // return original components list
            Components = oldComponents;
        }

        #region XML Docs
        /// <summary>
        /// Loads an effect definition from a file
        /// </summary>
        /// <param name="fileName">The name of the effect file</param>
        /// <returns>The effect definition</returns>
        #endregion
        public static EffectDefinition FromFile(string fileName)
        {
            EffectDefinition effect = FileManager.XmlDeserialize<EffectDefinition>(fileName);

            string directory = FileManager.GetDirectory(fileName);
            for (int i = 0; i < effect.Components.Count; i++)
            {
                effect.Components[i] = directory + effect.Components[i];
            }

            return effect;
        }

        #endregion
    }
}
