using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.ViewModels
{
    public class IndividualFileAddDownloadViewModel : ViewModel
    {
        public long? TotalLength 
        {
            get => Get<long?>();
            set => Set(value);
        }

        public long DownloadedBytes
        {
            get => Get<long>();
            set => Set(value);
        }

        public string Url
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(DownloadedBytes))]
        [DependsOn(nameof(TotalLength))]
        [DependsOn(nameof(Url))]
        public string FullDetails
        {
            get
            {
                string strippedUrl = Url;
                if(!string.IsNullOrEmpty(Url))
                {
                    strippedUrl = FileManager.RemovePath(Url);
                }

                if(TotalLength == null)
                {
                    return 
                        $"{ToMem(DownloadedBytes)} {strippedUrl}";

                }
                else
                {
                    return 
                        $"{ToMem(DownloadedBytes)}/{ToMem(TotalLength)} {strippedUrl}";
                }
            }
        }

        public GeneralResponse DownloadResponse
        {
            get => Get<GeneralResponse>();
            set => Set(value);
        }

        public override string ToString() => FullDetails;

        static string ToMem(long? bytes)
        {
            if(bytes == null)
            {
                return "Unknown";
            }
            else if (bytes < 1024)
            {
                return $"{bytes}b";
            }
            else if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024}kb";
            }
            //else if(bytes < 1024L * 1024L * 1024L)
            {
                var mb = bytes / (1024 * 1024);

                var extraKb = bytes - (mb * 1024 * 1024);

                var hundredKb = extraKb / (100 * 1000);

                return $"{bytes / (1024 * 1024)}.{hundredKb:0}mb";
            }
        }

    }
}
