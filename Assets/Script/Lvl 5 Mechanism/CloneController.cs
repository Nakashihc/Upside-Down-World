using System;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class CloneController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;
        private float _time;

        [Header("Clone Settings")]
        public bool flipHorizontalMovement = true;
        public bool flipVerticalMovement = false;
        public bool flipGravity = true;

        private bool _isGravityReversed;

        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput => _frameInput.Move;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            if (flipGravity) IsGravityReversed = true;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            // Untuk clone, GatherInput bisa disiapkan manual (contoh untuk testing sementara ini random/move tetap)
            _frameInput = new FrameInput
            {
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                JumpDown = Input.GetButtonDown("Jump"),
                JumpHeld = Input.GetButton("Jump")
            };

            if (flipHorizontalMovement)
                _frameInput.Move.x *= -1;

            if (flipVerticalMovement)
                _frameInput.Move.y *= -1;

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

        #region Gravity Control

        public bool IsGravityReversed
        {
            get => _isGravityReversed;
            set
            {
                if (_isGravityReversed != value)
                {
                    _isGravityReversed = value;
                    transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
                    _grounded = false;
                    _frameLeftGrounded = _time;
                }
            }
        }

        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = false;
            Vector2 rayStart = _col.bounds.center;
            float rayLength = _col.bounds.extents.y + _stats.GrounderDistance;
            Vector2 groundCheckDir = _isGravityReversed ? Vector2.up : Vector2.down;

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

            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, -groundCheckDir, _stats.GrounderDistance, ~_stats.PlayerLayer);

            if (ceilingHit) _frameVelocity.y = _isGravityReversed ? Mathf.Max(0, _frameVelocity.y) : Mathf.Min(0, _frameVelocity.y);

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));

                if (Mathf.Abs(_frameVelocity.x) < 0.1f)
                    _frameVelocity.x = 0;
            }
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
            _frameVelocity.y = _isGravityReversed ? -_stats.JumpPower : _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal Movement

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
                _frameVelocity.y = _isGravityReversed ? -_stats.GroundingForce : _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && ((!_isGravityReversed && _frameVelocity.y > 0) || (_isGravityReversed && _frameVelocity.y < 0)))
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;

                float targetFallSpeed = _isGravityReversed ? _stats.MaxFallSpeed : -_stats.MaxFallSpeed;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, targetFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the CloneController's Stats slot", this);
        }
#endif
    }
}
