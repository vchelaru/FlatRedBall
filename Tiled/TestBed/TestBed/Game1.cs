using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.Scene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using TMXGlueLib;
using TestBed.Screens;
using FlatRedBall.IO;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content;
using FlatRedBall.Content.Math.Geometry;

namespace TestBed
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 600;
            Content.RootDirectory = "Content";
			
			BackStack<string> bs = new BackStack<string>();
			bs.Current = string.Empty;
        }

        protected override void Initialize()
        {
            Renderer.UseRenderTargets = false;
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);
			GlobalContent.Initialize();


			Screens.ScreenManager.Start(typeof(TestBed.Screens.TestScreen).FullName);

            SpriteManager.Camera.BackgroundColor = Color.Black;
            

            SpriteManager.Camera.Position.Z += 250;
            SpriteManager.Camera.Position.Y -= 75;
            SpriteManager.Camera.CameraCullMode = CameraCullMode.None;
            //FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter.Point;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            TiledMapSave tms = TiledMapSave.FromFile("isometrictest.tmx");
            Scene s = tms.ToScene(typeof(TestBed.Screens.TestScreen).FullName, 1.0f);
            s.AddToManagers();

            //SpriteEditorScene sec = SpriteEditorScene.FromScene(s);
            //sec.Save("isometrictest.scnx");

            SceneSave sec = tms.ToSceneSave(1.0f);

            sec.Save("isometrictest.scnx");

            ShapeCollectionSave scs = tms.ToShapeCollectionSave("nonodes");
            scs.Save("polygons.schx");
            ShapeCollection sc = tms.ToShapeCollection("nonodes");
            sc.AddToManagers();            

            // Convert once in case of any exceptions
            NodeNetwork nodeNetwork = tms.ToNodeNetwork();
            nodeNetwork.Visible = true;
            base.LoadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.W))
            {
                SpriteManager.Camera.Y += 10;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                SpriteManager.Camera.Y -= 10;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                SpriteManager.Camera.X -= 10;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                SpriteManager.Camera.X += 10;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                SpriteManager.Camera.RotationX += .01f;
            }
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                SpriteManager.Camera.RotationX -= .01f;
            }

            if (keyboardState.IsKeyDown(Keys.OemPlus))
            {
                SpriteManager.Camera.Z -= 3;
            }
            if (keyboardState.IsKeyDown(Keys.OemMinus))
            {
                SpriteManager.Camera.Z += 3;
            }


            FlatRedBallServices.Update(gameTime);

            ScreenManager.Activity();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }
    }
}
