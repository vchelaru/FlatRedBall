using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Instructions;
using System.ComponentModel;

namespace FlatRedBall.Glue.SaveClasses
{
    public class CustomVariableInNamedObject : InstructionSave
    {
        [Browsable(false)]
        [CategoryAttribute("Varaible Set Events")]
        public string EventOnSet
        {
            get;
            set;
        }

    }
}
