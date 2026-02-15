using GoogleMobileAds.Api;
using UnityEngine;

public class GoogleMobileAdsController : MonoBehaviour
{
    public static GoogleMobileAdsController Instance;
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("[Ads] Initializing MobileAds");

        MobileAds.Initialize(initStatus =>
        {
            IsInitialized = true;
            Debug.Log("[Ads] MobileAds initialized");
        });
    }
}
