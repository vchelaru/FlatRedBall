using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers
{
    public class VSSolution
    {
        public List<string> ReferencedProjects
        {
            get;
            private set;
        } = new List<string>();

        public string FullFileName
        {
            get;
            set;
        }

        public static VSSolution FromFile(string fileName)
        {
            VSSolution toReturn = new VSSolution();

            toReturn.FullFileName = fileName;

            var lines = System.IO.File.ReadAllLines(fileName);

            foreach(var line in lines)
            {
                ParseLine(line, toReturn);
            }

            return toReturn;
        }

        private static void ParseLine(string line, VSSolution solution)
        {
            if(line.StartsWith("Project("))
            {
                ParseProject(line, solution);
            }
        }

        private static void ParseProject(string line, VSSolution solution)
        {
            var splitBySpaces = line.Split('"');

            var projectReference = splitBySpaces[5];

            solution.ReferencedProjects.Add(projectReference);

        }

        public override string ToString()
        {
            return FileManager.RemovePath(FullFileName);
        }
    }
}
