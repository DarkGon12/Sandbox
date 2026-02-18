using System.Collections;
using UnityEngine;

namespace Sandbox.Weapon
{
    public class HandMagicModel : MonoBehaviour
    {
        public bool IsAttacking => _isAttacking;

        [Header("Magic Type")]
        [SerializeField] private MagicType _magicType;

        [Header("Effects")]
        [SerializeField] private GameObject _idleEffectGO;
        [SerializeField] private GameObject _attackEffectGO;

        [Header("Animation")]
        [SerializeField] private Animator _animatorEffect;
        [SerializeField] private string _activeParam = "active";

        [Header("Timing")]
        [SerializeField] private float _attackStartDelay = 0.25f;
        [SerializeField] private float _cooldown = 1.5f;

        [Header("Input")]
        [SerializeField] private CarControlButton _attackButton;

        private bool _isAttacking;
        private bool _onCooldown;
        private Coroutine _attackRoutine;

        private void Start()
        {
            ShowIdle();
        }

        private void Update()
        {
            _animatorEffect.SetBool(_activeParam, _attackButton.Pressed);

            if (_attackButton.Pressed)
                TryStartAttack();
            else
                StopAttack();
        }

        // ================= ATTACK =================

        private void TryStartAttack()
        {
            if (_isAttacking)
                return;

            if (_magicType == MagicType.Cooldown && _onCooldown)
                return;

            _attackRoutine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;

            yield return new WaitForSeconds(_attackStartDelay);

            if (!_attackButton.Pressed)
            {
                _isAttacking = false;
                yield break;
            }

            ShowAttack();

            if (_magicType == MagicType.Cooldown)
            {
                yield return new WaitForSeconds(_cooldown);
                StopAttack();
            }
        }

        private void StopAttack()
        {
            if (!_isAttacking)
                return;

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);

            _attackRoutine = null;
            _isAttacking = false;

            ShowIdle();

            if (_magicType == MagicType.Cooldown)
                StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            _onCooldown = true;
            yield return new WaitForSeconds(_cooldown);
            _onCooldown = false;
        }

        // ================= EFFECTS =================

        private void ShowIdle()
        {
            _attackEffectGO.SetActive(false);
            _idleEffectGO.SetActive(true);
        }

        private void ShowAttack()
        {
            _idleEffectGO.SetActive(false);
            _attackEffectGO.SetActive(true);
        }
    }

    public enum MagicType
    {
        Continuous,
        Cooldown
    }
}
