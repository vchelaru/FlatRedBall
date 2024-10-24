using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;


//using FlatRedBall.Content;
using FlatRedBall.Input;
using FlatRedBall.Gui;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Microsoft.Xna.Framework;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using FlatRedBall.Utilities;

namespace FlatRedBall
{
    public static class SpriteGridListExtensionMethods
    {
        [Obsolete]
        public static SpriteGrid FindByName(this List<SpriteGrid> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == name)
                {
                    return list[i];
                }
            }
            return null;
        }

    }


    public class Scene : IEquatable<Scene>, IMouseOver
    {
        #region Fields

        string mName;

        SpriteList mSprites;
        [Obsolete]
        List<SpriteGrid> mSpriteGrids;

        PositionedObjectList<SpriteFrame> mSpriteFrames;
        PositionedObjectList<Text> mTexts;

        #endregion

        #region Properties

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public SpriteList Sprites
        {
            get { return mSprites; }
        }

        [Obsolete]
        public List<SpriteGrid> SpriteGrids
        {
            get { return mSpriteGrids; }
        }

        public PositionedObjectList<SpriteFrame> SpriteFrames
        {
            get { return mSpriteFrames; }
            set { mSpriteFrames = value; }
        }

        public PositionedObjectList<Text> Texts
        {
            get { return mTexts; }
            set { mTexts = value; }
        }

        public bool Visible
        {
            set
            {
                for (int i = 0; i < mSprites.Count; i++)
                {
                    mSprites[i].Visible = value;
                }

                for (int i = 0; i < mSpriteGrids.Count; i++)
                {
                    
                    SpriteGrid spriteGrid = mSpriteGrids[i];

                    spriteGrid.Blueprint.Visible = value;

                    for (int j = 0; j < spriteGrid.VisibleSprites.Count; j++)
                    {
                        spriteGrid.VisibleSprites[j].Visible = value;
                    }
                }

                for (int i = 0; i < mSpriteFrames.Count; i++)
                {
                    mSpriteFrames[i].Visible = value;
                }

                for (int i = 0; i < mTexts.Count; i++)
                {
                    mTexts[i].Visible = value;
                }
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public Scene()
        {
            mSprites = new SpriteList();
            mSprites.Name = "Scene SpriteList";

            mSpriteGrids = new List<SpriteGrid>();
            
            mSpriteFrames = new PositionedObjectList<SpriteFrame>();
            mSpriteFrames.Name = "Scene SpriteFrame PositionedObjectList";

            mTexts = new PositionedObjectList<Text>();
            mTexts.Name = "Scene Text PositionedObjectList";
        }

        #endregion

        #region Public Methods

        public void AddToThis(Scene scene)
        {
            mSprites.AddRange(scene.mSprites);
            mSpriteGrids.AddRange(scene.mSpriteGrids);
            mSpriteFrames.AddRange(scene.mSpriteFrames);
            mTexts.AddRange(scene.mTexts);

        }


        public void AddToManagers()
        {
            AddToManagers(null);
        }


        public void AddToManagers(Layer layer)
        {
            #region Add the Sprites
            if (layer == null)
            {
                for (int i = 0; i < Sprites.Count; i++)
                {
                    Sprite sprite = Sprites[i];

                    if (sprite.mOrdered)
                    {
                        SpriteManager.AddSprite(sprite);
                    }
                    else
                    {
                        SpriteManager.AddZBufferedSprite(sprite);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Sprites.Count; i++)
                {
                    Sprite sprite = Sprites[i];
                    SpriteManager.AddToLayer(sprite, layer);
                }
            }
            #endregion

            #region Add the SpriteGrids
            for (int i = 0; i < SpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = SpriteGrids[i];
                spriteGrid.Layer = layer;

                spriteGrid.PopulateGrid();
                spriteGrid.RefreshPaint();
                spriteGrid.Manage();
            }
            #endregion

            #region Add the SpriteFrames
            for (int i = 0; i < SpriteFrames.Count; i++)
            {
                SpriteFrame frame = SpriteFrames[i];
                if (frame.CenterSprite != null && frame.CenterSprite.mOrdered == false)
                {
                    SpriteManager.AddToLayerZBuffered(frame, layer);
                }
                else
                {
                    SpriteManager.AddSpriteFrame(frame);
                    SpriteManager.AddToLayer(frame, layer);
                }
            }
            #endregion

            #region Add the Texts

            for (int i = 0; i < Texts.Count; i++)
            {
                if (layer == null)
                {
                    TextManager.AddText(Texts[i]);
                }
                else
                {
                    TextManager.AddText(Texts[i], layer);
                }
            }

            #endregion
        }


        public void AttachAllDetachedTo(PositionedObject newParent, bool changeRelative)
        {
            mSprites.AttachAllDetachedTo(newParent, changeRelative);

            mSpriteFrames.AttachAllDetachedTo(newParent, changeRelative);
            mTexts.AttachAllDetachedTo(newParent, changeRelative);
        }




        public void AttachTo(PositionedObject newParent, bool changeRelative)
		{

			mSprites.AttachTo(newParent, changeRelative);

			mSpriteFrames.AttachTo(newParent, changeRelative);
			mTexts.AttachTo(newParent, changeRelative);
		}


		public void Clear()
        {
            mSprites.Clear();
            mSpriteGrids.Clear();
            mSpriteFrames.Clear();
            mTexts.Clear();
        }


        public Scene Clone()
        {
            Scene scene = new Scene();

            #region Create the Sprites
            for (int i = 0; i < mSprites.Count; i++)
            {
                Sprite sprite = mSprites[i];
                scene.mSprites.Add(sprite.Clone());
            }

            for (int i = 0; i < mSprites.Count; i++)
            {
                Sprite thisSprite = mSprites[i];

                if (thisSprite.Parent != null)
                {
                    Sprite otherSprite = scene.mSprites[i];

                    otherSprite.AttachTo(
                        scene.mSprites.FindByName(thisSprite.Parent.Name), false);
                }
            }

            #endregion

            #region Create the SpriteGrids
            for (int i = 0; i < mSpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = mSpriteGrids[i];
                scene.mSpriteGrids.Add(spriteGrid.Clone());
            }
            #endregion

            #region Create and Attach the SpriteFrames
            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                SpriteFrame spriteFrame = mSpriteFrames[i];
                scene.mSpriteFrames.Add(spriteFrame.Clone());
            }
            #endregion

            #region Create and attach the Texts

            for (int i = 0; i < mTexts.Count; i++)
            {
                Text text = mTexts[i];
                scene.mTexts.Add(text.Clone());
            }

            for (int i = 0; i < mTexts.Count; i++)
            {
                Text thisText = mTexts[i];
                
                if (thisText.Parent != null)
                {
                    Text otherText = scene.mTexts[i];
                    otherText.AttachTo(
                        scene.mTexts.FindByName(thisText.Parent.Name), false);
                }

            }

            #endregion

            scene.Name = Name;

            return scene;
        }


        public void CopyAbsoluteToRelative()
        {
            CopyAbsoluteToRelative(true);
        }

        public void CopyAbsoluteToRelative(bool includeItemsWithParent)
        {
            mSprites.CopyAbsoluteToRelative(includeItemsWithParent);
            //mSpriteGrids;

            mSpriteFrames.CopyAbsoluteToRelative(includeItemsWithParent);
            mTexts.CopyAbsoluteToRelative(includeItemsWithParent);

        }


        public INameable FindByName(string name)
        {
            Sprite sprite = mSprites.FindByName(name);
            {
                if (sprite != null)
                    return sprite;
            }

            foreach (SpriteGrid spriteGrid in mSpriteGrids)
            {
                if (spriteGrid.Name == name)
                {
                    return spriteGrid;
                }
            }


            SpriteFrame spriteFrame = mSpriteFrames.FindByName(name);
            {
                if (spriteFrame != null)
                    return spriteFrame;
            }

            Text text = mTexts.FindByName(name);
            {
                if (text != null)
                    return text;
            }

            return null;
        }


        public void InvertHandedness()
        {
            int count = mSprites.Count;
            for(int i = 0; i < count; i++)
            {
                Sprite s = mSprites[i];
                s.InvertHandedness();

            }

            foreach (SpriteGrid sg in mSpriteGrids)
            {
                sg.InvertZ();
            }

            count = mSpriteFrames.Count;
            for(int i = 0; i < count; i++)
            {
                SpriteFrame sf = mSpriteFrames[i];
                sf.InvertHandedness();
            }

            count = mTexts.Count;
            for(int i = 0; i < count; i++)
            {
                Text text = mTexts[i];
                text.InvertHandedness();
            }
        }


        public void ConvertToManuallyUpdated()
        {
            SpriteManager.ConvertToManuallyUpdated(mSprites);

            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                SpriteManager.ConvertToManuallyUpdated(mSpriteFrames[i]);
            }


            for (int i = 0; i < mTexts.Count; i++)
            {
                TextManager.ConvertToManuallyUpdated(mTexts[i]);
            }

            for (int i = 0; i < mSpriteGrids.Count; i++)
            {
                mSpriteGrids[i].CreatesAutomaticallyUpdatedSprites = false;
                for(int j = 0; j < mSpriteGrids[i].mVisibleSprites.Count; j++)
                {
                    SpriteManager.ConvertToManuallyUpdated(mSpriteGrids[i].mVisibleSprites[j]);
                }
            }

        }


        public void ManageAll()
        {
            for (int i = 0; i < mSpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = mSpriteGrids[i];

                spriteGrid.Manage();
            }

            // No need to manage SpriteFrames now that they're automatically managed by the SpriteManager.
        }

        
        public void RemoveFromManagers()
        {
            // Clearing may remove some references, but
            // it can cause bugs if people want to reuse
            // Scenes, which is common for reusing levels
            // Therefore, let's not clear
            //RemoveFromManagers(true);
            RemoveFromManagers(false);

        }

        public void RemoveFromManagers(bool clearThis)
        {

            if (!clearThis)
            {
                MakeOneWay();
            }


            for (int i = mSprites.Count - 1; i > -1; i--)
            {
                Sprite sprite = mSprites[i];

                PositionedObject oldParent = sprite.Parent;

                SpriteManager.RemoveSprite(sprite);

                if (!clearThis && oldParent != null)
                {
                    sprite.AttachTo(oldParent, false);
                }
            }

            for (int i = mSpriteGrids.Count - 1; i > -1; i--)
            {
                //SpriteGrids don't get attached, so there is no code to preserve Parent and re-attach
                SpriteGrid spriteGrid = mSpriteGrids[i];

                spriteGrid.Destroy();
            }

            for(int i = mSpriteFrames.Count -1 ; i > -1; i--)
            {
                SpriteFrame spriteFrame = mSpriteFrames[i];

                PositionedObject oldParent = spriteFrame.Parent;

                SpriteManager.RemoveSpriteFrame(spriteFrame);

                if (!clearThis && oldParent != null)
                {
                    spriteFrame.AttachTo(oldParent, false);
                }
            }


            for (int i = mTexts.Count - 1; i > -1; i--)
            {
                Text text = mTexts[i];

                PositionedObject oldParent = text.Parent;

                TextManager.RemoveText(text);

                if (!clearThis && oldParent != null)
                {
                    text.AttachTo(oldParent, false);
                }
            }

            if (clearThis)
            {

                Clear();
            }
            else
            {
                MakeTwoWay();

            }
        }

        public void MakeTwoWay()
        {
            mSprites.MakeTwoWay();

            mSpriteFrames.MakeTwoWay();
            mTexts.MakeTwoWay();
            // The SpriteGrids is not a two-way list
        }

        public void MakeOneWay()
        {
            mSprites.MakeOneWay();

            mSpriteFrames.MakeOneWay();
            mTexts.MakeOneWay();

            // The SpriteGrids is not a two-way list
        }


        public void ScalePositionsAndScales(float value)
        {

            Vector3 amountToShiftBy = new Vector3(value, value, value);

            for (int i = 0; i < SpriteFrames.Count; i++)
            {
                SpriteFrame spriteFrame = SpriteFrames[i];

                spriteFrame.X *= amountToShiftBy.X;
                spriteFrame.Y *= amountToShiftBy.Y;
                spriteFrame.Z *= amountToShiftBy.Z;

                spriteFrame.RelativeX *= amountToShiftBy.X;
                spriteFrame.RelativeY *= amountToShiftBy.Y;
                spriteFrame.RelativeZ *= amountToShiftBy.Z;

                spriteFrame.ScaleX *= amountToShiftBy.X;
                spriteFrame.ScaleY *= amountToShiftBy.Y;
            }

            for (int i = 0; i < Sprites.Count; i++)
            {
                Sprite sprite = Sprites[i];


                Sprites[i].X *= amountToShiftBy.X;
                Sprites[i].Y *= amountToShiftBy.Y;
                Sprites[i].Z *= amountToShiftBy.Z;

                Sprites[i].RelativeX *= amountToShiftBy.X;
                Sprites[i].RelativeY *= amountToShiftBy.Y;
                Sprites[i].RelativeZ *= amountToShiftBy.Z;

                Sprites[i].ScaleX *= amountToShiftBy.X;
                Sprites[i].ScaleY *= amountToShiftBy.Y;
            }

            for (int i = 0; i < Texts.Count; i++)
            {
                Texts[i].X *= amountToShiftBy.X;
                Texts[i].Y *= amountToShiftBy.Y;
                Texts[i].Z *= amountToShiftBy.Z;

                Texts[i].RelativeX *= amountToShiftBy.X;
                Texts[i].RelativeY *= amountToShiftBy.Y;
                Texts[i].RelativeZ *= amountToShiftBy.Z;

                Texts[i].Scale *= amountToShiftBy.X;
                Texts[i].Spacing *= amountToShiftBy.X;
                Texts[i].NewLineDistance *= amountToShiftBy.X;
                Texts[i].MaxWidth *= amountToShiftBy.X;
            }
        }


        public void Shift(Vector3 shiftVector)
        {
            mSprites.Shift(shiftVector);

            for (int i = 0; i < mSpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = mSpriteGrids[i];

                spriteGrid.Shift(shiftVector.X, shiftVector.Y, shiftVector.Z);
                spriteGrid.XLeftBound += shiftVector.X;
                spriteGrid.XRightBound += shiftVector.X;
                spriteGrid.YTopBound += shiftVector.Y;
                spriteGrid.YBottomBound += shiftVector.Y;
                spriteGrid.ZCloseBound += shiftVector.Z;
                spriteGrid.ZFarBound += shiftVector.Z;
            }

            mSpriteFrames.Shift(shiftVector);

            mTexts.Shift(shiftVector);
        }

        public void ShiftRelative(float x, float y, float z)
        {
            ShiftRelative(new Vector3(x, y, z));
        }

        public void ShiftRelative(Vector3 relativeShiftVector)
        {
            mSprites.ShiftRelative(relativeShiftVector);

            // SpriteGrids don't attach

            //for (int i = 0; i < mSpriteGrids.Count; i++)
            //{
            //    SpriteGrid spriteGrid = mSpriteGrids[i];

            //    spriteGrid.Shift(shiftVector.X, shiftVector.Y, shiftVector.Z);
            //}

            mSpriteFrames.ShiftRelative(relativeShiftVector);

            mTexts.ShiftRelative(relativeShiftVector);
        }


        public override string ToString()
        {
            return "Scene: " + mName;
        }


        public void UpdateDependencies(double currentTime)
        {
            for (int i = 0; i < mSprites.Count; i++)
            {
                Sprite sprite = mSprites[i];

                sprite.UpdateDependencies(currentTime);
            }


            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                SpriteFrame spriteFrame = mSpriteFrames[i];

                spriteFrame.UpdateDependencies(currentTime);
            }

            for (int i = 0; i < mTexts.Count; i++)
            {
                Text text = mTexts[i];

                text.UpdateDependencies(currentTime);
            }
        }


        #endregion

        #endregion

        #region IEquatable<Scene> Members

        bool IEquatable<Scene>.Equals(Scene other)
        {
            return this == other;
        }

        #endregion

        #region IMouseOver
        bool IMouseOver.IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this, null);
        }

        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion
    }
}
