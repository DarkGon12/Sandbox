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
    }

    public static void SetInitialized()
    {
        IsInitialized = true;
        Debug.Log("[Ads] SDK Ready");
    }
}