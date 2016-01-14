using System;
using System.Collections.Generic;
using System.Text;

#if FRB_XNA
using Microsoft.Xna.Framework.Input;
#else
using Keys = Microsoft.DirectX.DirectInput.Key;
#endif

namespace FlatRedBall.Input.Recording
{
    public class KeyboardRecord : InputRecordBase<Keys, Keyboard.KeyAction>
    {
        #region Fields




        bool mRecordingKeyPushed = true;
        bool mRecordingKeyTyped = true;
        bool mRecordingKeyReleased = true;

        #endregion

        #region Properties


        public bool RecordingKeyPushed
        {
            get { return mRecordingKeyPushed; }
            set { mRecordingKeyPushed = value; }
        }

        public bool RecordingKeyTyped
        {
            get { return mRecordingKeyTyped; }
            set { mRecordingKeyTyped = value; }
        }

        public bool RecordingKeyReleased
        {
            get { return mRecordingKeyReleased; }
            set { mRecordingKeyReleased = value; }
        }

        #endregion

        #region Methods

        public bool KeyPushed(Keys key, double timeBlockStartInclusive, double timeBlockEndExclusive)
        {
            if(IsPlayingBack == false)
            {
                throw new System.InvalidOperationException(
                    "Can't check for key pushed if IsPlayingBack is false.");
            }

            foreach (InputEvent<Keys, Keyboard.KeyAction> inputEvent in InputEvents)
            {
                // The events should be time-ordered, so if we're beyond the end of the time block,
                // just kick out to avoid the extra processing.
                if (inputEvent.Time + TimePlaybackStarted > timeBlockEndExclusive)
                {
                    return false;
                }
                else if (inputEvent.Value == Keyboard.KeyAction.KeyPushed && inputEvent.Type == key &&
                    inputEvent.Time + TimePlaybackStarted >= timeBlockStartInclusive)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Update()
        {
            Update(0);
        }

        public void Update(double timeOffset)
        {
            if (IsRecording)
            {
                EventsRecordedThisFrame.Clear();

                for (int i = 0; i < Keyboard.NumberOfKeys; i++)
                {
                    if ( mRecordingKeyPushed && InputManager.Keyboard.KeyPushed((Keys)i))
                    {
                        InputEvent<Keys, Keyboard.KeyAction> newEvent =
                            new InputEvent<Keys, Keyboard.KeyAction>(
                                timeOffset + TimeManager.CurrentTime - TimeRecordingStarted,
                                (Keys)i,
                                Keyboard.KeyAction.KeyPushed);

                        InputEvents.Add(newEvent );
                        EventsRecordedThisFrame.Add(newEvent);

                        InputEvents.Sort();

                    }
                    else if ( mRecordingKeyTyped && InputManager.Keyboard.KeyTyped((Keys)i)) // Typed is true when pushed, so don't want to double the info
                    {
                        InputEvent<Keys, Keyboard.KeyAction> newEvent =
                            new InputEvent<Keys, Keyboard.KeyAction>(
                                timeOffset + TimeManager.CurrentTime - TimeRecordingStarted,
                                (Keys)i,
                                Keyboard.KeyAction.KeyTyped);

                        InputEvents.Add(newEvent );
                        EventsRecordedThisFrame.Add(newEvent);

                        InputEvents.Sort();
                    }
                    if ( mRecordingKeyReleased && InputManager.Keyboard.KeyReleased((Keys)i))
                    {
                        InputEvent<Keys, Keyboard.KeyAction> newEvent =
                            new InputEvent<Keys, Keyboard.KeyAction>(
                                timeOffset + TimeManager.CurrentTime - TimeRecordingStarted,
                                (Keys)i,
                                Keyboard.KeyAction.KeyReleased);

                        InputEvents.Add(newEvent);
                        EventsRecordedThisFrame.Add(newEvent);
                        InputEvents.Sort();
                    }       
                }
            }
        }

        #endregion

    }  
}
