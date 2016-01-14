using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall;

namespace EffectEditor.HelperClasses
{
    public class EffectProperty
    {
        #region Fields

        private string mName;
        private object mValue;
        private bool mReadOnly;
        private bool mVisible;

        #endregion

        #region Properties

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public object Value
        {
            get { return mValue; }
            set { mValue = value; }
        }

        public bool ReadOnly
        {
            get { return mReadOnly; }
            set { mReadOnly = value; }
        }

        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }

        #endregion

        #region Methods

        public EffectProperty(string name, object value, bool readOnly, bool visible)
        {
            mName = name;
            mValue = value;
            mReadOnly = readOnly;
            mVisible = visible;
        }

        #endregion
    }

    public class EffectPropertyDescriptor : PropertyDescriptor
    {
        #region Fields

        private EffectParameter mParameter;
        private string mTagString;

        #endregion

        #region Properties

        public EffectParameter Parameter
        {
            get { return mParameter; }
        }

        public override string Category
        {
            get
            {
                if (mParameter.ParameterClass == EffectParameterClass.MatrixColumns ||
                    mParameter.ParameterClass == EffectParameterClass.MatrixRows)
                {
                    return "Matrices";
                }
                else if (mParameter.ParameterClass == EffectParameterClass.Object &&
                    (mParameter.ParameterType == EffectParameterType.Texture ||
                     mParameter.ParameterType == EffectParameterType.Texture1D ||
                     mParameter.ParameterType == EffectParameterType.Texture2D ||
                     mParameter.ParameterType == EffectParameterType.Texture3D ||
                     mParameter.ParameterType == EffectParameterType.TextureCube))
                {
                    return "Textures";
                }
                else return String.Empty;
            }
        }
        
        public override string DisplayName
        {
            get
            {
                return mParameter.Name;
            }
        }

        public override string Name
        {
            get
            {
                return mParameter.Name;
            }
        }

        public override bool IsBrowsable
        {
            get
            {
                // Look for annotation
                if (mParameter.Annotations.Count > 0)
                {
                    foreach (EffectAnnotation annotation in mParameter.Annotations)
                    {
                        if (annotation.Name == "SasUiVisible")
                            return annotation.GetValueBoolean();
                    }
                }

                switch (mParameter.ParameterType)
                {
                    case EffectParameterType.Bool:
                    case EffectParameterType.Int32:
                    case EffectParameterType.Single:
                    case EffectParameterType.String:
                        return true;
                        break;
                    case EffectParameterType.Texture:
                        return true;
                        break;
                    case EffectParameterType.Texture1D:
                    case EffectParameterType.Texture2D:
                    case EffectParameterType.Texture3D:
                    case EffectParameterType.TextureCube:
                    default:
                        return false;
                        break;
                }
            }
        }

        public override string Description
        {
            get
            {
                // Try to find the annotation
                if (mParameter.Annotations.Count > 0)
                {
                    foreach (EffectAnnotation annotation in mParameter.Annotations)
                    {
                        if (annotation.Name == "SasUiDescription")
                            return annotation.GetValueString();
                    }
                }

                // Provide better description based on property
                // mParameter.Annotations
                return "Edit the " + Name + " Parameter";
            }
        }

        #endregion

        #region Constructor

        public EffectPropertyDescriptor(EffectParameter parameter, Attribute[] attributes)
            : base(parameter.Name, attributes)
        {
            mParameter = parameter;
        }

        Type GetParameterType()
        {
            switch (mParameter.ParameterType)
            {
                case EffectParameterType.Bool:
                    return typeof(bool);
                    break;
                case EffectParameterType.Int32:
                    return typeof(Int32);
                    break;
                case EffectParameterType.Single:
                    switch (mParameter.ParameterClass)
                    {
                        case EffectParameterClass.MatrixColumns:
                            return typeof(Matrix);
                            break;
                        case EffectParameterClass.MatrixRows:
                            return typeof(Matrix);
                            break;
                        case EffectParameterClass.Scalar:
                            return typeof(float);
                            break;
                        case EffectParameterClass.Vector:
                            switch (mParameter.ColumnCount)
                            {
                                case 1:
                                    return typeof(float);
                                    break;
                                case 2:
                                    return typeof(Vector2);
                                    break;
                                case 3:
                                    return typeof(Vector3);
                                    break;
                                case 4:
                                    return typeof(Vector4);
                                    break;
                                default:
                                    return null;
                                    break;
                            }
                            break;
                        default:
                            return typeof(float);
                            break;
                    }
                    break;
                case EffectParameterType.String:
                    return typeof(string);
                    break;
                case EffectParameterType.Texture:
                    return typeof(string);
                    break;
                default:
                    return null;
                    break;
            }
        }

        #endregion

        #region Methods

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get {
                switch (mParameter.ParameterType)
                {
                    case EffectParameterType.Bool:
                        return typeof(bool);
                        break;
                    case EffectParameterType.Int32:
                        return typeof(Int32);
                        break;
                    case EffectParameterType.Single:
                        return typeof(float);
                        break;
                    case EffectParameterType.String:
                        return typeof(string);
                        break;
                    case EffectParameterType.Texture:
                        return typeof(string);
                        break;
                    default:
                        return null;
                        break;
                }
            }
        }

        public override object GetValue(object component)
        {
            switch (mParameter.ParameterType)
            {
                case EffectParameterType.Bool:
                    return mParameter.GetValueBoolean();
                    break;
                case EffectParameterType.Int32:
                    return mParameter.GetValueInt32();
                    break;
                case EffectParameterType.Single:
                    switch (mParameter.ParameterClass)
                    {
                        case EffectParameterClass.MatrixColumns:
                            return mParameter.GetValueMatrix();
                            break;
                        case EffectParameterClass.MatrixRows:
                            return mParameter.GetValueMatrix();
                            break;
                        case EffectParameterClass.Scalar:
                            return mParameter.GetValueSingle();
                            break;
                        case EffectParameterClass.Vector:
                            switch (mParameter.ColumnCount)
                            {
                                case 1:
                                    return mParameter.GetValueSingle();
                                    break;
                                case 2:
                                    return mParameter.GetValueVector2();
                                    break;
                                case 3:
                                    return mParameter.GetValueVector3();
                                    break;
                                case 4:
                                    return mParameter.GetValueVector4();
                                    break;
                                default:
                                    return null;
                                    break;
                            }
                            break;
                        default:
                            return mParameter.GetValueSingle();
                            break;
                    }
                    break;
                case EffectParameterType.String:
                    return mParameter.GetValueString();
                    break;
                case EffectParameterType.Texture:
                    return (mTagString != null) ? mTagString : String.Empty;
                    break;
                default:
                    return null;
                    break;
            }

            return null;
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return GetParameterType(); }
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            switch (mParameter.ParameterType)
            {
                case EffectParameterType.Bool:
                    mParameter.SetValue((bool)value);
                    break;
                case EffectParameterType.Int32:
                    mParameter.SetValue((Int32)value);
                    break;
                case EffectParameterType.Single:
                    switch (mParameter.ParameterClass)
                    {
                        case EffectParameterClass.MatrixColumns:
                            mParameter.SetValue((Matrix)value);
                            break;
                        case EffectParameterClass.MatrixRows:
                            mParameter.SetValue((Matrix)value);
                            break;
                        case EffectParameterClass.Scalar:
                            mParameter.SetValue((float)value);
                            break;
                        case EffectParameterClass.Vector:
                            switch (mParameter.ColumnCount)
                            {
                                case 1:
                                    mParameter.SetValue((float)value);
                                    break;
                                case 2:
                                    mParameter.SetValue((Vector2)value);
                                    break;
                                case 3:
                                    mParameter.SetValue((Vector3)value);
                                    break;
                                case 4:
                                    mParameter.SetValue((Vector4)value);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            mParameter.SetValue((float)value);
                            break;
                    }
                    break;
                case EffectParameterType.String:
                    mParameter.SetValue((string)value);
                    break;
                case EffectParameterType.Texture:
                    string fileName = (string)value;
                    if (System.IO.File.Exists(fileName))
                    {
                        Texture2D texture = FlatRedBallServices.Load<Texture2D>(fileName);
                        mParameter.SetValue(texture);
                        mTagString = fileName;
                    }
                    break;
                default:
                    break;
            }
        }

        public override object GetEditor(Type editorBaseType)
        {
            return base.GetEditor(editorBaseType);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        #endregion
    }

    public class EffectPropertyEditor : ICustomTypeDescriptor
    {
        Effect mEffect;

        public EffectPropertyEditor(Effect effect)
        {
            mEffect = effect;
        }

        #region ICustomTypeDescriptor Members

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }
        
        
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            List<EffectPropertyDescriptor> newProps = new List<EffectPropertyDescriptor>();
            for (int i = 0; i < mEffect.Parameters.Count; i++)
            {
                List<Attribute> attrs = new List<Attribute>();

                if (mEffect.Parameters[i].ParameterType == EffectParameterType.Texture)
                {
                    attrs.Add(new EditorAttribute(
                        typeof(System.Windows.Forms.Design.FileNameEditor),
                        typeof(System.Drawing.Design.UITypeEditor)));
                }

                attrs.AddRange(attributes);

                EffectPropertyDescriptor newPropDesc = new EffectPropertyDescriptor(
                    mEffect.Parameters[i], attrs.ToArray());
                if (newPropDesc.IsBrowsable) newProps.Add(newPropDesc);
            }

            return new PropertyDescriptorCollection(newProps.ToArray());
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion
    }
}
