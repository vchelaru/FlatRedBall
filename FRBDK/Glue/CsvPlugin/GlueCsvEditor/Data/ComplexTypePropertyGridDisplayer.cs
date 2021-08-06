using System;
using System.Linq;
using FlatRedBall.Glue.GuiDisplay;
using System.ComponentModel;
using System.Diagnostics;

namespace GlueCsvEditor.Data
{
    public class ComplexTypePropertyGridDisplayer : PropertyGridDisplayer
    {
        #region Fields

        protected CsvData _csvData;
        CategoryAttribute mPropertyCategory = new CategoryAttribute("Properties");

        #endregion

        #region Properties

        public string ColumnType
        {
            get;
            set;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                mInstance = value;
                UpdateDisplayedFields(value as ComplexCsvTypeDetails);
                base.Instance = value;

                // Victor Chelaru
                // Right now we ahve to set the after member change *after* changing base.Instance
                // I need to fix this
                GetPropertyGridMember("UseNewSyntax").AfterMemberChange += AfterUseNewSyntaxChanged;

            }
        }

        public ComplexTypeUpdatedDelegate ComplexTypeUpdatedHandler { get; set; }

        #endregion

        public ComplexTypePropertyGridDisplayer(CsvData csvData)
        {
            _csvData = csvData;
        }

        protected void UpdateDisplayedFields(ComplexCsvTypeDetails complexTypeDetails)
        {
            if (complexTypeDetails == null)
                return;

            ResetToDefault();
            
            // Write excludes
            ExcludeMember("ConstructorValues");
            ExcludeMember("Properties");


            // Add properties
            for (int i = 0; i < complexTypeDetails.Properties.Count; i++)
            {
                ComplexTypeProperty property = complexTypeDetails.Properties[i];

                IncludeComplexProperty(property);
            }

            if (complexTypeDetails.UseNewSyntax == false)
            {
                ExcludeMember("Namespace");
                ExcludeMember("TypeName");
            }

        }

        private void AfterUseNewSyntaxChanged(object sender, MemberChangeArgs args)
        {

            CallOnUpdate();
        }

        private void IncludeComplexProperty(ComplexTypeProperty property)
        {
            string propertyName = property.Name;
            if (!string.IsNullOrWhiteSpace(property.Type))
            {
                propertyName = string.Concat(propertyName, " (", property.Type, ")");
            }

            // Setup events
            Func<object> getter = () => property.Value;
            MemberChangeEventHandler setter = (sender, args) =>
            {
                property.Value = args.Value as string;
                CallOnUpdate();
            };

            // Setup type converter
            var knownValues = _csvData.GetKnownValuesForType(property.Type);
            TypeConverter converter = null;
            var enumerable = knownValues as string[] ?? knownValues.ToArray();
            if (enumerable.Any())
            {
                converter = new AvailableKnownValuesTypeConverter(enumerable);
            }

            IncludeMember(propertyName, typeof(string), setter, getter, converter, new Attribute[] { mPropertyCategory });
        }

        private void CallOnUpdate()
        {
            if (ComplexTypeUpdatedHandler != null)
            {
                var complexCsvTypeDetails = mInstance as ComplexCsvTypeDetails;
                if (complexCsvTypeDetails != null)
                    ComplexTypeUpdatedHandler(complexCsvTypeDetails.ToString());
            }
        }

        public delegate void ComplexTypeUpdatedDelegate(string complexTypeString);
    }
}
