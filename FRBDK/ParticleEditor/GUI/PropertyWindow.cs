using System;
using FRB;
using FRB.Gui;
using FRB.Collections;

namespace ParticleEditor.GUI
{
	/// <summary>
	/// Summary description for Property.
	/// </summary>
	public class PropertyWindow : CollapseWindow
	{

		#region members

		public WindowArrayVisibilityListBox propertiesEditingListBox;

		#region textureGUI
		public WindowArray textureGUI;
		public ComboBox textureOrAnimation;

		TextDisplay texturePath;
		public Button textureButton;
		public Button addFrame;
		

		#endregion

		#region particlePropertiesGUI

		public WindowArray particlePropertiesGUI;

		TextDisplay emitterNameDisplay;
		public TextBox emitterName;
		#region position, scale and rotation
		
		#region positioning
		TextDisplay xPos;
		public TextBox xPosTextBox;

		TextDisplay yPos;
		public TextBox yPosTextBox;

		TextDisplay zPos;
		public TextBox zPosTextBox;
		#endregion

		#region scale
		TextDisplay xScl;
		public TextBox xSclTextBox;

		TextDisplay xSclVelocity;
		public TextBox xSclVelocityTextBox;

		TextDisplay yScl;
		public TextBox ySclTextBox;

		TextDisplay ySclVelocity;
		public TextBox ySclVelocityTextBox;

		#endregion

		TextDisplay rotZDisplay;
		public ComboBox rotZFixedOrRange;
		public TextBox rotZMinTextBox;

		public FRB.Collections.WindowArray rotZRangeGUI;
		public TextDisplay rotZTo;
		public TextBox rotZMaxTextBox;

		TextDisplay rotZVelocityDisplay;
		public ComboBox rotZVelocityFixedOrRange;
		public TextBox rotZVelocityMinTextBox;

		public FRB.Collections.WindowArray rotZVelocityRangeGUI;
		public TextDisplay rotZVelocityTo;
		public TextBox rotZVelocityMaxTextBox;
		#endregion

		public ToggleButton particleColorOperations;
		
		TextDisplay velocityLossPercentageRate;
		public TextBox velocityLossTextBox;

		#region Removal Event
		TextDisplay removalEvent;
		public ComboBox removalEventComboBox;

		public WindowArray lastingTimeGUI;
		TextDisplay lastingTime;
		public TextBox lastingTimeTextBox;
		#endregion


		
		#endregion

		#region emissionAreaGUI

		WindowArray emissionAreaGUI;
		public ComboBox emissionAreaType;

		public TextDisplay emissionAreaSclXTextDisplay;
		public TextDisplay emissionAreaSclYTextDisplay;
		public TextDisplay emissionAreaSclZTextDisplay;

		public UpDown emissionAreaSclX;
		public UpDown emissionAreaSclY;
		public UpDown emissionAreaSclZ;


		#endregion

		#region attachment props.

		WindowArray relativePropertiesGUI;

		public TextDisplay attachmentInformationTextDisplay;

		public TextDisplay relXDisplay;
		public TextDisplay relYDisplay;
		public TextDisplay relZDisplay;
		public TextDisplay relRotZDisplay;

		public TextBox relXTextBox;
		public TextBox relYTextBox;
		public TextBox relZTextBox;
		public TextBox relRotZTextBox;

		public ToggleButton considerParentVelocityToggleButton;

		#endregion

		#region sprite color operation window
		
		public UpDown tintRed = null;
		public UpDown tintGreen = null;
		public UpDown tintBlue = null;

		public UpDown tintRedRate = null;
		public UpDown tintGreenRate = null;
		public UpDown tintBlueRate = null;


		public ToggleButton noColorOp = null;
		public ToggleButton addColorOp = null;
		public ToggleButton addSignedColorOp = null;
		public ToggleButton modulateColorOp = null;
		public ToggleButton subtractColorOp = null;

		TextDisplay fadeDisplay;
		public UpDown fadeUpDown;

		TextDisplay fadeRateDisplay;
		public UpDown fadeRateUpDown;
		public ToggleButton regularBlend = null;
		public ToggleButton additiveBlend = null;
		public ToggleButton modulateBlend = null;
		public ToggleButton modulate2XBlend = null;

		#endregion

		#region initialVelocityGUI
		public WindowArray initialVelocityGUI;

		TextDisplay spreadStyle;
		public ComboBox spreadStyleComboBox;
		
		#region square spread

		public WindowArray squareSpreadGUI;

		TextDisplay xVelocityText;
		TextDisplay yVelocityText;
		TextDisplay zVelocityText;

		public ComboBox xVelocityType; // can be fixed or range
		public TextBox xMinValue;
		TextDisplay xTo;
		public TextBox xMaxValue;

		public WindowArray xRangeGUI;

		public ComboBox yVelocityType;
		public TextBox yMinValue;
		TextDisplay yTo;
		public TextBox yMaxValue;
		public WindowArray yRangeGUI;


		public ComboBox zVelocityType;
		public TextBox zMinValue;
		TextDisplay zTo;
		public TextBox zMaxValue;
		public WindowArray zRangeGUI;

		#endregion

		#region circular spread

		public WindowArray circularSpreadGUI;

		TextDisplay outwardVelocity;
		public ComboBox outwardVelocityRangeOrFixed;
		public TextBox outwardVelocityTextBox;

		public WindowArray outwardVelocityRangeGUI;
		TextDisplay outwardVelocityTo;
		public TextBox outwardVelocityTextBoxMax;

		public ComboBox wedgeOrFull; // "wedge" or "full"

		public WindowArray wedgeGUI;

		TextDisplay directionAngle;
		public TextBox directionAngleTextBox;

		TextDisplay spreadAngle;
		public TextBox spreadAngleTextBox;


		#endregion
		
		#endregion

		#region initialAccelerationGUI
		public WindowArray initialAccelerationGUI;

		TextDisplay xAccelerationText;
		TextDisplay yAccelerationText;
		TextDisplay zAccelerationText;

		TextDisplay AccelerationType;

		public ComboBox xAccelerationType; // can be fixed or range
		public TextBox xMinAccelerationValue;
		TextDisplay xAccelerationTo;
		public TextBox xMaxAccelerationValue;
		public WindowArray xAccelerationRangeGUI;

		public ComboBox yAccelerationType;
		public TextBox yMinAccelerationValue;
		TextDisplay yAccelerationTo;
		public TextBox yMaxAccelerationValue;
		public WindowArray yAccelerationRangeGUI;


		public ComboBox zAccelerationType;
		public TextBox zMinAccelerationValue;
		TextDisplay zAccelerationTo;
		public TextBox zMaxAccelerationValue;
		public WindowArray zAccelerationRangeGUI;

		#endregion

		#region emissionTimingGUI
		public WindowArray emissionTimingGUI;
		TextDisplay emissionEvent;
		public ComboBox emissionEventComboBox; // call only, timed

		public WindowArray timingGUI;
		TextDisplay onceEvery;
		public TextBox secondFrequencyTextBox;
		TextDisplay millisecondsDisplay;

		TextDisplay numberPerEmissionDisplay;
		public TextBox numberPerEmissionTextBox;

		#endregion

		#region Instructions

		public WindowArray instructionGUI;

		public ListBox instructionListBox;

		public Button addInstructionButton;
		public Button deleteInstructionButton;

		public TextDisplay typeTextDisplay;

