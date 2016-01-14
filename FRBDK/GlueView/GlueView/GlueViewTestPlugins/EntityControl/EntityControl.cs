using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;
using GlueView.Facades;
using FlatRedBall.Gui;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Content.Instructions;
using FlatRedBall.IO;
using FlatRedBall.Input;
using GlueViewTestPlugins.EntityControl.Handles;
using InteractiveInterface;

namespace GlueViewTestPlugins.EntityControl
{
	[Export(typeof(GlueViewPlugin))]
	class EntityControl : GlueViewPlugin
    {
        #region Fields
        RuntimeOptions mRuntimeOptions = new RuntimeOptions();

        EntityControlControls entityControlControls;
		ElementRuntime mCurrentSelectedElementRuntime;


        ElementRuntime mElementRuntimeOver = null;

        ElementRuntimeHighlight mHighlight;

		List<Layer> mLayers;

		ScalingHandles mScalingHandles;
		RotationHandles mRotationHandles;

		ActionType mCurrentAction;

        List<string> mPositionedObjectVariables = new List<string>();
        List<string> mIScalableVariables = new List<string>();
        List<string> mCircleVariables = new List<string>();

        #endregion

        #region Properties


        Layer SelectedLayer
        {
            get
            {
                return mLayers[entityControlControls.LayerComboBox.SelectedIndex];

            }
        }

		public bool ShouldSave
		{
			get
			{
                return mRuntimeOptions.ShouldSave;
			}
		}

		public override string FriendlyName
		{
			get
			{
				return "Entity Control";
			}
		}

		public override Version Version
		{
			get
			{
				return new Version();
			}
		}

        #endregion


        public override void StartUp()
		{
			//Highlights
            mHighlight = new ElementRuntimeHighlight();
            mHighlight.Color = Color.Yellow;

			//UI
			entityControlControls = new EntityControlControls(mCurrentSelectedElementRuntime, mRuntimeOptions);
			GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm("Entity Control", 200, entityControlControls, this);

			//Events
			this.Push += new EventHandler(OnPush);
			this.ElementLoaded += new EventHandler(OnElementLoaded);
			this.Drag += new EventHandler(OnDrag);
			this.Click += new EventHandler(OnClick);
            this.MouseMove += new EventHandler(OnMouseMove);
			this.Update += new EventHandler(OnUpdate);
            this.ElementHiglight += new EventHandler(OnElementHighlight);

			//Layers
			mLayers = new List<Layer>();
			setUpLayerComboBox();

			//Handles
			mScalingHandles = new ScalingHandles();
			mRotationHandles = new RotationHandles();

			//Action
			mCurrentAction = ActionType.None;

            FillVariableList();
		}

        

        void OnElementHighlight(object sender, EventArgs e)
        {
            SelectElement(GlueViewState.Self.HighlightedElementRuntime);
        }

        void FillVariableList()
        {
            mPositionedObjectVariables.Add("X");
            mPositionedObjectVariables.Add("Y");

            mIScalableVariables.Add("ScaleX");
            mIScalableVariables.Add("ScaleY");

            mCircleVariables.Add("Radius");
        }

        void OnMouseMove(object sender, EventArgs e)
        {
            try
            {
                ElementRuntime elementRuntimeOver = GlueViewState.Self.CursorState.GetElementRuntimeOver();

                mHighlight.CurrentElement = elementRuntimeOver;
            }
            catch
            {
                int m = 3;
            }
        }

		void OnUpdate(object sender, EventArgs e)
		{
			mScalingHandles.Update();

			if (entityControlControls.TypeOfAction == ActionType.None)
			{
				mCurrentAction = ActionType.None;
			}
		}

		/// <summary>
		/// Parses the layers out of the currently selected element in Glue,
		/// then adds them to the combobox.
		/// </summary>
		private void setUpLayerComboBox()
		{
			//Clear the lists
			entityControlControls.LayerComboBox.Items.Clear();
			mLayers.Clear();

			//Set the defaults
			entityControlControls.LayerComboBox.Items.Add("None");
			entityControlControls.LayerComboBox.SelectedIndex = 0;
			mLayers.Add(null);

			//Add all layers found 
			if (GlueViewState.Self.CurrentElementRuntime != null)
			{
				foreach (ElementRuntime element in GlueViewState.Self.CurrentElementRuntime.ContainedElements)
				{
					if (element.DirectObjectReference != null && element.DirectObjectReference is Layer)
					{
						entityControlControls.LayerComboBox.Items.Add(element.DirectObjectReference.ToString().Replace("Name: ", ""));
						mLayers.Add((Layer)element.DirectObjectReference);
					}
				}
			}
		}

