using System;
using System.Collections.Generic;
using System.Text;
using GlueControl.Models;

namespace GlueControl.Managers
{
    internal class GlueState
    {
        public GlueElement CurrentElement
        {
            get => Editing.EditingManager.Self.CurrentGlueElement;
        }
        public static GlueState Self { get; }

        static GlueState() => Self = new GlueState();
    }
}
