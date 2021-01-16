using FlatRedBall.Graphics;
using FlatRedBall.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Forms.Controls.Popups
{
    class ToastInfo
    {
        public string Message { get; set; }
        public Layer FrbLayer { get; set; }
    }

    public static class ToastManager 
    {
        static BlockingCollection<ToastInfo> toastMessages = new BlockingCollection<ToastInfo>();

        /// <summary>
        /// The default layer for showing toast. If this is set at the Screen level, it should
        /// be set back to null when the Screen is destroyed.
        /// </summary>
        public static Layer DefaultToastLayer { get; set; }

        static bool hasBeenStarted;
        static void Start()
        {
            if(!hasBeenStarted)
            {
                hasBeenStarted = true;
                var thread = new Thread(new ThreadStart(DoLoop));
                thread.Start();
            }
        }

        public static void Show(string message, Layer frbLayer = null)
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

            var toastInfo = new ToastInfo { Message = message, FrbLayer = frbLayer };

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
                await Task.Delay(2000);
                toast.Close();
                // so there's a small gap between toasts
                await Task.Delay(100);
            }
        }
    }
}
