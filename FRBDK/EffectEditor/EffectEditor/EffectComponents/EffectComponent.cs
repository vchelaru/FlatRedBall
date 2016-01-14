using System;
using System.Collections.Generic;
using System.Text;
using EffectEditor.EffectComponents.HLSLInformation;
using Microsoft.Xna.Framework.Graphics;

namespace EffectEditor.EffectComponents
{
    [Serializable]
    public struct EffectParameterDefinition : IComparable
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The type of this parameter
        /// </summary>
        #endregion
        public HlslTypeDefinition Type;

        #region XML Docs
        /// <summary>
        /// The name of this parameter
        /// Spaces are not allowed
        /// </summary>
        #endregion
        public String Name;

        #region XML Docs
        /// <summary>
        /// The storage class for this parameter
        /// </summary>
        #endregion
        public StorageClass StorageClass;

        #region XML Docs
        /// <summary>
        /// The semantic for this parameter
        /// </summary>
        #endregion
        public HlslSemantic Semantic;

        // True if these are used
        public bool HasStorageClass;
        public bool HasSemantic;

        #endregion

        #region Constructor

        public EffectParameterDefinition(String name, HlslTypeDefinition type)
        {
            Name = name;
            Type = type;
            HasStorageClass = false;
            HasSemantic = false;
            StorageClass = StorageClass.None;
            Semantic = new HlslSemantic();
        }

        public EffectParameterDefinition(String name, HlslTypeDefinition type, StorageClass storageClass)
        {
            Name = name;
            Type = type;
            HasStorageClass = true;
            HasSemantic = false;
            StorageClass = storageClass;
            Semantic = new HlslSemantic();
        }

        public EffectParameterDefinition(String name, HlslTypeDefinition type, HlslSemantic semantic)
        {
            Name = name;
            Type = type;
            HasStorageClass = false;
            HasSemantic = true;
            StorageClass = StorageClass.None;
            Semantic = semantic;
        }

