using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions;
using FlatRedBall.Performance.Measurement;
using FlatRedBallProfiler.ViewModels;

namespace FlatRedBallProfiler.Controls
{

    enum LineDrawingState
    {
        NeedsToDrawDiagonalLine,
        NeedsToCreateHorizontalLine,
        IsExtendingHorizontalLine
    }

    /// <summary>
    /// Interaction logic for RenderBreakHistoryControl.xaml
    /// </summary>
    public partial class RenderBreakHistoryControl : UserControl
    {
        LineDrawingState lineDrawingState;

        List<Line> lineList = new List<Line>();

        bool isOnHorizontalLine = false;

        RenderBreakHistoryViewModel ViewModel
        {
            get
            {
                return this.DataContext as RenderBreakHistoryViewModel;
            }
        }
        
        public RenderBreakHistoryControl()
        {
            InitializeComponent();

            CreateHorizontalLines();

            this.DataContextChanged += HandleDataContextChanged;

            this.DataContext = new RenderBreakHistoryViewModel();

        }

        private void CreateHorizontalLines()
        {
            for (int i = 0; i < 30; i++)
            {
                CreateHorizontalLines(i);

            }

            // Congratulations War Haven, you broke everything! Time to expand what
            // we show:
            for (int i = 30; i < 100; i+=5)
            {
                CreateHorizontalLines(i);
            }

            for (int i = 100; i <= 200; i += 10)
            {
                CreateHorizontalLines(i);
            }
        }

        private void CreateHorizontalLines(int renderBreakCount)
        {
            var line = new Line();
            line.X1 = 0;
            line.X2 = 100000;

            line.Y1 = CountToY(renderBreakCount);
            line.Y2 = CountToY(renderBreakCount);


            byte alpha = 255;

            if(renderBreakCount % 10 == 0)
            {
                alpha = 120;
            }
            else if(renderBreakCount % 5 == 0)
            {
                alpha = 40;
            }
            else
            {
                alpha = 20;
            }


            Brush brush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
            line.Stroke = brush;

            MainCanvas.Children.Add(line);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.RecordedFrames.CollectionChanged += HandleRenderBreaksAdded;
            }
        }

        private void HandleRenderBreaksAdded(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var collection = ViewModel.RecordedFrames;

            FrameRecordViewModel newFrame= null;
            FrameRecordViewModel previousFrame = null;

            if(collection.Count > 0)
            {
                newFrame = collection.Last();
            }
            if(collection.Count > 1)
            {
                previousFrame = collection[collection.Count - 2];
            }

            if (previousFrame != null && previousFrame.RenderBreaks.Count != newFrame.RenderBreaks.Count)
            {
                lineDrawingState = LineDrawingState.NeedsToDrawDiagonalLine;
            }

            bool needsNewLine = lineDrawingState == LineDrawingState.NeedsToDrawDiagonalLine || lineDrawingState == LineDrawingState.NeedsToCreateHorizontalLine;


            if (needsNewLine)
            {
                if(lineDrawingState == LineDrawingState.NeedsToDrawDiagonalLine)
                {
                    lineDrawingState = LineDrawingState.NeedsToCreateHorizontalLine;
                }
                else if(lineDrawingState == LineDrawingState.NeedsToCreateHorizontalLine)
                {
                    lineDrawingState = LineDrawingState.IsExtendingHorizontalLine;
                }
                AddLineFor(newFrame, previousFrame);
            }
            else
            {
                ExtendExistingLineFor(newFrame);
            }
        }

        private void ExtendExistingLineFor(FrameRecordViewModel newFrame)
        {
            var lastLine = lineList.LastOrDefault();

            if(lastLine != null)
            {
                lastLine.X2 = ViewModel.TimeToX(newFrame.Time);
                lastLine.Y2 = CountToY(newFrame.RenderBreaks.Count);

                if(double.IsNaN( MainCanvas.Height) || lastLine.Y2 > MainCanvas.Height)
                {
                    MainCanvas.Height = lastLine.Y2;
                }

                if (lastLine.X2 > MainCanvas.Width || double.IsNaN(MainCanvas.Width))
                {
                    MainCanvas.Width = lastLine.X2;
                    this.ScrollViewer.ScrollToRightEnd();
                }
            }
        }

        private void AddLineFor(FrameRecordViewModel newBreak, FrameRecordViewModel previous)
        {


            var line = new Line();

            // Let's try making this a little thicker so it's easier to read, but transparent so it shows some of the lines behind it
            byte alpha = 100;
            Brush brush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
            line.Stroke = brush;

            line.StrokeThickness = 5;

            line.X1 = 0;
            line.X2 = ViewModel.TimeToX(newBreak.Time);


            line.Y1 = CountToY(newBreak.RenderBreaks.Count);
            line.Y2 = line.Y1;

            if (line.X2 > MainCanvas.Width || double.IsNaN(MainCanvas.Width))
            {
                MainCanvas.Width = line.X2;
                this.ScrollViewer.ScrollToRightEnd();
            }
            if(previous != null)
            {
                line.X1 = ViewModel.TimeToX(previous.Time);
                line.Y1 = CountToY(previous.RenderBreaks.Count);
            }

            lineList.Add(line);

            this.MainCanvas.Children.Add(line);
        }

        double CountToY(int count)
        {
            return count * 10;
        }

        private void HandleCurrentFrameRecordClick(object sender, RoutedEventArgs e)
        {
            if (Renderer.RecordRenderBreaks)
            {
                ViewModel.RecordCurrentFrameRenderBreaks();
            }
            else
            {
                Renderer.RecordRenderBreaks = true;

                var instruction = new DelegateInstruction(ViewModel.RecordCurrentFrameRenderBreaks);
                float delay = .3f;
                instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + delay;

                InstructionManager.Add(instruction);
            }
        }

    }
}
