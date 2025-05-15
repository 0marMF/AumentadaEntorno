using UnityEngine;

namespace YourProject.Scripts.NPCs // Asegúrate que el namespace sea el correcto
{
    [RequireComponent(typeof(CharacterController))]
    public class NPCMovement : MonoBehaviour
    {
        [Header("Movement Parameters")]
        [SerializeField]
        [Tooltip("La velocidad de movimiento base del NPC.")]
        [Range(1f, 10f)]
        private float movementSpeed = 3f;

        [SerializeField]
        [Tooltip("La velocidad a la que el NPC rota para encarar su dirección de movimiento.")]
        [Range(1f, 15f)]
        private float rotationSpeed = 7f;

        [Header("Gravity & Grounding")]
        [SerializeField]
        [Tooltip("La fuerza de gravedad aplicada al NPC.")]
        [Range(1f, 20f)]
        private float gravity = 9.81f;

        private CharacterController _characterController;
        private Vector3 _currentMovementDirection = Vector3.zero;
        private Vector3 _verticalVelocity = Vector3.zero;

        private float _baseMovementSpeed; // Para guardar la velocidad original

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _baseMovementSpeed = movementSpeed; // Guardamos la velocidad configurada
        }

        void Update()
        {
            ApplyGravity();
            MoveCharacter();
            RotateCharacter();
        }

        public void SetMovementDirection(Vector3 direction)
        {
            _currentMovementDirection = new Vector3(direction.x, 0, direction.z).normalized;
        }

        public void StopMovement()
        {
            _currentMovementDirection = Vector3.zero;
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded && _verticalVelocity.y < 0)
            {
                _verticalVelocity.y = -2f;
            }
            else
            {
                _verticalVelocity.y -= gravity * Time.deltaTime;
            }
        }

        private void MoveCharacter()
        {
            Vector3 finalVelocity = _currentMovementDirection * movementSpeed; // Usa la 'movementSpeed' actual
            finalVelocity.y = _verticalVelocity.y;
            _characterController.Move(finalVelocity * Time.deltaTime);
        }

        private void RotateCharacter()
        {
            if (_currentMovementDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_currentMovementDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        public bool IsGrounded => _characterController.isGrounded;

        // Propiedad para obtener la velocidad base original
        public float BaseMovementSpeed => _baseMovementSpeed;

        // Propiedad para ajustar la velocidad de movimiento actual desde otros scripts
        public float CurrentMovementSpeed
        {
            get => movementSpeed;
            set => movementSpeed = Mathf.Max(0, value);
        }
    }
}
