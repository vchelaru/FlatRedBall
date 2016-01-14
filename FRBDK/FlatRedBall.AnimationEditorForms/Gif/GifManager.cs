using FlatRedBall.Graphics.Texture;
using Gif.Components;
using Jillzhang.GifUtility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Gif
{
    public class GifManager : Singleton<GifManager>
    {
        public void SaveCurrentAnimationAsGif()
        {
            //String[] imageFilePaths = new String[] { "c:\\01.png", "c:\\02.png", "c:\\03.png" };
            //String outputFilePath = "c:\\test.gif";
            //AnimatedGifEncoder e = new AnimatedGifEncoder();
            //e.Start(outputFilePath);
            //e.SetDelay(500);
            ////-1:no repeat,0:always repeat
            //e.SetRepeat(0);
            //for (int i = 0, count = imageFilePaths.Length; i < count; i++)
            //{
            //    e.AddFrame(Image.FromFile(imageFilePaths[i]));
            //}
            //e.Finish();
            ///* extract Gif */
            //string outputPath = "c:\\";
            //GifDecoder gifDecoder = new GifDecoder();
            //gifDecoder.Read("c:\\test.gif");
            //for (int i = 0, count = gifDecoder.GetFrameCount(); i < count; i++)
            //{
            //    Image frame = gifDecoder.GetFrame(i); // frame i
            //    frame.Save(outputPath + Guid.NewGuid().ToString()
            //                          + ".png", ImageFormat.Png);
            //}

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Graphics Interchange Format (*.gif)|*.gif";
            string fileName = null;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                fileName = dialog.FileName;
                SaveGifFile(fileName);
            }

        }

        private static void SaveGifFile(string fileName)
        {

            List<Bitmap> bitmaps = GetBitmapList();


            SaveGifFromBitmaps(fileName, bitmaps);


        }


        private static void SaveGifFromBitmaps(string fileName, List<Bitmap> bitmaps)
        {

            // This code was pulled from a codeplex
            // example, but the GIFs that are saved have
            // weird colors and the last 2 pixels do not show
            // up properly.  I wish there was a better library for this...
            AnimatedGifEncoder e = new AnimatedGifEncoder();

            e.Start(fileName);
            e.SetDelay(1500);
            ////-1:no repeat,0:always repeat
            e.SetRepeat(0);


            foreach (var bitmap in bitmaps)
            {
                e.AddFrame(bitmap);
            }

            e.Finish();



            
        }

        private static List<Bitmap> GetBitmapList()
        {
            List<Bitmap> bitmaps = new List<Bitmap>();

            for (int i = 0; i < SelectedState.Self.SelectedChain.Frames.Count; i++)
            {
                var frame = SelectedState.Self.SelectedChain.Frames[i];

                var texture = WireframeManager.Self.GetTextureForFrame(frame);
                if (texture != null)
                {

                    FlatRedBall.Graphics.Texture.ImageData imageData =
                        ImageData.FromTexture2D(texture);

                    int left = Math.MathFunctions.RoundToInt(frame.LeftCoordinate * texture.Width);
                    int right = Math.MathFunctions.RoundToInt(frame.RightCoordinate * texture.Width);
                    int top = Math.MathFunctions.RoundToInt(frame.TopCoordinate * texture.Height);
                    int bottom = Math.MathFunctions.RoundToInt(frame.BottomCoordinate * texture.Height);

                    int width = right - left;
                    int height = bottom - top;

                    Bitmap bitmap = new Bitmap(width, height);
                    bitmaps.Add(bitmap);
                    for (int ySource = top; ySource < bottom; ySource++)
                    {
                        for (int xSource = left; xSource < right; xSource++)
                        {
                            int yDestination = ySource - top;
                            int xDestination = xSource - left;

                            Microsoft.Xna.Framework.Color sourceColor = imageData.GetPixelColor(xSource, ySource);
                            System.Drawing.Color destinationColor = Color.FromArgb(
                                sourceColor.A,
                                sourceColor.R,
                                sourceColor.G,
                                sourceColor.B);

                            bitmap.SetPixel(xDestination, yDestination, destinationColor);

                        }
                    }
                }
            }
            return bitmaps;
        }
    }
}