		/// <summary>
		/// When the mouse is pressed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnPush(object sender, EventArgs e)
		{
			if (InputManager.Mouse.IsOwnerFocused && GlueViewState.Self.CurrentElementRuntime != null)
			{
                ElementRuntime elementRuntimeOver = GlueViewState.Self.CursorState.GetElementRuntimeOver();
				bool containsSourceFile = CheckForSourceFile(elementRuntimeOver);

				//Select Element
				if (entityControlControls.TypeOfAction == ActionType.Everything && !containsSourceFile)
				{
					//Scaling Handles
					if (mCurrentSelectedElementRuntime != null && mScalingHandles.IsMouseOnHandle())
					{
						mCurrentAction = ActionType.Scale;
					}
					else
					{
						//Connect to Glue
						//Current lags like crazy when something is first selected...
						if (InteractiveConnection.Callback != null)
						{
							string containerName;
							string namedObjectName;

							if (elementRuntimeOver == null)
							{
								containerName = "";
								namedObjectName = "";
							}
							else
							{
								containerName = GlueViewState.Self.CurrentElementRuntime.AssociatedIElement.Name;
								namedObjectName = elementRuntimeOver.AssociatedNamedObjectSave.InstanceName;
							}

							InteractiveConnection.Callback.SelectNamedObjectSave(containerName, namedObjectName);
						}

						// We used to set the value directly in the plugin
						// but now we want to go through GView so that the app-wide
						// selection is set, and this also notifies other plugins.
						//SelectElement(elementRuntimeOver);
						GlueViewState.Self.HighlightedElementRuntime = elementRuntimeOver;

						mCurrentAction = ActionType.Move;
					}
				}
			}
		}

		/// <summary>
		/// Checks to see if the elemenetRuntime is owned by another file
		/// </summary>
		/// <param name="elementRuntimeOver">The ElementRuntime to check</param>
		/// <returns>Whether the elementRuntime is owned by a file</returns>
		private bool CheckForSourceFile(ElementRuntime elementRuntimeOver)
		{
			if (elementRuntimeOver == null)
			{
				return false;
			}
			else if (elementRuntimeOver.AssociatedNamedObjectSave == null)
			{
				return false;
			}
			else if (elementRuntimeOver.AssociatedNamedObjectSave.SourceFile == null)
			{
				return false;
			}

			return true;
		}

        private void SelectElement(ElementRuntime elementRuntimeOver)
        {
            mCurrentSelectedElementRuntime = elementRuntimeOver;

            if (mCurrentSelectedElementRuntime != null && mCurrentSelectedElementRuntime.AssociatedNamedObjectSave != null)
            {
                entityControlControls.SetCurrentNamedObject(
                    mCurrentSelectedElementRuntime,
                    mCurrentSelectedElementRuntime.AssociatedNamedObjectSave);
            }

            if (mScalingHandles.IsElementRuntimeScalable(mCurrentSelectedElementRuntime))
            {
                mScalingHandles.CurrentElement = mCurrentSelectedElementRuntime;
            }
            else
            {
                mScalingHandles.CurrentElement = null;
            }

			mRotationHandles.CurrentElement = mCurrentSelectedElementRuntime;

			if (mCurrentSelectedElementRuntime == null)
			{
				mCurrentAction = ActionType.None;
			}
			else
			{
				mCurrentAction = ActionType.Move;
			}
        }

