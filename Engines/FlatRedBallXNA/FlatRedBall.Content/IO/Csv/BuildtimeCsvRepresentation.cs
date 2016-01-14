using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;
using System.Xml.Serialization;

namespace FlatRedBall.Content.IO.Csv
{
    #region XML Docs
    /// <summary>
    /// Represents the content-pipeline version of RuntimeCsvRepresentation.
    /// </summary>
    #endregion
    public class BuildtimeCsvRepresentation : FlatRedBall.IO.Csv.RuntimeCsvRepresentation
    {
        #region Properties

        public bool EnableEncryption
        {
            get;
            set;
        }

        public string EncryptionPassword
        {
            get;
            set;
        }

        #endregion
    }
}
