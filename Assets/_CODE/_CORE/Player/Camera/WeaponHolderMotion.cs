using DG.Tweening;
using UnityEngine;

namespace Sandbox.Player
{
    public class WeaponHolderMotion : MonoBehaviour
    {
        [Header("Look Sway")]
        [SerializeField] private float _swayAmount = 2.5f;
        [SerializeField] private float _maxRotation = 6f;
        [SerializeField] private float _swaySmoothTime = 0.25f;

        [Header("Recoil")]
        [SerializeField] private float _recoilBackForce = 0.08f;
        [SerializeField] private float _recoilRotationX = 6f;
        [SerializeField] private float _recoilRotationZ = 3f;
        [SerializeField] private float _recoilKickTime = 0.08f;
        [SerializeField] private float _recoilReturnTime = 0.15f;

        private Vector3 _startLocalRot;
        private Vector3 _startLocalPos;

        private Vector3 _currentTargetRot;

        private Tween _rotationTween;
        private Tween _recoilTween;

        private void Awake()
        {
            _startLocalRot = transform.localEulerAngles;
            _startLocalPos = transform.localPosition;
            _currentTargetRot = Vector3.zero;
        }

        #region LOOK SWAY

        public void ApplyLookSway(Vector2 lookDelta)
        {
            float x = -lookDelta.y * _swayAmount;
            float y = lookDelta.x * _swayAmount;

            x = Mathf.Clamp(x, -_maxRotation, _maxRotation);
            y = Mathf.Clamp(y, -_maxRotation, _maxRotation);

            _currentTargetRot = new Vector3(x, y, 0f);

            SmoothRotateTo(_currentTargetRot);
        }

        private void SmoothRotateTo(Vector3 offset)
        {
            _rotationTween?.Kill();

            _rotationTween = transform.DOLocalRotate(
                    _startLocalRot + offset,
                    _swaySmoothTime
                )
                .SetEase(Ease.OutSine);
        }

        private void LateUpdate()
        {
            _currentTargetRot = Vector3.Lerp(
                _currentTargetRot,
                Vector3.zero,
                Time.deltaTime * 4f
            );

            SmoothRotateTo(_currentTargetRot);
        }

        #endregion

        #region RECOIL

        public void PlayShoot()
        {
            _recoilTween?.Kill();

            Vector3 recoilPos = _startLocalPos - new Vector3(0f, 0f, _recoilBackForce);

            Vector3 recoilRot = new Vector3(
                -_recoilRotationX,
                0f,
                Random.Range(-_recoilRotationZ, _recoilRotationZ)
            );

            Sequence seq = DOTween.Sequence();

            seq.Append(transform.DOLocalMove(recoilPos, _recoilKickTime)
                .SetEase(Ease.OutQuad));

            seq.Join(transform.DOLocalRotate(
                    _startLocalRot + recoilRot,
                    _recoilKickTime
                ).SetEase(Ease.OutQuad));

            seq.Append(transform.DOLocalMove(_startLocalPos, _recoilReturnTime)
                .SetEase(Ease.OutSine));

            seq.Join(transform.DOLocalRotate(
                    _startLocalRot,
                    _recoilReturnTime
                ).SetEase(Ease.OutSine));

            _recoilTween = seq;
        }

        #endregion

        private void OnDisable()
        {
            _rotationTween?.Kill();
            _recoilTween?.Kill();
        }
    }
}
