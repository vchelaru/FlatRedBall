using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace SpriteEditor.File
{
    [Serializable]
    public class TextureReference
    {
        public string TextureFileName;

        public List<DisplayRegion> DisplayRegions = new List<DisplayRegion>();


    }
}
