using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Model;
using System.ComponentModel;

namespace EffectEditor.HelperClasses
{
    public class AnimationSelector : StringConverter
    {
        #region Methods

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (ModelEditor.CurrentModel.HasAnimation)
            {
                List<String> animationNames = ModelEditor.CurrentModel.GetAnimationNameList();
                animationNames.Insert(0, "");

                return new StandardValuesCollection(animationNames.ToArray());
            }
            else
            {
                return new StandardValuesCollection(new string[] { });
            }
        }

        #endregion
    }

    public class ModelEditor
    {
        #region Fields

        public static PositionedModel CurrentModel;
        PositionedModel mSelectedModel;
        string mCurrentAnimation;

        #endregion

        #region Properties

        [Browsable(false)]
        public PositionedModel SelectedModel
        {
            get { return mSelectedModel; }
            set { mSelectedModel = value; }
        }

        [Browsable(true),
         Category("Orientation"),
         Description("The model's rotation about the X axis")]
        public float RotationX
        {
            get { return mSelectedModel.RotationX; }
            set { mSelectedModel.RotationX = value; }
        }

        [Browsable(true),
         Category("Orientation"),
         Description("The model's rotation about the Y axis")]
        public float RotationY
        {
            get { return mSelectedModel.RotationY; }
            set { mSelectedModel.RotationY = value; }
        }

        [Browsable(true),
         Category("Orientation"),
         Description("The model's rotation about the Z axis")]
        public float RotationZ
        {
            get { return mSelectedModel.RotationZ; }
            set { mSelectedModel.RotationZ = value; }
        }

        [Browsable(true),
         Category("Animation"),
         Description("The model's current animation"),
         TypeConverter(typeof(AnimationSelector))]
        public String Animation
        {
            get { return mCurrentAnimation; }
            set
            {
                if (mSelectedModel.HasAnimation)
                {
                    if (value == "")
                    {
                        mSelectedModel.ClearAnimation();
                    }
                    else
                    {
                        mSelectedModel.SetAnimation(value);
                    }
                }
                mCurrentAnimation = value;
            }
        }

        #endregion

        #region Constructor

        public ModelEditor(PositionedModel model)
        {
            ModelEditor.CurrentModel = model;
            mSelectedModel = model;
            mCurrentAnimation = String.Empty;
        }

        #endregion
    }
}
