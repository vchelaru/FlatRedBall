using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Model;

namespace EditorObjects.Gui
{
    public static class WindowAssociationHelper
    {
        public static void SetSceneAssociations()
        {
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Scene), typeof(ScenePropertyGrid));
            
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Sprite), typeof(SpritePropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Text), typeof(TextPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(SpriteFrame), typeof(SpriteFramePropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(SpriteGrid), typeof(SpriteGridPropertyGrid));
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(PositionedModel), typeof(PositionedModelPropertyGrid));
        }

    }
}
