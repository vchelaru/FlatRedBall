using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Particle;
using FlatRedBall.IO;
using System.Xml;
using FlatRedBall.Graphics.Particle;

namespace EditorObjects.Cleaners
{
    public class EmixCleaner
    {
        public static EmitterSave EmitterSave = new EmitterSave();
        public static EmissionSettingsSave EmissionSettingsSave = new EmissionSettingsSave();


        public static Type EmitterSaveType = typeof(EmitterSave);
        public static Type EmissionSettingsSaveType = typeof(EmissionSettingsSave);


        public static void Clean(string fileName)
        {
            System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument();

            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }
            xmlDocument.Load(fileName);

            XmlNode sceneNode = xmlDocument.ChildNodes[1];

            foreach (XmlNode child in sceneNode)
            {
                switch (child.Name)
                {
                    case "EmitterSave":
                        GeneralCleaner.CleanNode(child, EmitterSave, EmitterSaveType);
                        break;
                    //case "Camera":
                    //    CleanNode(child, cameraSave, cameraSaveType);
                    //    break;
                    //case "SpriteFrame":
                    //    CleanNode(child, spriteFrameSave, spriteFrameSaveType);
                    //    break;
                    //case "Text":
                    //    CleanNode(child, textSave, textSaveType);
                    //    break;

                }
            }

            xmlDocument.Save(fileName);

        }
    }
}
