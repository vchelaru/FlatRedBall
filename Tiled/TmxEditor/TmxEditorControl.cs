using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TmxEditor.GraphicalDisplay.Tilesets;
using System.Reflection;
using RenderingLibrary.Content;
using RenderingLibrary;
using FlatRedBall.SpecializedXnaControls;
using TmxEditor.Controllers;
using TmxEditor.UI;
using ToolsUtilities;
using FlatRedBall.AnimationEditorForms.Controls;
using TmxEditor.CommandsAndState;

namespace TmxEditor
{
    public partial class TmxEditorControl : UserControl
    {
        #region Fields

        SystemManagers mManagers;
        string mCurrentFileName;
        private List<string> _entities;

        ScrollBarControlLogic mScrollBarControlLogic;

        ImageRegionSelectionControl XnaControl;

        #endregion

        #region Events

        public event EventHandler AnyTileMapChange;
        public event EventHandler TilesetDisplayRightClick;
        public event EventHandler LoadEntities;

        #endregion

        public TmxEditorControl()
        {
            InitializeComponent();

            CreateXnaControl();

            TilesetController.Self.Initialize(this.XnaControl, TilesetsListBox, this.StatusLabel, 
                this.TilesetTilePropertyGrid, this.HasCollisionsCheckBox, NameTextBox, EntitiesComboBox);
            TilesetController.Self.AnyTileMapChange += HandleChangeInternal;

            XnaControl.XnaUpdate += new Action(HandleXnaUpdate);
            XnaControl.XnaDraw += new Action(HandleXnaDraw);

            mScrollBarControlLogic = new ScrollBarControlLogic(this.splitContainer3.Panel1);
            ApplicationEvents.Self.WireframePanning += delegate
            {
                mScrollBarControlLogic.UpdateScrollBars();
            };
            ApplicationEvents.Self.SelectedTilesetChanged += delegate
            {
                int width = 256;
                int height = 256;

                var texture = TilesetController.Self.CurrentTexture;

                if(texture != null)
                {
                    width = texture.Width;
                    height = texture.Height;
                }

                mScrollBarControlLogic.UpdateToImage(width, height);
            };


            LayersController.Self.Initialize(this.LayersListBox, LayerPropertyGrid);
            LayersController.Self.AnyTileMapChange += HandleChangeInternal;
            HandleXnaInitialize();

            EditorObjects.IoC.Container.Set<TmxEditor.Managers.ResizeFixer>(new Managers.ResizeFixer());
        }

        private void CreateXnaControl()
        {
            XnaControl = new ImageRegionSelectionControl();

            this.splitContainer3.Panel1.Controls.Add(this.XnaControl);

            this.XnaControl.ContextMenuStrip = this.TilesetXnaContextMenu;
            this.XnaControl.DesiredFramesPerSecond = 30F;
            //this.XnaControl.Location = new System.Drawing.Point(145, 82);
            this.XnaControl.Dock = DockStyle.Fill;
            this.XnaControl.Name = "XnaControl";
            //this.XnaControl.Size = new System.Drawing.Size(296, 172);
            this.XnaControl.TabIndex = 5;
            this.XnaControl.Text = "graphicsDeviceControl1";
            this.XnaControl.MouseClick += new System.Windows.Forms.MouseEventHandler(this.XnaControl_MouseClick);


            List<int> availableZoomLevels = new List<int>();

            availableZoomLevels.Add(1600);
            availableZoomLevels.Add(1200);
            availableZoomLevels.Add(800);
            availableZoomLevels.Add(600);
            availableZoomLevels.Add(400);
            availableZoomLevels.Add(300);
            availableZoomLevels.Add(200);
            availableZoomLevels.Add(175);
            availableZoomLevels.Add(150);
            availableZoomLevels.Add(125);
            availableZoomLevels.Add(100);
            availableZoomLevels.Add(80);
            availableZoomLevels.Add(60);
            availableZoomLevels.Add(40);
            availableZoomLevels.Add(20);

            XnaControl.AvailableZoomLevels = availableZoomLevels;
            XnaControl.ZoomValue = 100;

            XnaControl.MouseWheelZoom += HandleZoom;
        }

        private void HandleZoom(object sender, EventArgs e)
        {
            TilesetController.Self.ReactToZoom();
            

        }

        public List<string> Entities
        {
            get
            {
                return _entities;
            }
            set
            {
                _entities = value;
                EntitiesComboBox.Items.Clear();
                EntitiesComboBox.Items.Add("None");

                if (value != null)
                {
                    _entities.ForEach(e => EntitiesComboBox.Items.Add(e));
                }
            }
        }

        void HandleChangeInternal(object sender, EventArgs args)
        {
            if (AnyTileMapChange != null)
            {
                AnyTileMapChange(this, args);
            }
        }



        public void LoadFile(string fileName)
        {

            if (!string.IsNullOrEmpty(fileName))
            {
                mCurrentFileName = fileName;

                bool succeeded = false;

                try
                {
                    ProjectManager.Self.LoadTiledMapSave(fileName);
                    succeeded = true;
                }
                catch
                {
                    // do nothing, we already warned the user (I think)
                }
                if (succeeded)
                {
                    ToolComponentManager.Self.ReactToLoadedFile(fileName);
                    LayersController.Self.TiledMapSave = ProjectManager.Self.TiledMapSave;
                    this.LoadedTmxLabel.Text = fileName;
                }
                else
                {
                    LayersController.Self.TiledMapSave = null;
                    this.LoadedTmxLabel.Text = "No loaded tmx";
                }
            }
        }

