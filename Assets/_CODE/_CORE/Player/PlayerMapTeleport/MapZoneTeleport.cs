using Sandbox.Player;
using UnityEngine;

public class MapZoneTeleport : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Player")
        {
            other.GetComponent<PlayerInformation>().TakeDamage(1000000);
        }
    }
}