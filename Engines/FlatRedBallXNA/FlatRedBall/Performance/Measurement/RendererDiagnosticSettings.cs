using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Performance.Measurement
{
    public class RendererDiagnosticSettings
    {
        public bool RenderBreaksPerformStateChanges
        {
            get;
            set;
        }

        public RendererDiagnosticSettings()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            RenderBreaksPerformStateChanges = true;
        }
    }
}
