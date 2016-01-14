using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public interface IFileReferencer
    {
        bool UseGlobalContent
        {
            get;
            set;
        }

        ReferencedFileSave GetReferencedFileSave(string name);


        List<ReferencedFileSave> ReferencedFiles
        {
            get;
        }
    }

    
    public static class FileReferencerHelper
    {
        public static ReferencedFileSave GetReferencedFileSave(IFileReferencer instance, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Replace("\\", "/");
                foreach (ReferencedFileSave rfs in instance.ReferencedFiles)
                {
                    if (rfs.Name == name)
                    {
                        return rfs;
                    }
                }
            }
            return null;
        }
    }
}
