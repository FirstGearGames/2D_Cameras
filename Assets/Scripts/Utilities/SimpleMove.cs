using System.Collections;
using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    public float WalkSpeed = 5f;
    public float MaxFallSpeed = -10f;
    public float JumpForce = 10f;

    private bool _jump = false;
    private Rigidbody2D _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        SetJump();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CheckPerformJump();
        CheckMoveHorizontally();

        /* Add additional gravity to increase rebound on jumps and to help
         * the unit stay grounded when going down slopes. */
        _rigidbody.AddForce(Physics2D.gravity);

        float velocityY = _rigidbody.velocity.y;
        //Cancel any falling speed beyond max fall speed.
        if (velocityY < MaxFallSpeed)
        {
            float difference = MaxFallSpeed - velocityY;
            _rigidbody.AddForce(new Vector2(0f, difference), ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Checks if jump force should be applied.
    /// </summary>
    private void CheckPerformJump()
    {
        if (!_jump)
            return;

        //_jumpsRemaining--;
        _jump = false;
        _rigidbody.AddForce(new Vector2(0f, JumpForce) + new Vector2(0f, -_rigidbody.velocity.y), ForceMode2D.Impulse);
    }

    /// <summary>
    /// Updates animator values.
    /// </summary>
    private void UpdateAnimator()
    {
        if (_animator == null)
            return;

        bool walking = (Mathf.Abs(_rigidbody.velocity.x) > 0.25f);
        _animator.SetBool("Walking", walking);
    }

    /// <summary>
    /// Checks if horizontal force should be added.
    /// </summary>
    private void CheckMoveHorizontally()
    {
        float horizontal = Input.GetAxis("Horizontal");
        //If there is horizontal input.
        if (horizontal != 0f)
        {
            //Add movement force for walking. Cancel existing horizontal force to ensure speed is consistent.
            _rigidbody.AddForce(new Vector3((horizontal * WalkSpeed) - _rigidbody.velocity.x, 0f), ForceMode2D.Impulse);
            //Turn sprite to proper facing.
            if (horizontal > 0f)
                transform.eulerAngles = new Vector3(0f, 0f, 0f);
            else
                transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }

    /// <summary>
    /// Sets if a jump should be queued next FixedUpdate.
    /// </summary>
    private void SetJump()
    {
        //Issue a junp for next fixed update.
        if (Input.GetKeyDown(KeyCode.Space))
            _jump = true;
    }

}
