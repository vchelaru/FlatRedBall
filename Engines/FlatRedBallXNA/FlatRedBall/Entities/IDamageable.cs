using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;

namespace FlatRedBall.Entities
{
    public interface IDamageable
    {
        Dictionary<IDamageArea, double> DamageAreaLastDamage { get; }
        int TeamIndex { get; }
    }

    public static class DamageableExtensionMethods
    {
        public static bool ShouldTakeDamage(this IDamageable damageable, IDamageArea damageArea)
        {
            if(damageable.TeamIndex == damageArea.TeamIndex)
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
    }
}
