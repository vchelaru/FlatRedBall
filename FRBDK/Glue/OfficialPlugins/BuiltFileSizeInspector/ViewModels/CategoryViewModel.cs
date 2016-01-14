using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;

namespace OfficialPlugins.BuiltFileSizeInspector.ViewModels
{
    public class CategoryViewModel : ViewModel
    {

        long cachedSizeInBytes;

        public string Name
        {
            get;
            set;
        }
        
        public long ContainingZipSize 
        { 
            set
            {
                if (value == 0)
                {
                    PercentageDisplay = "Unknown";

                }
                else
                {
                    var percentage = 100m * SizeInBytes / (decimal)value;

                    PercentageDisplay = percentage.ToString("0.00") + "%";

                    NotifyPropertyChanged("PercentageDisplay");
                }
            }
        }

        public long SizeInBytes
        {
            get
            {
                return cachedSizeInBytes;
            }
        }

        public string DisplayedSize
        {
            get
            {
                if(SizeInBytes > 1024)
                {
                    return (SizeInBytes / 1024m).ToString("0.0") + "kb";
                }
                else
                {
                    return SizeInBytes.ToString();
                }
            }
        }

        public string PercentageDisplay
        {
            get;
            set;
        }

        public ObservableCollection<FileViewModel> Files
        {
            get;
            set;
        }

        public CategoryViewModel()
        {
            Files = new ObservableCollection<FileViewModel>();
        }



        public void RecalculateEverything()
        {
            cachedSizeInBytes = Files.Sum(item => item.SizeInBytes);

            NotifyPropertyChanged("SizeInBytes");
            NotifyPropertyChanged("DisplayedSize");
        }

    }
}
