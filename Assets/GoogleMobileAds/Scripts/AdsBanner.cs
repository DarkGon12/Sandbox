using GoogleMobileAds.Api;
using UnityEngine;

public class AdsBanner
{
    private BannerView _bannerView;

    private const string AdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-9039391751547678/2983162927"; // test banner
#else
        "unexpected_platform";
#endif

    public void CreateAndShow()
    {
        if (!GoogleMobileAdsController.IsInitialized)
        {
            Debug.LogWarning("[Ads][Banner] SDK not initialized");
            return;
        }

        Debug.Log("[Ads][Banner] Creating banner");

        _bannerView = new BannerView(
            AdUnitId,
            AdSize.Banner,
            AdPosition.Bottom
        );

        AdRequest request = new AdRequest();
        _bannerView.LoadAd(request);
        _bannerView.OnBannerAdLoaded += () =>
        {
            _bannerView.Show();
        };

        RegisterCallbacks(_bannerView);
    }

    public void ShowBanner(bool state)
    {
        if (state)
            _bannerView.Show();
        else
            _bannerView.Hide();
    }

    public void Destroy()
    {
        _bannerView?.Destroy();
        _bannerView = null;
    }

    private void RegisterCallbacks(BannerView _bannerView)
    {
        _bannerView.OnAdPaid += adValue =>
        {
            AppsFlyerSender.LogAdRevenueToAppsFlyer(
                adValue,
                "Banner",
                "bottom_banner",
                AdUnitId
            );
        };
    }
}

