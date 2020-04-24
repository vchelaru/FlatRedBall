using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace FlatRedBall.AnimationEditor.Models
{
    public class AppSettingsModel
    {
        public List<string> RecentFiles { get; set; } = new List<string>();

        public void AddFile(FilePath filePath)
        {
            RecentFiles.RemoveAll(item => new FilePath(item) == filePath);

            // now insert it at the front:
            RecentFiles.Insert(0,filePath.FullPath);

            while(RecentFiles.Count > 20)
            {
                RecentFiles.Remove(RecentFiles.Last());
            }
        }
    }
}
