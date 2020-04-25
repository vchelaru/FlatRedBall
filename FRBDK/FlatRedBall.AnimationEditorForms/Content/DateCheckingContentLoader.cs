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
    public class DateCheckingContentLoader : IContentLoader
    {
        Dictionary<string, IDisposable> disposables = new Dictionary<string, IDisposable>();
        SystemManagers systemManagers;

        public DateCheckingContentLoader(SystemManagers systemManagers)
        {
            this.systemManagers = systemManagers;
        }

        public void AddDisposable(string contentName, IDisposable disposable)
        {
            if (disposables.ContainsKey(contentName))
            {
                throw new Exception("This item has already been added");
            }
            else
            {
                disposables.Add(contentName, disposable);
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
                return (T)(object)disposables[fileNameStandardized];
            }

            if (typeof(T) == typeof(Texture2D))
            {
                var texture = LoaderManager.Self.LoadTextureFromFile(
                    contentName, systemManagers);


                AddDisposable(contentName, texture);
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
                return (T)disposables[contentName];
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
