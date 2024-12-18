using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DestroyableObject : NetworkBehaviour, IDamageable
{
    [SerializeField] int HealthMax = 2;
    int Health;
    private void Start()
    {
        SetMaxHP();
    }
    [Server]
    private void SetMaxHP()
    {
        Health = HealthMax;
    }
    [Server]
    public void Damage(int amount, uint shooterID)
    {
        Health -= amount;
        if (Health < 1)
        {
            Die();
        }
    }
    [Server]
    public void Die()
    {
        Destroy(gameObject);
    }
}
