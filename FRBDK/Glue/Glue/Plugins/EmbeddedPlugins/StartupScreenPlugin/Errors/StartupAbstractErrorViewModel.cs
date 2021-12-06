using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin.Errors
{
    class StartupAbstractErrorViewModel : ErrorViewModel
    {
        public ScreenSave Screen { get; private set; }

        public StartupAbstractErrorViewModel(ScreenSave screen)
        {
            Screen = screen;
            if(screen.GetStrippedName() == "GameScreen")
            {
                this.Details = $"GameScreen is abstract (is a base screen). Select a level (derived screen) as the startup screen.";
            }
            else
            {
                this.Details = $"{screen} is abstract (is a base screen). Select a derived screen as the startup screen.";
            }
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentScreenSave = Screen;
        }

        public override bool GetIfIsFixed()
        {
            // if startup screen changed
            if(GlueState.Self.CurrentGlueProject.StartUpScreen != Screen.Name)
            {
                return true;
            }

            // if the screen is no longer abstract
            if(!IsAbstract(Screen))
            {
                return true;
            }

            return false;
        }


        public static bool IsAbstract(ScreenSave screen) => screen.AllNamedObjects.Any(item => item.SetByDerived);

    }
}
