//using Firebase.RemoteConfig;
//using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
/*
public static class RemoteConfigManager
{
    public static int InterstitialDelay = 90;

    public static async Task Initialize()
    {
        var defaults = new Dictionary<string, object>()
        {
            { "ad_delay", 90 }
        };

        await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);

        await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);

        await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

        InterstitialDelay =
            (int)FirebaseRemoteConfig.DefaultInstance
            .GetValue("ad_delay").LongValue;

        Debug.Log("[RemoteConfig] ad_delay = " + InterstitialDelay);
    }
}
*/