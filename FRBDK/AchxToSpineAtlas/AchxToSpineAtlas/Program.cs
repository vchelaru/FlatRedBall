using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using System.Drawing;
using System.Text;

namespace AchxToSpineAtlas
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.Error.WriteLine("Expected 2 arguments {input} {output}, but instead received " + args.Length + " arguments");
                return 1;
            }

            if (!System.IO.File.Exists(args[0]))
            {
                throw new FileNotFoundException(args[0]);
            }

            var animationChainListSave = AnimationChainListSave.FromFile(args[0]);

            string converted = ConvertToAtlasString(animationChainListSave, args[0], args[1]);

            System.IO.File.WriteAllText(args[1], converted);

            return 0;
        }

        private static string ConvertToAtlasString(AnimationChainListSave animationChainListSave, string achxLocation, string destinationLocation)
        {
            if(animationChainListSave.AnimationChains.Count == 0)
            {
                return string.Empty;
            }
            var stringBuilder = new StringBuilder();

            var firstFrame = animationChainListSave.AnimationChains.First(item => item.Frames.Count > 0).Frames[0];

            var texture = firstFrame.TextureName?.Replace("\\", "/");

            var achxDirectory = Path.GetDirectoryName(achxLocation);
            var textureFull = Path.Combine(achxLocation, texture);
            var destinationDirectory = Path.GetDirectoryName(destinationLocation);

            var relativeTexture = FlatRedBall.IO.FileManager.MakeRelative(textureFull, destinationDirectory);

            stringBuilder.AppendLine(relativeTexture);

            var size = GetImageSize(textureFull);

            stringBuilder.AppendLine($"size:{size.Width},{size.Height}");
            stringBuilder.AppendLine("pma:false");
            foreach(var animationChain in animationChainListSave.AnimationChains)
            {
                if(animationChain.Frames.Count == 0)
                {
                    continue;
                }


                if(animationChain.Frames.Count == 1)
                {
                    stringBuilder.AppendLine(animationChain.Name);
                    var frame = animationChain.Frames[0];
                    WriteFrameCooordinates(stringBuilder, frame);
                }
                else
                {
                    for(int i = 0; i <  animationChain.Frames.Count; i++)
                    {
                        var frame = animationChain.Frames[i];

                        stringBuilder.AppendLine(animationChain.Name + "_" + i);
                        WriteFrameCooordinates(stringBuilder, frame);
                    }
                }
            }

            return stringBuilder.ToString();
        }

        private static void WriteFrameCooordinates(StringBuilder stringBuilder, AnimationFrameSave frame)
        {
            var x = MathFunctions.RoundToInt(frame.LeftCoordinate);
            var y = MathFunctions.RoundToInt(frame.TopCoordinate);
            var width = MathFunctions.RoundToInt(frame.RightCoordinate - frame.LeftCoordinate);
            var height = MathFunctions.RoundToInt(frame.BottomCoordinate - frame.TopCoordinate);

            stringBuilder.AppendLine($"bounds:{x},{y},{width},{height}");
        }

        static Size GetImageSize(string filename)
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

    }
}
