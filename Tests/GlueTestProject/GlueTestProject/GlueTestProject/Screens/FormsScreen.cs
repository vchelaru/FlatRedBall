using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using GlueTestProject.Forms.Controls;
using FlatRedBall.Forms.Controls;
using GlueTestProject.TestFramework;

namespace GlueTestProject.Screens
{
	public partial class FormsScreen
	{
        CustomUserControl control;
        void CustomInitialize()
		{
            // Test if derived controls automatically get visuals from their base if the derived doesn't exist...
            control = new CustomUserControl();
            control.Visual.AddToManagers();


            TestRadioButtonSelected();

            TestListBoxSelected();

		}

        private void TestListBoxSelected()
        {
            var listBox = new ListBox();

            var listBoxItem = new ListBoxItem();
            listBox.Items.Add(listBoxItem);

            listBox.Items.Add(1);
            listBox.Items.Add(2);

            object selectedItem = null;

            listBox.SelectionChanged += (not, used) =>
            {
                selectedItem = listBox.SelectedObject;
            };

            listBox.SelectedObject = listBoxItem;

            selectedItem.ShouldBe(listBoxItem, "because the SelectionChanged should be raised");
        }

        private void TestRadioButtonSelected()
        {
            var radioButton1 = new RadioButton();
            radioButton1.Visual.AddToManagers();

            var radioButton2 = new RadioButton();
            radioButton2.Visual.AddToManagers();

            radioButton1.IsChecked = true;
            radioButton2.IsChecked.ShouldBe(false);

            radioButton2.IsChecked = true;
            radioButton1.IsChecked.ShouldBe(false, "because checking the 2nd should uncheck the first");

            radioButton1.Visual.RemoveFromManagers();
            radioButton2.Visual.RemoveFromManagers();
        }

        void CustomActivity(bool firstTimeCalled)
		{
            IsActivityFinished = true;

		}

		void CustomDestroy()
		{
            control.Visual.RemoveFromManagers();

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
