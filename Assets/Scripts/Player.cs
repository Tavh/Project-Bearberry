using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class Player : MonoBehaviour {

    // ------------------------------------------ Class variables ------------------------------------------

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Configuration parameters ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // Player health
    [SerializeField] int health = 5;
    // Player run speed
    [SerializeField] float runSpeed = 5f;
    // Player jump force
    [SerializeField] float jumpForce = 5f;
    // Player jump force when jumping off a rope
    [SerializeField] float jumpForceFromRope;
    // At what falling speed should the player switch to falling animation
    [SerializeField] float speedThresholdForFalling = -0.5f;
    // At what distance from the ground should the player switch to falling animation
    [SerializeField] float distanceThresholdForFalling = 0.3f;
    // Cooldown duration after jumping
    [SerializeField] float jumpCooldown = 0.3f;
    // Cooldown duration after shooting
    [SerializeField] float shotCooldown = 1f;
    // How hard should the player be knocked upwards when shooting downwards while airborne
    [SerializeField] float knockUpwardsFactor = 18;
    // How hard should the player be knocked back when shooting
    [SerializeField] float shootKnockBackFactor = 0.2f;
    // How hard should the player be knocked back when shooting while airborne
    [SerializeField] float knockbackFactorAir = 0.2f;
    // Cooldown for the knock upwards the player receives when shooting downwards while airborne
    [SerializeField] float knockUpwardsCooldown = 0.3f;
    // Cooldown for climbing, prevents bugs and loops
    [SerializeField] float climbCoolDown = 0.3f;
    // How far from a wall should the character stop being knocked back when shooting
    [SerializeField] float distanceToRaycastKnockback = 0.5f;
    // How fast the character climbs
    [SerializeField] float climbingSpeed;
    // Keeps the direction a projectile should fly at a given time
    Vector2 bulletDirection;
    // Keeps the center of the rope the character is climbing
    Vector2 ropeCenter;
    // Padding the received rope center to improve the visuals
    [SerializeField] float ropeCenterPadding;
    // Minimal distance from ground to determine if the character is grounded
    [SerializeField] float minimalDistanceFromGround;
    // How long the player is immune from hits after being hit
    [SerializeField] float invincibilityDuration = 2f;

    [SerializeField] float countJumpAsExecutableDuration = 0.15f;
    [SerializeField] float countShotAsExecutableDuration = 0.15f;


    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ GameObject references ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    [SerializeField] Projectile bulletPrefab;
    [SerializeField] GameObject gun;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Saved references ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // Components
    Rigidbody2D myRigidBody;
    Animator myAnimator;
    Collider2D myCollider2D;

    // Buttons
    bool rightButton;
    bool leftButton;
    bool altRightButton;
    bool altLeftButton;
    bool downButton;
    bool altDownButton;
    bool upButton;
    bool altUpButton;
    bool jumpButton;
    bool shotButton;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Character status & cooldown ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // If exposed, only for observation
    [SerializeField] bool isBulletAvailable;
    [SerializeField] bool isJumpAvailable;
    [SerializeField] bool isShootingAvailable;
    [SerializeField] bool isShooting;
    [SerializeField] bool isKnockUpwardsAvailable;
    [SerializeField] bool isClimbingAvailable;
    [SerializeField] bool isClimbing;
    [SerializeField] bool isInvincible;
    [SerializeField] bool isHit;
    [SerializeField] bool isGrounded;

    [SerializeField] ArrayList jumpQueue = new ArrayList();
    [SerializeField] ArrayList shotQueue = new ArrayList();

    // ---------------------------------------------------- Methods -------------------------------------------------------------

    void Start ()
    {
        isHit = false;
        isClimbingAvailable = true;
        isShooting = false;
        isJumpAvailable = true;
        isShootingAvailable = true;
        isKnockUpwardsAvailable = true;
        isBulletAvailable = true;
        myAnimator = GetComponent<Animator>();
        myRigidBody = GetComponent<Rigidbody2D>();
        myCollider2D = GetComponent<Collider2D>();
    }

    void FixedUpdate ()
    {
        isGrounded = IsGrounded();

        CheckButtonInput();

        Move();
        Jump();
        Fall();
        Shoot();
        Crouch();
        HandleSlope();
        ClimbRope();

        SetIsGroundedInAnimator();
    }

    private void CheckButtonInput()
    {
        rightButton = Input.GetKey(KeyCode.RightArrow);
        leftButton = Input.GetKey(KeyCode.LeftArrow);
        altRightButton = Input.GetKey(KeyCode.D);
        altLeftButton = Input.GetKey(KeyCode.A);
        downButton = Input.GetKey(KeyCode.DownArrow);
        altDownButton = Input.GetKey(KeyCode.S);
        upButton = Input.GetKey(KeyCode.UpArrow);
        altUpButton = Input.GetKey(KeyCode.W);
        jumpButton = Input.GetKeyDown(KeyCode.Z);
        shotButton = Input.GetKeyDown(KeyCode.X);

        StartCoroutine(ManageJumpQueue());
        StartCoroutine(ManageShotQueue());
    }
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ GETTERS AND SETTERS ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // Always checks the player's input and flips the sprite towards the right direction
    private void FlipSprite()
    {
        if (!isClimbing)
        {
            if ((rightButton || altRightButton) && !leftButton && !altLeftButton)
            {
                transform.localScale = new Vector2(1f, 1f);
            }
            else if ((leftButton || altLeftButton) && !rightButton && !altRightButton)
            {
                transform.localScale = new Vector2(-1f, 1f);
            }
        }

        /*
        bool playerHasHorizontalVelocity = Mathf.Abs(myRigidBody.velocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalVelocity)
        {
            Debug.Log(myRigidBody.velocity);
            transform.localScale = new Vector2(Mathf.Sign(myRigidBody.velocity.x), 1f);
            return;
        } */
    }

    // Detects if the player is touching the ground or not
    private bool IsGrounded()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(transform.position, Vector2.down);

        bool isTouchingGround = myCollider2D.IsTouchingLayers(LayerMask.GetMask("Ground"));

        if (isTouchingGround && rayCastHit.distance < this.minimalDistanceFromGround)
        {
            return true;
        }

        return false;
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ MOVEMENT ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Character grounded movement
    private void Move()
    {
        if (!isShooting && !isHit)
        {
            FlipSprite();
            float controlThrow = CrossPlatformInputManager.GetAxis("Horizontal"); // value is between 1 and -1
            Vector2 playerVelocity = new Vector2(controlThrow * runSpeed, myRigidBody.velocity.y);
            myRigidBody.velocity = playerVelocity;
            myAnimator.SetFloat("speed", Mathf.Abs(controlThrow));

            if (IsGrounded())
            {
                bool playerHasHorizontalVelocity = Mathf.Abs(myRigidBody.velocity.x) > Mathf.Epsilon;
                myAnimator.SetBool("isRunning", playerHasHorizontalVelocity);
            }
        }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ JUMP ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Jumps only if grounded
    private void Jump()
    {
        if (IsGrounded() && isJumpAvailable)
        {
            if (jumpQueue.Count > 0)
            {
                var jumpVelocity = new Vector2(0f, jumpForce);
                myRigidBody.velocity = jumpVelocity;
                myAnimator.SetBool("isRunning", false);
                myAnimator.SetBool("isJumping", true);
                StartCoroutine(JumpCoolDown());
                jumpQueue = new ArrayList();
            }
        }
    }

    private IEnumerator ManageJumpQueue()
    {
        if (jumpButton)
        {
            object jumpAttempt = new object();
            jumpQueue.Add(jumpAttempt);
            yield return new WaitForSeconds(countJumpAsExecutableDuration);
            jumpQueue.Remove(jumpAttempt);
        }
    }

    // Makes sure the player cannot double jump
    private IEnumerator JumpCoolDown()
    {
        isJumpAvailable = false;
        yield return new WaitForSeconds(jumpCooldown);
        isJumpAvailable = true;
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ FALL ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Detects if the player is falling
    private void Fall ()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(transform.position, Vector2.down);
        
        if (myRigidBody.velocity.y < speedThresholdForFalling && !IsGrounded() && !isClimbing && rayCastHit.distance > distanceThresholdForFalling)
        {
            // Debug.Log("Player rigidbody velocity : " + myRigidBody.velocity);
            myAnimator.SetBool("isRunning", false);
            myAnimator.SetBool("isJumping", false);
            myAnimator.SetBool("isFalling", true);
        }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ LAND ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // Detects if the player landed on an object that is tagged "Ground"
    private void Land()
    {
        myAnimator.SetBool("isShootingAirborneDownwards", false);
        myAnimator.SetBool("isShootingAirborne", false);
        myAnimator.SetBool("isFalling", false);
        myAnimator.SetBool("isJumping", false);
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ CROUCH ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private void Crouch()
    {
        if (downButton || altDownButton && IsGrounded())
        {
            myAnimator.SetBool("isCrouching", true);
        }
        else
        {
            myAnimator.SetBool("isCrouching", false);
        }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ CLIMB ROPE ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private void ClimbRope()
    {
        bool isTouchingLadder = myCollider2D.IsTouchingLayers(LayerMask.GetMask("Rope"));

        bool isShootingAirborne = myAnimator.GetBool("isShootingAirborne");
        bool isShootingAirborneDownwards = myAnimator.GetBool("isShootingAirborneDownwards");

        if(isTouchingLadder && !isShooting && !isShootingAirborne && !isShootingAirborneDownwards && isClimbingAvailable)
        {
            JumpOffRope();

            if ((upButton || altUpButton) && !downButton && !altDownButton && !IsGrounded())
            {
                StartClimbing();
                myRigidBody.velocity = new Vector2(0, climbingSpeed);
            }
            else if ((downButton || altDownButton) && !upButton && !altUpButton && (IsGrounded() || isClimbing))
            {
                StartClimbing();
                myRigidBody.velocity = new Vector2(0, -climbingSpeed);
            }
            else if(isClimbing)
            {
                myRigidBody.velocity = new Vector2(0, 0);
                myAnimator.enabled = false;
            }
        }
        else
        {
            myAnimator.SetBool("isClimbing", false);
            isClimbing = false;
            StartCoroutine(ClimbCoolDown());
            myRigidBody.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void JumpOffRope()
    {
        if (isClimbing && jumpButton && !upButton && !downButton && (((leftButton || altLeftButton) || (rightButton || altRightButton))))
        {
            StartCoroutine(ClimbCoolDown());
            myAnimator.enabled = true;
            isClimbing = false;
            myAnimator.SetBool("isJumping", true);
            FlipSprite();
            myAnimator.SetBool("isClimbing", false);
            myRigidBody.bodyType = RigidbodyType2D.Dynamic;
            var jumpVelocity = new Vector2(0f, jumpForceFromRope);
            myRigidBody.velocity = jumpVelocity;
        }
    }
    // Used when the player initializes rope climbing
    private void StartClimbing()
    {
        myRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        transform.position = new Vector2(ropeCenter.x - ropeCenterPadding * Mathf.Sign(transform.localScale.x), transform.position.y);
        isClimbing = true;
        myAnimator.enabled = true;
        myRigidBody.bodyType = RigidbodyType2D.Kinematic;
        myAnimator.SetBool("isJumping", false);
        myAnimator.SetBool("isRunning", false);
        myAnimator.SetBool("isClimbing", true);
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ SHOOT ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private void Shoot()
    {
        bool isFalling = myAnimator.GetBool("isFalling");

        if (isShootingAvailable && !isClimbing)
        {
            // Case - shoot on ground
            if (shotQueue.Count > 0 && IsGrounded()) // && Mathf.Abs(velocity) < Mathf.Epsilon)
            {
                StartCoroutine(JumpCoolDown());
                StartCoroutine(ShotCoolDown());
                StopMovement();
                myAnimator.SetBool("isShooting", true);
                shotQueue = new ArrayList();
            }

            // Case - shoot in air downwards
            else if ((downButton || altDownButton) && shotQueue.Count > 0 && !IsGrounded())
            {
                StartCoroutine(ShotCoolDown());
                myAnimator.SetBool("isShootingAirborneDownwards", true);
                shotQueue = new ArrayList();
            }

            // Case - shoot in air
            else if (shotQueue.Count > 0 && !IsGrounded() && !isFalling)
            {
                StartCoroutine(ShotCoolDown());
                myAnimator.SetBool("isShootingAirborne", true);
                shotQueue = new ArrayList();
            }
        }
    }

    private IEnumerator ManageShotQueue()
    {
        if (shotButton)
        {
            object shotAttempt = new object();
            shotQueue.Add(shotAttempt);
            yield return new WaitForSeconds(countShotAsExecutableDuration);
            shotQueue.Remove(shotAttempt);
        }
    }

    public void ReceiveKnockback()
    {
        // Shouldn't be too high or you might teleport into walls
        var dir = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -dir);
        if (hit.collider == null || hit.distance > distanceToRaycastKnockback)
        {
            if (!IsGrounded())
            {
                transform.Translate(dir * -knockbackFactorAir * Time.deltaTime);
                return;
            }

            transform.Translate(dir * -shootKnockBackFactor * Time.deltaTime);
        }
    }

    public void KnockUpwards ()
    {
        if (isKnockUpwardsAvailable)
        {
            myRigidBody.velocity += new Vector2(0, knockUpwardsFactor);
            StartCoroutine(KnockUpwardsCoolDown());
        }
    }

    IEnumerator KnockUpwardsCoolDown()
    {
        isKnockUpwardsAvailable = false;
        yield return new WaitForSeconds(knockUpwardsCooldown);
        isKnockUpwardsAvailable = true;
    }

    IEnumerator ShotCoolDown()
    {
        isShootingAvailable = false;
        yield return new WaitForSeconds(shotCooldown);
        isShootingAvailable = true;
    }

    IEnumerator ClimbCoolDown()
    {
        isClimbingAvailable = false;
        yield return new WaitForSeconds(climbCoolDown);
        isClimbingAvailable = true;
    }

    IEnumerator BulletSpawnCoolDown()
    {
        isBulletAvailable = false;
        yield return new WaitForSeconds(shotCooldown);
        isBulletAvailable = true;
    }

    public void SetShootingTrue()
    {
        StopMovement();
        isShooting = true;
    }

    public void SetShootingAirborneFalse()
    {
        myAnimator.SetBool("isShootingAirborne", false);
    }

    public void SetShootingAirborneDownwardsFalse()
    {
        myAnimator.SetBool("isShootingAirborneDownwards", false);
    }

    public void SetShootingFalse()
    {
        isShooting = false;
        myAnimator.SetBool("isShooting", false);
    }

    private void SetBulletDirectionHorizontal()
    {
        bulletDirection = new Vector2((Mathf.Sign(transform.localScale.x)), 0);
    }

    private void SetBulletDirectionAirborneDownwards()
    {
        bulletDirection = new Vector2(0, -1);
    }

    private void InstantiateBullet()
    {
        if (isBulletAvailable)
        {
            StartCoroutine(BulletSpawnCoolDown());
            PlayerBullet bullet = Instantiate(bulletPrefab, gun.transform.position, Quaternion.identity) as PlayerBullet;
            bullet.SetDireciton(bulletDirection);
        }
    }

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public void SetIsGroundedInAnimator()
    {
        myAnimator.SetBool("isGrounded", IsGrounded());
    }

    private void StopMovement()
    {
        myAnimator.SetBool("isRunning", false);
        myRigidBody.velocity = new Vector2(0f, 0f);
    }

    // Prevents movement altogether when the player is on a slope and doesn't push any key
    private void HandleSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down);

        // The angle of the ground
        float angle = Vector2.Angle(hit.normal, Vector2.up);

        bool isJumping = myAnimator.GetBool("isJumping");

        if (IsGrounded() && angle >= Mathf.Abs(1) && !isJumping
                    && (Mathf.Abs(CrossPlatformInputManager.GetAxis("Horizontal")) <= Mathf.Epsilon || isShooting == true))
        {
            myRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            myRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void HandleHit(int damage)
    {
        if (!isInvincible)
        {
            isHit = true;
            health -= damage;
            myAnimator.SetTrigger("hit");
            HandleDeath();
            StartCoroutine(invincibilityCoolDown());
        }
    }

    public void StopHit()
    {
        isHit = false;
    }

    IEnumerator invincibilityCoolDown()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void HandleDeath()
    {
        if (health <= 0)
        {
            Debug.Log("I'm dead");
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var otherGameObject = other.gameObject;

        if (otherGameObject.GetComponent<Hostile>())
        {
            HandleHit(otherGameObject.GetComponent<Hostile>().GetDamage());
        }

        if (otherGameObject.tag.Equals("Ground"))
        {
            Land();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var otherGameObject = other.gameObject;

        // Keeps the rope's position
        if (otherGameObject.tag.Equals("Rope"))
        {
            ropeCenter = other.gameObject.transform.position;
        }
    }
}
