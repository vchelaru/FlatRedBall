using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using System.ComponentModel;
using FlatRedBall.Glue.Controls;
using System.Drawing.Design;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    #region ProjectSpecificFileDisplayer class
    public class ProjectSpecificFileDisplayer : ICustomTypeDescriptor
    {
        public List<ProjectSpecificFile> Instance
        {
            get;
            set;
        }


        public PropertyDescriptorCollection GetProperties()
        {
            var pds = new PropertyDescriptorCollection(null);

            for (var i = 0; i < Instance.Count; i++)
            {
                PropertyDescriptor pd = new ProjectSpecificFileCollectionPropertyDescriptor(Instance, i);

                pds.Add(pd);
            }

            return pds;
        }

        public override string ToString()
        {
            if (Instance.Count == 0)
            {
                return "No project-specific files";
            }
            else if (Instance.Count == 1)
            {
                return "1 Project Specific File";
            }
            else
            {
                return Instance.Count + " Project Specific Files";
            }
        }

        #region The other Unimportant ICustomDescriptor stuff

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

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }



        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
        #endregion
    }

    #endregion

    #region ProjectSpecificFileCollectionPropertyDescriptor
    public class ProjectSpecificFileCollectionPropertyDescriptor : PropertyDescriptor
    {
        private List<ProjectSpecificFile> collection = null;
        private int index = -1;

        public ProjectSpecificFileCollectionPropertyDescriptor(List<ProjectSpecificFile> coll, int idx)
            : base("#" + idx.ToString(), null)
        {
            this.collection = coll;
            this.index = idx;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(null);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return this.collection.GetType();
            }
        }

        public override string DisplayName
        {
            get
            {
                ProjectSpecificFile projectSpecificFile = this.collection[index];
                return projectSpecificFile.ProjectName;
            }
        }

        public override string Description
        {
            get
            {
                ProjectSpecificFile projectSpecificFile = this.collection[index];
                StringBuilder sb = new StringBuilder();
                sb.Append(projectSpecificFile.File.FullPath);

                return sb.ToString();
            }
        }

        public override object GetValue(object component)
        {
            return collection[index];
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override string Name
        {
            get { return "#" + index.ToString(); }
        }

        public override Type PropertyType
        {
            get { return collection[index].GetType(); }
        }

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override void SetValue(object component, object value)
        {
            // this.collection[index] = value;

        }
    }
    #endregion


    #region ProjectSpecificFileCollectionConverter
    internal class ProjectSpecificFileCollectionConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is ProjectSpecificFileCollection)
            {
                var collection = (ProjectSpecificFileCollection)value;
                return collection.Count.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    #endregion

    /// <summary>
    /// Shows a selected file in the WinForms Properties grid. Which properties exist, which can change and the values
    /// offered come from <see cref="ReferencedFileSavePropertyModel"/>; this renders them and shows the project-specific
    /// files as a list with its own editor.
    /// </summary>
    public class ReferencedFileSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        const string ProjectSpecificFilesDisplayName = "Project Specific Files";

        readonly ReferencedFileSavePropertyModel model;
        ProjectSpecificFileDisplayer mProjectSpecificFileDisplayer = new ProjectSpecificFileDisplayer();

        public ReferencedFileSavePropertyGridDisplayer(ReferencedFileSavePropertyModel model)
        {
            this.model = model;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                var rfs = (ReferencedFileSave)value;
                mProjectSpecificFileDisplayer.Instance = rfs.ProjectSpecificFiles;

                EditorPropertyGridMapping.Apply(this, rfs, model.GetProperties(rfs));
                AddProjectSpecificFileMember();

                base.Instance = value;
            }
        }

        ProjectSpecificFileDisplayer GetProjectSpecificFileMember()
        {
            return mProjectSpecificFileDisplayer;
        }

        void SetProjectSpecificFiles(object sender, MemberChangeArgs args)
        {
            // do nothing
        }

        private void AddProjectSpecificFileMember()
        {
            ExcludeMember(nameof(ReferencedFileSave.ProjectSpecificFiles));

            Attribute[] attributes = new Attribute[]
            {
                new EditorAttribute(typeof(ProjectSpecificFileCollectionEditor), typeof(UITypeEditor))

            };

            IncludeMember(ProjectSpecificFilesDisplayName, typeof(ProjectSpecificFileDisplayer),
                SetProjectSpecificFiles, GetProjectSpecificFileMember, new ProjectSpecificFileCollectionConverter(), attributes);
        }
    }
}
