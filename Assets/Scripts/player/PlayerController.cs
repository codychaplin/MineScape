using UnityEngine;
using Unity.Mathematics;
using minescape.world.chunk;
using static UnityEditor.PlayerSettings;

namespace minescape.player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public Transform playerBody;
        public Transform selectedBlock;
        public CharacterController characterController;
        public ChunkManager chunkManager;
        public LayerMask groundLayer;

        [Header("Movement")]
        public bool CreativeMode = false;
        public float creativeSpeed = 20f;
        public float speed = 10f;
        public float jumpHeight = 1.5f;
        public float moveSensitivity = 40f;
        public float mouseSensitivity = 150f;

        public float reach = 10f;

        float gravity => -9.81f * 3f;

        bool isGrounded = false;
        float lastTimeJumped = 0f;
        const float jumpPreventionTime = 0.2f;

        Vector3 velocity = Vector3.zero;
        Vector3 move = Vector3.zero;
        
        float xRotation = 0f;

        float mouseX = 0;
        float mouseY = 0;

        float moveX = 0;
        float moveZ = 0;
        float moveY = 0;

        Vector3Int selectedBlockPosition;
        Vector3 defaultSelectedBlockPosition = new(-1f, -1f, -1f);

        void Start ()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

            PlayerInput();
            MoveCamera();
            GetBlockInView();

            if (CreativeMode)
            {
                MoveCreative();
            }
            else
            {
                GroundCheck();
                MovePlayer();
            }
        }

        void OnValidate()
        {
            // disable collisions in creative mode
            characterController.excludeLayers = (CreativeMode) ? 64 : 0;
        }

        void PlayerInput()
        {
            // camera
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // movement
            moveX = Input.GetAxis("Horizontal") * moveSensitivity * Time.deltaTime;
            moveZ = Input.GetAxis("Vertical") * moveSensitivity * Time.deltaTime;
            moveY = Input.GetAxis("Jump") * moveSensitivity * Time.deltaTime;
            moveY -= Input.GetAxis("Crouch") * moveSensitivity * Time.deltaTime;
        }

        void GroundCheck()
        {
            // ground check
            if (Time.time >= lastTimeJumped + jumpPreventionTime)
                isGrounded = Physics.CheckSphere(transform.position, 0.3f, groundLayer);

            // reset velocity
            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;
        }

        void MoveCamera()
        {
            // set
            xRotation -= mouseY;
            xRotation = math.clamp(xRotation, -85f, 85f);

            // apply
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        void MovePlayer()
        {
            // move
            move = transform.right * moveX + transform.forward * moveZ;
            characterController.Move(move * speed * Time.deltaTime);

            // jump
            if ((Input.GetButtonDown("Jump") || Input.GetButton("Jump")) && isGrounded)
                velocity.y = math.sqrt(jumpHeight * -2f * gravity);

            // apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        void GetBlockInView()
        {
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hitInfo, reach, groundLayer))
            {
                hitInfo.point -= hitInfo.normal * 0.1f;
                selectedBlockPosition.x = (int)math.floor(hitInfo.point.x);
                selectedBlockPosition.y = (int)math.floor(hitInfo.point.y);
                selectedBlockPosition.z = (int)math.floor(hitInfo.point.z);
                if (chunkManager.CheckBlockAtPos(selectedBlockPosition))
                {
                    selectedBlock.position = selectedBlockPosition;
                }
            }
            else
                selectedBlock.position = defaultSelectedBlockPosition;
        }

        void MoveCreative()
        {
            move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;
            characterController.Move(move * creativeSpeed * Time.deltaTime);
        }
    }
}