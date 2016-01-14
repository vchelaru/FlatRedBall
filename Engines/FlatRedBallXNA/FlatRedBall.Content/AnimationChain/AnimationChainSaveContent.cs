using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Content.AnimationChain
{
    [XmlRoot("AnimationChain")]
    public class AnimationChainSaveContent : AnimationChainSaveBase<AnimationFrameSaveContent>
    {
        
        public AnimationChainSaveContent() { }

    }
}
