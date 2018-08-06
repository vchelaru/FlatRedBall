using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.RuntimeObjects.File
{
    public interface IRuntimeFileManager
    {
        void Activity(ICollection<object> allFileObjects);

        object TryCreateFile(ReferencedFileSave file, IElement container);

        void Destroy(ICollection<object> allFileObjects);

        bool TryDestroy(object runtimeFileObject, ICollection<object> allFileObjects);

        object TryGetCombinedObjectByName(string name);

        bool TryHandleRefreshFile(string fileName, List<object> allFileObjects);
    }
}
