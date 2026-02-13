using GoogleMobileAds.Sample;
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

    [Inject] private AppOpenAdController _appOpen;
    [Inject] private InterstitialAdController _interstitial;
    [Inject] private AdsBanner _banner;

    [Inject] private ISubscriber<ShowInterstitialAdEvent> _showInterAdEventSub;

    private int _interstitialEnergy;
    private IDisposable _disposible;

    private void Start()
    {
        _interstitialEnergy = _interstitialMaxEnergy;
        var bag = DisposableBag.CreateBuilder();
        _showInterAdEventSub.Subscribe(OnShowInterAdEvent).AddTo(bag);
        bag.Add(_disposible);
        _disposible = bag.Build();

        _banner.CreateBannerView();
    }

    private void OnShowInterAdEvent(ShowInterstitialAdEvent evt)
    {
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
        int timeShow = 4;

        while (timeShow > 0)
        {
            _interstitialAdInfoText.text = "Out of energy Load AD: " + timeShow.ToString();
            yield return new WaitForSeconds(1);
            timeShow--;
        }

        _interstitial.LoadAd();
        _interstitialAdInfoPanel.SetActive(false);

        yield return StartCoroutine(InterstitialAdCooldown());
    }

    private IEnumerator InterstitialAdCooldown()
    {
        _readyInterstitial = false;
        yield return new WaitForSecondsRealtime(_interstitialCoolDown);
        _readyInterstitial = true;
    }

    private void OnDestroy() => _disposible?.Dispose();
}
