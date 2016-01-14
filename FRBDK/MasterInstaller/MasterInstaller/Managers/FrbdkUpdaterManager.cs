using System;
using System.Collections.Generic;
using System.Text;
using MasterInstaller.Components;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;
using Microsoft.Win32;
using OfficialPlugins.FrbdkUpdater;
using vbAccelerator.Components.Shell;
using System.IO;
using FileManager = OfficialPlugins.FrbdkUpdater.FrbdkUpdaterSettings;

namespace MasterInstaller.Managers
{
    public class FrbdkUpdaterManager
    {
        static FrbdkUpdaterManager mSelf;


        static string FrbdkApplicationData
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\FlatRedBall\"; 
            }
        }

        static string StartMenuFolder
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\FlatRedBall\";
            }
        }

        static string GetProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        static string FlatRedBallInProgramFiles
        {
            get
            {
                return GetProgramFilesx86() + @"\FlatRedBall\";
            }
        }

        public static string FrbdkInProgramFiles
        {
            get
            {
                return FlatRedBallInProgramFiles + @"FRBDK\";
            }
        }

        public static FrbdkUpdaterManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new FrbdkUpdaterManager();
                }
                return mSelf;
            }
        }

        public void CreateUpdaterFileAndAddShortcutFiles(object throwaway)
        {
            CreateUpdaterFiles();
            AddShortcuts();
        }

        void CreateUpdaterFiles()
        {
            FrbdkUpdaterSettings fus = new FrbdkUpdaterSettings();
            fus.CleanFolder = true;
            fus.GlueRunPath = null;
            fus.SelectedSource = "DailyBuild/";
            fus.SelectedDirectory = ComponentStorage.GetValue<string>(FrbdkSetupComponent.Path);
            fus.Passive = true;

            fus.SaveSettings();

#if !DEBUG
            var key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\FlatRedBall");
            key.SetValue("FrbdkDir", fus.SelectedDirectory);

            key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\FlatRedBall");
#endif

        }

        void AddShortcuts()
        {

            Directory.CreateDirectory(StartMenuFolder);
            Directory.CreateDirectory(FlatRedBallInProgramFiles);
            Directory.CreateDirectory(FrbdkInProgramFiles);

            CreateShortcutFor(@"Xna 4 Tools\Glue.exe", "Game development environment using FlatRedBall");
            CreateShortcutFor(@"Xna 4 Tools\NewProjectCreator.exe", "Creates new code-only FlatRedBall projects");

            CreateShortcutFor(@"AIEditor.exe", "Tool for creating NodeNetwork files");
            CreateShortcutFor(@"Xna 4 Tools\AnimationEditorPlugin\AnimationEditor.exe", "Tool for creating Sprite animation files (AnimationChains)");
            CreateShortcutFor(@"ParticleEditor.exe", "Tool for creating particle emitter files");
            CreateShortcutFor(@"PolygonEditorXna.exe", "Tool for creating ShapeCollection files");
            CreateShortcutFor(@"Xna 4 Tools\SplineEditor.exe", "Tool for creating Spline files");
            CreateShortcutFor(@"SpriteEditor.exe", "Tool for creating graphical scenes");

        }

        void CreateShortcutFor(string fileName, string description)
        {
            using (ShellLink shortcut = new ShellLink())
            {
                // The file that the .lnk file links to
                string target = ComponentStorage.GetValue<string>(FrbdkSetupComponent.Path) + fileName;
                shortcut.Target = target;
                shortcut.WorkingDirectory = Path.GetDirectoryName(target);
                shortcut.Description = description;
                shortcut.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;

                // Where to save the .lnk file
                shortcut.Save(StartMenuFolder + FileManager.RemovePath(FileManager.RemoveExtension(fileName)) + ".lnk");
            }

        }
    }
}
