using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.IO;

#if FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
#else
using Keys = Microsoft.DirectX.DirectInput.Key;
#endif
using Keyboard = FlatRedBall.Input.Keyboard;
using KeyboardRecord = FlatRedBall.Input.Recording.KeyboardRecord;

namespace FlatRedBall.Content.Input
{
    public class KeyboardRecordSave
    {
        #region Fields

        public List<FlatRedBall.Input.Recording.InputEvent<Keys, Keyboard.KeyAction>> InputEvents = 
            new List<FlatRedBall.Input.Recording.InputEvent<Keys, Keyboard.KeyAction>>();

        #endregion

        #region Methods

        public static KeyboardRecordSave FromKeyboardRecord(KeyboardRecord keyboardRecord)
        {
            KeyboardRecordSave krs = new KeyboardRecordSave();

            foreach(FlatRedBall.Input.Recording.InputEvent<Keys, Keyboard.KeyAction> inputEvent in keyboardRecord.InputEvents)
            {
                krs.InputEvents.Add(inputEvent);
            }

            return krs;
        }

        public static KeyboardRecordSave FromFile(string fileName)
        {
            KeyboardRecordSave krs = FileManager.XmlDeserialize<KeyboardRecordSave>(fileName);
            return krs;
        }        
        
        public void Save(string fileName)
        {
            string serializedString;

            

            FileManager.XmlSerialize(this, out serializedString);

            FileManager.SaveText(serializedString, fileName);


        }

        public KeyboardRecord ToKeyboardRecord()
        {
            KeyboardRecord keyboardRecord = new KeyboardRecord();

            foreach (FlatRedBall.Input.Recording.InputEvent<Keys, Keyboard.KeyAction> inputEvent in InputEvents)
            {
                keyboardRecord.InputEvents.Add(inputEvent);
            }

            return keyboardRecord;
        }

        #endregion

    }
}
