using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using DoorsDemo.Entities;
using DoorsDemo.Screens;
using FlatRedBall.Forms.Controls;
using System.Linq;
using System.CodeDom;
using System.Threading.Tasks;

namespace DoorsDemo.Screens
{

    public partial class GameScreen
    {
        async void OnPlayerListVsDoorListCollisionOccurred (Entities.Player player, Entities.Door door) 
        {
            if(player.InputEnabled && player.PressedUp)
            {
                door.Open();
                player.InputEnabled = false;

                var thisDoorLetter = door.Name[0];

                await GameScreenGum.ToBlackAnimation.PlayAsync();

                var otherDoor = DoorList.First(item => item != door && item.Name.StartsWith(thisDoorLetter.ToString()));

                player.X = otherDoor.X;
                player.Y = otherDoor.Y - 16;

                UpdateBoundsForPosition(otherDoor.Position);

                CameraControllingEntityInstance.ApplyTarget(
                    CameraControllingEntityInstance.GetTarget(), lerpSmooth: false);

                otherDoor.Open();

                await GameScreenGum.FromBlackAnimation.PlayAsync();

                otherDoor.Close();

                player.InputEnabled = true;

            }
        }
    }
}
