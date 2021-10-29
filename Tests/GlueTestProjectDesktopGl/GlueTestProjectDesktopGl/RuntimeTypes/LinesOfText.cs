using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;

namespace ProjectNamespace.RuntimeTypes
{
    public class LinesOfText
    {
        public List<string> Lines = new List<string>();

        public static LinesOfText FromFile(string fileName)
        {
            string entireText = FileManager.FromFileText(fileName);
            LinesOfText toReturn = new LinesOfText();
            toReturn.Lines.AddRange(entireText.Split('\n'));
            return toReturn;
        }
    }
}