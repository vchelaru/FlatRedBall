using System;
using System.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Opt-in mode for a program that launches the real editor and reads back what happened (the
    /// editor-launch test in GlueUnitTests). Enabled by the "automationlog=&lt;path&gt;" command line argument,
    /// see <see cref="CommandLineManager.AutomationLogPath"/>. While enabled:
    ///  - every output line, error line and dialog text is appended to the file, one tagged entry each;
    ///  - dialogs are not shown, so nothing modal blocks the desktop. Each answers as if closed without a click;
    ///  - user settings are neither read nor saved, so the run neither depends on nor changes the developer's
    ///    recent-project list and window layout.
    /// Off (the default), nothing here runs.
    /// </summary>
    public static class AutomationLog
    {
        public const string OutputTag = "[OUTPUT]";
        public const string ErrorTag = "[ERROR]";
        public const string DialogTag = "[DIALOG]";
        public const string ReadyTag = "[READY]";
        public const string ExitingTag = "[EXITING]";

        static readonly object _lock = new object();
        static string _path;

        public static bool IsEnabled => _path != null;

        public static void StartIfRequested()
        {
            var path = CommandLineManager.Self.AutomationLogPath;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _path = path;
            File.WriteAllText(_path, "");

            GlueSettingsSave.StopSavesAndLoads = true;
            FlatRedBall.Glue.Settings.FileAssociationSettings.StopSavesAndLoads = true;
            FlatRedBall.Glue.Settings.GlueLayoutSettings.StopSavesAndLoads = true;

            DialogService.ShowMessageImpl = text => Write(DialogTag, text);
            DialogService.ShowConfirmImpl = (text, _, _) =>
            {
                Write(DialogTag, text);
                return null;
            };
            DialogService.ShowChoiceImpl = (text, _) =>
            {
                Write(DialogTag, text);
                return null;
            };
            DialogService.ShowDeleteImpl = viewModel =>
            {
                Write(DialogTag, "Delete dialog");
                return false;
            };
        }

        public static void Write(string tag, string text)
        {
            if (_path == null)
            {
                return;
            }

            lock (_lock)
            {
                File.AppendAllText(_path, $"{tag} {text}{Environment.NewLine}");
            }
        }
    }
}
