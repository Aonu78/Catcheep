#if UNITY_ANDROID
using System;
using System.Collections;
using Assets.SimpleAndroidNotifications;
using Unity.Notifications.Android;
using UnityEngine;

public sealed class CatcheepNotificationDispatcher : MonoBehaviour
{
    public void Send(NotificationParams parameters) { StartCoroutine(Schedule(parameters)); }

    private IEnumerator Schedule(NotificationParams parameters)
    {
        AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
        {
            Id = "catcheep_rewards",
            Name = "Daily rewards",
            Description = "Daily spin reminders",
            Importance = Importance.Default,
            EnableVibration = parameters.Vibrate
        });

        var permission = new PermissionRequest();
        while (permission.Status == PermissionStatus.RequestPending) yield return null;
        if (permission.Status == PermissionStatus.Allowed && PlayerPrefs.GetInt("notifications") == 0)
        {
            AndroidNotificationCenter.SendNotificationWithExplicitID(new AndroidNotification
            {
                Title = parameters.Title,
                Text = parameters.Message,
                FireTime = DateTime.Now.Add(parameters.Delay)
            }, "catcheep_rewards", parameters.Id);
        }
        Destroy(gameObject);
    }
}
#endif
