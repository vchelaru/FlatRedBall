using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler.ViewModels
{
    
    public class RenderBreakHistoryViewModel
    {
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        double lastRefreshInFrbTime;

        public bool DidRenderBreaksChangeLastRecord
        {
            get
            {
                if(RecordedFrames.Count < 2)
                {
                    return false;
                }
                else
                {
                    var last = RecordedFrames.Last();
                    var beforeLast = RecordedFrames[RecordedFrames.Count - 2];

                    return last.RenderBreaks.Count != beforeLast.RenderBreaks.Count;
                }
            }
        }

        public ObservableCollection<FrameRecordViewModel> RecordedFrames
        {
            get;
            set;
        }

        public RenderBreakHistoryViewModel()
        {
            RecordedFrames = new ObservableCollection<FrameRecordViewModel>();

            dispatcherTimer.Tick += new EventHandler(Refresh);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, milliseconds:50);
            dispatcherTimer.Start();
        }

        private void Refresh(object sender, EventArgs e)
        {
            bool shouldRefresh = true && Renderer.RecordRenderBreaks;

            if(shouldRefresh)
            {
                RecordCurrentFrameRenderBreaks();
            }
        }

        public void RecordCurrentFrameRenderBreaks()
        {
            var list = FlatRedBall.Graphics.Renderer.LastFrameRenderBreakList;

            FrameRecordViewModel viewModel = new FrameRecordViewModel();

            viewModel.Time = TimeManager.CurrentTime;

            foreach (var runtime in list)
            {
                RenderBreakSave save = RenderBreakSave.FromRenderBreak(runtime);

                viewModel.RenderBreaks.Add(save);
            }

            RecordedFrames.Add(viewModel);
        }

        public double TimeToX(double time)
        {
            var firstTime = 0.0;

            var firstFrame = RecordedFrames.FirstOrDefault();
            if (firstFrame != null)
            {
                firstTime = firstFrame.Time;
            }
            return (time-firstTime) * 10;
        }
    }
}
