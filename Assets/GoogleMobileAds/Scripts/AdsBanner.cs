using GoogleMobileAds.Api;
using UnityEngine;

public class AdsBanner : MonoBehaviour
{
    [SerializeField] private string _adUnit_Id;
    private BannerView _bannerView;

    public void CreateBannerView()
    {
        _bannerView = new BannerView(_adUnit_Id, AdSize.Leaderboard, AdPosition.Bottom);
        LoadBannerView();
    }

    private void LoadBannerView()
    {
        _bannerView.LoadAd(new AdRequest());
    }

    public void DestroyBanner()
    {
        if (_bannerView != null)
        {
            _bannerView.Destroy();
            _bannerView = null;
        }
    }

    private void OnDestroy() => DestroyBanner();
}