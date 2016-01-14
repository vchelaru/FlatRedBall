using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Windows.Media.Effects;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Controls;
using SilverArcade.SilverSprite.Graphics;

namespace FlatRedBall.Graphics
{
    public static partial class Renderer
    {
        static void SpriteBatchInitialize()
        {
            GraphicsBatch = new GraphicsBatch(FlatRedBallServices.Game.GraphicsDevice);
        }

        public static Canvas Canvas
        {
            get
            {
                CanvasRenderer sbc = GraphicsBatch.GetCanvas();
                sbc.InUse = true;
                return sbc.Canvas;
            }

        }

        internal static GraphicsBatch GraphicsBatch;


        static List<VertexPositionColorTexture[]> mSpriteVertices = null;
        static List<RenderBreak> mSpriteRenderBreaks = null;

        private static void DrawSprites(
            List<VertexPositionColorTexture[]> spriteVertices,
            List<RenderBreak> mSpriteRenderBreaks,
            List<Sprite> spritesToDraw, int startIndex, int numberToDraw, Camera camera)
        {


        //public static void DrawSprites<T>(IList<T> spritesToDraw, int startIndex, int numberToDraw, Camera camera) where T : Sprite
        //{

            DrawSpritesNew(spritesToDraw, startIndex, numberToDraw, camera);
            return;




            if (numberToDraw == 0)
            {
                return;
            }

            // Vic says: I don't think this is needed now that the Renderer is handling mixed drawing
            //mAutomaticallyUpdatedSprites.SortZInsertionAscending();
            SpriteBlendMode lastBlendMode = SpriteBlendMode.AlphaBlend;

            // Vic says: At the time of this writing Additive blend isn't working properly
            if (spritesToDraw[startIndex].BlendOperation == BlendOperation.Add)
            {
                lastBlendMode = SpriteBlendMode.Additive;
            }

            Matrix lookAtMatrix = camera.GetLookAtMatrix();

            GraphicsBatch.Begin(lastBlendMode,
                              SpriteSortMode.Immediate,
                              SaveStateMode.None, 
                              lookAtMatrix
                              );

            int endIndex = startIndex + numberToDraw;

            for(int i = startIndex; i < endIndex; i++)
            {

                Sprite s = spritesToDraw[i];

                if(s.BlendOperation == BlendOperation.Add && lastBlendMode == SpriteBlendMode.AlphaBlend)
                {
                    lastBlendMode = SpriteBlendMode.Additive;

                    GraphicsBatch.End();

                    GraphicsBatch.Begin(lastBlendMode,
                                      SpriteSortMode.Immediate,
                                      SaveStateMode.None, lookAtMatrix);
                }
                if(s.BlendOperation == BlendOperation.Regular && lastBlendMode == SpriteBlendMode.Additive)
                {
                    lastBlendMode = SpriteBlendMode.AlphaBlend;

                    GraphicsBatch.End();

                    GraphicsBatch.Begin(lastBlendMode,
                                      SpriteSortMode.Immediate,
                                      SaveStateMode.None, lookAtMatrix);
                }

                // Cache this to speed up calls
                Color whiteColor = Color.White;

                if (camera.IsSpriteInView(s))
                {

                    //SpriteBatch.Draw(s.Texture, new Vector2(0, 0), Color.Black);


                    float x = s.X;
                    float y = s.Y;

                    if (s.Visible && s.Texture != null)
                    {



                        float leftPixel = 0;
                        float rightPixel = 32;
                        float topPixel = 0;
                        float bottomPixel = 32;

                        float textureWidth = 32;
                        float textureHeight = 32;

                        

                        if (s.Texture != null)
                        {
                            leftPixel = s.LeftTextureCoordinate * s.Texture.Width;
                            rightPixel = s.RightTextureCoordinate * s.Texture.Width;
                            topPixel = s.TopTextureCoordinate * s.Texture.Height;
                            bottomPixel = s.BottomTextureCoordinate * s.Texture.Height;

                            textureWidth = (float)s.Texture.Width;
                            textureHeight = (float)s.Texture.Height;
                        }

                        DoubleRectangle textureRectangle = new DoubleRectangle(leftPixel, topPixel,
                                                       rightPixel - leftPixel,
                                                       bottomPixel - topPixel);

                        float xOffsetForTopLeftDrawing = -s.ScaleX;
                        float yOffsetForTopLeftDrawing = -s.ScaleY;

                        // positive rotation should be counterclockwise, but for some reason
                        // it's clockwise.  Invert for now.  Man, I don't like these hacks.
                        float rotationToUse = -s.RotationZ;


                        FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(
                            0, 0,
                            ref xOffsetForTopLeftDrawing, ref yOffsetForTopLeftDrawing, rotationToUse);



                        Vector2 position = new Vector2(x + xOffsetForTopLeftDrawing,// - s.ScaleX,
                            -y + yOffsetForTopLeftDrawing);// - s.ScaleY);

                        Vector2 origin = new Vector2(0, 0);

                        // this keeps gaps from happening
                        const float extraScale = .05f;

                        Vector2 scale = 
                            new Vector2(
                                (extraScale + 2 * s.ScaleX) / (float)textureRectangle.Width,
                                (extraScale + 2 * s.ScaleY) / (float)textureRectangle.Height);

                        ShaderEffect effect = Renderer.GetShaderEffectForColorOperation(s.ColorOperation,
                            s.Red, s.Green, s.Blue, s.Alpha);

                        SpriteEffects flipValue = SpriteEffects.None;

                        if (s.FlipHorizontal)
                            flipValue |= SpriteEffects.FlipHorizontally;
                        if (s.FlipVertical)
                            flipValue |= SpriteEffects.FlipVertically;


                        if (effect == null)
                        {
                            whiteColor.A = (byte)(255 * s.Alpha);

                            GraphicsBatch.Draw(s.Texture,
                                     position,
                                     textureRectangle,
                                     whiteColor,
                                     rotationToUse,
                                     origin,
                                     scale, //scale
                                     flipValue, 0);
                        }
                        else
                        {
                            GraphicsBatch.Draw(s.Texture,
                                     position,
                                     textureRectangle,
                                     effect,
                                     rotationToUse,
                                     origin,
                                     scale, //scale
                                     flipValue, 0);
                        }
                    }
                }
            }

            GraphicsBatch.End();


        }

