using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using System.Xml;
using FlatRedBall.Content.Scene;
using System.Reflection;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Content.Saves;

namespace EditorObjects.Cleaners
{
    public static class ScnxCleaner
    {
        public static SpriteSave SpriteSave = new SpriteSave();
        public static CameraSave CameraSave = new CameraSave();
        public static SpriteFrameSave SpriteFrameSave = new SpriteFrameSave();
        public static TextSave TextSave = new TextSave();

        public static Type SpriteSaveType = typeof(SpriteSave);
        public static Type CameraSaveType = typeof(CameraSave);
        public static Type SpriteFrameSaveType = typeof(SpriteFrameSave);
        public static Type TextSaveType = typeof(TextSave);



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
                    case "Sprite":
                        GeneralCleaner.CleanNode(child, SpriteSave, SpriteSaveType);
                        break;
                    case "Camera":
                        GeneralCleaner.CleanNode(child, CameraSave, CameraSaveType);
                        break;
                    case "SpriteFrame":
                        GeneralCleaner.CleanNode(child, SpriteFrameSave, SpriteFrameSaveType);
                        break;
                    case "Text":
                        GeneralCleaner.CleanNode(child, TextSave, TextSaveType);
                        break;

                }
            }

            xmlDocument.Save(fileName);

        }
    }
}
