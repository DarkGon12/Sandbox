using AppsFlyerSDK;
using GoogleMobileAds.Api;
using System.Collections.Generic;

public static class AppsFlyerSender
{
    public static void LogAdRevenueToAppsFlyer(AdValue adValue, string adType, string placement, string adUnitId)
    {
        double revenue = adValue.Value / 1_000_000.0;
        string currency = adValue.CurrencyCode;

        var additionalParams = new Dictionary<string, string>()
    {
        { AdRevenueScheme.AD_TYPE, adType },
        { AdRevenueScheme.PLACEMENT, placement },
        { AdRevenueScheme.AD_UNIT, adUnitId }
    };

        var logRevenue = new AFAdRevenueData(
            "admob",
            MediationNetwork.GoogleAdMob,
            currency,
            revenue
        );

        AppsFlyer.logAdRevenue(logRevenue, additionalParams);
    }
}