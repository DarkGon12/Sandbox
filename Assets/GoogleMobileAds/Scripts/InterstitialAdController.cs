using GoogleMobileAds.Api;
using System;
using UnityEngine;

public class InterstitialAdController
{
    public Action AdClose;
    private InterstitialAd _interstitialAd;

    private const string AdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-9039391751547678/6039629156";
#else
        "unexpected_platform";
#endif

    public void Load()
    {
        if (!GoogleMobileAdsController.IsInitialized)
        {
            Debug.LogWarning("[Ads][Interstitial] SDK not initialized");
            return;
        }

        Debug.Log("[Ads][Interstitial] Loading");

        InterstitialAd.Load(AdUnitId, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("[Ads][Interstitial] Load failed: " + error);
                    return;
                }

                _interstitialAd = ad;
                RegisterCallbacks(ad);

                Debug.Log("[Ads][Interstitial] Loaded");
            });
    }

    public void Show()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("[Ads][Interstitial] Showing");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[Ads][Interstitial] Not ready");
        }
    }

    private void RegisterCallbacks(InterstitialAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[Ads][Interstitial] Closed");
            _interstitialAd = null;
            Load(); // preload next
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            AdClose?.Invoke();
        };
        ad.OnAdPaid += adValue =>
        {
            AppsFlyerSender.LogAdRevenueToAppsFlyer(
                adValue,
                "Interstitial",
                "game_interstitial",
                AdUnitId
            );
        };
    }
}
