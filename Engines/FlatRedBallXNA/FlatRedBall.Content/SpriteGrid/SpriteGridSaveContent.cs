using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Content.Scene;
using System.Xml.Serialization;
using FlatRedBall.Attributes;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace FlatRedBall.Content.SpriteGrid
{
    public class SpriteGridSaveContent : SpriteGridSaveBase<SpriteSaveContent>
    {

#if FRB_XNA
        [XmlIgnore]
        [InstanceMember("mTextureInstance")]
        public ExternalReference<TextureContent> BaseTextureReference;

        [XmlIgnore]
       [InstanceMember("mTextureGridInstance")]
       public ExternalReference<TextureContent>[][] GridTextureReferences;
#endif

        public SpriteGridSaveContent()
            : base()
        { }
        
    }
}
