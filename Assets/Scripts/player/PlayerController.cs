using UnityEngine;

namespace minescape.player
{
    public class PlayerController : MonoBehaviour
    {
        public float mouseSensitivityX = 200f;
        public float mouseSensitivityY = 200f;

        // Sensitivity values for keyboard movement
        public float moveSensitivityX = 40f;
        public float moveSensitivityY = 60f;
        public float moveSensitivityZ = 40f;

        // The current rotation of the camera
        private float xRotation = 0f;
        private float yRotation = 0f;

        void Start ()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
                Cursor.visible = true;

            if (Input.GetKey(KeyCode.Mouse0))
                Cursor.visible = false;

            if (Cursor.visible)
                return;
            
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
            float moveX = Input.GetAxis("Horizontal") * moveSensitivityX * Time.deltaTime;
            float moveY = Input.GetAxis("Jump") * moveSensitivityY * Time.deltaTime;
            moveY -= Input.GetAxis("Crouch") * moveSensitivityY * Time.deltaTime;
            float moveZ = Input.GetAxis("Vertical") * moveSensitivityZ * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;

            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            transform.Translate(moveX, moveY, moveZ);
        }
    }
}