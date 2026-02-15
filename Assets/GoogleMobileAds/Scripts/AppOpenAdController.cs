using GoogleMobileAds.Api;
using UnityEngine;
using GoogleMobileAds.Common;

public class AppOpenAdController : MonoBehaviour
{
    private AppOpenAd _appOpenAd;
    private bool _isShowing;

    private const string AdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-9039391751547678/1670081254";
#else
        "unexpected_platform";
#endif

    private void Awake()
    {
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;        
    }

    public void LoadAd()
    {
        if (!GoogleMobileAdsController.IsInitialized)
            return;

        Debug.Log("[Ads][AppOpen] Loading");

        AppOpenAd.Load(
                AdUnitId,
                new AdRequest(),
                (AppOpenAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError("[Ads][AppOpen] Load failed: " + error);
                        return;
                    }

                    _appOpenAd = ad;
                    Debug.Log("[Ads][AppOpen] Loaded");
                }
            );
    }

    private void OnAppStateChanged(AppState state)
    {
        if (state == AppState.Foreground)
            ShowIfAvailable();
    }

    private void ShowIfAvailable()
    {
        if (_isShowing || _appOpenAd == null || !_appOpenAd.CanShowAd())
        {
            Debug.Log("[Ads][AppOpen] Not ready");
            return;
        }

        Debug.Log("[Ads][AppOpen] Showing");
        _isShowing = true;

        _appOpenAd.OnAdFullScreenContentClosed += () =>
        {
            _isShowing = false;
            _appOpenAd = null;
            LoadAd();
        };

        _appOpenAd.Show();
    }
}
