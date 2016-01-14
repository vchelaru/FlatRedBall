using System;
using System.Collections.Generic;
using System.Text;

namespace EffectEditor.EffectComponents.HLSLInformation
{
    // commented values are not supported

    public enum StorageClass
    {
        None,
        Extern,
        //Nointerpolation,
        Shared,
        Static,
        Uniform,
        //Volatile
    }

    // All types except texture can be used as a vector or a matrix by
    // appending n or nxm to the type name, where n,m are numbers

    public enum HlslType
    {
        // Scalar types
        Bool,
        Int,
        Uint,
        Half,
        Float,
        Double,
        // Texture type
        Texture,
        Sampler
    }

    public struct HlslSemantic
    {
        #region Fields

        public HlslTypeDefinition Type;
        public String Name;
        public bool MultipleResourcesSupported; // Can N be attached to the end?
        public int ResourceNumber;

        #endregion

        #region Constructor

        public HlslSemantic(HlslTypeDefinition type, String name, bool multipleResourcesSupported)
        {
            Type = type;
            Name = name;
            MultipleResourcesSupported = multipleResourcesSupported;
            ResourceNumber = -1;
        }

        public HlslSemantic(HlslTypeDefinition type, String name,
            bool multipleResourcesSupported, int resourceNumber)
        {
            Type = type;
            Name = name;
            MultipleResourcesSupported = multipleResourcesSupported;
            ResourceNumber = resourceNumber;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return ToString(true);
        }

        #region XML Docs
        /// <summary>
        /// Returns a string representing the semantic
        /// </summary>
        /// <param name="includeType">Whether or not the type name should be included</param>
        /// <returns>The semantic as a string</returns>
        #endregion
        public string ToString(bool includeType)
        {
            return
                (includeType? Type.ToString() + " " : String.Empty) +
                Name +
                (MultipleResourcesSupported ?
                    ((ResourceNumber < 0) ? "[N]" : ResourceNumber.ToString()) : String.Empty);
        }

        #endregion
    }

    #region XML Docs
    /// <summary>
    /// A nested class used to store input/output semantics for vertex/pixel shaders, along with types
    /// </summary>
    #endregion
    public static class Semantics
    {
        #region XML Docs
        /// <summary>
        /// Finds a semantic by name, whether it is an input (or output) semantic, and
        /// what type of shader the semantic is for.
        /// </summary>
        /// <param name="name">The name of the semantic</param>
        /// <param name="isVertexShader">Whether or not the semantic is in a vertex shader</param>
        /// <param name="isInput">Whether or not the semantic is an input semantic</param>
        /// <returns>The semantic found, or null if none found</returns>
        #endregion
        public static HlslSemantic Find(String name, bool isVertexShader, bool isInput)
        {
            string searchName = name.ToUpper();

            List<HlslSemantic> results = new List<HlslSemantic>();

            List<HlslSemantic> source = (isVertexShader) ?
                (isInput ? VertexShader.Input : VertexShader.Output) :
                (isInput ? PixelShader.Input : PixelShader.Output);

            // find all results
            foreach (HlslSemantic semantic in source)
            {
                if (semantic.Name.ToUpper().Contains(searchName))
                    results.Add(semantic);
            }

            // score results
            int greatestScore = 0;
            int greatestScoreIndex = 0;
            int score;

            for (int i = 0; i < results.Count; i++)
            {
                string semanticName = results[i].Name.ToUpper();
                score = 0;

                if (semanticName.EndsWith(searchName)) score += 1;
                if (semanticName.StartsWith(searchName)) score += 2;
                if (semanticName.Equals(searchName)) score += 4;
                if (results[i].Name.Equals(name)) score += 8;

                if (score > greatestScore)
                {
                    greatestScore = score;
                    greatestScoreIndex = i;
                }
            }

            // return best result
            return results[greatestScoreIndex];
        }
        
        public static class VertexShader
        {
            public static List<HlslSemantic> Input;
            public static List<HlslSemantic> Output;

            #region Initialization

