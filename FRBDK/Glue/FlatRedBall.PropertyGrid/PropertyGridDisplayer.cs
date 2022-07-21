using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
//using FlatRedBall.Glue.FormHelpers;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FlatRedBall.Glue.GuiDisplay
{
    #region PropertyGridMember Class


    public class PropertyGridMember
    {
        public string Name;
        public Type Type;
        public MemberChangeEventHandler MemberChange;
        public Func<object> CustomGetMember;

        bool mIsReadOnly;
        public bool IsReadOnly
        {
            get { return mIsReadOnly; }
            set
            {
                if (value != mIsReadOnly)
                {
                    mIsReadOnly = value;

                    if (mIsReadOnly)
                    {
                        Attributes.Add(new ReadOnlyAttribute(true));
                    }

                    // todo:  Do we want to remove it if it's set to false
                }
            }
        }

        public event MemberChangeEventHandler AfterMemberChange;

        public List<Attribute> Attributes = new List<Attribute>();

        public bool IsExplicitlyIncluded
        {
            get;
            set;
        }

        public TypeConverter TypeConverter
        {
            get;
            set;
        }

        internal TypeConverter GetTypeConverter()
        {
            if (TypeConverter == null)
            {
                return TypeDescriptor.GetConverter(Type);
            }
            else
            {
                return TypeConverter;
            }
        }

        public void SetCategory(string category)
        {
            // If this thing already has a
            // category attribute then let's
            // replace it.  Otherwise add a new
            // instance
            bool found = false;
            for(int i = 0; i < Attributes.Count; i++)
            {
                Attribute attribute = Attributes[i];
                if (attribute is CategoryAttribute)
                {
                    CategoryAttribute categoryAttribute = new CategoryAttribute(category);
                    Attributes[i] = categoryAttribute;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                CategoryAttribute categoryAttribute = new CategoryAttribute(category);
                Attributes.Add(categoryAttribute);
            }
        }

        public void SetAttributes(object[] attributesInObjectArray)
        {
            Attributes.Clear();

            
            for (int i = 0; i < attributesInObjectArray.Length; i++)
            {
                Attributes.Add((Attribute)attributesInObjectArray[i]);
            }
        }

        public override string ToString()
        {
            return Type + " " + Name;
        }

        public void ReactToMemberChange (object sender, MemberChangeArgs args)
        {
            if (MemberChange != null)
            {
                MemberChange(sender, args);
            }
            else
            {
                PropertyGrid.LateBinder.GetInstance(args.Owner.GetType()).SetValue( args.Owner, args.Member, args.Value);
            }
            if(AfterMemberChange != null)
            {
                AfterMemberChange(sender, args);
            }

        }
    }


    #endregion


    public class PropertyGridDisplayer : ICustomTypeDescriptor
    {
        #region DllImport stuff

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
        internal static extern IntPtr GetFocus();

        private Control GetFocusedControl()
        {
            Control focusedControl = null;
            // To get hold of the focused control:
            IntPtr focusedHandle = GetFocus();
            if (focusedHandle != IntPtr.Zero)
                // Note that if the focused Control is not a .Net control, then this will return null.
                focusedControl = Control.FromHandle(focusedHandle);
            return focusedControl;
        }

        #endregion

        #region Fields

        List<PropertyGridMember> mNativePropertyGridMembers = new List<PropertyGridMember>();
        List<PropertyGridMember> mCustomPropertyGridMembers = new List<PropertyGridMember>();

        public HashSet<string> ForcedReadOnlyProperties { get; private set; } = new HashSet<string>();

        List<string> mExcludedMembers = new List<string>();


        protected object mInstance;

        System.Windows.Forms.PropertyGrid mPropertyGrid;

        Timer mTimer;
        bool mRefreshOnTimer = true;

        static EditorAttribute mFileWindowAttribute = new EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor));



        #endregion


        #region Properties

        public bool RefreshOnTimer
        {
            get { return mRefreshOnTimer; }
            set
            {

                mTimer.Enabled = value;

                mRefreshOnTimer = value;
            }
        }

        public static EditorAttribute FileWindowAttribute
        {
            get
            {
                return mFileWindowAttribute;            
            }
        
        }

        /// <summary>
        /// The object that should be displayed in the PropertyGrid.
        /// </summary>
        public virtual object Instance
        {
            get
            {
                return mInstance;
            }
            set
            {
                CreateTimer();

                mInstance = value;
                UpdateProperties();
            }
        }            



        public virtual System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get
            {
                return mPropertyGrid;
            }
            set
            {
                bool changed = value != mPropertyGrid;

                if (mPropertyGrid != null && changed)
                {
                    mPropertyGrid.PropertyValueChanged -= HandlePropertyValueChanged;
                    mPropertyGrid.SelectedObject = null;
                }

                mPropertyGrid = value;

                if (mPropertyGrid != null)
                {
                    mPropertyGrid.SelectedObject = this;

                    mPropertyGrid.VerticalScroll.Value = 0;

                    if (changed)
                    {
                        mPropertyGrid.PropertyValueChanged += HandlePropertyValueChanged;
                    }
                }
            }
        }


        protected List<PropertyGridMember> NativePropertyGridMembers => mNativePropertyGridMembers;
        protected List<PropertyGridMember> CustomPropertyGridMembers => mCustomPropertyGridMembers;



        #endregion


        public PropertyGridDisplayer()
        {
            CreateTimer();

            // Set it to refresh after creating it
            RefreshOnTimer = true;

        }

        private void CreateTimer()
        {
            if (mTimer != null)
            {
                mTimer.Dispose();
            }

            mTimer = new Timer();
            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = 1000;
            mTimer.Start();
        }

        void OnTimerTick(object sender, EventArgs e)
        {
            if (RefreshOnTimer && mPropertyGrid != null && !mPropertyGrid.Focused && !mPropertyGrid.ContainsFocus)
            {
                bool shouldRefresh = true;

                Control control = GetFocusedControl();
                if (control != null)
                {
                    if (control.GetType().Name == "GridViewListBox")
                    {
                        shouldRefresh = false;
                    }
                }

                if (shouldRefresh)
                {
                    mPropertyGrid.Refresh();
                }
            }
        }

        public void ExcludeAllMembers()
        {

            if (mInstance == null)
            {
                throw new NullReferenceException("The instance must be set first!");
            }

            PropertyInfo[] properties = mInstance.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (!mExcludedMembers.Contains(property.Name))
                {
                    mExcludedMembers.Add(property.Name);
                }
            }

            FieldInfo[] fields = mInstance.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (!mExcludedMembers.Contains(field.Name))
                {
                    mExcludedMembers.Add(field.Name);
                }
            }

            mCustomPropertyGridMembers.Clear();

            UpdateProperties();
        }

        public void ExcludeMember(string memberToExclude)
        {
            if (mCustomPropertyGridMembers.ContainsMember(memberToExclude))
            {
                mCustomPropertyGridMembers.RemoveMember(memberToExclude);
            }
            else if (!mExcludedMembers.Contains(memberToExclude))
            {
                mExcludedMembers.Add(memberToExclude);
            }

            UpdateProperties();
        }


        public void IncludeMember(string memberToInclude, Type containingType = null,  TypeConverter typeConverter = null, object[] attributes = null)
        {
            if (mExcludedMembers.Contains(memberToInclude))
            {
                mExcludedMembers.Remove(memberToInclude);
            }

            if (typeConverter != null || attributes != null)
            {
                if (containingType == null)
                {
                    throw new Exception("The containingType must not be null when passing a type converter");
                }
                else
                {
                    Type memberType = null;
                    object[] attributesFromType = null;


                    FieldInfo fieldInfo = containingType.GetField(memberToInclude);
                    if (fieldInfo != null)
                    {
                        memberType = fieldInfo.FieldType;
                        attributesFromType = fieldInfo.GetCustomAttributes(true);

                    }
                    else
                    {
                        PropertyInfo propertyInfo = containingType.GetProperty(memberToInclude);

                        if (propertyInfo != null)
                        {
                            memberType = propertyInfo.PropertyType;
                            attributesFromType = propertyInfo.GetCustomAttributes(true);
                        }
                    }

                    int count = attributesFromType.Length;
                    if (attributes != null)
                    {
                        count += attributes.Length;
                    }

                    List<Attribute> allList = new List<Attribute>(count);
                    foreach (var item in attributesFromType)
                    {
                        allList.Add(item as Attribute);
                    }
                    if (attributes != null)
                    {
                        foreach (var item in attributes)
                        {
                            allList.Add(item as Attribute);
                        }
                    }

                    if (memberType == null)
                    {
                        throw new Exception("Could not find member " + memberToInclude + " in type " + containingType);
                    }
                    else
                    {
                        PropertyGridMember pgm = IncludeMember(memberToInclude, memberType, null, null, typeConverter, null);
                        pgm.SetAttributes(allList.ToArray());
                    }
                }
            }

            UpdateProperties();
        }

        public void SetCategory(string memberName, string category)
        {
            foreach (var property in this.mNativePropertyGridMembers.Where(property=>property.Name == memberName))
            {
                property.SetCategory(category);
            }
            foreach (var property in this.mCustomPropertyGridMembers.Where(property=>property.Name == memberName))
            {
                property.SetCategory(category);
            }
        }

        public void SetAllPropertyCategory(string category)
        {
            foreach (var property in this.mNativePropertyGridMembers)
            {
                property.SetCategory(category);
            }
            foreach (var property in this.mCustomPropertyGridMembers)
            {
                property.SetCategory(category);
            }
        }

        public PropertyGridMember IncludeMember(string memberToInclude, Type type, 
            MemberChangeEventHandler memberChangeAction, 
            Func<object> getMember, TypeConverter converter = null, Attribute[] attributes = null)
        {
            if (mNativePropertyGridMembers.ContainsMember(memberToInclude))
            {
                mNativePropertyGridMembers.RemoveMember(memberToInclude);
            }

            PropertyGridMember pgm = new PropertyGridMember();
            pgm.MemberChange = memberChangeAction;
            pgm.CustomGetMember = getMember;
            pgm.Name = memberToInclude;
            pgm.Type = type;
            pgm.TypeConverter = converter;
            pgm.Attributes.Clear();

            if (attributes != null)
            {
                pgm.Attributes.AddRange(attributes);
            }

            if (getMember != null && memberChangeAction == null)
            {
                pgm.IsReadOnly = true;
                pgm.Attributes.Add(new ReadOnlyAttribute(true));
            }

            mCustomPropertyGridMembers.Add(pgm);

            return pgm;
        }

        public PropertyGridMember GetPropertyGridMember(string name)
        {
            foreach (PropertyGridMember pgm in mNativePropertyGridMembers)
            {
                if (pgm.Name == name)
                {
                    return pgm;
                }
            }

            foreach (PropertyGridMember pgm in mCustomPropertyGridMembers)
            {
                if (pgm.Name == name)
                {
                    return pgm;
                }
            }
            return null;
        }

        public void ResetToDefault()
        {
            mCustomPropertyGridMembers.Clear();
            mExcludedMembers.Clear();
        }

        public Attribute[] CategoryAttribute(string category)
        {
            return new Attribute[] { new CategoryAttribute(category) };
        }

        public Attribute[] ReadOnlyAttribute()
        {
            return new Attribute[] { new ReadOnlyAttribute(true) };
        }

        private void UpdateProperties()
        {
            if (mInstance != null)
            {
                // This only removes
                // native members that
                // haven't been explicitly
                // added.  Explicitly-added
                // members may have TypeConverters
                // so we want to preserve those.
                ClearNonExplicitNativeMembers();

                PropertyInfo[] propertyInfos = mInstance.GetType().GetProperties(
                    
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty // I don't think we want to show static members in PropertyGrids
                    );

                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    if (!mExcludedMembers.Contains(propertyInfo.Name) && !mCustomPropertyGridMembers.ContainsMember(propertyInfo.Name))
                    {
                        PropertyGridMember pgm = new PropertyGridMember();
                        pgm.Name = propertyInfo.Name;
                        pgm.Type = propertyInfo.PropertyType;

                        pgm.SetAttributes(propertyInfo.GetCustomAttributes(true));
                        pgm.IsReadOnly = propertyInfo.CanWrite == false || ForcedReadOnlyProperties.Contains(propertyInfo.Name);
                        // Does this thing have a type converter set on the property?
                        TypeConverterAttribute attrib =
                            (TypeConverterAttribute)Attribute.GetCustomAttribute(propertyInfo,
                            typeof(TypeConverterAttribute));
                        if (attrib != null)
                        {
                            TypeConverter converter =
                                (TypeConverter)Activator.CreateInstance(Type.GetType(attrib.ConverterTypeName),
                                false);
                            pgm.TypeConverter = converter;
                        }

                        mNativePropertyGridMembers.Add(pgm);
                    }
                }

                FieldInfo[] fieldInfos = mInstance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField);
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (!mExcludedMembers.Contains(fieldInfo.Name) && !mCustomPropertyGridMembers.ContainsMember(fieldInfo.Name))
                    {
                        PropertyGridMember pgm = new PropertyGridMember()
                        {
                            Name = fieldInfo.Name,
                            Type = fieldInfo.FieldType

                        };

                        mNativePropertyGridMembers.Add(pgm);
                    }
                }
            }

        }

        private void ClearNonExplicitNativeMembers()
        {
            for (int i = mNativePropertyGridMembers.Count - 1; i > -1; i--)
            {
                if (mNativePropertyGridMembers[i].IsExplicitlyIncluded == false)
                {
                    mNativePropertyGridMembers.RemoveAt(i);
                }

            }
            mNativePropertyGridMembers.Clear();
        }

        protected virtual void HandlePropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            
        }

        public void ScrollToTop()
        {
            if (PropertyGrid != null && PropertyGrid.SelectedGridItem != null)
            {
                GridItem parent = PropertyGrid.SelectedGridItem.Parent;
                if (parent != null)
                {
                    if (parent.Parent != null)
                    {
                        parent = parent.Parent;
                    }
                    PropertyGrid.SelectedGridItem = parent.GridItems[0];
                }
            }
        }

        #region ICustomTypeDescriptor Members



        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorHelper.CurrentInstance = Instance;
            PropertyDescriptorCollection pdc = new PropertyDescriptorCollection(null);

            //    TypeDescriptor.GetProperties(this, true);

            

            pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "Instance");
            pdc = PropertyDescriptorHelper.RemoveProperty(pdc, "PropertyGrid");

            List<PropertyGridMember> list = mNativePropertyGridMembers;

            pdc = PopulateFromPgmList(pdc, list);

            pdc = PopulateFromPgmList(pdc, mCustomPropertyGridMembers);


            return pdc;
        }

        private PropertyDescriptorCollection PopulateFromPgmList(PropertyDescriptorCollection pdc, List<PropertyGridMember> list)
        {
            foreach (PropertyGridMember pgm in list.ToArray())
            {

                TypeConverter converter = pgm.GetTypeConverter();

                //                                    new Attribute[] { new CategoryAttribute("Custom Variable") });
                pdc = PropertyDescriptorHelper.AddProperty(pdc,
                    pgm.Name,
                    pgm.Type,
                    converter,
                    pgm.Attributes.ToArray(),
                    pgm.ReactToMemberChange,
                    pgm.CustomGetMember
                    );
            }
            return pdc;
        }


        #region Default methods and properties




        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }



        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
        #endregion
        #endregion
    }

    internal static class PropertyGridDisplayerExtensionMethods
    {

        public static bool ContainsMember(this List<PropertyGridMember> list, string member)
        {
            return list.GetMember(member) != null;
        }

        public static PropertyGridMember GetMember(this List<PropertyGridMember> list, string member)
        {
            foreach (var pgm in list)
            {
                if (pgm.Name == member)
                {
                    return pgm;
                }
            }

            return null;
        }

        public static void RemoveMember(this List<PropertyGridMember> list, string member)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == member)
                {
                    list.RemoveAt(i);
                }
            }
        }
    }
}
