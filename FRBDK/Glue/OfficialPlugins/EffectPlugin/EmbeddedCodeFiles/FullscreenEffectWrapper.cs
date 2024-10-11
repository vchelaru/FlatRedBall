using System;
using FlatRedBall;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReplaceNamespace;

public class FullscreenEffectWrapper
{
    protected VertexPositionColorNormalTexture[] Vertices;
    protected VertexBuffer VertexBuffer;
    
    public float Period { get; set; } = 1;

    public Vector3 TopRight { get; set; } = new(1f, 1f, 0f);
    public Vector3 BottomRight { get; set; } = new(1f, -1f, 0f);
    public Vector3 TopLeft { get; set; } = new(-1f, 1f, 0f);
    public Vector3 BottomLeft { get; set; } = new(-1f, -1f, 0f);

    public FullscreenEffectWrapper()
    {
        Vertices = new VertexPositionColorNormalTexture[]
        {
            new(TopRight, Color.Blue, new Vector3(), new Vector2(1f, 0f)), // Top-Right
            new(BottomRight, Color.Green, new Vector3(), new Vector2(1f, 1f)), // Bottom-Right
            new(TopLeft, Color.Red, new Vector3(), new Vector2(0f, 0f)), // Top-Left
            new(BottomLeft, Color.Yellow, new Vector3(), new Vector2(0f, 1f)), // Bottom-Left
        };
        VertexBuffer = new VertexBuffer(FlatRedBallServices.GraphicsDevice, typeof(VertexPositionColorNormalTexture), Vertices.Length, BufferUsage.WriteOnly);
    }

    public virtual void Draw(Camera camera, Effect effect, Texture2D texture, float zDepth = 0, bool isVisible = true)
    {
        if (effect.IsDisposed)
        {
            throw new ArgumentException("Effect has been disposed. Are you attempting to draw with an effect loaded with a non-global content manager?" +
                                        " It is recommended that effect files for post processing be stored in global content.");
        }

        if (texture.IsDisposed)
        {
            throw new ArgumentException("Texture has been disposed. Are you attempting to draw with a texture loaded with a non-global content manager?" +
                                        " It is recommended that texture files for post processing be stored in global content.");
        }
        
        if (!isVisible) { return; }
        
        SetParameters(effect, camera, texture);
        SetVertexData(camera, Vertices, zDepth);
        DrawVertices(VertexBuffer, Vertices, effect);
    }

    public virtual void SetParameters(Effect effect, Camera camera, Texture2D texture)
    {
        effect.Parameters["ScreenTexture"]?.SetValue(texture);
        effect.Parameters["Time"]?.SetValue((float)TimeManager.CurrentScreenTime);
        effect.Parameters["NormalizedTime"]?.SetValue((float)TimeManager.CurrentScreenTime % Period);
        effect.Parameters["UVPerPixel"]?.SetValue(new Vector2(1f / camera.OrthogonalWidth, 1f / camera.OrthogonalHeight));
        effect.Parameters["Resolution"]?.SetValue(new Vector2(camera.OrthogonalWidth, camera.OrthogonalHeight));
    }

    void SetVertexData(Camera camera, VertexPositionColorNormalTexture[] vertices, float zDepth)
    {
        float internalAspectRatio = camera.OrthogonalWidth / camera.OrthogonalHeight;
        float externalAspectRatio = (float)FlatRedBallServices.GraphicsOptions.ResolutionWidth / FlatRedBallServices.GraphicsOptions.ResolutionHeight;

        float gameScreenWidth = 1;
        float gameScreenHeight = 1;

        if (internalAspectRatio < externalAspectRatio)
        {
            // gameScreenWidth = internalAspectRatio / externalAspectRatio;
        }

        if (externalAspectRatio < internalAspectRatio)
        {
            // gameScreenHeight = externalAspectRatio / internalAspectRatio;
        }

        vertices[0].Position = TopRight * new Vector3(gameScreenWidth, gameScreenHeight, 1f); // Top-Right
        vertices[1].Position = BottomRight * new Vector3(gameScreenWidth, gameScreenHeight, 1f); // Bottom-Right
        vertices[2].Position = TopLeft * new Vector3(gameScreenWidth, gameScreenHeight, 1f); // Top-Left
        vertices[3].Position = BottomLeft * new Vector3(gameScreenWidth, gameScreenHeight, 1f); // Bottom-Left

        vertices[0].Normal = new Vector3(camera.AbsoluteRightXEdge, camera.AbsoluteTopYEdge, zDepth); // Top-Right
        vertices[1].Normal = new Vector3(camera.AbsoluteRightXEdge, camera.AbsoluteBottomYEdge, zDepth); // Bottom-Right
        vertices[2].Normal = new Vector3(camera.AbsoluteLeftXEdge, camera.AbsoluteTopYEdge, zDepth); // Top-Left
        vertices[3].Normal = new Vector3(camera.AbsoluteLeftXEdge, camera.AbsoluteBottomYEdge, zDepth); // Bottom-Left
    }

    public virtual void DrawVertices(VertexBuffer vertexBuffer, VertexPositionColorNormalTexture[] vertices, Effect effect)
    {
        vertexBuffer.SetData(vertices);
        effect.CurrentTechnique.Passes[0].Apply();
        FlatRedBallServices.GraphicsDevice.SetVertexBuffer(vertexBuffer);
        FlatRedBallServices.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, vertices.Length - 2);
    }

    public void SetPositionByIndex(int rows = 1, int columns = 1, int index = 0, float zDepth = 0)
    {
        float width = 2f / columns;
        float height = -2f / rows;
        int x = index % columns;
        int y = index / columns;
        
        TopRight = new Vector3(-1 + (x + 1) * width, 1 + y * height, zDepth);
        BottomRight = new Vector3(-1 + (x + 1) * width, 1 + (y + 1) * height, zDepth);
        TopLeft = new Vector3(-1 + x * width, 1 + y * height, zDepth);
        BottomLeft = new Vector3(-1 + x * width, 1 + (y + 1) * height, zDepth);
    }

    public void SetPosition(float width = 1, float height = 1, float x = 0, float y = 0, float zDepth = 0)
    {
        width *= 2;
        height *= 2;
        
        TopRight = new Vector3(width / 2 + x * width, height / 2 + y * height, zDepth);
        BottomRight = new Vector3(width / 2 + x * width, -height / 2 + y * height, zDepth);
        TopLeft = new Vector3(-width / 2 + x * width, height / 2 + y * height, zDepth);
        BottomLeft = new Vector3(-width / 2 + x * width, -height / 2 + y * height, zDepth);
    }
}
