using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;

namespace PluginTestbed.SourceSetup
{
    public partial class SourceSetupPluginForm : Form
    {
        private readonly SourceSetupPlugin _plugin;
        private SourceSetupSettings _settings;

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
        );

        public SourceSetupPluginForm(SourceSetupPlugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
        }

        private void btnSetupSource_Click(object sender, EventArgs e)
        {
            LogMessage("Setting up source.");

            var action = new Action(() =>
                             {
                                 try
                                 {
                                     var project = ProjectManager.ProjectBase;
                                     string slnName;
                                     string slnDebugName;

                                     LogMessage("Processing " + project.Name);
                                     throw new NotImplementedException();
                                     //slnName = IdeManager.LocateSolution(project.FullFileName);
                                     slnDebugName = slnName.Substring(0, slnName.Length - 4) + "_Debug.sln";

                                     if (!File.Exists(slnDebugName))
                                     {
                                         File.Copy(slnName, slnDebugName);
                                     }

                                     //IdeManager.Initialize(GetVersion(project));
                                     //IdeManager.AddProjectsToSolution(IdeManager.LocateSolution(project.FullFileName),
                                                                      //GetEngineProjects(project));
                                     //IdeManager.ReleaseInstance();

                                     LinkDlls(project);

                                     foreach (var syncedProject in ProjectManager.SyncedProjects)
                                     {
                                         LogMessage("Processing " + syncedProject.Name);
                                         throw new NotImplementedException();
                                         //slnName = IdeManager.LocateSolution(syncedProject.FullFileName);
                                         slnDebugName = slnName.Substring(0, slnName.Length - 4) + "_Debug.sln";

                                         if (!File.Exists(slnDebugName))
                                         {
                                             File.Copy(slnName, slnDebugName);
                                         }

                                         //IdeManager.Initialize(GetVersion(syncedProject));
                                         //IdeManager.AddProjectsToSolution(
                                             //IdeManager.LocateSolution(syncedProject.FullFileName),
                                             //GetEngineProjects(syncedProject));
                                         //IdeManager.ReleaseInstance();

                                         LinkDlls(syncedProject);
                                     }

                                     LogMessage("Complete.");
                                 }
                                 catch (Exception ex)
                                 {
                                     LogMessage("Error.");
                                     BeginInvoke(new Action(() =>
                                                                {
                                                                    MessageBox.Show(ex.ToString(), @"Error");
                                                                }));
                                 }
                             });

            action.BeginInvoke(null, null);
        }

