using FlatRedBall.Graphics;
using FlatRedBall.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Forms.Controls.Popups
{
    #region Classes

    class ToastInfo
    {
        public string Message { get; set; }
        public Layer FrbLayer { get; set; }
        public double DurationInSeconds { get; set; }
    }

    #endregion

    /// <summary>
    /// Object responsible for manging the lifecycle of toasts. This can be used to perform fire-and-forget showing of Toast objects.
    /// </summary>
    public static class ToastManager 
    {
        static BlockingCollection<ToastInfo> toastMessages = new BlockingCollection<ToastInfo>();

        /// <summary>
        /// The default layer for showing toast. If this is set at the Screen level, it should
        /// be set back to null when the Screen is destroyed.
        /// </summary>
        public static Layer DefaultToastLayer { get; set; }

        static bool hasBeenStarted;
#if !UWP
        // threading works differently in UWP. do we care? Is UWP going to live?

        static void Start()
        {
            if(!hasBeenStarted)
            {
                hasBeenStarted = true;
                var thread = new System.Threading.Thread(new ThreadStart(DoLoop));
                thread.Start();
            }
        }
#endif

        public static void Show(string message, Layer frbLayer = null, double durationInSeconds = 2.0)
        {
            if(!hasBeenStarted)
            {
#if !UWP
                if(FlatRedBallServices.IsThreadPrimary())
                {
                    Start();
                }
                else
                {
                    Instructions.InstructionManager.AddSafe(Start);
                }
#endif
            }

            var toastInfo = new ToastInfo { Message = message, FrbLayer = frbLayer, DurationInSeconds = durationInSeconds};

            toastMessages.Add(toastInfo);
        }

        private static async void DoLoop()
        {
            Toast toast = null;

            foreach (var message in toastMessages.GetConsumingEnumerable(CancellationToken.None))
            {
                if(toast == null)
                {
                    toast = new FlatRedBall.Forms.Controls.Popups.Toast();
                }

                toast.Text = message.Message;
                toast.Show(message.FrbLayer ?? DefaultToastLayer);
                await Task.Delay( TimeSpan.FromSeconds(message.DurationInSeconds) );
                toast.Close();
                // so there's a small gap between toasts
                await Task.Delay(100);
            }
        }
    }
}
