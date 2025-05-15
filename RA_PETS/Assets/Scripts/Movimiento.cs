using UnityEngine;

public class FPSController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public float gravity = -9.81f; // Gravedad realista
    public float jumpHeight = 2f; // Opcional: si quieres salto

    private CharacterController controller;
    private Transform cameraTransform;
    private float xRotation = 0f;
    private Vector3 velocity; // Para controlar gravedad y salto

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Rotación con el mouse ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // --- Movimiento con WASD ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);

        // --- Gravedad ---
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeña fuerza hacia abajo para asegurar contacto con el suelo
        }
        else
        {
            velocity.y += gravity * Time.deltaTime; // Aplica gravedad
        }

        // --- Salto (Opcional) ---
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplica gravedad/salto al CharacterController
        controller.Move(velocity * Time.deltaTime);
    }
}
