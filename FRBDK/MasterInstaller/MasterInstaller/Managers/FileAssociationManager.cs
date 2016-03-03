using MasterInstaller.Components;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MasterInstaller.Managers
{
    public class FileAssociationManager
    {

        public static void SetAllFileAssociations()
        {
            // Set up file associations
            string frbdkFolder =
                FrbdkUpdaterManager.FrbdkInProgramFiles;
                //ComponentStorage.GetValue<string>(FrbdkSetupComponent.Path);

            // todo - set up more file associations here...
            string animationEditor = frbdkFolder + "Xna 4 Tools\\AnimationEditorPlugin\\AnimationEditor.exe";
            if (System.IO.File.Exists(animationEditor))
            {
                Associate(".achx", "FlatRedBall.FRBDK.AnimationEditor", "AnimationChain List file",
                    animationEditor);
            }

            string aiEditorFileName = frbdkFolder + "AIEditor.exe";
            if (System.IO.File.Exists(aiEditorFileName))
            {
                Associate(".nntx", "FlatRedBall.FRBDK.AIEditor", "NodeNetwork file",
                    aiEditorFileName);
            }

            string particleEditorFileName = frbdkFolder + "ParticleEditor.exe";
            if (System.IO.File.Exists(particleEditorFileName))
            {
                Associate(".emix", "FlatRedBall.FRBDK.ParticleEditor", "Emitter List file",
                    particleEditorFileName);
            }

            string polygonEditorFileName = frbdkFolder + "PolygonEditorXna.exe";
            if (System.IO.File.Exists(polygonEditorFileName))
            {
                Associate(".shcx", "FlatRedBall.FRBDK.PolygonEditor", "ShapeCollection file",
                    polygonEditorFileName);
            }

            string spriteEditorFileName = frbdkFolder + "SpriteEditor.exe";
            if (System.IO.File.Exists(spriteEditorFileName))
            {
                Associate(".scnx", "FlatRedBall.FRBDK.SpriteEditor", "Scene file",
                    spriteEditorFileName);
            }

            string glueFileName = frbdkFolder + "Xna 4 Tools\\Glue.exe";
            if (System.IO.File.Exists(glueFileName))
            {
                Associate(".glux", "FlatRedBall.FRBDK.Glue", "Glue file",
                    glueFileName);
            }

        }


        // Associate file extension with progID, description, icon and application
        static void Associate(string extension,
               string progID, string description, string applicationFileName)
        {
            Registry.ClassesRoot.CreateSubKey(extension).SetValue("", progID);
            if (progID != null && progID.Length > 0)
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progID))
                {
                    if (description != null)
                        key.SetValue("", description);

                    // icon is a string, just in case I ever want to add it back
                    //if (icon != null)
                    //    key.CreateSubKey("DefaultIcon").SetValue("", ToShortPathName(icon));
                    if (applicationFileName != null)
                        key.CreateSubKey(@"Shell\Open\Command").SetValue("",
                                    ToShortPathName(applicationFileName) + " \"%1\"");
                }
        }

        // Return true if extension already associated in registry
        public static bool IsAssociated(string extension)
        {
            return (Registry.ClassesRoot.OpenSubKey(extension, false) != null);
        }

        [DllImport("Kernel32.dll")]
        private static extern uint GetShortPathName(string lpszLongPath,
            [Out] StringBuilder lpszShortPath, uint cchBuffer);

        // Return short path format of a file name
        private static string ToShortPathName(string longName)
        {
            StringBuilder s = new StringBuilder(1000);
            uint iSize = (uint)s.Capacity;
            uint iRet = GetShortPathName(longName, s, iSize);
            return s.ToString();
        }
    }
}
