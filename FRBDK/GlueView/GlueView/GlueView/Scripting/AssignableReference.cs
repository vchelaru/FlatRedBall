using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GlueView.Scripting
{
    public class AssignableReference : IAssignableReference
    {
        #region Fields

        public FieldInfo FieldInfo
        {
            get;
            set;
        }

        public PropertyInfo PropertyInfo
        {
            get;
            set;
        }

        public object Owner
        {
            get;
            set;
        }

        public IAssignableReference Parent
        {
            get;
            set;
        }

        #endregion

        public Type TypeOfReference
        {
            get
            {
                if (FieldInfo != null)
                {
                    return FieldInfo.FieldType;
                }
                else if (PropertyInfo != null)
                {
                    return PropertyInfo.PropertyType;
                }
                else
                {
                    return null;
                }
            }
        }

        public object CurrentValue
        {
            get
            {
                if (FieldInfo != null)
                {
                    return FieldInfo.GetValue(Owner);
                }
                else if (PropertyInfo != null)
                {
                    return PropertyInfo.GetValue(Owner, null);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (FieldInfo != null)
                {
                    
                    FieldInfo.SetValue(Owner, value);

                    if (Parent != null && Parent.TypeOfReference.IsValueType)
                    {
                        Parent.CurrentValue = this.Owner;
                    }

                }
                else if (PropertyInfo != null)
                {
                    PropertyInfo.SetValue(Owner, value, null);
                }
            }
        }
    }
}
