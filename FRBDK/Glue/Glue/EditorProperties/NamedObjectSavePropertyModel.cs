using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// The properties the editor shows for a selected object (<see cref="NamedObjectSave"/>), not its variables: which
    /// apply to the object in its container, which can change, and the values offered. Plugins add or hide properties
    /// through <c>PluginBase.AdjustNamedObjectProperties</c>. The WinForms Properties grid
    /// (<c>NamedObjectPropertyGridDisplayer</c>) renders them and GlueCli's <c>set-property</c> sets them.
    /// </summary>
    public class NamedObjectSavePropertyModel
    {
        public const string LayerCategory = "Layer";

        readonly IGlueState glueState;
        readonly IAvailableAssetTypesCore assetTypes;
        readonly Action<NamedObjectSave, EditorPropertyListBuilder> adjustByPlugins;

        /// <param name="adjustByPlugins">Lets plugins add or hide properties (PluginManager.AdjustNamedObjectProperties).</param>
        public NamedObjectSavePropertyModel(IGlueState glueState, IAvailableAssetTypesCore assetTypes,
            Action<NamedObjectSave, EditorPropertyListBuilder> adjustByPlugins)
        {
            this.glueState = glueState;
            this.assetTypes = assetTypes;
            this.adjustByPlugins = adjustByPlugins;
        }

        /// <param name="element">The screen or entity holding <paramref name="nos"/>, whose files and Layers are offered.</param>
        public IReadOnlyList<EditorProperty> GetProperties(NamedObjectSave nos, GlueElement element)
        {
            var builder = new EditorPropertyListBuilder(nos);

            if (glueState.CurrentGlueProject.FileVersion < (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool)
            {
                builder.Exclude(nameof(NamedObjectSave.AssociateWithFactory));
            }

            builder.Exclude(nameof(NamedObjectSave.InstructionSaves));
            builder.Exclude(nameof(NamedObjectSave.Properties));
            builder.Exclude(nameof(NamedObjectSave.IsNodeHidden));
            builder.SetReadOnly(nameof(NamedObjectSave.DefinedByBase));
            builder.SetReadOnly(nameof(NamedObjectSave.InstantiatedByBase));

            var assetTypeInfo = nos.GetAssetTypeInfo();
            var containerType = nos.GetContainerType();

            var includeSourceClassType = true;
            var includeSourceFile = true;
            var includeSourceName = true;
            var includeSourceClassGenericType = true;
            var includeCurrentState = true;
            var includeSetByContainer = containerType == ContainerType.Entity;

            var includeIClickable = containerType == ContainerType.Entity && assetTypeInfo?.HasCursorIsOn != false;
            // Shapes don't implement ICollidable, but they have collision.
            var isShape = assetTypeInfo?.QualifiedRuntimeTypeName.QualifiedType?.StartsWith("FlatRedBall.Math.Geometry") ?? false;
            var includeICollidable = assetTypeInfo == null || assetTypeInfo.ImplementsICollidable || isShape;

            if (containerType != ContainerType.Entity || nos.IsList || nos.SetByDerived)
            {
                builder.Exclude(nameof(NamedObjectSave.AttachToContainer));
            }
            // Objects in entities can't be attached to the Camera.
            if (containerType != ContainerType.Screen)
            {
                builder.Exclude(nameof(NamedObjectSave.AttachToCamera));
            }

            if (nos.InstantiatedByBase)
            {
                builder.Exclude(nameof(NamedObjectSave.SourceType));
                includeSourceClassType = false;
                includeSourceClassGenericType = false;

                builder.Exclude(nameof(NamedObjectSave.InstanceName));
                builder.Exclude(nameof(NamedObjectSave.CallActivity));
                builder.Exclude(nameof(NamedObjectSave.IgnoresPausing));
                builder.Exclude(nameof(NamedObjectSave.HasPublicProperty));
                builder.Exclude(nameof(NamedObjectSave.ExposedInDerived));
                builder.Exclude(nameof(NamedObjectSave.SetByDerived));
                builder.Exclude(nameof(NamedObjectSave.SetByContainer));
            }

            if (nos.InstantiatedByBase || nos.IsList)
            {
                builder.Exclude(nameof(NamedObjectSave.AddToManagers));
            }

            AddLayerProperties(builder, nos, element);

            if (assetTypeInfo != AvailableAssetTypes.CommonAtis.Camera)
            {
                builder.Exclude(nameof(NamedObjectSave.IsNewCamera));
            }
            if (nos.SourceType != SourceType.FlatRedBallType || assetTypeInfo != AvailableAssetTypes.CommonAtis.Text)
            {
                builder.Exclude(nameof(NamedObjectSave.IsPixelPerfect));
            }

            // Sprites from a file go on the z buffer as the file says.
            var includeIsZBuffered = nos.SourceType == SourceType.FlatRedBallType;
            var includeGenerateTimedEmit =
                (nos.SourceType == SourceType.FlatRedBallType && nos.SourceClassType == "Emitter") ||
                (nos.SourceType == SourceType.File && nos.ClassType == "Emitter");

            if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.Gum)
            {
                includeCurrentState = false;
                includeSourceFile = false;
                includeSourceName = false;
                if (!nos.IsGenericType)
                {
                    includeSourceClassGenericType = false;
                }
            }
            else if (nos.SourceType == SourceType.File)
            {
                includeCurrentState = false;
                includeSourceClassType = false;
                includeSourceClassGenericType = false;
            }
            else if (nos.SourceType == SourceType.Entity)
            {
                includeSourceFile = false;
                includeSourceName = false;
                includeSourceClassGenericType = false;
                includeCurrentState = HasStates(nos);
            }

            IncludeOrExclude(builder, nameof(NamedObjectSave.SourceClassType), includeSourceClassType, () => GetSourceClassTypeOptions(nos.SourceType));
            IncludeOrExclude(builder, nameof(NamedObjectSave.SourceFile), includeSourceFile,
                () => AvailableFileStringConverter.GetAvailableOptions(element, includeNoneOption: true, removePathAndExtension: false));
            IncludeOrExclude(builder, nameof(NamedObjectSave.GenerateTimedEmit), includeGenerateTimedEmit, null);
            IncludeOrExclude(builder, nameof(NamedObjectSave.SourceName), includeSourceName,
                () => AvailableNameablesStringConverter.GetAvailableNamedObjectSourceNames(nos));
            IncludeOrExclude(builder, nameof(NamedObjectSave.SourceClassGenericType), includeSourceClassGenericType,
                () => new AvailableClassGenericTypeConverter().GetAvailableValues(true));
            IncludeOrExclude(builder, nameof(NamedObjectSave.CurrentState), includeCurrentState, () => GetStateOptions(nos));

            if (!includeIClickable)
            {
                builder.Exclude(nameof(NamedObjectSave.IncludeInIClickable));
            }
            if (!includeICollidable)
            {
                builder.Exclude(nameof(NamedObjectSave.IncludeInICollidable));
            }
            if (!includeIsZBuffered)
            {
                builder.Exclude(nameof(NamedObjectSave.IsZBuffered));
            }
            if (!includeSetByContainer)
            {
                builder.Exclude(nameof(NamedObjectSave.SetByContainer));
            }
            if (string.IsNullOrEmpty(assetTypeInfo?.AddManuallyUpdatedMethod))
            {
                builder.Exclude(nameof(NamedObjectSave.IsManuallyUpdated));
            }

            adjustByPlugins(nos, builder);

            return builder.Build();
        }

        static void IncludeOrExclude(EditorPropertyListBuilder builder, string name, bool include, Func<IReadOnlyList<string>>? options)
        {
            if (include)
            {
                builder.Include(name, options);
            }
            else
            {
                builder.Exclude(name);
            }
        }

        static void AddLayerProperties(EditorPropertyListBuilder builder, NamedObjectSave nos, GlueElement element)
        {
            if (!nos.IsLayer && !nos.IsList)
            {
                builder.Include(nameof(NamedObjectSave.LayerOn), () => AvailableLayersTypeConverter.GetAvailableLayers(element), LayerCategory);
            }
            else
            {
                builder.Exclude(nameof(NamedObjectSave.LayerOn));
            }

            if (!nos.IsLayer)
            {
                builder.Exclude(nameof(NamedObjectSave.IndependentOfCamera));
                builder.Exclude(nameof(NamedObjectSave.Is2D));
                builder.Exclude(nameof(NamedObjectSave.DestinationRectangle));
                builder.Exclude(nameof(NamedObjectSave.LayerCoordinateType));
                builder.Exclude(nameof(NamedObjectSave.LayerCoordinateUnit));
                return;
            }

            builder.Include(nameof(NamedObjectSave.IndependentOfCamera), category: LayerCategory);
            if (!nos.IndependentOfCamera)
            {
                builder.Exclude(nameof(NamedObjectSave.Is2D));
                return;
            }

            builder.Include(nameof(NamedObjectSave.Is2D), category: LayerCategory);
            if (nos.Is2D)
            {
                builder.Include(nameof(NamedObjectSave.DestinationRectangle), category: LayerCategory);
                builder.Include(nameof(NamedObjectSave.LayerCoordinateUnit), category: LayerCategory);
                builder.Include(nameof(NamedObjectSave.LayerCoordinateType), category: LayerCategory);
            }
            else
            {
                builder.Exclude(nameof(NamedObjectSave.DestinationRectangle));
                builder.Exclude(nameof(NamedObjectSave.LayerCoordinateType));
            }
        }

        /// <summary>&lt;NONE&gt; and, sorted, the FRB types an object can be (FlatRedBallType, Gum) or every entity (Entity).</summary>
        IReadOnlyList<string> GetSourceClassTypeOptions(SourceType sourceType)
        {
            IEnumerable<string> types = sourceType switch
            {
                SourceType.FlatRedBallType or SourceType.Gum => assetTypes.AllAssetTypes.Where(item => item.CanBeObject)
                    .Select(item => item.QualifiedRuntimeTypeName.QualifiedType).Distinct(),
                SourceType.Entity => glueState.CurrentGlueProject.Entities.Select(item => item.Name),
                _ => Enumerable.Empty<string>(),
            };
            return new[] { "<NONE>" }.Concat(types.OrderBy(item => item)).ToList();
        }

        static IReadOnlyList<string> GetStateOptions(NamedObjectSave nos)
        {
            var states = new List<string>();
            AvailableStates.FillPossibleStatesFor(states, nameof(NamedObjectSave.CurrentState), nos);
            return states;
        }

        /// <summary>Whether the entity <paramref name="nos"/> is, or one of its bases, has states.</summary>
        static bool HasStates(NamedObjectSave nos)
        {
            IElement? element = nos.GetReferencedElement();
            while (element != null)
            {
                if (element.States.Count != 0)
                {
                    return true;
                }
                element = ObjectFinder.Self.GetIElement(element.BaseElement);
            }
            return false;
        }

        /// <summary>
        /// Whether <paramref name="memberName"/> of an entity instance is a variable whose type is a CSV, so its value
        /// stays text instead of being converted.
        /// </summary>
        public static bool GetIfIsCsv(NamedObjectSave instance, string memberName)
        {
            if (instance.SourceType == SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
            {
                var entity = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);
                // The member may be an object set by container rather than a variable.
                var customVariable = entity?.GetCustomVariableRecursively(memberName);
                if (customVariable != null)
                {
                    return customVariable.GetIsCsv();
                }
            }
            return false;
        }
    }
}
