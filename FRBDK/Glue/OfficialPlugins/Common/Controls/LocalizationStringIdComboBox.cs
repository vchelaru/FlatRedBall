using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.Common.Controls
{
    internal class LocalizationStringIdComboBox : EditableComboBoxDisplay
    {
        protected override IEnumerable<object> CustomOptions
        {
            get
            {
                var localizationDatabase = GlueState.Self.GetAllReferencedFiles().Where(item => item.IsDatabaseForLocalizing);

                foreach(var rfs in localizationDatabase)
                {
                    var file = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                    if (file.Exists())
                    {
                        var runtime = CsvFileManager.CsvDeserializeToRuntime(file.FullPath);


                        foreach (var row in runtime.Records)
                        {
                            bool shouldProcess = row.Length > 1 &&
                                !string.IsNullOrWhiteSpace(row[0]) &&
                                row[0].StartsWith("//") == false;

                            if (shouldProcess)
                            {
                                yield return row[0];
                            }
                        }
                    }
                }
            }
        }

        public LocalizationStringIdComboBox()
        {

        }



    }
}
