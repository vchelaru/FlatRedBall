using System.Collections.Generic;
using System.Linq;
using FilePath = FlatRedBall.IO.FilePath;

namespace AnimationEditor.Core.Models
{
    public class AppSettingsModel
    {
        public List<string> RecentFiles { get; set; } = new List<string>();

        public void AddFile(FilePath filePath)
        {
            RecentFiles.RemoveAll(item => new FilePath(item) == filePath);
            RecentFiles.Insert(0, filePath.FullPath);
            while (RecentFiles.Count > 20)
            {
                RecentFiles.Remove(RecentFiles.Last());
            }
        }
    }
}
