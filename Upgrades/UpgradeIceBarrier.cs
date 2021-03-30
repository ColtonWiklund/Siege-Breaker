using TowerDefense.Units;

namespace TowerDefense.Towers
{
	// The first time this tower is reduced below 50% each wave, it will gain damage immunity for a short duration
	public class UpgradeIceBarrier : Upgrade, ISetModifierValues
	{
		private bool _triggeredThisWave;	// can only trigger once per wave

		public override void AddUpgrade()
		{
			TowerBase.TowerEvents.OnAttackedAction += CheckIceBarrier;
			TowerBase.TowerEvents.OnWaveEndAction += ResetIceBarrier;
		}

		// Check if Ice Barrier should trigger
		private void CheckIceBarrier(Base attacker, float damageTaken)
		{
			if (_triggeredThisWave) return;

			// only triggers below 50% health
			if (TowerBase.Attributes.GetHealthPercentage() > 0.5) return;

			// add the ice barrier modifier, granting damage immunity while it is active
			TowerBase.Attributes.AddModifier(UpgradeData.Modifiers[0], TowerBase.Unit, this);
			_triggeredThisWave = true;
		}

		private void ResetIceBarrier()
		{
			_triggeredThisWave = false;
		}

		public void SetModifierValues()
		{
			UpgradeData.Modifiers[0].SetMaxDuration(UpgradeData.GetValue(0));
		}
	}
}