using System.Collections.Generic;
using System;

namespace FlatRedBall.Graphics.Texture
{
    public class Atlas : IDisposable
    {
        public bool IsDisposed
        {
            get;
            private set;
        }

        private readonly Dictionary<string, AtlasedTexture> spriteList;

        public Atlas()
        {
            spriteList = new Dictionary<string, AtlasedTexture>();
        }

        public void Add(string name, AtlasedTexture sprite)
        {
            spriteList.Add(name, sprite);
        }
        

        public bool Contains(string name, bool ignoreCase)
        {
            if (ignoreCase)
            {
                // Do a fast search first:
                if(this.spriteList.ContainsKey(name))
                {
                    return true;
                }

                var nameLower = name.ToLowerInvariant().Replace("\\", "/");

                // do the slow search now:
                foreach(var item in spriteList)
                {
                    string keyLower = item.Key.ToLowerInvariant().Replace("\\", "/");

                    // The first folder will be the project name + "Content"
                    // We want to replace that with "content"
                    var firstSlash = keyLower.IndexOf("/");
                    if(firstSlash != -1 && firstSlash != keyLower.Length-1)
                    {
                        keyLower = "content/" + keyLower.Substring(firstSlash + 1);
                    }

                    if (keyLower == nameLower)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return this.spriteList.ContainsKey(name);
            }
        }

        public AtlasedTexture Sprite(string name, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                // Do a fast search first:
                if (this.spriteList.ContainsKey(name))
                {
                    return spriteList[name];
                }

                var nameLower = name.ToLowerInvariant().Replace("\\", "/");

                // do the slow search now:
                foreach (var item in spriteList)
                {
                    string keyLower = item.Key.ToLowerInvariant().Replace("\\", "/");

                    // The first folder will be the project name + "Content"
                    // We want to replace that with "content"
                    var firstSlash = keyLower.IndexOf("/");
                    if (firstSlash != -1 && firstSlash != keyLower.Length - 1)
                    {
                        keyLower = "content/" + keyLower.Substring(firstSlash + 1);
                    }

                    if (keyLower == nameLower)
                    {
                        return item.Value;
                    }
                }

                return null;
            }
            else
            {
                return this.spriteList[name];
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}