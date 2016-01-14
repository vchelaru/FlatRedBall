using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;




namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// Object which holds a list of strings that can be used
    /// to tell the PropertyGrid which members to exclude when
    /// creating a new PropertyGrid of a specific type.
    /// </summary>
    #endregion
    public class PropertyGridMemberSettings : List<string>
    {
        public void ExcludeMembersInType(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (this.Contains(propertyInfo.Name) == false)
                {
                    Add(propertyInfo.Name);
                }
            }

            FieldInfo[] fields = type.GetFields();

            foreach (FieldInfo fieldInfo in fields)
            {
                if (this.Contains(fieldInfo.Name) == false)
                {
                    Add(fieldInfo.Name);
                }
            }
        }
    }
}
      