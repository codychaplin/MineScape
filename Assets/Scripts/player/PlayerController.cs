using Unity.Mathematics;
using UnityEngine;

namespace minescape.player
{
    public class PlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public Transform playerBody;
        public CharacterController characterController;
        public LayerMask groundLayer;

        public float mouseSensitivityX = 150f;
        public float mouseSensitivityY = 150f;

        float xRotation = 0f;

        public float moveSensitivityX = 40f;
        public float moveSensitivityY = 40f;
        public float moveSensitivityZ = 40f;

        public float speed = 10f;
        public float jumpHeight = 1.5f;
        float gravity => -9.81f * 3f;

        bool isGrounded = false;
        float lastTimeJumped = 0f;
        const float jumpPreventionTime = 0.2f;

        Vector3 velocity = Vector3.zero;

        void Start ()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Update is called once per frame
        void Update()
        {
            PlayerInput();

            if (Cursor.visible)
                return;

            GroundCheck();
            MoveCamera();
            MovePlayer();
        }

        void PlayerInput()
        {
            if (Input.GetKey(KeyCode.Escape))
                Cursor.visible = true;

            if (Input.GetKey(KeyCode.Mouse0))
                Cursor.visible = false;
        }

        void GroundCheck()
        {
            if (Time.time >= lastTimeJumped + jumpPreventionTime)
                isGrounded = Physics.CheckSphere(transform.position, 0.3f, groundLayer);

            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;
        }

        void MoveCamera()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

            xRotation -= mouseY;

            xRotation = math.clamp(xRotation, -85f, 85f);

            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        void MovePlayer()
        {
            float moveX = Input.GetAxis("Horizontal") * moveSensitivityX * Time.deltaTime;
            float moveZ = Input.GetAxis("Vertical") * moveSensitivityZ * Time.deltaTime;

            float moveY = Input.GetAxis("Jump") * moveSensitivityY * Time.deltaTime;
            moveY -= Input.GetAxis("Crouch") * moveSensitivityY * Time.deltaTime;

            // move
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            characterController.Move(move * speed * Time.deltaTime);

            // jump
            if ((Input.GetButtonDown("Jump") || Input.GetButton("Jump")) && isGrounded)
                velocity.y = math.sqrt(jumpHeight * -2f * gravity);

            // apply gravity
            velocity.y += gravity * Time.deltaTime;

            characterController.Move(velocity * Time.deltaTime);
        }
    }
}