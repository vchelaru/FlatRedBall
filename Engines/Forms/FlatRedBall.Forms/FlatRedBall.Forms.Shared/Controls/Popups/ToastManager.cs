using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Managers;
using FlatRedBall.Screens;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    /// Internally this creates a Toast object using the FlatRedBall.Forms Toast control.
    /// </summary>
    public static class ToastManager 
    {
        static BlockingCollection<ToastInfo> toastMessages = new BlockingCollection<ToastInfo>();

        /// <summary>
        /// The default layer for showing toast. If this is set at the Screen level, it should
        /// be set back to null when the Screen is destroyed.
        /// </summary>
        public static Layer DefaultToastLayer { get; set; }
        static IList liveToasts;
        static bool hasBeenStarted;

        static void Start()
        {
            if(!hasBeenStarted)
            {
                // from:
                // https://stackoverflow.com/questions/5874317/thread-safe-listt-property
                // This seems to be the only one that supports add, remove, and foreach
                liveToasts = ArrayList.Synchronized(new ArrayList());
                hasBeenStarted = true;

                // Make sure we destroy toasts when the screen navigates
                ScreenManager.AfterScreenDestroyed += (unused) => DestroyLiveToasts();

                var thread = new System.Threading.Thread(new ThreadStart(DoLoop));
                thread.Start();
            }
        }

        /// <summary>
        /// Queues a toast to be shown for the given duration. This method can be called from any thread.
        /// If a toast is currently shown, then this message is queued and will be shown on the next toast.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="frbLayer">The layer for the Toast instance.</param>
        /// <param name="durationInSeconds">The number of seconds to display the toast.</param>
        public static void Show(string message, Layer frbLayer = null, double durationInSeconds = 2.0)
        {
            if(!hasBeenStarted)
            {
                if(FlatRedBallServices.IsThreadPrimary())
                {
                    Start();
                }
                else
                {
                    Instructions.InstructionManager.AddSafe(Start);
                }
            }

            var toastInfo = new ToastInfo { Message = message, FrbLayer = frbLayer, DurationInSeconds = durationInSeconds};

            toastMessages.Add(toastInfo);
        }

        /// <summary>
        /// Removes all live toasts from all managers. This is called automatically when a Screen is destroyed.
        /// </summary>
        public static void DestroyLiveToasts()
        {
            int numberOfToastsToClean = liveToasts?.Count ?? 0;
            if(liveToasts != null)
            {
                foreach(Toast item in liveToasts)
                {
                    item?.Close();
                }

            }
            liveToasts?.Clear();

#if DEBUG
            if(GuiManager.Windows.Any(item => item is Toast))
            {
                throw new Exception("Toasts did not clean up and they should have. Why not?");
            }
#endif
        }

        private static async void DoLoop()
        {
            const int msDelayBetweenToasts = 100;

            foreach (var message in toastMessages.GetConsumingEnumerable(CancellationToken.None))
            {
                Toast toast = null;

                // November 23, 2023
                // Note: Calling await
                // in this method results
                // in the SynchronizationContext
                // resuming the method on the main 
                // thread. Not sure if this is desirable
                // or not. For now I'll leave it as-is since
                // all seems to work, but if we want to keep this
                // on a separate thread, then we need to do this:
                //  'Task. Delay().configureawait(false)`

                // This must be done on the primary thread in case it loads
                // the PNG for the first time:
                await Instructions.InstructionManager.DoOnMainThreadAsync(() =>
                {
                    try
                    {
                        toast = new FlatRedBall.Forms.Controls.Popups.Toast();
                    }
                    // If the user doesn't have any toast implemented in Gum, this will error. 
                    // This causes problems in edit mode, so let's just consume it:
                    catch
                    {

                    }
                });

                if(toast != null)
                {
                    toast.Text = message.Message;
                    liveToasts.Add(toast);
                    toast.Show(message.FrbLayer ?? DefaultToastLayer);
                    await Task.Delay( TimeSpan.FromSeconds(message.DurationInSeconds) );
                    toast.Close();
                    // Moving this to be in the instruction:
                    //liveToasts.Remove(toast);

                    await Instructions.InstructionManager.DoOnMainThreadAsync(() =>
                    {
                        // liveToasts.Remove used to be called outside
                        // of the DoOnMainThreadAsync method. The reason 
                        // we do it in here is because it is possible for
                        // the Screen to end inbetween the instruction getting
                        // created and the instruction getting removed. If that
                        // happens and if we had liveToasts.Remove sitting outside
                        // of this call, then the toast would get removed from the list
                        // but still be part of managers. This can result in a crash because
                        // the engine believes the screen has exited while there is still a live
                        // toast. We'll keep the toast as part of the liveToasts until it is removed 
                        // from managers so that it can get cleaned up in DestroyLiveToasts.
                        // Just in case this gets cleaned up elsewhere:
                        if(liveToasts.Contains(toast))
                        {
                            liveToasts.Remove(toast);
                            toast.Visual.RemoveFromManagers();
                        }
                    });
                    // so there's a small gap between toasts
                    await Task.Delay(msDelayBetweenToasts);
                }
            }
        }
    }
}
