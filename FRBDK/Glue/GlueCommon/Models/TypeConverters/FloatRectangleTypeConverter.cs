using FlatRedBall.Math.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GlueSaveClasses
{
    public class FloatRectangleTypeConverter : ExpandableObjectConverter
    {
		/// <summary>Determines if this converter can convert an object in the given source type to the native type of the converter.</summary>
		/// <returns>This method returns true if this object can perform the conversion; otherwise, false.</returns>
		/// <param name="context">A formatter context. This object can be used to get additional information about the environment this converter is being called from. This may be null, so you should always check. Also, properties on the context object may also return null. </param>
		/// <param name="sourceType">The type you want to convert from. </param>
		/// <filterpriority>1</filterpriority>
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}
		/// <summary>Gets a value indicating whether this converter can convert an object to the given destination type using the context.</summary>
		/// <returns>This method returns true if this converter can perform the conversion; otherwise, false.</returns>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> object that provides a format context. This can be null, so you should always check. Also, properties on the context object can also return null.</param>
		/// <param name="destinationType">A <see cref="T:System.Type" /> object that represents the type you want to convert to. </param>
		/// <filterpriority>1</filterpriority>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
		}

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (text == null)
			{
				return base.ConvertFrom(context, culture, value);
			}
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				return null;
			}
			if (culture == null)
			{
				culture = CultureInfo.CurrentCulture;
			}
			char c = culture.TextInfo.ListSeparator[0];
			string[] array = text2.Split(new char[]
			{
				c
			});
			float[] array2 = new float[array.Length];
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(float));
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (float)converter.ConvertFromString(context, culture, array[i]);
			}
			if (array2.Length == 4)
			{
				return new FloatRectangle(array2[0], array2[1], array2[2], array2[3]);
			}
			throw new ArgumentException("TextParseFailedFormat");
		}
		/// <summary>Converts the specified object to the specified type.</summary>
		/// <returns>The converted object.</returns>
		/// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that can be used to get additional information about the environment this converter is being called from. This may be null, so you should always check. Also, properties on the context object may also return null. </param>
		/// <param name="culture">An <see cref="T:System.Globalization.CultureInfo" /> that contains culture specific information, such as the language, calendar, and cultural conventions associated with a specific culture. It is based on the RFC 1766 standard. </param>
		/// <param name="value">The object to convert. </param>
		/// <param name="destinationType">The type to convert the object to. </param>
		/// <exception cref="T:System.NotSupportedException">The conversion cannot be completed.</exception>
		/// <filterpriority>1</filterpriority>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == null)
			{
				throw new ArgumentNullException("destinationType");
			}
			if (value is FloatRectangle)
			{
				if (destinationType == typeof(string))
				{
					FloatRectangle rectangle = (FloatRectangle)value;
					if (culture == null)
					{
						culture = CultureInfo.CurrentCulture;
					}
					string separator = culture.TextInfo.ListSeparator + " ";
					TypeConverter converter = TypeDescriptor.GetConverter(typeof(float));
					string[] array = new string[4];
					int num = 0;
					array[num++] = converter.ConvertToString(context, culture, rectangle.X);
					array[num++] = converter.ConvertToString(context, culture, rectangle.Y);
					array[num++] = converter.ConvertToString(context, culture, rectangle.Width);
					array[num++] = converter.ConvertToString(context, culture, rectangle.Height);
					return string.Join(separator, array);
				}
				if (destinationType == typeof(InstanceDescriptor))
				{
					FloatRectangle rectangle2 = (FloatRectangle)value;
					ConstructorInfo constructor = typeof(FloatRectangle).GetConstructor(new Type[]
					{
						typeof(float),
						typeof(float),
						typeof(float),
						typeof(float)
					});
					if (constructor != null)
					{
						return new InstanceDescriptor(constructor, new object[]
						{
							rectangle2.X,
							rectangle2.Y,
							rectangle2.Width,
							rectangle2.Height
						});
					}
				}
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
		/// <summary>Creates an instance of this type given a set of property values for the object. This is useful for objects that are immutable but still want to provide changeable properties.</summary>
		/// <returns>The newly created object, or null if the object could not be created. The default implementation returns null.</returns>
		/// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> through which additional context can be provided. </param>
		/// <param name="propertyValues">A dictionary of new property values. The dictionary contains a series of name-value pairs, one for each property returned from a call to the <see cref="M:System.Drawing.RectangleConverter.GetProperties(System.ComponentModel.ITypeDescriptorContext,System.Object,System.Attribute[])" /> method. </param>
		/// <filterpriority>1</filterpriority>
		public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
		{
			if (propertyValues == null)
			{
				throw new ArgumentNullException("propertyValues");
			}
			object obj = propertyValues["X"];
			object obj2 = propertyValues["Y"];
			object obj3 = propertyValues["Width"];
			object obj4 = propertyValues["Height"];
			if (obj == null || obj2 == null || obj3 == null || obj4 == null || !(obj is float) || !(obj2 is float) || !(obj3 is float) || !(obj4 is float))
			{
				throw new ArgumentException("PropertyValueInvalidEntry");
			}
			return new FloatRectangle((float)obj, (float)obj2, (float)obj3, (float)obj4);
		}
		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
		{
			return true;
		}
		/// <summary>Retrieves the set of properties for this type. By default, a type does not return any properties. </summary>
		/// <returns>The set of properties that should be exposed for this data type. If no properties should be exposed, this may return null. The default implementation always returns null.</returns>
		/// <param name="context">A <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> through which additional context can be provided. </param>
		/// <param name="value">The value of the object to get the properties for. </param>
		/// <param name="attributes">An array of <see cref="T:System.Attribute" /> objects that describe the properties. </param>
		/// <filterpriority>1</filterpriority>
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
            Type typeFromHandle = typeof(FloatRectangle);
            PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(new PropertyDescriptor[]
			{
				new FieldPropertyDescriptor(typeFromHandle.GetField("X")),
				new FieldPropertyDescriptor(typeFromHandle.GetField("Y")),
				new FieldPropertyDescriptor(typeFromHandle.GetField("Width")),
				new FieldPropertyDescriptor(typeFromHandle.GetField("Height"))
			});

            propertyDescriptorCollection = propertyDescriptorCollection.Sort(new string[]
			{
				"X",
				"Y",
				"Width",
				"Height",

			});

            return propertyDescriptorCollection;


		}
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}


		/// <summary>Initializes a new instance of the <see cref="T:System.Drawing.RectangleConverter" /> class.</summary>
		
	}




    internal abstract class MemberPropertyDescriptor : PropertyDescriptor
    {
        private MemberInfo _member;
        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public override Type ComponentType
        {
            get
            {
                return this._member.DeclaringType;
            }
        }
        public MemberPropertyDescriptor(MemberInfo member)
            : base(member.Name, (Attribute[])member.GetCustomAttributes(typeof(Attribute), true))
        {
            this._member = member;
        }
        public override bool Equals(object obj)
        {
            MemberPropertyDescriptor memberPropertyDescriptor = obj as MemberPropertyDescriptor;
            return memberPropertyDescriptor != null && memberPropertyDescriptor._member.Equals(this._member);
        }
        public override int GetHashCode()
        {
            return this._member.GetHashCode();
        }
        public override void ResetValue(object component)
        {
        }
        public override bool CanResetValue(object component)
        {
            return false;
        }
        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }
    }



    internal class FieldPropertyDescriptor : MemberPropertyDescriptor
    {
        private FieldInfo _field;
        public override Type PropertyType
        {
            get
            {
                return this._field.FieldType;
            }
        }
        public FieldPropertyDescriptor(FieldInfo field)
            : base(field)
        {
            this._field = field;
        }
        public override object GetValue(object component)
        {
            return this._field.GetValue(component);
        }
        public override void SetValue(object component, object value)
        {
            this._field.SetValue(component, value);
            this.OnValueChanged(component, EventArgs.Empty);
        }
    }










}

