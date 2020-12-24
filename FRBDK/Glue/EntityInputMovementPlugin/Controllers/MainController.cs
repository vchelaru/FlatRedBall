using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EntityInputMovementPlugin.Controllers
{
    public class MainController : Singleton<MainController>
    {
        ViewModels.MainViewModel viewModel;

        bool ignoresPropertyChanges = false;


        public ViewModels.MainViewModel GetViewModel()
        {
            if(viewModel == null)
            {
                viewModel = new ViewModels.MainViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }

            return viewModel;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /////////// early out ///////////
            if (ignoresPropertyChanges)
            {
                return;
            }
            ///////////// end early out ///////////


        }
    }
}
