using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.IO;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Lighting;
using FlatRedBall.Graphics;
using EditorObjects;

namespace ParticleEditor
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
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Renderer.UseRenderTargets = false;
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);

            LightManager.AddAmbientLight(Color.White);

            
            IsMouseVisible = true;
            GuiManager.IsUIEnabled = true;

            FormMethods methods = new FormMethods();
            SpriteManager.Camera.CameraModelCullMode = FlatRedBall.Graphics.CameraModelCullMode.None;

            methods.AllowFileDrop(EditorData.HandleDragDrop);

            EditorData.Initialize();
            GuiData.Initialize();

            ProcessCommandLineArguments();

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            try
            {
                FlatRedBallServices.Update(gameTime);


                EditorData.Update();

                if (EditorData.Scene != null)
                {
                    foreach (SpriteGrid spriteGrid in EditorData.Scene.SpriteGrids)
                    {
                        spriteGrid.Manage();
                    }
                }

                base.Update(gameTime);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
        }

        protected override void Draw(GameTime gameTime)
        {

            FlatRedBallServices.Draw();

            base.Draw(gameTime);
        }


        static void ProcessCommandLineArguments()
        {
            foreach (string s in Program.CommandLineArguments)
            {
                ProcessCommandLineArgument(s);
            }
        }

        public static void ProcessCommandLineArgument(string argument)
        {
            string extension = FileManager.GetExtension(argument);

            switch (extension)
            {
                case "emix":
                    AppCommands.Self.File.LoadEmitters(argument);

                    break;
            }
        }
    }
}
