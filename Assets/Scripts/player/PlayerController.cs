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
        public LayerMask groundLayer;

        [Header("Camera")]
        public float defaultFOV = 90f;
        [Range(1f, 1.3f)]
        public float sprintFOVMultiplier = 1.1f;
        public float FOVChangeSharpness = 5f;
        public float mouseSensitivity = 150f;

        [Header("Movement")]
        public bool CreativeMode = false;
        public float creativeSpeed = 20f;
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
        public float reach = 10f;

        float blockCooldownTimer = 0f;
        const float blockCooldown = 0.03f;

        float gravity => -9.81f * 3f;

        float targetHeight;
        bool targetHeightReached;
        bool isCrouching = false;
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

            SetFOV(defaultFOV);
            targetHeight = standingHeight;
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
                Crouch();
                UpdateHeight();
                MovePlayer();
            }
        }

        void OnValidate()
        {
            // disable collisions in creative mode
            characterController.excludeLayers = (CreativeMode) ? 64 : 0;
        }

        public void SetFOV(float fov)
        {
            playerCamera.fieldOfView = fov;
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
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchingSharpness * Time.deltaTime);
            characterController.center = 0.5f * characterController.height * Vector3.up;

            // camera
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition,
                cameraHeightRatio * targetHeight * Vector3.up, crouchingSharpness * Time.deltaTime);

            // body
            playerBody.localScale = new Vector3(0.5f, Mathf.Lerp(playerBody.localScale.y, targetHeight, crouchingSharpness * Time.deltaTime), 0.5f);
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
            if (isCrouching)
                move *= CrouchSpeedModifier;
            characterController.Move(move * speed * Time.deltaTime);

            // jump
            if ((Input.GetButtonDown("Jump") || Input.GetButton("Jump")) && isGrounded)
                velocity.y = math.sqrt(jumpHeight * -2f * gravity);

            // apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        void MoveCreative()
        {
            move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;
            characterController.Move(move * creativeSpeed * Time.deltaTime);
        }

        void GetBlockInView()
        {
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hitInfo, reach, groundLayer))
            {
                hitInfo.point -= hitInfo.normal * 0.1f; // slightly past point

                selectedBlockPosition.x = (int)math.floor(hitInfo.point.x);
                selectedBlockPosition.y = (int)math.floor(hitInfo.point.y);
                selectedBlockPosition.z = (int)math.floor(hitInfo.point.z);

                var chunk = world.GetChunk(selectedBlockPosition);
                if (chunk == null)
                    return;

                int localX = selectedBlockPosition.x % Constants.ChunkWidth;
                int localZ = selectedBlockPosition.z % Constants.ChunkWidth;
                byte blockID = chunk.GetBlock(localX, selectedBlockPosition.y, localZ);
                Block block = Blocks.blocks[blockID];
                if (block.IsSolid)
                    selectedBlock.position = selectedBlockPosition;

                // break block
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if (block.IsSolid && block.ID != Blocks.BEDROCK.ID)
                    {
                        if (Time.time < blockCooldownTimer)
                            return;

                        // break block
                        chunk.SetBlock(localX, selectedBlockPosition.y, localZ, Blocks.AIR.ID);
                        blockCooldownTimer = Time.time + blockCooldown;

                        // update chunk
                        chunk.isRendered = false;
                        world.chunkManager.ChunksToCreate.Enqueue(chunk.coord);

                        UpdateAdjacentChunks(chunk, localX, selectedBlockPosition.y, localZ);
                    }
                }

                // place block
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    if (Time.time < blockCooldownTimer)
                        return;

                    hitInfo.point += hitInfo.normal * 0.2f; // slightly before point

                    // update position and get chunk
                    selectedBlockPosition.x = (int)math.floor(hitInfo.point.x);
                    selectedBlockPosition.y = (int)math.floor(hitInfo.point.y);
                    selectedBlockPosition.z = (int)math.floor(hitInfo.point.z);
                    chunk = world.GetChunk(selectedBlockPosition);
                    if (chunk == null)
                        return;

                    // get local coordinates and block info
                    localX = selectedBlockPosition.x % Constants.ChunkWidth;
                    localZ = selectedBlockPosition.z % Constants.ChunkWidth;

                    // place block
                    chunk.SetBlock(localX, selectedBlockPosition.y, localZ, Blocks.DIRT.ID);
                    blockCooldownTimer = Time.time + blockCooldown;

                    // update Chunk
                    chunk.isRendered = false;
                    world.chunkManager.ChunksToCreate.Enqueue(chunk.coord);

                    UpdateAdjacentChunks(chunk, localX, selectedBlockPosition.y, localZ);
                }
            }
            else
            {
                selectedBlock.position = defaultSelectedBlockPosition;
            }
        }

        void UpdateAdjacentChunks(Chunk chunk, int x, int y, int z)
        {
            if (!Chunk.IsBlockInChunk(x, selectedBlockPosition.y, z + 1)) // north
            {
                Chunk north = world.GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z + 1));
                north.isRendered = false;
                world.chunkManager.ChunksToCreate.Enqueue(north.coord);
            }
            if (!Chunk.IsBlockInChunk(x, selectedBlockPosition.y, z - 1)) // south
            {
                Chunk south = world.GetChunk(new ChunkCoord(chunk.coord.x, chunk.coord.z - 1));
                south.isRendered = false;
                world.chunkManager.ChunksToCreate.Enqueue(south.coord);
            }
            if (!Chunk.IsBlockInChunk(x + 1, selectedBlockPosition.y, z)) // east
            {
                Chunk east = world.GetChunk(new ChunkCoord(chunk.coord.x + 1, chunk.coord.z));
                east.isRendered = false;
                world.chunkManager.ChunksToCreate.Enqueue(east.coord);
            }
            if (!Chunk.IsBlockInChunk(x - 1, selectedBlockPosition.y, z)) // west
            {
                Chunk west = world.GetChunk(new ChunkCoord(chunk.coord.x - 1, chunk.coord.z));
                west.isRendered = false;
                world.chunkManager.ChunksToCreate.Enqueue(west.coord);
            }
        }
    }
}