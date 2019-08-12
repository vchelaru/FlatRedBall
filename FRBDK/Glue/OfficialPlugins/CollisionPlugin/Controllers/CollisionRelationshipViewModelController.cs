using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollisionRelationshipViewModelController
    {

        public static CollisionRelationshipViewModel CreateViewModel()
        {
            var viewModel = new CollisionRelationshipViewModel();
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;

            return viewModel;
        }

        public static void RefreshAvailableCollisionObjects(IElement element, CollisionRelationshipViewModel viewModel)
        {
            viewModel.FirstCollisionItemSource.Clear();
            viewModel.SecondCollisionItemSource.Clear();

            var namedObjects = element.AllNamedObjects.ToArray();

            List<string> names = new List<string>();

            // consider:
            // 1. Individual ICollidables
            // 2. Lists of ICollidables
            // 3. TileShapeCollections
            // 4. ShapeCollections

            foreach (var nos in namedObjects)
            {
                var nosElement = nos.GetReferencedElement();
                var nosAti = nos.GetAssetTypeInfo();

                var entity = nosElement as EntitySave;

                var shouldConsider = false;

                if (entity?.ImplementsICollidable == true)
                {
                    shouldConsider = true;
                }

                if (!shouldConsider)
                {
                    // See if it's a list of ICollidables
                    shouldConsider = nos != null &&
                        nos.SourceType == SourceType.FlatRedBallType &&
                        nos.SourceClassType == "PositionedObjectList<T>" &&
                        !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                        ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType)?.ImplementsICollidable == true;
                }

                if (!shouldConsider)
                {
                    shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                        "FlatRedBall.TileCollisions.TileShapeCollection";
                }

                if (!shouldConsider)
                {
                    shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                        "FlatRedBall.Math.Geometry.ShapeCollection";
                }

                if (shouldConsider)
                {
                    //names.Add(nos.InstanceName);
                    viewModel.FirstCollisionItemSource.Add(nos.InstanceName);
                    viewModel.SecondCollisionItemSource.Add(nos.InstanceName);

                }
            }
        }

        static bool CanHaveSubCollisions(NamedObjectSave nos)
        {
            EntitySave entity = null;
            if (nos != null)
            {
                return GetEntitySaveReferencedBy(nos)?.ImplementsICollidable == true;
            }
            return false;
        }

        static EntitySave GetEntitySaveReferencedBy(NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.Entity)
            {
                return ObjectFinder.Self.GetEntitySave(nos.SourceClassType);
            }

            // if it's a list, make sure it's a list of collidables:
            if (nos.IsList && !string.IsNullOrEmpty(nos.SourceClassGenericType))
            {
                var sourceClassGenericType = nos.SourceClassGenericType;

                var entitySave = ObjectFinder.Self.GetEntitySave(sourceClassGenericType);
                return entitySave;
            }
            return null;
        }

        static bool IsShape(NamedObjectSave nos)
        {
            if(nos.SourceType == SourceType.FlatRedBallType)
            {
                return
                    nos.SourceClassType == "AxisAlignedRectangle" ||
                    nos.SourceClassType == "Circle" ||
                    nos.SourceClassType == "Line" ||
                    nos.SourceClassType == "Polygon";
            }
            return false;
        }
        public static void RefreshSubcollisionObjects(IElement element, CollisionRelationshipViewModel viewModel)
        {
            var firstNos = element.GetNamedObject(viewModel.FirstCollisionName);
            var secondNos = element.GetNamedObject(viewModel.SecondCollisionName);


            var canFirstHaveSubCollisions = CanHaveSubCollisions(firstNos);
            var canSecondHaveSubCollisions = CanHaveSubCollisions(secondNos);

            viewModel.FirstSubCollisionEnabled = canFirstHaveSubCollisions;
            viewModel.SecondSubCollisionEnabled = canSecondHaveSubCollisions;

            if(canFirstHaveSubCollisions)
            {
                var firstEntity = GetEntitySaveReferencedBy(firstNos);

                var list = GetAvailableValues(firstEntity);

                var oldValue = viewModel.FirstSubCollisionSelectedItem;

                viewModel.FirstSubCollisionItemsSource.Clear();
                foreach(var item in list)
                {
                    viewModel.FirstSubCollisionItemsSource.Add(item);
                }

                if(list.Contains(oldValue))
                {
                    viewModel.FirstSubCollisionSelectedItem = oldValue;
                }
            }
            else
            {
                viewModel.FirstSubCollisionSelectedItem = null;
            }

            if (canSecondHaveSubCollisions)
            {
                var secondEntity = GetEntitySaveReferencedBy(secondNos);

                var list = GetAvailableValues(secondEntity);

                var oldValue = viewModel.SecondSubCollisionSelectedItem;

                viewModel.SecondSubCollisionItemsSource.Clear();
                foreach(var item in list)
                {
                    viewModel.SecondSubCollisionItemsSource.Add(item);
                }

                if(list.Contains(oldValue))
                {
                    viewModel.SecondSubCollisionSelectedItem = oldValue;
                }
            }
            else
            {
                viewModel.SecondSubCollisionSelectedItem = null;
            }
        }

        private static List<string> GetAvailableValues(EntitySave firstEntity)
        {
            List<string> availableValues = new List<string>();
            availableValues.Add(CollisionRelationshipViewModel.EntireObject);

            foreach (var nos in firstEntity.AllNamedObjects)
            {
                if (IsShape(nos))
                {
                    availableValues.Add(nos.InstanceName);
                }
            }
            return availableValues;
        }

        public static void HandleGlueObjectPropertyChanged(string changedMember, object oldValue)
        {
            var element = GlueState.Self.CurrentElement;
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            if (changedMember == nameof(NamedObjectSave.InstanceName) && namedObject != null)
            {
                var collisionRelationshipAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;
                var allNamedObjects = element.AllNamedObjects.ToArray();
                var collisionRelationships = allNamedObjects
                    .Where(item =>
                    {
                        TryFixSourceClassType(item);
                        return item.GetAssetTypeInfo() == collisionRelationshipAti;
                    })
                    .ToArray();


                var oldName = (string)oldValue;
                bool changedAny = false;
                var newName = namedObject.InstanceName;

                string GetFirstCollision(NamedObjectSave nos)
                {
                    return nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));
                }

                string GetSecondCollision(NamedObjectSave nos)
                {
                    return nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));
                }

                var withFirst = collisionRelationships
                    .Where(item => GetFirstCollision(item) == oldName)
                    .ToArray();

                foreach (var item in withFirst)
                {
                    item.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), newName);
                    changedAny = true;
                    GlueCommands.Self.PrintOutput($"Renaming {item.FieldName}.{oldName} to {item.FieldName}.{newName}");
                    TryFixSourceClassType(item);
                }

                var withSecond = collisionRelationships
                    .Where(item => GetSecondCollision(item) == oldName)
                    .ToArray();

                foreach (var item in withSecond)
                {
                    item.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), newName);
                    changedAny = true;
                    GlueCommands.Self.PrintOutput($"Renaming {item.FieldName}.{oldName} to {item.FieldName}.{newName}");
                    TryFixSourceClassType(item);
                }

                if (changedAny)
                {
                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
                }
            }
        }

        public static void TryFixSourceClassType(NamedObjectSave selectedNos)
        {

            if (selectedNos.SourceClassType == "CollisionRelationship" ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollisionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsListRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsListRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("CollisionRelationship<") == true)
            {
                selectedNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti
                    .QualifiedRuntimeTypeName.PlatformFunc(selectedNos);
            }
        }

        private static void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as CollisionRelationshipViewModel;
            var element = GlueState.Self.CurrentElement;
            switch (e.PropertyName)
            {
                case nameof(viewModel.FirstCollisionName):
                case nameof(viewModel.SecondCollisionName):
                    var nos = viewModel.GlueObject as NamedObjectSave;
                    CollisionRelationshipViewModelController.TryFixSourceClassType(nos);

                    RefreshSubcollisionObjects(element, viewModel);
                    break;
            }
        }

    }
}
