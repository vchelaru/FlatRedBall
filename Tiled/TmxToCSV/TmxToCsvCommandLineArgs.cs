using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMXGlueLib;

namespace TmxToCSV
{
    public class TmxToCsvCommandLineArgs
    {
        public TMXGlueLib.TiledMapSave.CSVPropertyType PropertyType
        {
            get;
            set;
        }

        public bool IsVerbose
        {
            get;
            set;
        }

        public string SourceTmx
        {
            get;
            set;
        }

        public string DestinationCsv
        {
            get;
            set;
        }

        public string LayerName
        {
            get;
            set;
        }

        public TmxToCsvCommandLineArgs(string[] args)
        {
            SourceTmx = args[0];
            DestinationCsv = args[1];

            var type = TiledMapSave.CSVPropertyType.Tile;
            string layerName = null;
            if(args.Length >=3)
            {
                ParseOptionalCommandLineArgs(args, out type, out layerName);
            }

            PropertyType = type;
            LayerName = layerName;
        }


        private void ParseOptionalCommandLineArgs(string[] args, out TiledMapSave.CSVPropertyType type, out string layerName)
        {
            type = TiledMapSave.CSVPropertyType.Tile;
            layerName = null;
            if (args.Length >= 3)
            {
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
                            case "type":
                                const bool ignoreCase = true;
                                if (!Enum.TryParse(value, ignoreCase, out type))
                                {
                                    type = TiledMapSave.CSVPropertyType.Tile;
                                }
                                break;
                            case "layername":
                                layerName = value;
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
}
