using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class ToolbarEntityAndStateViewModel : ViewModel
    {
        public ICommand ClickedCommand
        {
            get;
            private set;
        } 

        void RaiseClicked() => Clicked?.Invoke();

        public event Action Clicked;

        public ToolbarEntityAndStateViewModel() => ClickedCommand = new Command(RaiseClicked);

        public GlueElement GlueElement { get; set; }
        public StateSave StateSave { get; set; }
        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value) ;
        }
    }
}
