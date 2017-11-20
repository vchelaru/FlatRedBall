using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Texture2D
    {
        public string Name { get; private set; }
        public WebGLTexture WebGLTexture { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public void InitTexture(WebGLRenderingContext gl, string file)
        {
            Name = file;
            this.WebGLTexture = gl.CreateTexture();

            var textureImageElement = new HTMLImageElement();

            textureImageElement.OnLoad = (ev) =>
            {
                this.HandleLoadedTexture(textureImageElement, gl);

                Width = textureImageElement.Width;
                Height = textureImageElement.Height;

                Console.WriteLine("OnLoad");

            };

            textureImageElement.Src = file;
        }

        void HandleLoadedTexture(HTMLImageElement image, WebGLRenderingContext gl)
        {
            gl.PixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
            gl.BindTexture(gl.TEXTURE_2D, this.WebGLTexture);
            gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);

            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_NEAREST);
            gl.GenerateMipmap(gl.TEXTURE_2D);
            gl.BindTexture(gl.TEXTURE_2D, null);


        }
    }
}
