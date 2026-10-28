namespace InvaderOfWorlds
{
    /// <summary>Всё, что может получить урон: игрок, враги, разрушаемые объекты.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount);
    }
}
