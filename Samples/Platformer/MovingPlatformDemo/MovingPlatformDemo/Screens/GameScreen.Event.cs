using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using MovingPlatformDemo.Entities;
using MovingPlatformDemo.Screens;
namespace MovingPlatformDemo.Screens
{
    public partial class GameScreen
    {
        void OnMovingPlatformListCollectionChanged (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) 
        {
            foreach(MovingPlatform item in e.NewItems)
            {
                item.StoreBasePosition();
            }
        }

    }
}
