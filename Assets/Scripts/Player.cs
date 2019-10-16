﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class Player : MonoBehaviour {

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Configuration parameters ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    [SerializeField] int health = 5;
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float jumpForceFromRope;
    // At what falling speed should the player switch to falling animation
    [SerializeField] float speedThresholdForFalling = -0.5f;
    // At what distance from the ground should the player switch to falling animation
    [SerializeField] float distanceThresholdForFalling = 0.3f;
    [SerializeField] float jumpCooldown = 0.3f;
    [SerializeField] float shotCooldown = 1f;
    [SerializeField] float knockUpwardsFactor = 18;
    [SerializeField] float shootKnockBackFactor = 0.2f;
    [SerializeField] float knockbackFactorAir = 0.2f;
    [SerializeField] float knockUpwardsCooldown = 0.3f;
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

    [SerializeField] float maxTimeToKeepTrackOfLastJumpAttempt = 0.15f;
    [SerializeField] float maxTimeToKeepTrackOfLastShotAttempt = 0.15f;


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

    [SerializeField] float timePassedSinceShotPressed;
    [SerializeField] float timePassedSinceJumpPressed;


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
    }
    private void FlipSprite()
    {
        if (!isClimbing)
        {
            bool isPressingRight = rightButton || altRightButton;
            bool isPressingLeft = leftButton || altLeftButton;

            if (isPressingRight && !isPressingLeft)
            {
                transform.localScale = new Vector2(1f, 1f);
            }
            else if (isPressingLeft && !isPressingRight)
            {
                transform.localScale = new Vector2(-1f, 1f);
            }
        }
    }

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
                bool isPressingMoveButtons = leftButton || altLeftButton || rightButton || altRightButton;
                myAnimator.SetBool("isRunning", playerHasHorizontalVelocity || isPressingMoveButtons);
            }
        }
    }

    private void Jump()
    {
        timePassedSinceJumpPressed -= Time.deltaTime;
        if (jumpButton)
        {
            timePassedSinceJumpPressed = maxTimeToKeepTrackOfLastJumpAttempt;
        }

        if (IsGrounded() && isJumpAvailable) 
        {
            if (timePassedSinceJumpPressed > 0)
            {
                var jumpVelocity = new Vector2(0f, jumpForce);
                myRigidBody.velocity = jumpVelocity;
                myAnimator.SetBool("isRunning", false);
                myAnimator.SetBool("isJumping", true);
                StartCoroutine(JumpCoolDown());
                timePassedSinceJumpPressed = 0;
            }
        }
    }

    // Makes sure the player cannot double jump
    private IEnumerator JumpCoolDown()
    {
        isJumpAvailable = false;
        yield return new WaitForSeconds(jumpCooldown);
        isJumpAvailable = true;
    }

    // Detects if the player is falling
    private void Fall ()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(transform.position, Vector2.down);
        
        if (myRigidBody.velocity.y < speedThresholdForFalling && !IsGrounded() && !isClimbing && rayCastHit.distance > distanceThresholdForFalling)
        {
            myAnimator.SetBool("isRunning", false);
            myAnimator.SetBool("isJumping", false);
            myAnimator.SetBool("isFalling", true);
        }
    }

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
        bool isHoldingEitherRightOrLeftButton = ((leftButton || altLeftButton) ^ (rightButton || altRightButton));
        bool isHoldingUpOrDownButton = upButton || downButton;

        if (isClimbing && jumpButton && !isHoldingUpOrDownButton && isHoldingEitherRightOrLeftButton)
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
            timePassedSinceJumpPressed = 0;
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

    private void Shoot()
    {
        bool isFalling = myAnimator.GetBool("isFalling");
        bool isJumping = myAnimator.GetBool("isJumping");

        timePassedSinceShotPressed -= Time.deltaTime;
        if (shotButton)
        {
            timePassedSinceShotPressed = maxTimeToKeepTrackOfLastShotAttempt;
        }

        if (isShootingAvailable && !isClimbing)
        {
            // Shoot on ground
            if (timePassedSinceShotPressed > 0 && IsGrounded() && !isJumping) // && Mathf.Abs(velocity) < Mathf.Epsilon)
            {
                StartCoroutine(JumpCoolDown());
                StartCoroutine(ShotCoolDown());
                StopMovement();
                myAnimator.SetBool("isShooting", true);
                timePassedSinceShotPressed = 0;
            }

            // Shoot in air downwards
            else if ((downButton || altDownButton) && timePassedSinceShotPressed > 0 && !IsGrounded())
            {
                StartCoroutine(ShotCoolDown());
                myAnimator.SetBool("isShootingAirborneDownwards", true);
                timePassedSinceShotPressed = 0;
            }

            // Shoot in air
            else if (timePassedSinceShotPressed > 0 && !IsGrounded() && !isFalling)
            {
                StartCoroutine(ShotCoolDown());
                myAnimator.SetBool("isShootingAirborne", true);
                timePassedSinceShotPressed = 0;
            }
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
