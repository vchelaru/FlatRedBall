using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;
using FlatRedBall.Forms.Controls.Popups;
using FormsSampleProject.ViewModels;
using FlatRedBall.Forms.Controls.Games;




namespace FormsSampleProject.Screens
{
    public partial class MainMenu
    {
        MainMenuViewModel ViewModel;
        void CustomInitialize()
        {
            ViewModel = new MainMenuViewModel();
            ViewModel.ComboBoxItems.Add("Combo Box Item 1");
            ViewModel.ComboBoxItems.Add("Combo Box Item 2");
            ViewModel.ComboBoxItems.Add("Combo Box Item 3");
            ViewModel.ComboBoxItems.Add("Combo Box Item 4");
            ViewModel.ComboBoxItems.Add("Combo Box Item 5");

            Forms.ComboBoxInstance.SetBinding(
                nameof(Forms.ComboBoxInstance.Items),
                nameof(ViewModel.ComboBoxItems));

            Forms.ListBoxInstance.SetBinding(
                nameof(Forms.ListBoxInstance.Items),
                nameof(ViewModel.ListBoxItems));

            Forms.BindingContext = ViewModel;
            Forms.ButtonStandardInstance.Click += HandleButtonClicked;
            Forms.AddItemButton.Click += HandleAddItemClicked;

            Forms.ShowDialogButton.Click += HandleShowDialogButtonClicked;


        }

        private async void HandleShowDialogButtonClicked(object sender, EventArgs e)
        {
            var dialog = new DialogBox();
            //dialog.LettersPerSecond = 24;
            dialog.LettersPerSecond = null;
            dialog.IsFocused = true;
            var dialogVisual = dialog.Visual;
            dialogVisual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            dialogVisual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            dialogVisual.Y = 20;


            await dialog.ShowAsync("This is some really long text. We want to show long text so that it " + 
                "line wraps and so that it has enough text to fill an entire page. The DialogBox control " +
                "should automatically detect if the text is too long for a single page and it should break " +
                "it up into multiple pages. You can advance this dialog by clicking on it with the mouse or " +
                "by pressing the space bar on the keyboard");

            dialog.Visual.RemoveFromManagers();
        }

        private void HandleButtonClicked(object sender, EventArgs e)
        {
            ToastManager.Show(
                $"This is a toast instance.\nThe button was clicked at {DateTime.Now}.");
        }

        private void HandleAddItemClicked(object sender, EventArgs e)
        {
            ViewModel.ListBoxItems.Add($"Item @ {DateTime.Now}");
        }

        void CustomActivity(bool firstTimeCalled)
        {


        }

        void CustomDestroy()
        {


        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

    }
}
