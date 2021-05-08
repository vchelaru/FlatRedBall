using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TMXGlueLib
{
    public class TileAnimation
    {
        [XmlElement("frame")]
        public List<TileAnimationFrame> Frames
        {
            get;
            set;
        }

        public TileAnimation()
        {
            Frames = new List<TileAnimationFrame>();
        }
    }
}
