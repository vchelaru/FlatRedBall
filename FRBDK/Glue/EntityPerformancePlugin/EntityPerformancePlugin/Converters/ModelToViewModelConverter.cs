using EntityPerformancePlugin.Models;
using EntityPerformancePlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.Converters
{
    public static class ModelToViewModelConverter
    {
        public static MainViewModel ToViewModel(EntityManagementValues model)
        {
            MainViewModel viewModel = new MainViewModel();

            viewModel.Entity = FlatRedBall.Glue.Elements.ObjectFinder.Self.GetEntitySave(model.Name);


            viewModel.EntityManagementMode = model.PropertyManagementMode;

            foreach(var propertyName in model.SelectedProperties)
            {
                viewModel.EntityManagedProperties.Add(propertyName);
            }

            foreach(var instanceModel in model.InstanceManagementValuesList)
            {
                var instanceViewModel = new InstanceViewModel();
                instanceViewModel.Name = instanceModel.Name;

                instanceViewModel.PropertyManagementMode = instanceModel.PropertyManagementMode;

                instanceViewModel.SelectedProperties.AddRange(instanceModel.SelectedProperties);

                viewModel.Instances.Add(instanceViewModel);
            }

            return viewModel;
        }
    }
}
