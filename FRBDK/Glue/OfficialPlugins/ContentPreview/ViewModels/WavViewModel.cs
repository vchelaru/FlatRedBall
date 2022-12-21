using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPreview.ViewModels
{
    internal class WavViewModel : ViewModel
    {
        public TimeSpan Duration
        {
            get => Get<TimeSpan>();
            set => Set(value);
        }

        [DependsOn(nameof(Duration))]
        public string DurationDescription
        {
            get
            {
                if(Duration.TotalSeconds > 0)
                {
                    if(Duration.TotalSeconds >= 60)
                    {
                        return "Duration " + 
                            Duration.Minutes.ToString("0") + ":" +
                            Duration.Seconds.ToString("00") + "." + 
                            Duration.Milliseconds.ToString("00");

                    }
                    else
                    {
                        return "Duration " +
                            Duration.Seconds.ToString("00") + "." +
                            Duration.Milliseconds.ToString("00") + " seconds";
                    }
                }
                else
                {
                    return String.Empty;
                }
            }
        }

    }
}
