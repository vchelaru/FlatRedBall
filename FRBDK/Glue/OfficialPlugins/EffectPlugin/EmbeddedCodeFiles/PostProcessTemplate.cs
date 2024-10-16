using FlatRedBall;
using FlatRedBall.Graphics.PostProcessing;
using Microsoft.Xna.Framework.Graphics;

namespace ReplaceNamespace
{
    public class ReplaceClassName : IPostProcess
    {
        private readonly Effect _effect;

        public ReplaceClassName(Effect effect)
        {
            _effect = effect;
        }

        ReplaceClassMembers




        public void Apply(Texture2D source, RenderTarget2D target)
        {
            var device = FlatRedBallServices.GraphicsDevice;

            var oldRt = device.GetRenderTargets().FirstOrDefault().RenderTarget as RenderTarget2D;
            device.SetRenderTarget(target);
            Apply(source);
            device.SetRenderTarget(oldRt);
        }
        public void Apply(Texture2D sourceTexture)
        {
            ReplaceApplyBody
        }
    }
}
