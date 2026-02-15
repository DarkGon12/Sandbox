using MessagePipe;
using Services.ObjectPool;
using System;
using UnityEngine;
using Zenject;

namespace Creatures
{
    public class CreatureEffect : MonoBehaviour
    {
        [Header("Blood FX Pool")]
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private int _bloodFxPoolSize = 20;

        [Inject] private ISubscriber<CreatureVisibleEffect> _creatureVisibleEffectEventSub;

        private bool _canShow = true;
        private IDisposable _disposable;

        private void OnDestroy() => _disposable?.Dispose();

        private void Start()
        {
            var bag = DisposableBag.CreateBuilder();
            _creatureVisibleEffectEventSub.Subscribe(CanShowEffect).AddTo(bag);
            bag.Add(_disposable);
            _disposable = bag.Build();
        }

        public void CanShowEffect(CreatureVisibleEffect evt) => _canShow = evt.IsShow;

        public void PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!_canShow) return;

            GameObject fx = Instantiate(_bloodFxPrefab);

            fx.transform.SetPositionAndRotation(
                hitPoint + hitNormal * 0.02f,
                Quaternion.LookRotation(hitNormal)
            );
        }
    }
}