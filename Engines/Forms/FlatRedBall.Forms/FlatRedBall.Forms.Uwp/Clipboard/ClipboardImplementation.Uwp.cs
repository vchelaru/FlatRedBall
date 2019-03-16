using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace FlatRedBall.Forms.Clipboard
{
    class ClipboardImplementation
    {
        
        internal static void PushStringToClipboard(string whatToCopy)
        {
            // not implemented
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(whatToCopy);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        [STAThread]
        internal static string GetText()
        {
            string toReturn = null;
            DataPackageView dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                toReturn = Task.Run(async () => await dataPackageView.GetTextAsync()).Result;
            }

            return toReturn;
        }
    }
}
