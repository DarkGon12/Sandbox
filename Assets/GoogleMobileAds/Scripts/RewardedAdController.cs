using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class RewardedAdService : MonoBehaviour
{
#if UNITY_ANDROID
    private const string AdUnitId = "ca-app-pub-9039391751547678/3062112026";
#elif UNITY_IPHONE
    private const string AdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string AdUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;
    private Action _onReward;

    private void Awake()
    {
        LoadAd();
    }

    // ================= PUBLIC API =================

    /// <summary>
    /// Показать рекламу и выдать награду, если досмотрели
    /// </summary>
    public bool ShowAd(Action onReward)
    {
        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
        {
            Debug.LogWarning("[Ads] Rewarded not ready");
            LoadAd();
            return false;
        }

        _onReward = onReward;

        _rewardedAd.Show((Reward reward) =>
        {
            Debug.Log($"[Ads] Reward granted: {reward.Amount} {reward.Type}");
            _onReward?.Invoke();
            _onReward = null;
        });

        return true;
    }

    // ================= LOAD =================

    public void LoadAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        var request = new AdRequest();

        RewardedAd.Load(AdUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("[Ads] Rewarded load failed: " + error);
                return;
            }

            Debug.Log("[Ads] Rewarded loaded");
            _rewardedAd = ad;

            RegisterEvents(ad);
        });
    }

    // ================= EVENTS =================

    private void RegisterEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[Ads] Rewarded closed");
            LoadAd(); // сразу грузим следующую
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[Ads] Rewarded failed to show: " + error);
            LoadAd();
        };
    }
}
