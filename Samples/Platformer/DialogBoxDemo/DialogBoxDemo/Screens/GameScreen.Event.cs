using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using DialogBoxDemo.Entities;
using DialogBoxDemo.Screens;
using FlatRedBall.Forms.Controls.Games;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Gui;
using FlatRedBall.Localization;

namespace DialogBoxDemo.Screens
{
    public partial class GameScreen
    {
        DialogBox currentDialogBox;
        async void OnPlayerListTalkCollisionVsNpcListCollisionOccurred (Entities.Player first, Entities.Npc second) 
        {
            if(first.TalkInput.WasJustPressed && currentDialogBox == null)
            {
                if(first.X < second.X)
                {
                    second.DirectionFacing = HorizontalDirection.Left;
                }
                else
                {
                    second.DirectionFacing = HorizontalDirection.Right;
                }
                foreach(var player in PlayerList)
                {
                    player.InputEnabled = false;
                }

                var playerGamepad = first.InputDevice as Xbox360GamePad;

                if(playerGamepad != null)
                {
                    GuiManager.GamePadsForUiControl.Clear();
                    GuiManager.GamePadsForUiControl.Add(playerGamepad);
                }

                await ShowDialogBox(first.InputDevice, second.DialogId);

                foreach (var player in PlayerList)
                {
                    player.InputEnabled = true;
                }
            }
        }

        private async Task ShowDialogBox(IInputDevice inputDevice, string stringId)
        {
            currentDialogBox = new DialogBox();
            currentDialogBox.IsFocused = true;
            var pages = LocalizationManager.Translate(stringId).Split('\n');

            var asGamepad = inputDevice as Xbox360GamePad;

            // Prevents the push that brought this up from advancing the first dialog
            asGamepad?.Clear();

            currentDialogBox.AdvancePageInputPredicate = () =>
            {
                return inputDevice.DefaultPrimaryActionInput.WasJustPressed ||
                    asGamepad?.ButtonPushed(Xbox360GamePad.Button.X) == true ||
                    asGamepad?.ButtonPushed(Xbox360GamePad.Button.A) == true ||
                    asGamepad?.ButtonPushed(Xbox360GamePad.Button.B) == true ||
                    asGamepad?.ButtonPushed(Xbox360GamePad.Button.Y) == true;
            };

            await currentDialogBox.ShowDialog(pages);

            currentDialogBox = null;
        }
    }
}
