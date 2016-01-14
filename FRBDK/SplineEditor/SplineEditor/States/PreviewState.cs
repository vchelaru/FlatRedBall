using SplineEditor.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolTemplate.Entities;

namespace SplineEditor.States
{


    public class PreviewState
    {
        public PreviewVelocityType PreviewVelocityType
        {
            get;
            set;
        }

        public float ConstantPreviewVelocity
        {
            get;
            set;
        }

        public float SplinePointRadius
        {
            get;
            set;
        }


        public PreviewState()
        {
            ConstantPreviewVelocity = 5;
            SplinePointRadius = 15;
        }
    }
}
