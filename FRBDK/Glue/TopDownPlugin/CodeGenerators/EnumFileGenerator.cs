using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace TopDownPlugin.CodeGenerators
{
    public class EnumFileGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => 
            // Vic says - not sure when this was introduced, prior to versions:
            GlueState.Self.CurrentGlueProject?.FileVersion > (int)GluxVersions.PreVersion 
            ? "TopDown/Enums.Generated.cs"
            : "TopDown/Enums.cs";

        static EnumFileGenerator mSelf;
        public static EnumFileGenerator Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new EnumFileGenerator();
                }
                return mSelf;
            }
        }

        protected override string GenerateFileContents()
        {
            var toReturn =
$@"
using Microsoft.Xna.Framework;

namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public enum TopDownDirection
    {{
        Right = 0,
        UpRight = 1,
        Up = 2,
        UpLeft = 3,
        Left = 4,
        DownLeft = 5,
        Down = 6,
        DownRight = 7
    }}


    public static class TopDownDirectionExtensions
    {{
        public static TopDownDirection FromDirection(Microsoft.Xna.Framework.Vector2 direction, PossibleDirections possibleDirections)
        {{
            return FromDirection(direction.X, direction.Y, possibleDirections);
        }}

        public static TopDownDirection FromDirection(Microsoft.Xna.Framework.Vector3 direction, PossibleDirections possibleDirections)
        {{
            return FromDirection(direction.X, direction.Y, possibleDirections);
        }}

        public static float ToAngleRadians(this TopDownDirection direction)
        {{
            switch (direction)
            {{
                case TopDownDirection.Right: return 0;
                case TopDownDirection.UpRight: return MathHelper.PiOver4;
                case TopDownDirection.Up: return MathHelper.PiOver2;
                case TopDownDirection.UpLeft: return MathHelper.PiOver2 + MathHelper.PiOver4;
                case TopDownDirection.Left: return MathHelper.Pi;
                case TopDownDirection.DownLeft: return MathHelper.Pi + MathHelper.PiOver4;
                case TopDownDirection.Down: return MathHelper.Pi + MathHelper.PiOver2;
                case TopDownDirection.DownRight: return MathHelper.Pi + MathHelper.PiOver2 + MathHelper.PiOver4;
            }}
            return 0;
        }}


        public static Microsoft.Xna.Framework.Vector3 ToVector(this TopDownDirection direction)
        {{
            float diagonalLength = (float)System.Math.Cos( MathHelper.PiOver4 );

            switch(direction)
            {{
                case TopDownDirection.Left: return Vector3.Left;
                case TopDownDirection.UpLeft: return new Vector3(-diagonalLength, diagonalLength, 0);
                case TopDownDirection.Up: return Vector3.Up;
                case TopDownDirection.UpRight: return new Vector3(diagonalLength, diagonalLength, 0);
                case TopDownDirection.Right: return Vector3.Right;
                case TopDownDirection.DownRight: return new Vector3(diagonalLength, -diagonalLength, 0);
                case TopDownDirection.Down: return Vector3.Down;
                case TopDownDirection.DownLeft: return new Vector3(-diagonalLength, -diagonalLength, 0);
            }}

            return Vector3.Right;
        }}

        public static TopDownDirection FlipX(this TopDownDirection directionToFlip)
        {{
            switch(directionToFlip)
            {{
                case TopDownDirection.Left: return TopDownDirection.Right;
                case TopDownDirection.UpLeft: return TopDownDirection.UpRight;
                case TopDownDirection.Up: return TopDownDirection.Up;
                case TopDownDirection.UpRight: return TopDownDirection.UpLeft;
                case TopDownDirection.Right: return TopDownDirection.Left;
                case TopDownDirection.DownRight: return TopDownDirection.DownLeft;
                case TopDownDirection.Down: return TopDownDirection.Down;
                case TopDownDirection.DownLeft: return TopDownDirection.DownRight;
            }}

            throw new System.Exception();
        }}

        public static TopDownDirection FlipY(this TopDownDirection directionToFlip)
        {{
            switch (directionToFlip)
            {{
                case TopDownDirection.Left: return TopDownDirection.Left;
                case TopDownDirection.UpLeft: return TopDownDirection.DownLeft;
                case TopDownDirection.Up: return TopDownDirection.Down;
                case TopDownDirection.UpRight: return TopDownDirection.DownRight;
                case TopDownDirection.Right: return TopDownDirection.Right;
                case TopDownDirection.DownRight: return TopDownDirection.UpRight;
                case TopDownDirection.Down: return TopDownDirection.Up;
                case TopDownDirection.DownLeft: return TopDownDirection.UpLeft;
            }}
            throw new System.Exception();
        }}

        public static TopDownDirection Mirror(this TopDownDirection directionToFlip)
        {{
            switch (directionToFlip)
            {{
                case TopDownDirection.Left: return TopDownDirection.Right;
                case TopDownDirection.UpLeft: return TopDownDirection.DownRight;
                case TopDownDirection.Up: return TopDownDirection.Down;
                case TopDownDirection.UpRight: return TopDownDirection.DownLeft;
                case TopDownDirection.Right: return TopDownDirection.Left;
                case TopDownDirection.DownRight: return TopDownDirection.UpLeft;
                case TopDownDirection.Down: return TopDownDirection.Up;
                case TopDownDirection.DownLeft: return TopDownDirection.UpRight;
            }}
            throw new System.Exception();
        }}




        public static TopDownDirection FromDirection(float x, float y, PossibleDirections possibleDirections, TopDownDirection? lastDirection = null)
        {{
            if(x == 0 && y == 0)
            {{
                throw new System.Exception(""Can't convert 0,0 to a direction"");
            }}

            switch (possibleDirections)
            {{
                case PossibleDirections.LeftRight:
                    if (x > 0)
                    {{
                        return TopDownDirection.Right;
                    }}
                    else if (x < 0)
                    {{
                        return TopDownDirection.Left;
                    }}
                    break;
                case PossibleDirections.FourWay:
                    var absXVelocity = System.Math.Abs(x);
                    var absYVelocity = System.Math.Abs(y);

                    if (absXVelocity > absYVelocity)
                    {{
                        if (x > 0)
                        {{
                            return TopDownDirection.Right;
                        }}
                        else if (x < 0)
                        {{
                            return TopDownDirection.Left;
                        }}
                    }}
                    else if (absYVelocity > absXVelocity)
                    {{
                        if (y > 0)
                        {{
                            return TopDownDirection.Up;
                        }}
                        else if (y < 0)
                        {{
                            return TopDownDirection.Down;
                        }}
                    }}
                    else // absx and absy are equal:
                    {{
                        // The first two if-checks preserve the direction the user was facing when
                        // moving diagonal, instead of making the right direction the dominant direction.
                        if(lastDirection == TopDownDirection.Up && y > 0)
                        {{
                            return TopDownDirection.Up;
                        }}
                        else if(lastDirection == TopDownDirection.Down && y < 0)
                        {{
                            return TopDownDirection.Down;
                        }}
                        else if(x > 0)
                        {{
                            return TopDownDirection.Right;
                        }}
                        else
                        {{
                            return TopDownDirection.Left;
                        }}
                    }}
                    break;
                case PossibleDirections.EightWay:
                    if (x != 0 || y != 0)
                    {{
                        var angle = FlatRedBall.Math.MathFunctions.RegulateAngle(
                            (float)System.Math.Atan2(y, x));

                        var ratioOfCircle = angle / Microsoft.Xna.Framework.MathHelper.TwoPi;

                        var eights = FlatRedBall.Math.MathFunctions.RoundToInt(ratioOfCircle * 8) % 8;

                        return (TopDownDirection)eights;
                    }}

                    break;
            }}

            throw new System.Exception();
        }}

        public static string ToFriendlyString(this TopDownDirection direction)
        {{
            switch(direction)
            {{
                case TopDownDirection.Down:
                    return nameof(TopDownDirection.Down);
                case TopDownDirection.DownLeft:
                    return nameof(TopDownDirection.DownLeft);
                case TopDownDirection.DownRight:
                    return nameof(TopDownDirection.DownRight);
                case TopDownDirection.Left:
                    return nameof(TopDownDirection.Left);
                case TopDownDirection.Right:
                    return nameof(TopDownDirection.Right);
                case TopDownDirection.Up:
                    return nameof(TopDownDirection.Up);
                case TopDownDirection.UpLeft:
                    return nameof(TopDownDirection.UpLeft);
                case TopDownDirection.UpRight:
                    return nameof(TopDownDirection.UpRight);
            }}
        
            return nameof(TopDownDirection.Down);
        }}
    }}

    public enum PossibleDirections
    {{
        LeftRight,
        FourWay,
        EightWay
    }}

}}

";
            return toReturn;
        }
    }
}
