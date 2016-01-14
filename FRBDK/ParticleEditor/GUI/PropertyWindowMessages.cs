using System;
using FRB;
using FRB.Gui;
using FRB.Particle;
using FRB.Instructions;
using FRB.Instructions.PositionedObject_Instructions;
using FRB.Instructions.Sprite_Instructions;

namespace ParticleEditor.GUI
{
	/// <summary>
	/// Summary description for PropertyWindowMessages.
	/// </summary>
	public class PropertyWindowMessages
	{
		#region members

		public static SpriteManager sprMan;
		public static GuiManager guiMan;

		public static GuiData guiData;
		public static GameData gameData;

		#endregion

		#region delegate and delegate calling methods

		#region gui for modifying the particle blueprint and particle properties

		public static void emitterNameTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.name = ((TextBox)callingWindow).text;

			CollapseItem ci = guiData.emitterListBoxWindow.emitterListBox.GetHighlightedItems()[0];
			ci.text = ((TextBox)callingWindow).text;
			

		}

		#region position and scale
		public static void xPosTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.x = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);

			gameData.updateMarkerPosition();
		}
		public static void yPosTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.y = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);
			gameData.updateMarkerPosition();
		}
		public static void zPosTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.z = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);
			gameData.updateMarkerPosition();
		}
		public static void xSclTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.sclX = (float)(System.Convert.ToDouble( ((TextBox)callingWindow).text));
		}
		public static void xSclVelocityTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.sclXVelocity = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));
		}
		public static void ySclTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.sclY = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));
		}
		public static void ySclVelocityTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.sclYVelocity = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));
		}

		#endregion

		#region rotation
		public static void rotZFixedOrRangeItemClick(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Fixed")
            //    gameData.currentEmitter.RotZRange = false;
            //else
            //    gameData.currentEmitter.RotZRange = true;
		}
		public static void rotZMinTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.RotZMin = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));

            //guiData.propWindow.rotZMaxTextBox.text = gameData.currentEmitter.RotZMax.ToString();
		}
		public static void rotZMaxTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.RotZMax = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text)); 

            //guiData.propWindow.rotZMinTextBox.text = gameData.currentEmitter.RotZMin.ToString();
		}

		public static void rotZVelocityFixedOrRangeItemClick(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Fixed")
            //    gameData.currentEmitter.RotZVelocityRange = false;
            //else
            //    gameData.currentEmitter.RotZVelocityRange = true;
		}
		public static void rotZVelocityMinTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.RotZVelocityMin = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));

            //guiData.propWindow.rotZVelocityMaxTextBox.text = gameData.currentEmitter.RotZVelocityMax.ToString();
		}
		public static void rotZVelocityMaxTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.RotZVelocityMax = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));

            //guiData.propWindow.rotZVelocityMinTextBox.text = gameData.currentEmitter.RotZVelocityMin.ToString();

		}
		#endregion

		#region velocity loss
		public static void velocityLossTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.Drag = (float)(System.Convert.ToDouble(((TextBox)callingWindow).text));
		}
		#endregion

		#region removal event GUI
		public static void removalEventComboBoxItemClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			string itemString = ((ComboBox)callingWindow).text;

			if(itemString == "Fade out")
				gameData.currentEmitter.RemovalEvent = Emitter.RemovalEventType.Fadeout;
			else if(itemString == "Out of screen")
				gameData.currentEmitter.RemovalEvent = Emitter.RemovalEventType.OutOfScreen;
			else if(itemString == "Timed")
				gameData.currentEmitter.RemovalEvent = Emitter.RemovalEventType.Timed;
			else if(itemString == "None")
				gameData.currentEmitter.RemovalEvent = Emitter.RemovalEventType.None;


		}
		public static void lastingTimeTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.SecondsLasting = (float)System.Convert.ToDouble(((TextBox)callingWindow).text);

		}

		#endregion

		#region color operations and fade

		public static void colorOperationWindowClose(FRB.Gui.Window callingWindow)
		{
			guiData.propWindow.particleColorOperations.Unpress();

		}
		public static void tintRedChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintRed = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);

		}
		public static void tintBlueChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintBlue = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);
		}

		public static void tintGreenChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintGreen = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);
		}

		public static void tintRedRateChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintRedRate= (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);

		}
		public static void tintBlueRateChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintBlueRate = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);
		}

		public static void tintGreenRateChange(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter != null)
				gameData.currentEmitter.ParticleBlueprint.tintGreenRate = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);
		}

		public static void fadeTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.fade = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);

		}
		public static void fadeRateTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.ParticleBlueprint.fadeRate = (float)System.Convert.ToDouble(((UpDown)callingWindow).CurrentValue);
		}


		public static void noColorOpClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;
			gameData.currentEmitter.ParticleBlueprint.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg1;
		}
		public static void addColorOpClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;
			gameData.currentEmitter.ParticleBlueprint.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;
		}
		public static void addSignedColorOpClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;
			gameData.currentEmitter.ParticleBlueprint.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.AddSigned;
		}
		public static void modulateColorOpClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;
			gameData.currentEmitter.ParticleBlueprint.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Modulate;
		}
		public static void subtractColorOpClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;
			gameData.currentEmitter.ParticleBlueprint.colorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Subtract;
		}

		public static void regularBlendClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)				return;
			gameData.currentEmitter.ParticleBlueprint.blend = Sprite.BlendTypes.REGULAR;
		}
		public static void additiveBlendClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)				return;
			gameData.currentEmitter.ParticleBlueprint.blend = Sprite.BlendTypes.ALPHAADD;
		}

		public static void modulateBlendClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)				return;
			gameData.currentEmitter.ParticleBlueprint.blend = Sprite.BlendTypes.MODULATE;
		}

		public static void modulate2XBlendClick(FRB.Gui.Window callingWindow)
		{
			if(gameData.currentEmitter == null)				return;
			gameData.currentEmitter.ParticleBlueprint.blend = Sprite.BlendTypes.MODULATE2X;
		}

		#endregion
		#endregion

		#region emission area GUI

		public static void ChangeAreaEmissionType(Window callingWindow)
		{
			if(gameData.currentEmitter != null)
                gameData.currentEmitter.AreaEmissionType = propWindow.emissionAreaType.text;
		}

		
		public static void EmissionAreaSclXChange(Window callingWindow)
		{
			gameData.currentEmitter.SclX = ((UpDown)callingWindow).CurrentValue;
		}

		public static void EmissionAreaSclYChange(Window callingWindow)
		{
			gameData.currentEmitter.SclY = ((UpDown)callingWindow).CurrentValue;
		}

		public static void EmissionAreaSclZChange(Window callingWindow)
		{
			gameData.currentEmitter.SclZ = ((UpDown)callingWindow).CurrentValue;
		}
		
		#endregion

		#region square velocity GUI

		public static void xMinValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MinXVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);

            //guiData.propWindow.xMaxValue.text = gameData.currentEmitter.MaxXVelocity.ToString();
		}
		public static void xMaxValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MaxXVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);

            //guiData.propWindow.xMinValue.text = gameData.currentEmitter.MinXVelocity.ToString();
		}


		public static void yMinValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MinYVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);

            //guiData.propWindow.yMaxValue.text = gameData.currentEmitter.MaxYVelocity.ToString();
		}
		public static void yMaxValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MaxYVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);

            //guiData.propWindow.yMinValue.text = gameData.currentEmitter.MinYVelocity.ToString();
		}


		public static void zMinValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MinZVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);

            //guiData.propWindow.zMaxValue.text = gameData.currentEmitter.MaxZVelocity.ToString();

		}
		public static void zMaxValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.MaxZVelocity = (float) System.Convert.ToDouble(((TextBox)callingWindow).text);
            //guiData.propWindow.zMinValue.text = gameData.currentEmitter.MinZVelocity.ToString();
		}



		public static void xVelocityTypeLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.XVelocityRange = true;
            //else
            //    gameData.currentEmitter.XVelocityRange = false;

		}
		public static void yVelocityTypeLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.YVelocityRange = true;
            //else
            //    gameData.currentEmitter.YVelocityRange = false;
		}

		public static void zVelocityTypeLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.ZVelocityRange = true;
            //else
            //    gameData.currentEmitter.ZVelocityRange = false;
		}

		#endregion

		#region outward velocity controlling GUI
		public static void outwardVelocityRangeOrFixedItemClicked(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.OutwardVelocityRange = true;
		}

		public static void outwardVelocityTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MinOutwardVelocity = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));
            //guiData.propWindow.outwardVelocityTextBoxMax.text = gameData.currentEmitter.MaxOutwardVelocity.ToString();
		
		}

		public static void outwardVelocityTextBoxMaxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MaxOutwardVelocity = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));

            //guiData.propWindow.outwardVelocityTextBox.text = gameData.currentEmitter.MinOutwardVelocity.ToString();
		}

		public static void wedgeOrFullItemClick(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.OutwardVelocityStyle = guiData.propWindow.wedgeOrFull.text;
		}

		public static void directionAngleTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.OutwardVelocityAngle = (float)System.Convert.ToDouble(((TextBox)callingWindow).text);
		}
		public static void spreadAngleTextBoxLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)		return;
            //gameData.currentEmitter.OutwardVelocitySpread = (float)System.Convert.ToDouble(((TextBox)callingWindow).text);
		}

		public static void spreadStyleComboBoxItemClick(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)
            //    return;
            //gameData.currentEmitter.SpreadStyle = ((ComboBox)callingWindow).text;
		}


		#endregion

		#region acceleratoin controlling GUI
		public static void xMinAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MinXAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));

            //guiData.propWindow.xMaxAccelerationValue.text = gameData.currentEmitter.MaxXAcceleration.ToString();
		}

		public static void yMinAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MinYAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));

            //guiData.propWindow.yMaxAccelerationValue.text = gameData.currentEmitter.MaxYAcceleration.ToString();
		}

		public static void zMinAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MinZAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));
            //guiData.propWindow.zMaxAccelerationValue.text = gameData.currentEmitter.MaxZAcceleration.ToString();
		}

		
		public static void xMaxAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MaxXAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));

            //guiData.propWindow.xMinAccelerationValue.text = gameData.currentEmitter.MinXAcceleration.ToString();
		}

		public static void yMaxAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MaxYAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));
            //guiData.propWindow.yMinAccelerationValue.text = gameData.currentEmitter.MinYAcceleration.ToString();
		}

		public static void zMaxAccelerationValueLoseFocus(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.MaxZAcceleration = (float)(System.Convert.ToDouble((( TextBox)callingWindow).text));
            //guiData.propWindow.zMinAccelerationValue.text = gameData.currentEmitter.MinZAcceleration.ToString();
		}


		public static void xAccelerationTypeSelectItem(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.XAccelerationRange = true;
            //else
            //    gameData.currentEmitter.XAccelerationRange = false;
		}

		public static void yAccelerationTypeSelectItem(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.YAccelerationRange = true;
            //else
            //    gameData.currentEmitter.YAccelerationRange = false;
		}

		public static void zAccelerationTypeSelectItem(FRB.Gui.Window callingWindow)
		{
            //if(gameData.currentEmitter == null)	return;
            //if( ((ComboBox)callingWindow).text == "Range")
            //    gameData.currentEmitter.ZAccelerationRange = true;
            //else
            //    gameData.currentEmitter.ZAccelerationRange = false;
		}




		#endregion

		#region emission timing gui
		public static void secondFrequencyTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null) return;

			gameData.currentEmitter.SecondFrequency = ((float)System.Convert.ToDouble(((TextBox)callingWindow).text))/1000.0f;



		}

		public static void emissionEventComboBoxItemSelect(Window callingWindow)
		{
			if(gameData.currentEmitter == null)
				return;

			if( ((ComboBox)callingWindow).text == "Call only")
			{
				gameData.currentEmitter.TimedEmission = false;
			}
			else
			{
				gameData.currentEmitter.TimedEmission = true;

			}


		}
		public static void numberPerEmissionTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.NumberPerEmission = System.Convert.ToInt32( ((TextBox)callingWindow).text);
		}
		#endregion

		#region relative variable GUI
		public static void RelXTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.relX = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);
		}


		public static void RelYTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.relY = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);
		}


		public static void RelZTextBoxLoseFocus(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;
			gameData.currentEmitter.relZ = (float)System.Convert.ToDouble( ((TextBox)callingWindow).text);
		}


		public static void ConsiderParentVelocityToggleButtonClick(Window window)
		{
            //if(gameData.currentEmitter == null)	return;
            //gameData.currentEmitter.ConsiderParentVelocity = ((ToggleButton)window).isPressed();
		}
		#endregion

		#region Instructions
		
		public static void RefillInstructionListBox()
		{
			guiData.propWindow.instructionListBox.Clear();

			foreach(FrbInstruction instruction in gameData.currentEmitter.ParticleBlueprint.instructionArray)
			{
				string typeOfInstruction = instruction.GetType().Name;

				if(typeOfInstruction == "FrbInstruction") typeOfInstruction = "No Instruction Type Selected";

				guiData.propWindow.instructionListBox.AddItem( typeOfInstruction, instruction);
			}

			if(guiData.propWindow.instructionListBox.GetHighlighted().Count != 0)
				ListBoxSelectInstruction(guiData.propWindow.instructionListBox);
		}



		public static void AddInstructionButtonClick(Window callingWindow)
		{
			if(gameData.currentEmitter == null)	return;

			gameData.currentEmitter.ParticleBlueprint.instructionArray.Add(new FRB.Instructions.FrbInstruction());

			RefillInstructionListBox();
		}

		
		public static void ListBoxSelectInstruction(Window callingWindow)
		{
			FrbInstruction instruction = ((ListBox)callingWindow).GetHighlightedObject() as FrbInstruction;

			propWindow.typeComboBox.text = ((ListBox)callingWindow).GetHighlighted()[0];

			ShowInstructionGUI();

		}
		
		
		static void CreateValueWindow(int index, string variableName, string windowType)
		{
			#region null - get rid of the windows for the index
			if(windowType == "null")
			{
				if(index == 0)
				{
					propWindow.value1TextDisplay.visible = false;
					if(propWindow.value1Window != null)
					{
						propWindow.RemoveWindow(propWindow.value1Window);
						propWindow.instructionGUI.Remove(propWindow.value1Window);
						propWindow.value1Window = null;
					}
				}
				else if(index == 1)
				{
					propWindow.value2TextDisplay.visible = false;
					if(propWindow.value2Window != null)
					{
						propWindow.RemoveWindow(propWindow.value2Window);
						propWindow.value2Window = null;
						propWindow.instructionGUI.Remove(propWindow.value1Window);
					}
				}
				else if(index == 2)
				{
					propWindow.value3TextDisplay.visible = false;
					if(propWindow.value3Window != null)
					{
						propWindow.RemoveWindow(propWindow.value3Window);
						propWindow.value3Window = null;
						propWindow.instructionGUI.Remove(propWindow.value1Window);
					}
				}

				return;
			}
			#endregion

			if(index == 0)
			{
				guiData.propWindow.value1TextDisplay.visible = true;
				guiData.propWindow.value1TextDisplay.text = variableName;

				if(propWindow.value1Window != null)
				{
					propWindow.RemoveWindow(propWindow.value1Window);
					propWindow.instructionGUI.Remove(propWindow.value1Window);
				}

				if(windowType == "ComboBox")
					propWindow.value1Window = propWindow.AddComboBox();
				else if(windowType == "UpDownWindow")
					propWindow.value1Window = propWindow.AddUpDown();
				propWindow.instructionGUI.Add(propWindow.value1Window);
				propWindow.value1Window.sclX = 5;
				propWindow.value1Window.SetPositionTL( propWindow.value1TextDisplay.x + 15, propWindow.value1TextDisplay.y);			

			}
			else if(index == 1)
			{
				guiData.propWindow.value2TextDisplay.visible = true;
				guiData.propWindow.value2TextDisplay.text = variableName;

				if(propWindow.value2Window != null)
				{
					propWindow.RemoveWindow(propWindow.value2Window);
					propWindow.instructionGUI.Remove(propWindow.value2Window);
				}

				if(windowType == "ComboBox")
					propWindow.value2Window = propWindow.AddComboBox();
				else if(windowType == "UpDownWindow")
					propWindow.value2Window = propWindow.AddUpDown();
				propWindow.instructionGUI.Add(propWindow.value2Window);
				propWindow.value2Window.sclX = 5;
				propWindow.value2Window.SetPositionTL( propWindow.value2TextDisplay.x + 15, propWindow.value2TextDisplay.y);			
			}
			else if(index == 2)
			{
				guiData.propWindow.value3TextDisplay.visible = true;
				guiData.propWindow.value3TextDisplay.text = variableName;

				if(propWindow.value3Window != null)
				{
					propWindow.RemoveWindow(propWindow.value3Window);
					propWindow.instructionGUI.Remove(propWindow.value3Window);
				}


				if(windowType == "ComboBox")
					propWindow.value3Window = propWindow.AddComboBox();
				else if(windowType == "UpDownWindow")
					propWindow.value3Window = propWindow.AddUpDown();
				propWindow.instructionGUI.Add(propWindow.value3Window);
				propWindow.value3Window.sclX = 5;
				propWindow.value3Window.SetPositionTL( propWindow.value3TextDisplay.x + 15, propWindow.value3TextDisplay.y);			

			}


		}


		/// <summary>
		/// Creates the value windows depending on the instruction type.
		/// </summary>
		/// <remarks>
		/// This method needs to be changed when adding new FrbInstruction types
		/// </remarks>
		static void ShowInstructionGUI()
		{
			
			string instructionType = "";
			if(guiData.propWindow.instructionListBox.GetHighlighted().Count != 0)
				instructionType = guiData.propWindow.instructionListBox.GetHighlighted()[0];

			guiData.propWindow.typeTextDisplay.visible = true;
			guiData.propWindow.typeComboBox.visible = true;
			
			#region No Instruction Type Selected
			if(instructionType == "No Instruction Type Selected")
			{
				CreateValueWindow(0, "null", "null");
				CreateValueWindow(1, "null", "null");
				CreateValueWindow(2, "null", "null");
				
			}
				#endregion
			
			#region FadeRate
			else if(instructionType == "FadeRate")
			{
				guiData.propWindow.instructionTimeTextDisplay.visible = true;
				guiData.propWindow.instructionTimeTextBox.visible = true;

				CreateValueWindow(0, "FadeRate", "UpDownWindow");
				((UpDown)propWindow.value1Window).onGUIChange += new FrbGuiMessage(SetValueGUIFadeRate);
				((UpDown)propWindow.value1Window).CurrentValue = 
					(propWindow.instructionListBox.GetHighlightedObject() as FadeRate).fadeRate;
				CreateValueWindow(1, "null", "null");
				CreateValueWindow(2, "null", "null");				
			}
			#endregion

				#region XAcceleration
			else if(instructionType == "XAcceleration")
			{
				guiData.propWindow.instructionTimeTextDisplay.visible = true;
				guiData.propWindow.instructionTimeTextBox.visible = true;

				CreateValueWindow(0, "XAcceleration", "UpDownWindow");
				((UpDown)propWindow.value1Window).onGUIChange += new FrbGuiMessage(SetValueGUIXAcceleration);
				((UpDown)propWindow.value1Window).CurrentValue = 
					(propWindow.instructionListBox.GetHighlightedObject() as XAcceleration).xAcceleration;
				CreateValueWindow(1, "null", "null");
				CreateValueWindow(2, "null", "null");				
			}

				#endregion

				#region YAcceleration
			else if(instructionType == "YAcceleration")
			{
				guiData.propWindow.instructionTimeTextDisplay.visible = true;
				guiData.propWindow.instructionTimeTextBox.visible = true;

				CreateValueWindow(0, "YAcceleration", "UpDownWindow");
				((UpDown)propWindow.value1Window).onGUIChange += new FrbGuiMessage(SetValueGUIYAcceleration);
				((UpDown)propWindow.value1Window).CurrentValue = 
					(propWindow.instructionListBox.GetHighlightedObject() as YAcceleration).yAcceleration;
				CreateValueWindow(1, "null", "null");
				CreateValueWindow(2, "null", "null");				
			}

				#endregion

				#region ZAcceleration
			else if(instructionType == "ZAcceleration")
			{
				guiData.propWindow.instructionTimeTextDisplay.visible = true;
				guiData.propWindow.instructionTimeTextBox.visible = true;

				CreateValueWindow(0, "ZAcceleration", "UpDownWindow");
				((UpDown)propWindow.value1Window).onGUIChange += new FrbGuiMessage(SetValueGUIZAcceleration);
				((UpDown)propWindow.value1Window).CurrentValue = 
					(propWindow.instructionListBox.GetHighlightedObject() as ZAcceleration).zAcceleration;
				CreateValueWindow(1, "null", "null");
				CreateValueWindow(2, "null", "null");				
			}
			#endregion

			if(propWindow.instructionListBox.GetHighlighted().Count != 0)
				propWindow.instructionTimeTextBox.text = (propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction).timeToExecute.ToString();
		}
		
		
		public static void ChangeInstructionType(Window callingWindow)
		{
			string s = ((ComboBox)callingWindow).text;

			if(s == "FadeRate")
			{
				gameData.currentEmitter.ParticleBlueprint.instructionArray.Remove(
					guiData.propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction);

				gameData.currentEmitter.ParticleBlueprint.instructionArray.Add(
					new FRB.Instructions.Sprite_Instructions.FadeRate(null, 0, 0));
			}

			else if(s == "XAcceleration")
			{
				gameData.currentEmitter.ParticleBlueprint.instructionArray.Remove(
					guiData.propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction);

				gameData.currentEmitter.ParticleBlueprint.instructionArray.Add(
					new FRB.Instructions.PositionedObject_Instructions.XAcceleration(null, 0, 0));
			}

			else if(s == "YAcceleration")
			{
				gameData.currentEmitter.ParticleBlueprint.instructionArray.Remove(
					guiData.propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction);

				gameData.currentEmitter.ParticleBlueprint.instructionArray.Add(
					new FRB.Instructions.PositionedObject_Instructions.YAcceleration(null, 0, 0));
			}
			
			else if(s == "ZAcceleration")
			{
				gameData.currentEmitter.ParticleBlueprint.instructionArray.Remove(
					guiData.propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction);

				gameData.currentEmitter.ParticleBlueprint.instructionArray.Add(
					new FRB.Instructions.PositionedObject_Instructions.ZAcceleration(null, 0, 0));
			}

			RefillInstructionListBox();
		}

		

		#region changing instruction values
		
		private static void SetValueGUIFadeRate(Window callingWindow)
		{
			FadeRate fr = ((ListBox)propWindow.instructionListBox).GetHighlightedObject() as FadeRate;
			fr.fadeRate = 
				((UpDown)propWindow.value1Window).CurrentValue;
		}

		
		private static void SetValueGUIXAcceleration(Window callingWindow)
		{
			(((ListBox)propWindow.instructionListBox).GetHighlightedObject() as XAcceleration).xAcceleration =
				((UpDown)propWindow.value1Window).CurrentValue;
		}

		private static void SetValueGUIYAcceleration(Window callingWindow)
		{
			(((ListBox)propWindow.instructionListBox).GetHighlightedObject() as YAcceleration).yAcceleration =
				((UpDown)propWindow.value1Window).CurrentValue;
		}
		
		private static void SetValueGUIZAcceleration(Window callingWindow)
		{
			(((ListBox)propWindow.instructionListBox).GetHighlightedObject() as ZAcceleration).zAcceleration =
				((UpDown)propWindow.value1Window).CurrentValue;
		}


		public static void SetTimeToExecute(Window callingWindow)
		{
			(propWindow.instructionListBox.GetHighlightedObject() as FrbInstruction).timeToExecute = long.Parse(((TextBox)callingWindow).text);
		}
		
		#endregion


		#endregion
		
		#endregion
		

		#region methods

		#region updating gui VISIBILITY

		public static void propertiesEditingClick(Window callingWindow)
		{
			string highlightedItem = "";
			if(((ListBox)callingWindow).GetHighlighted().Count != 0)
				highlightedItem = ((ListBox)callingWindow).GetHighlighted()[0];
			
			updateSpreadGUI(null);
			updateCircularVelocityRangeGUI(null);
			updateTimingGUI(null);
			updateLastingTimeGUI(null);
			updateInstructionGUI(null);
			updateEmissionAreaGUIVisibility(null);

			#region Texture


			if(highlightedItem == "Texture")
			{
				updateTextureOrAnimationButton(null);

				guiData.propWindow.xRangeGUI.visible = guiData.propWindow.yRangeGUI.visible = guiData.propWindow.zRangeGUI.visible = false;

				guiData.propWindow.xAccelerationRangeGUI.visible = false;
				guiData.propWindow.yAccelerationRangeGUI.visible = false;
				guiData.propWindow.zAccelerationRangeGUI.visible = false;

				guiData.propWindow.lastingTimeGUI.visible = false;
			}
				#endregion
				#region Velocity
			else if(highlightedItem == "Velocity")
			{
				guiData.propWindow.xAccelerationRangeGUI.visible = false;
				guiData.propWindow.yAccelerationRangeGUI.visible = false;
				guiData.propWindow.zAccelerationRangeGUI.visible = false;
				guiData.propWindow.lastingTimeGUI.visible = false;
			}
				#endregion
				#region Acceleratoin
			else if(highlightedItem == "Acceleration")
			{
				updateAccelerationRangeGUI(null);

				guiData.propWindow.xRangeGUI.visible = guiData.propWindow.yRangeGUI.visible = guiData.propWindow.zRangeGUI.visible = false;
				guiData.propWindow.lastingTimeGUI.visible = false;
			}
				#endregion
				#region Emission Timing
			else if(highlightedItem == "Emission Timing")
			{
				guiData.propWindow.xRangeGUI.visible = guiData.propWindow.yRangeGUI.visible = guiData.propWindow.zRangeGUI.visible = false;
				guiData.propWindow.xAccelerationRangeGUI.visible = false;
				guiData.propWindow.yAccelerationRangeGUI.visible = false;
				guiData.propWindow.zAccelerationRangeGUI.visible = false;
				guiData.propWindow.lastingTimeGUI.visible = false;
			}
				#endregion
				#region Particle Prop.
			else if(highlightedItem == "Particle Prop.")
			{
				guiData.propWindow.xRangeGUI.visible = guiData.propWindow.yRangeGUI.visible = guiData.propWindow.zRangeGUI.visible = false;
				guiData.propWindow.xAccelerationRangeGUI.visible = false;
				guiData.propWindow.yAccelerationRangeGUI.visible = false;
				guiData.propWindow.zAccelerationRangeGUI.visible = false;

				updateLastingTimeGUI(null);

			}

			updateRotZRange(null);
			updateRotZVelocityRange(null);
			#endregion
		
		
		
		}


		public static void updateSpreadGUI(Window callingWindow)
		{

			string text = guiData.propWindow.spreadStyleComboBox.text;

			if(guiData.propWindow.spreadStyleComboBox.visible == false)
				text = "";

			if(text == "square")
			{
				guiData.propWindow.squareSpreadGUI.visible = true;
				guiData.propWindow.circularSpreadGUI.visible = false;
				guiData.propWindow.wedgeGUI.visible = false;
				updateVelocityRangeGUI(null);

				guiData.propWindow.outwardVelocityRangeGUI.visible = false;

			}
			else if(text == "circle")
			{
				guiData.propWindow.squareSpreadGUI.visible = false;
				guiData.propWindow.circularSpreadGUI.visible = true;
				updateWedgeOrFullGUI(guiData.propWindow.wedgeOrFull);

				guiData.propWindow.xRangeGUI.visible = false;
				guiData.propWindow.yRangeGUI.visible = false;
				guiData.propWindow.zRangeGUI.visible = false;

				updateCircularVelocityRangeGUI(null);

			}
			else
			{
				guiData.propWindow.squareSpreadGUI.visible = false;
				guiData.propWindow.circularSpreadGUI.visible = false;
				guiData.propWindow.wedgeGUI.visible = false;
			}
		}

		
		public static void updateVelocityRangeGUI(Window callingWindow)
		{
			if(guiData.propWindow.xVelocityType.text == "Range")
				guiData.propWindow.xRangeGUI.visible = true;
			else
				guiData.propWindow.xRangeGUI.visible = false;

			if(guiData.propWindow.yVelocityType.text == "Range")
				guiData.propWindow.yRangeGUI.visible = true;
			else
				guiData.propWindow.yRangeGUI.visible = false;

			if(guiData.propWindow.zVelocityType.text == "Range")
				guiData.propWindow.zRangeGUI.visible = true;
			else
				guiData.propWindow.zRangeGUI.visible = false;
		}

		
		public static void updateCircularVelocityRangeGUI(Window callingWindow)
		{
			if(guiData.propWindow.outwardVelocityRangeOrFixed.text == "Range" && guiData.propWindow.outwardVelocityRangeOrFixed.visible)
				guiData.propWindow.outwardVelocityRangeGUI.visible = true;
			else
				guiData.propWindow.outwardVelocityRangeGUI.visible = false;
		}
		

		public static void updateAccelerationRangeGUI(Window callingWindow)
		{
			if(guiData.propWindow.xAccelerationType.text == "Range")
				guiData.propWindow.xAccelerationRangeGUI.visible = true;
			else
				guiData.propWindow.xAccelerationRangeGUI.visible = false;

			if(guiData.propWindow.yAccelerationType.text == "Range")
				guiData.propWindow.yAccelerationRangeGUI.visible = true;
			else
				guiData.propWindow.yAccelerationRangeGUI.visible = false;

			if(guiData.propWindow.zAccelerationType.text == "Range")
				guiData.propWindow.zAccelerationRangeGUI.visible = true;
			else
				guiData.propWindow.zAccelerationRangeGUI.visible = false;
		}

		
		public static void updateWedgeOrFullGUI(Window callingWindow)
		{
			if(guiData.propWindow.wedgeOrFull.text == "wedge")
			{
				guiData.propWindow.wedgeGUI.visible = true;
			}
			else
			{
				guiData.propWindow.wedgeGUI.visible = false;
			}

		}
		

		public static void updateTimingGUI(Window callingWindow)
		{
			if(guiData.propWindow.emissionEventComboBox.visible &&
				guiData.propWindow.emissionEventComboBox.text == "Timed")
			{
				guiData.propWindow.timingGUI.visible = true;
			}
			else
			{
				guiData.propWindow.timingGUI.visible = false;
			}


		}
		

		public static void updateLastingTimeGUI(Window callingWindow)
		{
			if(guiData.propWindow.removalEventComboBox.text == "Timed" && guiData.propWindow.removalEventComboBox.visible == true)
				guiData.propWindow.lastingTimeGUI.visible = true;
			else
				guiData.propWindow.lastingTimeGUI.visible = false;
		}

		
		public static void updateRotZRange(Window callingWindow)
		{
			if(guiData.propWindow.propertiesEditingListBox.GetHighlighted().Count != 0 && 
				guiData.propWindow.propertiesEditingListBox.GetHighlighted()[0] != "Particle Prop." || 
				guiData.propWindow.rotZFixedOrRange.text == "Fixed")
				guiData.propWindow.rotZRangeGUI.visible = false;
			else if(guiData.propWindow.propertiesEditingListBox.GetHighlighted().Count == 0)
				guiData.propWindow.rotZRangeGUI.visible = false;
			else
				guiData.propWindow.rotZRangeGUI.visible = true;

		}
	
	
		public static void updateRotZVelocityRange(Window callingWindow)
		{
			if(guiData.propWindow.propertiesEditingListBox.GetHighlighted().Count != 0 && 
				guiData.propWindow.propertiesEditingListBox.GetHighlighted()[0] != "Particle Prop." ||
				guiData.propWindow.rotZVelocityFixedOrRange.text == "Fixed")
				guiData.propWindow.rotZVelocityRangeGUI.visible = false;
			else if(guiData.propWindow.propertiesEditingListBox.GetHighlighted().Count == 0)
				guiData.propWindow.rotZVelocityRangeGUI.visible = false;
			else
				guiData.propWindow.rotZVelocityRangeGUI.visible = true;
		}

		
		public static void updateInstructionGUI(Window callingWindow)
		{
			if(guiData.propWindow.instructionListBox.visible == true &&
				guiData.propWindow.instructionListBox.GetHighlightedObject() != null)
			{
				guiData.propWindow.typeComboBox.visible = true;
				guiData.propWindow.typeTextDisplay.visible = true;

				guiData.propWindow.instructionTimeTextBox.visible = true;
				guiData.propWindow.instructionTimeTextDisplay.visible = true;

				if(guiData.propWindow.instructionTimeTextBox.text != null &&
					(System.Convert.ToDouble(guiData.propWindow.instructionTimeTextBox.text) >= 0))
				{
					guiData.propWindow.cycleTimeTextBox.visible = true;
					guiData.propWindow.cycleTimeTextDisplay.visible = true;
				}
			}
			else
			{
				guiData.propWindow.typeComboBox.visible = false;
				guiData.propWindow.typeTextDisplay.visible = false;


				guiData.propWindow.instructionTimeTextBox.visible = false;
				guiData.propWindow.instructionTimeTextDisplay.visible = false;

				guiData.propWindow.cycleTimeTextBox.visible = false;
				guiData.propWindow.cycleTimeTextDisplay.visible = false;
				

			}
		}
		

		public static void updateEmissionAreaGUIVisibility(Window callingWindow)
		{
			if(propWindow.emissionAreaType.visible == false || propWindow.emissionAreaType.text == "Point")
			{
				propWindow.emissionAreaSclXTextDisplay.visible = false;
				propWindow.emissionAreaSclYTextDisplay.visible = false;
				propWindow.emissionAreaSclZTextDisplay.visible = false;

				propWindow.emissionAreaSclX.visible = false;
				propWindow.emissionAreaSclY.visible = false;
				propWindow.emissionAreaSclZ.visible = false;
			}
			else if(propWindow.emissionAreaType.text == "Rectangle")
			{
				propWindow.emissionAreaSclXTextDisplay.visible = true;
				propWindow.emissionAreaSclYTextDisplay.visible = true;
				propWindow.emissionAreaSclZTextDisplay.visible = false;

				propWindow.emissionAreaSclX.visible = true;
				propWindow.emissionAreaSclY.visible = true;
				propWindow.emissionAreaSclZ.visible = false;
			}
			else if(propWindow.emissionAreaType.text == "Circle")
			{

			}
			else if(propWindow.emissionAreaType.text == "Sphere")
			{

			}
			else if(propWindow.emissionAreaType.text == "Cube")
			{
				propWindow.emissionAreaSclXTextDisplay.visible = true;
				propWindow.emissionAreaSclYTextDisplay.visible = true;
				propWindow.emissionAreaSclZTextDisplay.visible = true;

				propWindow.emissionAreaSclX.visible = true;
				propWindow.emissionAreaSclY.visible = true;
				propWindow.emissionAreaSclZ.visible = true;

			}
			else
			{
				propWindow.emissionAreaSclXTextDisplay.visible = false;
				propWindow.emissionAreaSclYTextDisplay.visible = false;

				propWindow.emissionAreaSclX.visible = false;
				propWindow.emissionAreaSclY.visible = false;

			}

		}

		#endregion



		public static void UpdateEmissionAreaGuiValues()
		{
			propWindow.emissionAreaSclX.CurrentValue = gameData.currentEmitter.SclX;
			propWindow.emissionAreaSclY.CurrentValue = gameData.currentEmitter.SclY;

			if(gameData.currentEmitter.AreaEmissionType != null)
                propWindow.emissionAreaType.text = gameData.currentEmitter.AreaEmissionType;
			else
				propWindow.emissionAreaType.text = "Point";



			updateEmissionAreaGUIVisibility(null);
	
		}

		public static void UpdateRelativeGuiValues()
		{

			if(gameData.currentEmitter.dependentOn != null)
				guiData.propWindow.attachmentInformationTextDisplay.text = "Attached To:  " + gameData.currentEmitter.dependentOn.name;
			else
				guiData.propWindow.attachmentInformationTextDisplay.text = "Attached To:  null";
			
			guiData.propWindow.relXTextBox.text = gameData.currentEmitter.relX.ToString();
			guiData.propWindow.relYTextBox.text = gameData.currentEmitter.relY.ToString();
			guiData.propWindow.relZTextBox.text = gameData.currentEmitter.relZ.ToString();


		}

		#endregion


	}
}
