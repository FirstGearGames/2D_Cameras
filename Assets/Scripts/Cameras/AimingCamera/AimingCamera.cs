using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AimingCamera : MonoBehaviour
{
    #region Serialized.
    /// <summary>
    /// Target transform to follow.
    /// </summary>
    [Tooltip("Target transform to follow.")]
    [SerializeField]
    private Transform _targetTransform;
    /// <summary>
    /// How quickly to move towards aiming direction.
    /// </summary>
    [Tooltip("How quickly to move towards aiming direction.")]
    [SerializeField]
    private float _aimRate = 3f;
    /// <summary>
    /// Distance to offset the camera from the target in aiming direction.
    /// </summary>
    [Tooltip("Distance to offset the camera from the target in aiming direction.")]
    [SerializeField]
    private float _overshoot = 1;
    /// <summary>
    /// True to always aim at max overshoot distance from the target.
    /// </summary>
    [Tooltip("True to always aim at max overshoot distance from the target.")]
    [SerializeField]
    private bool _useMaxOvershoot = false;
    #endregion

    #region Private.
    /// <summary>
    /// Last position of the target from last frames calculations.
    /// </summary>
    private Vector3 _lastTargetPosition = Vector3.zero;
    /// <summary>
    /// Last position of the mouse from last frames calculations.
    /// </summary>
    private Vector2 _lastMousePosition = Vector2.zero;
    /// <summary>
    /// Last direction of the target from last frames calculations.
    /// </summary>
    private Vector2 _lastTargetDirection = Vector2.zero;
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

    CameraShaker shaker;

    private void Awake()
    {
        shaker = GetComponent<CameraShaker>();
        _camera = GetComponent<Camera>();
        transform.position = new Vector3(_targetTransform.position.x, _targetTransform.position.y, transform.position.z);
    }

    public float Duration = 1f;
    public float Magnitude = 1f;
    public float Violence = 1f;
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.W))
            shaker.GenerateShake(Duration, Magnitude, Violence);

        FollowTarget();
    }

    private void FollowTarget()
    {
        //No target.
        if (_targetTransform == null)
            return;

        Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        /* If to use max overshoot match the z value
         * with the targets z. This will ensure the normalized
         * direction will be at max x/y values. */
        if (_useMaxOvershoot)
            mousePosition.z = _targetTransform.position.z;
        //Direction the mouse is aiming from the target. May want to use the center of screen if the target is always centered.
        Vector3 direction = (mousePosition - _targetTransform.position).normalized;
        //Set goal to the target's position + the direction.
        Vector3 offsetTarget = _targetTransform.position + (direction * _overshoot);

        Vector3 goal = new Vector3(offsetTarget.x, offsetTarget.y, transform.position.z);
        //Move to the camera goal.
        MoveToGoal(goal);
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
        float distance = Mathf.Max(1f, Vector2.Distance(goal, transform.position));

        transform.position = Vector3.MoveTowards(transform.position, goal, _aimRate * distance * Time.deltaTime);
    }


}
