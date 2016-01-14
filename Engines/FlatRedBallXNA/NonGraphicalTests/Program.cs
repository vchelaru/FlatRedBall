using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueViewUnitTests;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;

namespace NonGraphicalTests
{
    class Program
    {
        static void Main(string[] args)
        {
            FlatRedBallServices.InitializeCommandLine(null);

            SpriteManager.Cameras.Add(new Camera("test", 800, 600));
            System.IntPtr ptr = new IntPtr();
            InputManager.Initialize(ptr);
            GuiManager.Initialize((Texture2D)null, new Cursor(SpriteManager.Camera));
            TextManager.Initialize(null);



            bool succeeded = false;
            try
            {

                TestFramework.RunTests();
                succeeded = true;
            }

            catch (Exception e)
            {
                succeeded = false;
                System.Console.WriteLine("Error:\n" + e.ToString());
            }


            if (succeeded)
            {
                System.Console.WriteLine("All tests passed!");
            }

            System.Console.ReadLine();
        }
    }
}