        public EffectParameterDefinition(String name, HlslTypeDefinition type,
            StorageClass storageClass, HlslSemantic semantic)
        {
            Name = name;
            Type = type;
            HasStorageClass = true;
            HasSemantic = true;
            StorageClass = storageClass;
            Semantic = semantic;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return
                ((HasStorageClass && StorageClass != StorageClass.None) ? StorageClass.ToString().ToLower() + " " : String.Empty) +
                Type.ToString() + " " + Name +
                (HasSemantic ? " : " + Semantic.ToString(false) : String.Empty);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((EffectParameterDefinition)obj).Name);
        }

        #endregion
    }

    #region XML Docs
    /// <summary>
    /// An effect component - a function and information about return values
    /// </summary>
    #endregion
    public class EffectComponent : IComparable
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// the name of this component
        /// Spaces are not allowed
        /// </summary>
        #endregion
        public String Name;

        #region XML Docs
        /// <summary>
        /// The code for this component, without the function signature (this is auto-generated)
        /// </summary>
        #endregion
        public String Code;

        #region XML Docs
        /// <summary>
        /// The parameters required by this component
        /// </summary>
        #endregion
        public List<EffectParameterDefinition> Parameters;

        #region XML Docs
        /// <summary>
        /// The return type of this component (always a struct for simplicity on the backend)
        /// </summary>
        #endregion
        public List<EffectParameterDefinition> ReturnType;

        #region XML Docs
        /// <summary>
        /// Whether or not this component is available to a vertex shader
        /// </summary>
        #endregion
        public bool AvailableToVertexShader = true;

        #region XML Docs
        /// <summary>
        /// Whether or not this component is available to a pixel shader
        /// </summary>
        #endregion
        public bool AvailableToPixelShader = true;

        #region XML Docs
        /// <summary>
        /// The minimum vertex shader profile required to run this component (if available to vertex shaders)
        /// </summary>
        #endregion
        public string MinimumVertexShaderProfile = ShaderProfile.VS_1_1.ToString();

        #region XML Docs
        /// <summary>
        /// The minimum pixel shader profile required to run this component (if available to pixel shaders)
        /// </summary>
        #endregion
        public string MinimumPixelShaderProfile = ShaderProfile.PS_1_1.ToString();

        #region XML Docs
        /// <summary>
        /// Whether or not the function should be inline
        /// </summary>
        #endregion
        public bool IsInline = true;

        #endregion

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates an empty effect component
        /// </summary>
        #endregion
        public EffectComponent()
        {
            Parameters = new List<EffectParameterDefinition>();
            ReturnType = new List<EffectParameterDefinition>();
        }

        #endregion

        #region Properties

        public String OutputStructName
        {
            get { return Name + "Output"; }
        }

        public String FunctionStart
        {
            get
            {
                String outputFunction = String.Empty;

                if (IsInline) outputFunction += @"inline "; // inline if requested

                outputFunction += OutputStructName + " "; // return type
                outputFunction += Name + @"("; // function name and start of parameter list

                for (int i = 0; i < Parameters.Count; i++)
                {
                    outputFunction += Parameters[i].ToString(); // parameter type and name
                    if (i < Parameters.Count - 1) outputFunction += @", "; // comma for all but last parameter
                }

                outputFunction += @") {" + Environment.NewLine; // end parameter list and begin function body

                outputFunction += "    " + OutputStructName + @" Output = (" + OutputStructName + @")0;"; // Start code with new output structure named Output }

                return outputFunction;
            }
        }

        public String FunctionEnd
        {
            get
            {
                String outputFunction = String.Empty;

                outputFunction += @"    return Output;";// end code by returning output structure

                outputFunction += Environment.NewLine + @"}"; // end function

                return outputFunction;
            }
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Gets the output structure definition as a string
        /// </summary>
        #endregion
        public String OutputStruct()
        {
            String outputStruct = @"struct " + OutputStructName + @" {" + Environment.NewLine;

            foreach (EffectParameterDefinition paramdef in ReturnType)
            {
                outputStruct += "    " + paramdef.ToString() + @";" + Environment.NewLine;
            }

            outputStruct += @"};";

            return outputStruct;
        }

        #region XML Docs
        /// <summary>
        /// Gets the function as a string
        /// </summary>
        #endregion
        public String FunctionString()
        {
            return FunctionStart + Environment.NewLine + Environment.NewLine +
                Code + Environment.NewLine + Environment.NewLine + FunctionEnd;
        }

        #region XML Docs
        /// <summary>
        /// Gets a string to help with coding
        /// </summary>
        #endregion
        public String FunctionHelperString()
        {
            String outputFunction = String.Empty;

            outputFunction += "// Function " + Name + " returns ";
            for (int i = 0; i < ReturnType.Count; i++)
            {
                outputFunction += ReturnType[i].ToString();
                if (i < ReturnType.Count - 1) outputFunction += ", ";
            }
            outputFunction += Environment.NewLine;
            outputFunction += OutputStructName + " returnVar = "; // return type
            outputFunction += Name + @"("; // function name and start of parameter list

            for (int i = 0; i < Parameters.Count; i++)
            {
                outputFunction += @"[" + Parameters[i].ToString() + "]"; // parameter type and name
                if (i < Parameters.Count - 1) outputFunction += @", "; // comma for all but last parameter
            }

            outputFunction += @");"; // end parameter list

            return outputFunction;
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// Saves the component to a file
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        #endregion
        public void Save(String fileName)
        {
            FlatRedBall.IO.FileManager.XmlSerialize<EffectComponent>(this, fileName);
        }

        #region XML Docs
        /// <summary>
        /// Loads an effect component from file
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <returns>The loaded effect component</returns>
        #endregion
        public static EffectComponent FromFile(String fileName)
        {
            return FlatRedBall.IO.FileManager.XmlDeserialize<EffectComponent>(fileName);
        }

        public override string ToString()
        {
            return Name;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is EffectComponent)
                return Name.CompareTo(((EffectComponent)obj).Name);
            else
                return Name.CompareTo(String.Empty);
        }

        #endregion
    }
}
