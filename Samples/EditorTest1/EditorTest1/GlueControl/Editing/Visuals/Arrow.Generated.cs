#define IncludeSetVariable
// The following #defines come from the version of your GLUJ/GLUX file. For more information see https://docs.flatredball.com/flatredball/glue-reference/glujglux
#define PreVersion
#define HasFormsObject
#define AddedGeneratedGame1
#define ListsHaveAssociateWithFactoryBool
#define GumGueHasGetAnimation
#define CsvInheritanceSupport
#define IPositionedSizedObjectInEngine
#define NugetPackageInCsproj
#define SupportsEditMode
#define SupportsShapeCollectionAddToManagerMakeAutomaticallyUpdated
#define ScreensHaveActivityEditMode
#define SupportsNamedSubcollisions
#define TimeManagerHasDelaySeconds
#define GumTextHasIsBold
#define GlueSavedToJson
#define IEntityInFrb
#define SeparateJsonFilesForElements
#define GumSupportsAchxAnimation
#define StartupInGeneratedGame
#define RemoveAutoLocalizationOfVariables
#define GumHasMIsLayoutSuspendedPublic
#define SpriteHasUseAnimationTextureFlip
#define RemoveIsScrollableEntityList
#define HasGetGridLine
#define HasScreenManagerAfterScreenDestroyed
#define ScreenManagerHasPersistentPolygons
#define ShapeManagerCollideAgainstClosest
#define SpriteHasTolerateMissingAnimations
#define AnimationLayerHasName
#define IPlatformer
#define GumDefaults2
#define IStackableInEngine
#define ICollidableHasItemsCollidedAgainst
#define CollisionRelationshipManualPhysics
#define GumSupportsStackSpacing
#define CollisionRelationshipsSupportMoveSoft
#define GeneratedCameraSetupFile
#define ShapeCollectionHasMaxAxisAlignedRectanglesRadiusX
#define AutoNameCollisionListsAsSingle
#define GumHasIgnoredByParentSize
#define GumTextObjectsUpdateTextWith0ChildDepth
#define HasFrameworkElementManager
#define HasGumSkiaElements
#define ITiledTileMetadataInFrb
#define DamageableHasHealth
#define HasGame1GenerateEarly
#define ICollidableHasObjectsCollidedAgainst
#define HasIRepeatPressableInput
#define AllTiledFilesGenerated
#define RemoveRedundantDerivedData
#define GraphicalUiElementProtectedAnimationProperties
#define GraphicalUiElementINotifyPropertyChanged
#define GumTextObjectsHaveTextOverflowProperties
#define TileShapeCollectionIsICollidable
#define TileShapeCollectionAddToLayerSupportsAutomaticallyUpdated
#define ISongInFrb
#define RendererHasExternalEffectManager
#define SpriteHasSetCollisionFromAnimation
#define HasIGumScreenOwner
#define ScreenIsINameable
#define SpriteManagerHasInsertLayer
#define GumUsesSystemTypes
#define GumCommonCodeReferencing
#define GumTextSupportsBbCode
#define DamageDealingToggles
#define VariantsInsteadOfTypes
#define ITopDownEntity
#define CaseSensitiveLoading
#define ScreensHaveDefaultLayer
#define HasFrbServicesGraphicsDeviceManager
#define ShapeCollectionHasLastCollisionCallDeepCheckCount
#define ScreenHasCancellationToken
#define GameCanStartInEditMode
#define GumHasRenderableCloneLogic
#define ShapeCollectionHasIsPointOnOrInside
#define AudioManagerStopSongTakesBool
#define GraphicalUiElementRemoveFromManagersIsVirtual
#define GumVisualHasRenderTarget
#define GumNineSliceHasAnimate
#define ObsoleteGumDimensionUnitTypes
#define GumHasIRenderTargetTextureReferencer
#define GumHasGueVirtualIsPointInside
#define PositionedNodeHasTag
#define NineSliceHasTilingMiddleSections
#define GumHasFrbRuntimeInterfaces
using EditorTest1;

