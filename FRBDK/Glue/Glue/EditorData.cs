using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Settings;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue
{
	public static class EditorData
	{
		static FileAssociationSettings mFileAssociationSettings = new FileAssociationSettings();
        static PreferenceSettings mPreferenceSettings = new PreferenceSettings();
        private static GlueLayoutSettings mGlueLayoutSettings = new GlueLayoutSettings();

		public static FileAssociationSettings FileAssociationSettings
		{
			get { return mFileAssociationSettings; }
		}

        public static PreferenceSettings PreferenceSettings
        {
            get { return mPreferenceSettings; }
        }

        public static GlueLayoutSettings GlueLayoutSettings
        {
            get { return mGlueLayoutSettings; }
        }

        public static void LoadPreferenceSettings()
        {
            mPreferenceSettings = PreferenceSettings.LoadSettings();
        }

        public static void LoadGlueLayoutSettings()
        {
            mGlueLayoutSettings = GlueLayoutSettings.LoadSettings();
        }


        static EditorData()
        {
            FacadeContainer.Self.ApplicationSettings = FileAssociationSettings;
            FacadeContainer.Self.GlueState = FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self;
            //FacadeContainer.Self.ObjectFinder = ObjectFinder.Self;

        }
	}
}
