using SplineEditor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolTemplate.Entities;

namespace SplineEditor.Commands
{
    public class PreviewCommands
    {
        SplineCrawler mSplineCrawler;

        public SplineCrawler SplineCrawler
        {
            get
            {
                return mSplineCrawler;
            }
        }
        

        public void CreateSplineCrawler()
        {
            var currentSpline = AppState.Self.CurrentSpline;

            if (currentSpline == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "There's no spline to show movement on");
            }
            else
            {
                if (mSplineCrawler != null)
                    mSplineCrawler.Destroy();

                // Let's make sure we have the most up-to-date information:
                AppState.Self.CurrentSpline.CalculateDistanceTimeRelationships(.02f);

                mSplineCrawler = new SplineCrawler(currentSpline);
                mSplineCrawler.ConstantVelocityValue = AppState.Self.Preview.ConstantPreviewVelocity;
                mSplineCrawler.PreviewVelocityType = AppState.Self.Preview.PreviewVelocityType;
            }
        }


        internal void SplineCrawlerActivity()
        {
            if (mSplineCrawler != null)
            {
                mSplineCrawler.Activity();

                // The SplineCrawler destroys itself in its Activity if it reaches the end
                if (mSplineCrawler.IsDestroyed)
                    mSplineCrawler = null;
            }
        }




    }
}
