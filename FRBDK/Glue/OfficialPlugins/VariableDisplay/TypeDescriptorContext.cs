using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace OfficialPlugins.VariableDisplay
{
    public class TypeDescriptorContext : ITypeDescriptorContext
    {


        public IContainer Container
        {
            get { throw new NotImplementedException(); }
        }

        public object Instance
        {
            get { return null; }
        }

        public void OnComponentChanged()
        {
            throw new NotImplementedException();
        }

        public bool OnComponentChanging()
        {
            throw new NotImplementedException();
        }

        PropertyDescriptor propertyDescriptor;
        public PropertyDescriptor PropertyDescriptor
        {
            get { return propertyDescriptor; }
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public TypeDescriptorContext(string name)
        {
            propertyDescriptor = new PropertyDescriptorImplementation(name);
        }
    }
}