using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hostile : MonoBehaviour {

    [SerializeField] protected int damage = 1;

    public int GetDamage()
    {
        return damage;
    }
}
