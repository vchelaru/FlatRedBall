using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Math.Collision;
using FlatRedBall.Utilities;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using CollisionType = OfficialPlugins.CollisionPlugin.ViewModels.CollisionType;

namespace OfficialPlugins.CollisionPlugin.Managers
{
    /// <summary>
    /// Collision relationship logic with no UI: creating one, reading and changing its settings, and the follow-up
    /// rules (auto name, sub-collision reconciliation, TileShapeCollection masses, source class type). The Collision
    /// tab's controllers call this and then refresh their view models; GlueCli reaches it through
    /// <see cref="GlueCommands.CollisionRelationshipCommands"/>.
    /// </summary>
    public class CollisionRelationshipCommands : ICollisionRelationshipCommands
    {
        #region Settings

        const string FirstCollisionName = nameof(CollisionRelationshipViewModel.FirstCollisionName);
        const string SecondCollisionName = nameof(CollisionRelationshipViewModel.SecondCollisionName);
        const string FirstSub = nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem);
        const string SecondSub = nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem);
        const string IsAutoNameEnabled = nameof(CollisionRelationshipViewModel.IsAutoNameEnabled);
        const string CollisionTypeName = nameof(CollisionRelationshipViewModel.CollisionType);
        const string FirstMass = nameof(CollisionRelationshipViewModel.FirstCollisionMass);
        const string SecondMass = nameof(CollisionRelationshipViewModel.SecondCollisionMass);

        /// <summary>
        /// Every setting the Collision tab persists, with the default its view model shows. The tab's view model
        /// declares the same list as [SyncedProperty] members; CollisionRelationshipCommandsTests keeps them equal.
        /// </summary>
        static readonly CollisionRelationshipSetting[] settings =
        {
            new(FirstCollisionName, typeof(string), null),
            new(SecondCollisionName, typeof(string), null),
            new(FirstSub, typeof(string), null),
            new(SecondSub, typeof(string), null),
            new(IsAutoNameEnabled, typeof(bool), false),
            new(nameof(CollisionRelationshipViewModel.IsCollisionActive), typeof(bool), true),
            new(nameof(CollisionRelationshipViewModel.ListVsListLoopingMode), typeof(ListVsListLoopingMode), default(ListVsListLoopingMode)),
            new(CollisionTypeName, typeof(CollisionType), default(CollisionType)),
            new(nameof(CollisionRelationshipViewModel.CollisionLimit), typeof(CollisionLimit), default(CollisionLimit)),
            new(nameof(CollisionRelationshipViewModel.IsAutomaticallyApplyPhysicsChecked), typeof(bool), true),
            new(FirstMass, typeof(float), 1.0f),
            new(SecondMass, typeof(float), 1.0f),
            new(nameof(CollisionRelationshipViewModel.SoftCollisionCoefficient), typeof(float), 1.0f),
            new(nameof(CollisionRelationshipViewModel.CollisionElasticity), typeof(float), 1.0f),
            new(nameof(CollisionRelationshipViewModel.GroundPlatformerVariableName), typeof(string), null),
            new(nameof(CollisionRelationshipViewModel.AirPlatformerVariableName), typeof(string), null),
            new(nameof(CollisionRelationshipViewModel.AfterDoubleJumpPlatformerVariableName), typeof(string), null),
            new(nameof(CollisionRelationshipViewModel.IsDealDamageChecked), typeof(bool), false),
            new(nameof(CollisionRelationshipViewModel.IsDestroyFirstOnDamageChecked), typeof(bool), false),
            new(nameof(CollisionRelationshipViewModel.FirstCollisionDestroyType), typeof(CollisionDestroyType), default(CollisionDestroyType)),
            new(nameof(CollisionRelationshipViewModel.IsDestroySecondOnDamageChecked), typeof(bool), false),
            new(nameof(CollisionRelationshipViewModel.SecondCollisionDestroyType), typeof(CollisionDestroyType), default(CollisionDestroyType)),
        };

        /// <summary>Enums the view model stores as their int value (its setters call SetAndPersist((int)value)).</summary>
        static readonly HashSet<Type> enumsStoredAsInt = new() { typeof(ListVsListLoopingMode), typeof(CollisionType), typeof(CollisionLimit) };

        public IReadOnlyList<CollisionRelationshipSetting> Settings => settings;

        public bool IsCollisionRelationship(NamedObjectSave nos) => nos.IsCollisionRelationship();

        public bool CanCollide(NamedObjectSave nos) => GetIfCanBeReferencedByRelationship(nos);

        public IReadOnlyList<string> GetSubCollisionOptions(NamedObjectSave collidable) =>
            CanHaveSubCollisions(collidable) ? GetAvailableSubCollisions(GetEntitySaveReferencedBy(collidable)) : null;

        public IReadOnlyList<string> GetOptions(NamedObjectSave relationship, string setting)
        {
            var element = ObjectFinder.Self.GetElementContaining(relationship);
            switch (setting)
            {
                case FirstCollisionName:
                case SecondCollisionName:
                    var names = element.AllNamedObjects.Where(GetIfCanBeReferencedByRelationship)
                        .Select(item => item.InstanceName).OrderBy(item => item).ToList();
                    if (setting == SecondCollisionName)
                    {
                        names.Add(CollisionRelationshipViewModel.AlwaysColliding);
                    }
                    return names;
                case FirstSub:
                    return GetSubCollisionOptions(element.GetNamedObject(relationship.Properties.GetValue<string>(FirstCollisionName)));
                case SecondSub:
                    return GetSubCollisionOptions(element.GetNamedObject(relationship.Properties.GetValue<string>(SecondCollisionName)));
                case nameof(CollisionRelationshipViewModel.GroundPlatformerVariableName):
                case nameof(CollisionRelationshipViewModel.AirPlatformerVariableName):
                case nameof(CollisionRelationshipViewModel.AfterDoubleJumpPlatformerVariableName):
                    var firstEntity = GetEntitySaveReferencedBy(element.GetNamedObject(relationship.Properties.GetValue<string>(FirstCollisionName)));
                    return firstEntity?.Properties.GetValue<bool>("IsPlatformer") == true
                        ? GetPlatformerVariableNames(firstEntity)
                        : new List<string>();
                default:
                    return null;
            }
        }

        public object GetValue(NamedObjectSave relationship, string setting)
        {
            var definition = GetSetting(setting);
            var stored = relationship.Properties.FirstOrDefault(item => item.Name == setting);
            if (stored == null)
            {
                return definition.DefaultValue;
            }
            var value = stored.Value;
            if (definition.Type.IsEnum)
            {
                return value == null ? definition.DefaultValue : Enum.ToObject(definition.Type, Convert.ToInt32(value));
            }
            if (definition.Type == typeof(float))
            {
                return value == null ? definition.DefaultValue : Convert.ToSingle(value);
            }
            if (definition.Type == typeof(bool))
            {
                return value == null ? definition.DefaultValue : Convert.ToBoolean(value);
            }
            return value as string;
        }

        public void SetValue(NamedObjectSave relationship, string setting, object value)
        {
            var definition = GetSetting(setting);
            if (setting == SecondCollisionName && (string)value == CollisionRelationshipViewModel.AlwaysColliding)
            {
                value = null;
            }
            var oldValue = GetValue(relationship, setting);
            if (Equals(oldValue, value))
            {
                return;
            }
            Persist(relationship, setting, definition.Type, value);
            ReactToChangedSetting(ObjectFinder.Self.GetElementContaining(relationship), relationship, setting);
            PropertyListChangeFollowUp.QueueCodegenReactionAndSave(relationship, setting, oldValue);
        }

        static void Persist(NamedObjectSave relationship, string setting, Type type, object value)
        {
            // Stored the way the view model's SetAndPersist stores it.
            var stored = value != null && enumsStoredAsInt.Contains(type) ? (int)value : value;
            relationship.Properties.SetValuePersistIfDefault(setting, stored);
        }

        static CollisionRelationshipSetting GetSetting(string setting) =>
            settings.FirstOrDefault(item => item.Name == setting) ??
            throw new ArgumentException($"Collision relationships have no setting {setting}", nameof(setting));

        /// <summary>
        /// The model rules that follow a changed setting, wherever it was changed (the Collision tab or
        /// <see cref="SetValue"/>): the source class type follows the objects and collision type, sub-collisions
        /// follow the objects, the auto name follows the objects and sub-collisions, and TileShapeCollection
        /// relationships keep their masses.
        /// </summary>
        public static void ReactToChangedSetting(GlueElement element, NamedObjectSave relationship, string setting)
        {
            switch (setting)
            {
                case FirstCollisionName:
                case SecondCollisionName:
                    TryFixSourceClassType(relationship);
                    ReconcileSubCollision(element, relationship, FirstCollisionName, FirstSub);
                    ReconcileSubCollision(element, relationship, SecondCollisionName, SecondSub);
                    TryApplyAutoName(element, relationship);
                    TryFixMassesForTileShapeCollisionRelationship(element, relationship);
                    // The relationship's first/second collidable can now be empty or point at a
                    // missing object (see CollisionRelationshipErrorViewModel.TryGetErrorMessageFor) -
                    // refresh immediately instead of waiting for the next Error tab refresh/project reload.
                    GlueCommands.Self.RefreshCommands.RefreshErrors();
                    break;
                case FirstSub:
                case SecondSub:
                case IsAutoNameEnabled:
                    TryApplyAutoName(element, relationship);
                    break;
                case CollisionTypeName:
                    TryFixSourceClassType(relationship);
                    TryFixMassesForTileShapeCollisionRelationship(element, relationship);
                    break;
            }
        }

        /// <summary>
        /// The sub-collision the Collision tab keeps for an object: cleared when the object cannot have one,
        /// &lt;Entire Object&gt; when none was chosen, otherwise unchanged.
        /// </summary>
        static void ReconcileSubCollision(GlueElement element, NamedObjectSave relationship, string objectSetting, string subSetting)
        {
            var collidable = element.GetNamedObject(relationship.Properties.GetValue<string>(objectSetting));
            var current = relationship.Properties.GetValue<string>(subSetting);
            var options = CanHaveSubCollisions(collidable) ? GetAvailableSubCollisions(GetEntitySaveReferencedBy(collidable)) : null;

            string desired;
            if (options == null)
            {
                desired = null;
            }
            else if (current == null)
            {
                desired = CollisionRelationshipViewModel.EntireObject;
            }
            else
            {
                return;
            }
            if (desired != current)
            {
                Persist(relationship, subSetting, typeof(string), desired);
                TryApplyAutoName(element, relationship);
                PropertyListChangeFollowUp.QueueCodegenReactionAndSave(relationship, subSetting, current);
            }
        }

        #endregion

        #region Create

        public bool IsUnusualContainer(GlueElement container)
        {
            if (container is ScreenSave screen)
            {
                var gameScreen = ObjectFinder.Self.GetScreenSave("GameScreen");
                return ObjectFinder.Self.GetAllBaseElementsRecursively(screen).Contains(gameScreen);
            }
            return false;
        }

        public async Task<NamedObjectSave> CreateAsync(GlueElement container, string firstObjectName, string secondObjectName)
        {
            NamedObjectSave newNos = null;
            await TaskManager.Self.AddAsync(async () =>
            {
                newNos = await CreateInTaskAsync(container, firstObjectName, secondObjectName);
            }, $"Creating collision relationships between {firstObjectName} and {secondObjectName}");
            return newNos;
        }

        /// <summary>The body of <see cref="CreateAsync"/>, for a caller already running in a task.</summary>
        internal static async Task<NamedObjectSave> CreateInTaskAsync(GlueElement container, string firstNosName, string secondNosName)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            var addObjectModel = new AddObjectViewModel();
            addObjectModel.ForcedElementToAddTo = container;

            var firstNos = container.GetNamedObjectRecursively(firstNosName);
            var secondNos = container.GetNamedObjectRecursively(secondNosName);

            if (firstNos == null)
            {
                throw new InvalidOperationException(
                    $"Could not find an entity with the name {firstNosName} in {container}");
            }

            addObjectModel.SourceType = SourceType.FlatRedBallType;
            addObjectModel.SelectedAti =
                AssetTypeInfoManager.Self.CollisionRelationshipAti;
            //"FlatRedBall.Math.Collision.CollisionRelationship";

            addObjectModel.Properties.SetValue(IsAutoNameEnabled, true);

            NamedObjectSave effectiveFirstNos;
            NamedObjectSave effectiveSecondNos;

            bool needToInvert = firstNos.SourceType != SourceType.Entity &&
                firstNos.IsList == false;

            if (needToInvert)
            {
                addObjectModel.Properties.SetValue(FirstCollisionName, secondNosName);
                addObjectModel.Properties.SetValue(SecondCollisionName, firstNosName);

                effectiveFirstNos = secondNos;
                effectiveSecondNos = firstNos;
            }
            else
            {
                addObjectModel.Properties.SetValue(FirstCollisionName, firstNosName);
                addObjectModel.Properties.SetValue(SecondCollisionName, secondNosName);

                effectiveFirstNos = firstNos;
                effectiveSecondNos = secondNos;
            }

            EntitySave firstEntityType = null;
            if (effectiveFirstNos.SourceType == SourceType.Entity)
            {
                firstEntityType = ObjectFinder.Self.GetEntitySave(effectiveFirstNos.SourceClassType);
            }
            else if (effectiveFirstNos.IsList)
            {
                firstEntityType = ObjectFinder.Self.GetEntitySave(effectiveFirstNos.SourceClassGenericType);
            }

            EntitySave secondEntityType = null;
            if (effectiveSecondNos != null)
            {
                // This can happen if the user is creating an always-colliding relationship.
                if (effectiveSecondNos.SourceType == SourceType.Entity)
                {
                    secondEntityType = ObjectFinder.Self.GetEntitySave(effectiveSecondNos.SourceClassType);
                }
                else
                {
                    secondEntityType = ObjectFinder.Self.GetEntitySave(effectiveSecondNos.SourceClassGenericType);
                }
            }

            // this used to rely on the name "SolidCollision" but that's not a set standard and there could be multiple
            // TileShapeCollections
            if (secondNos?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection")
            {
                bool isPlatformer = false;
                if (firstEntityType != null)
                {
                    isPlatformer = firstEntityType.Properties.GetValue<bool>("IsPlatformer");
                }

                if (isPlatformer)
                {
                    addObjectModel.Properties.SetValue(CollisionTypeName, (int)CollisionType.PlatformerSolidCollision);
                }
                else
                {
                    addObjectModel.Properties.SetValue(CollisionTypeName, (int)CollisionType.BounceCollision);
                    addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.CollisionElasticity), 0.0f);
                }
            }

            var sourceClassType = AssetTypeInfoManager.GetCollisionRelationshipSourceClassType(container, addObjectModel.Properties);
            addObjectModel.SourceClassType = sourceClassType;

            // setting the SourceClassType sets the ObjectName. Overwrite it...
            addObjectModel.ObjectName = "ToBeRenamed";

            var newNos =
                await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectModel,
                container, listToAddTo: null);

            var indexToInsertAfter = GetIndexToInsertAfter(newNos, container);

            if (indexToInsertAfter != null)
            {
                container.NamedObjects.Remove(newNos);
                container.NamedObjects.Insert(indexToInsertAfter.Value + 1, newNos);
            }

            // if this is an always-colliding relationship, the user will typically want this
            // to be at the beginning before all other relationships. Therefore, let's remove
            // and re-add it:
            var isAlwaysColliding = secondNos == null;
            if (isAlwaysColliding)
            {
                container.NamedObjects.Remove(newNos);
                container.NamedObjects.Insert(0, newNos);
            }

            // this will regenerate and save everything too:
            TryApplyAutoName(container, newNos);

            // January 10, 2023
            // Why not always check
            // this value? If the objects
            // are not IDamageable/IDamageArea,
            // then the code generator will ignore
            // these options. If they are, then we want
            // them always enabled.
            await GlueCommands.Self.GluxCommands.SetPropertyOnAsync(newNos, nameof(CollisionRelationshipViewModel.IsDealDamageChecked),
                value: true,
                performSaveAndGenerateCode: false,
                recordUndo: true);

            // March 1, 2024
            // NO NO! This used to be true, but we don't want that because it results in the player
            // being destroyed right away. This causes so much confusion that it's best to leave it unchecked.
            //IsDestroyFirstOnDamageChecked, IsDestroySecondOnDamageChecked

            TryFixMassesForTileShapeCollisionRelationship(container, newNos);

            return newNos;
        }

        private static int? GetIndexToInsertAfter(NamedObjectSave newNos, GlueElement container)
        {
            var newNosFirst = newNos.Properties.GetValue<string>(FirstCollisionName);

            for (int i = container.NamedObjects.Count - 1; i >= 0; i--)
            {
                var candidate = container.NamedObjects[i];

                if (candidate == newNos) continue;

                var candidateFirst = candidate.Properties.GetValue<string>(FirstCollisionName);

                if (candidateFirst == newNosFirst)
                {
                    return i;
                }
            }

            return null;
        }

        #endregion

        #region Model rules

        public static bool GetIfCanBeReferencedByRelationship(NamedObjectSave nos)
        {
            if (nos == null)
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
                    ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType)?.IsICollidableRecursive() == true;
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

        public static bool CanHaveSubCollisions(NamedObjectSave nos)
        {
            if (nos != null)
            {
                return GetEntitySaveReferencedBy(nos)?.IsICollidableRecursive() == true;
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

        internal static bool IsShape(NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.FlatRedBallType)
            {
                var ati = nos.GetAssetTypeInfo();

                return ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                    ati == AvailableAssetTypes.CommonAtis.CapsulePolygon ||
                    ati == AvailableAssetTypes.CommonAtis.Circle ||
                    ati == AvailableAssetTypes.CommonAtis.Line ||
                    ati == AvailableAssetTypes.CommonAtis.Polygon;
            }
            return false;
        }

        public static bool IsTileShapeCollection(NamedObjectSave namedObject)
        {
            return namedObject?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection";
        }

        public static List<string> GetAvailableSubCollisions(EntitySave firstEntity)
        {
            List<string> availableValues = new List<string>();
            availableValues.Add(CollisionRelationshipViewModel.EntireObject);

            foreach (var nos in firstEntity.AllNamedObjects)
            {
                var canBeSubCollision = IsShape(nos) ||
                    nos.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection ||
                    IsTileShapeCollection(nos) ||
                    nos.IsCollidableOrCollidableList();

                if (canBeSubCollision)
                {
                    availableValues.Add(nos.InstanceName);
                }
            }
            return availableValues;
        }

        /// <summary>The platformer value sets in a platformer entity's PlatformerValuesStatic.csv.</summary>
        public static List<string> GetPlatformerVariableNames(IElement platformerEntity)
        {
            var names = new List<string>();
            var rfs = platformerEntity.ReferencedFiles.FirstOrDefault(item => item.Name.EndsWith("PlatformerValuesStatic.csv"));
            if (rfs != null)
            {
                names.AddRange(FlatRedBall.Glue.GuiDisplay.AvailableSpreadsheetValueTypeConverter.GetAvailableValues(
                    GlueCommands.Self.GetAbsoluteFileName(rfs), shouldAppendFileName: true));
            }
            return names;
        }

        public static bool TryFixSourceClassType(NamedObjectSave selectedNos)
        {
            var didChange = false;
            if (selectedNos.IsCollisionRelationship())
            {
                var oldSourceClassType = selectedNos.SourceClassType;

                selectedNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti
                    .QualifiedRuntimeTypeName.PlatformFunc(selectedNos);

                didChange = oldSourceClassType != selectedNos.SourceClassType;
            }

            return didChange;
        }

        public static void TryApplyAutoName(GlueElement element, NamedObjectSave namedObject)
        {
            var isAutoNameEnabled = namedObject.Properties.GetValue<bool>(IsAutoNameEnabled);
            if (isAutoNameEnabled)
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

                    if (!nameExists)
                    {
                        // if we got here, then there are no conflicting names in
                        // the current element or any base elements, but we should
                        // also check derived in case the user made a relationship that
                        // is level-specific, and now is trying to make a similar one in
                        // the base screen
                        var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(element);

                        foreach (var derivedElement in derivedElements)
                        {
                            nameExists = derivedElement.AllNamedObjects.Any(item => item.DefinedByBase == false && item.InstanceName == desiredName);

                            if (nameExists)
                            {
                                break;
                            }
                        }
                    }

                    if (nameExists)
                    {
                        if (StringFunctions.HasNumberAtEnd(desiredName))
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
                    if (namedObject != GlueState.Self.CurrentNamedObjectSave)
                    {
                        GlueState.Self.CurrentNamedObjectSave = namedObject;
                    }

                    // This is important otherwise references to this (like events) won't update their references
                    EditorObjects.IoC.Container.Get<NamedObjectSetVariableLogic>().ReactToNamedObjectChangedValue(
                        nameof(NamedObjectSave.InstanceName), oldName, namedObjectSave: namedObject);


                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element as GlueElement);
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element as GlueElement);
                }
            }
        }

        public static bool TryFixMassesForTileShapeCollisionRelationship(IElement selectedElement, NamedObjectSave collisionRelationshipNos)
        {
            // at this point "inverting" has already happened so check if the 2nd object is a tile shape collection

            var secondName = collisionRelationshipNos.Properties.GetValue<string>(SecondCollisionName);
            var secondObjectInstance = selectedElement.GetNamedObjectRecursively(secondName);

            var didMakeChange = false;

            if (secondObjectInstance?.SourceClassType?.Contains("TileShapeCollection") == true)
            {
                // set the first object's mass to 1, the 2nd object's mass to 1:
                var shouldSetTo0 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == FirstMass) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(FirstMass) != 0;

                if (shouldSetTo0)
                {
                    collisionRelationshipNos.Properties.SetValuePersistIfDefault(FirstMass, 0f);
                    didMakeChange = true;
                }

                var shouldSetTo1 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == SecondMass) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(SecondMass) != 1;

                if (shouldSetTo1)
                {
                    collisionRelationshipNos.Properties.SetValue(SecondMass, 1.0f);
                    didMakeChange = true;
                }
            }

            if (didMakeChange)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            }

            return didMakeChange;
        }

        public static string GetAutoName(NamedObjectSave namedObject)
        {
            var element = ObjectFinder.Self.GetElementContaining(namedObject);

            var firstName = namedObject.Properties.GetValue<string>(FirstCollisionName);
            var firstSub = namedObject.Properties.GetValue<string>(FirstSub);
            if (firstSub == CollisionRelationshipViewModel.EntireObject)
            {
                firstSub = null;
            }

            var secondName = namedObject.Properties.GetValue<string>(SecondCollisionName);
            var secondSub = namedObject.Properties.GetValue<string>(SecondSub);
            if (secondSub == CollisionRelationshipViewModel.EntireObject)
            {
                secondSub = null;
            }

            var isFirstList = element?.GetNamedObjectRecursively(firstName)?.IsList == true;
            var isSecondList = element?.GetNamedObjectRecursively(secondName)?.IsList == true;

            var firstEffectiveName = firstName;
            var secondEffectiveName = secondName;

            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.AutoNameCollisionListsAsSingle)
            {
                if (isFirstList && firstEffectiveName.EndsWith("List"))
                {
                    firstEffectiveName = firstEffectiveName.Substring(0, firstEffectiveName.Length - "List".Length);
                }
                if (isSecondList && secondEffectiveName.EndsWith("List"))
                {
                    secondEffectiveName = secondEffectiveName.Substring(0, secondEffectiveName.Length - "List".Length);
                }
            }

            // special case - always colliding
            if (secondName == null)
            {
                return $"{firstEffectiveName}AlwaysColliding";
            }
            else
            {
                return $"{firstEffectiveName}{firstSub}Vs{secondEffectiveName}{secondSub}";
            }
        }

        #endregion
    }
}
