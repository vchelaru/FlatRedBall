using FlatRedBall.AnimationEditorForms.CommandsAndState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public class ZoomControlLogic
    {
        ComboBox mComboBox;

        List<int> mAvailableZoomLevels = new List<int>();
        public List<int> AvailableZoomLevels
        {
            get
            {
                return mAvailableZoomLevels;
            }
        }
        public ZoomControlLogic(ComboBox comboBox)
        {
            mComboBox = comboBox;

            InitializeComboBox();
        }
        public int PercentageValue
        {
            get
            {
                return int.Parse(mComboBox.Text.Substring(0, mComboBox.Text.Length - 1));
            }
            set
            {
                mComboBox.Text = value.ToString() + "%";

                ApplicationEvents.Self.CallAfterZoomChange();
            }
        }
        private void InitializeComboBox()
        {
            mAvailableZoomLevels.Add(1600);
            mAvailableZoomLevels.Add(1200);
            mAvailableZoomLevels.Add(800);
            mAvailableZoomLevels.Add(600);
            mAvailableZoomLevels.Add(400);
            mAvailableZoomLevels.Add(300);
            mAvailableZoomLevels.Add(200);
            mAvailableZoomLevels.Add(175);
            mAvailableZoomLevels.Add(150);
            mAvailableZoomLevels.Add(125);
            mAvailableZoomLevels.Add(100);
            mAvailableZoomLevels.Add(80);
            mAvailableZoomLevels.Add(60);
            mAvailableZoomLevels.Add(40);
            mAvailableZoomLevels.Add(20);

            foreach (var value in mAvailableZoomLevels)
            {
                mComboBox.Items.Add(value.ToString() + "%");
            }

            mComboBox.Text = "100%";

        }




    }
}
