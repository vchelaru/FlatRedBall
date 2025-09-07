using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;

namespace SpineAtlasLibrary;
public class AtlasConverter
{
    public string SerializeAtlas(AnimationChainListSave animationChainListSave, string destinationLocation)
    {
        if (animationChainListSave.AnimationChains.Count == 0)
        {
            return string.Empty;
        }
        var stringBuilder = new StringBuilder();

        var firstFrame = animationChainListSave.AnimationChains.FirstOrDefault(item => item.Frames.Count > 0)?.Frames.FirstOrDefault();

        var texture = firstFrame?.TextureName?.Replace("\\", "/");

        string textureFull = texture != null ? Path.Combine(destinationLocation, texture) : string.Empty;
        var destinationDirectory = Path.GetDirectoryName(destinationLocation);

        var relativeTexture = FlatRedBall.IO.FileManager.MakeRelative(textureFull, destinationDirectory);

        stringBuilder.AppendLine(relativeTexture);

        var size = GetImageSize(textureFull);

        stringBuilder.AppendLine($"size:{size.Width},{size.Height}");
        stringBuilder.AppendLine("pma:false");
        foreach (var animationChain in animationChainListSave.AnimationChains)
        {
            if (animationChain.Frames.Count == 0)
            {
                // we still want to save the atlas so that the file can be saved
                stringBuilder.AppendLine(animationChain.Name);
                // Well inject some bogus animation frame:
                WriteFrameCooordinates(
                    stringBuilder,
                    new AnimationFrameSave
                    {
                        RightCoordinate = 32,
                        BottomCoordinate = 32
                    });
                continue;
            }


            else if (animationChain.Frames.Count == 1)
            {
                stringBuilder.AppendLine(animationChain.Name);
                var frame = animationChain.Frames[0];

                WarnIfFrameTextureDiffers(frame, texture);

                WriteFrameCooordinates(stringBuilder, frame);
            }
            else
            {
                for (int i = 0; i < animationChain.Frames.Count; i++)
                {
                    var frame = animationChain.Frames[i];

                    WarnIfFrameTextureDiffers(frame, texture);

                    stringBuilder.AppendLine(animationChain.Name + "_" + i);
                    WriteFrameCooordinates(stringBuilder, frame);
                }
            }
        }

        return stringBuilder.ToString();
    }

    private void WarnIfFrameTextureDiffers(AnimationFrameSave frame, string texture)
    {
        var frameTexture = frame.TextureName?.Replace("\\", "/");

        if (frameTexture != texture)
        {
            Console.WriteLine($"warning : animation frame uses different texture from first: {frameTexture}");
        }
    }

    static Size GetImageSize(string filename)
    {
        if(System.IO.File.Exists(filename))
        {
            try
            {

                using FileStream fileStream = File.OpenRead(filename);
                using BinaryReader br = new BinaryReader(fileStream);
                br.BaseStream.Position = 16;
                byte[] widthbytes = new byte[sizeof(int)];
                for (int i = 0; i < sizeof(int); i++) widthbytes[sizeof(int) - 1 - i] = br.ReadByte();
                int width = BitConverter.ToInt32(widthbytes, 0);
                byte[] heightbytes = new byte[sizeof(int)];
                for (int i = 0; i < sizeof(int); i++) heightbytes[sizeof(int) - 1 - i] = br.ReadByte();
                int height = BitConverter.ToInt32(heightbytes, 0);
                return new Size(width, height);
            }
            catch
            {
                // do nothing, let the default happen
            }
        }

        return new Size(32, 32);
    }

    private void WriteFrameCooordinates(StringBuilder stringBuilder, AnimationFrameSave frame)
    {
        var x = MathFunctions.RoundToInt(frame.LeftCoordinate);
        var y = MathFunctions.RoundToInt(frame.TopCoordinate);
        var width = MathFunctions.RoundToInt(frame.RightCoordinate - frame.LeftCoordinate);
        var height = MathFunctions.RoundToInt(frame.BottomCoordinate - frame.TopCoordinate);

        stringBuilder.AppendLine($"bounds:{x},{y},{width},{height}");
    }

    public AnimationChainListSave? DeserializeAtlas(string contents)
    {
        var lines = contents.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return null;
        }
        var texture = lines[0];
        var sizeLine = lines[1];
        if (!sizeLine.StartsWith("size:"))
        {
            return null;
        }
        var sizeParts = sizeLine.Substring("size:".Length).Split(',');
        if (sizeParts.Length != 2)
        {
            return null;
        }
        if (!int.TryParse(sizeParts[0], out int textureWidth))
        {
            return null;
        }
        if (!int.TryParse(sizeParts[1], out int textureHeight))
        {
            return null;
        }
        var animationChainListSave = new AnimationChainListSave();
        AnimationChainSave? currentAnimationChain = null;
        for (int i = 2; i < lines.Length; i++)
        {
            var line = lines[i];
            if(line.StartsWith("pma:"))
            {
                continue;
            }
            else if (!line.StartsWith("bounds:"))
            {
                // This is a new animation chain
                currentAnimationChain = new AnimationChainSave();
                currentAnimationChain.Name = line;
                animationChainListSave.AnimationChains.Add(currentAnimationChain);
            }
            else
            {
                if (currentAnimationChain == null)
                {
                    // Error - bounds without a name
                    return null;
                }
                var boundsPart = line.Substring("bounds:".Length);
                var boundsParts = boundsPart.Split(',');
                if (boundsParts.Length != 4)
                {
                    return null;
                }
                if (!int.TryParse(boundsParts[0], out int x))
                {
                    return null;
                }
                if (!int.TryParse(boundsParts[1], out int y))
                {
                    return null;
                }
                if (!int.TryParse(boundsParts[2], out int width))
                {
                    return null;
                }
                if (!int.TryParse(boundsParts[3], out int height))
                {
                    return null;
                }
                var frame = new AnimationFrameSave();
                frame.TextureName = texture;
                frame.LeftCoordinate = x;
                frame.TopCoordinate = y;
                frame.RightCoordinate = x + width;
                frame.BottomCoordinate = y + height;
                currentAnimationChain.Frames.Add(frame);
            }
        }
        return animationChainListSave;
    }
}
