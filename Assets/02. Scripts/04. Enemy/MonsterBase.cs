using UnityEngine;

// Rigidbody2D 컴포넌트가 오브젝트에 없을 경우 자동으로 추가합니다.
[RequireComponent(typeof(Rigidbody2D))]
public class MonsterBase : MonoBehaviour
{
    // 몬스터의 현재 행동 상태를 정의하는 열거형
    public enum State { Idle, Patrol, Chase }

    [Header("몬스터 스텟 데이터")]
    [SerializeField] protected MonsterData monsterData; // 체력, 이동속도, 방어력 등이 저장된 ScriptableObject

    [Header("레이어 설정")]
    [SerializeField] protected LayerMask groundLayer;  // 바닥 및 벽 감지 전용 레이어
    [SerializeField] protected LayerMask playerLayer;  // 플레이어 인지 전용 레이어

    [Header("감지 및 유턴 설정")]
    [SerializeField] protected bool canDetectPlayer = true; // true: 플레이어 감지 및 추적, false: 플레이어를 감지하지 않음
    [SerializeField] protected Vector2 detectionCapsuleSize = new Vector2(10f, 5f); // 플레이어 감지 캡슐 범위 (가로 너비, 세로 높이)
    [SerializeField] protected CapsuleDirection2D detectionDirection = CapsuleDirection2D.Horizontal; // 캡슐 정렬 방향 (Horizontal: 가로, Vertical: 세로)
    [SerializeField] protected float uTurnDelay = 1.5f;     // 플레이어가 등 뒤로 넘어갔을 때 유턴까지의 대기 시간(초)

    [Header("지형 체크 (낭떠러지 & 벽)")]
    [SerializeField] protected Transform ledgeCheckPoint;  // 발 앞부분 바닥 감지 위치 (자식 빈 오브젝트)
    [SerializeField] protected float ledgeCheckDistance = 1f; // 바닥 감지 레이저(Ray) 길이
    [SerializeField] protected float wallCheckDistance = 0.5f; // 전방 벽 감지 레이저(Ray) 길이

    [Header("패트롤 제어")]
    [SerializeField] protected float minIdleTime = 1.5f;   // 제자리 대기 최소 시간
    [SerializeField] protected float maxIdleTime = 3.5f;   // 제자리 대기 최대 시간
    [SerializeField] protected float minPatrolTime = 2f;    // 패트롤 이동 최소 시간
    [SerializeField] protected float maxPatrolTime = 5f;    // 패트롤 이동 최대 시간

    // 내부 시스템 관리 변수
    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer spriteRenderer;               // [추가] 스프라이트 렌더러 참조
    protected Transform playerTransform;                  // 감지된 플레이어의 Transform 참조
    protected State currentState = State.Patrol;           // 몬스터의 초기 상태

    protected float currentHp;                             // 몬스터의 현재 체력
    protected int facingDirection = 1;                     // 바라보는 X축 방향 (1: 오른쪽, -1: 왼쪽)

    private float stateTimer;                              // Idle / Patrol 상태 전환용 타이머
    private float uTurnTimer;                              // 등 뒤의 플레이어를 추적하기 위한 유턴 지연 타이머
    private bool isWaitingInPatrol;                        // 패트롤 중 멈춰 서 있는 상태인지 여부

    protected virtual void Awake()
    {
        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // SpriteRenderer 캐싱 (본인에 없으면 자식 오브젝트에서 찾음)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    protected virtual void Start()
    {
        if (monsterData != null)
        {
            currentHp = monsterData.maxHp;
        }

        // 게임 시작 시 초기 방향에 맞춰 flipX 및 감지 포인트 세팅
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingDirection > 0);
        }

        if (ledgeCheckPoint != null)
        {
            Vector3 localPos = ledgeCheckPoint.localPosition;
            localPos.x = Mathf.Abs(localPos.x) * facingDirection;
            ledgeCheckPoint.localPosition = localPos;
        }

