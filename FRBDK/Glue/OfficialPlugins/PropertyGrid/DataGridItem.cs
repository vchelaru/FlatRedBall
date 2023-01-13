using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using WpfDataUi.DataTypes;
using static System.ComponentModel.TypeConverter;

namespace OfficialPlugins.VariableDisplay
{
    class DataGridItem : InstanceMember
    {
        bool isDefault;

        TypeConverter typeConverter;

        bool? isExplicitlyReadOnly;
        public override bool IsReadOnly
        {
            get => isExplicitlyReadOnly ?? base.IsReadOnly;
        }

        public TypeConverter TypeConverter
        {
            get
            {
                return typeConverter;
            }
            set
            {
                typeConverter = value;
                RefreshOptions();
            }
        }

        public override bool IsDefault
        {
            get
            {
                return isDefault;
            }
            set
            {
                if (value != isDefault)
                {
                    isDefault = value;

                    if (IsDefaultSet != null)
                    {
                        IsDefaultSet(this, null);
                    }
                }
            }
        }

        public string UnmodifiedVariableName
        {
            get;
            set;
        }

        public event EventHandler IsDefaultSet;

        public void RefreshOptions()
        {
            StandardValuesCollection newCustomOptions = null;


            if (this.typeConverter != null)
            {
                if (string.IsNullOrEmpty(UnmodifiedVariableName))
                {
                    throw new InvalidOperationException("UnmodifiedVariableName must be set first.");
                }
                var descriptor = new TypeDescriptorContext(UnmodifiedVariableName);
                newCustomOptions = typeConverter.GetStandardValues(descriptor);
            }

            if(newCustomOptions == null)
            {
                // January 13, 2023
                // Why do we do this?
                // If we are refreshing
                // and there's a CustomOptions
                // already set, we don't want to
                // clear this. If we clear this then
                // we just lose the options unless there's
                // a type converter.
                //CustomOptions?.Clear();
                // I don't know if this is going to cause problems
                // but I'm commenting it out now.
            }
            else
            {
                var differs = false;
                if(CustomOptions?.Count != newCustomOptions.Count)
                {
                    differs = true;
                }
                else if(CustomOptions == null)
                {
                    differs = true;
                }
                else 
                {
                    // counts are the same
                    for(int i = 0; i < CustomOptions.Count; i++)
                    {
                        if (CustomOptions[i] != newCustomOptions[i])
                        {
                            differs = true;
                            break;
                        }
                    }
                }
                if(differs)
                {
                    CustomOptions?.Clear();

                    List<object> valuesAsList = new List<object>();

                    if(newCustomOptions.Count != 0 && CustomOptions == null)
                    {
                        CustomOptions = new List<object>();
                    }

                    foreach (var item in newCustomOptions)
                    {
                        CustomOptions.Add(item);
                    }

                }
            }
        }

        public void MakeReadOnly()
        {
            isExplicitlyReadOnly = true;
        }
    }
}
