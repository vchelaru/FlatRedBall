using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall;
using FlatRedBall.IO;

namespace EditorObjects.EditorSettings
{
    public class EmitterEditorSettingsSave
    {
        public CameraSave Camera = new CameraSave();

        public void ApplyTo(Camera cameraToApplyTo)
        {
            bool isPixelPerfect2D = this.Camera.OrthogonalHeight <= 0;

            this.Camera.SetCamera(cameraToApplyTo);

            if (isPixelPerfect2D)
            {
                cameraToApplyTo.UsePixelCoordinates();
            }
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public static EmitterEditorSettingsSave Load(string fileName)
        {
            return FileManager.XmlDeserialize<EmitterEditorSettingsSave>(fileName);
        }
    }
}
