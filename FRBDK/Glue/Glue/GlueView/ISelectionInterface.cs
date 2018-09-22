using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.GlueView
{
    [DescriptionAttribute("SelectionInterface")]
    public interface ISelectionInterface
    {
        void LoadGluxFile(string file);


        void UnloadGlux();

        void RefreshGlueProject();

        void RefreshFile(string fileName);

        void ShowElement(string name);

        void HighlightElement(string name);

        void SetState(string name);

        void RefreshCurrentElement();

        void RefreshVariables();

        bool IsConnected();

        void ExecuteScript(string script);
    }
}
