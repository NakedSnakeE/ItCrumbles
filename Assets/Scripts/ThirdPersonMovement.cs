using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMovement : MonoBehaviour
{
    #region Variables

    Animator animator;

    int isWalkingHash;

    int isIdleHash;

    PlayerControls input;

    Vector2 currentMovement;
    bool movementPressed;

    public float turnSmoothTime = 0.8f;
    float turnSmoothVelocity;

    public Transform cam;

    //Gravidade
    public float gravity = -9.81f;
    [SerializeField] private float groundDistance = 1.14f;
    [SerializeField] private LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    CharacterController characterController;

    Transform groundCheck;

    private Vector3 rightFootPosition, leftFootPosition, leftFootIKPosition, rightFootIKPosition;
    private Quaternion leftFootIKRotation, rightFootIKRotation;
    private float lastPelvisPositionY, lastRightFootPositionY, lastLeftFootPositionY;

    [Header("Feet Grounder")]
    public bool enableFeetIK = true;
    [SerializeField] private float rayCastDownDistance = 1.5f;
    [SerializeField] private float pelvisOffset = 0f;
    [SerializeField] private float pelvisUpAndDownSpeed = 0.28f;
    [SerializeField] private float feetToIKPositionSpeed = 0.5f;

    public string leftFootAnimVariableName = "LeftFootCurve";
    public string rightFootAnimVariableName = "RightFootCurve";

    public bool useProIKFeature = false;
    public bool showSolverDegub = true;

    #endregion

    #region Initialization
    void Awake()
    {
        input = new PlayerControls();
        input.Player.Move.performed += ctx => {
            //print(ctx.ReadValueAsObject());
            currentMovement = ctx.ReadValue<Vector2>();
            movementPressed = currentMovement.x != 0 || currentMovement.y != 0;
        };
        input.Player.Move.canceled += ctx => {
            currentMovement = ctx.ReadValue<Vector2>();
            movementPressed = currentMovement.x != 0 || currentMovement.y != 0;
        };
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        characterController = GetComponent<CharacterController>();

        isWalkingHash = Animator.StringToHash("Walk");
        isIdleHash = Animator.StringToHash("Idle");

        groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(transform);
        groundCheck.localPosition = new Vector3(0, -characterController.height /2, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
        handleMovement();
        handleRotation();

        handleGravity();

    }
    #endregion

    #region PlayerMovement

    void handleRotation()
    {
        Vector3 newPosition = new Vector3(currentMovement.x, 0f, currentMovement.y).normalized;

        if(newPosition.magnitude >= 0.1f){
            float targetAngle = Mathf.Atan2(newPosition.x, newPosition.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    void handleMovement()
    {
        bool isWalking = animator.GetBool(isWalkingHash);

        //print(currentMovement.x + "&" + currentMovement.y);

        if (movementPressed && !isWalking) {
            
            animator.SetBool(isWalkingHash, true);
            animator.SetBool(isIdleHash, false);
            
        }

        if (!movementPressed && isWalking) {
            
            animator.SetBool(isWalkingHash, false);
            animator.SetBool(isIdleHash, true);
            
        }

    }

    void handleGravity()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity.normalized);
    }

    void OnEnable()
    {
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }
    #endregion

    #region FeetGrounding

    ///<summary>
    /// We are updating the AjustFeetTarget method and also find the position of each foot inside our Solver Position
    ///</summary>
    private void FixedUpdate()
    {
        if (enableFeetIK == false) { return; }
        if (animator == null) { return; }

        AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
        AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

        //find and cast to the ground to find positions
        FeetPositionSolver(rightFootPosition, ref rightFootIKPosition, ref rightFootIKRotation); // handle the solver for the right foot
        FeetPositionSolver(leftFootPosition, ref leftFootIKPosition, ref leftFootIKRotation); // handle the solver for the left foot
    }

    private void OnAnimatorIK(int layerIndex){
        if(enableFeetIK == false) { return; }
        if(animator == null) { return; }

        MovePelvisHeight();

        //right foot ik position and rotation -- utilizes the pro features in here
        animator.SetIKPositionWeight (AvatarIKGoal.RightFoot, 1);

        if (useProIKFeature){
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rightFootAnimVariableName));
        }

        MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPosition, rightFootIKRotation, ref lastRightFootPositionY);

        //left foot ik position and rotation -- utilizes the pro features in here
        animator.SetIKPositionWeight (AvatarIKGoal.LeftFoot, 1);

        if (useProIKFeature){
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(leftFootAnimVariableName));
        }

        MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPosition, leftFootIKRotation, ref lastLeftFootPositionY);
    }

    #endregion

    #region FeetGroundingMethods

    ///<summary>
    /// Moves the feet to the ik point
    ///</summary>
    void MoveFeetToIKPoint (AvatarIKGoal foot, Vector3 positionIKHolder, Quaternion rotationIKHolder, ref float lastFootPositionY){
        Vector3 targetIKPosition = animator.GetIKPosition(foot);

        if(positionIKHolder != Vector3.zero){
            targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
            positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

            float yVariable = Mathf.Lerp(lastFootPositionY, positionIKHolder.y, feetToIKPositionSpeed);
            targetIKPosition.y += yVariable;

            lastFootPositionY = yVariable;

            targetIKPosition = transform.TransformPoint(targetIKPosition);

            animator.SetIKRotation(foot, rotationIKHolder);
        }

        animator.SetIKPosition (foot, targetIKPosition);
    }

    ///<summary>
    /// Moves the height of the pelvis
    ///</summary>
    private void MovePelvisHeight(){
        if(rightFootIKPosition == Vector3.zero || leftFootIKPosition == Vector3.zero || lastPelvisPositionY == 0){
            lastPelvisPositionY = animator.bodyPosition.y;
            return;
        }

        float lOffsetPosition = leftFootIKPosition.y - transform.position.y;
        float rOffsetPosition = rightFootIKPosition.y - transform.position.y;

        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;

        Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset;

        newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);

        animator.bodyPosition = newPelvisPosition;

        lastPelvisPositionY = animator.bodyPosition.y;
    }

    ///<summary>
    /// We are locating the feet position via a Raycast and then solving
    ///</summary>
    private void FeetPositionSolver(Vector3 fromSkyPosition, ref Vector3 feetIKPosition, ref Quaternion feetIKRotation){
        //raycast handling section
        RaycastHit feetOutHit;

        if (showSolverDegub)
            Debug.DrawLine(fromSkyPosition, fromSkyPosition + Vector3.down * (rayCastDownDistance + groundDistance), Color.yellow);
    
        if (Physics.Raycast(fromSkyPosition, Vector3.down, out feetOutHit, rayCastDownDistance + groundDistance, groundMask)){
            //finding our feet ik positions from the sky position
            feetIKPosition = fromSkyPosition;
            feetIKPosition.y = feetOutHit.point.y + pelvisOffset;
            feetIKRotation = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation;

            return;
        }

        feetIKPosition = Vector3.zero; //it didn't work :(
    }

    ///<summary>
    /// Adjust the feet target
    ///</summary>
    private void AdjustFeetTarget (ref Vector3 feetPositions, HumanBodyBones foot){
        feetPositions = animator.GetBoneTransform(foot).position;
        feetPositions.y = transform.position.y + groundDistance;
    }

    #endregion

    /*private void OnDrawGizmos(){

    }*/
}
