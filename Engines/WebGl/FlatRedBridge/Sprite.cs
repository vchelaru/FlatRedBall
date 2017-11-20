using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall
{
    public class Sprite
    {
        Texture2D texture;
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; UpdateToTextureScale(); }
        }

        public float Width { get; set; } = 5f;
        public float Height { get; set; } = 10;

        public float X { get; set; }
        public float Y { get; set; }

        private void UpdateToTextureScale()
        {
            //Console.WriteLine("UpdateToTextureScale");
            //this.Width = texture.Width;
            //this.Height = texture.Height;
        }
    }
}
