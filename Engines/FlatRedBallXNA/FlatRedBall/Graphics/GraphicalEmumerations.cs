using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


using FlatRedBall.Graphics.Particle;

using FlatRedBall.Utilities;


namespace FlatRedBall.Graphics
{
    #region Enums

    public enum ColorOperation
    {
        Texture,
        Add,
        Subtract,
        Modulate,
        InverseTexture,
        Color,
        ColorTextureAlpha,
        Modulate2X,
        Modulate4X,
        InterpolateColor
    }


    public enum BlendOperation
    {
        Regular,
        Add,
        Modulate,
        Modulate2X,
        NonPremultipliedAlpha,
        SubtractAlpha
    }

    public enum SortType
    {
        /// <summary>
        /// No sorting will be performed, objects will remain in the the same order that they've been added to their respective list
        /// </summary>
        None,
        /// <summary>
        /// Objects will sort by their Z value
        /// </summary>
        Z,
        DistanceFromCamera,
        DistanceAlongForwardVector,
        /// <summary>
        /// Objects will be sorted based on their Z value first. Objects with identical Z values will sort using their top parent Y values.
        /// </summary>
        ZSecondaryParentY,
        CustomComparer,
        Texture

    }

    public enum TextureCoordinateType
    {
        UV,
        Pixel
    }


    public enum CameraModelCullMode
    {
        Frustum,
        None
    }

    public enum CameraCullMode
    {
        UnrotatedDownZ,
        None
    }

    public enum OrderingMode
    {
        Undefined,
        DistanceFromCamera,
        ZBuffered

    }


    #endregion

    public static class GraphicalEnumerations
    {
        #region Fields

        public const float MaxColorComponentValue = 1.0f;

        
        public static List<String> mConvertableSpriteProperties = new List<string>
            (
            new string[8] {  "Alpha",
                            "AlphaRate",
                            "Blue",
                            "BlueRate",
                            "Green",
                            "GreenRate",
                            "Red",
                            "RedRate"
            }
            );
        
        #endregion

        #region Properties

        public static List<String> ConvertableSpriteProperties
        {
            get { return mConvertableSpriteProperties; }
        }

        #endregion

        #region BlendOperation Methods

