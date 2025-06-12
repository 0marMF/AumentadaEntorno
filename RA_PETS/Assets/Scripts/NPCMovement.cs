using UnityEngine;

// Asegúrate que el namespace sea el correcto para tu proyecto
namespace YourProject.Scripts.NPCs 
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
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
        
        // --- SE ELIMINÓ LA LÓGICA DE SEGUIMIENTO DE AQUÍ ---
        // Ya no necesitamos playerTransform ni stopDistance en este script.
        // NPCFollowPlayer se encargará de eso.

        private CharacterController _characterController;
        private Vector3 _currentMovementDirection = Vector3.zero;
        private Vector3 _verticalVelocity = Vector3.zero;
        private Animator _animator;
        private float _baseMovementSpeed;

        // Propiedad para obtener la velocidad base original
        public float BaseMovementSpeed => _baseMovementSpeed;

        // Propiedad para ajustar la velocidad de movimiento actual desde otros scripts
        public float CurrentMovementSpeed
        {
            get => movementSpeed;
            set => movementSpeed = Mathf.Max(0, value);
        }

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _baseMovementSpeed = movementSpeed;
        }

        void Update()
        {
            // --- LÓGICA DE SEGUIMIENTO ELIMINADA DE UPDATE ---
            // El método Update ahora solo se encarga de aplicar el movimiento,
            // la rotación y la gravedad que otros scripts le ordenan.
            
            ApplyGravity();
            MoveCharacter();
            RotateCharacter();
            UpdateAnimator();
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
            // Usa la velocidad actual (que puede ser modificada por NPCFollowPlayer)
            Vector3 finalVelocity = _currentMovementDirection * movementSpeed; 
            finalVelocity += _verticalVelocity; // Aplicar gravedad
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

        private void UpdateAnimator()
        {
            if (_animator != null)
            {
                // La animación ahora depende de si la dirección de movimiento tiene magnitud
                // y de si la velocidad actual es mayor que un umbral pequeño.
                // Esto asegura que si la velocidad es 0, la animación se detenga.
                bool isWalking = _currentMovementDirection.magnitude > 0.01f && movementSpeed > 0.1f;
                _animator.SetBool("isWalking", isWalking);
            }
        }
    }
}