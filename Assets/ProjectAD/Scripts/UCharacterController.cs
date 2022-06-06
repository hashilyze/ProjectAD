using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAD
{
    public enum EState { 
        Walk, // Effected by gravity
        Fly, // Effected by zero gravity
        Swim, // Efftected by buoyancy
        Custom // User defined movement
    }

    [System.Serializable]
    public struct GroundHitReport
    {
        [Tooltip("Whether detect ground regardless with stablity")]
        public bool hitAnyGround;
        [Tooltip("Whether detected ground is stabe, character doesn't slide on")]
        public bool isStable;
        public Collider hitCollider;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float hitAngle;
        
    }

    public class UCharacterController : MonoBehaviour
    {
        // Called when character is grounded but previous tick is not
        public System.Action<UCharacterController> OnLand;
        // Called when character is falling but previous tick is not
        public System.Action<UCharacterController> OnFall;

        [Header("Base")]
        [ReadOnly]
        [SerializeField] private Vector3 m_velocity;
        //[SerializeField] private float m_mass = 100.0f;
        [SerializeField] private EState m_state = EState.Walk;
        
        [Header("Gravity")]
        [Tooltip("Apply gravity as character is on airbone")]
        [SerializeField] private bool m_useGravity = true;
        [Tooltip("Sacle of gravity without any weights; Higher falls faster")]
        [SerializeField] private float m_gravity = 10.0f;
        [Tooltip("Enhance gravity when fall")]
        [SerializeField] private float m_fallWeights = 1.2f;
        [ReadOnly]
        [SerializeField] private float m_gravityWeights = 1.0f;

        [Header("Grounding")]
        [Tooltip("Snap character to ledge when bounce on ledge")]
        [SerializeField] private bool m_useGroundSnap = true;
        [Tooltip("Maximum angle to stand on ground")]
        [Range(0.0f, 90.0f)]
        [SerializeField] private float m_stableAngle = 50.0f;
        [ReadOnly]
        [SerializeField] private GroundHitReport m_groundReport;
        // Force Unground parameters
        private bool m_forcedUnground;
        private float m_ungroundedTimmer;
        private float m_elapsedUngroundedTime = 0.0f;

        [Header("Walk")]
        [Tooltip("Maximum speed as move on ground")]
        [SerializeField] private float m_maxSpeed = 5.0f;
        [Tooltip("The bigger, the faster reach MaxSpeed")]
        [SerializeField] private float m_acceleration = float.PositiveInfinity;
        [Tooltip("Fast to reach stop(zero velocity)s")]
        [SerializeField] private float m_friction = float.PositiveInfinity;

        [Header("Fall/Jump")]
        [Tooltip("Maximum speed as move on air")]
        [SerializeField] private float m_maxAirSpeed = 5.0f;
        [Tooltip("Fast to reach MaxSpeed")]
        [SerializeField] private float m_airAcceleration = 10.0f;
        [Tooltip("Fast to reach stop(zero velocity)")]
        [SerializeField] private float m_drag = float.PositiveInfinity;
        [Tooltip("Limit applied garvity")]
        [SerializeField] private float m_maxFallSpeed = 10.0f;

        [Tooltip("Maximum distance as jump without obstacle")]
        [SerializeField] private float m_maxJumpDistance = 5.0f;
        [Tooltip("Minimum distance as jump without obstacle")]
        [SerializeField] private float m_minJumpDistance = 1.0f;
        [Tooltip("How many do jumping on air")]
        [SerializeField] private int m_moreJumpCount = 1;
        private int m_leftMoreJumpCount;

        private bool m_doJump;
        private float m_elapsedJumpTime;

        
        [Header("Physics Interaction")]
        [Tooltip("Character could collide and detect with objects whose layer is in this")]
        [SerializeField] private LayerMask m_interactiveCollider = -1;
        [Tooltip("Gap between character and others")]
        [SerializeField] private float m_contactOffset = 0.02f;
        [Tooltip("Accuracy of depentration solver; Higher costs more")]
        [SerializeField] private int m_depentrationIteration = 2;
        [Tooltip("Accuracy of velocity solver; Higher costs more")]
        [SerializeField] private int m_velocityIteration = 5;
        [Tooltip("Canceal velocity interation (recomandation to use)")]
        [SerializeField] private bool m_killPositionWhenExceedVelocityIteration = true;
        [Tooltip("Discard remained deistance but not appand (recomandation to use)")]
        [SerializeField] private bool m_killRemainedDistanceWhenExceedVelocityIteration = true;

        // Memory cache
        private readonly Collider[] m_colliderBuffer = new Collider[8];
        private readonly RaycastHit[] m_hitInfoBuffer = new RaycastHit[8];

        // Outside components
        private CapsuleCollider m_bodyCollider;
        private Rigidbody m_rigidbody;
        private GameObject m_visual;

        // Transform before simulate
        private Vector3 m_initPos;
        private Quaternion m_initRot;
        // Transform character will be placed
        private Vector3 m_nextPos;
        private Quaternion m_nextRot;

        // Requested values
        private Vector3 m_inputDirection;
        private bool m_requestedJump;
        private bool m_requestedStopJump;
        
        private bool m_requestedTeleport;
        private Vector3 m_teleportLocation;
        private bool m_killVelocityWhenTP;

        private bool m_requestedLook;
        private Vector3 m_lookDirection;


        private void ValidateData ()
        {
            if(m_visual == null)
            {
                m_visual = transform.Find("Visual").gameObject;
            }

            m_bodyCollider = GetComponent<CapsuleCollider>();
            if (m_bodyCollider != null)
            {
                m_bodyCollider.isTrigger = false;
                // The foot position is character's position
                m_bodyCollider.center = new Vector3(0.0f, m_bodyCollider.height * 0.5f + m_contactOffset, 0.0f);

                if(m_visual != null)
                {
                    m_visual.transform.localPosition = new Vector3(0.0f, m_bodyCollider.height * 0.5f + m_contactOffset, 0.0f);
                }
            }

            m_rigidbody = GetComponent<Rigidbody>();
            if(m_rigidbody != null)
            {
                m_rigidbody.useGravity = false;
                m_rigidbody.isKinematic = true;
                m_rigidbody.constraints = RigidbodyConstraints.None;
            }
        }

        private void ResetData ()
        {
            m_doJump = false;
            m_leftMoreJumpCount = m_moreJumpCount;
        }

        private void OnValidate ()
        {
            ValidateData();
        }

        private void OnEnable ()
        {
            OnValidate();
            ResetData();

            KinematicPhysicsSystem.RegisterCharacterController(this);
            InputManager.RegisterPlayer(this);
        }
        private void OnDisable ()
        {
            KinematicPhysicsSystem.UnregisterCharacterController(this);
            InputManager.UnregisterPlayer(this);
        }


        /// <summary>
        /// Locate character at directed location without checking safe
        /// </summary>
        public void Teleport(Vector3 position, bool killVelocity = false)
        {
            m_requestedTeleport = true;
            m_teleportLocation = position;
            m_killVelocityWhenTP = killVelocity;
        }
        public void Look(Vector3 forward)
        {
            m_requestedLook = true;
            m_lookDirection = forward;
        }

        /// <summary>
        /// Move character continuosly
        /// </summary>
        /// <param name="direction">
        /// X: left-right; Z: forward-backward based on camera
        /// Y: top-bottom based on character
        /// </param>
        public void InputMove (Vector3 direction)
        {
            m_inputDirection = direction;
        }
        public void InputJump ()
        {
            m_requestedJump = true;
        }
        public void InputStopJump ()
        {
            m_requestedStopJump = true;
        }

        /// <summary>
        /// Character can't touch and detect ground until unground time is over
        /// </summary>
        public void ForceUnground (float time = 0.1f)
        {
            m_forcedUnground = true;
            m_ungroundedTimmer = time;
            m_elapsedUngroundedTime = 0.0f;
        }

        /// <summary>
        /// Simulate Part.1: Called before platforms are change transform but velocity
        /// - Intialize simulation values
        /// - Consume Teleport or Look
        /// - Decollision interative objects
        /// - Detect ground
        /// - Follow ground movement
        /// </summary>
        public void SimulatePart1 (float deltaTime)
        {
            // Initialize variables
            m_initPos = m_rigidbody.position;
            m_initRot = m_rigidbody.rotation;

            m_nextPos = m_initPos;
            m_nextRot = m_initRot;

            #region Set Traonsform (Teleport, Look): Async Function
            // Consume Teleport
            if (m_requestedTeleport)
            {
                m_requestedTeleport = false;
                m_nextPos = m_teleportLocation;
                if (m_killVelocityWhenTP)
                {
                    m_velocity = Vector3.zero;
                }
            }

            // Consume Look
            if (m_requestedLook)
            {
                m_requestedLook = false;
                m_nextRot = Quaternion.LookRotation(m_lookDirection);
            }
            #endregion

            #region SolveOverlap
            SolveOverlap();
            #endregion

            #region Detect Ground
            if (m_state == EState.Walk)
            {
                // Set ground report to no touched
                if (m_forcedUnground)
                {
                    // Force detaching ground
                    m_groundReport.hitAnyGround = false;
                    m_groundReport.hitCollider = null;

                    // Update timmer
                    m_elapsedUngroundedTime += deltaTime;
                    if (m_elapsedUngroundedTime >= m_ungroundedTimmer)
                    {
                        m_forcedUnground = false;
                    }
                }
                // Check ground
                else
                {
                    // Before update report, cache previous report; used for checking OnLanded
                    bool wasStable = m_groundReport.hitAnyGround && m_groundReport.isStable;
                    m_groundReport.hitAnyGround = false;
                    m_groundReport.hitCollider = null;
                    Vector3 characterUp = m_nextRot * Vector3.up;

                    float detectDistance = m_contactOffset * 1.5f;
                    // Expand detect distance to snap down ledge (loose detection span)
                    float maxExpand = Vector3.ProjectOnPlane(m_velocity * deltaTime, characterUp).magnitude * Mathf.Tan(m_stableAngle * Mathf.Deg2Rad);
                    // Ground means an interactive object detected from foot
                    if (GetClosestHit(m_nextPos, m_nextRot, -characterUp, detectDistance + maxExpand, out RaycastHit closestHit))
                    {
                        m_groundReport.hitAnyGround = true;

                        m_groundReport.hitCollider = closestHit.collider;
                        m_groundReport.hitPoint = closestHit.point;
                        m_groundReport.hitNormal = closestHit.normal;

                        float angle = Vector3.Angle(characterUp, closestHit.normal);
                        m_groundReport.hitAngle = angle;

                        m_groundReport.isStable = false;
                        if (angle < m_stableAngle)
                        {
                            // Strict detection span
                            float realExpand;
                            float verticalSpeed = Vector3.Dot(characterUp, m_velocity);
                            if (verticalSpeed > 0.0f)
                            {
                                realExpand = verticalSpeed * deltaTime;
                            }
                            else
                            {
                                realExpand = Vector3.ProjectOnPlane(m_velocity * deltaTime, characterUp).magnitude * Mathf.Tan(angle * Mathf.Deg2Rad);
                            }
                            if (closestHit.distance <= detectDistance + realExpand)
                            {
                                m_groundReport.isStable = true;
                            }
                        }
                    }

                    // Events for detecting ground
                    // On Ground
                    if (m_groundReport.hitAnyGround && m_groundReport.isStable)
                    {
                        // Ground snapping
                        if (m_useGroundSnap)
                        {
                            m_nextPos -= (closestHit.distance - m_contactOffset) * characterUp;
                        }

                        // On Landed (first tick when grounded)
                        if (!wasStable)
                        {
                            m_velocity = Vector3.ProjectOnPlane(m_velocity, characterUp);

                            m_leftMoreJumpCount = m_moreJumpCount;
                            m_doJump = false;
                            m_gravityWeights = 1.0f;

                            OnLand?.Invoke(this);
                        }
                    }
                }
            }
            #endregion

            #region Apply Ground Movement (Tracking target)
            if (m_state == EState.Walk)
            {
                if (m_groundReport.hitAnyGround && m_groundReport.isStable)
                {
                    Rigidbody groundRB = m_groundReport.hitCollider.attachedRigidbody;
                    if (groundRB != null && groundRB.TryGetComponent(out UPlatformController platform))
                    {
                        Vector3 groundDistance = Vector3.zero;

                        // Linear velocity
                        groundDistance += platform.Velocity * deltaTime;

                        // Angular velocity
                        if (!Mathf.Approximately(platform.RotateSpeed, 0.0f))
                        {
                            Vector3 start = m_groundReport.hitPoint;
                            Quaternion deltaRotation = Quaternion.AngleAxis(platform.RotateSpeed * deltaTime, platform.RotateAxis);
                            Vector3 dest = deltaRotation * (start - groundRB.position) + groundRB.position;
                            groundDistance += dest - start;
                        }

                        CharacterMove(groundDistance);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Simulate Part.2: Called after platforms are change transform
        /// - Decollision interactive objects
        /// - Update velocity with input and surround
        /// - Change character's transform
        /// </summary>
        public void SimulatePart2 (float deltaTime)
        {
            #region SolveOverlap
            SolveOverlap();
            #endregion

            #region Update Velocity
            switch (m_state)
            {
            case EState.Walk:
                // Movement on stable ground 
                if (m_groundReport.hitAnyGround && m_groundReport.isStable)
                {
                    // Reorient current velocity
                    Vector3 tangentVelocity = Vector3.ProjectOnPlane(m_velocity, m_groundReport.hitNormal).normalized;
                    Vector3 reorientVelocity = m_velocity.magnitude * tangentVelocity;

                    // Acceleration the character movement
                    if (m_inputDirection != Vector3.zero)
                    {
                        Quaternion inputRotatorForGround = Quaternion.FromToRotation(m_nextRot * Vector3.up, m_groundReport.hitNormal);
                        if (float.IsPositiveInfinity(m_acceleration))
                        {
                            m_velocity = m_maxSpeed * (inputRotatorForGround * m_inputDirection);
                        }
                        else
                        {
                            Vector3 deltaAddedVelocity = m_acceleration * deltaTime * (inputRotatorForGround * m_inputDirection);
                            m_velocity = Vector3.ClampMagnitude(reorientVelocity + deltaAddedVelocity, m_maxSpeed);
                        }
                    }
                    // Break the character movement (Decline velocity to zero)
                    else
                    {
                        m_velocity = Vector3.Lerp(reorientVelocity, Vector3.zero, 1.0f - Mathf.Exp(-m_friction * deltaTime));
                    }
                }
                // Movement on airbone
                else
                {
                    Vector3 characterUp = m_nextRot * Vector3.up;

                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(m_velocity, characterUp);
                    float verticalSpeed = Vector3.Dot(m_velocity, characterUp);
                    Vector3 verticalVelocity;

                    // Horizontal velocity
                    {
                        if (m_inputDirection != Vector3.zero)
                        {
                            if (float.IsPositiveInfinity(m_airAcceleration))
                            {
                                m_velocity = m_maxSpeed * m_inputDirection;
                            }
                            else
                            {
                                Vector3 deltaAddedVelocity = m_airAcceleration * deltaTime * m_inputDirection;
                                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity + deltaAddedVelocity, m_maxAirSpeed);
                            }
                        }
                        else
                        {
                            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, 1.0f - Mathf.Exp(-m_drag * deltaTime));
                        }
                    }

                    // Vertical velocity
                    {
                        if (m_useGravity)
                        {
                            verticalSpeed += deltaTime * -m_gravity * m_gravityWeights;
                        }

                        verticalSpeed = Mathf.Max(verticalSpeed, -m_maxFallSpeed);
                        verticalVelocity = verticalSpeed * characterUp;

                        if (verticalSpeed < 0.0f)
                        {
                            m_doJump = false;
                            m_gravityWeights = m_fallWeights;

                            OnFall?.Invoke(this);
                        }
                    }

                    m_velocity = horizontalVelocity + verticalVelocity;
                }

                if (m_doJump)
                {
                    m_elapsedJumpTime += deltaTime;
                }

                if (m_requestedJump)
                {
                    m_requestedJump = false;

                    // Check can character jump
                    // Awalys enable to jump on stable ground
                    if (m_groundReport.hitAnyGround && m_groundReport.isStable)
                    {
                        m_doJump = true;
                    }
                    // Should have more jump on airborn
                    else
                    {
                        if (m_leftMoreJumpCount > 0)
                        {
                            m_doJump = true;
                            --m_leftMoreJumpCount;
                        }
                    }

                    // Execute jump if enable
                    if (m_doJump)
                    {
                        m_elapsedJumpTime = 0.0f;
                        m_gravityWeights = 1.0f;

                        Vector3 characterUp = m_nextRot * Vector3.up;
                        // Rewrite vertical velocity but keep horizontal
                        m_velocity = Vector3.ProjectOnPlane(m_velocity, characterUp)
                            + characterUp * Mathf.Sqrt(2.0f * m_gravity * m_maxJumpDistance);
                        ForceUnground();
                    }
                }

                if (m_requestedStopJump)
                {
                    m_requestedStopJump = false;

                    // Execute only if jumping
                    if (m_doJump)
                    {
                        float init = Mathf.Sqrt(2.0f * m_gravity * m_maxJumpDistance);
                        float currentJumpHeight = init * m_elapsedJumpTime + 0.5f * -m_gravity * m_elapsedJumpTime * m_elapsedJumpTime;
                        // Ceil(Left-Jump-Distance) / Min-Jump-Distance
                        float forceFallingWeights = (m_maxJumpDistance - Mathf.Floor(currentJumpHeight)) / m_minJumpDistance;

                        if (forceFallingWeights < 1.0f)
                        {
                            forceFallingWeights = 1.0f;
                        }
                        m_gravityWeights = forceFallingWeights;
                    }
                }
                break;
            case EState.Fly:
                break;
            case EState.Swim:
                break;
            case EState.Custom:
                break;
            default:
                Debug.LogError("Undefined state");
                break;
            }
            #endregion

            #region Update Position And Rotation
            CharacterMove(m_velocity * deltaTime);

            // Broadcasting other controllers where does it place
            m_rigidbody.position = m_nextPos;
            m_rigidbody.rotation = m_nextRot;
            #endregion
        }

        public void SimulateCommit ()
        {
            m_rigidbody.position = m_initPos;
            m_rigidbody.rotation = m_initRot;

            m_rigidbody.MovePosition(m_nextPos);
            m_rigidbody.MoveRotation(m_nextRot);
        }


        private int CharacterOverlap (Vector3 position, Quaternion rotation, Collider[] colliders)
        {
            Vector3 center = position + rotation * m_bodyCollider.center;
            Vector3 relativePoint = rotation * ((0.5f * m_bodyCollider.height - m_bodyCollider.radius) * Vector3.up);

            return Physics.OverlapCapsuleNonAlloc(center + relativePoint, center - relativePoint,
                m_bodyCollider.radius, colliders, m_interactiveCollider, QueryTriggerInteraction.Ignore);
        }

        private int CharacterSweep (Vector3 position, Quaternion rotation, Vector3 direction, float distance, RaycastHit[] hitinfos)
        {
            Vector3 center = position + rotation * m_bodyCollider.center;
            Vector3 relativePoint = rotation * ((0.5f * m_bodyCollider.height - m_bodyCollider.radius) * Vector3.up);

            return Physics.CapsuleCastNonAlloc(center + relativePoint, center - relativePoint,
                m_bodyCollider.radius, direction, hitinfos, distance, m_interactiveCollider, QueryTriggerInteraction.Ignore);
        }

        private bool GetClosestHit(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            bool hasValidHit = false;
            closestHit = default;
            float closestDist = float.PositiveInfinity;

            int hitCnt = CharacterSweep(position, rotation, direction, distance, m_hitInfoBuffer);
            for (int cur = 0; cur < hitCnt; ++cur)
            {
                ref RaycastHit hit = ref m_hitInfoBuffer[cur];

                // Ignore itself collider
                if (hit.collider == m_bodyCollider)
                {
                    continue;
                }
                // Recalculate overlaped collider's hit info
                if (hit.distance <= 0.0f)
                {
                    Vector3 hitPos;
                    Quaternion hitRot;
                    if(hit.rigidbody != null)
                    {
                        hitPos = hit.rigidbody.position;
                        hitRot = hit.rigidbody.rotation;
                    }
                    else
                    {
                        hitPos = hit.transform.position;
                        hitRot = hit.transform.rotation;
                    }

                    if (Physics.ComputePenetration(
                        m_bodyCollider, position, rotation,
                        hit.collider, hitPos, hitRot,
                        out Vector3 dir, out float _))
                    {
                        hit.normal = dir;
                    }
                }
                // Only hit obstacles
                if (Vector3.Dot(direction, hit.normal) >= 0.0f)
                {
                    continue;
                }

                if (hit.distance < closestDist)
                {
                    closestHit = hit;
                    closestDist = hit.distance;
                    hasValidHit = true;
                }
            }
            return hasValidHit;
        }

        /// <summary>
        /// Depentration body collider from other interactive colliders
        /// </summary>
        private void SolveOverlap ()
        {
            int currentIteration = 0;
            while (currentIteration++ < m_depentrationIteration)
            {
                // If there are overlaped collider with character, detach character from these
                int overlapCnt = CharacterOverlap(m_nextPos, m_nextRot, m_colliderBuffer);

                for (int cur = 0; cur < overlapCnt; ++cur)
                {
                    Collider overlapCollider = m_colliderBuffer[cur];
                    // Ignore itself collider
                    if (overlapCollider == m_bodyCollider)
                    {
                        continue;
                    }
                    Vector3 overlapPos;
                    Quaternion overlapRot;

                    
                    Rigidbody overlapRigidbody = overlapCollider.attachedRigidbody;
                    if (overlapRigidbody != null)
                    {
                        overlapPos = overlapRigidbody.position + overlapRigidbody.rotation * overlapCollider.transform.localPosition;
                        overlapRot = overlapRigidbody.rotation;
                    }
                    else
                    {
                        Transform overlapTransform = overlapCollider.transform;
                        overlapPos = overlapTransform.position;
                        overlapRot = overlapTransform.rotation;
                    }

                    // Decollision if colliders are overlaped with deeper than zero
                    if (Physics.ComputePenetration(
                        m_bodyCollider, m_nextPos, m_nextRot,
                        overlapCollider, overlapPos, overlapRot,
                        out Vector3 dir, out float dist))
                    {
                        m_nextPos += dir * (dist + m_contactOffset);
                    }
                }
                // If no more found overlaped collider; stop 'Solve Overlap'
                if (overlapCnt == 0)
                {
                    break;
                }
            }
        }

        /// <summary>Move character continously</summary>
        /// <param name="distance">Distance character will move based on world unity</param>
        private void CharacterMove (Vector3 distance)
        {
            if(float.IsNaN(distance.x) || float.IsNaN(distance.y) || float.IsNaN(distance.z))
            {
                return;
            }
            if(Mathf.Approximately(distance.sqrMagnitude, 0.0f))
            {
                return;
            }

            Vector3 tgtPos = m_nextPos;
            Vector3 remainingDistance = distance;

            int currentIteration = 0;
            while (currentIteration < m_velocityIteration && remainingDistance.sqrMagnitude > 0.0f)
            {
                Vector3 remainingDirection = remainingDistance.normalized;
                float remainingMagnitude = remainingDistance.magnitude;
                
                // Check if character hit something as it move
                if (GetClosestHit(tgtPos, m_nextRot, remainingDirection, remainingMagnitude + m_contactOffset, out RaycastHit closestHit))
                {
                    closestHit.distance -= m_contactOffset;
                    tgtPos += closestHit.distance * remainingDirection;

                    remainingMagnitude = Mathf.Max(remainingMagnitude - closestHit.distance, 0.0f);
                    // Reorient movement direction based on surface
                    Vector3 tmp = Vector3.Cross(closestHit.normal, remainingDirection);
                    Vector3 tangent = Vector3.Cross(closestHit.normal, tmp).normalized;

                    if (m_state == EState.Walk)
                    {
                        remainingDistance = Vector3.Dot(tangent, remainingDirection) * remainingMagnitude * tangent;
                        Vector3 characterUp = m_nextRot * Vector3.up;

                        if (Vector3.Angle(characterUp, closestHit.normal) > m_stableAngle)
                        {
                            if(Vector3.Dot(remainingDistance, characterUp) > 0.0f)
                            {
                                remainingDistance -= Vector3.Dot(remainingDistance, characterUp) * characterUp;
                            }
                        }
                    }
                    else
                    {
                        remainingDistance = Vector3.Dot(tangent, remainingDirection) * remainingMagnitude * tangent;
                    }
                    ++currentIteration;
                }
                else
                {
                    tgtPos += remainingDistance;
                    remainingDistance = Vector3.zero;
                    break;
                }
            }

            // When Exceed velocity iteration
            if(currentIteration >= m_velocityIteration)
            {
                // Discard calculated next position from here
                if (m_killPositionWhenExceedVelocityIteration)
                {
                    tgtPos = m_initPos;
                }
                
                // Appand remained distance to destination
                if (!m_killRemainedDistanceWhenExceedVelocityIteration)
                {
                    tgtPos += remainingDistance;
                }
            }

            m_nextPos = tgtPos;
        }
    }
}