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
using System.Windows.Forms;
using FlatRedBall.IO;
using PolygonEditor;
using PolygonEditor.Gui;
using FlatRedBall.ManagedSpriteGroups;
using PolygonEditorXna.IO;

namespace PolygonEditorXna
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

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
           
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);

            //SpriteManager.Camera.LightingEnabled = true;
            //SpriteManager.Camera.Lights.SetDefaultLighting(FlatRedBall.Graphics.Lighting.LightCollection.DefaultLightingSetup.Evening);

            FlatRedBall.Graphics.Lighting.LightManager.AddAmbientLight(Color.White);
            

            IsMouseVisible = true;
            GuiManager.IsUIEnabled = true;

            SpriteManager.Camera.CameraModelCullMode = FlatRedBall.Graphics.CameraModelCullMode.None;

            EditorData.Initialize(null);
            GuiData.Initialize();

            ProcessCommandLineArguments();

            // TODO:  Set the name of the file that the user is editing.

            base.Initialize();
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);
            // TODO: Add your update logic here


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

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();
            base.Draw(gameTime);
        }

        void ProcessCommandLineArguments()
        {
            foreach (string s in Program.CommandLineArguments)
            {
                ProcessCommandLineArgument(s);
            }
        }


        public void ProcessCommandLineArgument(string argument)
        {
            string extension = FileManager.GetExtension(argument);

            switch (extension)
            {
                case "scnx":
                    EditorData.LoadScene(argument);

                    break;
                case "plylstx":

                    EditorData.LoadPolygonList(argument);
                    break;
                case "shcx":
                    FileLoader.LoadShapeCollection(argument);
                    break;

            }
        }

        protected void ProcessDroppedFile(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            switch (extension)
            {

                case "scnx":
                    EditorData.LoadScene(fileName);
                    break;

            }
        }

        private void GameForm_DragDrop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (string fileName in files)
                    {
                        string extension = FileManager.GetExtension(fileName);

                        switch (extension)
                        {
                            case "plylstx":
                                EditorData.LoadPolygonList(fileName);

                                //this.BringToFront();
                                //this.Focus();

                                break;
                            case "scnx":
                                //GameData.guiData.fileButtonWindow.AskToReplaceOrInsertNewScene(fileName);
                                break;

                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return;
                }
            }

        }






    }
}
