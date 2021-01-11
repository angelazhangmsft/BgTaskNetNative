using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.System.Threading;
using Windows.UI.Notifications;

namespace OldWRC_CSharp
{
    public sealed class BgTask
    {
        BackgroundTaskCancellationReason _cancelReason = BackgroundTaskCancellationReason.Abort;
        volatile bool _cancelRequested = false;
        BackgroundTaskDeferral _deferral = null;
        ThreadPoolTimer _periodicTimer = null;
        uint _progress = 0;
        IBackgroundTaskInstance _taskInstance = null;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background " + taskInstance.Task.Name + " Starting...");

            // Query BackgroundWorkCost
            var cost = BackgroundWorkCost.CurrentBackgroundWorkCost;
            if (cost == BackgroundWorkCostValue.High) return;

            // Associate a cancellation handler with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);


            _taskInstance = taskInstance;
            SendToast();

            _periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(PeriodicTimerCallback), TimeSpan.FromSeconds(1));

        }

        private void SendToast()
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
            XmlNodeList textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode("A toast example"));
            textElements[1].AppendChild(toastXml.CreateTextNode("Here's a message from your background task"));
            ToastNotification notification = new ToastNotification(toastXml);
            notification.ExpirationTime = DateTime.Now.AddMinutes(5);
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        //
        // Handles background task cancellation.
        //
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
            _cancelReason = reason;

            Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested...");
        }

        //
        // Simulate the background task activity.
        //
        private void PeriodicTimerCallback(ThreadPoolTimer timer)
        {
            if ((_cancelRequested == false) && (_progress < 100))
            {
                _progress += 10;
                _taskInstance.Progress = _progress;
            }
            else
            {
                _periodicTimer.Cancel();

                //
                // Record that this background task ran.
                //
                String taskStatus = (_progress < 100) ? "Canceled with reason: " + _cancelReason.ToString() : "Completed";
                Debug.WriteLine("Background " + _taskInstance.Task.Name + taskStatus);

                //
                // Indicate that the background task has completed.
                //
                _deferral.Complete();
            }
        }

        public static void Start(BackgroundTaskRegistrationGroup sender, BackgroundActivatedEventArgs args)
        {
            Start(args.TaskInstance);
        }

        public static void Start(IBackgroundTaskInstance taskInstance)
        {
            // Use the taskInstance.Name and/or taskInstance.InstanceId to determine
            // what background activity to perform. In this sample, all of our
            // background activities are the same, so there is nothing to check.
            var activity = new BgTask();
            activity.Run(taskInstance);
        }
    }
}
