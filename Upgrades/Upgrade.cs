using UnityEngine;

namespace TowerDefense.Towers
{
	// The base class for all tower upgrade instances
	// Upgrades can only be added (not removed)
	public abstract class Upgrade : MonoBehaviour
	{
		protected UpgradeData UpgradeData;
		protected TowerBase TowerBase;
		
		// Initialize the data used by the upgrade
		public void InitializeUpgrade(UpgradeData upgradeData, TowerBase tower)
		{
			UpgradeData = upgradeData;
			TowerBase = tower;
			AddUpgrade();
		}

		// What happens when a tower gains this upgrade (initialize values, subscribe to events, etc)
		public abstract void AddUpgrade();      
	}
}