            static VertexShader()
            {
                Input = new List<HlslSemantic>();
                Output = new List<HlslSemantic>();

                // Initialize input semantics
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "BINORMAL", true));
                Input.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Uint), "BLENDINDICES", true));
                Input.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "BLENDWEIGHT", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "COLOR", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "NORMAL", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "POSITION", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "POSITIONT", false));
                Input.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "PSIZE", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "TANGENT", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 1), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 2), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 3), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "TEXCOORD", true));

                // Initialize output semantics
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "COLOR", true));
                Output.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "FOG", false));
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "POSITION", true));
                Output.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "PSIZE", false));
                Output.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "TESSFACTOR", true));
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 1), "TEXCOORD", true));
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 2), "TEXCOORD", true));
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 3), "TEXCOORD", true));
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "TEXCOORD", true));
            }

            #endregion
        }

        public static class PixelShader
        {
            public static List<HlslSemantic> Input;
            public static List<HlslSemantic> Output;

            #region Initialization

            static PixelShader()
            {
                Input = new List<HlslSemantic>();
                Output = new List<HlslSemantic>();

                // Initialize input semantics
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "COLOR", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 1), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 2), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 3), "TEXCOORD", true));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "TEXCOORD", true));
                Input.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "VFACE", false));
                Input.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 2), "VPOS", false));

                // Initialize output semantics
                Output.Add(new HlslSemantic(HlslTypeDefinition.CreateVector(HlslType.Float, 4), "COLOR", true));
                Output.Add(new HlslSemantic(new HlslTypeDefinition(HlslType.Float), "DEPTH", true));
            }

            #endregion
        }
    }

    #region XML Docs
    /// <summary>
    /// Defines an hlsl type, making it easy to edit
    /// </summary>
    #endregion
    public struct HlslTypeDefinition
    {
        #region Fields

        public HlslType Type;

        // Only one of these may be true
        public bool IsVector;
        public bool IsMatrix;
        public bool IsArray;

        // Used to set sizes
        public int Size; // used to represent vector/array size, or matrix columns
        public int MatrixColumns;

        #endregion

        #region Constructor

        public HlslTypeDefinition(HlslType type)
        {
            Type = type;
            IsVector = IsArray = IsMatrix = false;
            Size = MatrixColumns = 1;
        }

        // Static creation methods
        static public HlslTypeDefinition CreateVector(HlslType type, int size)
        {
            HlslTypeDefinition typedef = new HlslTypeDefinition(type);
            typedef.SetVector(size);
            return typedef;
        }

        static public HlslTypeDefinition CreateArray(HlslType type, int size)
        {
            HlslTypeDefinition typedef = new HlslTypeDefinition(type);
            typedef.SetArray(size);
            return typedef;
        }

        static public HlslTypeDefinition CreateMatrix(HlslType type, int rows, int columns)
        {
            HlslTypeDefinition typedef = new HlslTypeDefinition(type);
            typedef.SetMatrix(rows, columns);
            return typedef;
        }

        #endregion

        #region Methods

        public void SetVector(int size)
        {
            // Check if this is a texture (invalid if true)
            if (Type == HlslType.Texture)
            {
                throw new InvalidOperationException("Can not create a vector of textures");
            }

            // verify size
            if (size < 1 || size > 4) throw new ArgumentException("vector size is outside valid range - must be in the range [1,4]");

            // Set values
            IsVector = true;
            IsMatrix = IsArray = false;

            Size = size;
        }

        public void SetArray(int size)
        {
            // Check if this is a texture (invalid if true)
            if (Type == HlslType.Texture)
            {
                throw new InvalidOperationException("Can not create an array of textures");
            }

            // Set values
            IsArray = true;
            IsMatrix = IsVector = false;

            Size = size;
        }

        public void SetMatrix(int rows, int columns)
        {
            // Check if this is a texture (invalid if true)
            if (Type == HlslType.Texture)
            {
                throw new InvalidOperationException("Can not create a matrix of textures");
            }

            // verify size
            if (rows < 1 || rows > 4) throw new ArgumentException("Matrix rows are outside valid range - must be in the range [1,4]");
            if (columns < 1 || columns > 4) throw new ArgumentException("Matrix columns are outside valid range - must be in the range [1,4]");

            // Set values
            IsMatrix = true;
            IsVector = IsArray = false;

            Size = rows;
            MatrixColumns = columns;
        }

        #region XML Docs
        /// <summary>
        /// Generates a string representing this type
        /// </summary>
        /// <returns>A string representing this type</returns>
        #endregion
        public override string ToString()
        {
            return
                Type.ToString().ToLower() +
                ((IsVector) ? Size.ToString() : String.Empty) +
                ((IsArray) ? "[" + Size.ToString() + "]" : String.Empty) +
                ((IsMatrix) ? Size.ToString() + "x" + MatrixColumns.ToString() : String.Empty);
        }

        #endregion
    }
}
