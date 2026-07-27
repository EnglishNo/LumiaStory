using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // InputSystem 네임스페이스 필수

public class PlayerController : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Player playerScript; // 플레이어 스탯 및 상태를 가져올 스크립트 참조

    private float originalGravity;
    private Transform currentLadder;

    // 일방통행(하향 가능) 발판 참조
    private Collider2D currentOneWayPlatform;

    // 입력 값
    private Vector2 moveInput;

    // 상태 플래그
    private bool isGrounded;
    private bool isNearLadder;
    private bool isClimbing;

    // --- [스탯 연동 프로퍼티] ---
    // Player 스크립트에 저장된 최신 스탯을 실시간으로 가져옵니다.
    private float CurrentMoveSpeed => playerScript != null ? playerScript.GetStats().moveSpeed : 6f;
    private float CurrentJumpForce => playerScript != null ? playerScript.GetStats().jumpPower : 12f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerScript = GetComponent<Player>(); // Player 스크립트 캐싱
        originalGravity = rb.gravityScale;
    }

    private void FixedUpdate()
    {
        // ★ [넉백 예외 처리] 넉백 중일 때는 물리 이동 및 속도 덮어쓰기를 건너뜁니다!
        if (playerScript != null && playerScript.IsKnockback)
        {
            // 혹시 사다리를 타던 중에 피격당했다면 사다리에서 즉시 떨어뜨림
            if (isClimbing)
            {
                isClimbing = false;
                rb.gravityScale = originalGravity;
            }
            return;
        }

        if (isClimbing)
        {
            // 사다리 상태 (이동 속도 스탯 적용)
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, moveInput.y * CurrentMoveSpeed);

            if (currentLadder != null)
            {
                Vector3 targetPosition = transform.position;
                targetPosition.x = currentLadder.position.x;
                transform.position = targetPosition;
            }
        }
        else
        {
            // 일반 상태 (이동 속도 스탯 적용)
            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }
    }

    // Send Messages 방식: InputValue 타입을 받습니다.
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        // 넉백 중이 아닐 때만 사다리 진입 판정
        if (playerScript != null && playerScript.IsKnockback) return;

        // 사다리 범위 안에서 위/아래 방향키 입력 시 사다리 모드 진입
        if (isNearLadder && !isClimbing && Mathf.Abs(moveInput.y) > 0.1f)
        {
            isClimbing = true;
        }
    }

    // Send Messages 방식: InputValue 타입을 받습니다.
    public void OnJump(InputValue value)
    {
        // ★ 넉백 중일 때는 점프 입력 무시
        if (playerScript != null && playerScript.IsKnockback) return;

        // 버튼을 누른 순간(isPressed가 true일 때) 실행
        if (value.isPressed)
        {
            if (isClimbing)
            {
                // 사다리 탈출 점프
                isClimbing = false;
                rb.gravityScale = originalGravity;

                if (Mathf.Abs(moveInput.x) > 0.1f)
                {
                    float jumpDirection = Mathf.Sign(moveInput.x);
                    rb.linearVelocity = new Vector2(jumpDirection * CurrentMoveSpeed, CurrentJumpForce);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, CurrentJumpForce);
                }
            }
            else if (isGrounded)
            {
                // 하향 점프 (아래 키 + 점프)
                if (moveInput.y < -0.1f && currentOneWayPlatform != null)
                {
                    StartCoroutine(DisableCollisionRoutine());
                }
                else
                {
                    // 일반 점프
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentJumpForce);
                }
            }
        }
    }

    // 통과 발판 아래로 떨어뜨리는 코루틴
    private IEnumerator DisableCollisionRoutine()
    {
        Collider2D platformToIgnore = currentOneWayPlatform;
        if (platformToIgnore == null) yield break;

        // 1. 플레이어와 해당 발판 간의 충돌 무시
        Physics2D.IgnoreCollision(playerCollider, platformToIgnore, true);

        // 2. 아래쪽으로 강제로 속도를 주어 발판을 빠르게 빠져나가게 함
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -CurrentJumpForce * 0.5f);

        // 3. 발판을 완전히 통과할 수 있도록 여유 있게 대기 (0.35초)
        yield return new WaitForSeconds(0.35f);

        // 4. 발판을 완전히 벗어난 후 충돌 다시 활성화
        if (platformToIgnore != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformToIgnore, false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isNearLadder = true;
            currentLadder = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isNearLadder = false;
            isClimbing = false;
            currentLadder = null;
            rb.gravityScale = originalGravity;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("OneWayPlatform"))
        {
            isGrounded = true;

            if (collision.gameObject.CompareTag("OneWayPlatform"))
            {
                currentOneWayPlatform = collision.collider;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("OneWayPlatform"))
        {
            isGrounded = false;

            if (collision.gameObject.CompareTag("OneWayPlatform"))
            {
                currentOneWayPlatform = null;
            }
        }
    }
}