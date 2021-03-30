using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TowerDefense.Units;
using UnityEngine;

namespace TowerDefense.Towers
{
    // Each of this tower's attack has a chance to create multiple additional attacks target random enemies
    public class UpgradeApocalypse : Upgrade
    {
	    bool _isApocalypseActive;   // is this effect currently active?

        public override void AddUpgrade()
        {
	        TowerBase.TowerEvents.OnAttackInitializedAction += Apocalypse;
        }

        private void Apocalypse(Attack attack)
        {
	        // will not proc from fireballs created from apocalypse
            if (_isApocalypseActive) return;

            // roll the proc chance
            if (Random.Range(0, 100) < UpgradeData.GetValue(0))
                StartCoroutine(ApocalypseAttack(attack));
        }

        IEnumerator ApocalypseAttack(Attack attack)
		{
            _isApocalypseActive = true;

            Base target = ((ProjectileFireball)attack).Target;
            TowerUnit tower = (TowerUnit)attack.Attacker.Unit;

            // find all enemies within range of the target enemy
            List<Collider> targetsFound = Physics.OverlapSphere(target.transform.position, tower.TowerAttributes.Range, tower.SearchLayer).ToList();

            // remove the primary target from the list of enemies found so it isn't targeted again
            targetsFound.Remove(target.Unit.Components.SearchCollider);

            // generate a fireball for each target found, up to the rank's maximum amount
            var fireballsGenerated = 0;
            foreach (var bonusTarget in targetsFound)
            {
	            // stagger the creation of fireballs so the projectiles don't overlap
                yield return new WaitForSeconds(0.2f);

                // ensure the target still exists (wasn't removed by hitting the nexus or being killed)
                if (bonusTarget == null)
		            continue;

                // create the fireball attack
	            var fireball = Instantiate(tower.Components.AttackPrefab, tower.Components.AttackSpawnLocation.position, Quaternion.identity, tower.transform).GetComponent<ProjectileFireball>();
	            fireball.InitializeAttack(attack.Attacker, tower.TowerAttributes.Damage, UI.CombatTextType.SpecialDamage);
	            fireball.InitializeProjectile(bonusTarget.GetComponent<UnitCollider>().Base, tower.TowerAttributes.ProjectileSpeed);
	            fireball.InitializeFireball(tower.TowerAttributes["explosionSize"].AttributeValue / 10);

                // stop generating fireballs when it reaches its rank's maximum
                fireballsGenerated++;
                if (fireballsGenerated == (UpgradeData.GetValue(1) - 1))
		            break;
            }

            _isApocalypseActive = false;
        }
    }
}