        SetRandomPatrolState();
    }

    protected virtual void Update()
    {
        // 매 프레임 플레이어 감지 시도
        DetectPlayer();

        // 현재 상태에 맞게 타이머 차감 및 논리적 상태 계산
        switch (currentState)
        {
            case State.Idle:
            case State.Patrol:
                HandlePatrolLogic();
                break;

            case State.Chase:
                HandleChaseLogic();
                break;
        }
    }

    protected virtual void FixedUpdate()
    {
        // 물리 연산 기반의 실제 이동 처리는 FixedUpdate에서 실행
        switch (currentState)
        {
            case State.Patrol:
                // 대기 중이 아니면 지정된 방향으로 이동, 대기 중이면 정지
                if (!isWaitingInPatrol)
                    Move(facingDirection * monsterData.moveSpeed);
                else
                    Move(0);
                break;

            case State.Chase:
                // 추적 상태에서는 멈춤 없이 플레이어를 향해 X축으로 계속 이동
                Move(facingDirection * monsterData.moveSpeed);
                break;

            case State.Idle:
                // 제자리 정지
                Move(0);
                break;
        }
    }

    #region 이동 및 방향 처리
    protected virtual void Move(float speed)
    {
        // 기존 Y축 속도(중력)는 유지하면서 X축 이동 속도만 변경
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        // 이동 속도에 따라 애니메이터의 int 변수 "Move" 값 할당 (이동 중: 1, 정지 중: 0)
        if (anim != null)
        {
            int moveState = Mathf.Abs(speed) > 0.01f ? 1 : 0;
            anim.SetInteger("Move", moveState);
        }
    }

    protected virtual void Flip()
    {
        // 1. 바라보는 논리 방향 반전 (1: 오른쪽, -1: 왼쪽)
        facingDirection *= -1;

        // 2. 원본이 '왼쪽'을 바라보는 에셋 기준 반전
        if (spriteRenderer != null)
        {
            // facingDirection이 1(오른쪽)일 때 flipX를 true로 만들어 뒤집음!
            spriteRenderer.flipX = (facingDirection > 0);
        }

        // 3. 자식 오브젝트(LedgeCheckPoint) 위치 동기화
        if (ledgeCheckPoint != null)
        {
            Vector3 localPos = ledgeCheckPoint.localPosition;
            localPos.x = Mathf.Abs(localPos.x) * facingDirection;
            ledgeCheckPoint.localPosition = localPos;
        }
    }

    protected virtual bool IsLedgeOrWallAhead()
    {
        if (ledgeCheckPoint == null) return false;

        // 1. 발 앞 체크 포인트에서 아래쪽 방향으로 바닥(groundLayer) 탐지
        RaycastHit2D groundHit = Physics2D.Raycast(ledgeCheckPoint.position, Vector2.down, ledgeCheckDistance, groundLayer);

        // 2. 몬스터 중앙에서 바라보는 진행 방향으로 벽(groundLayer) 탐지
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, wallCheckDistance, groundLayer);

        // 발밑 바닥이 없거나(null) 전방에 벽이 감지(not null)되었으면 true 반환
        return groundHit.collider == null || wallHit.collider != null;
    }
    #endregion

    #region 상태별 로직 (패트롤 & 추적)
    protected virtual void HandlePatrolLogic()
    {
        stateTimer -= Time.deltaTime;

        if (!isWaitingInPatrol)
        {
            if (IsLedgeOrWallAhead())
            {
                Flip();
            }

            if (stateTimer <= 0)
            {
                isWaitingInPatrol = true;
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
                currentState = State.Idle;
            }
        }
        else
        {
            if (stateTimer <= 0)
            {
                if (Random.value > 0.5f) Flip();

                isWaitingInPatrol = false;
                stateTimer = Random.Range(minPatrolTime, maxPatrolTime);
                currentState = State.Patrol;
            }
        }
    }

    protected virtual void HandleChaseLogic()
    {
        if (playerTransform == null) return;

        // 추적 중 전방에 낭떠러지나 벽이 있으면 멈추거나 패트롤 상태로 복귀
        if (IsLedgeOrWallAhead())
        {
            // 옵션 A: 낭떠러지를 만나면 즉시 정지하고 추적 포기 (패트롤로 전환)
            currentState = State.Idle;
            stateTimer = Random.Range(minIdleTime, maxIdleTime);
            isWaitingInPatrol = true;
            return;
        }

        float xDifference = playerTransform.position.x - transform.position.x;
        bool isPlayerBehind = (xDifference > 0 && facingDirection < 0) || (xDifference < 0 && facingDirection > 0);

        if (isPlayerBehind)
        {
            uTurnTimer += Time.deltaTime;

            if (uTurnTimer >= uTurnDelay)
            {
                Flip();
                uTurnTimer = 0f;
            }
        }
        else
        {
            uTurnTimer = 0f;
        }
    }

    protected virtual void DetectPlayer()
    {
        if (!canDetectPlayer || currentState == State.Chase) return;

        Collider2D playerCollider = Physics2D.OverlapCapsule(transform.position, detectionCapsuleSize, detectionDirection, 0f, playerLayer);

        if (playerCollider != null)
        {
            playerTransform = playerCollider.transform;
            currentState = State.Chase;
            uTurnTimer = 0f;
        }
    }

    private void SetRandomPatrolState()
    {
        isWaitingInPatrol = Random.value > 0.5f;
        stateTimer = isWaitingInPatrol ? Random.Range(minIdleTime, maxIdleTime) : Random.Range(minPatrolTime, maxPatrolTime);
        currentState = isWaitingInPatrol ? State.Idle : State.Patrol;
    }
    #endregion

    #region 데미지 및 사망
    public virtual void TakeDamage(float damageAmount, ElementType attackElement = ElementType.None)
    {
        // 1. 회피 판정 (0 ~ 100 무작위 값이 회피력보다 작으면 회피 성공)
        float evasionRate = (monsterData != null) ? monsterData.evasion : 0f;
        if (evasionRate > 0f && Random.Range(0f, 100f) < evasionRate)
        {
            Debug.Log($"{gameObject.name}이(가) 공격을 회피했습니다!");
            // TODO: 나중에 회피 텍스트(Dodge)나 핏빛/연기 이펙트 띄우는 위치
            return;
        }

        // 2. 속성 상성 계산
        ElementType monsterElement = (monsterData != null) ? monsterData.elementType : ElementType.None;
        float elementMultiplier = ElementCalculator.GetMultiplier(attackElement, monsterElement);
        float calculatedDamage = damageAmount * elementMultiplier;

        // 3. 방어력 차감 (최소 1 데미지 보장)
        float monsterDefense = (monsterData != null) ? monsterData.defense : 0f;
        float finalDamage = Mathf.Max(calculatedDamage - monsterDefense, 1f);

        // 4. 데미지 적용 및 사망 처리
        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
    #endregion

    protected virtual void OnDrawGizmosSelected()
    {
        if (canDetectPlayer)
        {
            Gizmos.color = Color.red;
            DrawWireCapsule(transform.position, detectionCapsuleSize, detectionDirection);
        }

        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ledgeCheckPoint.position, Vector2.down * ledgeCheckDistance);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector2.right * facingDirection * wallCheckDistance);
    }

    private void DrawWireCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction)
    {
        float x = size.x / 2f;
        float y = size.y / 2f;
        Vector3 lastPoint = Vector3.zero;
        Vector3 thisPoint = Vector3.zero;

        for (int i = 0; i <= 36; i++)
        {
            float theta = i * 10f * Mathf.Deg2Rad;

            if (direction == CapsuleDirection2D.Horizontal)
            {
                float radius = y;
                float offset = Mathf.Max(0, x - y);

                if (theta > Mathf.PI / 2f && theta < 3f * Mathf.PI / 2f)
                    thisPoint = new Vector3(Mathf.Cos(theta) * radius - offset, Mathf.Sin(theta) * radius, 0);
                else
                    thisPoint = new Vector3(Mathf.Cos(theta) * radius + offset, Mathf.Sin(theta) * radius, 0);
            }
            else
            {
                float radius = x;
                float offset = Mathf.Max(0, y - x);

                if (theta > 0 && theta < Mathf.PI)
                    thisPoint = new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius + offset, 0);
                else
                    thisPoint = new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius - offset, 0);
            }

            if (i > 0)
            {
                Gizmos.DrawLine(position + lastPoint, position + thisPoint);
            }
            lastPoint = thisPoint;
        }
    }
}