        public static BlendOperation TranslateBlendOperation(string op)
        {
            if (string.IsNullOrEmpty(op))
            {
                return FlatRedBall.Graphics.BlendOperation.Regular;
            }

            switch(op)
            {
                case var _ when String.Equals(op, "regular", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.Regular;
                case var _ when String.Equals(op, "add", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.Add;
                case var _ when String.Equals(op, "alphaadd", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.Add;
                case var _ when String.Equals(op, "modulate", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.Modulate;
                case var _ when String.Equals(op, "modulate2x", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.Modulate2X;
                case var _ when String.Equals(op, "nonpremultipliedalpha", StringComparison.OrdinalIgnoreCase):
                    return BlendOperation.NonPremultipliedAlpha;
                default:
                    throw new NotImplementedException("Color Operation " + op + " not implemented");
            }
        }

        // TODO:  Need to add this in once FRB MDX uses strings instead of BlendOperationSave
        /*
        public static string TranslateBlendOperation(BlendOperation op)
        {
            switch (op)
            {
                case FlatRedBall.Graphics.BlendOperation.Add:
                    return "ADD";

//                case "ALPHAADD":
                    // TODO:  Add ALPHAADD
                    //throw new System.NotImplementedException("ALPHAADD is currently not supported in FlatRedBall XNA");
  //                  return FlatRedBall.Graphics.BlendOperation.Add;
                case FlatRedBall.Graphics.BlendOperation.Modulate:
                    return "MODULATE";

                case FlatRedBall.Graphics.BlendOperation.Modulate2X:
                    // TODO:  Add Modulate2X
                    return "MODULATE2X";
                //break;
                case FlatRedBall.Graphics.BlendOperation.Regular:
                    return "REGULAR";
                default:
                    throw new NotImplementedException("Color Operation " + op + " not implemented");
            }
        }
        */


        public static string BlendOperationToFlatRedBallMdxString(FlatRedBall.Graphics.BlendOperation op)
        {
            switch (op)
            {
                case FlatRedBall.Graphics.BlendOperation.Add:
                    return "ADD";
                case FlatRedBall.Graphics.BlendOperation.Modulate:
                    return "MODULATE";
                    
                case FlatRedBall.Graphics.BlendOperation.Modulate2X:
                    return "MODULATE2X";
                
                //break;
                case FlatRedBall.Graphics.BlendOperation.Regular:
                    return "REGULAR";
                default:
                    throw new NotImplementedException("Color Operation " + op + " not implemented");
            }
        }

        #endregion

        #region ColorOperation Methods

        public static ColorOperation TranslateColorOperation(string op)
        {
            // This is likely not going to compile on all platforms, need to test it out.

            switch (op)
            {
                case "Add":
                    return FlatRedBall.Graphics.ColorOperation.Add;
                    //break;
                
                case "Modulate":
                    return FlatRedBall.Graphics.ColorOperation.Modulate;
                    //break;
                case "SelectArg1":
                case "Texture":
                case "None":
                case "":
                case null:
                    return FlatRedBall.Graphics.ColorOperation.Texture;
                    //break;
                case "InverseTexture":
                    return ColorOperation.InverseTexture;
                case "Color":
                    return ColorOperation.Color;
                case "SelectArg2":
                case "ColorTextureAlpha":
                    return FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha;
                //break;
                case "Subtract":
                    return FlatRedBall.Graphics.ColorOperation.Subtract;
                    //break;
                case "Modulate2X":
                    return FlatRedBall.Graphics.ColorOperation.Modulate2X;
                    //break;
                case "Modulate4X":
                    return FlatRedBall.Graphics.ColorOperation.Modulate4X;
                    //break;
                case "InterpolateColor":
                    return ColorOperation.InterpolateColor;
                default:
                    throw new System.NotImplementedException(
                        op + " is currently not supported in FlatRedBall XNA");
                    //break;
            }
        }


        public static string ColorOperationToFlatRedBallMdxString(ColorOperation op)
        {
            switch (op)
            {
                case FlatRedBall.Graphics.ColorOperation.Add:
                    return "Add";
                //break;
                case ColorOperation.InverseTexture:
                    return "InverseTexture";
                    //break;
                case ColorOperation.InterpolateColor:
                    return "InterpolateColor";
                    //break;
                case FlatRedBall.Graphics.ColorOperation.Modulate:
                    return "Modulate";
                //break;
                case FlatRedBall.Graphics.ColorOperation.Texture:
                    return "SelectArg1";
                //break;
                case ColorOperation.Color:
                case FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha:
                    return "SelectArg2";
                //break;
                case FlatRedBall.Graphics.ColorOperation.Subtract:
                    return "Subtract";
                //break;
                case FlatRedBall.Graphics.ColorOperation.Modulate2X:
                    return "Modulate2X";
                // break;
                case FlatRedBall.Graphics.ColorOperation.Modulate4X:
                    return "Modulate4X";
                // break;
                default:
                    throw new System.NotImplementedException(
                        op + " is currently not supported in FlatRedBall XNA");
                //break;
            }
        }

        public static void SetColors(IColorable colorable, 
            float desiredRed, float desiredGreen, float desiredBlue, string op)
        {
            if (op == "AddSigned")
            {
                desiredRed -= 127.5f;
                desiredGreen -= 127.5f;
                desiredBlue -= 127.5f;

                op = "Add";
            }

            colorable.ColorOperation = TranslateColorOperation(op);

            colorable.Red = desiredRed / 255.0f;
            colorable.Green = desiredGreen / 255.0f;
            colorable.Blue = desiredBlue / 255.0f;
        }

        #endregion


        #region AreaEmissionType

        public static Emitter.AreaEmissionType TranslateAreaEmissionType(string op)
        {
            switch (op)
            {
                case "Point":
                case null:
                case "":
                    return Emitter.AreaEmissionType.Point;
                    //break;
                case "Rectangle":
                    return Emitter.AreaEmissionType.Rectangle;
                    //break;
                case "Cube":
                    return Emitter.AreaEmissionType.Cube;
                    //break;
                default:
                    throw new System.NotImplementedException(
                        op + " is currently not supported in FlatRedBall XNA");
                    //break;
            }
        }

        public static string TranslateAreaEmissionType(Emitter.AreaEmissionType op)
        {
            switch (op)
            {
                case Emitter.AreaEmissionType.Point:
                    return "Point";
                //break;
                case Emitter.AreaEmissionType.Rectangle:
                    return "Rectangle";
                //break;
                case Emitter.AreaEmissionType.Cube:
                    return "Cube";
                //break;
                default:
                    throw new System.NotImplementedException(
                        op + " is currently not supported in FlatRedBall XNA");
                //break;
            }
        }

        #endregion

        #region Texture Methods

#if FRB_MDX
        public static int BitsPerPixelInFormat(Microsoft.DirectX.Direct3D.Format imageFormat)
        {
            switch (imageFormat)
            {
                case Format.A4L4:
                case Format.A8:
                case Format.L8:
                case Format.P8:
                case Format.R3G3B2:
                    return 8;
                    break;

                case Format.A1R5G5B5:
                case Format.A4R4G4B4:
                case Format.A8L8:
                case Format.A8P8:
                case Format.A8R3G3B2:
                case Format.D15S1:
                case Format.D16:
                case Format.D16Lockable:
                case Format.L16:
                case Format.L6V5U5:
                case Format.R16F:
                case Format.R5G6B5:
                case Format.V8U8:
                case Format.X1R5G5B5:
                case Format.X4R4G4B4:
                    return 16;
                    break;

                case Format.R8G8B8:
                    return 24;
                    break;

                case Format.A2B10G10R10:
                case Format.A2R10G10B10:
                case Format.A2W10V10U10:
                case Format.A8B8G8R8:
                case Format.A8R8G8B8:
                case Format.CxV8U8:
                case Format.D24S8:
                case Format.D24SingleS8:
                case Format.D24X4S4:
                case Format.D24X8:
                case Format.D32:
                case Format.D32SingleLockable:
                case Format.G16R16:
                case Format.G16R16F:
                case Format.G8R8G8B8:
                case Format.Q8W8V8U8:
                case Format.R32F:
                case Format.R8G8B8G8:
                case Format.V16U16:
                case Format.X8B8G8R8:
                case Format.X8L8V8U8:
                case Format.X8R8G8B8:
                    return 32;
                    break;
                
                case Format.A16B16G16R16:
                case Format.A16B16G16R16F:
                case Format.G32R32F:
                case Format.Q16W16V16U16:
                    return 64;
                    break;
                
                case Format.A32B32G32R32F:
                    return 128;

                default:
                    return 32; // assume 32 bpp 
                    break;

            }

        }

#endif

        #endregion
    }
}