        public void SaveCurrentTileMap(bool saveTsxFiles = true)
        {
            if (!string.IsNullOrEmpty(mCurrentFileName) && ProjectManager.Self.TiledMapSave != null)
            {

                const int maxTries = 5;
                int numberOfTries = 0;
                bool hasSaved = false;

                Exception lastException = null;

                while (!hasSaved && numberOfTries < maxTries)
                {
                    try
                    {
                        ProjectManager.Self.SaveTiledMapSave(saveTsxFiles);
                        hasSaved = true;
                    }
                    catch(Exception exception)
                    {
                        System.Threading.Thread.Sleep(50);
                        exception = lastException;
                        numberOfTries++;
                    }
                }

                if (!hasSaved)
                {
                    throw new Exception("Error saving TMX file: " + lastException);
                }

            }
        }

        void HandleXnaInitialize()
        {
            try
            {
                CreateManagers();

                mScrollBarControlLogic.Managers = mManagers;


                string targetFntFileName = CreateAndSaveFonts();

                // Implement the default content loader since we're not providing
                // a custom one:
                var contentLoader = new ContentLoader();
                contentLoader.SystemManagers = mManagers;
                LoaderManager.Self.ContentLoader = contentLoader;
                LoaderManager.Self.Initialize(null, targetFntFileName, XnaControl.Services, mManagers);
                ToolComponentManager.Self.ReactToXnaInitialize(mManagers);
            }
            catch (Exception e)
            {
                throw new Exception("Error initializing XNA\n\n" + e.ToString());
            }
        }

        private void CreateManagers()
        {
            // For now we'll just use one SystemManagers but we may need to expand this if we have two windows
            mManagers = this.XnaControl.SystemManagers;
            //new SystemManagers();
            //mManagers.Initialize(XnaControl.GraphicsDevice);
        }

        private static string CreateAndSaveFonts()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(XnaAndWinforms.GraphicsDeviceControl));

            string targetFntFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial.fnt";
            string targetPngFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial_0.png";
            FileManager.SaveEmbeddedResource(
                assembly,
                "XnaAndWinforms.Content.Font18Arial.fnt",
                targetFntFileName);

            FileManager.SaveEmbeddedResource(
                assembly,
                "XnaAndWinforms.Content.Font18Arial_0.png",
                targetPngFileName);
            return targetFntFileName;
        }


        void HandleXnaDraw()
        {
            mManagers.Renderer.Draw(mManagers);
        }


        void HandleXnaUpdate()
        {
            TimeManager.Self.Activity();
        }

        public void UpdateTilesetDisplay()
        {

            TilesetController.Self.UpdateXnaDisplayToTileset();

            TilesetController.Self.Displayer.UpdateDisplayedProperties();
            TilesetController.Self.Displayer.PropertyGrid.Refresh();
        }


        public void AddTab(string tabName, System.Windows.Controls.UserControl wpfControl)
        {
            this.tabControl1.TabPages.Add(tabName);
            var newlyAdded = tabControl1.TabPages[tabControl1.TabPages.Count - 1];

            // Instantiate the host:
            System.Windows.Forms.Integration.ElementHost wpfHost;
            wpfHost = new System.Windows.Forms.Integration.ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            // Set the host’s child to the WPF control
            wpfHost.Child = wpfControl;

            newlyAdded.Controls.Add(wpfHost);
        }
        

        internal void LoadTilesetProperties(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                string output;

                ProjectManager.Self.LoadTilesetFrom(fileName, out output);

                ToolComponentManager.Self.ReactToLoadedAndMergedProperties(fileName);
                if (!string.IsNullOrEmpty(output))
                {
                    MessageBox.Show(output);
                }

            }
        }

        private void AddLayerPropertyButton_Click(object sender, EventArgs e)
        {
            LayersController.Self.HandleAddPropertyClick();

        }

        private void RemovePropertyButton_Click(object sender, EventArgs e)
        {
            LayersController.Self.HandleRemovePropertyClick();
        }

        private void LayersListBox_MouseClick(object sender, MouseEventArgs e)
        {
            LayersController.Self.HandleListRightClick(e);
        }

        private void LayerPropertyGrid_Click(object sender, EventArgs e)
        {
        }

        private void LayerPropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            LayersController.Self.UpdatePropertyGridContextMenu(e);

        }

        private void TilesetsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void AddTilesetPropertyButton_Click(object sender, EventArgs e)
        {
            TilesetController.Self.HandleAddPropertyClick();
        }

        private void RemoveTilesetPropertyButton_Click(object sender, EventArgs e)
        {
            TilesetController.Self.HandleRemovePropertyClick();
        }

        private void XnaControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && TilesetDisplayRightClick != null)
            {
                TilesetDisplayRightClick(this, e);
            }
        }

        private void HasCollisionsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TilesetController.Self.HasCollisionsCheckBoxChanged(HasCollisionsCheckBox.Checked);
        }

        private void EntitiesComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            TilesetController.Self.EntitiesComboBoxChanged(EntitiesComboBox.SelectedItem as string);
        }

        private void EntitiesComboBox_Click(object sender, EventArgs e)
        {
            if (LoadEntities != null)
            {
                LoadEntities(sender, e);
            }
        }

        
    }
}