        private static void DrawSpritesNew<T>(IList<T> spritesToDraw, int startIndex, int numberToDraw, Camera camera) where T : Sprite
        {
            if (numberToDraw == 0)
            {
                return;
            }

            float zDistanceForPixelperfect = 0;
            
            if(camera.Orthogonal == false)
            {
                zDistanceForPixelperfect = camera.GetZDistanceForPixelPerfect();
            }

            // Vic says: I don't think this is needed now that the Renderer is handling mixed drawing
            //mAutomaticallyUpdatedSprites.SortZInsertionAscending();
            SpriteBlendMode lastBlendMode = SpriteBlendMode.AlphaBlend;

            // Vic says: At the time of this writing Additive blend isn't working properly
            if (spritesToDraw[startIndex].BlendOperation == BlendOperation.Add)
            {
                lastBlendMode = SpriteBlendMode.Additive;
            }

            Matrix lookAtMatrix = camera.GetLookAtMatrix();

            GraphicsBatch.Begin(lastBlendMode,
                              SpriteSortMode.Immediate,
                              SaveStateMode.None,
                              lookAtMatrix
                              );

            int endIndex = startIndex + numberToDraw;

            for (int i = startIndex; i < endIndex; i++)
            {

                Sprite s = spritesToDraw[i];

                if (s.BlendOperation == BlendOperation.Add && lastBlendMode == SpriteBlendMode.AlphaBlend)
                {
                    lastBlendMode = SpriteBlendMode.Additive;

                    GraphicsBatch.End();

                    GraphicsBatch.Begin(lastBlendMode,
                                      SpriteSortMode.Immediate,
                                      SaveStateMode.None, lookAtMatrix);
                }
                if (s.BlendOperation == BlendOperation.Regular && lastBlendMode == SpriteBlendMode.Additive)
                {
                    lastBlendMode = SpriteBlendMode.AlphaBlend;

                    GraphicsBatch.End();

                    GraphicsBatch.Begin(lastBlendMode,
                                      SpriteSortMode.Immediate,
                                      SaveStateMode.None, lookAtMatrix);
                }

                // Cache this to speed up calls
                Color whiteColor = Color.White;

                if (camera.IsSpriteInView(s))
                {

                    //SpriteBatch.Draw(s.Texture, new Vector2(0, 0), Color.Black);


                    if (s.Visible && s.Texture != null)
                    {



                        float leftPixel = 0;
                        float rightPixel = 32;
                        float topPixel = 0;
                        float bottomPixel = 32;

                        float textureWidth = 32;
                        float textureHeight = 32;



                        if (s.Texture != null)
                        {
                            leftPixel = s.LeftTextureCoordinate * s.Texture.Width;
                            rightPixel = s.RightTextureCoordinate * s.Texture.Width;
                            topPixel = s.TopTextureCoordinate * s.Texture.Height;
                            bottomPixel = s.BottomTextureCoordinate * s.Texture.Height;

                            textureWidth = (float)s.Texture.Width;
                            textureHeight = (float)s.Texture.Height;
                        }

                        DoubleRectangle textureRectangle = new DoubleRectangle(leftPixel, topPixel,
                                                       rightPixel - leftPixel,
                                                       bottomPixel - topPixel);

                        float scaleX = s.ScaleX;
                        float scaleY = s.ScaleY;

                        float x = s.X;
                        float y = s.Y;

                        if(camera.Orthogonal == false)
                        {
                            float distanceFromCamera = camera.Z - s.Z;

                            float ratio = zDistanceForPixelperfect / distanceFromCamera;

                            scaleX *= ratio;
                            scaleY *= ratio;
                            
                            float differenceX = x - camera.X;
                            float differenceY = y - camera.Y;

                            differenceX *= ratio;
                            differenceY *= ratio;

                            x = camera.Position.X + differenceX;
                            y = camera.Position.Y + differenceY;
                        }

                        float xOffsetForTopLeftDrawing = -scaleX;
                        float yOffsetForTopLeftDrawing = -scaleY;

                        // positive rotation should be counterclockwise, but for some reason
                        // it's clockwise.  Invert for now.  Man, I don't like these hacks.
                        float rotationToUse = -s.RotationZ;


                        FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(
                            0, 0,
                            ref xOffsetForTopLeftDrawing, ref yOffsetForTopLeftDrawing, rotationToUse);



                        Vector2 position = new Vector2(x + xOffsetForTopLeftDrawing,// - s.ScaleX,
                            -y + yOffsetForTopLeftDrawing);// - s.ScaleY);

                        Vector2 origin = new Vector2(0, 0);

                        // this keeps gaps from happening
                        const float extraScale = .05f;

                        Vector2 scale =
                            new Vector2(
                                (extraScale + 2 * scaleX) / (float)textureRectangle.Width,
                                (extraScale + 2 * scaleY) / (float)textureRectangle.Height);

                        ShaderEffect effect = Renderer.GetShaderEffectForColorOperation(s.ColorOperation,
                            s.Red, s.Green, s.Blue, s.Alpha);

                        SpriteEffects flipValue = SpriteEffects.None;

                        if (s.FlipHorizontal)
                            flipValue |= SpriteEffects.FlipHorizontally;
                        if (s.FlipVertical)
                            flipValue |= SpriteEffects.FlipVertically;


                        if (effect == null)
                        {
                            whiteColor.A = (byte)(255 * s.Alpha);

                            GraphicsBatch.Draw(s.Texture,
                                     position,
                                     textureRectangle,
                                     whiteColor,
                                     rotationToUse,
                                     origin,
                                     scale, //scale
                                     flipValue, 0);
                        }
                        else
                        {
                            GraphicsBatch.Draw(s.Texture,
                                     position,
                                     textureRectangle,
                                     effect,
                                     rotationToUse,
                                     origin,
                                     scale, //scale
                                     flipValue, 0);
                        }
                    }
                }
            }

            GraphicsBatch.End();
        }

    }
}
