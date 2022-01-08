using FlatRedBall.Glue.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.ViewModels
{
    public class AddCustomVariableViewModel
    {
        public string VariableName
        {
            get; set;
        }

        public string TunnelingObject
        {
            get; set;
        }

        public string TunnelingVariable
        {
            get; set;
        }

        public string ResultType
        {
            get; set;
        }

        public string OverridingType
        {
            get; set;
        }

        public string TypeConverter
        {
            get; set;
        }

        public bool IsStatic
        {
            get; set;
        }

        public CustomVariableType DesiredVariableType
        {
            get; set;
        }
    }
}
