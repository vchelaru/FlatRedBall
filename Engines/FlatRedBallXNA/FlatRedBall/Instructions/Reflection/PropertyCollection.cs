using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;

namespace FlatRedBall.Instructions.Reflection
{
    #region XML Docs
    /// <summary>
    /// Stores a list of properties and values for those properties (PropertyValuePair)
    /// </summary>
    /// <remarks>
    /// This class can be used to either store states of objects or abstract the setting of states.
    /// </remarks>
    #endregion
    public class PropertyCollection : INameable
    {
        #region Fields

        List<PropertyValuePair> mObjectPropertyPairs;

        string mName;

        #endregion

        #region Properties
        public string Name
        {
            get{ return mName;}
            set{ mName = value;}
        }
        #endregion

        #region Methods

        public PropertyCollection()
        {
            mObjectPropertyPairs = new List<PropertyValuePair>();
        }

        public void Add(string property, object value)
        {
            mObjectPropertyPairs.Add(new PropertyValuePair(property, value));
        }

        public void ApplyTo<T>(T objectToApplyTo)
        {
            for (int i = 0; i < mObjectPropertyPairs.Count; i++)
            {
                LateBinder<T>.Instance.SetProperty<object>(objectToApplyTo,
                    mObjectPropertyPairs[i].Property, mObjectPropertyPairs[i].Value);
            }
        }

        public void Clear()
        {
            mObjectPropertyPairs.Clear();
        }

        #endregion
    }
}
