using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TMXGlueLib
{
    [XmlRoot("frame")]
    public class TileAnimationFrame
    {
        [XmlAttribute("tileid")]
        public int TileId
        {
            get;
            set;
        }

        [XmlAttribute("duration")]
        public int Duration
        {
            get;
            set;
        }


    }
}
