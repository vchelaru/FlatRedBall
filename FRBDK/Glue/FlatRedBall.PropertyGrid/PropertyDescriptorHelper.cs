using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FlatRedBall.Glue.GuiDisplay
{

    public class MemberChangeArgs : EventArgs
    {
        public object Owner;
        public string Member;
        public object Value;
    }

    public delegate void MemberChangeEventHandler(object sender, MemberChangeArgs args);

    public static class PropertyDescriptorHelper
    {

        public static object CurrentInstance
        {
            get;
            set;
        }

        

        static PropertyDescriptor GetPropertyDescriptor(PropertyDescriptorCollection pdc, string name)
        {
            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name == name)
                {
                    return pd;
                }
            }
            return null;
        }


        public static PropertyDescriptorCollection RemoveProperty(PropertyDescriptorCollection pdc, string propertyName)
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>();

            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name != propertyName)
                {
                    properties.Add(pd);
                }
            }
            return new PropertyDescriptorCollection(properties.ToArray());
        }

        public static PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType)
        {
            return AddProperty(pdc, propertyName, propertyType, null, new Attribute[0] );
		}

		public static PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType, TypeConverter converter, 
            Attribute[] attributes)
		{
            return AddProperty(pdc, propertyName, propertyType, converter, attributes, null, null);
        }

        
		public static PropertyDescriptorCollection AddProperty(PropertyDescriptorCollection pdc, string propertyName, Type propertyType, TypeConverter converter,
            Attribute[] attributes, MemberChangeEventHandler eventArgs, Func<object> getMember)
        {
            List<PropertyDescriptor> properties = new List<PropertyDescriptor>(pdc.Count);

            for (int i = 0; i < pdc.Count; i++)
            {
                PropertyDescriptor pd = pdc[i];

                if (pd.Name != propertyName)
                {
                    properties.Add(pd);
                }
            }

            // If it doesn't have
            // events for getting and
            // setting the value, then
            // the variable is part of the
            // type itself.  Objects can't have
            // fields/properties with spaces in them
            // so this is an invalid property.  The name
            // is either wrong or the user should have passed
            // in get and set methods.
            if (propertyName.Contains(' ') && (getMember == null || eventArgs == null))
            {
                throw new ArgumentException("The member cannot have spaces in the name if it doesn't have getters and setters explicitly set");
            }

            ReflectingParameterDescriptor epd = new ReflectingParameterDescriptor(propertyName, attributes);
            epd.SetComponentType(propertyType);
            epd.Instance = CurrentInstance;
            epd.MemberChangeEvent = eventArgs as MemberChangeEventHandler;
            epd.CustomGetMember = getMember;
            epd.TypeConverter = converter;
                


            properties.Add(epd);            

            //PropertyDescriptor propertyDescriptor;

            return new PropertyDescriptorCollection(properties.ToArray());
             
        }

        public static PropertyDescriptorCollection SetPropertyDisplay(PropertyDescriptorCollection pdc, string oldName, string newName)
        {
            PropertyDescriptor pd = GetPropertyDescriptor(pdc, oldName);

            pdc = RemoveProperty(pdc, oldName);


            Attribute[] attributeArray = new Attribute[pd.Attributes.Count];

            for(int i = 0; i < attributeArray.Length; i++)
            {
                attributeArray[i] = pd.Attributes[i];

            }

            pdc = AddProperty(pdc, newName, pd.PropertyType, null, attributeArray);

            return pdc;
        }
    }
}
