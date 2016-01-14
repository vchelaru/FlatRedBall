using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Collections;
using FlatRedBall.Math;
using SpriteEditor.Gui;


namespace SpriteEditor.SEPositionedObjects
{
    public class EditorCamera : Camera
    {

        #region Methods

        public EditorCamera(string contentManagerName)
            : base(contentManagerName)
        { }

        #endregion
    }
}
