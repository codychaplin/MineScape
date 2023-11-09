using UnityEngine;
using Unity.Mathematics;
using minescape.init;
using minescape.world;
using minescape.block;
using minescape.world.chunk;

namespace minescape.player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public Transform playerBody;
        public Transform selectedBlock;
        public CharacterController characterController;
        public World world;
        public Transform ceilingCheck;

        [Header("Camera")]
        public float defaultFOV = 90f;
        [Range(1f, 1.3f)]
        public float sprintFOVMultiplier = 1.2f;
        public float FOVChangeSharpness = 5f;
        public float mouseSensitivity = 150f;

        [Header("Movement")]
        public bool CreativeMode = false;
        public float creativeSpeed = 50f;
        public float speed = 10f;
        public float jumpHeight = 1.5f;
        public float moveSensitivity = 40f;
        public float sprintSpeedModifier = 2f;
        public float crouchingSharpness = 20f;
        [Range(0, 1)]
        public float CrouchSpeedModifier = 0.5f;

        [Header("Character")]
        public float standingHeight = 1.8f;
        [Range(0.5f, 1.9f)]
        public float crouchingHeight = 1.25f;
        [Range(0f, 1f)]
        public float cameraHeightRatio = 0.95f;

        [Header("Misc")]
        public float reach = 5f;

        //float blockCooldownTimer = 0f;
        //const float blockCooldown = 0.03f;

        float gravity => -9.81f * 3f;

        float targetHeight;
        bool targetHeightReached;
        bool isSprinting = false;
        bool isCrouching = false;
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

        Vector3 VectorHalf = new(0.5f, 0.5f, 0.5f);
        Vector3Int selectedBlockPosition;
        Vector3 defaultPosition = new(-1f, -1f, -1f);

        BlockInView blockInView = new();

        void Start ()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetFOV(defaultFOV);
            targetHeight = standingHeight;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.visible = true;
                Application.Quit();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                CreativeMode = !CreativeMode;
                characterController.excludeLayers = (CreativeMode) ? 64 : 0;
            }

            if (Input.GetKey(KeyCode.Mouse0))
                Cursor.visible = false;

            if (Cursor.visible)
                return;

            MovementInput();
            MoveCamera();
            var chunk = GetBlockInView();
            //HitBlock(chunk);

            if (CreativeMode)
            {
                MoveCreative();
            }
            else
            {
                CollisionCheck();
                Crouch();
                UpdateHeight();
                MovePlayer();
            }
        }

        public void SetFOV(float fov)
        {
            playerCamera.fieldOfView = fov;
        }

        void MovementInput()
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

        void Crouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
                SetCrouchingState(!isCrouching);
        }

        void SetCrouchingState(bool crouched)
        {
            targetHeight = crouched ? crouchingHeight : standingHeight;
            isCrouching = crouched;
            targetHeightReached = false;
        }

        void UpdateHeight()
        {
            if (targetHeightReached)
                return;

            // character controller
            characterController.height = math.lerp(characterController.height, targetHeight, crouchingSharpness * Time.deltaTime);
            characterController.center = 0.5f * characterController.height * Vector3.up;

            // camera
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition,
                cameraHeightRatio * targetHeight * Vector3.up, crouchingSharpness * Time.deltaTime);

            // ceiling check
            ceilingCheck.localPosition = Vector3.Lerp(ceilingCheck.localPosition, targetHeight * Vector3.up, crouchingSharpness * Time.deltaTime);

            // body
            playerBody.localScale = new Vector3(0.5f, math.lerp(playerBody.localScale.y, targetHeight, crouchingSharpness * Time.deltaTime), 0.5f);
            playerBody.localPosition = 0.5f * playerBody.localScale.y * Vector3.up;

            // snap to targetHeight when close
            if (math.abs(playerBody.localScale.y - targetHeight) < 0.01f)
            {
                characterController.height = targetHeight;
                characterController.center = 0.5f * characterController.height * Vector3.up;

                playerBody.localScale = new Vector3(0.5f, targetHeight, 0.5f);
                playerBody.localPosition = 0.5f * playerBody.localScale.y * Vector3.up;
                targetHeightReached = true;
            }
        }

        void CollisionCheck()
        {
            // if head hits ceiling when jumping, set downwards velocity
            if (!characterController.isGrounded)
                if (Physics.CheckSphere(ceilingCheck.position, 0.1f, 1 << 6)) // 6 = ground
                    velocity.y = -1f;

            // reset velocity
            if (characterController.isGrounded && velocity.y < 0)
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

        bool CanSprint(Vector3 move)
        {
            Vector3 forward = transform.InverseTransformDirection(move);
            if (forward.z > 0.1 && (characterController.isGrounded || isSprinting))
                return Input.GetKey(KeyCode.LeftControl);
            else
                return false;
        }

        void MovePlayer()
        {
            // move
            move = transform.right * moveX + transform.forward * moveZ;

            float speedModifier;
            if (CanSprint(move)) // if player is sprinting
            {
                isSprinting = true;
                SetFOV(math.lerp(playerCamera.fieldOfView, defaultFOV * sprintFOVMultiplier, FOVChangeSharpness * Time.deltaTime));
                SetCrouchingState(false); // forces player to stand
                speedModifier = sprintSpeedModifier;
            }
            else
            {
                isSprinting = false;
                SetFOV(Mathf.Lerp(playerCamera.fieldOfView, defaultFOV, FOVChangeSharpness * Time.deltaTime));
                speedModifier = 1f;
            }

            move *= speedModifier;

            if (isCrouching)
                move *= CrouchSpeedModifier;

            characterController.Move((move * speed + velocity) * Time.deltaTime);

            // jump
            if ((Input.GetButtonDown("Jump") || Input.GetButton("Jump")) &&
                characterController.isGrounded &&
                Time.time >= lastTimeJumped + jumpPreventionTime)
            {
                lastTimeJumped = Time.time;
                velocity.y = math.sqrt(jumpHeight * -2f * gravity);
            }

            // apply gravity
            velocity.y += gravity * Time.deltaTime;
        }

        void MoveCreative()
        {
            move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;
            characterController.Move(creativeSpeed * Time.deltaTime * move);
        }

        Chunk GetBlockInView()
        {
            int layerMask = 1 << 6 | 1 << 7; // ground and plants
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hitInfo, reach, layerMask))
            {
                hitInfo.point -= hitInfo.normal * 0.1f; // slightly past point

                // clamp selected block position
                selectedBlockPosition.x = (int)math.floor(hitInfo.point.x);
                selectedBlockPosition.y = (int)math.floor(hitInfo.point.y);
                selectedBlockPosition.z = (int)math.floor(hitInfo.point.z);

                // get chunk where block is located
                var chunk = world.GetChunk(selectedBlockPosition);
                if (chunk == null)
                    return null;

                // get block data
                int localX = selectedBlockPosition.x % Constants.ChunkWidth;
                int localZ = selectedBlockPosition.z % Constants.ChunkWidth;
                byte blockID = chunk.GetBlock(localX, selectedBlockPosition.y, localZ);
                Block block = world.Blocks.blocks[blockID];

                blockInView.Set(hitInfo, localX, localZ, blockID, block);

                // set focused block indicator
                if (block.IsSolid)
                    selectedBlock.position = selectedBlockPosition;

                return chunk;
            }
            else
            {
                // if no block in reach, hide indicator
                selectedBlock.position = defaultPosition;
            }

            return null;
        }

        /*void HitBlock(Chunk chunk)
        {
            if (chunk == null)
                return;

            // break block
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (blockInView.block.IsSolid && blockInView.blockID != BlockIDs.BEDROCK)
                {
                    if (Time.time < blockCooldownTimer)
                        return;

                    // break block
                    chunk.SetBlock(blockInView.x, selectedBlockPosition.y, blockInView.z, BlockIDs.AIR);
                    blockCooldownTimer = Time.time + blockCooldown;

                    // update chunk
                    world.chunkManager.AlterBlock(chunk, blockInView.x, selectedBlockPosition.y, blockInView.z);
                }
            }

            // place block
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (Time.time < blockCooldownTimer)
                    return;

                blockInView.hit.point += blockInView.hit.normal * 0.2f; // slightly before point

                // update position
                selectedBlockPosition.x = (int)math.floor(blockInView.hit.point.x);
                selectedBlockPosition.y = (int)math.floor(blockInView.hit.point.y);
                selectedBlockPosition.z = (int)math.floor(blockInView.hit.point.z);

                // if player collides with where block would be placed, don't place
                if (Physics.CheckBox(selectedBlockPosition + VectorHalf, VectorHalf * 1.1f, Quaternion.identity, 1 << 3)) // 3 == player
                    return;

                // get chunk
                chunk = world.GetChunk(selectedBlockPosition);
                if (chunk == null)
                    return;

                // get local coordinates
                blockInView.x = selectedBlockPosition.x % Constants.ChunkWidth;
                blockInView.z = selectedBlockPosition.z % Constants.ChunkWidth;

                // place block
                chunk.SetBlock(blockInView.x, selectedBlockPosition.y, blockInView.z, BlockIDs.GRASS);
                blockCooldownTimer = Time.time + blockCooldown;

                // update Chunk
                world.chunkManager.AlterBlock(chunk, blockInView.x, selectedBlockPosition.y, blockInView.z);
            }
        }*/
    }

    struct BlockInView
    {
        public RaycastHit hit;
        public int x;
        public int z;
        public byte blockID;
        public Block block;

        public void Set(RaycastHit _hit, int _x, int _z, byte _blockID, Block _block)
        {
            hit = _hit;
            x = _x;
            z = _z;
            blockID = _blockID;
            block = _block;
        }
    }
}