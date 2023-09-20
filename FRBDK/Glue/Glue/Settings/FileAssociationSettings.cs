using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using System.Windows.Forms;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Settings
{
    internal class ExtensionApplicationReturner
    {
        string mExtension;
        Dictionary<string, string> mAssociations;

        public ExtensionApplicationReturner(string extension, Dictionary<string, string> associations)
        {
            mAssociations = associations;
            mExtension = extension;

        }

        public string GetApplication()
        {
            if (mAssociations.ContainsKey(mExtension))
            {
                return mAssociations[mExtension];
            }
            else
            {
                return null;
            }
        }

    }

	public class FileAssociationSettings : ICustomTypeDescriptor, IApplicationSettings
	{
		#region Fields

		Dictionary<string, string> mExtensionApplicationAssociations = new Dictionary<string, string>();

		List<string> mAvailableApplications;

		#endregion

        [Browsable(false)]
        public List<string> AvailableBuildTools
        {
            get
            {
                var toReturn = new List<string>();
                foreach (var buildTool in GlueState.Self.GlueSettingsSave.BuildToolAssociations)
                {
                    toReturn.Add(buildTool.ToString());
                }
                return toReturn;
                // Not sure what this is:
                //return ProjectManager.GlueSettingsSave.BuildTools;
            }
        }

		[Browsable(false)]
		public List<string> AvailableApplications
		{
			get
			{
				return mAvailableApplications;
			}
		}

        [XmlIgnore]
        public static bool StopSavesAndLoads { get; set; }

		public FileAssociationSettings()
		{
			AddDefaults();

			mAvailableApplications = new List<string>();
			mAvailableApplications.Add("<DEFAULT>");
		}

		private void AddDefaults()
		{
			List<string> extensions = new List<string>();
			extensions.Add("achx");
            extensions.Add("bmp");
            extensions.Add("csv");
            extensions.Add("emix");
            extensions.Add("mp3");
            extensions.Add("nntx");
            extensions.Add("ogg");
            extensions.Add("png");
            extensions.Add("scnx");
			extensions.Add("shcx");
			extensions.Add("splx");
            extensions.Add("txt");
            extensions.Add("wav");
            extensions.Add("xml");


            for (int i = 0; i < extensions.Count; i++)
			{
				if (!mExtensionApplicationAssociations.ContainsKey(extensions[i]))
				{
					mExtensionApplicationAssociations.Add(extensions[i], "<DEFAULT>");
				}
			}
		}

		public void LoadSettings()
		{
            if(StopSavesAndLoads) return;

			string fileName = FileManager.UserApplicationDataForThisApplication + "FileAssociationSettings.xml";

			if (FileManager.FileExists(fileName))
			{

				FileAssociationsSave fas = FileManager.XmlDeserialize<FileAssociationsSave>(fileName);

				mAvailableApplications.Clear();
				mExtensionApplicationAssociations.Clear();

				mAvailableApplications = fas.AvailableApplications;

				for (int i = 0; i < fas.Extensions.Count; i++)
				{
					mExtensionApplicationAssociations.Add(fas.Extensions[i], fas.AssociatedApplications[i]);
				}

				AddDefaults();
			}
		}

		public string GetApplicationForExtension(string extension)
		{
			if (mExtensionApplicationAssociations.ContainsKey(extension))
			{
				return mExtensionApplicationAssociations[extension];
			}
			else
			{
				return null;
			}
		}

		public void SaveSettings()
		{
            if (StopSavesAndLoads) return;

			FileAssociationsSave fas = new FileAssociationsSave();

			fas.AvailableApplications = mAvailableApplications;

			foreach (KeyValuePair<string, string> kvp in mExtensionApplicationAssociations)
			{
				fas.Extensions.Add(kvp.Key);
				fas.AssociatedApplications.Add(kvp.Value);
			}

			FileManager.XmlSerialize(fas, FileManager.UserApplicationDataForThisApplication + "FileAssociationSettings.xml");
		}

		public string SetApplicationForExtension(string extension, string application)
		{
			if (application == "New Application...")
			{
				OpenFileDialog ofd = new OpenFileDialog();
				if (ofd.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
				{
					string result = ofd.FileName;

                    AddAvailableApplication(result, false);

					application = result;
				}
				else
				{
					return null;
				}
			}

            if (!string.IsNullOrEmpty(extension))
            {
                mExtensionApplicationAssociations[extension] = application;
            }

			SaveSettings();

            return application;
		}

        private void AddAvailableApplication(string result, bool saveSettings)
        {
            if (!AvailableApplications.Contains(result))
            {
                AvailableApplications.Add(result);
            }

            if (saveSettings)
            {
                SaveSettings();
            }
        }

        public void ReplaceApplicationInList(string oldName, string newName)
        {
            string containsName = null;

            oldName = oldName.Replace("\\", "/");

            for (int i = 0; i < mAvailableApplications.Count; i++)
            {
                if (String.Equals(mAvailableApplications[i].Replace("\\", "/"), oldName, StringComparison.OrdinalIgnoreCase))
                {
                    containsName = mAvailableApplications[i];
                    break;
                }
            }

            if (containsName != null)
            {
                mAvailableApplications.Remove(containsName);

                mAvailableApplications.Add(newName);

                SaveSettings();
            }
        }


        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc =
                TypeDescriptor.GetProperties(this, true);


            foreach (string extension in mExtensionApplicationAssociations.Keys)
            {
                ExtensionApplicationReturner ear = new ExtensionApplicationReturner(
                    extension, mExtensionApplicationAssociations);

                pdc = PropertyDescriptorHelper.AddProperty(pdc,
                    extension,
                    typeof(string), new AvailableApplicationsStringConverters(),
                    new Attribute[] { new CategoryAttribute("File Types") },
                    delegate(object sender, MemberChangeArgs args)
                    {
                        SetApplicationForExtension(args.Member, (string)args.Value);
                    }
                    ,
                    ear.GetApplication

                    );

            }

            return pdc;
        }


		#region ICustomTypeDescriptor Members

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
	}
}
