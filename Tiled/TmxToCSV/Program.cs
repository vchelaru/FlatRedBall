using System;
using System.IO;
using System.Reflection;
using TMXGlueLib;

namespace TmxToCSV
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: tmxtocsv.exe <input.tmx> <output.csv> [type=Tile|Layer|Map|Object] [layername=name]");
                return;
            }

            try
            {
                TmxToCsvCommandLineArgs parsedArgs = new TmxToCsvCommandLineArgs(args);

                if (parsedArgs.IsVerbose)
                {
                    Assembly assembly = Assembly.GetEntryAssembly();
                    AssemblyName assemblyName = assembly.GetName();
                    Version version = assemblyName.Version;
                    Console.WriteLine("TMX to CSV converter version " + version.ToString());
                    Console.WriteLine("Create columns from " + parsedArgs.PropertyType);
                }
                TiledMapSave tms = TiledMapSave.FromFile(parsedArgs.SourceTmx);
                
                // Convert once in case of any exceptions
                string csvstring = tms.ToCSVString(type: parsedArgs.PropertyType, layerName: parsedArgs.LayerName);

                System.IO.File.WriteAllText(parsedArgs.DestinationCsv, csvstring);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException is FileNotFoundException)
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

    }
}
