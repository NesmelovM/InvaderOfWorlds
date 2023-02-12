
using UnityEngine;

public abstract class Enemy
{
    protected int Health;
    protected int Armor;

    protected float RangeofAttack;
    protected int Damage;
    protected float ChanceCriticalDamage;

    protected float MoveSpeed;

    protected int CostWorldPower;
    protected bool IsAbleOfAttack;

    protected Enemy(int costWorldPower)
    {
        Health = (int)(100 * Mathf.Pow(1.2f, costWorldPower));
        Armor = (int)(10 * Mathf.Pow(1.2f, costWorldPower));

        Damage = (int)(10 * Mathf.Pow(1.2f, costWorldPower));
        ChanceCriticalDamage = (3f * Mathf.Pow(1.2f, costWorldPower));

        MoveSpeed = 5f * Mathf.Pow(1.2f, costWorldPower);
    }

    protected virtual void GetTargetDirection()
    {
        Debug.Log("Get target direction");
    }   
}


