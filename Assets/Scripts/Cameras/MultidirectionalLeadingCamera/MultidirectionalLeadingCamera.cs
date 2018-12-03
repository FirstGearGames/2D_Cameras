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
    /// How quickly to follow Target horizontally.
    /// </summary>
    [Tooltip("How quickly to follow Target horizontally.")]
    [SerializeField]
    private float _horizontalFollowRate = 3f;
    /// <summary>
    /// How quickly to catch up to the target when moving in a constant horizontal direction.
    /// </summary>
    [Tooltip("How quickly to catch up to the target when moving in a constant horizontal direction.")]
    [SerializeField]
    private float _horizontalCatchUpRate = 7f;
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
    #endregion

    #region Private.
    /// <summary>
    /// Last position of the target from last frames calculations.
    /// </summary>
    private Vector3 _lastTargetPosition = Vector3.zero;
    /// <summary>
    /// Last direction of the target from last frames calculations.
    /// </summary>
    private Vector2 _lastTargetDirection = Vector2.zero;
    /// <summary>
    /// Rate in which to catch up to the target. Increases with prolonged movement in the same direction.
    /// </summary>
    private float _catchUpMultiplier = 1f;
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
    #endregion


    private void Awake()
    {
        _camera = GetComponent<Camera>();
        //Center on the target.
        transform.position = new Vector3(_targetTransform.position.x, _targetTransform.position.y, transform.position.z);
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
            Vector2 direction = new Vector2(
                Mathf.Sign(_targetTransform.position.x - _lastTargetPosition.x),
                Mathf.Sign(_targetTransform.position.y - _lastTargetPosition.y)
                );

            float goalX;
            //If there are no floating bounds check if one needs to be applied.
            if (_floatingBounds == null)
            {
                //If target direction has changed on X.
                if (direction.x != _lastTargetDirection.x)
                {
                    SetFloatingBounds(_targetTransform.position.x, direction.x);
                    //Reset the multiplier as direction has changed.
                    _catchUpMultiplier = 1f;
                }
                //Same direction.
                else
                {
                    _catchUpMultiplier += (_horizontalCatchUpRate * Time.deltaTime);
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
                goalX = _targetTransform.position.x + (_horizontalOvershoot * direction.x);
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
            _lastTargetDirection = direction;
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

        /* Distance to meet goal is used as a multiplier so that the speed is increased
         * further this transform is away from the goal. It's important to use a max value so that
         * this transform doesn't crawl when very close to it's goal. */
        float xDistance = Mathf.Max(1f, Mathf.Abs(goal.x - transform.position.x));
        float nextX = Mathf.MoveTowards(transform.position.x, goal.x, _horizontalFollowRate * xDistance * _catchUpMultiplier * Time.deltaTime);

        transform.position = new Vector3(nextX, goal.y, goal.z);
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
