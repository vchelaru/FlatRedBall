using System;
using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Collections;
using FlatRedBall.Instructions;
using System.Collections;
using System.Collections.Generic;

using SpriteEditor.SEPositionedObjects;
using SpriteEditor.Gui;

namespace SpriteEditor
{
	/// <summary>
	/// Summary description for EditorSpriteInfo.
	/// </summary>
	public class EditorSprite : Sprite, ISpriteEditorObject
    {
        #region Fields
        float mStoredXVelocity;
        float mStoredYVelocity;
        float mStoredZVelocity;
		float mStoredFadeRate;
		
	
		List<float> mHorizontalSnaps;
		List<float> mVerticalSnaps;

		string mFirstHorizontal;
		string mSecondHorizontal;
		string mFirstVertical;
		string mSecondVertical;

		string mCollisionType;
		bool mCollisionRotation;
		bool mCollisionAllowance;
		bool mCollisionMove;


		bool mConstantPixelSizeExempt;
        #endregion

        #region Properties

        #region collision
        public string CollisionType 
        {
            get { return mCollisionType; }
            set { mCollisionType = value; }
        }
        public bool CollisionRotation 
        {
            get { return mCollisionRotation; }
            set { mCollisionRotation = value; }
        }
        public bool CollisionAllowance 
        {
            get { return mCollisionAllowance; }
            set { mCollisionAllowance = value; }
        }
        public bool CollisionMove 
        {
            get { return mCollisionMove; }
            set { mCollisionMove = value; }
        }
        #endregion

        public string type
        {
            get;
            set;
        }

        public bool ConstantPixelSizeExempt
        {
            get { return mConstantPixelSizeExempt; }
            set { mConstantPixelSizeExempt = value; }
        }
        #endregion

        #region methods

        public EditorSprite() : base()
		{
		}

        public void SetFromRegularSprite(Sprite spriteToSetFrom)
        {
            X = spriteToSetFrom.X;
            Y = spriteToSetFrom.Y;
            Z = spriteToSetFrom.Z;

            ScaleX = spriteToSetFrom.ScaleX;
            ScaleY = spriteToSetFrom.ScaleY;

            RelativePosition = spriteToSetFrom.RelativePosition;
            RelativeRotationMatrix = spriteToSetFrom.RelativeRotationMatrix;

            RotationX = spriteToSetFrom.RotationX;
            RotationY = spriteToSetFrom.RotationY;
            RotationZ = spriteToSetFrom.RotationZ;

            Texture = spriteToSetFrom.Texture;

            Name = spriteToSetFrom.Name;

            Red = spriteToSetFrom.Red;
            Blue = spriteToSetFrom.Blue;
            Green = spriteToSetFrom.Green;

            ColorOperation = spriteToSetFrom.ColorOperation;
            BlendOperation = spriteToSetFrom.BlendOperation;

            LeftTextureCoordinate = spriteToSetFrom.LeftTextureCoordinate;
            RightTextureCoordinate = spriteToSetFrom.RightTextureCoordinate;
            TopTextureCoordinate = spriteToSetFrom.TopTextureCoordinate;
            BottomTextureCoordinate = spriteToSetFrom.BottomTextureCoordinate;

            Alpha = spriteToSetFrom.Alpha;

            Animate = spriteToSetFrom.Animate;

            BlendOperation = spriteToSetFrom.BlendOperation;
            ColorOperation = spriteToSetFrom.ColorOperation;


        }

        #endregion
    }
}
