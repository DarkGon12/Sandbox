using Firebase.RemoteConfig;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class RemoteConfigManager
{
    public static int InterstitialDelay = 90;
    public static bool IsLoaded = false;

    public static void Initialize()
    {
        Debug.Log("[RemoteConfig] Initialize");

        var defaults = new Dictionary<string, object>()
        {
            { "ad_delay", 90}
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
        .ContinueWithOnMainThread(task =>
        {
            Debug.Log("[RemoteConfig] Defaults set");

            Fetch();
        });
    }

    private static void Fetch()
    {
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero)
        .ContinueWithOnMainThread(fetchTask =>
        {
            if (fetchTask.IsCompleted)
            {
                Debug.Log("[RemoteConfig] Fetch success");

                FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                .ContinueWithOnMainThread(activateTask =>
                {
                    ApplyValues();
                });
            }
            else
            {
                Debug.LogError("[RemoteConfig] Fetch failed");
            }
        });
    }

    private static void ApplyValues()
    {
        InterstitialDelay =
            (int)FirebaseRemoteConfig.DefaultInstance
            .GetValue("ad_delay").LongValue;

        IsLoaded = true;
        var value = FirebaseRemoteConfig.DefaultInstance.GetValue("ad_delay");

        Debug.Log(value.Source);

        var info = FirebaseRemoteConfig.DefaultInstance.Info;

        Debug.Log("Fetch status: " + info.LastFetchStatus);
        Debug.Log("Fetch time: " + info.FetchTime);
        Debug.Log("[RemoteConfig] ad_delay = " + InterstitialDelay);
    }
}