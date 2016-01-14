using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Attributes;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using FlatRedBall.Graphics.Animation;

namespace FlatRedBall.Content.AnimationChain
{
    public class AnimationFrameSaveContent : AnimationFrameSaveBase
    {

#if !FRB_MDX
        [XmlIgnore]
        [InstanceMember("mTextureInstance")]
        public ExternalReference<TextureContent> TextureReference;

#endif

        public AnimationFrameSaveContent() { }

        public AnimationFrameSaveContent(AnimationFrame template)
        {
            FrameLength = template.FrameLength;
            TextureName = template.TextureName;
            FlipVertical = template.FlipVertical;
            FlipHorizontal = template.FlipHorizontal;

            LeftCoordinate = template.LeftCoordinate;
            RightCoordinate = template.RightCoordinate;
            TopCoordinate = template.TopCoordinate;
            BottomCoordinate = template.BottomCoordinate;

            RelativeX = template.RelativeX;
            RelativeY = template.RelativeY;
        }

    }
}
