using System;
using MasterInstaller.Components.Controls;

namespace MasterInstaller.Components.MainComponents.Introduction
{
    public class IntroductionComponent : ComponentBase
    {
        public IntroductionComponent()
        {
        }

        protected override BasePage CreateControl()
        {
            return new IntroControl();
        }
    }
}
