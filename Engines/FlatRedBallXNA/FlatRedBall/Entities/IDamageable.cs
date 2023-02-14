using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FlatRedBall;

namespace FlatRedBall.Entities
{
    public interface IDamageable
    {
        Dictionary<IDamageArea, double> DamageAreaLastDamage { get; }
        int TeamIndex { get; }
        decimal CurrentHealth { get; set; }
        decimal MaxHealth { get; set; }

        Func<decimal, IDamageArea, decimal> ModifyDamageReceived { get; set; }
        Action<decimal, IDamageArea> ReactToDamageReceived { get; set; }
        Action<decimal, IDamageArea> Died { get; set; }


    }

    public static class DamageableExtensionMethods
    {
        /// <summary>
        /// Returns whether the argument IDamageable should take damage from the argument IDamageArea.
        /// This returns true if the team indexes are different, ifthe damageable has > 0 CurrentHealth,
        /// and if enough time has passed since the last damage was dealt by this particular IDamageArea instance. 
        /// </summary>
        /// <param name="damageable">The damageable object, typically a Player or Enemy.</param>
        /// <param name="damageArea">The damage dealing object, typically a bullet or enemy.</param>
        /// <returns></returns>
        public static bool ShouldTakeDamage(this IDamageable damageable, IDamageArea damageArea)
        {
            if (damageable.TeamIndex == damageArea.TeamIndex || damageable.CurrentHealth <= 0)
            {
                return false;
            }
            else if (damageable.DamageAreaLastDamage.ContainsKey(damageArea) == false)
            {
                // This is the first time the player has collided with this
                // damage area, so deal damage and record the time in the damageAreaLastDamage
                // dictionary.
                damageable.DamageAreaLastDamage.Add(damageArea, TimeManager.CurrentScreenTime);

                // Remove the damage area from the dictionary when it is destroyed or else
                // the Player may accumulate a large collection of damage areas, resulting in
                // an accumulation memory leak.
                damageArea.Destroyed += () => damageable.DamageAreaLastDamage.Remove(damageArea);
                return true;
            }
            else
            {
                // See when the last time damage was dealt...
                var lastDamage = damageable.DamageAreaLastDamage[damageArea];

                // ... has enough time passed?
                if (FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(lastDamage) > damageArea.SecondsBetweenDamage)
                {
                    // If so, update the last damage time.
                    damageable.DamageAreaLastDamage[damageArea] = TimeManager.CurrentScreenTime;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static decimal TakeDamage(this IDamageable damageable, IDamageArea damageArea)
        {
            // The DamageArea provides the damage, so the order should be:
            // 1. Damageable modifies
            // 2. DamageArea modifies
            // 3. Damageable CurrentHealth -= 
            // 4. Damageable.ReactToDamageReceived
            // 5. DamageArea.ReactToDamageDealt
            // --if Damageable.CurrentHealth <= 0
            // 6. Damageable.Died
            // 7. DamageArea.KilledDamageable

            // do we destroy? I think ....hm...

            // damageArea could be null, in the case of the player taking damage from something that isn't IDamageArea like a TileShapeCollection, so be sure to do null checks

            var damage = damageArea.DamageToDeal;

            var modifiedByDamageable = damageable.ModifyDamageReceived?.Invoke(damage, damageArea) ?? damage;
            var modifiedByBoth = damageArea.ModifyDamageDealt?.Invoke(damage, damageable) ?? modifiedByDamageable;

            var healthBefore = damageable.CurrentHealth;

            if (modifiedByBoth != 0)
            {
                damageable.CurrentHealth -= modifiedByBoth;
            }

            // We used to not raise events when taking 0 damage, but we may want
            // to have some kind of logic play when taking 0 damage, likeplay a sound
            // effect to indicate that this is not a spot that an enemy can get hit.
            damageable.ReactToDamageReceived?.Invoke(modifiedByBoth, damageArea);
            damageArea.ReactToDamageDealt?.Invoke(modifiedByBoth, damageable);

            if(healthBefore > 0 && damageable.CurrentHealth <= 0)
            {
                damageable?.Died?.Invoke(modifiedByBoth, damageArea);
                damageArea?.KilledDamageable?.Invoke(modifiedByBoth, damageable);
            }
            

            return modifiedByBoth;
        }

        // There could be situations where an object takes damage from something (like a tile shape collection) which
        // is not an IDamageDealer.
        // Vic considered making a version of this method that takes an object parameter, but this requires delegates
        // that also take object. Then...does the player have to implement both to handle being dealt damage from IDamageable
        // and object? That's a pain and confusing. So, we'll just keep it on IDamageable for now, and make it easier to create
        // wrappers for common types like TileShapeCollection.
        public static void TakeDamage(this IDamageable damageable, decimal damage)
        {
            var modifiedByDamageable = damageable.ModifyDamageReceived?.Invoke(damage, null) ?? damage;
            //var modifiedByBoth = damageArea.ModifyDamageDealt?.Invoke(damage, damageable) ?? modifiedByDamageable;

            var healthBefore = damageable.CurrentHealth;

            damageable.CurrentHealth -= modifiedByDamageable;

            damageable.ReactToDamageReceived?.Invoke(modifiedByDamageable, null);
            //damageArea.ReactToDamageDealt?.Invoke(modifiedByBoth, damageable);

            if (healthBefore > 0 && damageable.CurrentHealth <= 0)
            {
                damageable.Died?.Invoke(modifiedByDamageable, null);
                //damageArea.KilledDamageable?.Invoke(modifiedByBoth, damageable);
            }
        }


    }
}
