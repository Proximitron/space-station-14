// ReSharper disable CheckNamespace
namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    /// <summary>
    /// Handles the remote heavy-attack input at the requested map position.
    /// </summary>
    /// <returns>
    /// Returns the success of the function if consumed and handled.
    /// </returns>
    public bool CanHandleRemoteHeavyAttack(EntityUid user)
    {
        if (!CombatMode.IsInCombatMode(user)
            || !TryGetWeapon(user, out var weaponUid, out var weapon))
            return false;

        return CanUseRemoteHeavyAttackWeapon(user, weaponUid, weapon);
    }
}
