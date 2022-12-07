using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    internal class CsvDuplicateItemInFileViewModel : FileErrorViewModel
    {
        public string DuplicateErrorDetails
        {
            get; set;
        }

        public override void UpdateDetails()
        {
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(FilePath);
            if (rfs == null)
            {
                return;
            }

            RefreshDuplicateErrorDetails();

            Details = $"CSV File {FilePath} error: {DuplicateErrorDetails}";
        }

        private void RefreshDuplicateErrorDetails()
        {
            DuplicateErrorDetails = null;
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(FilePath);

            bool succeeded = false;
            RuntimeCsvRepresentation rcr = null;
            if (rfs != null)
            {
                CsvCodeGenerator.DeserializeToRcr(rfs.CsvDelimiter,
                    FilePath,
                    out rcr, out succeeded);
            }
            if (succeeded)
            {
                DuplicateErrorDetails = CsvCodeGenerator.GetDuplicateMessageIfDuplicatesFound(
                    rcr,
                    rfs.CreatesDictionary,
                    FilePath.FullPath);
            }
        }

        public override bool GetIfIsFixed()
        {
            if(base.GetIfIsFixed())
            {
                return true;
            }

            RefreshDuplicateErrorDetails();

            return string.IsNullOrEmpty(DuplicateErrorDetails);
        }
    }
}
