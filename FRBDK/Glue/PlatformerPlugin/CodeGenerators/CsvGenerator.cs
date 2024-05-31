using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO.Csv;
using FlatRedBall.PlatformerPlugin.SaveClasses;
using GlueCommon.Models;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class CsvGenerator : Singleton<CsvGenerator>
    {
        #region Fields/Properties

        public static string StrippedCsvFile
        {
            get
            {
                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CsvInheritanceSupport)
                {
                    return "PlatformerValuesStatic";
                }
                else
                {
                    return "PlatformerValues";
                }
            }
        }
        public static string RelativeCsvFile => StrippedCsvFile + ".csv";

        #endregion

        public FilePath CsvPlatformerFileFor(EntitySave entity)
        {
            string absoluteFileName = 
                GlueCommands.Self.FileCommands.GetContentFolder(entity) + 
                RelativeCsvFile;
            return absoluteFileName;
        }


        internal Task GenerateFor(EntitySave entity, bool inheritsFromPlatformer, PlatformerEntityViewModel viewModel)
        {
            return TaskManager.Self.AddAsync(() =>
            {
                string contents = GenerateCsvContents(inheritsFromPlatformer, viewModel);

                var fileName = CsvPlatformerFileFor(entity);

                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    FileManager.SaveText(contents, fileName.FullPath);
                });

            }, $"Generating Platformer CSV for {entity}");
        }

        private string GenerateCsvContents(bool inheritsFromPlatformer, PlatformerEntityViewModel viewModel)
        {
            List<PlatformerValues> values = new List<PlatformerValues>();

            foreach(var valuesViewModel in viewModel.PlatformerValues)
            {
                var platformerValues = valuesViewModel.ToValues();

                var shouldInclude = inheritsFromPlatformer == false
                    || platformerValues.InheritOrOverwrite == InheritOrOverwrite.Overwrite;

                if (shouldInclude)
                {
                    values.Add(platformerValues);
                }
            }

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);

            var nameHeader = rcr.Headers[0];

            nameHeader.IsRequired = true;
            // Setting it to IsRequired is not sufficient, need to
            // modify the "Original Text" prop
            // chop off the closing quote, and add ", required)"
            nameHeader.OriginalText = nameHeader.OriginalText.Substring(0, nameHeader.OriginalText.Length - 1) + ", required)";

            rcr.Headers[0] = nameHeader;

            var toReturn = rcr.GenerateCsvString();

            return toReturn;
        }
    }
}
