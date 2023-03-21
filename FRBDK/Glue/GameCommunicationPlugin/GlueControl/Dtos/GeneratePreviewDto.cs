using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.Dtos
{
    internal class GeneratePreviewDto
    {
        public string ImageFilePath { get; set; }
        public string Element { get; set; }
        public string CategoryName { get; set; }
        public string State { get; set; }
    }
}
