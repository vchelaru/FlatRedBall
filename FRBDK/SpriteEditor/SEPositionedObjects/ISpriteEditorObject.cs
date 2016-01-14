using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Input;

namespace SpriteEditor.SEPositionedObjects
{
    public interface ISpriteEditorObject : IAttachable, ICursorSelectable
    {

        #region Properties

        float PixelSize { get; set;}

        string type { get; set; }


        bool ConstantPixelSizeExempt { get; set;}
        
        #endregion


        #region Methods

        //ISpriteEditorObject Clone();

        void SetFromRegularSprite(Sprite spriteToSetFrom);

        #endregion
    }
}
