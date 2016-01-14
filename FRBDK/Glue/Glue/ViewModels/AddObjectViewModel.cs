using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.ViewModels
{
    public class AddObjectViewModel
    {
        public DialogResult DialogResult { get; set;}
        public SourceType SourceType { get; set;}
        public string SourceClassType { get; set;}
        public string SourceFile { get; set;}
        public string ObjectName { get; set;}
        public string SourceNameInFile { get; set;}
        public string SourceClassGenericType { get; set;}
    }
}
