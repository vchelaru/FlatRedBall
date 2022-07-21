using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Managers
{
    internal class GenerateCodeCommands : GlueCommandsStateBase
    {
        public Task GenerateElementCodeAsync(GlueElement element) =>
            base.SendMethodCallToGame(
                new GenerateCodeCommandDto(),
                nameof(GenerateElementCodeAsync),
                GlueElementReference.From(element));

    }
}
