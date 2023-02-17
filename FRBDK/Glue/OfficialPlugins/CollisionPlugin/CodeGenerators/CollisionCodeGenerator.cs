using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OfficialPlugins.CollisionPlugin
{
    public class CollisionCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            if(ShouldGenerateCollisionNameListCode(element as GlueElement))
            {
                codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.BeforeCollision += HandleBeforeCollisionGenerated;");
            }

            return codeBlock;
        }

        public static void GenerateInitializeCodeFor(GlueElement containerGlueElement, NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            var firstCollidable = namedObject.GetFirstCollidableObjectName();
            var secondCollidable = namedObject.GetSecondCollidableObjectName();

            //////////////Early Out/////////////////////
            if (string.IsNullOrEmpty(firstCollidable))
            {
                return;
            }


            ///////////End Early Out///////////////////

            T Get<T>(string name) => namedObject.Properties.GetValue<T>(name);

            var collisionType = (CollisionType)Get<int>(
                nameof(CollisionRelationshipViewModel.CollisionType));

            var firstMass = Get<float>(
                nameof(CollisionRelationshipViewModel.FirstCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var secondMass = Get<float>(
                nameof(CollisionRelationshipViewModel.SecondCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var elasticity = Get<float>(
                nameof(CollisionRelationshipViewModel.CollisionElasticity))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var softCollisionCoefficient = Get<float>(
                nameof(CollisionRelationshipViewModel.SoftCollisionCoefficient))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var firstSubCollision = Get<string>(
                nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));

            if (firstSubCollision == "<Entire Object>")
            {
                firstSubCollision = null;
            }

            var secondSubCollision = Get<string>(
                nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));

            if (secondSubCollision == "<Entire Object>")
            {
                secondSubCollision = null;
            }

            //var isCollisionActive = Get<bool>(
            //    nameof(CollisionRelationshipViewModel.IsCollisionActive));
            // Old projects do not have control over whether the collision relationship is active
            // therefore, we need to check if the property is there. If not, treat it as active:
            var collisionActiveProperty = namedObject.Properties
                .Find(item => item.Name == nameof(CollisionRelationshipViewModel.IsCollisionActive));
            var isCollisionActive = collisionActiveProperty == null ||
                (collisionActiveProperty.Value is bool asBool && asBool);

            var automaticPhysicsProperty = namedObject.Properties
                .Find(item => item.Name == nameof(CollisionRelationshipViewModel.IsAutomaticallyApplyPhysicsChecked));

            var isAutomaticPhysics = automaticPhysicsProperty == null ||
                (automaticPhysicsProperty.Value is bool automaticAsBool && automaticAsBool);

            var collisionLimit = (FlatRedBall.Math.Collision.CollisionLimit)Get<int>(
                nameof(CollisionRelationshipViewModel.CollisionLimit));

            var listVsListLoopingMode = (FlatRedBall.Math.Collision.ListVsListLoopingMode)Get<int>(
                nameof(CollisionRelationshipViewModel.ListVsListLoopingMode));

            var groupPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.GroundPlatformerVariableName));
            var airPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.AirPlatformerVariableName));
            var afterDoubleJumpPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.AfterDoubleJumpPlatformerVariableName));



            var instanceName = namedObject.InstanceName;

            bool isFirstList;
            var firstType = AssetTypeInfoManager.GetFirstGenericType(namedObject, out isFirstList);

            bool isSecondList;
            var secondType = AssetTypeInfoManager.GetSecondGenericType(namedObject, out isSecondList);

            var isFirstTileShapeCollection = firstType == "FlatRedBall.TileCollisions.TileShapeCollection";
            var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

            var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
            var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

            var isAlwaysColliding = secondCollidable == null;

            bool shouldManuallyAddToCollisionManager = false;

            if (collisionType == CollisionType.PlatformerSolidCollision ||
                collisionType == CollisionType.PlatformerCloudCollision)
            {
                GeneratePlatformerCollision(codeBlock, firstCollidable, secondCollidable, firstSubCollision, collisionType, instanceName, isFirstList, firstType, isSecondList, secondType);
            }
            else if (collisionType == CollisionType.StackingCollision)
            {
                GenerateStackingCollision(codeBlock, firstCollidable, secondCollidable, namedObject, isFirstList, firstType, isSecondList, secondType);
            }
            else if (collisionType == CollisionType.DelegateCollision)
            {
                if (isFirstList && isSecondList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateListVsListRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else if (isFirstList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else if (isSecondList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateCollisionRelationshipBase<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }

                shouldManuallyAddToCollisionManager = true;

            }
            else if (isSecondTileShapeCollection)
            {
                // It's possible the second is null since these often come from maps, and the level editor could load something invalid:
                codeBlock = codeBlock.If($"{secondCollidable} != null");
                // same method used for both list and non-list
                codeBlock.Line($"{instanceName} = " +
                    $"FlatRedBall.Math.Collision.CollisionManagerTileShapeCollectionExtensions.CreateTileRelationship(" +
                    $"FlatRedBall.Math.Collision.CollisionManager.Self, " +
                    $"{firstCollidable}, {secondCollidable});");

            }
            else if (isAlwaysColliding)
            {
                codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.AlwaysCollidingListCollisionRelationship<{firstType}>({firstCollidable});");
                shouldManuallyAddToCollisionManager = true;
            }
            //else if(isSecondShapeCollection)
            //{
            //    codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
            //        $"{firstCollidable});");
            //}
            else
            {
                codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
                    $"{firstCollidable}, {secondCollidable});");
            }

            if (shouldManuallyAddToCollisionManager)
            {
                codeBlock.Line($"FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add({instanceName});");
            }

            var doesGluxSupportNamedSubcollisions =
                FlatRedBall.Glue.Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProject.FileVersion >=
                (int)GlueProjectSave.GluxVersions.SupportsNamedSubcollisions;

            if (!string.IsNullOrEmpty(firstSubCollision) &&
                firstSubCollision != CollisionRelationshipViewModel.EntireObject &&
                collisionType != CollisionType.PlatformerCloudCollision &&
                collisionType != CollisionType.PlatformerSolidCollision)
            {
                if (doesGluxSupportNamedSubcollisions)
                {
                    codeBlock.Line($"{instanceName}.SetFirstSubCollision(item => item.{firstSubCollision}, \"{firstSubCollision}\");");
                }
                else
                {
                    codeBlock.Line($"{instanceName}.SetFirstSubCollision(item => item.{firstSubCollision});");
                }
            }
            if (!string.IsNullOrEmpty(secondSubCollision) &&
                secondSubCollision != CollisionRelationshipViewModel.EntireObject &&
                collisionType != CollisionType.PlatformerCloudCollision &&
                collisionType != CollisionType.PlatformerSolidCollision &&
                // delegate collision cannot specify subcollision, that can be done manually in the collision
                collisionType != CollisionType.DelegateCollision)
            {
                if (doesGluxSupportNamedSubcollisions)
                {
                    codeBlock.Line($"{instanceName}.SetSecondSubCollision(item => item.{secondSubCollision}, \"{secondSubCollision}\");");
                }
                else
                {
                    codeBlock.Line($"{instanceName}.SetSecondSubCollision(item => item.{secondSubCollision});");
                }
            }

            if (isFirstList && isSecondList)
            {
                codeBlock.Line($"{instanceName}.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.{collisionLimit};");

                // currently list vs list delegate collision doesn't support the looping mode:
                var supportsLoopingMode = collisionType != CollisionType.PlatformerSolidCollision &&
                    collisionType != CollisionType.PlatformerCloudCollision;
                if (supportsLoopingMode)
                {
                    codeBlock.Line($"{instanceName}.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.{listVsListLoopingMode};");
                }
            }

            codeBlock.Line($"{instanceName}.Name = \"{instanceName}\";");



            switch (collisionType)
            {
                case CollisionType.NoPhysics:
                    // don't do anything
                    break;
                case CollisionType.MoveCollision:

                    codeBlock.Line($"{instanceName}.SetMoveCollision({firstMass}, {secondMass});");
                    break;
                case CollisionType.MoveSoftCollision:
                    if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CollisionRelationshipsSupportMoveSoft)
                    {
                        codeBlock.Line($"{instanceName}.SetMoveSoftCollision({firstMass}, {secondMass}, {softCollisionCoefficient});");
                    }
                    else
                    {
                        codeBlock.Line(
                            $"This project is version {GlueState.Self.CurrentGlueProject.FileVersion} " +
                            $"but must be of version {(int)GlueProjectSave.GluxVersions.CollisionRelationshipsSupportMoveSoft} to use move soft collision in codegen");
                    }
                    break;
                case CollisionType.BounceCollision:
                    //var relationship = new FlatRedBall.Math.Collision.CollisionRelationship();
                    //relationship.SetBounceCollision(firstMass, secondMass, elasticity);
                    codeBlock.Line($"{instanceName}.SetBounceCollision({firstMass}, {secondMass}, {elasticity});");
                    break;
            }

            if (!isCollisionActive)
            {
                codeBlock.Line(
                    $"{instanceName}.{nameof(FlatRedBall.Math.Collision.CollisionRelationship.IsActive)} = false;");
            }

            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.CollisionRelationshipManualPhysics &&
                !isAutomaticPhysics)
            {
                codeBlock.Line(
                    $"{instanceName}.{nameof(FlatRedBall.Math.Collision.CollisionRelationship.ArePhysicsAppliedAutomatically)} = false;");
            }

            GeneratePlatformerCode(codeBlock, groupPlatformerVariableName, airPlatformerVariableName, afterDoubleJumpPlatformerVariableName, instanceName, firstType, isAlwaysColliding);

            GenerateDamageDealingCode(codeBlock, namedObject, instanceName, firstType, secondType);
        }

        private static void GeneratePlatformerCode(ICodeBlock codeBlock, string groupPlatformerVariableName, string airPlatformerVariableName, string afterDoubleJumpPlatformerVariableName, string instanceName, string firstType, bool isAlwaysColliding)
        {
            if (!string.IsNullOrEmpty(groupPlatformerVariableName) ||
                            !string.IsNullOrEmpty(airPlatformerVariableName) ||
                            !string.IsNullOrEmpty(afterDoubleJumpPlatformerVariableName))
            {
                string StrippedName(string nameWithCsv)
                {
                    if (nameWithCsv?.Contains(" in ") == true)
                    {
                        var index = nameWithCsv.IndexOf(" in ");
                        return nameWithCsv.Substring(0, index);
                    }
                    else
                    {
                        return nameWithCsv;
                    }
                }

                if (isAlwaysColliding)
                {
                    codeBlock.Line(
                        $"{instanceName}.CollisionOccurred += (first) =>");
                }
                else
                {
                    codeBlock.Line(
                        $"{instanceName}.CollisionOccurred += (first, second) =>");
                }
                var eventBlock = codeBlock.Block();

                string GetRightSide(string variableName)
                {
                    if (variableName == "<NULL>")
                    {
                        return "null";
                    }
                    else
                    {
                        return $"{firstType}.PlatformerValuesStatic[\"{StrippedName(variableName)}\"]";
                    }
                }

                if (!string.IsNullOrEmpty(groupPlatformerVariableName))
                {
                    eventBlock.Line(
                        $"first.GroundMovement = {GetRightSide(groupPlatformerVariableName)};");
                }
                if (!string.IsNullOrEmpty(airPlatformerVariableName))
                {
                    eventBlock.Line(
                        $"first.AirMovement = {GetRightSide(airPlatformerVariableName)};");
                }
                if (!string.IsNullOrEmpty(afterDoubleJumpPlatformerVariableName))
                {
                    eventBlock.Line(
                        $"first.AfterDoubleJump = {GetRightSide(afterDoubleJumpPlatformerVariableName)};");
                }
                codeBlock.Line(";");

            }
        }

        private static void GenerateDamageDealingCode(ICodeBlock codeBlock, NamedObjectSave namedObject, string instanceName, string firstType, string secondType)
        {
            T Get<T>(string name) => namedObject.Properties.GetValue<T>(name);

            var firstEntityType = ObjectFinder.Self.GetEntitySave(firstType?.Replace(".", "/"));
            var secondEntityType = ObjectFinder.Self.GetEntitySave(secondType?.Replace(".", "/"));

            bool isFirstDamageable = firstEntityType?.GetPropertyValue("ImplementsIDamageable") as bool? == true;
            bool isFirstDamageArea = firstEntityType?.GetPropertyValue("ImplementsIDamageArea") as bool? == true;

            bool isSecondDamageable = secondEntityType?.GetPropertyValue("ImplementsIDamageable") as bool? == true;
            bool isSecondDamageArea = secondEntityType?.GetPropertyValue("ImplementsIDamageArea") as bool? == true;

            var dealDamageInGeneratedCode = Get<bool>(nameof(CollisionRelationshipViewModel.IsDealDamageChecked));
            var destroyFirst = Get<bool>(nameof(CollisionRelationshipViewModel.IsDestroyFirstOnDamageChecked));
            var destroySecond = Get<bool>(nameof(CollisionRelationshipViewModel.IsDestroySecondOnDamageChecked));

            var firstCollisionDestroyType = Get< CollisionDestroyType>(nameof(CollisionRelationshipViewModel.FirstCollisionDestroyType));
            var secondCollisionDestroyType = Get<CollisionDestroyType>(nameof(CollisionRelationshipViewModel.SecondCollisionDestroyType));

            var shouldGenerateEvent = dealDamageInGeneratedCode || 
                (destroyFirst && isFirstDamageArea) ||
                (destroySecond && isSecondDamageArea);

            // Only generate if there is a secondType. If it's an always-colliding event (second type is null), damage can't be dealt:
            shouldGenerateEvent = shouldGenerateEvent && !string.IsNullOrEmpty(secondType);

            ICodeBlock eventBlock = null;

            if(shouldGenerateEvent)
            {
                codeBlock.Line($"{instanceName}.CollisionOccurred += (first, second) =>");
                eventBlock = codeBlock.Block();
            }

            if(shouldGenerateEvent)
            {
                var firstTakesDamage = isFirstDamageable && isSecondDamageArea;
                var secondTakesDamage = isSecondDamageable && isFirstDamageArea;
                if(firstTakesDamage && dealDamageInGeneratedCode)
                {
                    var ifBlock = eventBlock.If("FlatRedBall.Entities.DamageableExtensionMethods.ShouldTakeDamage(first, second)");
                    if(destroySecond)
                    {
                        if(secondCollisionDestroyType == CollisionDestroyType.Always)
                        {
                            ifBlock.Line("FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(first, second);");
                            ifBlock.Line("second.RemovedByCollision?.Invoke(first);");
                            ifBlock.Line("second.Destroy();");
                        }
                        else if(secondCollisionDestroyType == CollisionDestroyType.OnlyIfDealtDamage)
                        {
                            ifBlock.Line("var damageDealt = FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(first, second);");
                            var innerIf = ifBlock.If("damageDealt > 0");
                            innerIf.Line("second.RemovedByCollision?.Invoke(first);");
                            innerIf.Line("second.Destroy();");
                        }
                    }
                    else
                    {
                        ifBlock.Line("FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(first, second);");
                    }
                    // Do we want this to be conditional? Let's make it always on for now and see if users complain...
                    ifBlock.If("first.CurrentHealth <= 0").Line("first.Destroy();");
                }
                else if(destroySecond && isSecondDamageArea)
                {
                    eventBlock.Line("second.RemovedByCollision?.Invoke(null);");
                    eventBlock.Line("second.Destroy();");
                }

                if (secondTakesDamage && dealDamageInGeneratedCode)
                {
                    var ifBlock = eventBlock.If("FlatRedBall.Entities.DamageableExtensionMethods.ShouldTakeDamage(second, first)");
                    if(destroyFirst)
                    {
                        if(firstCollisionDestroyType == CollisionDestroyType.Always)
                        {
                            ifBlock.Line("FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(second, first);");
                            ifBlock.Line("first.RemovedByCollision?.Invoke(second);");
                            ifBlock.Line("first.Destroy();");
                        }
                        else if(firstCollisionDestroyType == CollisionDestroyType.OnlyIfDealtDamage)
                        {
                            ifBlock.Line("var damageDealt = FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(second, first);");
                            var innerIf = ifBlock.If("damageDealt > 0");
                            innerIf.Line("first.RemovedByCollision?.Invoke(second);");
                            innerIf.Line("first.Destroy();");
                        }
                    }
                    else
                    {
                        ifBlock.Line("FlatRedBall.Entities.DamageableExtensionMethods.TakeDamage(second, first);");
                    }
                    ifBlock.If("second.CurrentHealth <= 0").Line("second.Destroy();");
                }
                else if(destroyFirst && isFirstDamageArea)
                {
                    eventBlock.Line("first.RemovedByCollision?.Invoke(null);");
                    eventBlock.Line("first.Destroy();");
                }
                codeBlock.Line(";");
            }

        }

        private static void GeneratePlatformerCollision(ICodeBlock codeBlock,
            string firstCollidable, string secondCollidable,
            string firstSubCollision, CollisionType collisionType,
            string instanceName, bool isFirstList, string firstType, bool isSecondList, string secondType)
        {
            var block = codeBlock.Block();

            var effectiveFirstType = firstType;
            if (isFirstList)
            {
                effectiveFirstType = $"FlatRedBall.Math.PositionedObjectList<{firstType}>";
            }
            var effectiveSecondType = secondType;
            if (isSecondList)
            {
                effectiveSecondType = $"FlatRedBall.Math.PositionedObjectList<{secondType}>";
            }


            var relationshipType =
                $"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{effectiveFirstType}, {effectiveSecondType}>";

            if (isFirstList && isSecondList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsListRelationship<{firstType}, {secondType}>";
            }
            else if (isFirstList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>";
            }
            else if (isSecondList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<{firstType}, {secondType}>";
            }

            block.Line($"var temp = new {relationshipType}({firstCollidable}, {secondCollidable});");
            // This causes a closure which allocates!
            //block.Line($"var isCloud = {(collisionType == CollisionType.PlatformerCloudCollision).ToString().ToLowerInvariant()};");
            var collisionFunctionName = $"{firstCollidable}v{secondCollidable}PlatformFunction";
            block.Line($"temp.CollisionFunction = {collisionFunctionName};");
            // use firstType and secondType because the collision function is not called on the lists but on the individuals
            block.Line($"static bool {collisionFunctionName}({firstType} first, {secondType} second)");
            block = block.Block();

            var isCloud = (collisionType == CollisionType.PlatformerCloudCollision).ToString().ToLowerInvariant();
            string whatToCollideAgainst = "second";

            if (!isFirstList && isSecondList)
            {
                if (collisionType == CollisionType.PlatformerCloudCollision || collisionType == CollisionType.PlatformerSolidCollision)
                {
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, {isCloud});");
                }
                else
                {
                    // list vs list is internally handled already
                    if (firstSubCollision == null)
                    {
                        // it's an icollidable probably
                        block.Line($"return first.CollideAgainst({whatToCollideAgainst}.Collision, {isCloud});");
                    }
                    else
                    {
                        block.Line($"return first.CollideAgainst({whatToCollideAgainst}.Collision, first.{firstSubCollision}, {isCloud});");
                    }

                }
            }
            else // even if the first is list, we don't loop because we use a collision relationship type that handles the looping internally
            {
                if (firstSubCollision == null)
                {
                    // assume it's a shape collection
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, {isCloud});");
                }
                else
                {
                    // assume it's a shape collection
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, first.{firstSubCollision}, {isCloud});");
                }
            }

            block = block.End();
            block.Line(";");

            block.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(temp);");
            block.Line($"{instanceName} = temp;");
            //CollisionManager.Self.Relationships.Add(PlayerVsSolidCollision);
        }

        private static void GenerateStackingCollision(ICodeBlock codeBlock,

            string firstCollidable, string secondCollidable,
            NamedObjectSave namedObjectSave, bool isFirstList, string firstType, bool isSecondList, string secondType)
        {
            var instanceName = namedObjectSave.InstanceName;


            var block = codeBlock.Block();

            var effectiveFirstType = firstType;
            if (isFirstList)
            {
                effectiveFirstType = $"FlatRedBall.Math.PositionedObjectList<{firstType}>";
            }
            var effectiveSecondType = secondType;
            if (isSecondList)
            {
                effectiveSecondType = $"FlatRedBall.Math.PositionedObjectList<{secondType}>";
            }

            string relationshipType;
            relationshipType = namedObjectSave
                //.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType;
                .SourceClassType;
            //if(isFirstList)
            //{
            //    relationshipType =
            //        //$"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{effectiveFirstType}, {effectiveSecondType}>";
            //        // Since we create a list delegate if necessary, we don't use the list type:
            //        $"FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>";


            //}

            //else
            //{
            //    relationshipType =
            //        //$"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{effectiveFirstType}, {effectiveSecondType}>";
            //        // Since we create a list delegate if necessary, we don't use the list type:
            //        $"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{firstType}, {secondType}>";


            //}

            block.Line($"var temp = new {relationshipType}({firstCollidable}, {secondCollidable});");
            block.Line($"temp.CollisionFunction = (first, second) =>");
            block = block.Block();

            var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection" || secondType == "TileShapeCollection";

            if (isSecondTileShapeCollection == false)
            {
                // return stackable vs stackable
                block.Line($"return FlatRedBall.Math.Geometry.IStackableExtensionMethods.CollideAgainstBounceStackable(first, second, 1, 1);");
            }
            else
            {
                // stackable vs TileShapeCollection
                block.Line($"return second.CollideAgainstSolid(first);");
            }

            block = block.End();
            block.Line(";");

            block.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(temp);");
            block.Line($"{instanceName} = temp;");
        }

        public static bool CanBePartitioned(NamedObjectSave nos)
        {
            if (nos.IsList)
            {
                var genericType = nos.SourceClassGenericType;

                var entity = ObjectFinder.Self.GetEntitySave(genericType);

                // todo - what about inheritance? We may need to handle that here.
                return entity?.ImplementsICollidable == true;
            }

            return false;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            // we only care about the top-level
            foreach (var nos in element.NamedObjects)
            {
                if (CanBePartitioned(nos))
                {
                    T Get<T>(string propName) =>
                        nos.Properties.GetValue<T>(propName);

                    if (Get<bool>(nameof(CollidableNamedObjectRelationshipViewModel.PerformCollisionPartitioning)))
                    {
                        var sortAxis = Get<Axis>(nameof(CollidableNamedObjectRelationshipViewModel.SortAxis));
                        var sortEveryFrame = Get<bool>(nameof(CollidableNamedObjectRelationshipViewModel.IsSortListEveryFrameChecked));
                        var automaticOrManual = Get<PartitioningAutomaticManual>(nameof(CollidableNamedObjectRelationshipViewModel.PartitioningAutomaticManual));

                        float partitionWidthHeight;
                        if(automaticOrManual == PartitioningAutomaticManual.Automatic)
                        {
                            partitionWidthHeight = AutomatedCollisionSizeLogic.GetAutomaticCollisionWidthHeight(
                                nos, sortAxis);
                        }
                        else
                        {
                            partitionWidthHeight = Get<float>(nameof(CollidableNamedObjectRelationshipViewModel.PartitionWidthHeight));
                        }

                        // fill in this line:
                        codeBlock.Line(
                            $"FlatRedBall.Math.Collision.CollisionManager.Self.Partition({nos.InstanceName}, FlatRedBall.Math.Axis.{sortAxis}, " +
                            $"{CodeParser.ConvertValueToCodeString(partitionWidthHeight)}, {sortEveryFrame.ToString().ToLowerInvariant()});");
                    }
                }
            }

            return codeBlock;
        }

        static bool IsTileShapeCollection(NamedObjectSave nos) => nos?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection";

        bool ShouldGenerateCollisionNameListCode(GlueElement element) =>
            GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ICollidableHasItemsCollidedAgainst &&
            element.NamedObjects.Any(item => item.IsCollisionRelationship());

        
        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            if(ShouldGenerateCollisionNameListCode(element as GlueElement))
            {
                codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.BeforeCollision -= HandleBeforeCollisionGenerated;");
            }

            if (element is ScreenSave)
            {
                codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();");
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            if(ShouldGenerateCollisionNameListCode(element as GlueElement))
            {
                codeBlock = codeBlock.Function("void", "HandleBeforeCollisionGenerated");
                var itemsToGenerate = element
                    // Only use the top level NamedObjects. Don't go into the individual objects since those will be handled by their list
                    .NamedObjects
                    .Where(item => item.IsCollidableOrCollidableList() && !IsTileShapeCollection(item) && !item.DefinedByBase && !item.IsDisabled);

                var hasObjectsCollidedAgainst = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ICollidableHasObjectsCollidedAgainst;
                foreach (var item in itemsToGenerate)
                {
                    if (item.IsList)
                    {
                        var forBlock = codeBlock.For($"int i = 0; i < {item.FieldName}.Count; i++");
                        forBlock.Line($"var item = {item.FieldName}[i];");

                        if (IsPlatformer(item.SourceClassGenericType))
                        {
                            forBlock.Line("item.GroundCollidedAgainst.Clear();");
                        }

                        // If-check Handled above in ShouldGenerateCollisionNameListCode
                        forBlock.Line($"item.LastFrameItemsCollidedAgainst.Clear();");
                        {
                            var innerForeach = forBlock.ForEach("var name in item.ItemsCollidedAgainst");
                            innerForeach.Line("item.LastFrameItemsCollidedAgainst.Add(name);");
                        }
                        forBlock.Line($"item.ItemsCollidedAgainst.Clear();");
                        if (hasObjectsCollidedAgainst)
                        {
                            forBlock.Line($"item.LastFrameObjectsCollidedAgainst.Clear();");
                            {
                                var innerForeach = forBlock.ForEach("var name in item.ObjectsCollidedAgainst");
                                innerForeach.Line("item.LastFrameObjectsCollidedAgainst.Add(name);");
                            }
                            forBlock.Line($"item.ObjectsCollidedAgainst.Clear();");
                        }

                    }
                    else
                    {
                        if(IsPlatformer(item.SourceClassType))
                        {
                            codeBlock.Line($"{item.FieldName}.GroundCollidedAgainst.Clear();");

                        }

                        // If-check Handled above in ShouldGenerateCollisionNameListCode
                        codeBlock.Line($"{item.FieldName}.LastFrameItemsCollidedAgainst.Clear();");
                        {
                            var innerForeach = codeBlock.ForEach($"var name in {item.FieldName}.ItemsCollidedAgainst");
                            innerForeach.Line($"{item.FieldName}.LastFrameItemsCollidedAgainst.Add(name);");
                        }
                        codeBlock.Line($"{item.FieldName}.ItemsCollidedAgainst.Clear();");

                        if(hasObjectsCollidedAgainst)
                        {
                            codeBlock.Line($"{item.FieldName}.LastFrameObjectsCollidedAgainst.Clear();");
                            {
                                var innerForeach = codeBlock.ForEach($"var name in {item.FieldName}.ObjectsCollidedAgainst");
                                innerForeach.Line($"{item.FieldName}.LastFrameObjectsCollidedAgainst.Add(name);");
                            }
                            codeBlock.Line($"{item.FieldName}.ObjectsCollidedAgainst.Clear();");
                        }

                    }
                }

            }


            return codeBlock;
        }

        private bool IsPlatformer(string sourceClassGenericType)
        {
            var entity = ObjectFinder.Self.GetEntitySave(sourceClassGenericType);

            return entity?.Properties.GetValue<bool>("IsPlatformer") == true;

        }
    }
}
