using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics.Lighting;


namespace EditorObjects.Gui
{
    public class ScenePropertyGrid : PropertyGrid<Scene>
    {
        #region Fields

        ListDisplayWindow mSprites;
        ListDisplayWindow mSpriteFrames;
        ListDisplayWindow mTexts;
        ListDisplayWindow mPositionedModels;
        ListDisplayWindow mSpriteGrids;
        ListDisplayWindow mLights;

        #endregion

        #region Properties

        public Sprite CurrentSprite
        {
            get
            {
                return mSprites.GetFirstHighlightedObject() as Sprite;
            }
            set
            {
                mSprites.HighlightObject(value, false);
            }
        }

        public LightBase CurrentLight
        {
            get
            {
                return mLights.GetFirstHighlightedObject() as LightBase;
            }
            set
            {
                mLights.HighlightObject(value, false);
            }
        }

        public SpriteFrame CurrentSpriteFrame
        {
            get
            {
                return mSpriteFrames.GetFirstHighlightedObject() as SpriteFrame;
            }
            set
            {
                mSpriteFrames.HighlightObject(value, false);
            }
        }

        public Text CurrentText
        {
            get
            {
                return mTexts.GetFirstHighlightedObject() as Text;
            }
            set
            {
                mTexts.HighlightObject(value, false);
            }
        }

        public PositionedModel CurrentPositionedModel
        {
            get
            {
                return mPositionedModels.GetFirstHighlightedObject() as PositionedModel;
            }
            set
            {
                mPositionedModels.HighlightObject(value, false);
            }
        }

        public SpriteGrid CurrentSpriteGrid
        {
            get
            {
                return mSpriteGrids.GetFirstHighlightedObject() as SpriteGrid;
            }
            set
            {
                mSpriteGrids.HighlightObject(value, false);
            }
        }

        public bool ShowPropertyGridOnStrongSelect
        {
            get { return mSprites.ShowPropertyGridOnStrongSelect; }
            set
            {
                mSprites.ShowPropertyGridOnStrongSelect = value;
                mSpriteFrames.ShowPropertyGridOnStrongSelect = value;
                mSpriteGrids.ShowPropertyGridOnStrongSelect = value;
                mTexts.ShowPropertyGridOnStrongSelect = value;
                mPositionedModels.ShowPropertyGridOnStrongSelect = value;
                mLights.ShowPropertyGridOnStrongSelect = value;
            }
        }

        public override List<FlatRedBall.Instructions.InstructionList> UndoInstructions
        {
            set
            {
                base.UndoInstructions = value;

                mSprites.UndoInstructions = value;
                mSpriteFrames.UndoInstructions = value;
                mTexts.UndoInstructions = value;
                mPositionedModels.UndoInstructions = value;
                mSpriteGrids.UndoInstructions = value;
                mLights.UndoInstructions = value;
            }
        }

        #endregion

        #region Events

        public event GuiMessage SpriteSelected;
        public event GuiMessage SpriteFrameSelected;
        public event GuiMessage TextSelected;
        public event GuiMessage PositionedModelSelected;
        public event GuiMessage SpriteGridSelected;

        #endregion

        #region Event Methods

        private void SpriteListBoxClick(Window callingWindow)
        {
            if (SpriteSelected != null)
            {
                SpriteSelected(this);
            }
        }

        private void SpriteFrameListBoxClick(Window callingWindow)
        {
            if (SpriteFrameSelected != null)
            {
                SpriteFrameSelected(this);
            }
        }

        private void TextListBoxClick(Window callingWindow)
        {
            if (TextSelected != null)
            {
                TextSelected(this);
            }
        }

        private void PositionedModelListBoxClick(Window callingWindow)
        {
            if (PositionedModelSelected != null)
            {
                PositionedModelSelected(this);
            }
        }

        private void SpriteGridListBoxClick(Window callingWindow)
        {
            if (SpriteGridSelected != null)
            {
                SpriteGridSelected(this);
            }
        }

        #endregion

        #region Methods

        public ScenePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region Exclude/include members and create categories

            ExcludeMember("Name");
            ExcludeMember("Visible");


            IncludeMember("Sprites", "Sprites");
            IncludeMember("SpriteFrames", "SpriteFrames");
            IncludeMember("SpriteGrids", "SpriteGrids");
            IncludeMember("PositionedModels", "PositionedModels");
            IncludeMember("Texts", "Texts");
            IncludeMember("Lights", "Lights");


            RemoveCategory("Uncategorized");

            #endregion

            CreateListDisplayWindows();

            SetPropertyGridTypeAssociations();
        }

        private void CreateListDisplayWindows()
        {
            float listDisplayWindowScaleX = 10;
            float listDisplayWindowScaleY = 16;

            mSprites = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("Sprites", mSprites);
            mSprites.ScaleX = listDisplayWindowScaleX;
            mSprites.ScaleY = listDisplayWindowScaleY;
            mSprites.ListBox.Highlight += SpriteListBoxClick;
            mSprites.ConsiderAttachments = true;

            SetMemberDisplayName("Sprites", "");

            mSpriteFrames = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("SpriteFrames", mSpriteFrames);
            mSpriteFrames.ScaleX = listDisplayWindowScaleX;
            mSpriteFrames.ScaleY = listDisplayWindowScaleY;
            mSpriteFrames.ListBox.Highlight += SpriteFrameListBoxClick;
            mSpriteFrames.ConsiderAttachments = true;
            SetMemberDisplayName("SpriteFrames", "");

            mTexts = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("Texts", mTexts);
            mTexts.ScaleX = listDisplayWindowScaleX;
            mTexts.ScaleY = listDisplayWindowScaleY;
            mTexts.ListBox.Highlight += TextListBoxClick;
            mTexts.ConsiderAttachments = true;
            SetMemberDisplayName("Texts", "");

            mPositionedModels = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("PositionedModels", mPositionedModels);
            mPositionedModels.ScaleX = listDisplayWindowScaleX;
            mPositionedModels.ScaleY = listDisplayWindowScaleY;
            mPositionedModels.ListBox.Highlight += PositionedModelListBoxClick;
            mPositionedModels.ConsiderAttachments = true;
            SetMemberDisplayName("PositionedModels", "");

            mSpriteGrids = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("SpriteGrids", mSpriteGrids);
            mSpriteGrids.ScaleX = listDisplayWindowScaleX;
            mSpriteGrids.ScaleY = listDisplayWindowScaleY;
            mSpriteGrids.ListBox.Highlight += SpriteGridListBoxClick;
            SetMemberDisplayName("SpriteGrids", "");

            mLights = new ListDisplayWindow(this.mCursor);
            this.ReplaceMemberUIElement("Lights", mLights);
            mLights.ScaleX = listDisplayWindowScaleX;
            mLights.ScaleY = listDisplayWindowScaleY;
            mLights.ListBox.Highlight += SpriteGridListBoxClick;
            SetMemberDisplayName("Lights", "");            
        }

        private void SetPropertyGridTypeAssociations()
        {
            SetPropertyGridTypeAssociation(typeof(Sprite), typeof(SpritePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(SpriteGrid), typeof(SpriteGridPropertyGrid));
            // TODO:  Add SpriteGridPropertyGrid
            SetPropertyGridTypeAssociation(typeof(SpriteFrame), typeof(SpriteFramePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(Text), typeof(TextPropertyGrid));
            SetPropertyGridTypeAssociation(typeof(PositionedModel), typeof(PositionedModelPropertyGrid));
        }

        #endregion
    }
}
