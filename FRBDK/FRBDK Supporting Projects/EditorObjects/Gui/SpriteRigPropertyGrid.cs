using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Gui;
using FlatRedBall;

namespace EditorObjects.Gui
{
    public class SpriteRigPropertyGrid : PropertyGrid<SpriteRig>
    {
        #region Fields

        ListDisplayWindow mJointsListDisplayWindow;
        ListDisplayWindow mBodySpritesListDisplayWindow;
        ListDisplayWindow mAttachmentsListDisplayWindow;

        #endregion

        #region Delegate Definitions

        public delegate void SelectSprite(Sprite spriteToSelect);

        #endregion

        #region Properties

        public override SpriteRig SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                Visible = (SelectedObject != null);
            }
        }

        #endregion

        #region Events

        public event SelectSprite SelectBodySprite;
        public event SelectSprite SelectJointSprite;

        #endregion

        #region Event Methods

        private void BodySpriteListClick(Window callingWindow)
        {
            ListBoxBase asListDisplayWindow = callingWindow as ListBoxBase;

            if (SelectBodySprite != null)
                SelectBodySprite(asListDisplayWindow.GetFirstHighlightedObject() as Sprite);
                
        }

        private void JointSpriteListClick(Window callingWindow)
        {
            ListBoxBase asListDisplayWindow = callingWindow as ListBoxBase;

            if (SelectJointSprite != null)
                SelectJointSprite(asListDisplayWindow.GetFirstHighlightedObject() as Sprite);
        }

        #endregion

        #region Methods

        #region Constructor

        public SpriteRigPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            Name = "SpriteRig";

            #region Exclude/Include members
            ExcludeAllMembers();

            IncludeMember("BodySprites", "BodySprites");
            IncludeMember("Joints", "Joints");
            IncludeMember("Attachments", "Attachments");
            #endregion

            #region Create the BodySprites ListDisplayWindow
            mBodySpritesListDisplayWindow = new ListDisplayWindow(mCursor);
            mBodySpritesListDisplayWindow.ScaleX = 8;
            mBodySpritesListDisplayWindow.ScaleY = 12;
            mBodySpritesListDisplayWindow.ListBox.Click += BodySpriteListClick;

            ReplaceMemberUIElement("BodySprites", mBodySpritesListDisplayWindow);
            this.SetMemberDisplayName("BodySprites", "");
            #endregion

            #region Create the Joints ListDisplayWindow
            mJointsListDisplayWindow = new ListDisplayWindow(mCursor);
            mJointsListDisplayWindow.ScaleX = 8;
            mJointsListDisplayWindow.ScaleY = 12;
            mJointsListDisplayWindow.ListBox.Click += JointSpriteListClick;

            ReplaceMemberUIElement("Joints", mJointsListDisplayWindow);
            this.SetMemberDisplayName("Joints", "");
            #endregion

            #region Create the Attachments ListDisplayWindow
            mAttachmentsListDisplayWindow = new ListDisplayWindow(mCursor);
            mAttachmentsListDisplayWindow.ScaleX = 8;
            mAttachmentsListDisplayWindow.ScaleY = 12;
            //mAttachmentsListDisplayWindow.ListBox.Click += AttachmentsListDisplayWindowClick;

            ReplaceMemberUIElement("Attachments", mAttachmentsListDisplayWindow);
            this.SetMemberDisplayName("Attachments", "");

            #endregion

            X = SpriteManager.Camera.XEdge;
            Y = SpriteManager.Camera.YEdge;

            RemoveCategory("Uncategorized");

            SelectCategory("BodySprites");
        }

        #endregion

        #region Public Methods

        public void HighlightJointNoCallback(Sprite joint)
        {
            mJointsListDisplayWindow.HighlightObjectNoCall(joint, false);
        }
        public void HighlightBodySpriteNoCallback(Sprite bodySprite)
        {
            mBodySpritesListDisplayWindow.HighlightObjectNoCall(bodySprite, false);

        }

        #endregion

        #endregion
    }
}
