using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    public class ProfilingManager : Singleton<ProfilingManager>
    {
        /// <summary>
        /// Assignable rather than set only by <see cref="Initialize"/>, which also starts the polling
        /// timer - a test wants the refresh logic without a WPF dispatcher running.
        /// </summary>
        internal ProfilingControlViewModel ProfilingViewModel { get; set; }
        CompilerViewModel CompilerViewModel;

        CommandSending.CommandSender commandSender;
        /// <summary>
        /// Defaults to the process-wide sender; assignable so a test can point this manager at its own
        /// fake game instead of mutating the singleton.
        /// </summary>
        internal CommandSending.CommandSender CommandSender
        {
            get => commandSender ?? CommandSending.CommandSender.Self;
            set => commandSender = value;
        }

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
                await RefreshProfilingData();
            }
        }

        public async Task RefreshProfilingData()
        {
            var dto = new Dtos.GetProfilingDataDto();

            dto.IsTimestepDisabled = ProfilingViewModel.IsDisableFixedTimestepChecked;

            GeneralResponse<ProfilingDataDto> response;
            if(CommandSender.GlueViewSettingsViewModel.EnableLiveEdit == false)
            {
                response = GeneralResponse<ProfilingDataDto>.UnsuccessfulWith("Live edit is disabled - enable it from the Editor Settings tab to receive profiling information from the game");
            }

            else
            {
                response = await CommandSender.Send<Dtos.ProfilingDataDto>(dto, CommandSending.SendImportance.IfNotBusy );
            }

            if (response.Succeeded)
            {
                // Succeeded no longer implies data - a command the game dropped is reported as a failure
                // now - but a handler can still legitimately answer with a JSON null.
                if(response.Data != null)
                {
                    ProfilingViewModel.SummaryText = response.Data.SummaryData;
                    //ProfilingViewModel.CollisionText = response.Data.CollisionData;
                    string text = "";

                    var totalCollisionCount = response.Data.CollisionData.Sum(item => item.DeepCollisions);
                    text += $"Total Collisions: {totalCollisionCount}\n\n";

                    var ordered = response.Data.CollisionData.OrderByDescending(item => item.DeepCollisions).Where(item => item.DeepCollisions > 0).ToArray();

                    foreach (var item in ordered)
                    {
                        string itemCountString = null;
                        if(item.FirstItemListCount != null && item.SecondItemListCount != null)
                        {
                            itemCountString = $" {item.FirstItemListCount} vs {item.SecondItemListCount}";
                        }
                        // This could happen if dealing with a List vs. TileShapeCollection:
                        else if(item.FirstItemListCount != null)
                        {
                            itemCountString = $" {item.FirstItemListCount}";
                        }

                        string partitionText = null;
                        if(item.IsPartitioned == false)
                        {
                            partitionText = " (not partitioned)";
                        }
                        else if(item.FirstPartitionAxis != item.SecondPartitionAxis && item.FirstPartitionAxis != null && item.SecondPartitionAxis != null)
                        {
                            partitionText = $" (partition axis mismatch {item.FirstPartitionAxis} vs {item.SecondPartitionAxis})";
                        }

                        text += $"{item.DeepCollisions} - {item.RelationshipName}{itemCountString}{partitionText}\n";
                    }

                    var itemsWith0 = response.Data.CollisionData.Count() - ordered.Length;

                    if(itemsWith0 > 0)
                    {
                        text += $"{itemsWith0} relationship(s) with 0 deep collisions";
                    }

                    ProfilingViewModel.CollisionText = text;
                }
            }
            else
            {
                ProfilingViewModel.SummaryText = response.Message;
                ProfilingViewModel.CollisionText = response.Message;

            }
        }
    }
}
