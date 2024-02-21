using FlatRedBall.Glue.Settings;
using FlatRedBall.Glue.GuiDisplay.Facades;

namespace FlatRedBall.Glue
{
	public static class EditorData
	{
		static FileAssociationSettings mFileAssociationSettings = new();
        static PreferenceSettings mPreferenceSettings = new();
        private static GlueLayoutSettings mGlueLayoutSettings = new();

		public static FileAssociationSettings FileAssociationSettings => mFileAssociationSettings;
        public static PreferenceSettings PreferenceSettings => mPreferenceSettings;
        public static GlueLayoutSettings GlueLayoutSettings => mGlueLayoutSettings;

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
        }
	}
}
