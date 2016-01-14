using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.AnimationEditorForms
{
    public class AnimationChainDisplayer : PropertyGridDisplayer
    {

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                base.Instance = value;
                UpdateShownProperties();
            }
        }

        private void UpdateShownProperties()
        {
            AnimationChainSave acs = new AnimationChainSave();
            ExcludeMember("ParentFile");
            ExcludeMember("Frames");
            ExcludeMember("ColorKey");
        }


    }
}
