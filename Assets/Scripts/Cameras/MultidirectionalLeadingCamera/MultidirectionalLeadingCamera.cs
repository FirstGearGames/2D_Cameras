using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MultidirectionalLeadingCamera : MonoBehaviour
{
    #region Serialized.
    /// <summary>
    /// Reference to the player's transform.
    /// </summary>
    [SerializeField]
    private Transform _targetTransform;
    /// <summary>
    /// Minimum and Maximum values the target must exceed within viewport space before this transform follows the target vertically.
    /// </summary>
    [SerializeField]
    private FloatRange _verticalViewportBounds = new FloatRange(0.25f, 0.65f);
    /// <summary>
    /// Distance player can travel from center point for horizontal floating bounds.
    /// </summary>
    [SerializeField]
    private float _horizontalFloatingWidth = 1.5f;
    /// <summary>
    /// Distance to travel past the target in the target's moving direction.
    /// </summary>
    [SerializeField]
    private float _horizontalOvershoot = 1.5f;
    /// <summary>
    /// True to stop the camera horizontal movement when the target's horizontal direction changes.
    /// </summary>
    [SerializeField]
    private bool _stopHorizontalMovementOnDirectionChange = true;

    [Header("Smoothing")]
    /// <summary>
    /// Default time to smooth damp to target position.
    /// </summary>
    [SerializeField]
    private float _smoothTimeBase = 0.5f;
    /// <summary>
    /// Lowest value smooth time may reach.
    /// </summary>
    [SerializeField]
    private float _minimumSmoothTime = 0.05f;
    /// <summary>
    /// How quickly to decrease smooth time from SmoothTimeBase.
    /// </summary>
    [SerializeField]
    [Range(0.01f, 1f)]
    private float _smoothDecreaseRate = 0.25f;
    #endregion

    #region Private.
    /// <summary>
    /// Reference to the camera on this gameObject.
    /// </summary>
    private Camera _camera;
    /// <summary>
    /// Position of the target after last frames calculations.
    /// </summary>
    private Vector3 _lastTargetPosition;
    /// <summary>
    /// Bounds where the target can move freely horizontally without the camera moving.
    /// </summary>
    private FloatRange _horizontalFloatingBounds = null;
    /// <summary>
    /// Direction of the target after last frames calculations.
    /// </summary>
    private float _lastHorizontalDirection;
    /// <summary>
    /// Time to SmoothDamp this transform to the target's position.
    /// </summary>
    private float _currentSmoothTime;
    /// <summary>
    /// Destination for this transform to move towards.
    /// </summary>
    private Vector3 _cameraGoal;
    /// <summary>
    /// X position of this transform after last frames calculations.
    /// </summary>
    private float _lastTransformX;
    #endregion

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        transform.position = new Vector3(_targetTransform.position.x, _targetTransform.position.y, transform.position.z);
        _cameraGoal = transform.position;
        _currentSmoothTime = _smoothTimeBase;
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    /// <summary>
    /// Sets CameraGoal and issues this transform to move to the goal.
    /// </summary>
    private void FollowTarget()
    {
        if (_targetTransform == null)
            return;

        if (_targetTransform.position != _lastTargetPosition)
        {
            float goalX, goalY;
            float horizontalDirection = Mathf.Sign(_targetTransform.position.x - _lastTargetPosition.x);

            //Currently no horizontal floating bounds.
            if (_horizontalFloatingBounds == null)
            {
                //If direction has changed make new floating bounds.
                if (horizontalDirection != _lastHorizontalDirection)
                    SetHorizontalFloatingBounds(_targetTransform.position.x, horizontalDirection);
            }
            //There is a floating bounds.
            else
            {
                //If target is outside the bounds then nullify floating bounds.
                if (_targetTransform.position.x < _horizontalFloatingBounds.Minimum || _targetTransform.position.x > _horizontalFloatingBounds.Maximum)
                {
                    _horizontalFloatingBounds = null;
                    //Reset smoothing time as well.
                    _currentSmoothTime = _smoothTimeBase;
                }
            }
            //Set goalX.
            if (_horizontalFloatingBounds == null)
            {
                goalX = _targetTransform.position.x + (_horizontalOvershoot * horizontalDirection);
            }
            else
            {
                if (_stopHorizontalMovementOnDirectionChange)
                    goalX = transform.position.x;
                else
                    goalX = _cameraGoal.x;
            }

            //Where the target is within the viewport.
            Vector2 targetViewportPosition = _camera.WorldToViewportPoint(_targetTransform.position);
            //Becomes true if this transform needs to follow the target vertically.
            bool moveVertically = (targetViewportPosition.y < _verticalViewportBounds.Minimum || targetViewportPosition.y > _verticalViewportBounds.Maximum);
            //If this transform needs to move
            if (moveVertically)
            {
                float requiredUnits;
                float cameraHeight = _camera.orthographicSize * 2f;
                //If target is going up.
                if (targetViewportPosition.y >= _verticalViewportBounds.Maximum)
                    requiredUnits = (targetViewportPosition.y - _verticalViewportBounds.Maximum) * cameraHeight;
                else
                    requiredUnits = (_verticalViewportBounds.Minimum - targetViewportPosition.y) * -cameraHeight;

                goalY = transform.position.y + requiredUnits;
            }
            //Don't need to move vertically.
            else
            {
                goalY = _cameraGoal.y;
            }

            _cameraGoal = new Vector3(goalX, goalY, transform.position.z);
            //Update the "_last" variables.
            _lastHorizontalDirection = horizontalDirection;
            _lastTargetPosition = _targetTransform.position;
        }

        MoveToGoal(_cameraGoal);
    }

    /// <summary>
    /// Moves to the specified goal smoothly.
    /// </summary>
    /// <param name="goal"></param>
    private void MoveToGoal(Vector3 goal)
    {
        if (transform.position == goal)
            return;

        float nextX = Mathf.SmoothDamp(transform.position.x, goal.x, ref _lastTransformX, _currentSmoothTime);
        transform.position = new Vector3(nextX, goal.y, goal.z);
        //Reduce smoothing time.
        _currentSmoothTime = Mathf.Max(_currentSmoothTime - (Time.deltaTime * _smoothDecreaseRate), _minimumSmoothTime);
    }

    /// <summary>
    /// Creates a horizontal floating bounds.
    /// </summary>
    /// <param name="targetX"></param>
    /// <param name="targetDirection"></param>
    private void SetHorizontalFloatingBounds(float targetX, float targetDirection)
    {
        float halfFloatingWidth = _horizontalFloatingWidth / 2f;
        float middle = targetX + (targetDirection * halfFloatingWidth);
        _horizontalFloatingBounds = new FloatRange(middle - halfFloatingWidth, middle + halfFloatingWidth);
    }
}
