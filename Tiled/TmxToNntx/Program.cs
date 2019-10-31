using System;
using FlatRedBall.Content.AI.Pathfinding;
using TMXGlueLib;

namespace TmxToNntx
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: tmxtonntx.exe <input.tmx> <output.csv> [requiretile=true|false] [layervisibilitybehavior=Ignore|Skip|Match] [offset=xf,yf,zf]");
                return;
            }

            try
            {
                string sourceTmx = args[0];
                string destinationNntx = args[1];
                bool requiretile = true;
                var layerVisibilityBehavior = TiledMapSave.LayerVisibleBehavior.Ignore;
                var offset = new Tuple<float, float, float>(0f, 0f, 0f);

                if (args.Length >= 3)
                {
                    ParseOptionalCommandLineArgs(args, out requiretile, out layerVisibilityBehavior, out offset);
                }

                TiledMapSave.LayerVisibleBehaviorValue = layerVisibilityBehavior;
                TiledMapSave.Offset = offset;
                TiledMapSave tms = TiledMapSave.FromFile(sourceTmx);
                // Convert once in case of any exceptions
                NodeNetworkSave save = tms.ToNodeNetworkSave(requiretile);

                save.Save(destinationNntx);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException is System.IO.FileNotFoundException)
                {
                    var exception = ex.InnerException;
                    Console.Error.WriteLine("Error: [" + exception.Message + "] Stack trace: [" + exception.StackTrace + "]");
                }
                else
                {
                    Console.Error.WriteLine("Error: [" + ex.Message + "] Stack trace: [" + ex.StackTrace + "]");
                }
            }
        }

        private static void ParseOptionalCommandLineArgs(string[] args, out bool requiretile, out TiledMapSave.LayerVisibleBehavior layerVisibleBehavior, out Tuple<float, float, float> offset)
        {
            requiretile = true;
            layerVisibleBehavior = TiledMapSave.LayerVisibleBehavior.Ignore;
            offset = new Tuple<float, float, float>(0f, 0f, 0f);
            for (int x = 2; x < args.Length; ++x)
            {
                string arg = args[x];
                string[] tokens = arg.Split("=".ToCharArray());
                if (tokens.Length == 2)
                {
                    string key = tokens[0];
                    string value = tokens[1];

                    switch (key.ToLowerInvariant())
                    {
                        case "requiretile":
                            if (!bool.TryParse(value, out requiretile))
                            {
                                requiretile = true;
                            }
                            break;
                        case "layervisiblebehavior":
                        case "layervisibilitybehavior":
                            if (!Enum.TryParse(value, out layerVisibleBehavior))
                            {
                                layerVisibleBehavior = TiledMapSave.LayerVisibleBehavior.Ignore;
                            }
                            break;
                        case "offset":
                            string[] tupleVals = value.Split(",".ToCharArray());
                            if (tupleVals.Length == 3)
                            {
                                float xf, yf, zf;
                                if (float.TryParse(tupleVals[0], out xf) && float.TryParse(tupleVals[1], out yf) &&
                                    float.TryParse(tupleVals[2], out zf))
                                {
                                    offset = new Tuple<float, float, float>(xf, yf, zf);
                                }
                            }
                            break;
                        default:
                            Console.Error.WriteLine("Invalid command line argument: {0}", arg);
                            break;
                    }
                }
            }
        }

    }
}
