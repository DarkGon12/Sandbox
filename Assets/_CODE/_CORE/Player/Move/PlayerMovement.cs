using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sandbox.PlayerMove
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private FloatingJoystick _inputJoy;

        [Header("Jump")]
        [SerializeField] private float _jumpForce = 6f;
        [SerializeField] private float _groundCheckDistance = 0.25f;
        [SerializeField] private LayerMask _groundMask;

        [Header("Jetpack")]
        [SerializeField] private float _jetpackForce = 10f;
        [SerializeField] private Button _jetPackBtn;

        private Rigidbody _rb;
        private Vector3 _moveInput;
        private bool _isGrounded;
        private bool _jetpackActive;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            SetupJetpackButton();
        }

        void Update()
        {
            float x = _inputJoy.Direction.x;
            float z = _inputJoy.Direction.y;

            _moveInput = new Vector3(x, 0f, z).normalized;

            CheckGround();
        }

        void FixedUpdate()
        {
            Vector3 move = transform.TransformDirection(_moveInput) * _moveSpeed;
            _rb.linearVelocity = new Vector3(move.x, _rb.linearVelocity.y, move.z);

            if (_jetpackActive && !_isGrounded)
            {
                _rb.AddForce(Vector3.up * _jetpackForce, ForceMode.Acceleration);
            }
        }

        private void Jump()
        {
            if (!_isGrounded) return;

            Vector3 vel = _rb.linearVelocity;
            vel.y = 0;
            _rb.linearVelocity = vel;

            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }

        private void CheckGround()
        {
            _isGrounded = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                _groundCheckDistance,
                _groundMask
            );
        }

        private void SetupJetpackButton()
        {
            if (_jetPackBtn == null) return;

            EventTrigger trigger = _jetPackBtn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = _jetPackBtn.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry down = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            down.callback.AddListener(_ =>
            {
                if (_isGrounded)
                    Jump();       

                _jetpackActive = true;
            });

            EventTrigger.Entry up = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            up.callback.AddListener(_ =>
            {
                _jetpackActive = false;
            });

            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
        }
    }
}