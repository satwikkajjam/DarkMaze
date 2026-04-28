using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Demonic enemy AI with patrol, detect, and chase states.
/// Enemies detect player through vision cone, noise, and flashlight.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Investigate, Chase, Search, Return }

    [Header("Detection")]
    public float viewDistance = 18f;
    public float viewAngle = 110f;
    public float hearingRange = 12f;
    public float flashlightDetectionRange = 25f;
    public float detectionSpeed = 2f;
    public float losePlayerTime = 5f;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 5.5f;
    public float investigateSpeed = 3.5f;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("Patrol")]
    public float patrolRadius = 20f;
    public float waypointReachDistance = 1.5f;
    public float patrolWaitTime = 2f;

    [Header("Visual")]
    public Color normalEyeColor = Color.yellow;
    public Color alertEyeColor = Color.red;
    public Color searchEyeColor = new Color(1f, 0.5f, 0f);

    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform player;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private Vector3 lastKnownPlayerPos;
    private Vector3 homePosition;
    private float detectionLevel;
    private float timeSinceLastSeen;
    private float patrolWaitTimer;
    private float attackTimer;
    private bool playerInSight;
    private Renderer enemyRenderer;
    private MaterialPropertyBlock propBlock;

    public EnemyState CurrentState => currentState;
    public float DetectionLevel => detectionLevel;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = patrolSpeed;
        agent.angularSpeed = 180f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = true;

        homePosition = transform.position;
        enemyRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        FindPlayer();
        SetNewPatrolPoint();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (playerHealth != null && playerHealth.IsDead) return;

        CheckPlayerDetection();
        UpdateState();
        UpdateVisuals();
    }

    void CheckPlayerDetection()
    {
        playerInSight = false;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Vision cone check
        if (distToPlayer <= viewDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 1.5f,
                    dirToPlayer, distToPlayer, LayerMask.GetMask("Wall")))
                {
                    playerInSight = true;
                    float distanceFactor = 1f - (distToPlayer / viewDistance);
                    float crouchFactor = (playerController != null && playerController.IsCrouching) ? 0.3f : 1f;
                    detectionLevel += detectionSpeed * distanceFactor * crouchFactor * Time.deltaTime;
                }
            }
        }

        // Noise detection
        if (playerController != null)
        {
            float noise = playerController.GetNoiseLevel();
            if (noise > 0f && distToPlayer <= hearingRange * noise)
            {
                detectionLevel += noise * detectionSpeed * 0.5f * Time.deltaTime;
                if (detectionLevel > 0.3f && currentState == EnemyState.Patrol)
                {
                    lastKnownPlayerPos = player.position;
                }
            }
        }

        // Flashlight detection
        PlayerFlashlight flashlight = player.GetComponentInChildren<PlayerFlashlight>();
        if (flashlight != null && flashlight.isOn && distToPlayer <= flashlightDetectionRange)
        {
            Vector3 dirToEnemy = (transform.position - player.position).normalized;
            float flashAngle = Vector3.Angle(player.forward, dirToEnemy);
            if (flashAngle < 30f)
            {
                detectionLevel += detectionSpeed * 1.5f * Time.deltaTime;
            }
        }

        detectionLevel = Mathf.Clamp01(detectionLevel);

        if (!playerInSight && currentState != EnemyState.Chase)
        {
            detectionLevel -= 0.3f * Time.deltaTime;
            detectionLevel = Mathf.Max(0f, detectionLevel);
        }

        if (playerInSight)
        {
            lastKnownPlayerPos = player.position;
            timeSinceLastSeen = 0f;
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                if (detectionLevel >= 1f) TransitionTo(EnemyState.Chase);
                else if (detectionLevel >= 0.5f) TransitionTo(EnemyState.Investigate);
                break;

            case EnemyState.Investigate:
                HandleInvestigate();
                if (detectionLevel >= 1f) TransitionTo(EnemyState.Chase);
                else if (detectionLevel <= 0.1f) TransitionTo(EnemyState.Patrol);
                break;

            case EnemyState.Chase:
                HandleChase();
                if (timeSinceLastSeen > losePlayerTime) TransitionTo(EnemyState.Search);
                break;

            case EnemyState.Search:
                HandleSearch();
                if (playerInSight && detectionLevel >= 0.8f) TransitionTo(EnemyState.Chase);
                else if (timeSinceLastSeen > losePlayerTime * 2f) TransitionTo(EnemyState.Return);
                break;

            case EnemyState.Return:
                HandleReturn();
                if (detectionLevel >= 0.5f) TransitionTo(EnemyState.Investigate);
                if (Vector3.Distance(transform.position, homePosition) < 3f) TransitionTo(EnemyState.Patrol);
                break;
        }
    }

    void TransitionTo(EnemyState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case EnemyState.Patrol:
                agent.speed = patrolSpeed;
                SetNewPatrolPoint();
                break;
            case EnemyState.Investigate:
                agent.speed = investigateSpeed;
                agent.SetDestination(lastKnownPlayerPos);
                break;
            case EnemyState.Chase:
                agent.speed = chaseSpeed;
                break;
            case EnemyState.Search:
                agent.speed = investigateSpeed;
                SetSearchPoint();
                break;
            case EnemyState.Return:
                agent.speed = patrolSpeed;
                agent.SetDestination(homePosition);
                break;
        }
    }

    void HandlePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= waypointReachDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                SetNewPatrolPoint();
            }
        }
    }

    void HandleInvestigate()
    {
        agent.SetDestination(lastKnownPlayerPos);

        if (!agent.pathPending && agent.remainingDistance <= waypointReachDistance)
        {
            // Look around behavior
            transform.Rotate(Vector3.up, 120f * Time.deltaTime);
        }
    }

    void HandleChase()
    {
        agent.SetDestination(player.position);

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= attackRange)
        {
            AttackPlayer();
        }
    }

    void HandleSearch()
    {
        if (!agent.pathPending && agent.remainingDistance <= waypointReachDistance)
        {
            SetSearchPoint();
        }
    }

    void HandleReturn()
    {
        if (!agent.pathPending && agent.remainingDistance <= waypointReachDistance)
        {
            detectionLevel = 0f;
        }
    }

    void AttackPlayer()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(100f);
            }
        }
    }

    void SetNewPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += homePosition;
            randomDir.y = homePosition.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    void SetSearchPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * 10f;
            randomDir += lastKnownPlayerPos;
            randomDir.y = lastKnownPlayerPos.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    void UpdateVisuals()
    {
        if (enemyRenderer == null) return;

        Color targetColor;
        switch (currentState)
        {
            case EnemyState.Chase:
                targetColor = alertEyeColor;
                break;
            case EnemyState.Search:
            case EnemyState.Investigate:
                targetColor = searchEyeColor;
                break;
            default:
                targetColor = normalEyeColor;
                break;
        }

        enemyRenderer.GetPropertyBlock(propBlock);
        Color current = propBlock.GetColor("_EmissionColor");
        Color lerped = Color.Lerp(current == default ? normalEyeColor : current, targetColor * 3f, Time.deltaTime * 3f);
        propBlock.SetColor("_EmissionColor", lerped);
        enemyRenderer.SetPropertyBlock(propBlock);
    }
}
