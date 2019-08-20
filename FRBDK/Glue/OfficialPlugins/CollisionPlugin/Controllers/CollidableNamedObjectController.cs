using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollidableNamedObjectController
    {
        static CollidableNamedObjectRelationshipViewModel ViewModel;

        public static void RegisterViewModel(CollidableNamedObjectRelationshipViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public static string FirstCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));
        }

        public static string SecondCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.SecondCollisionName));
        }

        public static void RefreshViewModelTo(IElement container,
            NamedObjectSave thisNamedObject,
            CollidableNamedObjectRelationshipViewModel viewModel)
        {
            viewModel.CollisionRelationshipsTitle =
                $"{thisNamedObject.InstanceName} Collision Relationships";

            var isSingleEntity = thisNamedObject.IsList == false && thisNamedObject.SourceType == SourceType.Entity;
            var isTileShapeCollection = thisNamedObject.SourceClassType ==
                "FlatRedBall.TileCollisions.TileShapeCollection" ||
                thisNamedObject.SourceClassType == "TileShapeCollection";
            List<NamedObjectSave> collidables;
            
            if(isTileShapeCollection)
            {
                // only against collidables:
                collidables = container.AllNamedObjects
                    .Where(item =>
                    {
                        var entity = CollisionRelationshipViewModelController.GetEntitySaveReferencedBy(item);
                        return entity?.ImplementsICollidable == true;
                    })
                    .ToList();
            }
            else
            {
                collidables = container.AllNamedObjects
                    .Where(item =>
                    {
                        return CollisionRelationshipViewModelController.GetIfCanBeReferencedByRelationship(item);
                    })
                    .ToList();
            }

            if(isSingleEntity)
            {
                // don't let this be against itself
                if(collidables.Contains(thisNamedObject))
                {
                    collidables.Remove(thisNamedObject);
                }
            }

            var relationships = container.AllNamedObjects
                .Where(item =>
                {
                    return item.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti;
                })
                .ToArray();



            viewModel.NamedObjectPairs.Clear();

            var name1 = thisNamedObject.InstanceName;

            foreach(var collidable in collidables)
            {
                var name2 = collidable.InstanceName;

                var pairViewModel = new NamedObjectPairRelationshipViewModel();
                pairViewModel.AddObjectClicked += (not, used) => HandleAddCollisionRelationshipAddClicked(pairViewModel);
                pairViewModel.OtherObjectName = collidable.InstanceName;
                pairViewModel.SelectedNamedObjectName = thisNamedObject.InstanceName;

                var relationshipsForThisPair = relationships
                    .Where(item =>
                    {
                        return (FirstCollidableIn(item) == name1 && SecondCollidableIn(item) == name2) ||
                            (FirstCollidableIn(item) == name2 && SecondCollidableIn(item) == name1);
                    })
                    .ToArray();

                foreach(var relationship in relationshipsForThisPair)
                {
                    var relationshipViewModel = new RelationshipListCellViewModel();
                    relationshipViewModel.OwnerNamedObject = thisNamedObject;
                    relationshipViewModel.OtherNamedObject = collidable;
                    relationshipViewModel.CollisionRelationshipNamedObject = relationship;

                    pairViewModel.Relationships.Add(relationshipViewModel);
                }


                viewModel.NamedObjectPairs.Add(pairViewModel);
            }

        }

        private static void HandleAddCollisionRelationshipAddClicked(NamedObjectPairRelationshipViewModel pairViewModel)
        {

            var addObjectModel = new AddObjectViewModel();

            // do we need to set dialog result?
            addObjectModel.SourceType = FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType;
            addObjectModel.SourceClassType = "FlatRedBall.Math.Collision.CollisionRelationship";
            addObjectModel.ObjectName = "ToBeRenamed";

            IElement selectedElement = GlueState.Self.CurrentElement;
            var selectedNamedObject = GlueState.Self.CurrentNamedObjectSave;

            var newNos =
                GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectModel,
                selectedElement, namedObject: null);

            newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled), true);

            bool needToInvert = selectedNamedObject.SourceType != SourceType.Entity;

            if(needToInvert)
            {
                newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName),
                    pairViewModel.OtherObjectName);
                newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName),
                    pairViewModel.SelectedNamedObjectName);
            }
            else
            {
                newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName),
                    pairViewModel.SelectedNamedObjectName);
                newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName),
                    pairViewModel.OtherObjectName);
            }

            CollisionRelationshipViewModelController.TryFixSourceClassType(newNos);

            // this will regenerate and save everything too:
            CollisionRelationshipViewModelController.TryApplyAutoName(
                selectedElement, newNos);


            RefreshViewModelTo(selectedElement, selectedNamedObject, ViewModel);
        }
    }
}
