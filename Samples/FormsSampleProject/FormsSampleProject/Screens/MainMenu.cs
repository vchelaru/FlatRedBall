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
            dialog.LettersPerSecond = 47;
            dialog.IsFocused = true;
            var dialogVisual = dialog.Visual;
            dialogVisual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            dialogVisual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            dialogVisual.Y = -20;
            dialogVisual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            dialogVisual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;

            await dialog.ShowAsync("This is a DialogBox in [Color=Red]FlatRedBall.Forms[/Color]. It supports " +
                "lots of features including typing out the text [Color=Yellow]letter-by-letter[/Color], multiple pages, " +
                "and even [Color=Green]styling[/Color] using BBCode. Wow, how handy! You can create mulitple " +
                "pages by explicitly giving it a string array, or you can give it a long string and let the " +
                "dialog box [Color=Pink]automatically[/Color] handle the multiple pages.");



            //await dialog.ShowAsync("This is [Color=Orange]some really[/Color] long [Color=Pink]text[/Color]. [Color=Purple]We[/Color] want to show long text so that it " + 
            //    "line wraps [Color=Cyan]and[/Color] so that it has [Color=Green]enough[/Color] text to fill an [Color=Yellow]entire page[/Color]. The DialogBox control " +
            //    "should automatically detect if the text is too long for a single page and it should break " +
            //    "it up into multiple pages. You can advance this dialog by clicking on it with the [Color=Blue]mouse[/Color] or " +
            //    "by pressing the [Color=Gold]space bar[/Color] on the keyboard.");

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
