using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace FlatRedBall.AnimationEditorForms.Content
{
    class DateReferencingContent
    {
        public DateTime? LastWriteTime { get; set; }
        public IDisposable Disposable { get; set; }
    }

    public class DateCheckingContentLoader : IContentLoader
    {
        Dictionary<string, DateReferencingContent> disposables = new Dictionary<string, DateReferencingContent>();
        SystemManagers systemManagers;

        public DateCheckingContentLoader(SystemManagers systemManagers)
        {
            this.systemManagers = systemManagers;
        }

        public void AddDisposable(string contentName, IDisposable disposable)
        {
            AddDisposable(contentName, disposable, null);
        }

        public void AddDisposable(string contentName, IDisposable disposable, DateTime? lastWriteTime)
        {
            if (disposables.ContainsKey(contentName))
            {
                throw new Exception("This item has already been added");
            }
            else
            {
                var content = new DateReferencingContent();
                content.Disposable = disposable;
                content.LastWriteTime = lastWriteTime;
                disposables.Add(contentName, content);
            }
        }

        public T LoadContent<T>(string contentName)
        {
            string fileNameStandardized = FileManager.Standardize(contentName, false, false);

            if (FileManager.IsRelative(fileNameStandardized))
            {
                fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

                fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
            }

            if(disposables.ContainsKey(fileNameStandardized))
            {
                var lastFileWriteTime = System.IO.File.GetLastWriteTime(fileNameStandardized);

                var disposableContent = disposables[fileNameStandardized];

                var isOutOfDate = lastFileWriteTime > disposableContent.LastWriteTime;

                if(!isOutOfDate)
                {
                    return (T)(object)(disposableContent.Disposable);
                }
                else
                {
                    disposableContent.Disposable.Dispose();
                    disposables.Remove(fileNameStandardized);
                }
            }

            if (typeof(T) == typeof(Texture2D))
            {
                var texture = LoaderManager.Self.LoadTextureFromFile(
                    fileNameStandardized, systemManagers);

                var lastWriteTime = System.IO.File.GetLastWriteTime(fileNameStandardized);

                AddDisposable(fileNameStandardized, texture, lastWriteTime);
                return (T)(object)texture;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public T TryGetCachedDisposable<T>(string contentName)
        {
            if (disposables.ContainsKey(contentName))
            {
                return (T)disposables[contentName].Disposable;
            }
            else
            {
                return default(T);
            }
        }

        public T TryLoadContent<T>(string contentName)
        {
            try
            {
                return LoadContent<T>(contentName);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
