using UnityEngine;

namespace nugen.modding
{

    public class CameraControl : MonoBehaviour
    {

        public static CameraControl instance;

        public static CameraControl GetInstance()
        {

            if (instance != null) return instance;

            instance = new GameObject("CameraControl").AddComponent<CameraControl>();

            return instance;

        }

        public static KeyCode moveLeft = KeyCode.A;
        public static KeyCode moveRight = KeyCode.D;
        public static KeyCode moveForward = KeyCode.W;
        public static KeyCode moveBack= KeyCode.S;
        public static KeyCode moveUp = KeyCode.Space;
        public static KeyCode moveDown = KeyCode.LeftControl;

        public static KeyCode toggleKey = KeyCode.LeftShift;

        public static KeyCode toggleModeKey = KeyCode.Tab;

        public bool toggle = false;

        public float sensitivity = 1;

        public float maxSpeedFast = 10;

        public float minSpeedFast = 1;

        public float maxSpeedSlow = 1;

        public float minSpeedSlow = 0.01f;

        [Range(0, 1)]
        public float fastSpeed = 0.25f;

        [Range(0, 1)]
        public float slowSpeed = 0.4f;

        public bool precisionToggle = false;

        public float yaw = 0;

        public float pitch = 0;

        private void Start()
        {

            yaw = Camera.main.transform.rotation.eulerAngles.y;

            pitch = Camera.main.transform.rotation.eulerAngles.x;

        }

        void Update()
        {

            if (instance != null)
            {

                if (instance != this)
                {

                    GameObject.DestroyImmediate(this.gameObject);

                    return;

                }

            }
            else instance = this;

            if (toggleKey == TransformWidget.shiftAxisKey) if (TransformWidget.IsActive()) return;

            if (Input.GetKeyDown(toggleKey))
            {

                toggle = !toggle;

                if (toggle)
                {

                    Cursor.lockState = CursorLockMode.Locked;

                } else
                {

                    Cursor.lockState = CursorLockMode.None;

                    return;

                }

            }

            if (!toggle) return;

            if (Input.GetKeyDown(toggleModeKey)) precisionToggle = !precisionToggle;

            if (precisionToggle)
            {

                slowSpeed = Mathf.Clamp01(slowSpeed + Input.mouseScrollDelta.y * 0.01f);

            } 
            else
            {

                fastSpeed = Mathf.Clamp01(fastSpeed + Input.mouseScrollDelta.y * 0.01f);

            }

            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            yaw = yaw + mx * sensitivity;

            pitch = Mathf.Clamp(pitch - my * sensitivity, -89, 89);

            yaw = yaw - (360 * Mathf.Floor(yaw / 360));

            Camera.main.transform.rotation = Quaternion.Euler(pitch, yaw, 0);

            float speed = precisionToggle ? Mathf.Lerp(minSpeedSlow, maxSpeedSlow, slowSpeed) : Mathf.Lerp(minSpeedFast, maxSpeedFast, fastSpeed);

            float sx = (Input.GetKey(moveRight) ? 1 : 0) - (Input.GetKey(moveLeft) ? 1 : 0);
            float sy = (Input.GetKey(moveUp) ? 1 : 0) - (Input.GetKey(moveDown) ? 1 : 0);
            float sz = (Input.GetKey(moveForward) ? 1 : 0) - (Input.GetKey(moveBack) ? 1 : 0);

            Camera.main.transform.position = Camera.main.transform.position + Camera.main.transform.rotation * new Vector3(sx, sy, sz) * speed * Time.deltaTime;

        }

        public Camera GetCamera()
        {

            return Camera.main;

        }

    }

}
