using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AesEncrypter
{
    public class CommandLineParameters
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public string EncryptionKey { get; set; }
        public string InitialVector { get; set; }
        public bool IsVerbose { get; set; }

        public bool IsDecrypting { get; set; }

        public static CommandLineParameters Create(string[] args)
        {
            CommandLineParameters toReturn = new CommandLineParameters();

            toReturn.Source = args[0];
            toReturn.Destination = args[1];
            toReturn.EncryptionKey = args[2];
            toReturn.InitialVector = args[3];

            if(args.Length > 4)
            {
                for(int i = 4; i < args.Length; i++)
                {
                    ProcessOptionalParameter(args[i], toReturn);
                }
            }

            return toReturn;
        }

        private static void ProcessOptionalParameter(string parameter, CommandLineParameters parametersObject)
        {
            switch (parameter.ToLowerInvariant())
            {
                case "encrypt":
                    parametersObject.IsDecrypting = false;
                    break;
                case "decrypt":
                    parametersObject.IsDecrypting = true;
                    break;
                case "-v":
                    parametersObject.IsVerbose = true;
                    break;
                default:
                    // do anything?
                    break;
            }
        }

        public static GeneralResponse ValidateArgs(string[] args)
        {
            GeneralResponse toReturn = new GeneralResponse();
            toReturn.Succeeded = true;

            if (args.Length < 4)
            {
                toReturn.Succeeded = false;

                string message = "This tool requires the following arguments: " +
                    "{source file} {destination file} {encryption key} {initial vector} {optional encrypt or decrypt}";

                toReturn.Message = message;
            }

            return toReturn;
        }

        
    }
}
