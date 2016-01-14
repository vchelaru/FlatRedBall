using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game1Copier
{
    class FileManager
    {
        public static string GetStringFromEmbeddedResource(Assembly assembly, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new NullReferenceException("ResourceName must not be null - can't get the byte array for resource");
            }

            if (assembly == null)
            {
                throw new NullReferenceException("Assembly is null, so can't find the resource\n" + resourceName);
            }

            string toReturn = null;

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {

                if (resourceStream == null)
                {
                    string message = "Could not find a resource stream for\n" + resourceName + "\n but found " +
                        "the following names:\n\n";

                    foreach (string containedResource in assembly.GetManifestResourceNames())
                    {
                        message += containedResource + "\n";
                    }


                    throw new NullReferenceException(message);
                }

                using (var streamReader = new StreamReader(resourceStream))
                {
                    toReturn = streamReader.ReadToEnd();
                }
            }
            return toReturn;
        }



    }
}
