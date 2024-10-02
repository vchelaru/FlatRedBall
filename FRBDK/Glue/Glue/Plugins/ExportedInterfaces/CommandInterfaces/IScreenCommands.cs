using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IScreenCommands
    {
        Task<ScreenSave> AddScreen(string screenName);

        Task AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage = false);

        Task<ScreenSave> AddScreenAsync(AddScreenViewModel viewModel);
    }
}
