using Sandbox.Weapon;
using UnityEngine;

public class RainbowGun : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private CarControlButton _fireButton;
    [SerializeField] private WeaponData _weaponData;
    [SerializeField] private ParticleSystem _muzzleParticle;
    [SerializeField] private AudioClip _shootClip;
}