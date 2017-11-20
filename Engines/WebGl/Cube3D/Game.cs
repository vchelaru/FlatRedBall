using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cube3D
{
    public class Game : IDisposable
    {
        public void Dispose()
        {

        }

        protected virtual void Initialize()
        {

        }

        protected virtual void Update()
        {

        }

        protected virtual void Draw()
        {

        }

        public void Run()
        {
            Initialize();

            InternalLoop();
        }

        private void InternalLoop()
        {
            Update();

            Draw();

            Global.SetTimeout(InternalLoop, 16);

        }

        protected void ShowError(HTMLCanvasElement canvas, string message)
        {
            canvas.ParentElement.ReplaceChild(new HTMLParagraphElement { InnerHTML = message }, canvas);
        }
    }
}
