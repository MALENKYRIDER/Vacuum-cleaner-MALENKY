using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Movement settings")] [SerializeField]
    private float _walkSpeed = 5f;

    [SerializeField] private float _runSpeed = 8f;

    [Header("Jump settings")] [SerializeField]
    private float _jumpForce = 10f;

    [SerializeField] private float _doubleJumpForce = 8f;

    [Header("Mobile buttons")] [SerializeField]
    private Button _leftButton;

    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _jumpButton;
    [SerializeField] private Button _attack1Button;
    [SerializeField] private Button _attack2Button;

    private Rigidbody2D _body;
    private Animator _animator;
    private BoxCollider2D _boxCollider;

    private bool _grounded;
    private bool _canDoubleJump;
    private bool _touchingWall;
    private bool _facingRight = true;
    private bool _isDead;
    private bool _isAttacking;

    private float _horizontalInput;

    private bool _leftButtonDown;
    private bool _rightButtonDown;
    private bool _jumpButtonDown;
    private bool _attack1ButtonDown;
    private bool _attack2ButtonDown;
    
    

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.freezeRotation = true;
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Dodaj EventTriggery do przycisków ruchu
        AddEventTrigger(_leftButton.gameObject, EventTriggerType.PointerDown, (data) => { LeftButtonDown(); });
        AddEventTrigger(_leftButton.gameObject, EventTriggerType.PointerUp, (data) => { LeftButtonUp(); });
        AddEventTrigger(_leftButton.gameObject, EventTriggerType.PointerExit, (data) => { LeftButtonUp(); });

        AddEventTrigger(_rightButton.gameObject, EventTriggerType.PointerDown, (data) => { RightButtonDown(); });
        AddEventTrigger(_rightButton.gameObject, EventTriggerType.PointerUp, (data) => { RightButtonUp(); });
        AddEventTrigger(_rightButton.gameObject, EventTriggerType.PointerExit, (data) => { RightButtonUp(); });

        // Pozostałe przyciski mogą nadal używać onClick
        _jumpButton.onClick.AddListener(() => { JumpButtonDown(); });
        _attack1Button.onClick.AddListener(() => { Attack1ButtonDown(); });
        _attack2Button.onClick.AddListener(() => { Attack2ButtonDown(); });
    }

    private void AddEventTrigger(GameObject obj, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void Update()
    {
        if (_isDead || _isAttacking)
            return;

        if (_grounded)
        {
            _canDoubleJump = false;
            _animator.SetBool("IsDoubleJumping", false);
        }

        HandleInput();
        HandleAnimation();
    }
    private IEnumerator DisableDoubleJumpAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Czekaj przez 0.5 sekundy zanim wyłączysz animację
        _animator.SetBool("IsDoubleJumping", false);
    }
    private void LateUpdate()
    {
        _animator.SetBool("grounded", _grounded);
    }

    private void HandleInput()
    {
        _horizontalInput = 0;

        if (_leftButtonDown)
            _horizontalInput = -1;
        else if (_rightButtonDown)
            _horizontalInput = 1;

        float speed = Input.GetKey(KeyCode.LeftShift) ? _runSpeed : _walkSpeed;
        _body.linearVelocity = new Vector2(_horizontalInput * speed, _body.linearVelocity.y);

        HandleFlipDirection();

        if (_jumpButtonDown)
        {
            _jumpButtonDown = false; // Resetuj przycisk po skoku
            HandleJump();
        }

        if (_attack1ButtonDown)
        {
            _attack1ButtonDown = false; // Resetuj przycisk po ataku
            StartAttack("AttackSword");
        }

        if (_attack2ButtonDown)
        {
            _attack2ButtonDown = false; // Resetuj przycisk po ataku
            StartAttack("Attack");
        }
    }

    private void StartAttack(string attackTrigger)
    {
        if (!_isAttacking)
        {
            _isAttacking = true;
            _animator.SetTrigger(attackTrigger);
            StartCoroutine(ResetAttackAfterAnimation(attackTrigger));
        }
    }

    private IEnumerator<WaitForSeconds> ResetAttackAfterAnimation(string animationName)
    {
        yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
        _isAttacking = false;
    }

    private void HandleAnimation()
    {
        _animator.SetBool("IsRunning", Mathf.Abs(_horizontalInput) > 0.01f && _grounded);

        if (_grounded && Mathf.Abs(_body.linearVelocity.y) < 0.1f)
        {
            _animator.SetBool("IsJumping", false);
            _animator.SetBool("IsDoubleJumping", false);
            _canDoubleJump = false; // Dodane: upewnienie się, że nie można wykonać podwójnego skoku na ziemi
        }
        else if (!_grounded && _body.linearVelocity.y < -0.1f && !_animator.GetBool("IsDoubleJumping"))
        {
            _animator.Play("Jump down");
        }
    }

    private void HandleFlipDirection()
    {
        if (_horizontalInput > 0 && !_facingRight)
        {
            Flip();
        }
        else if (_horizontalInput < 0 && _facingRight)
        {
            Flip();
        }
    }

    private void HandleJump()
    {
        if (_grounded)
        {
            Jump(_jumpForce);
            _canDoubleJump = true; // Resetowane na false dopiero gdy wracamy na ziemię
            _animator.SetBool("IsJumping", true);
        }
        else if (_canDoubleJump && !_touchingWall)
        {
            Jump(_doubleJumpForce);
            _canDoubleJump = false;
            _animator.SetBool("IsDoubleJumping", true);
            _animator.SetBool("IsJumping", false);
        }
    }

    private void Jump(float force)
    {
        _body.linearVelocity = new Vector2(_body.linearVelocity.x, 0);
        _body.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        _grounded = false;
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void Die()
    {
        _isDead = true;
        _body.linearVelocity = Vector2.zero;
        _body.gravityScale = 0;
        _boxCollider.enabled = false;
        _animator.SetBool("IsDead", true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Ground"))
        {
            _grounded = true;
            _canDoubleJump = false;
            _animator.SetBool("IsJumping", false);
            _animator.SetBool("IsDoubleJumping", false);
            _animator.ResetTrigger("AttackSword");
            _animator.ResetTrigger("Attack");
        }
        else if (layer == LayerMask.NameToLayer("Wall"))
        {
            _touchingWall = true;
            _canDoubleJump = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Ground"))
        {
            _grounded = false;
        }
        else if (layer == LayerMask.NameToLayer("Wall"))
        {
            _touchingWall = false;
        }
    }

// Funkcje obsługi przycisków
    public void LeftButtonDown()
    {
        _leftButtonDown = true;
    }

    public void LeftButtonUp()
    {
        _leftButtonDown = false;
    }

    public void RightButtonDown()
    {
        _rightButtonDown = true;
    }

    public void RightButtonUp()
    {
        _rightButtonDown = false;
    }

    public void JumpButtonDown()
    {
        _jumpButtonDown = true;
    }

    public void Attack1ButtonDown()
    {
        _attack1ButtonDown = true;
    }

    public void Attack2ButtonDown()
    {
        _attack2ButtonDown = true;
    }
}