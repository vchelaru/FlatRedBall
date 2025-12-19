using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using OfficialPlugins.SpritePlugin.Managers;
using SkiaGum.Wpf;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

internal class AnimationChainVisualEditingManager
{
    private GumSKElement _canvas;
    private readonly CameraLogic _cameraLogic;

    public AnimationChainVisualEditingManager(SkiaGum.Wpf.GumSKElement canvas, CameraLogic cameraLogic)
    {
        _canvas = canvas;
        _cameraLogic = cameraLogic;
    }

    public void HandleMousePush(MouseButtonEventArgs args)
    {
        var positionOnCanvas = args.GetPosition(_canvas);
        _cameraLogic.GetWorldPosition(positionOnCanvas, out double worldXPushed, out double worldYPushed);

        RecordFramePositions();
    }

    private void RecordFramePositions()
    {

    }

    public void HandleMouseMove(System.Windows.Input.MouseEventArgs args)
    {

    }
}
