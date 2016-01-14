using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;

namespace EditorObjects.EditorSettings
{
	public class SplineEditorSettingsSave
	{
		public CameraSave BoundsCamera = new CameraSave();
		public CameraSave ViewCamera = new CameraSave();

        public SplineEditorSettingsSave()
        {
            BoundsCamera.Z = 40;
            ViewCamera.Z = 40;
        }

		public void Save(string fileName)
		{
			FileManager.XmlSerialize(this, fileName);
		}

		public static SplineEditorSettingsSave Load(string fileName)
		{
			return FileManager.XmlDeserialize<SplineEditorSettingsSave>(fileName);
		}
	}
}
