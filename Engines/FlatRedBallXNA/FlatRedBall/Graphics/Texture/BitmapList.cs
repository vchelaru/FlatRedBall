using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !SILVERLIGHT
using System.Drawing;
#endif

namespace FlatRedBall.Graphics.Texture
{
    #region XML Docs
    /// <summary>
    /// A list of Bitmaps which can be created from a loaded .gif.
    /// </summary>
    /// <remarks>
    /// This class can be created through the FlatRedBallServices.Load method.
    /// </remarks>
    #endregion
    public class BitmapList : List<Bitmap>, IDisposable
    {

        #region IDisposable Members

        public void Dispose()
        {
            foreach (Bitmap b in this)
            {
                b.Dispose();
            }

            this.Clear();
        }

        public static BitmapList FromFile(string fileName)
        {
            BitmapList bitmapList = new BitmapList();

            Image image = FlatRedBallServices.Load<Image>(fileName);

            // Vic says:
            // I have no idea why 0x5100 is the property, this is just something
            // I pulled from the Internets
            byte[] times = image.GetPropertyItem(0x5100).Value;

            int numberOfFrames =
                image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
            for (int i = 0; i < numberOfFrames; i++)
            {
                image.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, i);

                Bitmap bitmap = new Bitmap(image);
                bitmapList.Add(bitmap);
            }

            return bitmapList;

        }


        #endregion
    }
}
