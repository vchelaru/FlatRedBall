using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using Newtonsoft.Json;

namespace FlatRedBall.IO
{
    public abstract class ProfileManagerBase<T> where T : class, INameable, new()
    {


        public virtual string SaveFileExtension { get; } = ".sav";

        public T Profile { get; private set; }

        public FilePath GetSaveLocationForName(string name)
        {

            return FileManager.UserApplicationDataForThisApplication + name + SaveFileExtension;
        }

        public virtual T LoadOrCreate(string name)
        {
            var loaded = false;

            FilePath filePath = null;

            if (!string.IsNullOrEmpty(name))
            {
                filePath = GetSaveLocationForName(name);
            }
            if (filePath?.Exists() == true)
            {
                try
                {
                    // this is decrypted on the fly using the SimpleCryptoService
                    var text = System.IO.File.ReadAllText(filePath.FullPath)
                        // todo:
                        //.Decrypt()
                        ;
                    Profile = JsonConvert.DeserializeObject<T>(text);

                    loaded = true;
                }
                catch
                {

                }
            }

            if (!loaded)
            {
                Profile = new T();
                Profile.Name = name;
            }


            return Profile;
        }


        public void SaveProfile(T profileToSave = default(T))
        {
            profileToSave = profileToSave ?? Profile;
            var name = profileToSave.Name;
            var text = JsonConvert.SerializeObject(profileToSave);

            // encrypt on the fly using the SimpleCryptoService
            // todo add this
            //var encrypted = text.Encrypt();
            var encrypted = text;
            var filePath = GetSaveLocationForName(name);
            System.IO.Directory.CreateDirectory(filePath.GetDirectoryContainingThis().FullPath);
            System.IO.File.WriteAllText(filePath.FullPath, encrypted);
        }

        public void DeleteProfile(string profileName)
        {
            var filePath = GetSaveLocationForName(profileName);

            if (filePath.Exists())
            {
                System.IO.File.Delete(filePath.FullPath);
            }
        }

        public List<string> GetProfileNames()
        {
            List<string> toReturn = new List<string>();

            var directory = FileManager.UserApplicationDataForThisApplication;

            if (System.IO.Directory.Exists(directory))
            {
                var files = System.IO.Directory.GetFiles(directory, $"*{SaveFileExtension}")
                    .Select(item => new FilePath(item))
                    .Select(item => item.NoPathNoExtension);
                toReturn.AddRange(files);
            }
            else
            {
                // do nothing
            }

            return toReturn;
        }
    }
}