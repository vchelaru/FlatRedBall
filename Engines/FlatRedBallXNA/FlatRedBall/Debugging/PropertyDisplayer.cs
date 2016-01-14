using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions.Reflection;
using System.Reflection;

namespace FlatRedBall.Debugging
{
    public class PropertyDisplayer<T> : IObjectDisplayer<T>
    {
        #region Fields

        Text mText;

        List<string> mIncludedFields = new List<string>();
        List<string> mIncludedProperties = new List<string>();

        T mObjectDisplayingAsObject;

        StringBuilder mStringBuilder = new StringBuilder();

        #endregion

        #region Properties

        public object ObjectDisplayingAsObject
        {
            get { return mObjectDisplayingAsObject; }
            set 
            {
                mObjectDisplayingAsObject = (T)value ; 
            }
        }

        public T ObjectDisplaying
        {
            get
            {
                return mObjectDisplayingAsObject;
            }
            set
            {
                mObjectDisplayingAsObject = value;
            }
        }

        #endregion

        #region Methods

        public PropertyDisplayer()
        {
            mText = TextManager.AddText("Hi");
            mText.Z = -FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 3;
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                mIncludedProperties.Add(pi.Name);
            }
        }

        public void ExcludeAllMembers()
        {
            mIncludedFields.Clear();
            mIncludedProperties.Clear();
        }

        public void ExcludeMember(string memberToExclude)
        {
            if (mIncludedFields.Contains(memberToExclude))
            {
                mIncludedFields.Remove(memberToExclude);
            }
            else if (mIncludedProperties.Contains(memberToExclude))
            {
                mIncludedProperties.Remove(memberToExclude);
            }
        }

        public void IncludeMember(string memberToInclude)
        {
            if (mObjectDisplayingAsObject.GetType().GetProperty(memberToInclude) != null)
            {
                mIncludedProperties.Add(memberToInclude);
            }
            else
            {
                mIncludedFields.Add(memberToInclude);
            }
        }

        public void UpdateToObject()
        {
            if(ObjectDisplayingAsObject != null)
            {
                UpdatePosition();

                UpdateDisplayText();
            }
        }

        #region Private Methods

        private void UpdateDisplayText()
        {
            mStringBuilder.Remove(0, mStringBuilder.Length);

            for (int i = 0; i < mIncludedFields.Count; i++)
            {

                object field = LateBinder<T>.Instance.GetField(mObjectDisplayingAsObject, mIncludedFields[i]);

                string fieldString = null;

                if(field != null)
                {
                    fieldString = field.ToString();
                }

                mStringBuilder.AppendLine(mIncludedFields[i] + ":" + fieldString);
            }

            for (int i = 0; i < mIncludedProperties.Count; i++)
            {
                string propertyName = mIncludedProperties[i];
                LateBinder<T> instance = LateBinder<T>.Instance;

                object property = instance.GetProperty(mObjectDisplayingAsObject, propertyName);


                string propertyString = null;

                if (property != null)
                {
                    propertyString = property.ToString();
                }

                mStringBuilder.AppendLine(propertyName + ":" + propertyString);

            }

            mText.DisplayText = mStringBuilder.ToString();
        }

        private void UpdatePosition()
        {
            mText.X = SpriteManager.Camera.AbsoluteLeftXEdgeAt(mText.Z);

            mText.Y = -mText.ScaleY +  SpriteManager.Camera.AbsoluteTopYEdgeAt(mText.Z);
            mText.SetPixelPerfectScale(SpriteManager.Camera);
        }

        #endregion

        #endregion

    }
}
