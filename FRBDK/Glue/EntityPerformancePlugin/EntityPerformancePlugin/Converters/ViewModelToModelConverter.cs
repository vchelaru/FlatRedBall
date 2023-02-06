using EntityPerformancePlugin.Models;
using EntityPerformancePlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.Converters
{
    public static class ViewModelToModelConverter
    {
        public static EntityManagementValues ToModel(MainViewModel viewModel)
        {
            if(viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }
            EntityManagementValues model = new EntityManagementValues();

            model.Name = viewModel.EntityName;

            model.PropertyManagementMode = viewModel.EntityManagementMode;

            model.SelectedProperties.AddRange(viewModel.EntityManagedProperties);

            foreach(var instanceViewModel in viewModel.Instances)
            {
                InstanceManagementValues instanceModel = new InstanceManagementValues();
                instanceModel.Name = instanceViewModel.Name;

                instanceModel.PropertyManagementMode = 
                    instanceViewModel.PropertyManagementMode;

                instanceModel.SelectedProperties.AddRange(
                    instanceViewModel.SelectedProperties);

                model.InstanceManagementValuesList.Add(
                    instanceModel);
            }

            return model;
        }
    }
}