		public ComboBox typeComboBox;

		public TextDisplay value1TextDisplay;
		public Window value1Window;

		public TextDisplay value2TextDisplay;
		public Window value2Window;

		public TextDisplay value3TextDisplay;
		public Window value3Window;

		public TextDisplay instructionTimeTextDisplay;
		public TextBox instructionTimeTextBox;

		public TextDisplay cycleTimeTextDisplay;
		public TextBox cycleTimeTextBox;

		

		#endregion

		#endregion

		public PropertyWindow(GuiManager guiMan, InputManager inpMan, GameData gameData,
			GuiData guiData) : base(GuiManager.cursor)
		{
			#region engine data and GUI object references
			PropertyWindowMessages.guiData = guiData;
			PropertyWindowMessages.gameData = gameData;
			PropertyWindowMessages.guiMan = guiMan;
			PropertyWindowMessages.sprMan = gameData.sprMan;
			PropertyWindowMessages.propWindow = this;

			#endregion 

			#region Initialize this and the WAVListBox
			sclX = 20;
			sclY = 20;
			SetPositionTL(20, 22.8f);
			mMoveBar = true;
			AddXButton();
			mName = "Properties";

			propertiesEditingListBox = this.AddWAVListBox(GuiManager.cursor);
			propertiesEditingListBox.sclX = 7;
			propertiesEditingListBox.sclY = 18.5f;
			propertiesEditingListBox.SetPositionTL(sclX-12.5f, sclY + -.1f);
			propertiesEditingListBox.scrollBarVisible = false;
				
			propertiesEditingListBox.onClick += new FrbGuiMessage(PropertyWindowMessages.propertiesEditingClick);
			GuiManager.AddWindow(this);
			#endregion			

			#region textureGUI

			textureGUI = new WindowArray();
            
            texturePath = AddTextDisplay();
			texturePath.text = "Click button to set texture";
            texturePath.SetPositionTL(propertiesEditingListBox.sclX * 2 + 1, 1.5f);
			textureGUI.Add(texturePath);



			textureOrAnimation = AddComboBox();
			textureOrAnimation.sclX = 7;
			textureOrAnimation.AddItem("Single Texture");
			textureOrAnimation.AddItem("Animation Chain");
			textureOrAnimation.text = "Single Texture";
            textureOrAnimation.SetPositionTL(propertiesEditingListBox.sclX * 2 + 8, 3.5f);
			textureOrAnimation.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateTextureOrAnimationButton);
			textureGUI.Add(textureOrAnimation);

			#region single texture GUI


			textureButton = AddButton();
			textureButton.sclX = textureButton.sclY = 9;
            textureButton.SetPositionTL(propertiesEditingListBox.sclX * 2 + 10, 14.0f);
			textureButton.onClick += new FrbGuiMessage(PropertyWindowMessages.textureButtonClick);
			textureGUI.Add(textureButton);
			#endregion


			textureGUI.visible = false;

			propertiesEditingListBox.AddWindowArray("Texture", textureGUI);

			#endregion

			#region particlePropertiesGUI
			particlePropertiesGUI = new WindowArray();

			float runningY = 4;

			emitterNameDisplay = this.AddTextDisplay();
			emitterNameDisplay.text = "Name:";
			emitterNameDisplay.SetPositionTL(15, runningY);
			particlePropertiesGUI.Add(emitterNameDisplay);

			emitterName = AddTextBox();
			emitterName.sclX = 5;
			emitterName.SetPositionTL(26, runningY);
			emitterName.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.emitterNameTextBoxLoseFocus);
			particlePropertiesGUI.Add(emitterName);

			runningY += 3;

			#region x y z position

			xPos = AddTextDisplay();
			xPos.text = "X Pos:";
			xPos.SetPositionTL(15, runningY);
			particlePropertiesGUI.Add(xPos);

