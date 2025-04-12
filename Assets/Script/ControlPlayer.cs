using System;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ControlPlayer : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        public event Action<bool> GravityReversedChanged;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        [Header("Mobile Input")]
        [SerializeField] private MobileInputButton jumpButton;
        [SerializeField] private MobileInputButton leftButton;
        [SerializeField] private MobileInputButton rightButton;
        public bool Mobile;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            if (Mobile)
            {
                float moveX = 0;
                if (leftButton != null && leftButton.GetButton()) moveX -= 1;
                if (rightButton != null && rightButton.GetButton()) moveX += 1;

                _frameInput = new FrameInput
                {
                    JumpDown = (jumpButton != null && jumpButton.GetButtonDown()),
                    JumpHeld = (jumpButton != null && jumpButton.GetButton()),
                    Move = new Vector2(moveX, 0)
                };
            }
            else
            {
                _frameInput = new FrameInput
                {
                    JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                    JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                    Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                };
            }

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();
            ApplyMovement();
        }

        #region Collisions
        private bool _isGravityReversed;

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        // Added: Property to allow external control of gravity reversal
        public bool IsGravityReversed
        {
            get => _isGravityReversed;
            set
            {
                if (_isGravityReversed != value)
                {
                    _isGravityReversed = value;
                    // Flip the character when gravity changes
                    transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
                    // Reset grounded state
                    _grounded = false;
                    _frameLeftGrounded = _time;
                }
            }
        }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // More reliable ground check with multiple rays
            bool groundHit = false;
            Vector2 rayStart = _col.bounds.center;
            float rayLength = _col.bounds.extents.y + _stats.GrounderDistance;

            // Determine ground check direction based on gravity
            Vector2 groundCheckDir = _isGravityReversed ? Vector2.up : Vector2.down;

            // Cast multiple rays for more reliable ground detection
            for (int i = -1; i <= 1; i++)
            {
                Vector2 origin = rayStart + Vector2.right * (i * _col.bounds.extents.x * 0.8f);
                RaycastHit2D hit = Physics2D.Raycast(origin, groundCheckDir, rayLength, ~_stats.PlayerLayer);
                if (hit.collider != null)
                {
                    groundHit = true;
                    break;
                }
            }

            // Ceiling check (uses opposite direction of ground check)
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, -groundCheckDir, _stats.GrounderDistance, ~_stats.PlayerLayer);

            if (ceilingHit) _frameVelocity.y = _isGravityReversed ? Mathf.Max(0, _frameVelocity.y) : Mathf.Min(0, _frameVelocity.y);

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));

                // Reset horizontal velocity when landing to prevent sticking
                if (Mathf.Abs(_frameVelocity.x) < 0.1f)
                {
                    _frameVelocity.x = 0;
                }
            }
            // Left the Ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            // Modified: Check jump conditions based on gravity direction
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld &&
                ((!_isGravityReversed && _rb.linearVelocity.y > 0) || (_isGravityReversed && _rb.linearVelocity.y < 0)))
            {
                _endedJumpEarly = true;
            }

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            // Modified: Apply jump in correct direction based on gravity
            _frameVelocity.y = _isGravityReversed ? -_stats.JumpPower : _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        // No changes needed for horizontal movement
        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                float acceleration = _grounded ? _stats.Acceleration : _stats.Acceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && (_isGravityReversed ? _frameVelocity.y >= 0f : _frameVelocity.y <= 0f))
            {
                // Modified: Apply grounding force in correct direction
                _frameVelocity.y = _isGravityReversed ? -_stats.GroundingForce : _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && ((!_isGravityReversed && _frameVelocity.y > 0) || (_isGravityReversed && _frameVelocity.y < 0)))
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;

                // Modified: Apply gravity in correct direction
                float targetFallSpeed = _isGravityReversed ? _stats.MaxFallSpeed : -_stats.MaxFallSpeed;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, targetFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInputt
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerControllerr
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}