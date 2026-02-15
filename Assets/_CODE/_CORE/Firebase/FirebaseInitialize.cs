using Sandbox.Analyse;
using UnityEngine;

public class FirebaseInitialize : MonoBehaviour
{
    private void Start()
    {
        FirebaseSender.InitializeFirebase();
    }
}