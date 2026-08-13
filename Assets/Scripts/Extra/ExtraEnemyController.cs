using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ExtraEnemyController : MonoBehaviour
{
    public enum EnemyType
    {
        Melee,
        Ranged
    }

    [Header("Role")]
    [SerializeField] private EnemyType enemyType = EnemyType.Melee;
    [SerializeField] private Transform target;
    [SerializeField] private string fallbackTargetName = "Player";
    [SerializeField] private Camera worldCamera;
    [SerializeField, Min(0f)] private float boundaryPadding = 0.5f;

    [Header("Melee")]
    [SerializeField, Min(0f)] private float meleeMoveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float meleeStoppingDistance = 1.2f;

    [Header("Ranged Movement")]
    [SerializeField, Min(0f)] private float rangedMoveSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float idleChance = 0.4f;
    [SerializeField] private Vector2 stateDurationRange = new Vector2(0.5f, 1.4f);

    [Header("Ranged Attack")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0f)] private float projectileSpawnOffset = 0.65f;
    [SerializeField, Min(0f)] private float projectileSpeed = 7f;
    [SerializeField, Min(0.05f)] private float fireInterval = 1.2f;

    private Rigidbody2D body;
    private Vector2 rangedMoveDirection;
    private float nextStateChangeTime;
    private float fireCooldownRemaining;
    private float nextTargetSearchTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        TryFindTarget();
        PickNextRangedState();
        fireCooldownRemaining = Random.Range(0.1f, fireInterval);
    }

    private void Update()
    {
        if (target == null && Time.time >= nextTargetSearchTime)
        {
            TryFindTarget();
        }

        if (enemyType != EnemyType.Ranged)
        {
            return;
        }

        if (Time.time >= nextStateChangeTime)
        {
            PickNextRangedState();
        }

        fireCooldownRemaining = Mathf.Max(
            0f,
            fireCooldownRemaining - Time.deltaTime * TestControl.EnemyTimeScale);
        TryFireProjectile();
    }

    private void FixedUpdate()
    {
        Vector2 desiredVelocity;
        if (enemyType == EnemyType.Melee)
        {
            desiredVelocity = GetMeleeVelocity();
        }
        else
        {
            desiredVelocity = rangedMoveDirection * rangedMoveSpeed;
        }

        SetBoundedVelocity(desiredVelocity * TestControl.EnemyTimeScale);
    }

    private Vector2 GetMeleeVelocity()
    {
        if (target == null)
        {
            return Vector2.zero;
        }

        Vector2 toTarget = (Vector2)target.position - body.position;
        return toTarget.sqrMagnitude > meleeStoppingDistance * meleeStoppingDistance
            ? toTarget.normalized * meleeMoveSpeed
            : Vector2.zero;
    }

    private void SetBoundedVelocity(Vector2 desiredVelocity)
    {
        Vector2 clampedPosition = ExtraCameraBounds.Clamp(
            worldCamera,
            body.position,
            boundaryPadding,
            transform.position.z);

        if ((clampedPosition - body.position).sqrMagnitude > 0.000001f)
        {
            body.position = clampedPosition;
        }

        Vector2 desiredNextPosition = clampedPosition + desiredVelocity * Time.fixedDeltaTime;
        Vector2 clampedNextPosition = ExtraCameraBounds.Clamp(
            worldCamera,
            desiredNextPosition,
            boundaryPadding,
            transform.position.z);

        body.linearVelocity = (clampedNextPosition - clampedPosition) / Time.fixedDeltaTime;
    }

    private void PickNextRangedState()
    {
        float minimumDuration = Mathf.Max(0.05f, stateDurationRange.x);
        float maximumDuration = Mathf.Max(minimumDuration, stateDurationRange.y);
        nextStateChangeTime = Time.time + Random.Range(minimumDuration, maximumDuration);

        if (Random.value < idleChance)
        {
            rangedMoveDirection = Vector2.zero;
            return;
        }

        rangedMoveDirection = Random.Range(0, 4) switch
        {
            0 => Vector2.up,
            1 => Vector2.down,
            2 => Vector2.left,
            _ => Vector2.right
        };
    }

    private void TryFireProjectile()
    {
        if (target == null || projectilePrefab == null || fireCooldownRemaining > 0f)
        {
            return;
        }

        Vector2 direction = (Vector2)target.position - body.position;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        direction.Normalize();
        Vector2 spawnPosition = firePoint != null
            ? firePoint.position
            : body.position + direction * projectileSpawnOffset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.Euler(0f, 0f, angle));
        projectile.Launch(direction, projectileSpeed, gameObject);
        fireCooldownRemaining = fireInterval;
    }

    private void TryFindTarget()
    {
        nextTargetSearchTime = Time.time + 1f;

        if (string.IsNullOrWhiteSpace(fallbackTargetName))
        {
            return;
        }

        GameObject targetObject = GameObject.Find(fallbackTargetName);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
    }

    private void OnDisable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void OnValidate()
    {
        stateDurationRange.x = Mathf.Max(0.05f, stateDurationRange.x);
        stateDurationRange.y = Mathf.Max(stateDurationRange.x, stateDurationRange.y);
        meleeStoppingDistance = Mathf.Max(0f, meleeStoppingDistance);
        fireInterval = Mathf.Max(0.05f, fireInterval);
    }
}
