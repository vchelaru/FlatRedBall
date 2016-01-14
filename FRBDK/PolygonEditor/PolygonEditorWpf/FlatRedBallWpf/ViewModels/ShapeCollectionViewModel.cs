using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Math.Geometry;

namespace PolygonEditor.ViewModels
{
    public class ShapeCollectionViewModel
    {
        string fileName;

        public ObservableCollection<AxisAlignedRectangleViewModel> Rectangles
        {
            get;
            private set;
        }

        public ObservableCollection<CircleViewModel> Circles
        {
            get;
            private set;
        }

        public ObservableCollection<PolygonViewModel> Polygons
        {
            get;
            private set;
        }

        public ShapeCollectionViewModel()
        {
            Rectangles = new ObservableCollection<AxisAlignedRectangleViewModel>();
            Circles = new ObservableCollection<CircleViewModel>();
            Polygons = new ObservableCollection<PolygonViewModel>();
        }

        public void Load(string absoluteFileName)
        {
            fileName = absoluteFileName;

            var shapeCollectionSave = ShapeCollectionSave.FromFile(absoluteFileName);

            SetFrom(shapeCollectionSave);
        }

        void SetFrom(ShapeCollectionSave shapeCollectionSave)
        {
            Clear();

            foreach(var saveObject in shapeCollectionSave.AxisAlignedRectangleSaves)
            {
                var runtime = saveObject.ToAxisAlignedRectangle();

                var vm = new AxisAlignedRectangleViewModel(runtime);

                Rectangles.Add(vm);
            }

            foreach (var saveObject in shapeCollectionSave.CircleSaves)
            {
                var runtime = saveObject.ToCircle();

                var vm = new CircleViewModel(runtime);

                Circles.Add(vm);
            }

            foreach(var saveObject in shapeCollectionSave.PolygonSaves)
            {
                var runtime = saveObject.ToPolygon();

                var vm = new PolygonViewModel(runtime);

                Polygons.Add(vm);
            }


        }

        private void Clear()
        {

        }
    }
}