			xPosTextBox = AddTextBox();
			xPosTextBox.sclX = 3;
			xPosTextBox.SetPositionTL(24, runningY);
			xPosTextBox.format = TextBox.FormatTypes.DECIMAL;
			xPosTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xPosTextBoxLoseFocus);
			particlePropertiesGUI.Add(xPosTextBox);

			runningY += 2.5f;

			yPos = AddTextDisplay();
			yPos.text = "Y Pos:";
			yPos.SetPositionTL(15, runningY);
			particlePropertiesGUI.Add(yPos);

			yPosTextBox = AddTextBox();
			yPosTextBox.sclX = 3;
			yPosTextBox.SetPositionTL(24, runningY);
			yPosTextBox.format = TextBox.FormatTypes.DECIMAL;
			yPosTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.yPosTextBoxLoseFocus);
			particlePropertiesGUI.Add(yPosTextBox);

			runningY += 2.5f;

			zPos = AddTextDisplay();
			zPos.text = "Z Pos:";
			zPos.SetPositionTL(15, runningY);
			particlePropertiesGUI.Add(zPos);

			zPosTextBox = AddTextBox();
			zPosTextBox.sclX = 3;
			zPosTextBox.SetPositionTL(24, runningY);
			zPosTextBox.format = TextBox.FormatTypes.DECIMAL;
			zPosTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.zPosTextBoxLoseFocus);
			particlePropertiesGUI.Add(zPosTextBox);

			runningY += 3;

			#endregion

			#region sclX and sclXVelocity
			xScl = AddTextDisplay();
			xScl.SetPositionTL(15, runningY);
			xScl.text = "X Scl:";
			particlePropertiesGUI.Add(xScl);

			xSclTextBox = AddTextBox();
			xSclTextBox.SetPositionTL(24f, runningY);
			xSclTextBox.sclX = 3;
			xSclTextBox.format = TextBox.FormatTypes.DECIMAL;
			xSclTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xSclTextBoxLoseFocus);
			particlePropertiesGUI.Add(xSclTextBox);

			runningY += 2.5f;

			xSclVelocity = AddTextDisplay();
			xSclVelocity.SetPositionTL(15, runningY);
			xSclVelocity.text = "X Scl Vel:";
			particlePropertiesGUI.Add(xSclVelocity);

			xSclVelocityTextBox = AddTextBox();
			xSclVelocityTextBox.SetPositionTL(24f, runningY);
			xSclVelocityTextBox.sclX = 3;
			xSclVelocityTextBox.format = TextBox.FormatTypes.DECIMAL;
			xSclVelocityTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xSclVelocityTextBoxLoseFocus);
			particlePropertiesGUI.Add(xSclVelocityTextBox);

			runningY += 3;
			#endregion

			#region sclY and sclYVelocity
			yScl = AddTextDisplay();
			yScl.SetPositionTL(15, runningY);
			yScl.text = "Y Scl:";
			particlePropertiesGUI.Add(yScl);

			ySclTextBox = AddTextBox();
			ySclTextBox.SetPositionTL(24f, runningY);
			ySclTextBox.sclX = 3;
			ySclTextBox.format = TextBox.FormatTypes.DECIMAL;
			ySclTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.ySclTextBoxLoseFocus);
			particlePropertiesGUI.Add(ySclTextBox);

			runningY += 2.5f;

			ySclVelocity = AddTextDisplay();
			ySclVelocity.SetPositionTL(15, runningY);
			ySclVelocity.text = "Y Scl Vel:";
			particlePropertiesGUI.Add(ySclVelocity);

			ySclVelocityTextBox = AddTextBox();
			ySclVelocityTextBox.SetPositionTL(24f, runningY);
			ySclVelocityTextBox.sclX = 3;
			ySclVelocityTextBox.format = TextBox.FormatTypes.DECIMAL;
			ySclVelocityTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.ySclVelocityTextBoxLoseFocus);
			particlePropertiesGUI.Add(ySclVelocityTextBox);

			runningY += 3;
			#endregion

			#region rotZ and rotZVelocity


			rotZDisplay = AddTextDisplay();
			rotZDisplay.SetPositionTL(15, runningY);
			rotZDisplay.text = "Z Rot:";
			particlePropertiesGUI.Add(rotZDisplay);

			rotZFixedOrRange = AddComboBox();
			rotZFixedOrRange.sclX = 3.5f;
			rotZFixedOrRange.AddItem("Fixed");
			rotZFixedOrRange.AddItem("Range");
			rotZFixedOrRange.text = "Fixed";
			rotZFixedOrRange.SetPositionTL(24.5f, runningY);
			rotZFixedOrRange.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateRotZRange);
			rotZFixedOrRange.onItemClick += new FrbGuiMessage(PropertyWindowMessages.rotZFixedOrRangeItemClick);

			particlePropertiesGUI.Add(rotZFixedOrRange);

			rotZMinTextBox = AddTextBox();
			rotZMinTextBox.SetPositionTL(30.5f, runningY);
			rotZMinTextBox.sclX = 2;
			rotZMinTextBox.format = TextBox.FormatTypes.DECIMAL;
			rotZMinTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.rotZMinTextBoxLoseFocus);
			particlePropertiesGUI.Add(rotZMinTextBox);

			rotZRangeGUI = new WindowArray();

			rotZTo = AddTextDisplay();
			rotZTo.SetPositionTL(32.0f, runningY);
			rotZTo.text = "to";
			rotZRangeGUI.Add(rotZTo);	
				
			rotZMaxTextBox = AddTextBox();
			rotZMaxTextBox.SetPositionTL(36f, runningY);
			rotZMaxTextBox.sclX = 2;
			rotZMaxTextBox.format = TextBox.FormatTypes.DECIMAL;
			rotZMaxTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.rotZMaxTextBoxLoseFocus);
			rotZRangeGUI.Add(rotZMaxTextBox);

			rotZRangeGUI.visible = false;
				

			runningY += 2.5f;

			rotZVelocityDisplay = AddTextDisplay();
			rotZVelocityDisplay.SetPositionTL(15, runningY);
			rotZVelocityDisplay.text = "Z Rot Vel:";
			particlePropertiesGUI.Add(rotZVelocityDisplay);

			rotZVelocityFixedOrRange = AddComboBox();
			rotZVelocityFixedOrRange.sclX = 3.5f;
			rotZVelocityFixedOrRange.AddItem("Fixed");
			rotZVelocityFixedOrRange.AddItem("Range");
			rotZVelocityFixedOrRange.text = "Fixed";
			rotZVelocityFixedOrRange.SetPositionTL(24.5f, runningY);
			rotZVelocityFixedOrRange.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateRotZVelocityRange);
			rotZVelocityFixedOrRange.onItemClick += new FrbGuiMessage(PropertyWindowMessages.rotZVelocityFixedOrRangeItemClick);
			particlePropertiesGUI.Add(rotZVelocityFixedOrRange);


			rotZVelocityMinTextBox = AddTextBox();
			rotZVelocityMinTextBox.SetPositionTL(30.5f, runningY);
			rotZVelocityMinTextBox.sclX = 2;
			rotZVelocityMinTextBox.format = TextBox.FormatTypes.DECIMAL;
			rotZVelocityMinTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.rotZVelocityMinTextBoxLoseFocus);
			particlePropertiesGUI.Add(rotZVelocityMinTextBox);

			rotZVelocityRangeGUI = new WindowArray();

			rotZVelocityTo = AddTextDisplay();
			rotZVelocityTo.SetPositionTL(32.0f, runningY);
			rotZVelocityTo.text = "to";
			rotZVelocityRangeGUI.Add(rotZVelocityTo);

			rotZVelocityMaxTextBox = AddTextBox();
			rotZVelocityMaxTextBox.SetPositionTL(36, runningY);
			rotZVelocityMaxTextBox.sclX = 2;
			rotZVelocityMaxTextBox.format = TextBox.FormatTypes.DECIMAL;
			rotZVelocityMaxTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.rotZVelocityMaxTextBoxLoseFocus);
			rotZVelocityRangeGUI.Add(rotZVelocityMaxTextBox);

			rotZVelocityRangeGUI.visible = false;

			runningY += 3;
			#endregion

			#region velocityLoss
			velocityLossPercentageRate = AddTextDisplay();
			velocityLossPercentageRate.SetPositionTL(15, runningY);
			velocityLossPercentageRate.text = "Velocity Loss %:";
			particlePropertiesGUI.Add(velocityLossPercentageRate);

			velocityLossTextBox = AddTextBox();
			velocityLossTextBox.SetPositionTL(28, runningY);
			velocityLossTextBox.sclX = 2.5f;
			velocityLossTextBox.format = TextBox.FormatTypes.DECIMAL;
			velocityLossTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.velocityLossTextBoxLoseFocus);
			particlePropertiesGUI.Add(velocityLossTextBox);

			runningY += 3;
			#endregion

			#region removal event GUI
			removalEvent = AddTextDisplay();
			removalEvent.text = "Removal Event:";
			removalEvent.SetPositionTL(15, runningY);
			particlePropertiesGUI.Add(removalEvent);

			removalEventComboBox = AddComboBox();
			removalEventComboBox.SetPositionTL(31, runningY);
			removalEventComboBox.sclX = 6f;
			removalEventComboBox.AddItem("Fade out");
			removalEventComboBox.AddItem("Out of screen");
			removalEventComboBox.AddItem("Timed");
			removalEventComboBox.AddItem("None");
			removalEventComboBox.text = "None";
			removalEventComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.removalEventComboBoxItemClick);
			removalEventComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateLastingTimeGUI);
			particlePropertiesGUI.Add(removalEventComboBox);

			runningY += 3;

			lastingTimeGUI = new WindowArray();
			lastingTime = AddTextDisplay();
			lastingTime.text = "Seconds Lasting:";
			lastingTime.SetPositionTL(15, runningY);
			lastingTimeGUI.Add(lastingTime);

			lastingTimeTextBox = AddTextBox();
			lastingTimeTextBox.SetPositionTL(28, runningY);
			lastingTimeTextBox.sclX = 3;
			lastingTimeTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.lastingTimeTextBoxLoseFocus);			
			lastingTimeGUI.Add(lastingTimeTextBox);
			lastingTimeTextBox.format = TextBox.FormatTypes.DECIMAL;

			lastingTimeGUI.visible = false;

			#endregion

			particlePropertiesGUI.visible = false;
			propertiesEditingListBox.AddWindowArray("Particle Prop.", particlePropertiesGUI);


			#endregion				
	
			#region emissionAreaGUI
			emissionAreaGUI = new WindowArray();

			runningY = 2;

			emissionAreaType = AddComboBox();
			emissionAreaType.SetPositionTL(24, runningY);
			emissionAreaType.sclX = 8;
			emissionAreaType.AddItem("Point");
			emissionAreaType.AddItem("Rectangle");
			emissionAreaType.AddItem("Cube");
			emissionAreaType.text = "Point";
			emissionAreaType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateEmissionAreaGUIVisibility);
			emissionAreaType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.ChangeAreaEmissionType);

			emissionAreaGUI.Add(emissionAreaType);


			runningY += 2.5f;

			emissionAreaSclXTextDisplay = this.AddTextDisplay();
			emissionAreaSclXTextDisplay.SetPositionTL(15, runningY);
			emissionAreaSclXTextDisplay.text = "Emission Area SclX:";
			emissionAreaGUI.Add(emissionAreaSclXTextDisplay);

			emissionAreaSclX = AddUpDown();
			emissionAreaSclX.sclX = 3f;
			emissionAreaSclX.SetPositionTL(30.2f, runningY);
			emissionAreaSclX.CurrentValue = 1;
			emissionAreaSclX.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.EmissionAreaSclXChange);
			emissionAreaGUI.Add(emissionAreaSclX);

			runningY += 2;

			emissionAreaSclYTextDisplay = this.AddTextDisplay();
			emissionAreaSclYTextDisplay.SetPositionTL(15, runningY);
			emissionAreaSclYTextDisplay.text = "Emission Area SclY:";
			emissionAreaGUI.Add(emissionAreaSclYTextDisplay);

			emissionAreaSclY = AddUpDown();
			emissionAreaSclY.sclX = 3f;
			emissionAreaSclY.SetPositionTL(30.2f, runningY);
			emissionAreaSclY.CurrentValue = 1;
			emissionAreaSclY.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.EmissionAreaSclYChange);
			emissionAreaGUI.Add(emissionAreaSclY);


			runningY += 2;

			emissionAreaSclZTextDisplay = this.AddTextDisplay();
			emissionAreaSclZTextDisplay.SetPositionTL(15, runningY);
			emissionAreaSclZTextDisplay.text = "Emission Area SclZ:";
			emissionAreaGUI.Add(emissionAreaSclZTextDisplay);

			emissionAreaSclZ = AddUpDown();
			emissionAreaSclZ.sclX = 3f;
			emissionAreaSclZ.SetPositionTL(30.2f, runningY);
			emissionAreaSclZ.CurrentValue = 1;
			emissionAreaSclZ.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.EmissionAreaSclZChange);
			emissionAreaGUI.Add(emissionAreaSclZ);





			emissionAreaGUI.visible = false;
			propertiesEditingListBox.AddWindowArray("Emission Area", emissionAreaGUI);

			#endregion

			#region attachment props GUI
		
			relativePropertiesGUI = new WindowArray();

			runningY = 4.5f;

			attachmentInformationTextDisplay = this.AddTextDisplay();
			attachmentInformationTextDisplay.text = "Attached To:  null";
			attachmentInformationTextDisplay.SetPositionTL(15, runningY);
			relativePropertiesGUI.Add(attachmentInformationTextDisplay);

			runningY += 2.5f;

			relXDisplay = this.AddTextDisplay();
			relXDisplay.text = "RelX:";
			relXDisplay.SetPositionTL(15, runningY);
			relativePropertiesGUI.Add(relXDisplay);


			relXTextBox = this.AddTextBox();
			relXTextBox.SetPositionTL(25, runningY);
			relXTextBox.sclX = 3;
			relXTextBox.format = TextBox.FormatTypes.DECIMAL;
			relXTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.RelXTextBoxLoseFocus);
			relativePropertiesGUI.Add(relXTextBox);

			runningY += 2.5f;

			relYDisplay = this.AddTextDisplay();
			relYDisplay.text = "RelY:";
			relYDisplay.SetPositionTL(15, runningY);

			relativePropertiesGUI.Add(relYDisplay);

			relYTextBox = this.AddTextBox();
			relYTextBox.SetPositionTL(25, runningY);
			relYTextBox.sclX = 3;
			relYTextBox.format = TextBox.FormatTypes.DECIMAL;
			relYTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.RelYTextBoxLoseFocus);
			relativePropertiesGUI.Add(relYTextBox);

			runningY += 2.5f;


			relZDisplay = this.AddTextDisplay();
			relZDisplay.text = "RelZ:";
			relZDisplay.SetPositionTL(15, runningY);
			relativePropertiesGUI.Add(relZDisplay);

			relZTextBox = this.AddTextBox();
			relZTextBox.SetPositionTL(25, runningY);
			relZTextBox.sclX = 3;

			relZTextBox.format = TextBox.FormatTypes.DECIMAL;
			relZTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.RelZTextBoxLoseFocus);
			relativePropertiesGUI.Add(relZTextBox);

			runningY += 2.5f;


			relRotZDisplay = this.AddTextDisplay();
			relRotZDisplay.text = "RelRotZ:";
			relRotZDisplay.SetPositionTL(15, runningY);
			relativePropertiesGUI.Add(relRotZDisplay);

			relRotZTextBox = this.AddTextBox();
			relRotZTextBox.SetPositionTL(25, runningY);
			relRotZTextBox.sclX = 3;
			relRotZTextBox.format = TextBox.FormatTypes.DECIMAL;
			relativePropertiesGUI.Add(relRotZTextBox);

			runningY += 3.5f;
			
			considerParentVelocityToggleButton = this.AddToggleButton();
			considerParentVelocityToggleButton.SetPositionTL(28, runningY);
			considerParentVelocityToggleButton.sclX = 9.4f;
			considerParentVelocityToggleButton.sclY = 1.3f;
			considerParentVelocityToggleButton.SetText("Consider Parent Velocity OFF", "Consider Parent Velocity ON");
			considerParentVelocityToggleButton.onClick += new FrbGuiMessage(PropertyWindowMessages.ConsiderParentVelocityToggleButtonClick);
			relativePropertiesGUI.Add(considerParentVelocityToggleButton);
			
			relativePropertiesGUI.visible = false;
			propertiesEditingListBox.AddWindowArray("Relative Prop.", relativePropertiesGUI);

			#endregion

			#region sprite color operation window

			WindowArray colorOpWA = new WindowArray();

			TextDisplay tempTextDisplay;

			tempTextDisplay = AddTextDisplay();
			tempTextDisplay.text = "Color Op Type:";
			tempTextDisplay.SetPositionTL(15, 4f);
			colorOpWA.Add(tempTextDisplay);

			#region color op types
			noColorOp = AddToggleButton();
			noColorOp.sclX = 4f;
			noColorOp.sclY = 1f;
			noColorOp.SetPositionTL(19, 7f);
			noColorOp.text = "None";
			noColorOp.Press();
			noColorOp.SetOneAlwaysDown(true);
			noColorOp.onClick += new FrbGuiMessage(PropertyWindowMessages.noColorOpClick);
			colorOpWA.Add(noColorOp);

			addColorOp = AddToggleButton();
			addColorOp.sclX = 4f;
			addColorOp.sclY = 1f;
			addColorOp.SetPositionTL(19, 9f);
			addColorOp.text = "Add";
			addColorOp.onClick += new FrbGuiMessage(PropertyWindowMessages.addColorOpClick);
			noColorOp.AddToRadioGroup(addColorOp);
			colorOpWA.Add(addColorOp);

			addSignedColorOp = AddToggleButton();
			addSignedColorOp.sclX = 4f;
			addSignedColorOp.sclY = 1f;
			addSignedColorOp.SetPositionTL(19, 11f);
			addSignedColorOp.text = "AddSigned";
			addSignedColorOp.onClick += new FrbGuiMessage(PropertyWindowMessages.addSignedColorOpClick);
			noColorOp.AddToRadioGroup(addSignedColorOp);
			colorOpWA.Add(addSignedColorOp);

			modulateColorOp = AddToggleButton();
			modulateColorOp.sclX = 4f;
			modulateColorOp.sclY = 1f;
			modulateColorOp.SetPositionTL(19, 13f);
			modulateColorOp.text = "Modulate";
			modulateColorOp.onClick += new FrbGuiMessage(PropertyWindowMessages.modulateColorOpClick);
			noColorOp.AddToRadioGroup(modulateColorOp);
			colorOpWA.Add(modulateColorOp);

			subtractColorOp = AddToggleButton();
			subtractColorOp.sclX = 4f;
			subtractColorOp.sclY = 1f;
			subtractColorOp.SetPositionTL(19, 15f);
			subtractColorOp.text = "Subtract";
			subtractColorOp.onClick += new FrbGuiMessage(PropertyWindowMessages.subtractColorOpClick);
			noColorOp.AddToRadioGroup(subtractColorOp);
			colorOpWA.Add(subtractColorOp);

			#endregion

			#region RED updown
			tempTextDisplay = AddTextDisplay();
			tempTextDisplay.text = "R:";
			tempTextDisplay.SetPositionTL(25, 7f);
			colorOpWA.Add(tempTextDisplay);

			tintRed = AddUpDown();
			tintRed.SetPositionTL(29.5f, 7);
			tintRed.maxValue = 255;
			tintRed.minValue = 0;
			tintRed.sclX = 3;
			tintRed.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintRedChange);
			colorOpWA.Add(tintRed);

			tintRedRate = AddUpDown();
			tintRedRate.SetPositionTL(36, 7);
			tintRedRate.sclX = 3;
			tintRedRate.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintRedRateChange);
			colorOpWA.Add(tintRedRate);


			#endregion

			#region GREEN updown
			tempTextDisplay = AddTextDisplay();
			tempTextDisplay.text = "G:";
			tempTextDisplay.SetPositionTL(25, 10f);
			colorOpWA.Add(tempTextDisplay);

			tintGreen = AddUpDown();
			tintGreen.SetPositionTL(29.5f, 10);
			tintGreen.maxValue = 255;
			tintGreen.minValue = 0;
			tintGreen.sclX = 3;
			tintGreen.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintGreenChange);
			colorOpWA.Add(tintGreen);

			tintGreenRate = AddUpDown();
			tintGreenRate.SetPositionTL(36, 10);
			tintGreenRate.sclX = 3;
			tintGreenRate.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintGreenRateChange);
			colorOpWA.Add(tintGreenRate);

			#endregion

			#region BLUE updown
			tempTextDisplay = AddTextDisplay();
			tempTextDisplay.text = "B:";
			tempTextDisplay.SetPositionTL(25, 13f);
			colorOpWA.Add(tempTextDisplay);

			tintBlue = AddUpDown();
			tintBlue.SetPositionTL(29.5f, 13);
			tintBlue.maxValue = 255;
			tintBlue.minValue = 0;
			tintBlue.sclX = 3;
			tintBlue.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintBlueChange);
			colorOpWA.Add(tintBlue);

			tintBlueRate = AddUpDown();
			tintBlueRate.SetPositionTL(36, 13);
			tintBlueRate.sclX = 3;
			tintBlueRate.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.tintBlueRateChange);
			colorOpWA.Add(tintBlueRate);

			#endregion
	

			#region transparency
			tempTextDisplay = AddTextDisplay();
			tempTextDisplay.text = "Blend Op Type:";
			tempTextDisplay.SetPositionTL(15, 19);
			colorOpWA.Add(tempTextDisplay);

			fadeDisplay = AddTextDisplay();
			fadeDisplay.SetPositionTL(23f, 21);
			fadeDisplay.text = "Fade:";
			
			colorOpWA.Add(fadeDisplay);

			fadeUpDown = AddUpDown();
			fadeUpDown.SetPositionTL(29.5f, 21);
			fadeUpDown.sclX = 3;
			fadeUpDown.minValue = 0;
			fadeUpDown.maxValue = 255;
			fadeUpDown.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.fadeTextBoxLoseFocus);
			colorOpWA.Add(fadeUpDown);

			fadeRateUpDown = AddUpDown();
			fadeRateUpDown.sclX = 3f;
			fadeRateUpDown.SetPositionTL(36, 21);
			fadeRateUpDown.onGUIChange += new FrbGuiMessage(PropertyWindowMessages.fadeRateTextBoxLoseFocus);
			colorOpWA.Add(fadeRateUpDown);



			regularBlend = AddToggleButton();
			regularBlend.sclY = 1f;
			regularBlend.sclX = 3.5f;
			regularBlend.text = "Regular";
			regularBlend.SetPositionTL(19, 21);
			regularBlend.SetOneAlwaysDown(true);
			regularBlend.onClick += new FrbGuiMessage(PropertyWindowMessages.regularBlendClick);
			colorOpWA.Add(regularBlend);

			additiveBlend = AddToggleButton();
			additiveBlend.sclY = 1f;
			additiveBlend.sclX = 3.5f;
			additiveBlend.text = "Additive";
			additiveBlend.SetPositionTL(19, 23);
			additiveBlend.onClick += new FrbGuiMessage(PropertyWindowMessages.additiveBlendClick);
			regularBlend.AddToRadioGroup(additiveBlend);
			colorOpWA.Add(additiveBlend);


			modulateBlend = AddToggleButton();
			modulateBlend.sclX = 3.5f;
			modulateBlend.sclY = 1f;
			modulateBlend.text = "Modulate";
			modulateBlend.SetPositionTL(19, 25);
			modulateBlend.onClick += new FrbGuiMessage(PropertyWindowMessages.modulateBlendClick);
			regularBlend.AddToRadioGroup(modulateBlend);
			colorOpWA.Add(modulateBlend);

			modulate2XBlend = AddToggleButton();
			modulate2XBlend.sclX = 3.5f;
			modulate2XBlend.sclY = 1f;
			modulate2XBlend.text = "Modulate2X";
			modulate2XBlend.SetPositionTL(19, 27);
			modulate2XBlend.onClick += new FrbGuiMessage(PropertyWindowMessages.modulate2XBlendClick);
			regularBlend.AddToRadioGroup(modulate2XBlend);
			colorOpWA.Add(modulate2XBlend);



			#endregion

			propertiesEditingListBox.AddWindowArray("Tint and fade", colorOpWA);
			colorOpWA.visible = false;


			#endregion

			#region initialVelocityGUI
			initialVelocityGUI = new WindowArray();

			spreadStyle = AddTextDisplay();
			spreadStyle.text = "Spread Style:";
			spreadStyle.SetPositionTL(sclX-5, 4);
				
			initialVelocityGUI.Add(spreadStyle);
				


			spreadStyleComboBox = AddComboBox();
			spreadStyleComboBox.AddItem("square");
			spreadStyleComboBox.AddItem("circle");
			spreadStyleComboBox.sclX = 8;
			spreadStyleComboBox.SetPositionTL(sclX+3.5f, 8f);
			spreadStyleComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateSpreadGUI);
			spreadStyleComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.spreadStyleComboBoxItemClick);
			initialVelocityGUI.Add(spreadStyleComboBox);

			initialVelocityGUI.visible = false;

			propertiesEditingListBox.AddWindowArray("Velocity", initialVelocityGUI);


			#region squareSpreadGUI
			
			squareSpreadGUI = new WindowArray();

			#region xVelocity
			xVelocityText = AddTextDisplay();
			xVelocityText.text = "X Vel.:";
			xVelocityText.SetPositionTL(sclX-5, 13);
			squareSpreadGUI.Add(xVelocityText);

			xVelocityType = AddComboBox();
			xVelocityType.SetPositionTL(sclX+3, 13);
			xVelocityType.sclX = 3.5f;
			xVelocityType.AddItem("Fixed");
			xVelocityType.AddItem("Range");
			xVelocityType.text = "Fixed";
			xVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateVelocityRangeGUI);
			xVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.xVelocityTypeLoseFocus);
			squareSpreadGUI.Add(xVelocityType);

			xMinValue = AddTextBox();
			xMinValue.sclX = 2;
			xMinValue.SetPositionTL(sclX+9, 13);
			xMinValue.format = TextBox.FormatTypes.DECIMAL;
			xMinValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xMinValueLoseFocus);
			squareSpreadGUI.Add(xMinValue);

			xRangeGUI = new WindowArray();
			xTo = AddTextDisplay();
			xTo.text = "to";
			xTo.SetPositionTL(sclX+11.5f, 13);
			xRangeGUI.Add(xTo);

			xMaxValue = AddTextBox();
			xMaxValue.sclX = 2;
			xMaxValue.SetPositionTL(sclX+15.5f, 13);
			xMaxValue.format = TextBox.FormatTypes.DECIMAL;
			xMaxValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xMaxValueLoseFocus);
			xRangeGUI.Add(xMaxValue);

			xRangeGUI.visible = false;
			#endregion
			#region yVelocity
			yVelocityType = AddComboBox();
			yVelocityType.SetPositionTL(sclX+3, 16);
			yVelocityType.sclX = 3.5f;
			yVelocityType.AddItem("Fixed");
			yVelocityType.AddItem("Range");
			yVelocityType.text = "Fixed";
			yVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateVelocityRangeGUI);
			yVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.yVelocityTypeLoseFocus);
			squareSpreadGUI.Add(yVelocityType);

			yMinValue = AddTextBox();
			yMinValue.sclX = 2;
			yMinValue.SetPositionTL(sclX+9, 16);
			yMinValue.format = TextBox.FormatTypes.DECIMAL;
			yMinValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.yMinValueLoseFocus);
			squareSpreadGUI.Add(yMinValue);

			yRangeGUI = new WindowArray();
			yTo = AddTextDisplay();
			yTo.text = "to";
			yTo.SetPositionTL(sclX+11.5f, 16);
			yRangeGUI.Add(yTo);

			yMaxValue = AddTextBox();
			yMaxValue.sclX = 2;
			yMaxValue.SetPositionTL(sclX+15.5f, 16);
			yMaxValue.format = TextBox.FormatTypes.DECIMAL;
			yMaxValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.yMaxValueLoseFocus);
			yRangeGUI.Add(yMaxValue);

			yRangeGUI.visible = false;


			yVelocityText = AddTextDisplay();
			yVelocityText.text = "Y Vel.:";
			yVelocityText.SetPositionTL(sclX-5, 16);
			squareSpreadGUI.Add(yVelocityText);
			#endregion
			#region zVelocity

			zVelocityType = AddComboBox();
			zVelocityType.SetPositionTL(sclX+3, 19);
			zVelocityType.sclX = 3.5f;
			zVelocityType.AddItem("Fixed");
			zVelocityType.AddItem("Range");
			zVelocityType.text = "Fixed";
			zVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateVelocityRangeGUI);
			zVelocityType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.zVelocityTypeLoseFocus);
			squareSpreadGUI.Add(zVelocityType);

			zMinValue = AddTextBox();
			zMinValue.sclX = 2;
			zMinValue.SetPositionTL(sclX+9, 19);
			zMinValue.format = TextBox.FormatTypes.DECIMAL;
			zMinValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.zMinValueLoseFocus);
			squareSpreadGUI.Add(zMinValue);

			zRangeGUI = new WindowArray();
			zTo = AddTextDisplay();
			zTo.text = "to";
			zTo.SetPositionTL(sclX+11.5f, 19);
			zRangeGUI.Add(zTo);

			zMaxValue = AddTextBox();
			zMaxValue.sclX = 2;
			zMaxValue.SetPositionTL(sclX+15.5f, 19);
			zMaxValue.format = TextBox.FormatTypes.DECIMAL;
			zMaxValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.zMaxValueLoseFocus);
			zRangeGUI.Add(zMaxValue);

				
			zVelocityText = AddTextDisplay();
			zVelocityText.text = "Z Vel.:";
			zVelocityText.SetPositionTL(sclX-5, 19);
			squareSpreadGUI.Add(zVelocityText);

			zRangeGUI.visible = false;
			#endregion
			squareSpreadGUI.visible = false;
			#endregion
			#region circleSpreadGUI

			circularSpreadGUI = new WindowArray();

			outwardVelocity = AddTextDisplay();
			outwardVelocity.text = "Outward Velocity:";
			outwardVelocity.SetPositionTL(sclX-5, 13);
			circularSpreadGUI.Add(outwardVelocity);


			outwardVelocityRangeOrFixed = AddComboBox();
			outwardVelocityRangeOrFixed.sclX = 4;
			outwardVelocityRangeOrFixed.AddItem("Fixed");
			outwardVelocityRangeOrFixed.AddItem("Range");
			outwardVelocityRangeOrFixed.SetPositionTL(sclX+9.5f, 13);
			outwardVelocityRangeOrFixed.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateCircularVelocityRangeGUI);
			outwardVelocityRangeOrFixed.onItemClick += new FrbGuiMessage(PropertyWindowMessages.outwardVelocityRangeOrFixedItemClicked);
			circularSpreadGUI.Add(outwardVelocityRangeOrFixed);


			outwardVelocityTextBox = AddTextBox();
			outwardVelocityTextBox.SetPositionTL(sclX, 16);
			outwardVelocityTextBox.sclX = 2;
			outwardVelocityTextBox.format = TextBox.FormatTypes.DECIMAL;
			outwardVelocityTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.outwardVelocityTextBoxLoseFocus);
			circularSpreadGUI.Add(outwardVelocityTextBox);

			outwardVelocityRangeGUI = new WindowArray();
			outwardVelocityTo = AddTextDisplay();
			outwardVelocityTo.text = "to";
			outwardVelocityTo.SetPositionTL(sclX+2.5f, 16);
			outwardVelocityRangeGUI.Add(outwardVelocityTo);

			outwardVelocityTextBoxMax = AddTextBox();
			outwardVelocityTextBoxMax.sclX = 2;
			outwardVelocityTextBoxMax.format = TextBox.FormatTypes.DECIMAL;
			outwardVelocityTextBoxMax.SetPositionTL(sclX+6.5f, 16);
			outwardVelocityTextBoxMax.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.outwardVelocityTextBoxMaxLoseFocus);
			outwardVelocityRangeGUI.Add(outwardVelocityTextBoxMax);

			outwardVelocityRangeGUI.visible = false;


			wedgeOrFull = AddComboBox();
			wedgeOrFull.sclX = 5;
			wedgeOrFull.AddItem("circle");
			wedgeOrFull.AddItem("wedge");
			wedgeOrFull.AddItem("sphere");
			wedgeOrFull.text = "circle";
			wedgeOrFull.SetPositionTL(sclX, 19);
			wedgeOrFull.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateWedgeOrFullGUI);
			wedgeOrFull.onItemClick += new FrbGuiMessage(PropertyWindowMessages.wedgeOrFullItemClick);
			circularSpreadGUI.Add(wedgeOrFull);

			circularSpreadGUI.visible = false;
				
				
			wedgeGUI = new WindowArray();
				
			directionAngle = AddTextDisplay();
			directionAngle.text = "Angle of Direction:";
			directionAngle.SetPositionTL(sclX-5, 22);
			wedgeGUI.Add(directionAngle);

			directionAngleTextBox = AddTextBox();
			directionAngleTextBox.SetPositionTL( sclX+9, 22);
			directionAngleTextBox.sclX = 2;
			directionAngleTextBox.format = TextBox.FormatTypes.DECIMAL;
			directionAngleTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.directionAngleTextBoxLoseFocus);
			wedgeGUI.Add(directionAngleTextBox);

			spreadAngle = AddTextDisplay();
			spreadAngle.text = "Angle of Spread:";
			spreadAngle.SetPositionTL(sclX-5, 25);
			wedgeGUI.Add(spreadAngle);

			spreadAngleTextBox = AddTextBox();
			spreadAngleTextBox.SetPositionTL( sclX+9, 25);
			spreadAngleTextBox.sclX = 2;
			spreadAngleTextBox.format = TextBox.FormatTypes.DECIMAL;
			spreadAngleTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.spreadAngleTextBoxLoseFocus);
			wedgeGUI.Add(spreadAngleTextBox);
			wedgeGUI.visible = false;



			#endregion

			#endregion

			#region initialAccelerationGUI

			initialAccelerationGUI = new WindowArray();
			xAccelerationRangeGUI = new WindowArray();
			yAccelerationRangeGUI = new WindowArray();
			zAccelerationRangeGUI = new WindowArray();


			xAccelerationText = AddTextDisplay();
			xAccelerationText.text = "X Acc.:";
			xAccelerationText.SetPositionTL(sclX-5, 5);
			initialAccelerationGUI.Add(xAccelerationText);

			xAccelerationType = AddComboBox();
			xAccelerationType.SetPositionTL(sclX+3, 5);
			xAccelerationType.sclX = 3.5f;
			xAccelerationType.AddItem("Fixed");
			xAccelerationType.AddItem("Range");
			xAccelerationType.text = "Fixed";
			xAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateAccelerationRangeGUI);
			xAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.xAccelerationTypeSelectItem);
			initialAccelerationGUI.Add(xAccelerationType);

			xMinAccelerationValue = AddTextBox();
			xMinAccelerationValue.sclX = 2;
			xMinAccelerationValue.SetPositionTL(sclX+9, 5);
			xMinAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			xMinAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xMinAccelerationValueLoseFocus);
			initialAccelerationGUI.Add(xMinAccelerationValue);

			xAccelerationTo = AddTextDisplay();
			xAccelerationTo.text = "to";
			xAccelerationTo.SetPositionTL(sclX+11.5f, 5);
			xAccelerationRangeGUI.Add(xAccelerationTo);

			xMaxAccelerationValue = AddTextBox();
			xMaxAccelerationValue.sclX = 2;
			xMaxAccelerationValue.SetPositionTL(sclX+15.5f, 5);
			xMaxAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			xMaxAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.xMaxAccelerationValueLoseFocus);
			xAccelerationRangeGUI.Add(xMaxAccelerationValue);

			xAccelerationRangeGUI.visible = false;

			yAccelerationType = AddComboBox();
			yAccelerationType.SetPositionTL(sclX+3, 8);
			yAccelerationType.sclX = 3.5f;
			yAccelerationType.AddItem("Fixed");
			yAccelerationType.AddItem("Range");
			yAccelerationType.text = "Fixed";
			yAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateAccelerationRangeGUI);
			yAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.yAccelerationTypeSelectItem);
			initialAccelerationGUI.Add(yAccelerationType);

			yMinAccelerationValue = AddTextBox();
			yMinAccelerationValue.sclX = 2;
			yMinAccelerationValue.SetPositionTL(sclX+9, 8);
			yMinAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			yMinAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.yMinAccelerationValueLoseFocus);
			initialAccelerationGUI.Add(yMinAccelerationValue);

			yAccelerationTo = AddTextDisplay();
			yAccelerationTo.text = "to";
			yAccelerationTo.SetPositionTL(sclX+11.5f, 8);
			yAccelerationRangeGUI.Add(yAccelerationTo);

			yMaxAccelerationValue = AddTextBox();
			yMaxAccelerationValue.sclX = 2;
			yMaxAccelerationValue.SetPositionTL(sclX+15.5f, 8);
			yMaxAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			yMaxAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.yMaxAccelerationValueLoseFocus);
			yAccelerationRangeGUI.Add(yMaxAccelerationValue);

			yAccelerationRangeGUI.visible = false;


			yAccelerationText = AddTextDisplay();
			yAccelerationText.text = "Y Acc.:";
			yAccelerationText.SetPositionTL(sclX-5, 8);
			initialAccelerationGUI.Add(yAccelerationText);

			zAccelerationType = AddComboBox();
			zAccelerationType.SetPositionTL(sclX+3, 11);
			zAccelerationType.sclX = 3.5f;
			zAccelerationType.AddItem("Fixed");
			zAccelerationType.AddItem("Range");
			zAccelerationType.text = "Fixed";
			zAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateAccelerationRangeGUI);
			zAccelerationType.onItemClick += new FrbGuiMessage(PropertyWindowMessages.zAccelerationTypeSelectItem);
			initialAccelerationGUI.Add(zAccelerationType);

			zMinAccelerationValue = AddTextBox();
			zMinAccelerationValue.sclX = 2;
			zMinAccelerationValue.SetPositionTL(sclX+9, 11);
			zMinAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			zMinAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.zMinAccelerationValueLoseFocus);
			initialAccelerationGUI.Add(zMinAccelerationValue);

			zAccelerationTo = AddTextDisplay();
			zAccelerationTo.text = "to";
			zAccelerationTo.SetPositionTL(sclX+11.5f, 11);
			zAccelerationRangeGUI.Add(zAccelerationTo);

			zMaxAccelerationValue = AddTextBox();
			zMaxAccelerationValue.sclX = 2;
			zMaxAccelerationValue.SetPositionTL(sclX+15.5f, 11);
			zMaxAccelerationValue.format = TextBox.FormatTypes.DECIMAL;
			zMaxAccelerationValue.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.zMaxAccelerationValueLoseFocus);
			zAccelerationRangeGUI.Add(zMaxAccelerationValue);

				
			zAccelerationText = AddTextDisplay();
			zAccelerationText.text = "Z Acc.:";
			zAccelerationText.SetPositionTL(sclX-5, 11);
			initialAccelerationGUI.Add(zAccelerationText);

			zAccelerationRangeGUI.visible = false;
			initialAccelerationGUI.visible = false;

			propertiesEditingListBox.AddWindowArray("Acceleration", initialAccelerationGUI);


			#endregion

			#region emissionTimingGUI
			emissionTimingGUI = new WindowArray();

			emissionEvent = AddTextDisplay();
			emissionEvent.text = "Emission Event:";
			emissionEvent.SetPositionTL(sclX-5, 6);
			emissionTimingGUI.Add(emissionEvent);

			emissionEventComboBox = AddComboBox();
			emissionEventComboBox.sclX = 5;
			emissionEventComboBox.SetPositionTL(sclX+10, 6);
			emissionEventComboBox.AddItem("Call only");
			emissionEventComboBox.AddItem("Timed");
			emissionEventComboBox.text = "Call only";
			emissionEventComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.updateTimingGUI);
			emissionEventComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.emissionEventComboBoxItemSelect);
			emissionTimingGUI.Add(emissionEventComboBox);

			timingGUI = new WindowArray();

			onceEvery = AddTextDisplay();
			onceEvery.SetPositionTL(sclX-5, 10);
			onceEvery.text = "Once every:";
			timingGUI.Add(onceEvery);

			secondFrequencyTextBox = AddTextBox();
			secondFrequencyTextBox.SetPositionTL(sclX+6, 10);
			secondFrequencyTextBox.sclX = 3.5f;
			secondFrequencyTextBox.format = TextBox.FormatTypes.DECIMAL;
			secondFrequencyTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.secondFrequencyTextBoxLoseFocus);
			timingGUI.Add(secondFrequencyTextBox);

			millisecondsDisplay = AddTextDisplay();
			millisecondsDisplay.text = "ms.";
			millisecondsDisplay.SetPositionTL(sclX+10, 10);
			timingGUI.Add(millisecondsDisplay);

			numberPerEmissionDisplay = AddTextDisplay();
			numberPerEmissionDisplay.text = "Number of particles per emission:";
			numberPerEmissionDisplay.SetPositionTL(sclX-5, 13);
			emissionTimingGUI.Add(numberPerEmissionDisplay);

			numberPerEmissionTextBox = AddTextBox();
			numberPerEmissionTextBox.sclX = 1.7f;
			numberPerEmissionTextBox.SetPositionTL(sclX+17, 13);
			numberPerEmissionTextBox.format = TextBox.FormatTypes.DECIMAL;
			numberPerEmissionTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.numberPerEmissionTextBoxLoseFocus);
			emissionTimingGUI.Add(numberPerEmissionTextBox);

			emissionTimingGUI.visible = false;
			timingGUI.visible = false;


			propertiesEditingListBox.AddWindowArray("Emission Timing", emissionTimingGUI);

			#endregion
	
			#region instruction GUI
			instructionGUI = new WindowArray();

			instructionListBox = this.AddListBox();
			instructionListBox.SetPositionTL(27f, 11);
			instructionListBox.sclX = 12f;
			instructionListBox.sclY = 10;
			instructionListBox.onHighlight += new FrbGuiMessage(PropertyWindowMessages.ListBoxSelectInstruction);
			instructionGUI.Add(instructionListBox);
			
			runningY = 22;

			addInstructionButton = this.AddButton();
			addInstructionButton.text = "Add Instruction";
			addInstructionButton.sclX = 6f;
			addInstructionButton.SetPositionTL(21f, runningY);
			addInstructionButton.onClick += new FrbGuiMessage(PropertyWindowMessages.AddInstructionButtonClick);
			instructionGUI.Add(addInstructionButton);

			deleteInstructionButton = this.AddButton();
			deleteInstructionButton.text = "Delete Instruction";
			deleteInstructionButton.sclX = 6f;
			deleteInstructionButton.SetPositionTL(33, runningY);
			instructionGUI.Add(deleteInstructionButton);

			runningY += 3;

			typeTextDisplay = this.AddTextDisplay();
			typeTextDisplay.SetPositionTL(15, runningY);
			typeTextDisplay.text = "Type:";
			instructionGUI.Add(typeTextDisplay);

			typeComboBox = this.AddComboBox();
			typeComboBox.SetPositionTL(28, runningY);
			typeComboBox.sclX = 9.5f;
			typeComboBox.text = "<Select Instruction Type>";
			typeComboBox.onItemClick += new FrbGuiMessage(PropertyWindowMessages.ChangeInstructionType);

			#region add the types in the combo box

			typeComboBox.AddItem("<Select Instruction Type>");

			typeComboBox.AddItem("Emit");

			typeComboBox.AddItem("Fade");
			typeComboBox.AddItem("FadeRate");

			typeComboBox.AddItem("X");
			typeComboBox.AddItem("XVelocity");
			typeComboBox.AddItem("XAcceleration");
	
			typeComboBox.AddItem("Y");
			typeComboBox.AddItem("YVelocity");
			typeComboBox.AddItem("YAcceleration");

			typeComboBox.AddItem("Z");
			typeComboBox.AddItem("ZVelocity");
			typeComboBox.AddItem("ZAcceleration");

			#endregion

			instructionGUI.Add(typeComboBox);

			runningY += 2.5f;

			value1TextDisplay = this.AddTextDisplay();
			value1TextDisplay.visible = false;
			value1TextDisplay.SetPositionTL(15, runningY);
			instructionGUI.Add(value1TextDisplay);
			
			value1Window = null;
			
			runningY += 2.5f;

			value2TextDisplay = this.AddTextDisplay();
			value2TextDisplay.visible = false;
			value2TextDisplay.SetPositionTL(15, runningY);
			instructionGUI.Add(value2TextDisplay);

			value2Window = null;

			runningY += 2.5f;

			value3TextDisplay = this.AddTextDisplay();
			value3TextDisplay.visible = false;
			value3TextDisplay.SetPositionTL(15, runningY);
			instructionGUI.Add(value3TextDisplay);

			value3Window = null;

			runningY += 2.5f;

			instructionTimeTextDisplay = this.AddTextDisplay();
			instructionTimeTextDisplay.text = "Delay (ms):";
			instructionTimeTextDisplay.SetPositionTL(15, runningY);
			instructionGUI.Add(instructionTimeTextDisplay);

			instructionTimeTextBox = this.AddTextBox();
			instructionTimeTextBox.format = TextBox.FormatTypes.INTEGER;
			instructionTimeTextBox.sclX = 5;
			instructionTimeTextBox.SetPositionTL(30, runningY);
			instructionTimeTextBox.onLosingFocus += new FrbGuiMessage(PropertyWindowMessages.SetTimeToExecute);
			instructionGUI.Add(instructionTimeTextBox);

			runningY += 2.5f;

			cycleTimeTextDisplay = this.AddTextDisplay();
			cycleTimeTextDisplay.text = "Cycle Time (ms):";
			cycleTimeTextDisplay.SetPositionTL(15, runningY);
			instructionGUI.Add(cycleTimeTextDisplay);

			cycleTimeTextBox = this.AddTextBox();
			cycleTimeTextBox.format = TextBox.FormatTypes.INTEGER;
			cycleTimeTextBox.sclX = 5;
			cycleTimeTextBox.SetPositionTL(30, runningY);
			instructionGUI.Add(cycleTimeTextBox);

			instructionGUI.visible = false;
			propertiesEditingListBox.AddWindowArray("Instructions", instructionGUI);


			#endregion

		}
	}
}
