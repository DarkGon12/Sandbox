using Firebase.Extensions;
using Firebase.Analytics;
using UnityEngine;
using Firebase;
using System;
using Creatures;

namespace Sandbox.Analyse
{
    public static class FirebaseSender
    {
        private static bool _firebaseInitialized = false;
        private static DependencyStatus _dependencyStatus = DependencyStatus.UnavailableOther;
        private static string _userId;

        public static void InitializeFirebase()
        {
            Debug.Log("[Firebase] InitializeFirebase called");

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                _dependencyStatus = task.Result;
                Debug.Log($"[Firebase] Dependency status: {_dependencyStatus}");

                if (_dependencyStatus == DependencyStatus.Available)
                {
                    Initialize();
                }
                else
                {
                    Debug.LogError("[Firebase] Could not resolve dependencies: " + _dependencyStatus);
                }
            });
        }

        private static void Initialize()
        {
           // RemoteConfigManager.Initialize().ContinueWithOnMainThread(task =>
           // {
           //     Debug.Log("[RemoteConfig] Loaded");
           // });

            Debug.Log("[Firebase] Initializing Analytics");

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            FirebaseAnalytics.SetUserProperty(
                FirebaseAnalytics.UserPropertySignUpMethod,
                "Google"
            );

            if (!PlayerPrefs.HasKey("uid"))
            {
                _userId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("uid", _userId);
                Debug.Log($"[Firebase] Generated new userId: {_userId}");
            }
            else
            {
                _userId = PlayerPrefs.GetString("uid");
                Debug.Log($"[Firebase] Loaded existing userId: {_userId}");
            }

            FirebaseAnalytics.SetUserId(_userId);
            FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));

            _firebaseInitialized = true;
            Debug.Log("[Firebase] Firebase initialized SUCCESSFULLY");
        }

        public static void LogEventSafe(string eventName)
        {
            if (!_firebaseInitialized)
            {
                Debug.LogWarning($"[Firebase] LogEventSafe skipped (not initialized): {eventName}");
                return;
            }

            Debug.Log($"[Firebase] LogEvent: {eventName}");
            FirebaseAnalytics.LogEvent(eventName);
        }

        public static void LogEventCreature(
            string eventName,
            string creatureName,
            CreaturesType creatureType)
        {
            if (!_firebaseInitialized)
            {
                Debug.LogWarning($"[Firebase] LogEventCreature skipped (not initialized): {eventName}");
                return;
            }

            Debug.Log(
                $"[Firebase] LogEventCreature: {eventName}, " +
                $"creature_name={creatureName}, creature_type={creatureType}"
            );

            FirebaseAnalytics.LogEvent(
                eventName,
                new Parameter("creature_name", creatureName),
                new Parameter("creature_type", creatureType.ToString())
            );
        }
    }
}