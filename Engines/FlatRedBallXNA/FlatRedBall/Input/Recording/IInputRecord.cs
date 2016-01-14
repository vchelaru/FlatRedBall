using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Utilities;

namespace FlatRedBall.Input.Recording
{
    #region XML Docs
    /// <summary>
    /// Base interface for class which can record input.  
    /// </summary>
    #endregion
    public interface IInputRecord
    {
        bool IsRecording { get; set;}

        bool IsPlayingBack { get; set;}

        void Update();
    }
}
