using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using PolygonEditor;
using FlatRedBall;
using FlatRedBall.IO;
using EditorObjects.EditorSettings;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework;

namespace PolygonEditorXna.IO
{
    public enum ReplaceOrInsert
    {
        Replace,
        Insert
    }


    public static class FileLoader
    {
        #region Fields

        static string mFileNameToLoad;

        static MultiButtonMessageBox mLastMbmb;

        static AdvancedShapeCollectionLoadingWindow mLastAsclw;

        static Vector3 mOffset = new Vector3();

        static TypesToLoad mTypesToLoad;

        #endregion

        #region Event Methods

        static void AdvancedClick(Window callingWindow)
        {
            mTypesToLoad = new TypesToLoad();
            mLastAsclw = new AdvancedShapeCollectionLoadingWindow(
                GuiManager.Cursor, CreateMbmb(mFileNameToLoad), mTypesToLoad);

            GuiManager.AddWindow(mLastAsclw);
        }

        static void ReplaceClicked(Window callingWindow)
        {
            if (mLastAsclw == null)
            {
                mOffset = new Vector3();
            }
            else
            {
                mOffset = mLastAsclw.Offset;
            }

            LoadShapeCollection(mFileNameToLoad, ReplaceOrInsert.Replace);

            mLastAsclw = null;
        }

        static void InsertClicked(Window callingWindow)
        {
            if (mLastAsclw == null)
            {
                mOffset = new Vector3();
            }
            else
            {
                mOffset = mLastAsclw.Offset;
            }

            LoadShapeCollection(mFileNameToLoad, ReplaceOrInsert.Insert);

            mLastAsclw = null;
        }

        #endregion

        public static void LoadShapeCollectionAskReplaceOrInsert(string fileName)
        {
            mFileNameToLoad = fileName;

            MultiButtonMessageBox newMbmb = CreateMbmb(fileName);

            mLastMbmb = newMbmb;
            GuiManager.AddWindow(mLastMbmb);   
        
        }

        private static MultiButtonMessageBox CreateMbmb(string fileName)
        {
            MultiButtonMessageBox newMbmb = new MultiButtonMessageBox(GuiManager.Cursor);

            newMbmb.ScaleX = 20;
            newMbmb.Text = "Replace or Insert this file?\n\n" + fileName;

            newMbmb.Name = "";

            newMbmb.AddButton("Replace", ReplaceClicked);
            newMbmb.AddButton("Insert", InsertClicked);
            newMbmb.AddButton("Advanced >>", AdvancedClick);
            newMbmb.AddButton("Cancel", null);
            return newMbmb;
        }

        public static void LoadShapeCollection(string fileName)
        {
            LoadShapeCollection(fileName, ReplaceOrInsert.Replace);
        }

        public static void LoadShapeCollection(string fileName, ReplaceOrInsert replaceOrInsert)
        {
            #region Load and set the ShapeCollection

            FlatRedBall.Content.Math.Geometry.ShapeCollectionSave ssl = FlatRedBall.Content.Math.Geometry.ShapeCollectionSave.FromFile(fileName);
            ssl.Shift(mOffset);
            mOffset = new Vector3();
            // Remove the current shapes

            if (mTypesToLoad != null)
            {
                if (!mTypesToLoad.LoadCubes)
                {
                    ssl.AxisAlignedCubeSaves.Clear();
                }

                if (!mTypesToLoad.LoadRectangles)
                {
                    ssl.AxisAlignedRectangleSaves.Clear();
                }

                if (!mTypesToLoad.LoadCircles)
                {
                    ssl.CircleSaves.Clear();
                }

                if (!mTypesToLoad.LoadPolygons)
                {
                    ssl.PolygonSaves.Clear();
                }

                if (!mTypesToLoad.LoadSpheres)
                {
                    ssl.SphereSaves.Clear();
                }


                mTypesToLoad = null;
            }

            if (replaceOrInsert == ReplaceOrInsert.Replace)
            {
                EditorData.ShapeCollection.RemoveFromManagers(true);

                ssl.SetShapeCollection(EditorData.ShapeCollection);
                EditorData.ShapeCollection.AddToManagers();
            }
            else
            {
                string nameToPreserve = EditorData.ShapeCollection.Name;

                // Easiest way to get everything in the ShapeManager is to remove the shapes from the managers
                // then re-add them once the newly-loaded shapes have been put in the ShapeCollection
                EditorData.ShapeCollection.RemoveFromManagers(false);

                ssl.SetShapeCollection(EditorData.ShapeCollection); // this will just add to the ShapeCollection

                EditorData.ShapeCollection.AddToManagers();
                EditorData.ShapeCollection.Name = nameToPreserve;
            }
            #endregion

            #region Set the title of the window

            if (replaceOrInsert == ReplaceOrInsert.Replace)
            {
#if FRB_MDX
            // Do this before loading the SavedInformation
            GameForm.TitleText = "PolygonEditor - Editing " + fileName;
#else
                FlatRedBallServices.Game.Window.Title = "PolygonEditor Editing - " + fileName;
#endif
            }

            #endregion

            if (replaceOrInsert == ReplaceOrInsert.Replace)
            {
                EditorData.LastLoadedPolygonList = null;
                EditorData.LastLoadedShapeCollection = fileName;
            }

            #region Load the SavedInformation if available

            if (replaceOrInsert == ReplaceOrInsert.Replace)
            {
                fileName = FileManager.RemoveExtension(fileName) + ".pesix";
                if (System.IO.File.Exists(fileName))
                {
                    try
                    {

                        PolygonEditorSettings savedInformation = PolygonEditorSettings.FromFile(fileName);
                        if (savedInformation.LineGridSave != null)
                        {
                            savedInformation.LineGridSave.ToLineGrid(EditorData.LineGrid);
                        }

                        if (savedInformation.BoundsCameraSave != null)
                        {
                            savedInformation.BoundsCameraSave.SetCamera(EditorData.BoundsCamera);
                        }

                        if (savedInformation.UsePixelCoordinates)
                        {
                            SpriteManager.Camera.UsePixelCoordinates(false);
                        }

                    }
                    catch
                    {
                        GuiManager.ShowMessageBox(
                            "Could not load the settings file " + fileName + ".  \nThe data file was loaded with no problems",
                            "Error");
                    }

                }
            }
            #endregion

        }

    }
}
