using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : Projectile  {

    [SerializeField] protected int damage = 1;
    [SerializeField] GameObject VFXPrefab;
    [SerializeField] float VFXLifeDuration;

    public int GetDamage()
    {
        return damage;
    }

    private void InstantiateVFX()
    {
        GameObject VFX = Instantiate(VFXPrefab, transform.position, Quaternion.identity) as GameObject;
        Destroy(VFX, VFXLifeDuration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponent<Player>() && !other.isTrigger)
        {
            InstantiateVFX();
            Destroy(gameObject);
        }
    }
}
