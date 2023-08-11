using UnityEngine;
using Unity.Mathematics;

public class CameraController : MonoBehaviour
{
    public Transform playerBody;

    public float mouseSensitivityX = 150f;
    public float mouseSensitivityY = 150f;

    float xRotation = 0f;
    float yRotation = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = math.clamp(xRotation, -85f, 85f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
