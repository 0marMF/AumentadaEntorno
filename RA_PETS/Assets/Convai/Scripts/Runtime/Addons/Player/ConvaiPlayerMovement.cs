using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Convai.Scripts.Runtime.Addons
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Player Movement")]
    [HelpURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview")]
    public class ConvaiPlayerMovement : MonoBehaviour
    {
        [Header("Movement Parameters")]
        [SerializeField] [Tooltip("The speed at which the player walks.")] [Range(1, 10)]
        private float walkingSpeed = 3f;

        [SerializeField] [Tooltip("The speed at which the player runs.")] [Range(1, 10)]
        private float runningSpeed = 8f;

        [SerializeField] [Tooltip("The speed at which the player jumps.")] [Range(1, 10)]
        private float jumpSpeed = 4f;

        [Header("Gravity & Grounding")]
        [SerializeField] [Tooltip("The gravity applied to the player.")] [Range(1, 10)]
        private float gravity = 9.8f;

        [Header("Camera Parameters")]
        [SerializeField] [Tooltip("The main camera the player uses.")]
        private Camera playerCamera;

        [SerializeField] [Tooltip("Speed at which the player can look around.")] [Range(0, 1)]
        private float lookSpeedMultiplier = 0.5f;

        [SerializeField] [Tooltip("Limit of upwards and downwards look angles.")] [Range(1, 90)]
        private float lookXLimit = 45.0f;

        [Header("Head Bobbing")]
        [SerializeField] [Tooltip("Amount of camera movement when walking")]
        private float walkBobAmount = 0.05f;

        [SerializeField] [Tooltip("Speed of camera movement when walking")]
        private float walkBobSpeed = 10f;

        [SerializeField] [Tooltip("Amount of camera movement when running")]
        private float runBobAmount = 0.1f;

        [SerializeField] [Tooltip("Speed of camera movement when running")]
        private float runBobSpeed = 14f;

        [Header("Footstep Sounds")]
        [SerializeField] [Tooltip("Sound played when walking")]
        private AudioClip footstepSound;

        [SerializeField] [Tooltip("Walking step interval")]
        private float walkStepInterval = 0.5f;

        [SerializeField] [Tooltip("Running step interval")]
        private float runStepInterval = 0.3f;

        [SerializeField] [Tooltip("Fade in duration for footsteps (seconds)")]
        private float footstepFadeIn = 0.3f;

        [SerializeField] [Tooltip("Fade out duration for footsteps (seconds)")]
        private float footstepFadeOut = 0.2f;

        [SerializeField] [Tooltip("Maximum volume for footsteps")] [Range(0, 1)]
        private float footstepMaxVolume = 0.7f;

        private CharacterController _characterController;
        private Vector3 _moveDirection = Vector3.zero;
        private float _rotationX;
        private float _defaultCameraY;
        private float _bobTimer;
        private AudioSource _audioSource;
        private bool _isMoving;
        private bool _wasMoving;
        private float _currentFadeTime;
        private float _targetVolume;
        private float _fadeDuration;
        private bool _isFading;

        // ✅ Singleton Instance
        public static ConvaiPlayerMovement Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            // ✅ Auto-asignar la cámara si no está asignada
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                    Debug.LogWarning("No camera assigned to ConvaiPlayerMovement and no child camera found.");
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
                _audioSource.loop = true;
                _audioSource.volume = 0f;
            }

            if (footstepSound != null)
            {
                _audioSource.clip = footstepSound;
            }
        }

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            if (playerCamera != null)
                _defaultCameraY = playerCamera.transform.localPosition.y;
        }

        private void Update()
        {
            MovePlayer();
            RotatePlayerAndCamera();
            ApplyHeadBobbing();
            UpdateMovementState();
            HandleFootsteps();
            UpdateFade();
        }

        private void UpdateFade()
        {
            if (_isFading)
            {
                _currentFadeTime += Time.deltaTime;
                float progress = Mathf.Clamp01(_currentFadeTime / _fadeDuration);
                _audioSource.volume = Mathf.Lerp(_audioSource.volume, _targetVolume, progress);

                if (_currentFadeTime >= _fadeDuration)
                {
                    _isFading = false;
                    
                    // Si el fade out terminó y el volumen es 0, detener el sonido
                    if (_targetVolume <= 0f)
                    {
                        _audioSource.Stop();
                    }
                }
            }
        }

        private void UpdateMovementState()
        {
            _isMoving = _characterController.isGrounded && 
                       ConvaiInputManager.Instance.moveVector.magnitude > 0.1f;
        }

        private void HandleFootsteps()
        {
            // Si acaba de empezar a moverse
            if (_isMoving && !_wasMoving)
            {
                StartFootsteps();
            }
            // Si acaba de detenerse
            else if (!_isMoving && _wasMoving)
            {
                StopFootsteps();
            }

            // Ajustamos el pitch según si está corriendo o caminando
            if (_isMoving)
            {
                AdjustFootstepSpeed();
            }

            _wasMoving = _isMoving;
        }

        private void StartFootsteps()
        {
            if (_audioSource != null && footstepSound != null)
            {
                if (!_audioSource.isPlaying)
                {
                    _audioSource.Play();
                    _audioSource.volume = 0f; // Empieza con volumen 0
                }
                
                // Configurar fade in
                _targetVolume = footstepMaxVolume;
                _fadeDuration = footstepFadeIn;
                _currentFadeTime = 0f;
                _isFading = true;
                
                AdjustFootstepSpeed();
            }
        }

        private void StopFootsteps()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                // Configurar fade out
                _targetVolume = 0f;
                _fadeDuration = footstepFadeOut;
                _currentFadeTime = 0f;
                _isFading = true;
            }
        }

        private void AdjustFootstepSpeed()
        {
            if (ConvaiInputManager.Instance.isRunning)
            {
                _audioSource.pitch = walkStepInterval / runStepInterval;
            }
            else
            {
                _audioSource.pitch = 1f;
            }
        }

        private void OnEnable()
        {
            ConvaiInputManager.Instance.jumping += Jump;
        }

        private void OnDisable()
        {
            ConvaiInputManager.Instance.jumping -= Jump;
        }

        private void MovePlayer()
        {
            Vector3 horizontalMovement = Vector3.zero;

            if (!EventSystem.current.IsPointerOverGameObject() && !UIUtilities.IsAnyInputFieldFocused())
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                Vector3 right = transform.TransformDirection(Vector3.right);

                float speed = ConvaiInputManager.Instance.isRunning ? runningSpeed : walkingSpeed;

                Vector2 moveVector = ConvaiInputManager.Instance.moveVector;
                float curSpeedX = speed * moveVector.x;
                float curSpeedY = speed * moveVector.y;

                horizontalMovement = forward * curSpeedY + right * curSpeedX;
            }

            if (!_characterController.isGrounded)
                _moveDirection.y -= gravity * Time.deltaTime;

            _characterController.Move((_moveDirection + horizontalMovement) * Time.deltaTime);
        }

        private void Jump()
        {
            if (_characterController.isGrounded && !UIUtilities.IsAnyInputFieldFocused())
                _moveDirection.y = jumpSpeed;
        }

        private void RotatePlayerAndCamera()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _rotationX -= ConvaiInputManager.Instance.lookVector.y * lookSpeedMultiplier;
            _rotationX = Mathf.Clamp(_rotationX, -lookXLimit, lookXLimit);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0, 0);

            float rotationY = ConvaiInputManager.Instance.lookVector.x * lookSpeedMultiplier;
            transform.rotation *= Quaternion.Euler(0, rotationY, 0);
        }

        private void ApplyHeadBobbing()
        {
            Vector2 input = ConvaiInputManager.Instance.moveVector;

            if (_characterController.isGrounded && input.magnitude > 0.1f && playerCamera != null)
            {
                float bobAmount = ConvaiInputManager.Instance.isRunning ? runBobAmount : walkBobAmount;
                float bobSpeed = ConvaiInputManager.Instance.isRunning ? runBobSpeed : walkBobSpeed;

                _bobTimer += Time.deltaTime * bobSpeed;
                float newYPos = _defaultCameraY + Mathf.Sin(_bobTimer) * bobAmount;

                Vector3 cameraPos = playerCamera.transform.localPosition;
                cameraPos.y = newYPos;
                playerCamera.transform.localPosition = cameraPos;
            }
            else if (playerCamera != null)
            {
                _bobTimer = 0f;
                Vector3 cameraPos = playerCamera.transform.localPosition;
                cameraPos.y = Mathf.Lerp(cameraPos.y, _defaultCameraY, Time.deltaTime * 10f);
                playerCamera.transform.localPosition = cameraPos;
            }
        }
    }
}