﻿using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing.Visuals
{
    public class Arrow
    {
        #region Fields/Properties

        Line MainLine;

        List<Line> FirstArrow = new List<Line>();
        List<Line> SecondArrow = new List<Line>();

        public bool Visible
        {
            get => MainLine.Visible;
            set
            {
                MainLine.Visible = value;
                foreach (var line in FirstArrow)
                {
                    line.Visible = value;
                }
                foreach (var line in SecondArrow)
                {
                    line.Visible = value;
                }
            }
        }

        public Color Color
        {
            get => MainLine.Color;
            set
            {
                MainLine.Color = value;
                foreach (var line in FirstArrow)
                {
                    line.Color = value;
                }
                foreach (var line in SecondArrow)
                {
                    line.Color = value;
                }
            }
        }

        #endregion

        public Arrow(FlatRedBall.Graphics.Layer layer, bool firstArrow = false, bool secondArrow = true)
        {
            MainLine = new Line();

            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == false)
            {
                ShapeManager.AddToLayer(MainLine, layer);
                if (firstArrow)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var arrowLine = new Line();
                        ShapeManager.AddToLayer(arrowLine, layer);
                        FirstArrow.Add(arrowLine);

                    }

                }
                if (secondArrow)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var arrowLine = new Line();
                        ShapeManager.AddToLayer(arrowLine, layer);
                        SecondArrow.Add(arrowLine);

                    }
                }
            }
        }


        public void SetFromAbsoluteEndpoints(Vector3 first, Vector3 second)
        {
            var mainFirst = first;
            var mainSecond = second;

            var angle = (second - first).Angle() ?? 0;


            var pixelsPerUnit = FlatRedBall.Camera.Main.PixelsPerUnitAt((first.Z + second.Z) / 2.0f);

            float ArrowSizeX = 5 / pixelsPerUnit;
            float ArrowSizeY = ArrowSizeX;

            if (FirstArrow.Count > 0)
            {
                mainFirst += (Vector3.Right * ArrowSizeX).RotatedBy(angle);
                var firstLineOffset = new Vector3(ArrowSizeX, 5, 0).RotatedBy(angle);
                FirstArrow[0].SetFromAbsoluteEndpoints(first, first + firstLineOffset);
                var secondLineOffset = new Vector3(ArrowSizeX, -5, 0).RotatedBy(angle);
                FirstArrow[1].SetFromAbsoluteEndpoints(first, first + secondLineOffset);

                FirstArrow[2].SetFromAbsoluteEndpoints(
                    FirstArrow[0].AbsolutePoint2,
                    FirstArrow[1].AbsolutePoint2);
            }
            if (SecondArrow.Count > 0)
            {
                mainSecond += (Vector3.Left * ArrowSizeX).RotatedBy(angle);

                var firstLineOffset = new Vector3(-ArrowSizeX, ArrowSizeY, 0).RotatedBy(angle);
                SecondArrow[0].SetFromAbsoluteEndpoints(second, second + firstLineOffset);

                var secondLineOffset = new Vector3(-ArrowSizeX, -ArrowSizeY, 0).RotatedBy(angle);
                SecondArrow[1].SetFromAbsoluteEndpoints(second, second + secondLineOffset);

                SecondArrow[2].SetFromAbsoluteEndpoints(
                    SecondArrow[0].AbsolutePoint2,
                    SecondArrow[1].AbsolutePoint2);
            }

            MainLine.SetFromAbsoluteEndpoints(mainFirst, mainSecond);
        }


        public void Destroy()
        {
            ShapeManager.Remove(MainLine);

            foreach (var line in FirstArrow)
            {
                ShapeManager.Remove(line);
            }
            foreach (var line in SecondArrow)
            {
                ShapeManager.Remove(line);
            }
        }
    }
}
