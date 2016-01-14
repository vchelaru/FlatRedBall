using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;

namespace OfficialPlugins.BuiltFileSizeInspector.ViewModels
{
    public class FileViewModel : ViewModel
    {

        public string Name { get; set; }
        
        long sizeInBytes;
        public long SizeInBytes
        {
            get { return sizeInBytes; }
            set
            {
                base.ChangeAndNotify(ref sizeInBytes, value, "SizeInBytes");
                base.NotifyPropertyChanged("DisplayedSize");
            }
        }

        public string DisplayedSize
        {
            get
            {
                if (SizeInBytes > 1024)
                {
                    return (SizeInBytes / 1024m).ToString("0.0") + "kb";
                }
                else
                {
                    return SizeInBytes.ToString();
                }
            }
        }
    }
}
