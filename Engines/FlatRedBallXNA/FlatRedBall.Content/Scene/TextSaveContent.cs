using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Attributes;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using FlatRedBall.Content.Saves;

namespace FlatRedBall.Content.Scene
{
    public class TextSaveContent : TextSaveBase
    {
#if !XBOX360 && !FRB_MDX
        [XmlIgnore]
        [InstanceMember("mFontTextureInstance")]
        public ExternalReference<TextureContent> FontTextureReference;


        public string FontPatternText;
#endif
    }
}
