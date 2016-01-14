using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System.ComponentModel;

// TODO: replace these with the processor input and output types.
using TInput = FlatRedBall.Content.IO.Csv.BuildtimeCsvRepresentation;
using TOutput = FlatRedBall.Content.IO.Csv.BuildtimeCsvRepresentation;

namespace FlatRedBall.Content.IO.Csv
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type TInput to TOutput. The input and output types may be the same if
    /// the processor wishes to alter data without changing its type.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    ///
    /// TODO: change the ContentProcessor attribute to specify the correct
    /// display name for this processor.
    /// </summary>
    [ContentProcessor(DisplayName = "CsvProcessor - FlatRedBall")]
    public class CsvProcessor : ContentProcessor<TInput, TOutput>
    {
        #region Properties

        [DefaultValue(false)]
        [DisplayName("Enable Encryption")]
        [Description("Whether or not the data should be encrypted")]
        public bool EnableEncryption
        {
            get;
            set;
        }

        [DefaultValue("")]
        [DisplayName("Encryption Password")]
        [Description("Password used to generate a key for encrypting the data")]
        public string EncryptionPassword
        {
            get;
            set;
        }

        #endregion

        public override TOutput Process(TInput input, ContentProcessorContext context)
        {
            input.EnableEncryption = EnableEncryption;
            input.EncryptionPassword = EncryptionPassword ?? String.Empty;
            return input;
        }
    }
}