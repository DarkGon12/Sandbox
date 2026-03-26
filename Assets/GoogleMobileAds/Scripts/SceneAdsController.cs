
using MessagePipe;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Zenject;

public class SceneAdsController : MonoBehaviour
{
    [Header("Interstitial")]
    [SerializeField] private GameObject _interstitialAdInfoPanel;
    [SerializeField] private TextMeshProUGUI _interstitialAdInfoText;
    [Space(15)]
    [SerializeField] private int _interstitialMaxEnergy = 2;
    [SerializeField] private int _interstitialCoolDown = 90;
    [SerializeField] private bool _readyInterstitial = true;

    [Inject] private ISubscriber<ShowInterstitialAdEvent> _showInterAdEventSub;
    [Inject] private AppOpenAdController _appOpen;

    private InterstitialAdController _interstitial;
    private AdsBanner _banner;

    private bool _canShowInter = true;
    private int _interstitialEnergy;
    private IDisposable _disposible;

    public AppOpenAdController GetAppOpen => _appOpen;
    public AdsBanner GetBanner => _banner;

    public void CanVisibleInter(bool state) => _canShowInter = state; 

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GoogleMobileAdsController.IsInitialized);
        yield return new WaitUntil(() => RemoteConfigManager.IsLoaded);

        _interstitialCoolDown = RemoteConfigManager.InterstitialDelay;

        Debug.Log("[Ads] Cooldown from Firebase: " + _interstitialCoolDown);

        _appOpen.LoadAd();

        _interstitialEnergy = _interstitialMaxEnergy;
        var bag = DisposableBag.CreateBuilder();
        _showInterAdEventSub.Subscribe(OnShowInterAdEvent).AddTo(bag);
        _disposible = bag.Build();

        _banner = new AdsBanner();
        _banner.CreateAndShow();

        _interstitial = new InterstitialAdController();
        _interstitial.Load();
    }

    private void OnShowInterAdEvent(ShowInterstitialAdEvent evt)
    {
        if (!_canShowInter) return;
        if (!_readyInterstitial) return;

            _interstitialEnergy -= 1;

        if (_interstitialEnergy > 0) return;

        _interstitialEnergy = _interstitialMaxEnergy;
        StartCoroutine(TryLoadInterAd());
    }

    private IEnumerator TryLoadInterAd()
    {
        if (!_readyInterstitial) yield return null;

        _interstitialAdInfoPanel.SetActive(true);
        int timeShow = 2;

        while (timeShow > 0)
        {
            _interstitialAdInfoText.text = timeShow.ToString();
            yield return new WaitForSeconds(1);
            timeShow--;
        }

        _interstitial.Show();
        _interstitialAdInfoPanel.SetActive(false);

        _readyInterstitial = false;
        yield return StartCoroutine(InterstitialAdCooldown());
    }

    private IEnumerator InterstitialAdCooldown()
    {
        yield return new WaitForSecondsRealtime(_interstitialCoolDown);
        _readyInterstitial = true;
    }

    private void OnDestroy() => _disposible?.Dispose();
}
