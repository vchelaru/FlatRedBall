using System;
using Android.Views;
using Android.Views.InputMethods;
using Android.App;
using Android.Content;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	struct AndroidKeyboardAction
	{
		public Microsoft.Xna.Framework.Input.Keys Key;
		public KeyEventActions Action;
		public Keycode AndroidKeycode;
	}

	public partial class Keyboard
	{
		bool hasAddedEvent = false;

		List<AndroidKeyboardAction> androidActionsToProcess = new List<AndroidKeyboardAction>();
        string stringToProcess;
        string processedString;

		List<AndroidKeyboardAction> lastFrameActions = new List<AndroidKeyboardAction>();
		List<AndroidKeyboardAction> downKeys = new List<AndroidKeyboardAction>();

		static object androidActionListLock = new object();

		public void ShowKeyboard() 
		{
            var view = FlatRedBallServices.Game.Services.GetService<View>();
            var context = view.Context;

            view.RequestFocus();
            InputMethodManager inputMethodManager = context.GetSystemService(Context.InputMethodService) as InputMethodManager;
            inputMethodManager.ShowSoftInput(view, ShowFlags.Forced);
            inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);

            if (!hasAddedEvent)
            {
                view.KeyPress += HandleAndroidKeyPress;
                hasAddedEvent = true;
            }
		}

		public void HideKeyboard()
		{
            var view = FlatRedBallServices.Game.Services.GetService<View>();
            var context = view.Context;
            InputMethodManager inputMethodManager = context.GetSystemService(Context.InputMethodService) as InputMethodManager;
            inputMethodManager.HideSoftInputFromWindow(
                view.WindowToken, HideSoftInputFlags.None);
        }

		void HandleAndroidKeyPress (object sender, View.KeyEventArgs e) 
		{
            if (e.Event.Action == KeyEventActions.Multiple)
            {
                lock (androidActionListLock)
                {
                    stringToProcess += e.Event.Characters;
                }
            }
			if (e.Event.Action == KeyEventActions.Down ||
			   e.Event.Action == KeyEventActions.Up)
			{
				var newAction = new AndroidKeyboardAction ();

				newAction.Action = e.Event.Action;
                var stringTyped = e.Event.Characters;

				newAction.AndroidKeycode = e.KeyCode;

				newAction.Key = AndroidKeycodeToXnaKeys (e.KeyCode);
			
				lock (androidActionListLock)
				{
                    var unicodeChar = e.Event.UnicodeChar;

                    if(unicodeChar > 0 && e.Event.Action == KeyEventActions.Down)
                    {
                        stringToProcess += (char)unicodeChar;
                    }

					androidActionsToProcess.Add (newAction);
				}
			}
		}

		Microsoft.Xna.Framework.Input.Keys AndroidKeycodeToXnaKeys(Keycode keycode)
		{
			switch (keycode)
			{
			case Keycode.Num0:
				return Keys.D0;
			case Keycode.Num1:
				return Keys.D1;
			case Keycode.Num2:
				return Keys.D2;
			case Keycode.Num3:
				return Keys.D3;
			case Keycode.Num4:
				return Keys.D4;
			case Keycode.Num5:
				return Keys.D5;
			case Keycode.Num6:
				return Keys.D6;
			case Keycode.Num7:
				return Keys.D7;
			case Keycode.Num8:
				return Keys.D8;
			case Keycode.Num9:
				return Keys.D9;
			case Keycode.A:
				return Keys.A;
			case Keycode.B:
				return Keys.B;
			case Keycode.C:
				return Keys.C;
			case Keycode.D:
				return Keys.D;
			case Keycode.E:
				return Keys.E;
			case Keycode.F:
				return Keys.F;
			case Keycode.G:
				return Keys.G;
			case Keycode.H:
				return Keys.H;
			case Keycode.I:
				return Keys.I;
			case Keycode.J:
				return Keys.J;
			case Keycode.K:
				return Keys.K;
			case Keycode.L:
				return Keys.L;
			case Keycode.M:
				return Keys.M;
			case Keycode.N:
				return Keys.N;
			case Keycode.O:
				return Keys.O;
			case Keycode.P:
				return Keys.P;
			case Keycode.Q:
				return Keys.Q;
			case Keycode.R:
				return Keys.R;
			case Keycode.S:
				return Keys.S;
			case Keycode.T:
				return Keys.T;
			case Keycode.U:
				return Keys.U;
			case Keycode.V:
				return Keys.V;
			case Keycode.W:
				return Keys.W;
			case Keycode.X:
				return Keys.X;
			case Keycode.Y:
				return Keys.Y;
			case Keycode.Z:
				return Keys.Z;

			case Keycode.AltLeft:
				return Keys.LeftAlt;
			case Keycode.AltRight:
				return Keys.RightAlt;

			case Keycode.Back:
				return Keys.Back;
			case Keycode.Backslash:
				return Keys.OemBackslash;
			case Keycode.ButtonSelect:
				return Keys.Select;

			case Keycode.Clear:
				return Keys.OemClear;
			case Keycode.Comma:
				return Keys.OemComma;

			case Keycode.Del:
                //return Keys.Delete;
                // acts as a backspace
                return Keys.Back;
            case Keycode.Enter:
				return Keys.Enter;
			case Keycode.Home:
				return Keys.Home;
			case Keycode.LeftBracket:
				return Keys.OemOpenBrackets;
			case Keycode.MediaNext:
				return Keys.MediaNextTrack;
			case Keycode.MediaPlayPause:
				return Keys.MediaPlayPause;
			case Keycode.MediaPrevious:
				return Keys.MediaPreviousTrack;
			case Keycode.Minus:
				return Keys.OemMinus;
			case Keycode.Mute:
				return Keys.VolumeMute;
			case Keycode.PageDown:
				return Keys.PageDown;
			case Keycode.PageUp:
				return Keys.PageUp;
			case Keycode.Period:
				return Keys.OemPeriod;
			case Keycode.Plus:
				return Keys.OemPlus;
			case Keycode.RightBracket:
				return Keys.OemCloseBrackets;
			case Keycode.Search:
				return Keys.BrowserSearch;
			case Keycode.Semicolon:
				return Keys.OemSemicolon;
			case Keycode.ShiftLeft:
				return Keys.LeftShift;
			case Keycode.ShiftRight:
				return Keys.RightShift;
			case Keycode.Space:
				return Keys.Space;
			case Keycode.Star:
				return Keys.Multiply;
			case Keycode.Tab:
				return Keys.Tab;
			case Keycode.VolumeUp:
				return Keys.VolumeUp;
			case Keycode.VolumeDown:
				return Keys.VolumeDown;

			}

			return Keys.None;
		}

		void ProcessAndroidKeys()
		{
			lock (androidActionsToProcess)
			{
				for (int i = 0; i < androidActionsToProcess.Count; i++)
				{
					var itemAtI = androidActionsToProcess [i];

					if (itemAtI.Action == KeyEventActions.Down)
					{
						downKeys.Add (itemAtI);
					}
					else if (itemAtI.Action == KeyEventActions.Up)
					{
                        // remove the key:
                        for(int j = downKeys.Count-1; j > -1; j--)
                        {
                            if(downKeys[j].AndroidKeycode == itemAtI.AndroidKeycode)
                            {
                                downKeys.RemoveAt(j);
                            }
                        }
					}
				}

                processedString = stringToProcess;
                stringToProcess = null;


				lastFrameActions.Clear ();
				lastFrameActions.AddRange (androidActionsToProcess);

                androidActionsToProcess.Clear();
			}
		}

		bool KeyDownAndroid(Keys key)
		{
			lock (androidActionsToProcess)
			{

				int count = downKeys.Count;

				for (int i = 0; i < count; i++)
				{
					if (downKeys [i].Key == key)
					{
						return true;
					}
				}

				return false;

			}
		}
		bool KeyPushedAndroid(Keys key)
		{
			lock (androidActionsToProcess)
			{
				int count = lastFrameActions.Count;

				for (int i = 0; i < count; i++)
				{
					var itemAtI = lastFrameActions [i];
					if (itemAtI.Key == key && itemAtI.Action == KeyEventActions.Down)
					{
						return true;
					}
				}
				return false;
			}
		}

        bool KeyReleasedAndroid(Keys key)
        {
            lock (androidActionsToProcess)
            {
                int count = lastFrameActions.Count;

                for (int i = 0; i < count; i++)
                {
                    var itemAtI = lastFrameActions[i];
                    if (itemAtI.Key == key && itemAtI.Action == KeyEventActions.Up)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

    }
}