        private List<string> GetEngineProjects(ProjectBase project)
        {
            var result = new List<string>();
            string str;

            
            if (project is AndroidProject)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\FlatRedBallAndroid\FlatRedBallMonoDroid\FlatRedBallMonoDroid.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\MonoGame.Framework\MonoGame.Framework.Android.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\ThirdParty\Lidgren.Network\Lidgren.Network.Android.csproj";
                CheckFile(str);
                result.Add(str);
            }
            else if (project is WindowsPhoneProject)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\FlatRedBallWindowsPhone.csproj";
                CheckFile(str);
                result.Add(str);
            }
            else if (project is Xna360Project)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\FlatRedBallXbox360.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\FlatRedBall.Content.csproj";
                CheckFile(str);
                result.Add(str);
            }
            else if (project is Xna4_360Project)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\FlatRedBallXbox4_360.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\FlatRedBall.ContentXna4.csproj";
                CheckFile(str);
                result.Add(str);
            }
            else if (project is Xna4Project)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\FlatRedBallXbox4.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\FlatRedBall.ContentXna4.csproj";
                CheckFile(str);
                result.Add(str);
            }
            else if (project is XnaProject)
            {
                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\FlatRedBall.csproj";
                CheckFile(str);
                result.Add(str);

                str = CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\FlatRedBall.Content.csproj";
                CheckFile(str);
                result.Add(str);
            }

            return result;
        }

        private string CleanBaseDirectory(string selectedEngineDirectory)
        {
            if (selectedEngineDirectory.EndsWith(@"/") || selectedEngineDirectory.EndsWith(@"\"))
            {
                return selectedEngineDirectory.Substring(0, selectedEngineDirectory.Length - 1);
            }

            return selectedEngineDirectory;
        }

        private void LinkDlls(ProjectBase project)
        {
            var librariesPath = FileManager.GetDirectory(project.FullFileName) + @"Libraries\";
            string str, strPdb, engineStr, enginePdb;

            if (project is FsbProject)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if(str.Contains("FlatRedBall.dll"))
                    {
                        if(File.Exists(str))
                            File.Delete(str);
                        if(File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\FlatSilverBall\Bin\Debug\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\FlatSilverBall\Bin\Debug\FlatRedBall.pdb", IntPtr.Zero);
                    }else if(str.Contains("SilverArcade.SilverSprite.Core.dll"))
                    {
                        if(File.Exists(str))
                            File.Delete(str);
                        if(File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\SilverSpriteSource\SilverArcade.SilverSprite.Core\Bin\Debug\SilverArcade.SilverSprite.Core.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\SilverSpriteSource\SilverArcade.SilverSprite.Core\Bin\Debug\SilverArcade.SilverSprite.Core.pdb", IntPtr.Zero);
                    }else if(str.Contains("SilverArcade.SilverSprite.dll"))
                    {
                        if(File.Exists(str))
                            File.Delete(str);
                        if(File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\SilverSpriteSource\SilverArcade.SilverSprite\Bin\Debug\SilverArcade.SilverSprite.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatSilverBall\SilverSpriteSource\SilverArcade.SilverSprite\Bin\Debug\SilverArcade.SilverSprite.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is MdxProject)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBallMdx.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallMDX\bin\Debug\FlatRedBallMdx.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallMDX\bin\Debug\FlatRedBallMdx.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is AndroidProject)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBall.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\FlatRedBallMonoDroid\FlatRedBallMonoDroid\bin\Debug\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\FlatRedBallMonoDroid\FlatRedBallMonoDroid\bin\Debug\FlatRedBall.pdb", IntPtr.Zero);
                    }
                    else if (str.Contains("MonoGame.Framework.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\MonoGame.Framework\bin\Debug\MonoGame.Framework.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\MonoGame.Framework\bin\Debug\MonoGame.Framework.pdb", IntPtr.Zero);
                    }
                    else if (str.Contains("Lidgren.Network.Android.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\ThirdParty\Lidgren.Network\bin\Debug\Lidgren.Network.Android.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallAndroid\MonoGame\ThirdParty\Lidgren.Network\bin\Debug\Lidgren.Network.Android.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is WindowsPhoneProject)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBall.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is Xna4_360Project)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBall.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is Xna4Project)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBall.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.pdb", IntPtr.Zero);
                    }
                    else if (str.Contains("FlatRedBall.Content.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.pdb", IntPtr.Zero);
                    }
                }
            }
            else if (project is XnaProject)
            {
                foreach (var dll in project.LibraryDlls)
                {
                    str = librariesPath + dll;
                    strPdb = str.Remove(str.Length - 4) + ".pdb";

                    if (str.Contains("FlatRedBall.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.pdb", IntPtr.Zero);
                    }
                    else if (str.Contains("FlatRedBall.Content.dll"))
                    {
                        if (File.Exists(str))
                            File.Delete(str);
                        if (File.Exists(strPdb))
                            File.Delete(strPdb);

                        CreateHardLinkWithCheck(str, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\FlatRedBall.Content.dll", IntPtr.Zero);
                        CreateHardLinkWithCheck(strPdb, CleanBaseDirectory(_settings.SelectedEngineDirectory) + @"\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\FlatRedBall.Content.pdb", IntPtr.Zero);
                    }
                }
            }
        }

        private void CreateHardLinkWithCheck(string str, string s, IntPtr zero)
        {
            if (!CreateHardLink(str, s, zero))
            {
                BeginInvoke(new Action(() =>
                                           {
                                               MessageBox.Show(@"Unable to create hard link between " + str + @" and " +
                                                               s);
                                           }));

            }
        }

        private void CheckFile(string str)
        {
            if(!File.Exists(str))
                throw new Exception(str + " does not exist!");
        }

        private string GetVersion(ProjectBase project)
        {
            var vsProject = project as VisualStudioProject;
            return vsProject != null ? vsProject.NeededVisualStudioVersion : "9.0";
        }

        private void btnEnginePath_Click(object sender, EventArgs e)
        {
            fbdSourceSetup.SelectedPath = tbEnginePath.Text;
            if (fbdSourceSetup.ShowDialog() == DialogResult.OK)
            {
                tbEnginePath.Text = fbdSourceSetup.SelectedPath;
            }
        }

        private void SourceSetupPluginForm_Load(object sender, EventArgs e)
        {
            _settings = SourceSetupSettings.LoadSettings();
            tbEnginePath.Text = _settings.SelectedEngineDirectory;
            cbUseSource.Checked = _settings.UseSource;
        }

        private void tbEnginePath_TextChanged(object sender, EventArgs e)
        {
            _settings.SelectedEngineDirectory = tbEnginePath.Text;
            _settings.SaveSettings();
        }

        private void cbUseSource_CheckedChanged(object sender, EventArgs e)
        {
            _settings.UseSource = cbUseSource.Checked;
            _settings.SaveSettings();
        }

        private void LogMessage(string message)
        {
            BeginInvoke(new Action(() =>
                                       {
                                           lblLog.Text = message;
                                       }));
        }
    }
}
