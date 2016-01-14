using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Texture;

namespace FlatRedBall.IO.Gif
{
    #region XML Docs
    /// <summary>
    /// A list of ImageDatas with timing information on each element.
    /// </summary>
    /// <remarks>
    /// This class is used when loading GIF files to an AnimationChain.
    /// </remarks>
    #endregion
    public class ImageDataList : List<ImageData>
    {
        List<double> mFrameTimes = new List<double>();

        public List<Double> FrameTimes
        {
            get { return mFrameTimes; }
        }
    }
}