		void OnElementLoaded(object sender, EventArgs e)
		{
			//Parses the layers out of the currently selected element in Glue
			setUpLayerComboBox();

            if (mCurrentSelectedElementRuntime != null && mCurrentSelectedElementRuntime.AssociatedNamedObjectSave != null)
            {
                ElementRuntime newElementRuntime = GluxManager.CurrentElement;
                if (newElementRuntime.Name != mCurrentSelectedElementRuntime.Name)
                {
                    newElementRuntime = newElementRuntime.GetContainedElementRuntime(mCurrentSelectedElementRuntime.Name);
                }
                mCurrentSelectedElementRuntime = newElementRuntime;

                if (mCurrentSelectedElementRuntime != null)
                {
                    // The element has been reloaded which means the NamedObjectSaves and ElementRuntimes have been recreated.
                    // We need to update to that:
                    entityControlControls.SetCurrentNamedObject(
                        mCurrentSelectedElementRuntime,
                        mCurrentSelectedElementRuntime.AssociatedNamedObjectSave);
                }
            }
		}

		void OnDrag(object sender, EventArgs e)
		{
			if (InputManager.Mouse.IsOwnerFocused)
			{
				//If there is an element
				if (mCurrentSelectedElementRuntime != null)
				{
					if (mCurrentAction == ActionType.Scale)
					{
						mScalingHandles.Scale();
					}
					//Move
					else if (mCurrentAction == ActionType.Move)
					{
						//If the element is something else (such as part of an entity)
						if (mCurrentSelectedElementRuntime.DirectObjectReference != null && mCurrentSelectedElementRuntime.DirectObjectReference is PositionedObject && mCurrentSelectedElementRuntime.ReferencedFileRuntimeList.LoadedScenes.Count == 0 && mCurrentSelectedElementRuntime.ReferencedFileRuntimeList.LoadedShapeCollections.Count == 0)
						{
							PositionedObject element = (PositionedObject)mCurrentSelectedElementRuntime.DirectObjectReference;
							PositionedObject parent = element.Parent;

							element.Detach();
							element.X += GuiManager.Cursor.WorldXChangeAt(0, mLayers[entityControlControls.LayerComboBox.SelectedIndex]);
							element.Y += GuiManager.Cursor.WorldYChangeAt(0, mLayers[entityControlControls.LayerComboBox.SelectedIndex]);
							element.AttachTo(parent, true);
						}
						//Else, just move the element
						else
						{
							PositionedObject parent = mCurrentSelectedElementRuntime.Parent;
							mCurrentSelectedElementRuntime.Detach();
							mCurrentSelectedElementRuntime.X += GuiManager.Cursor.WorldXChangeAt(0, mLayers[entityControlControls.LayerComboBox.SelectedIndex]);
							mCurrentSelectedElementRuntime.Y += GuiManager.Cursor.WorldYChangeAt(0, mLayers[entityControlControls.LayerComboBox.SelectedIndex]);
							mCurrentSelectedElementRuntime.AttachTo(parent, true);
						}
					}
					//Rotate
					else if (mCurrentAction == ActionType.Rotate)
					{

					}
				}
			}
		}

		void OnClick(object sender, EventArgs e)
		{
			if (InputManager.Mouse.IsOwnerFocused && ShouldSave && mCurrentSelectedElementRuntime != null)
			{
                GlueProjectSaveCommands commands = GlueViewCommands.Self.GlueProjectSaveCommands;

                object directObjectReference = mCurrentSelectedElementRuntime.DirectObjectReference;

                if(directObjectReference != null)
                {
                    if(directObjectReference is PositionedObject)
                    {
                        commands.UpdateIElementVariables(mCurrentSelectedElementRuntime, mPositionedObjectVariables);

                    }
                    if(directObjectReference is IScalable)
                    {
                        commands.UpdateIElementVariables(mCurrentSelectedElementRuntime, mIScalableVariables);

                    }
                    if(directObjectReference is Circle)
                    {
                        commands.UpdateIElementVariables(mCurrentSelectedElementRuntime, mCircleVariables);
                    }
                }
                else
                {
                    commands.UpdateIElementVariables(mCurrentSelectedElementRuntime, mPositionedObjectVariables);

                }


                GlueViewCommands.Self.GlueProjectSaveCommands.SaveGlux();
			}
		}

		public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
		{
			// remove the form
            //GlueViewCommands.Self.CollapsibleFormCommands.RemoveCollapsableForm("Entity Control", 200, entityControlControls);
			return true;
		}
	}
}
