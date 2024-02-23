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
using FlatRedBall.Forms.MVVM;
using FlatRedBall.Screens;
using System.Net.NetworkInformation;

namespace GlueTestProject.Screens
{
    class TestViewModel : ViewModel
    {
        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }

    class GumPageViewModel : ViewModel
    {
        public bool IsFirstChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSecondChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsThirdChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFourthChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFifthChecked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }

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
            TestRemovalOfBinding();

            TestDialogBoxPaging();
        }

        private void TestDialogBoxPaging()
        {
            var dialogBox = Forms.DialogBoxInstance;

            var styledString =
                "This is [Color=Orange]some really[/Color] long[Color=Pink] text[/Color]. " +
                "[Color=Purple]We[/Color] want to show long text so that it line wraps[Color=Cyan] " +
                "and[/Color] so that it has [Color=Green]enough[/Color] text to fill an " +
                "[Color=Yellow]entire page[/Color]. The DialogBox control should automatically " +
                "detect if the text is too long for a single page and it should break it up into " +
                "multiple pages.You can advance this dialog by clicking on it with the " +
                "[Color=Blue]mouse[/Color] or by pressing the [Color=Gold]space bar[/Color] " +
                "on the keyboard.";
            dialogBox.LettersPerSecond = null;

            var gumObject = dialogBox.Visual;
            var gue = gumObject.GetGraphicalUiElementByName("TextInstance");
            gue.TextOverflowVerticalMode = RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
            dialogBox.Show(styledString);

            // As of Feb 22, 2024 this is an old .glux so it doesn't codegen the height limit:

            var textRenderable = gue.Component as RenderingLibrary.Graphics.Text;

            textRenderable.WrappedText.Count.ShouldBe(5);

        }

        private void TestRemovalOfBinding()
        {
            int timesCalled = 0;
            var vm = new TestViewModel();
            vm.PropertyChanged += (not, used) =>
            {
                timesCalled++;
            };

            timesCalled.ShouldBe(0);

            // Stack it a few deep to make sure all works okay
            var stack = new StackPanel();
            var innerStack = new StackPanel();
            var checkBox = new CheckBox();
            checkBox.SetBinding(nameof(checkBox.IsChecked), nameof(TestViewModel.IsChecked));
            stack.AddChild(innerStack);
            innerStack.AddChild(checkBox);
            stack.Visual.AddToManagers();

            stack.BindingContext = vm;

            timesCalled.ShouldBe(0);

            checkBox.IsChecked = true;

            timesCalled.ShouldBe(1);

            stack.Visual.RemoveFromManagers();

            stack.Visual.BindingContext.ShouldBe(null);

            timesCalled.ShouldBe(1);

            checkBox.IsChecked = false;

            timesCalled.ShouldBe(1);

            stack.Visual.AddToManagers();
            stack.Visual.BindingContext = vm;

            checkBox.IsChecked = !checkBox.IsChecked;

            timesCalled.ShouldBe(2);

            stack.Visual.RemoveFromManagers();

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
            //IsActivityFinished = true;

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
