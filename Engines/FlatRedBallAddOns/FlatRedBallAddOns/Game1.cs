using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using FlatRedBall;
using FlatRedBall.Content.AnimationChain;

using FlatRedBall.Graphics.Animation;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content;
using FlatRedBall.Instructions;
using System.Threading;
using System.Reflection;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Content.Particle;
using FlatRedBall.Graphics.Particle;
using System.Drawing;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using FlatRedBall.Content.Polygon;
using FlatRedBallAddOns.Screens;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Screens;

namespace FlatRedBallAddOns
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            FlatRedBallServices.InitializeFlatRedBall(this, this.graphics);

            //FlatRedBallServices.GraphicsOptions.SetResolution(320, 240);
            // Uncomment the following line and add your Screen's fully qualified name
            // if using Screens in your project.  If not, or if you don't know what it means,
            // just ignore the following line for now.
            // For more information on Screens see the Screens wiki article on FlatRedBall.com.
            //Screens.ScreenManager.Start(typeof(FlatRedBallAddOns.Screens.TestScreen).FullName);

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            ScreenManager.Activity();


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }


    }
}
