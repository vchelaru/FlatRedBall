using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.VariableDisplay
{
    class DataGridItem : InstanceMember
    {
        bool isDefault;

        TypeConverter typeConverter;

        public event Action CustomRefreshOptions;

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
            CustomOptions.Clear();

            if (this.typeConverter != null)
            {
                if (string.IsNullOrEmpty(UnmodifiedVariableName))
                {
                    throw new InvalidOperationException("UnmodifiedVariableName must be set first.");
                }
                var descriptor = new TypeDescriptorContext(UnmodifiedVariableName);
                var values = typeConverter.GetStandardValues(descriptor);

                List<object> valuesAsList = new List<object>();
                foreach (var item in values)
                {
                    CustomOptions.Add(item);
                }
            }
        }
    }
}
