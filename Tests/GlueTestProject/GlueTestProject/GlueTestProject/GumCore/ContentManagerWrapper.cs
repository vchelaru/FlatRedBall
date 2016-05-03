using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum
{
    public class ContentManagerWrapper : RenderingLibrary.Content.IContentLoader
    {
        public string ContentManagerName
        {
            get;
            set;
        }
        public T TryGetCachedDisposable<T>(string contentName)
        {
            var contentManager = FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName);

            if (contentManager.IsAssetLoadedByName<T>(contentName))
            {
                return contentManager.Load<T>(contentName);
            }
            else
            {
                return default(T);
            }
        }

        public void AddDisposable(string contentName, IDisposable disposable)
        {
            var contentManager = FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName);

            contentManager.AddDisposable(contentName, disposable);
        }





        public T LoadContent<T>(string contentName)
        {
#if ANDROID || IOS
			contentName = contentName.ToLowerInvariant();
#endif
            return FlatRedBall.FlatRedBallServices.Load<T>(contentName, ContentManagerName);
        }
    }
}
