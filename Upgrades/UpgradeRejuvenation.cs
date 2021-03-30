using System.Collections.Generic;
using TowerDefense.Units;
using UnityEngine;

namespace TowerDefense.Towers
{
	// Each of this tower's attacks has a change to heal a damaged tower in range
    public class UpgradeRejuvenation : Upgrade
    {
		readonly Collider[] _towersInRange = new Collider[16];   // supports 16 towers being in range
		readonly List<TowerBase> _damagedTowersInRange = new List<TowerBase>();
		LayerMask _towerLayer;

		public override void AddUpgrade()
		{
			_towerLayer = LayerMask.GetMask("Tower Search");
			TowerBase.Events.OnAttackHitAction += Rejuvenate;
		}

		// Heal a random damaged tower in range
		private void Rejuvenate(Attack attack, Base unit)
		{
			// roll the proc chance
			if (Random.Range(0, 100) >= UpgradeData.GetValue(0)) return;

			// find all towers in range
			int towersCount = Physics.OverlapSphereNonAlloc(transform.position, TowerBase.Attributes.Range, _towersInRange, _towerLayer);

			// get active and damaged towers only
			for (int i = 0; i < towersCount; i++)
			{
				var tower = (TowerBase)_towersInRange[i].GetComponent<UnitCollider>().Base;

				if (tower.Unit.IsEnabled && tower.Attributes.GetHealthPercentage() != 1)
					_damagedTowersInRange.Add(tower);
			}

			// all towers in range are full health or disabled
			if (_damagedTowersInRange.Count == 0) return;

			// choose a random damaged tower to heal
			TowerBase towerToHeal = _damagedTowersInRange[Random.Range(0, _damagedTowersInRange.Count)];

			// the heal amount is based off the damage of this attack
			float healAmount = ((AttackArcaneBolt)attack).Damage * UpgradeData.GetValue(1) / 100f;
			towerToHeal.Unit.HealUnit(TowerBase, healAmount);

			// play heal VFX on tower
			Instantiate(UpgradeData.PrefabComponents[0], towerToHeal.transform.position, Quaternion.identity, towerToHeal.transform);

			// reset the list of towers found
			_damagedTowersInRange.Clear();
		}
    }
}
