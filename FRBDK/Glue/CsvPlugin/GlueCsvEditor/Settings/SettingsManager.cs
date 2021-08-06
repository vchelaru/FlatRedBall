using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GlueCsvEditor.Data;
using Newtonsoft.Json;

namespace GlueCsvEditor.Settings
{
    static class SettingsManager
    {
        private const string SettingsExtension = ".csvSettings";

        public static EditorLayoutSettings LoadEditorSettings(CsvData csvData)
        {
            var settingsPath = GetSettingsPath(csvData);

            try
            {
                string content;
                using (var reader = File.OpenText(settingsPath))
                    content = reader.ReadToEnd();

                var settings = JsonConvert.DeserializeObject<EditorLayoutSettings>(content);
                return settings;
            }
            catch (IOException)
            {
                // File doesn't exist, thus no settings for this have been saved yet
                return new EditorLayoutSettings();
            }
            catch (JsonException)
            {
                // Saved settings file is invalid, so ignore it
                return new EditorLayoutSettings();
            }
        }

        public static void SaveEditorSettings(CsvData csvData, EditorLayoutSettings settings)
        {
            if (settings == null)
                return; // Nothing to save

            var settingsPath = GetSettingsPath(csvData);

            try
            {
                var content = JsonConvert.SerializeObject(settings);

                using (var stream = File.Open(settingsPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Saving of editor settings failed: {0}", ex.Message);
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetSettingsPath(CsvData csvData)
        {
            var csvPath = Path.GetDirectoryName(csvData.CsvPath);
            var csvName = Path.GetFileNameWithoutExtension(csvData.CsvPath);
            var settingsPath = string.Concat(csvPath, "\\", csvName, SettingsExtension);
            return settingsPath;
        }

    }
}