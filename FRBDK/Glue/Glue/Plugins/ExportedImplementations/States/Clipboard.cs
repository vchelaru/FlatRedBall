using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.States
{
    public class Clipboard
    {
        public object CopiedObject
        {
            get;
            set;
        }

        public EntitySave CopiedEntity
        {
            get
            {
                return CopiedObject as EntitySave;
            }
            set
            {
                CopiedObject = value;
            }
        }

        public ScreenSave CopiedScreen
        {
            get
            {
                return CopiedObject as ScreenSave;
            }
            set
            {
                CopiedObject = value;
            }
        }

        public NamedObjectSave CopiedNamedObject
        {
            get
            {
                return CopiedObject as NamedObjectSave;
            }
            set
            {
                CopiedObject = value;
            }
        }
    }
}
