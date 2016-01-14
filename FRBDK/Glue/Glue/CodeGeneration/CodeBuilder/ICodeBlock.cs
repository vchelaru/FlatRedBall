using System.Collections.Generic;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public interface ICodeBlock : ICode
    {
        ICodeBlock Parent { get; set; }

        List<ICode> PreCodeLines { get; }
        List<ICode> BodyCodeLines { get; }
        List<ICode> PostBodyCodeLines { get; }
        List<ICode> PostCodeLines { get; }
    }
}
