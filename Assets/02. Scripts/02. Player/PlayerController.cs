using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // InputSystem 네임스페이스 필수

public class PlayerController : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float climbSpeed = 5f;

    [Header("컴포넌트 참조")]
    private Rigidbody2D rb;
    private Collider2D playerCollider;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        originalGravity = rb.gravityScale;
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            // 사다리 상태
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, moveInput.y * climbSpeed);

            if (currentLadder != null)
            {
                Vector3 targetPosition = transform.position;
                targetPosition.x = currentLadder.position.x;
                transform.position = targetPosition;
            }
        }
        else
        {
            // 일반 상태
            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    // Send Messages 방식: InputValue 타입을 받습니다.
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        // 사다리 범위 안에서 위/아래 방향키 입력 시 사다리 모드 진입
        if (isNearLadder && !isClimbing && Mathf.Abs(moveInput.y) > 0.1f)
        {
            isClimbing = true;
        }
    }

    // Send Messages 방식: InputValue 타입을 받습니다.
    public void OnJump(InputValue value)
    {
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
                    rb.linearVelocity = new Vector2(jumpDirection * moveSpeed, jumpForce);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, jumpForce);
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
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

        // 2. [해결 핵심] 아래쪽으로 강제로 속도를 주어 발판을 빠르게 빠져나가게 함
        // (jumpForce의 절반 정도 수치나 일정 음수 값을 주면 자연스럽게 떨어집니다)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce * 0.5f);

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