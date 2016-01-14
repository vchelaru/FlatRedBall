using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler.ViewModels
{
    public class FrameRecordViewModel
    {
        public double Time { get; set; }
        public List<RenderBreakSave> RenderBreaks
        {
            get;
            set;
        }

        public FrameRecordViewModel()
        {
            RenderBreaks = new List<RenderBreakSave>();
        }
    }
}
