using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MultidirectionalLeadingCamera : MonoBehaviour
{
    #region Serialized.
    /// <summary>
    /// Target transform to follow.
    /// </summary>
    [Tooltip("Target transform to follow.")]
    [SerializeField]
    private Transform _targetTransform;
    /// <summary>
    /// Amount of distance the unit can travel in an opposite direction before the camera changes direction.
    /// </summary>
    [Tooltip("Amount of distance the unit can travel in an opposite direction before the camera changes direction.")]
    [SerializeField]
    private float _horizontalFloatingWidth = 1.5f;
    /// <summary>
    /// Vertical area of the viewport which the target must past before the camera will move vertically.
    /// </summary>
    [Tooltip("Vertical area of the viewport which the target must past before the camera will move vertically.")]
    [SerializeField]
    private FloatRange _verticalViewportBounds = new FloatRange(0.25f, 0.75f);
    /// <summary>
    /// Distance to offset the camera from the horizontal center of the target in either direction.
    /// </summary>
    [Tooltip("Distance to offset the camera from the horizontal center of the target in either direction.")]
    [SerializeField]
    private float _horizontalOvershoot = 1;

    [Header("Smoothing")]
    /// <summary>
    /// Default time to smooth damp to target position.
    /// </summary>
    [Tooltip("Default time to smooth damp to target position.")]
    [SerializeField]
    private float _smoothTimeBase = 0.5f;
    /// <summary>
    /// Lowest value smooth time may reach. Useful for always keep the transform slightly behind target for more fluid movement.
    /// </summary>
    [Tooltip("Lowest value smooth time may reach. Useful for always keep the transform slightly behind target for more fluid movement.")]
    [SerializeField]
    private float _minimumSmoothTime = 0.05f;
    /// <summary>
    /// How quickly to decrease smooth time from SmoothTimeBase.
    /// </summary>
    [Tooltip("How quickly to decrease smooth time from SmoothTimeBase.")]
    [SerializeField]
    [Range(0.01f, 1f)]
    private float _smoothDecreaseRate = 0.15f;
    #endregion

    #region Private.
    /// <summary>
    /// Position of the target from last frames calculations.
    /// </summary>
    private Vector3 _lastTargetPosition = Vector3.zero;
    /// <summary>
    ///Horizontal direction of the target from last frames calculations.
    /// </summary>
    private float _lastHorizontalDirection = 0f;
    /// <summary>
    /// Position where the camera should move towards.
    /// </summary>
    private Vector3 _cameraGoal;
    /// <summary>
    /// If not null the target must move outside this area in world space horizontal before the camera begans to move as well.
    /// </summary>
    private FloatRange _floatingBounds = null;
    /// <summary>
    /// Camera component on this game object.
    /// </summary>
    private Camera _camera;
    /// <summary>
    /// Last x value of this tranform. Used to SmoothDamp movement.
    /// </summary>
    private float _lastTransformX = 0f;
    /// <summary>
    /// Current time to smooth damp to target position. This value decreases as the target moves in the same direction.
    /// </summary>
    private float _currentSmoothTime = 0f;
    #endregion


    private void Awake()
    {
        _camera = GetComponent<Camera>();
        //Center on the target.
        transform.position = new Vector3(_targetTransform.position.x, _targetTransform.position.y, transform.position.z);
        ResetSmoothTime();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        //No target.
        if (_targetTransform == null)
            return;

        if (_targetTransform.position != _lastTargetPosition)
        {
            //Raw direction in which the target transform has moved on X/Y.
            float horizontalDirection = Mathf.Sign(_targetTransform.position.x - _lastTargetPosition.x);

            float goalX;
            //If there are no floating bounds check if one needs to be applied.
            if (_floatingBounds == null)
            {
                //If target direction has changed on X.
                if (horizontalDirection != _lastHorizontalDirection)
                {
                    SetFloatingBounds(_targetTransform.position.x, horizontalDirection);
                    //Reset the multiplier as direction has changed.
                    _currentSmoothTime = _smoothTimeBase;
                }
            }
            //FloatingBounds exist, check if it needs to be broken.
            else
            {
                //If outside either bounds horizontally.
                if (_targetTransform.position.x < _floatingBounds.Minimum || _targetTransform.position.x > _floatingBounds.Maximum)
                    _floatingBounds = null;
            }
            //If floating bounds wasn't set update camera to target position with overshoot and target velocity.
            if (_floatingBounds == null)
                goalX = _targetTransform.position.x + (_horizontalOvershoot * horizontalDirection);
            else
                goalX = transform.position.x;


            float goalY;
            //Where the target is within the main cameras viewport.
            Vector2 targetViewportPosition = Camera.main.WorldToViewportPoint(_targetTransform.position);
            //Becomes true if this transform needs to move vertically to keep up with the target.
            bool moveVertically = (targetViewportPosition.y < _verticalViewportBounds.Minimum || targetViewportPosition.y > _verticalViewportBounds.Maximum);
            //If the camera needs to move vertically to keep the target within vertical bounds.
            if (moveVertically)
            {
                float requiredPixels;
                float cameraHeight = _camera.orthographicSize * 2f;
                //If the target is going up.
                if (targetViewportPosition.y > 0.5f)
                    requiredPixels = (targetViewportPosition.y - _verticalViewportBounds.Maximum) * cameraHeight;
                //If the target is going down.
                else
                    requiredPixels = (_verticalViewportBounds.Minimum - targetViewportPosition.y) * -cameraHeight;

                goalY = transform.position.y + requiredPixels;
            }
            //Target is within bounds, no need to move camera.
            else
            {
                //Use current camera goal on y.
                goalY = _cameraGoal.y;
            }

            //Build camera goal using created values.            
            _cameraGoal = new Vector3(goalX, goalY, transform.position.z);
            //Update last direction and target position.
            _lastHorizontalDirection = horizontalDirection;
            _lastTargetPosition = _targetTransform.position;
        }

        //Move to the camera goal.
        MoveToGoal(_cameraGoal);
    }


    /// <summary>
    /// Moves to MoveGoalX and MoveGoalY.
    /// </summary>
    private void MoveToGoal(Vector3 goal)
    {
        if (transform.position == goal)
            return;

        float nextX = Mathf.SmoothDamp(transform.position.x, goal.x, ref _lastTransformX, _currentSmoothTime);
        transform.position = new Vector3(nextX, goal.y, goal.z);
        //Reduce smooth time.
        _currentSmoothTime = Mathf.Max(_currentSmoothTime - (Time.deltaTime * _smoothDecreaseRate), _minimumSmoothTime);
    }

    /// <summary>
    /// Sets the CurrentSmoothTime to the SmoothTimeBase.
    /// </summary>
    private void ResetSmoothTime()
    {
        _currentSmoothTime = _smoothTimeBase;
        _lastTransformX = transform.position.x;
    }

    private float CubicEase(float percent)
    {
        /* If either of these values result will always be 
         * the passed in value. */
        if (percent == 0 || percent == 1)
            return percent;

        if (percent < 0.5f)
        {
            return percent;
            //return percent * percent * percent * 0.5f;// Cubic equation then scale down to half size
        }
        else
        {
            return percent * percent * percent * 0.5f + 0.5f;// Same as above but inverted and shifted 
        }
    }

    /// <summary>
    /// Sets FloatingBounds using specified data.
    /// </summary>
    /// <param name="targetX"></param>
    /// <param name="targetDirection"></param>
    private void SetFloatingBounds(float targetX, float targetDirection)
    {
        float halfFloatingWidth = _horizontalFloatingWidth / 2f;
        float middle = targetX + (targetDirection * halfFloatingWidth);
        _floatingBounds = new FloatRange(middle - halfFloatingWidth, middle + halfFloatingWidth);
    }


}
