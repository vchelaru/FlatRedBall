using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace OfficialPlugins.VariableDisplay
{
    public class PropertyDescriptorImplementation : PropertyDescriptor
    {
        string displayName;

        public override string DisplayName
        {
            get
            {
                return displayName;
            }
        }
        public override bool CanResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override Type ComponentType
        {
            get { throw new NotImplementedException(); }
        }

        public override object GetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public override Type PropertyType
        {
            get { throw new NotImplementedException(); }
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            throw new NotImplementedException();
        }

        public override bool ShouldSerializeValue(object component)
        {
            throw new NotImplementedException();
        }

        public PropertyDescriptorImplementation(string displayName) : base(displayName, null)
        {
            this.displayName = displayName;
        }
    }
}
