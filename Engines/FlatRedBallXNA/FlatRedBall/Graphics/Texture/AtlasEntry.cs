using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Graphics.Texture
{
    public class AtlasEntry
    {
        #region Properties

        public int LeftPixel
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }

        public int TopPixel
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }

        public string OriginalName
        {
            get;
            set;
        }

        [XmlIgnore]
        public int ParentWidth
        {
            get;
            set;
        }

        [XmlIgnore]
        public int ParentHeight
        {
            get;
            set;
        }

        [XmlIgnore]
        public float LeftTextureCoordinate
        {
            get
            {
                return LeftPixel / (float)ParentWidth;
            }
        }

        [XmlIgnore]
        public float RightTextureCoordinate
        {
            get
            {
                return (LeftPixel + Width )/ (float)ParentWidth;
            }
        }

        [XmlIgnore]
        public float TopTextureCoordinate
        {
            get
            {
                return TopPixel / (float)ParentHeight;
            }
        }

        [XmlIgnore]
        public float BottomTextureCoordinate
        {
            get
            {
                return (TopPixel + Height) / (float)ParentHeight;
            }
        }
        #endregion

        public void FullToReduced(float fullLeft, float fullRight, float fullTop, float fullBottom,
            out float reducedLeft, out float reducedRight, out float reducedTop, out float reducedBottom)
        {
            reducedLeft = LeftTextureCoordinate + fullLeft * (RightTextureCoordinate - LeftTextureCoordinate);
            reducedRight = LeftTextureCoordinate + fullRight * (RightTextureCoordinate - LeftTextureCoordinate);
            reducedTop = TopTextureCoordinate + fullTop * (BottomTextureCoordinate - TopTextureCoordinate);
            reducedBottom = TopTextureCoordinate + fullBottom * (BottomTextureCoordinate - TopTextureCoordinate);
        }
    }
}
