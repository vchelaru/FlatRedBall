using System;
using System.Collections.Generic;
using FlatRedBall.Attributes;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Content.Scene
{
    public class SpriteSaveContent : SpriteSaveBase
    {

#if !XBOX360 && !FRB_MDX
        [XmlIgnore]
        [InstanceMember("mTextureInstance")]
        public ExternalReference<TextureContent> TextureReference;
#endif

//#if !XBOX360 && !FRB_MDX
//        [XmlIgnore]
//        [InstanceMember("mAnimationChainListInstance")]
//        public ExternalReference<AnimationChainListSaveContent> AnimationChainReference;
//#endif

//        [XmlIgnore]
//        public AnimationChainListSaveContent AnimationChains;
    }
}
