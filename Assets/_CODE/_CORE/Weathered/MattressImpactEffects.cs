using UnityEngine;

public class MattressImpactEffects : MonoBehaviour
{
    [Header("Impact Thresholds")]
    [SerializeField] private float _minImpactSpeed = 4f;
    [SerializeField] private float _minImpactImpulse = 8f;
    [SerializeField] private float _minCreatureImpactSpeed = 5f;

    [Header("Cooldown")]
    [SerializeField] private float _impactCooldown = 0.15f;
    [SerializeField] private float _creatureHitCooldown = 0.25f;

    [Header("Dust VFX")]
    [SerializeField] private ParticleSystem _dustPrefab;
    [SerializeField] private int _dustPoolSize = 4;

    [Header("Sound")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _impactClips;

    [Header("Creature")]
    [SerializeField] private string _creatureTag = "Creature";

    [Header("Self Collision")]
    [SerializeField] private bool _ignoreSelfCollisions = true;

    private ParticleSystem[] _dustPool;
    private Collider[] _ownColliders;

    private int _dustPoolIndex;
    private int _clipIndex;

    private float _lastImpactTime = -100f;
    private float _lastCreatureHitTime = -100f;

    private float _minImpactSpeedSqr;
    private float _minCreatureImpactSpeedSqr;
    private float _minImpactImpulseSqr;

    private void Awake()
    {
        _minImpactSpeedSqr = _minImpactSpeed * _minImpactSpeed;
        _minCreatureImpactSpeedSqr = _minCreatureImpactSpeed * _minCreatureImpactSpeed;
        _minImpactImpulseSqr = _minImpactImpulse * _minImpactImpulse;

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        CacheOwnColliders();

        if (_ignoreSelfCollisions)
            IgnoreOwnCollisions();

        CreateDustPool();
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
    }

    public void ProcessCollision(Collision collision)
    {
        if (collision.contactCount == 0)
            return;

        if (IsSelfCollision(collision.collider))
            return;

        float speedSqr = collision.relativeVelocity.sqrMagnitude;
        float impulseSqr = collision.impulse.sqrMagnitude;

        bool strongImpact =
            speedSqr >= _minImpactSpeedSqr ||
            impulseSqr >= _minImpactImpulseSqr;

        if (!strongImpact)
            return;

        ContactPoint contact = collision.GetContact(0);

        if (Time.time >= _lastImpactTime + _impactCooldown)
        {
            _lastImpactTime = Time.time;

            SpawnDust(contact.point, contact.normal);
            PlayImpactSound();
        }

        bool strongCreatureImpact = speedSqr >= _minCreatureImpactSpeedSqr;

        if (strongCreatureImpact && Time.time >= _lastCreatureHitTime + _creatureHitCooldown)
            TryHitCreature(collision.collider);
    }

    private bool IsSelfCollision(Collider otherCollider)
    {
        if (otherCollider == null)
            return true;

        MattressImpactEffects otherMattress =
            otherCollider.GetComponentInParent<MattressImpactEffects>();

        return otherMattress == this;
    }

    private void CacheOwnColliders()
    {
        _ownColliders = GetComponentsInChildren<Collider>(true);
    }

    private void IgnoreOwnCollisions()
    {
        if (_ownColliders == null)
            return;

        for (int i = 0; i < _ownColliders.Length; i++)
        {
            Collider first = _ownColliders[i];

            if (first == null)
                continue;

            for (int j = i + 1; j < _ownColliders.Length; j++)
            {
                Collider second = _ownColliders[j];

                if (second == null)
                    continue;

                Physics.IgnoreCollision(first, second, true);
            }
        }
    }

    private void TryHitCreature(Collider hitCollider)
    {
        if (hitCollider == null)
            return;

        CreatureRagdollHelper creatureRagdollHelper =
            hitCollider.GetComponentInParent<CreatureRagdollHelper>();

        if (creatureRagdollHelper == null)
            return;

        if (!HasCreatureTag(hitCollider.transform, creatureRagdollHelper.transform))
            return;

        _lastCreatureHitTime = Time.time;

        creatureRagdollHelper.EnableRagdoll();
        creatureRagdollHelper.DisableRagdoll();
    }

    private bool HasCreatureTag(Transform hitTransform, Transform helperTransform)
    {
        Transform current = hitTransform;

        while (current != null)
        {
            if (current.CompareTag(_creatureTag))
                return true;

            if (current == helperTransform)
                break;

            current = current.parent;
        }

        return helperTransform.CompareTag(_creatureTag);
    }

    private void CreateDustPool()
    {
        if (_dustPrefab == null || _dustPoolSize <= 0)
            return;

        _dustPool = new ParticleSystem[_dustPoolSize];

        for (int i = 0; i < _dustPoolSize; i++)
            _dustPool[i] = CreateDustParticle();
    }

    private ParticleSystem CreateDustParticle()
    {
        ParticleSystem particle = Instantiate(_dustPrefab);
        particle.gameObject.SetActive(false);

        ParticleSystem.MainModule main = particle.main;
        main.stopAction = ParticleSystemStopAction.None;

        return particle;
    }

    private void SpawnDust(Vector3 position, Vector3 normal)
    {
        if (_dustPrefab == null || _dustPool == null || _dustPool.Length == 0)
            return;

        ParticleSystem particle = _dustPool[_dustPoolIndex];

        if (particle == null)
        {
            particle = CreateDustParticle();
            _dustPool[_dustPoolIndex] = particle;
        }

        _dustPoolIndex++;

        if (_dustPoolIndex >= _dustPool.Length)
            _dustPoolIndex = 0;

        Transform particleTransform = particle.transform;
        particleTransform.position = position;
        particleTransform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

        GameObject particleObject = particle.gameObject;

        if (!particleObject.activeSelf)
            particleObject.SetActive(true);

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
    }

    private void PlayImpactSound()
    {
        if (_audioSource == null)
            return;

        if (_impactClips == null || _impactClips.Length == 0)
            return;

        AudioClip clip = _impactClips[_clipIndex];

        _clipIndex++;

        if (_clipIndex >= _impactClips.Length)
            _clipIndex = 0;

        if (clip != null)
            _audioSource.PlayOneShot(clip);
    }

    private void OnValidate()
    {
        _minImpactSpeed = Mathf.Max(0f, _minImpactSpeed);
        _minImpactImpulse = Mathf.Max(0f, _minImpactImpulse);
        _minCreatureImpactSpeed = Mathf.Max(0f, _minCreatureImpactSpeed);
        _impactCooldown = Mathf.Max(0f, _impactCooldown);
        _creatureHitCooldown = Mathf.Max(0f, _creatureHitCooldown);
        _dustPoolSize = Mathf.Max(0, _dustPoolSize);
    }
}