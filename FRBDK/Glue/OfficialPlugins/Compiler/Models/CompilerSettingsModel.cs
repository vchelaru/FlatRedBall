using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.Models
{
    public class CompilerSettingsModel
    {
        public bool GenerateGlueControlManagerCode { get; set; }

        public bool EmbedGameInGameTab { get; set; } = true;
        public int PortNumber { get; set; } = 8021;
        public bool ShowScreenBoundsWhenViewingEntities { get; set; } = true;
        public decimal GridSize { get; set; } = 32;
    }
}
