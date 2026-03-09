using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class ConsentManager : MonoBehaviour
{
    private bool _adsInitialized;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RequestConsent();
    }

    private void RequestConsent()
    {
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false
        };

        ConsentInformation.Update(request, (FormError error) =>
        {
            if (error != null)
            {
                Debug.LogWarning("UMP Update error: " + error.Message);
                InitializeAds();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                    Debug.LogWarning("UMP Form error: " + formError.Message);

                InitializeAds();
            });
        });
    }

    private void InitializeAds()
    {
        if (_adsInitialized)
            return;

        _adsInitialized = true;

        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[Ads] Mobile Ads initialized");

            GoogleMobileAdsController.SetInitialized();
        });
    }
}