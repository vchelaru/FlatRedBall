using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollisionRelationshipViewModelController
    {
        public static CollisionRelationshipViewModel ViewModel { get; private set; }

        static CollisionRelationshipViewModelController()
        {
            ViewModel = new CollisionRelationshipViewModel();
            ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }


        static void RefreshAvailableCollisionObjects(IElement element, CollisionRelationshipViewModel viewModel, NamedObjectSave collisionRelationship)
        {
            var firstBefore = collisionRelationship.Properties.GetValue<string>(nameof(viewModel.FirstCollisionName));
            var secondBefore = collisionRelationship.Properties.GetValue<string>(nameof(viewModel.SecondCollisionName));

            viewModel.FirstCollisionItemSource.Clear();
            viewModel.SecondCollisionItemSource.Clear();

            var namedObjects = element.AllNamedObjects
                .OrderBy(item => item.InstanceName)
                .ToArray();

            List<string> names = new List<string>();

            // consider:
            // 1. Individual ICollidables
            // 2. Lists of ICollidables
            // 3. TileShapeCollections
            // 4. ShapeCollections

            foreach (var nos in namedObjects)
            {
                bool shouldConsider = GetIfCanBeReferencedByRelationship(nos);

                if (shouldConsider)
                {
                    //names.Add(nos.InstanceName);
                    viewModel.FirstCollisionItemSource.Add(nos.InstanceName);
                    viewModel.SecondCollisionItemSource.Add(nos.InstanceName);

                }
            }

            // allow the user to select "None" as the 2nd type:
            viewModel.SecondCollisionItemSource.Add(CollisionRelationshipViewModel.AlwaysColliding);

            if(viewModel.FirstCollisionItemSource.Contains(firstBefore))
            {
                viewModel.FirstCollisionName = firstBefore;
            }
            if(viewModel.SecondCollisionItemSource.Contains(secondBefore))
            {
                viewModel.SecondCollisionName = secondBefore;
            }
        }

        internal static void HandleFirstCollisionPartitionClicked(CollisionRelationshipViewModel viewModel)
        {
            var element = GlueState.Self.CurrentElement;
            var firstNos = element?.GetNamedObject(viewModel.FirstCollisionName);

            if(firstNos != null)
            {
                GlueState.Self.CurrentNamedObjectSave = firstNos;

                GlueCommands.Self.DialogCommands.FocusTab("Collision");
            }
        }

        internal static void HandleSecondCollisionPartitionClicked(CollisionRelationshipViewModel viewModel)
        {
            var element = GlueState.Self.CurrentElement;
            var secondNos = element?.GetNamedObject(viewModel.SecondCollisionName);

            if(secondNos != null)
            {
                GlueState.Self.CurrentNamedObjectSave = secondNos;

                GlueCommands.Self.DialogCommands.FocusTab("Collision");
            }
        }

        public static bool GetIfCanBeReferencedByRelationship(NamedObjectSave nos)
        {
            if(nos == null)
            {
                throw new ArgumentNullException(nameof(nos));
            }
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
                    nos.IsList &&
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

            return shouldConsider;
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

        public static EntitySave GetEntitySaveReferencedBy(NamedObjectSave nos)
        {
            if (nos != null)
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
            }
            return null;
        }

        static bool IsShape(NamedObjectSave nos)
        {
            if(nos.SourceType == SourceType.FlatRedBallType)
            {
                var ati = nos.GetAssetTypeInfo();

                return ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                    ati == AvailableAssetTypes.CommonAtis.Circle ||
                    ati == AvailableAssetTypes.CommonAtis.Line ||
                    ati == AvailableAssetTypes.CommonAtis.Polygon;
            }
            return false;
        }

        public static void RefreshViewModel(NamedObjectSave collisionRelationship)
        {

            var currentElement = GlueState.Self.CurrentElement;

            ViewModel.GlueObject = collisionRelationship;


            var wasUpdatingFromGlue = ViewModel.IsUpdatingFromGlueObject;
            RefreshFirstAndSecondTypes(ViewModel, collisionRelationship);
            RefreshDamageProperties(currentElement, ViewModel);
            ViewModel.Events.Clear();
            foreach (var eventSave in currentElement.Events)
            {
                if (eventSave.SourceObject == collisionRelationship.InstanceName)
                {
                    ViewModel.Events.Add(eventSave);
                }
            }
            ViewModel.IsUpdatingFromGlueObject = true;
            RefreshAvailableCollisionObjects(currentElement, ViewModel, collisionRelationship);
            RefreshSubcollisionObjects(currentElement, ViewModel);
            RefreshFirstProperties(currentElement, ViewModel, out IElement firstNosElementType);
            RefreshSecondProperties(currentElement, ViewModel);
            RefreshPlatformerMovementValues(ViewModel, firstNosElementType);
            RefreshPartitioningIcons(currentElement, ViewModel);
            ViewModel.IsUpdatingFromGlueObject = wasUpdatingFromGlue;

            ViewModel.SupportsManualPhysics = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.CollisionRelationshipManualPhysics;
            // UpdateFromGlueObject should be called after refreshing the lists so the right values
            // get set
            ViewModel.UpdateFromGlueObject();
        }


        private static void RefreshFirstAndSecondTypes(CollisionRelationshipViewModel viewModel, NamedObjectSave collisionRelationship)
        {
            viewModel.FirstIndividualType = AssetTypeInfoManager.GetFirstGenericType(collisionRelationship, out bool isFirstList);
            viewModel.SecondIndividualType = AssetTypeInfoManager.GetSecondGenericType(collisionRelationship, out bool isSecondList);

            viewModel.IsFirstList = isFirstList;
            viewModel.IsSecondList = isSecondList;
        }

        private static void RefreshDamageProperties(GlueElement element, CollisionRelationshipViewModel viewModel)
        {
            var firstNos = element.GetNamedObject(viewModel.FirstCollisionName);
            var secondNos = element.GetNamedObject(viewModel.SecondCollisionName);

            viewModel.IsFirstDamageable = false;
            viewModel.IsFirstDamageArea = false;

            viewModel.IsSecondDamageable = false;
            viewModel.IsSecondDamageArea = false;

            if (firstNos != null)
            {
                GlueElement firstNosElementType = null;
                if (firstNos.IsList)
                {
                    var genericType = firstNos.SourceClassGenericType;
                    firstNosElementType = ObjectFinder.Self.GetEntitySave(genericType);
                }
                else
                {
                    firstNosElementType = firstNos.GetReferencedElement();
                }

                viewModel.IsFirstDamageable = firstNosElementType?.Properties.GetValue<bool>("ImplementsIDamageable") == true;
                viewModel.IsFirstDamageArea = firstNosElementType?.Properties.GetValue<bool>("ImplementsIDamageArea") == true;
            }

            if(secondNos != null)
            {
                GlueElement secondNosElementType = null;
                if(secondNos.IsList)
                {
                    var genericType = secondNos.SourceClassGenericType;
                    secondNosElementType = ObjectFinder.Self.GetEntitySave(genericType);
                }
                else
                {
                    secondNosElementType = secondNos.GetReferencedElement();
                }

                viewModel.IsSecondDamageable = secondNosElementType?.Properties.GetValue<bool>("ImplementsIDamageable") == true;
                viewModel.IsSecondDamageArea = secondNosElementType?.Properties.GetValue<bool>("ImplementsIDamageArea") == true;
            }
        }

        private static void RefreshPartitioningIcons(IElement element, CollisionRelationshipViewModel viewModel)
        {
            var firstNos = element.GetNamedObject(viewModel.FirstCollisionName);
            var secondNos = element.GetNamedObject(viewModel.SecondCollisionName);
            T Get<T>(NamedObjectSave nos, string propName) => nos.Properties.GetValue<T>(propName);

            var isFirstEffectivelyPartitioned = false;
            if(firstNos != null)
            {
                if(firstNos.IsList == false)
                {
                    // If it's not a list, then it's effectively partitioned, assuming the other can be partitioned
                    isFirstEffectivelyPartitioned = true;
                }
                else
                {
                    isFirstEffectivelyPartitioned = Get<bool>(firstNos, nameof(CollidableNamedObjectRelationshipViewModel.PerformCollisionPartitioning));
                }
            }
            viewModel.IsFirstPartitioned = isFirstEffectivelyPartitioned;

            var isSecondEffectivelyPartitioned = false;
            if(secondNos != null)
            {
                if(secondNos.IsList == false)
                {
                    isSecondEffectivelyPartitioned = true;
                }
                else
                {
                    isSecondEffectivelyPartitioned = Get<bool>(secondNos, nameof(CollidableNamedObjectRelationshipViewModel.PerformCollisionPartitioning));
                }
            }
            viewModel.IsSecondPartitioned = isSecondEffectivelyPartitioned;

        }

        static void RefreshSubcollisionObjects(IElement element, CollisionRelationshipViewModel viewModel)
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

                var list = GetAvailableSubCollisions(firstEntity);

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
                else if(oldValue == null)
                {
                    viewModel.FirstSubCollisionSelectedItem = CollisionRelationshipViewModel.EntireObject;
                }
            }
            else
            {
                viewModel.FirstSubCollisionSelectedItem = null;
            }

            if (canSecondHaveSubCollisions)
            {
                var secondEntity = GetEntitySaveReferencedBy(secondNos);

                var list = GetAvailableSubCollisions(secondEntity);

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
                else if (oldValue == null)
                {
                    viewModel.SecondSubCollisionSelectedItem = CollisionRelationshipViewModel.EntireObject;
                }
            }
            else
            {
                viewModel.SecondSubCollisionSelectedItem = null;
            }
        }

        private static List<string> GetAvailableSubCollisions(EntitySave firstEntity)
        {
            List<string> availableValues = new List<string>();
            availableValues.Add(CollisionRelationshipViewModel.EntireObject);

            foreach (var nos in firstEntity.AllNamedObjects)
            {
                var canBeSubCollision = IsShape(nos) || nos.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection;

                if (canBeSubCollision)
                {
                    availableValues.Add(nos.InstanceName);
                }
            }
            return availableValues;
        }

        public static void HandleGlueObjectPropertyChanged(string changedMember, object oldValue, GlueElement element)
        {
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            if (changedMember == nameof(NamedObjectSave.InstanceName) && namedObject != null)
            {
                UpdateCollisionRelationshipsInThisElement(element, oldValue);

                UpdateOtherObjectsIfChangedObjectIsShapeInCollidable(element, (string)oldValue);
            }
        }

        private static void UpdateOtherObjectsIfChangedObjectIsShapeInCollidable(IElement element, string oldName)
        {
            var asEntitySave = element as EntitySave;

            if(asEntitySave != null && asEntitySave.ImplementsICollidable)
            {
                var namedObject = GlueState.Self.CurrentNamedObjectSave;
                string newName = namedObject.InstanceName;

                if(IsShape(namedObject))
                {
                    // a shape was renamed. This could change the name of another collision relationship
                    var collisionRelationshipAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;

                    foreach(var container in GlueState.Self.CurrentGlueProject.AllElements())
                    {
                        var collisionRelationships = container.AllNamedObjects
                            .Where(item =>
                            {
                                TryFixSourceClassType(item);
                                return item.GetAssetTypeInfo() == collisionRelationshipAti;
                            })
                            .ToArray();

                        foreach(var relationship in collisionRelationships)
                        {
                            var shouldRename = relationship
                                .Properties
                                .GetValue<bool>(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled));
                            if(shouldRename)
                            {
                                string firstNosName = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

                                string secondNosName = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));

                                var firstNos = container.GetNamedObjectRecursively(firstNosName);
                                var secondNos = container.GetNamedObjectRecursively(secondNosName);

                                var firstElement = GetEntitySaveReferencedBy(firstNos);
                                var secondElement = GetEntitySaveReferencedBy(secondNos);

                                var firstSubCollision = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));

                                var secondSubCollision = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));


                                var shouldApplyAutoName = false;
                                if (firstElement == element && firstSubCollision == oldName)
                                {
                                    relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem), newName);
                                    shouldApplyAutoName = true;
                                }
                                if (secondElement == element && secondSubCollision == oldName)
                                {
                                    relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem), newName);
                                    shouldApplyAutoName = true;
                                }

                                if(shouldApplyAutoName)
                                { 
                                    // we could test if it should be renamed but...why not just do a rename. We can be lazy here.
                                    // If there is no change in the name, it won't re-save or regenerate:
                                    TryApplyAutoName(container, relationship);
                                }

                            }


                        }
                    }



                }
            }
        }

        private static void UpdateCollisionRelationshipsInThisElement(IElement element, object oldValue)
        {
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

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
                TryApplyAutoName(element, item);
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
                TryApplyAutoName(element, item);
            }

            if (changedAny)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
        }

        public static void TryFixSourceClassType(NamedObjectSave selectedNos)
        {
            if (selectedNos.IsCollisionRelationship())
            {
                selectedNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti
                    .QualifiedRuntimeTypeName.PlatformFunc(selectedNos);
            }
        }

        private static void HandleViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var viewModel = sender as CollisionRelationshipViewModel;

            ////////////////Early Out///////////////////
            if (viewModel.IsUpdatingFromGlueObject)
            {
                return;
            }

            //////////////End Early Out/////////////////

            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            var element = GlueState.Self.CurrentElement;
            var relationshipNos = viewModel.GlueObject as NamedObjectSave;
            switch (e.PropertyName)
            {
                case nameof(viewModel.FirstCollisionName):
                case nameof(viewModel.SecondCollisionName):
                    CollisionRelationshipViewModelController.TryFixSourceClassType(relationshipNos);

                    RefreshFirstAndSecondTypes(viewModel, relationshipNos);

                    RefreshSubcollisionObjects(element, viewModel);

                    TryApplyAutoName(element, namedObject);

                    RefreshFirstProperties(element, viewModel, out IElement firstElementType);
                    RefreshSecondProperties(element, ViewModel);

                    RefreshPlatformerMovementValues(viewModel, firstElementType);

                    RefreshPartitioningIcons(element, viewModel);

                    RefreshDamageProperties(element, viewModel);

                    if (TryFixMassesForTileShapeCollisionRelationship(element, relationshipNos))
                    {
                        viewModel.UpdateFromGlueObject();
                    }

                    break;
                case nameof(viewModel.FirstSubCollisionSelectedItem):
                    TryApplyAutoName(element, namedObject);

                    break;
                case nameof(viewModel.SecondSubCollisionSelectedItem):
                    TryApplyAutoName(element, namedObject);

                    break;
                case nameof(viewModel.IsAutoNameEnabled):
                    TryApplyAutoName(element, namedObject);
                    break;

                case nameof(viewModel.CollisionType):
                    CollisionRelationshipViewModelController.TryFixSourceClassType(relationshipNos);
                    if (TryFixMassesForTileShapeCollisionRelationship(element, relationshipNos))
                    {
                        viewModel.UpdateFromGlueObject();
                    }
                    break;
            }
        }

        static void RefreshFirstProperties(GlueElement collisionRelationshipOwner, CollisionRelationshipViewModel viewModel, out IElement firstNosElementType)
        {
            firstNosElementType = null;
            var firstName = viewModel.FirstCollisionName;

            var firstNos = collisionRelationshipOwner.GetNamedObject(firstName);

            bool isPlatformer = false;
            bool isStackable = false;
                       
            if (firstNos != null)
            {                
                if(firstNos.IsList)
                {
                    var genericType = firstNos.SourceClassGenericType;
                    firstNosElementType = ObjectFinder.Self.GetEntitySave(genericType);

                }
                else
                {
                    firstNosElementType = firstNos.GetReferencedElement();
                }

                isPlatformer = firstNosElementType?.Properties.GetValue<bool>("IsPlatformer") == true;
                isStackable = firstNosElementType?.Properties.GetValue<bool>("ImplementsIStackable") == true;
            }

            viewModel.IsFirstPlatformer = isPlatformer;
            viewModel.IsFirstStackable = isStackable;
        }

        static void RefreshSecondProperties(GlueElement collisionRelationshipOwner, CollisionRelationshipViewModel viewModel)
        {
            GlueElement secondNosElement = null;

            var secondName = viewModel.SecondCollisionName;

            var secondNos = collisionRelationshipOwner.GetNamedObject(secondName);

            bool isStackable = false;
            bool isTileShapeCollection = false;

            if(secondNos != null)
            {
                if (secondNos.IsList)
                {
                    var genericType = secondNos.SourceClassGenericType;
                    secondNosElement = ObjectFinder.Self.GetEntitySave(genericType);

                }
                else
                {
                    secondNosElement = secondNos.GetReferencedElement();
                }

                isStackable = secondNosElement?.Properties.GetValue<bool>("ImplementsIStackable") == true;
                isTileShapeCollection = secondNos?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection";
            }

            viewModel.IsSecondStackable = isStackable;
            viewModel.IsSecondTileShapeCollection = isTileShapeCollection;
        }

        static void RefreshPlatformerMovementValues(CollisionRelationshipViewModel viewModel, IElement firstNosElementType)
        {
            viewModel.AvailablePlatformerVariableNames.Clear();
            
            if(viewModel.IsFirstPlatformer && firstNosElementType != null)
            {
                var rfs = firstNosElementType.ReferencedFiles.FirstOrDefault(item => item.Name.EndsWith("PlatformerValuesStatic.csv"));
                if(rfs != null)
                {
                    var platformerValues = FlatRedBall.Glue.GuiDisplay.AvailableSpreadsheetValueTypeConverter.GetAvailableValues(
                        GlueCommands.Self.GetAbsoluteFileName(rfs), shouldAppendFileName: true);
                    foreach (var platformerValue in platformerValues)
                    {
                        viewModel.AvailablePlatformerVariableNames.Add(platformerValue);
                    }
                }
            }
        }

        public static void TryApplyAutoName(IElement element, NamedObjectSave namedObject)
        {
            var isAutoNameEnabled = namedObject.Properties.GetValue<bool>(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled));
            if(isAutoNameEnabled)
            {
                var desiredName = GetAutoName(namedObject);

                bool nameExists = false;
                do
                {
                    // need to consider base objects not just the current element
                    var allNamedObjectsRecursive = element.GetAllNamedObjectsRecurisvely().ToArray();

                    nameExists = allNamedObjectsRecursive
                        .Any(item => item != namedObject &&
                                     item.InstanceName == desiredName);

                    if(!nameExists)
                    {
                        // if we got here, then there are no conflicting names in 
                        // the current element or any base elements, but we should
                        // also check derived in case the user made a relationship that
                        // is level-specific, and now is trying to make a similar one in 
                        // the base screen
                        var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(element);

                        foreach(var derivedElement in derivedElements)
                        {
                            nameExists = derivedElement.AllNamedObjects.Any(item => item.DefinedByBase == false && item.InstanceName == desiredName);

                            if(nameExists)
                            {
                                break;
                            }
                        }
                    }

                    if(nameExists)
                    {
                        if(StringFunctions.HasNumberAtEnd(desiredName))
                        {
                            desiredName = StringFunctions.IncrementNumberAtEnd(desiredName);
                        }
                        else
                        {
                            desiredName = desiredName + "2";
                        }
                    }
                } while (nameExists);

                if (desiredName != namedObject.InstanceName)
                {
                    var oldName = namedObject.InstanceName;

                    namedObject.InstanceName = desiredName;

                    // this requires the NOS being selected:
                    if(namedObject != GlueState.Self.CurrentNamedObjectSave)
                    {
                        GlueState.Self.CurrentNamedObjectSave = namedObject;
                    }

                    // This is important otherwise references to this (like events) won't update their references
                    EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                        nameof(NamedObjectSave.InstanceName), oldName, namedObjectSave:namedObject);


                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element as GlueElement);
                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element as GlueElement);
                }
            }
        }

        public static bool TryFixMassesForTileShapeCollisionRelationship(IElement selectedElement, NamedObjectSave collisionRelationshipNos)
        {
            // at this point "inverting" has already happened so check if the 2nd object is a tile shape collection

            var secondName = collisionRelationshipNos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));
            var secondObjectInstance = selectedElement.GetNamedObjectRecursively(secondName);

            var didMakeChange = false;

            if (secondObjectInstance?.SourceClassType?.Contains("TileShapeCollection") == true)
            {
                // set the first object's mass to 1, the 2nd object's mass to 1:
                var shouldSetTo0 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == nameof(CollisionRelationshipViewModel.FirstCollisionMass)) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(nameof(CollisionRelationshipViewModel.FirstCollisionMass)) != 0;

                if (shouldSetTo0)
                {
                    collisionRelationshipNos.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.FirstCollisionMass), 0f);
                    didMakeChange = true;
                }

                var shouldSetTo1 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == nameof(CollisionRelationshipViewModel.SecondCollisionMass)) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(nameof(CollisionRelationshipViewModel.SecondCollisionMass)) != 1;

                if (shouldSetTo1)
                {
                    collisionRelationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionMass), 1.0f);
                    didMakeChange = true;
                }
            }

            if(didMakeChange)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                GlueCommands.Self.GluxCommands.SaveGlux();
            }

            return didMakeChange;
        }

        public static string GetAutoName(NamedObjectSave namedObject)
        {
            var element = ObjectFinder.Self.GetElementContaining(namedObject);

            var firstName = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));
            var firstSub = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));
            if(firstSub == CollisionRelationshipViewModel.EntireObject)
            {
                firstSub = null;
            }

            var secondName = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.SecondCollisionName));
            var secondSub = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));
            if(secondSub == CollisionRelationshipViewModel.EntireObject)
            {
                secondSub = null;
            }

            var isFirstList = element?.GetNamedObjectRecursively(firstName)?.IsList == true;
            var isSecondList = element?.GetNamedObjectRecursively(secondName)?.IsList == true;

            var firstEffectiveName = firstName;
            var secondEffectiveName = secondName;

            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.AutoNameCollisionListsAsSingle)
            {
                if(isFirstList && firstEffectiveName.EndsWith("List"))
                {
                    firstEffectiveName = firstEffectiveName.Substring(0, firstEffectiveName.Length - "List".Length);
                }
                if(isSecondList && secondEffectiveName.EndsWith("List"))
                {
                    secondEffectiveName = secondEffectiveName.Substring(0, secondEffectiveName.Length - "List".Length);
                }
            }

            // special case - always colliding
            if(secondName == null)
            {
                return $"{firstEffectiveName}AlwaysColliding";
            }
            else
            {
                return $"{firstEffectiveName}{firstSub}Vs{secondEffectiveName}{secondSub}";
            }
        }
    }
}
