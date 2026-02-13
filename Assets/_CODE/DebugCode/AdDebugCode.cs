using GoogleMobileAds.Sample;
using UnityEngine;
using Zenject;

public class AdDebugCode : MonoBehaviour
{
    [Inject] private InterstitialAdController _adInterController;

    private void Start()
    {
        _adInterController.LoadAd();
    }

    [ContextMenu("DebugLoadAd")]
    private void LoadAd()
    {
        _adInterController.LoadAd();
    }
}