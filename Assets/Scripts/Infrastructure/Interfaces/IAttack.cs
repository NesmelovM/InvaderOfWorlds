using UnityEngine;

interface IAttack
{
    void Attack(int damage)
    {
        Debug.Log("Attack");
    }
}
