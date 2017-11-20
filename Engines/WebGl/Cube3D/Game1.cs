using Bridge;
using Bridge.Html5;
using Bridge.WebGL;
using FlatRedBall;
using System;

namespace Cube3D
{
    public class Game1 : Game
    {
        public static string CanvasId = "canvas1";
        static Renderer renderer;

        public Game1() : base()
        {

        }

        protected override void Initialize()
        {
            renderer = new Renderer();

            renderer.Initialize(CanvasId);

            Console.WriteLine("Sprite");

            var sprite = new Sprite();
            sprite.X = 3;
            sprite.Texture = FlatRedBallServices.Load("crate.gif");
            sprite.Y = 10;
            renderer.Sprites.Add(sprite);

            //Console.WriteLine("Sprite");
            //sprite = new Sprite();
            //sprite.Texture = FlatRedBallServices.Load("blueguy.png");
            //sprite.X = 10;
            //sprite.Y = -10;
            //renderer.Sprites.Add(sprite);

            //Console.WriteLine("Sprite");
            //sprite = new Sprite();
            //sprite.Texture = FlatRedBallServices.Load("blueguy.png");
            //sprite.X = 0;
            //renderer.Sprites.Add(sprite);

            if (Renderer.WebGlRenderingContext != null)
            {
                Document.AddEventListener(EventType.KeyDown, renderer.HandleKeyDown);
                Document.AddEventListener(EventType.KeyUp, renderer.HandleKeyUp);
            }
            else
            {
                string message =
                    "<b>Either the browser doesn't support WebGL or it is disabled." + 
                    "<br>Please follow <a href=\"http://get.webgl.com\">Get WebGL</a>.</b>";
                ShowError(renderer.canvas, message);
            }

            base.Initialize();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Draw()
        {
            renderer.Draw();

            base.Draw();
        }
    }
}
