using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Hostile {

    // ------------------------------------------ Class variables ------------------------------------------

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Configuration parameters ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // Enemy health
    [SerializeField] int health = 7;

    // Component references
    Animator myAnimator;
    Rigidbody2D myRigidBody;
    Collider2D myCollider;

    private void Start()
    {
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void HandleHit(int damage)
    {
        if (health >= 0)
        {
            health -= damage;
            myAnimator.SetTrigger("hit");
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (health <= 0)
        {
            myRigidBody.bodyType = RigidbodyType2D.Kinematic;
            myCollider.enabled = false;
            myAnimator.SetTrigger("die");
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var otherGO = other.gameObject;

        if(otherGO.GetComponent<PlayerBullet>())
        {
            HandleHit(otherGO.GetComponent<PlayerBullet>().GetDamage());
        }
    }
}
