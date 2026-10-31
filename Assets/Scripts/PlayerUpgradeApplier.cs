using UnityEngine;

public class PlayerUpgradeApplier : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private Health health;
    [SerializeField] private PlayerPickupRange pickupRange;

    public int EquippedWeaponCount => shooter.EquippedCount;
    public int WeaponSlotCapacity => shooter.WeaponSlotCapacity;
    public float HealthRatio => health.Max > 0f
        ? Mathf.Clamp01(health.Current / health.Max)
        : 0f;

    public bool CanApply(PlayerUpgradeData data)
    {
        if (data == null)
        {
            return false;
        }

        switch (data.EffectType)
        {
            case UpgradeEffectType.Damage:
                return shooter.CanIncreaseDamage;
            case UpgradeEffectType.FireRate:
                return shooter.CanIncreaseAttackSpeed;
            case UpgradeEffectType.Heal:
                return !health.IsDead && !health.IsFull;
            case UpgradeEffectType.MoveSpeed:
                return movement.CanIncreaseMoveSpeed;
            case UpgradeEffectType.MaxHealth:
                return true;
            case UpgradeEffectType.AttackRange:
                return shooter.CanIncreaseRange;
            case UpgradeEffectType.PickupRange:
                return pickupRange.CanIncrease;
            case UpgradeEffectType.EquipWeapon:
                return shooter.CanEquip(data.Weapon);
            default:
                return false;
        }
    }

    public void Apply(PlayerUpgradeData data)
    {
        switch (data.EffectType)
        {
            case UpgradeEffectType.Damage:
                shooter.AddDamageMultiplier(data.Value);
                break;
            case UpgradeEffectType.FireRate:
                shooter.AddAttackSpeedMultiplier(data.Value);
                break;
            case UpgradeEffectType.Heal:
                health.Heal(data.Value);
                break;
            case UpgradeEffectType.MoveSpeed:
                movement.AddMoveSpeed(data.Value);
                break;
            case UpgradeEffectType.MaxHealth:
                health.AddMaxHealth(data.Value);
                break;
            case UpgradeEffectType.AttackRange:
                shooter.AddRangeMultiplier(data.Value);
                break;
            case UpgradeEffectType.PickupRange:
                pickupRange.AddRadius(data.Value);
                break;
            case UpgradeEffectType.EquipWeapon:
                shooter.EquipWeapon(data.Weapon);
                break;
        }
    }

    public int GetWeaponCount(PlayerUpgradeData data)
    {
        return data != null && data.Weapon != null
            ? shooter.GetEquippedCount(data.Weapon.WeaponType)
            : 0;
    }
}
