using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.IO;
using Ionic.Zip;

namespace OfficialPlugins.BuiltFileSizeInspector.ViewModels
{
    public class BuiltFileSizeViewModel
    {
        public ObservableCollection<CategoryViewModel> Categories
        {
            get;
            private set;
        }

        public BuiltFileSizeViewModel()
        {
            Categories = new ObservableCollection<CategoryViewModel>();
        }

        public void SetFromFile(string fileName)
        {
            Categories.Clear();

            ObservableCollection<CategoryViewModel> tempList = new ObservableCollection<CategoryViewModel>();

            using (ZipFile zip = ZipFile.Read(fileName))
            {
                foreach(var entry in zip.OrderByDescending(item=>item.CompressedSize))
                {
                    string category = GetCategoryForFile(entry.FileName);
                    var categoryVm = GetOrCreateCategoryFor(category, tempList);

                    FileViewModel fileVm = new FileViewModel();
                    fileVm.Name = entry.FileName;
                    fileVm.SizeInBytes = entry.CompressedSize;

                    categoryVm.Files.Add(fileVm);
                }
            }

            foreach (var category in tempList)
            {
                category.RecalculateEverything();
            }

            var totalSize = tempList.Sum(item => item.SizeInBytes);

            foreach (var category in tempList)
            {
                category.ContainingZipSize = totalSize;
            }

            foreach(var category in tempList.OrderByDescending(item=>item.SizeInBytes))
            {
                Categories.Add(category);
            }

        }

        private CategoryViewModel GetOrCreateCategoryFor(string category, ObservableCollection<CategoryViewModel> list)
        {
            var found = list.FirstOrDefault(item => item.Name == category);

            if(found == null)
            {
                found = new CategoryViewModel();
                found.Name = category;

                list.Add(found);
            }

            return found;
        }

        private string GetCategoryForFile(string fileName)
        {
            var extension = FileManager.GetExtension(fileName);

            switch(extension)
            {
                case "png":
                case "jpg":
                case "jpeg":
                    return "Image";
                case "csv":
                    return "CSV";
                case "achx":
                    return "Animation Chains";
                case "m4a":
                    return "Audio";
                case "xnb":
                    return "General Content";
                case "dll":
                    return "Code DLL";
                default:
                    return "Other";
            }
        }
    }
}
