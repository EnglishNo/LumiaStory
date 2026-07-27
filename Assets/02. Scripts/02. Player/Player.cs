using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    [Header("Current Status")]
    public float currentHp;

    // PlayerStatManager로부터 전달받은 전체 스탯 저장
    private PlayerStats myStats;

    [Header("넉백 및 무적 설정")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(6f, 4f); // X: 좌우 밀려나는 힘, Y: 위로 튀어오르는 힘
    [SerializeField] private float knockbackDuration = 0.25f;              // 넉백 조작 불능 시간
    [SerializeField] private float invincibilityDuration = 1.0f;            // 피격 후 무적 지속 시간

    // 외부(PlayerController)에서 이동 조작 차단 여부를 확인할 수 있는 프로퍼티
    public bool IsKnockback { get; private set; } = false;
    private bool isInvincible = false; // 무적 상태 여부

    // 컴포넌트 참조
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerStatManager statManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        statManager = GetComponent<PlayerStatManager>();
    }

    /// <summary>
    /// StatManager로부터 전체 스탯 패키지를 전송받는 함수
    /// </summary>
    public void ReceiveStats(PlayerStats stats)
    {
        myStats = stats;
        currentHp = myStats.maxHp; // 게임 시작 시 최대 체력으로 설정

        Debug.Log($"플레이어 스탯 세팅 완료! [최대 체력: {myStats.maxHp}, 이동속도: {myStats.moveSpeed}, 점프력: {myStats.jumpPower}]");
    }

    /// <summary>
    /// 외부(적, 함정 등)에서 플레이어를 공격할 때 호출하는 피격 함수
    /// </summary>
    /// <param name="rawDamage">공격자의 기본 데미지</param>
    /// <param name="attackerPos">공격자의 위치 (넉백 방향 계산용)</param>
    /// <param name="dmgType">물리/마법 데미지 타입</param>
    /// <param name="element">공격의 속성 타입</param>
    public void TakeDamage(float rawDamage, Vector2 attackerPos, DamageType dmgType = DamageType.Physical, ElementType element = ElementType.None)
    {
        // 1. 무적 상태면 피격 판정 무시
        if (isInvincible) return;

        // 2. 회피력(Evasion) 확률 계산
        if (Random.Range(0f, 100f) < myStats.evasionRate)
        {
            Debug.Log("회피 성공! (MISS)");
            return;
        }

        // 3. StatManager를 통해 방어력 및 속성 저항이 적용된 최종 데미지 계산
        float finalDamage = statManager.CalculateTakenDamage(rawDamage, dmgType, element);

        // 4. 체력 차감
        currentHp -= finalDamage;
        Debug.Log($"피격 당함! 받은 데미지: {finalDamage}, 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            // 5. 피격 연출 (넉백 + 회색 깜빡임 무적) 실행
            StartCoroutine(HitRoutine(attackerPos));
        }
    }

    /// <summary>
    /// 피격 시 넉백 및 무적/깜빡임 처리 코루틴
    /// </summary>
    private IEnumerator HitRoutine(Vector2 attackerPos)
    {
        isInvincible = true;
        IsKnockback = true; // 이동 조작 잠금 ON

        // --- [1. Vector2 넉백 연산] ---
        // 플레이어가 공격자보다 왼쪽에 있으면 -1f (왼쪽으로 넉백), 오른쪽에 있으면 1f (오른쪽으로 넉백)
        float directionX = (transform.position.x < attackerPos.x) ? -1f : 1f;
        rb.linearVelocity = new Vector2(directionX * knockbackForce.x, knockbackForce.y);

        // --- [2. 회색 깜빡임 및 타이머 처리] ---
        Color originalColor = Color.white;
        Color hitColor = Color.gray;

        float timer = 0f;
        float blinkInterval = 0.15f; // 깜빡이는 속도
        bool isGray = false;

        while (timer < invincibilityDuration)
        {
            // 깜빡임 연출
            spriteRenderer.color = isGray ? originalColor : hitColor;
            isGray = !isGray;

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;

            // 넉백 시간(knockbackDuration)이 지나면 조작 잠금 해제 (무적 상태는 계속 유지)
            if (IsKnockback && timer >= knockbackDuration)
            {
                IsKnockback = false;
            }
        }

        // --- [3. 무적 종료 및 복구] ---
        spriteRenderer.color = originalColor; // 색상 복구
        IsKnockback = false;                   // 안전장치
        isInvincible = false;                  // 무적 해제
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // TODO: 사망 애니메이션 재생, 입력 비활성화, UI 표시 등 추가
    }

    /// <summary>
    /// 이동 스크립트 등 다른 곳에서 플레이어 스탯이 필요할 때 가져오는 함수
    /// </summary>
    public PlayerStats GetStats() => myStats;
}