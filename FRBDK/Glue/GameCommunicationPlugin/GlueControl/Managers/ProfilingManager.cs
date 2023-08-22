using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    public class ProfilingManager : Singleton<ProfilingManager>
    {
        ProfilingControlViewModel ProfilingViewModel;
        CompilerViewModel CompilerViewModel;
        public void Initialize(ProfilingControlViewModel profilingViewModel, CompilerViewModel compilerViewModel)
        {
            ProfilingViewModel = profilingViewModel;
            CompilerViewModel = compilerViewModel;
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += HandleTick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Start();
        }

        private async void HandleTick(object sender, EventArgs e)
        {
            if(ProfilingViewModel.IsAutoSnapshotEnabled && CompilerViewModel.IsRunning && 
                GlueState.Self.CurrentGlueProject != null)
            {
                var dto = new Dtos.GetProfilingDataDto();

                var response = await CommandSending.CommandSender.Self.Send<Dtos.ProfilingDataDto>(dto);

                if (response.Succeeded)
                {
                    ProfilingViewModel.SummaryText = response.Data.SummaryData;
                    ProfilingViewModel.CollisionText = response.Data.CollisionData;
                }
            }
        }
    }
}
