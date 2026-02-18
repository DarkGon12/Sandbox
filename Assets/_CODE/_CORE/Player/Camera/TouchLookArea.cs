using UnityEngine;

namespace Sandbox.Player
{
    public class TouchLookArea : MonoBehaviour
    {
        public float CurrentPitch => xRotation;

        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private Transform player;
        [SerializeField] private Transform cam;
        [SerializeField] private WeaponHolderMotion weaponMotion;
        [SerializeField] private CarControlButton fireButton;

        private float xRotation;
        private Rigidbody rb;

        private int lookFingerId = -1;
        private Vector2 lastPos;

        void Awake()
        {
            rb = player.GetComponent<Rigidbody>();
        }

        void Update()
        {
            foreach (var touch in Input.touches)
            {
                if (fireButton.Pressed && touch.fingerId == fireButton.PointerId)
                {
                    ProcessTouch(touch);
                    continue;
                }

                if (touch.position.x < Screen.width * 0.5f)
                    continue;

                ProcessTouch(touch); 
            }
        }


        private void ProcessTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                lookFingerId = touch.fingerId;
                lastPos = touch.position;
            }

            if (touch.fingerId != lookFingerId)
                return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastPos;
                lastPos = touch.position;
                ApplyLook(delta);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (lookFingerId == touch.fingerId)
                    lookFingerId = -1;
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            float yaw = delta.x * sensitivity;
            float pitch = delta.y * sensitivity;

            xRotation -= pitch;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            Quaternion deltaRotation = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);

            weaponMotion.ApplyLookSway(delta);
        }
    }
}
