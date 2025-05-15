using UnityEngine;

namespace YourProject.Scripts.NPCs // Asegúrate que el namespace sea el correcto
{
    [RequireComponent(typeof(NPCMovement))]
    public class NPCFollowPlayer : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("El Transform del jugador que el NPC debe seguir. Si no se asigna, intentará encontrarlo por la etiqueta 'Player'.")]
        public Transform playerTransform;

        [SerializeField]
        [Tooltip("La etiqueta del objeto del jugador, si no se asigna playerTransform manualmente.")]
        private string playerTag = "Player";

        [Header("Following Behaviour")]
        [SerializeField]
        [Tooltip("La distancia ideal que el NPC intentará mantener con el jugador. 0 para intentar alcanzar la posición exacta del jugador.")]
        [Range(0f, 10f)]
        private float targetDistance = 1.5f; // Distancia a la que el NPC intentará estar

        [SerializeField]
        [Tooltip("El radio alrededor del punto objetivo en el cual el NPC comenzará a desacelerar.")]
        [Range(0.5f, 20f)]
        private float slowingRadius = 5f;

        [SerializeField]
        [Tooltip("La velocidad mínima a la que el NPC se moverá cuando esté desacelerando y muy cerca de su objetivo. Puede ser 0 para una parada completa.")]
        [Range(0f, 3f)]
        private float minSpeedWhenClose = 0.2f;

        [SerializeField]
        [Tooltip("Umbral de distancia para considerar que el NPC ha llegado a su punto objetivo. Si es menor que esto, se detendrá.")]
        [Range(0.01f, 1f)]
        private float arrivalThreshold = 0.1f;

        [SerializeField]
        [Tooltip("¿Debe el NPC seguir activamente al jugador?")]
        private bool canFollow = true;

        private NPCMovement _npcMovement;

        void Awake()
        {
            _npcMovement = GetComponent<NPCMovement>();
        }

        void Start()
        {
            if (playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
                else
                {
                    Debug.LogError($"NPCFollowPlayer en {gameObject.name}: No se asignó jugador y no se encontró con la etiqueta '{playerTag}'. Seguimiento desactivado.");
                    canFollow = false;
                }
            }

            if (slowingRadius <= targetDistance && targetDistance > 0) // O slowingRadius <= arrivalThreshold
            {
                Debug.LogWarning($"NPCFollowPlayer en {gameObject.name}: 'Slowing Radius' debería ser mayor que 'Target Distance' (o 'Arrival Threshold' si Target Distance es 0) para un comportamiento de desaceleración adecuado.");
            }
        }

        void Update()
        {
            if (!canFollow || playerTransform == null || _npcMovement == null)
            {
                if (_npcMovement != null) _npcMovement.StopMovement();
                return;
            }

            Vector3 npcPositionHorizontal = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 playerPositionHorizontal = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);

            Vector3 directionToPlayer = playerPositionHorizontal - npcPositionHorizontal;
            float distanceToPlayer = directionToPlayer.magnitude;

            Vector3 targetPoint;

            if (distanceToPlayer < 0.01f && targetDistance < 0.01f) // Prácticamente encima y queremos estar encima
            {
                _npcMovement.StopMovement();
                _npcMovement.CurrentMovementSpeed = 0f;
                return;
            }

            if (targetDistance > 0.01f)
            {
                // El NPC intenta mantener una 'targetDistance' del jugador.
                // El punto objetivo está 'targetDistance' detrás del jugador, en la línea NPC-Jugador.
                if (distanceToPlayer > targetDistance) // Si está más lejos de la distancia objetivo, acercarse
                {
                    targetPoint = playerPositionHorizontal - directionToPlayer.normalized * targetDistance;
                }
                else // Si está más cerca de la distancia objetivo (o sobre ella), alejarse sutilmente o quedarse quieto
                {
                    // Si estamos dentro de la targetDistance, el punto objetivo es donde estamos
                    // para evitar que retroceda bruscamente si el jugador se acerca demasiado rápido.
                    // O, si queremos que siempre intente mantener la distancia, podría ser:
                     targetPoint = playerPositionHorizontal - directionToPlayer.normalized * targetDistance;
                    // Esto puede causar que el NPC retroceda si el jugador se mueve hacia él.
                    // Por simplicidad, si está más cerca que targetDistance, y no DEMASIADO cerca,
                    // podría simplemente quedarse quieto o moverse muy lento hacia el punto de offset.
                    // Si distanceToPlayer < targetDistance, pero > arrivalThreshold, podría intentar ajustarse lentamente.
                    // Vamos a mantener la lógica de que siempre intente llegar al 'targetPoint' definido por el offset.
                    targetPoint = playerPositionHorizontal - directionToPlayer.normalized * targetDistance;
                }
            }
            else
            {
                // targetDistance es 0, el NPC intenta alcanzar la posición exacta del jugador.
                targetPoint = playerPositionHorizontal;
            }

            Vector3 directionToTargetPoint = targetPoint - npcPositionHorizontal;
            float distanceToTargetPoint = directionToTargetPoint.magnitude;

            if (distanceToTargetPoint < arrivalThreshold)
            {
                _npcMovement.StopMovement();
                // Podríamos fijar la velocidad a minSpeedWhenClose si es > 0 y queremos que siga "nervioso"
                // Pero para una llegada clara, detenerse es mejor.
                _npcMovement.CurrentMovementSpeed = 0f; 
            }
            else
            {
                _npcMovement.SetMovementDirection(directionToTargetPoint); // Normalizado dentro de SetMovementDirection

                float desiredSpeed;
                if (distanceToTargetPoint < slowingRadius)
                {
                    // Interpolar velocidad desde la base hasta minSpeedWhenClose
                    // t = 0 cuando distanceToTargetPoint es arrivalThreshold (o 0) -> speed = minSpeedWhenClose
                    // t = 1 cuando distanceToTargetPoint es slowingRadius -> speed = _npcMovement.BaseMovementSpeed
                    float effectiveMinDist = (targetDistance > 0 && arrivalThreshold < targetDistance) ? arrivalThreshold : 0.01f; // Para evitar que t sea negativo si targetDistance es el punto de minSpeed
                    
                    float range = slowingRadius - effectiveMinDist; // Rango para la interpolación
                    if (range <= 0.01f) // Evitar división por cero o rango inválido
                    {
                        desiredSpeed = _npcMovement.BaseMovementSpeed;
                    }
                    else
                    {
                        float t = Mathf.Clamp01((distanceToTargetPoint - effectiveMinDist) / range);
                        desiredSpeed = Mathf.Lerp(minSpeedWhenClose, _npcMovement.BaseMovementSpeed, t);
                    }
                }
                else
                {
                    desiredSpeed = _npcMovement.BaseMovementSpeed;
                }
                _npcMovement.CurrentMovementSpeed = desiredSpeed;
            }
        }

        public void SetCanFollow(bool follow)
        {
            canFollow = follow;
            if (!canFollow && _npcMovement != null)
            {
                _npcMovement.StopMovement();
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 npcPos = transform.position;

            Gizmos.color = Color.red; // Arrival Threshold (NPC se detiene si su objetivo está dentro de este radio de sí mismo)
            Gizmos.DrawWireSphere(npcPos, arrivalThreshold);

            if (playerTransform != null)
            {
                Vector3 playerPos = playerTransform.position;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(npcPos, playerPos);

                if (targetDistance > 0.01f)
                {
                    Gizmos.color = Color.green; // Círculo de Target Distance alrededor del jugador
                    Gizmos.DrawWireSphere(playerPos, targetDistance);
                }

                // Visualizar el Slowing Radius alrededor del jugador si targetDistance es 0,
                // o como un indicador general si targetDistance > 0.
                Gizmos.color = Color.cyan;
                // El slowingRadius es conceptualmente alrededor del punto objetivo.
                // Si targetDistance es 0, el punto objetivo es el jugador.
                if (targetDistance < 0.01f)
                {
                     Gizmos.DrawWireSphere(playerPos, slowingRadius);
                }
                else // Si hay un targetDistance, el slowingRadius es relativo al punto de offset.
                {
                    // Podríamos dibujar el slowingRadius alrededor del NPC como una indicación
                    // de "si mi objetivo entra aquí, empiezo a frenar".
                    // Gizmos.DrawWireSphere(npcPos, slowingRadius);
                    // O mejor, indicar que es un valor, y en el tooltip explicarlo.
                    // Por ahora, si hay targetDistance, no dibujaremos el slowingRadius explícitamente
                    // para no confundir, ya que el punto objetivo es dinámico.
                    // El tooltip de slowingRadius debe ser claro.
                }

                // Gizmo para el targetPoint calculado (para depuración)
                // Vector3 npcPositionHorizontal = new Vector3(transform.position.x, 0, transform.position.z);
                // Vector3 playerPositionHorizontal = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
                // Vector3 calculatedTargetPoint;
                // if (targetDistance > 0.01f) {
                //     calculatedTargetPoint = playerPositionHorizontal - (playerPositionHorizontal - npcPositionHorizontal).normalized * targetDistance;
                // } else {
                //     calculatedTargetPoint = playerPositionHorizontal;
                // }
                // Gizmos.color = Color.magenta;
                // Gizmos.DrawSphere(calculatedTargetPoint, 0.3f);
            }
        }
    }
}