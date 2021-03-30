using System.Collections.Generic;
using System.Linq;
using TMPro;
using TowerDefense.Progress;
using TowerDefense.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.Overworld
{
	// Displays each tower's upgrades and allows them to be equipped and strengthened
	public class PanelUpgrades : MonoBehaviour, ITowerPanel
	{
		[Header("Components")]
		[SerializeField] UIBarManager _uiBarManager;

		[Header("Upgrade Grid")]
		[SerializeField] List<UpgradeTile> _upgradeTiles;	// the 6 upgrade tiles
		private readonly Dictionary<UpgradeReference, UpgradeTile> _upgradeTileDict = new Dictionary<UpgradeReference, UpgradeTile>();

		[Header("Info Panel - Essence")]
		[SerializeField] TextMeshProUGUI _upgradeInfoName;
		[SerializeField] TextMeshProUGUI _upgradeInfoDescription;

		[Header("Info Panel - Buttons")]
		[SerializeField] GameObject _buttonContainer;
		[SerializeField] Button _buttonStrengthen;
		[SerializeField] TextMeshProUGUI _textStrengthen;
		[SerializeField] TextMeshProUGUI _textResourceCosts;
		[SerializeField] Button _buttonEquip;
		[SerializeField] TextMeshProUGUI _textButtonEquip;
		[SerializeField] Button _buttonUnequip;
		[SerializeField] Color _colorTextEnabled;
		[SerializeField] Color _colorTextDisabled;

		[Header("Sound")]
		[SerializeField] AudioClip _soundUpgradeEquip;
		[SerializeField] AudioClip _soundUpgradeUnequip;
		[SerializeField] AudioClip _soundUpgradeStrengthen;

		private TowerInfo _loadedTower;				// the tower that is currently being displayed
		private UpgradeReference _selectedUpgrade;  // the upgrade currently selected (has it's information, equip, and strengthen buttons shown)

		// Reload the Upgrade tiles with the passed tower's Upgrades
		public void UpdatePanel(TowerInfo tower)
		{
			_uiBarManager.ShowBars(BarTop.Resources, BarBottom.TowerSelect);
			_loadedTower = tower;

			// reload upgrade information for each tile and display whether it is locked or unlocked
			_upgradeTileDict.Clear();
			UpgradeReference selectUpgradeOnLoad = null;
			var upgradeList = _loadedTower.Upgrades.Values.ToList();
			for (int i = 0; i < upgradeList.Count; i++)
			{
				_upgradeTiles[i].SetUpgradeTile(this, _loadedTower.Upgrades[upgradeList[i].UpgradeData]);
				_upgradeTiles[i].SetEquipped(upgradeList[i].Equipped, false);
				_upgradeTileDict.Add(upgradeList[i], _upgradeTiles[i]);

				// select the first unlocked upgrade to display it's information
				if (selectUpgradeOnLoad == null && upgradeList[i].Unlocked == true)
					selectUpgradeOnLoad = upgradeList[i];
			}

			// no upgrade is unlocked
			if (selectUpgradeOnLoad == null)
				if (_selectedUpgrade != null && _upgradeTileDict.Keys.Contains(_selectedUpgrade))	
					selectUpgradeOnLoad = _selectedUpgrade;		// select the previously selected tile (only works if the tower hasn't changed)
				else
					selectUpgradeOnLoad = upgradeList[0];		// otherwise select the first tile

			_selectedUpgrade = null;
			SelectUpgrade(selectUpgradeOnLoad, selectUpgradeOnLoad == null ? false : selectUpgradeOnLoad.Unlocked);
		}

		// Show the information for this upgrade in the right info box, allowing it to be equipped and strengthened
		public void SelectUpgrade(UpgradeReference upgrade, bool showInfo)
		{
			// hide the selection outline of the previously selected upgrade
			if (_selectedUpgrade != null)
				_upgradeTileDict[_selectedUpgrade].ShowSelectionOutline(false);

			// set the passed upgrade to be selected
			_selectedUpgrade = upgrade;
			_upgradeTileDict[_selectedUpgrade].ShowSelectionOutline(true);

			// upgrade is unlocked
			if (_selectedUpgrade.Unlocked)
			{
				_upgradeInfoName.text = _selectedUpgrade.UpgradeData.NameDisplay;
				_upgradeInfoDescription.text = _selectedUpgrade.UpgradeData.GetDescription(true);
				_buttonContainer.SetActive(true);  // show the strengthen and equip buttons
				UpdateEquipButtons();
				UpdateStrengthenButton();
			}
			// upgrade is locked
			else
			{
				_buttonContainer.SetActive(false);
				_upgradeInfoName.text = "Unknown";
				_upgradeInfoDescription.text = "This Essence has not been discovered.";
			}

			SoundManager.PlayClick1();
		}

		// UI - Equip this modifier, adding it to the upgrades available to the tower
		public void ButtonEquipModifier()
		{
			_selectedUpgrade.Equipped = true;
			_upgradeTileDict[_selectedUpgrade].SetEquipped(true, true);
			UpdateEquipButtons();
			SoundManager.PlaySound(_soundUpgradeEquip);
		}

		// UI - Unequip this modifier, removing it from the upgrades available to the tower
		public void ButtonUnequipModifier()
		{
			_selectedUpgrade.Equipped = false;
			_upgradeTileDict[_selectedUpgrade].SetEquipped(false, false);
			UpdateEquipButtons();
			SoundManager.PlaySound(_soundUpgradeUnequip);
		}

		// Show and hide the Equip/Unequip buttons based on what Upgrades are equipped
		public void UpdateEquipButtons()
		{
			if (_selectedUpgrade.Equipped)
			{
				_buttonEquip.gameObject.SetActive(false);
				_buttonUnequip.gameObject.SetActive(true);
			}
			else
			{
				int equippedModifiersCount = 0;
				foreach (var modifier in _loadedTower.Upgrades.Values)
					if (modifier.Equipped)
						equippedModifiersCount++;

				if (equippedModifiersCount == 3)
				{
					_textButtonEquip.text = "Equip (3 Max)";
					_textButtonEquip.color = _colorTextDisabled;
					_buttonEquip.interactable = false;
				}
				else
				{
					_textButtonEquip.text = "Equip";
					_textButtonEquip.color = _colorTextEnabled;
					_buttonEquip.interactable = true;
				}
				
				_buttonUnequip.gameObject.SetActive(false);
				_buttonEquip.gameObject.SetActive(true);
			}
		}
		
		// UI - Strengthen the selected modifier, increasing its effect
		public void ButtonStrengthenModifier()
		{
			ItemProgress.SpendResources(_selectedUpgrade.UpgradeData.GetStrengthenCost());
			_selectedUpgrade.UpgradeData.IncreaseRank();
			_upgradeInfoDescription.text = _selectedUpgrade.UpgradeData.GetDescription(true);
			//Debug.Log("Panel Upgrades: Strengthening Modifier: " + _selectedModifier.Modifier.ModifierNameInternal + " " + _selectedModifier.Rank);

			UpdateStrengthenButton();
			_upgradeTileDict[_selectedUpgrade].StrengthenEssence();
			SoundManager.PlaySound(_soundUpgradeStrengthen);
		}

		// Set the Strengthen button's interactability based on whether the player has enough resources
		public void UpdateStrengthenButton()
		{
			_buttonStrengthen.gameObject.SetActive(true);

			// remove the previous cost
			_textResourceCosts.text = "";

			// Populate new cost tiles
			var rankCost = _selectedUpgrade.UpgradeData.GetStrengthenCost();
			foreach (var resource in rankCost)
			{
				if (!string.IsNullOrEmpty(_textResourceCosts.text))
					_textResourceCosts.text += " ";		// add space to separate the resources

				_textResourceCosts.text += resource.Value.ToString();

				// add the respective sprite for the resource
				_textResourceCosts.text += SpriteReference.GetResourceSprite(resource.Key);
			}

			// Set interactability & text color based on whether the player has enough resources
			bool hasResources = ItemProgress.HasResources(rankCost);
			_buttonStrengthen.interactable = hasResources;
			_textResourceCosts.color = hasResources ? _colorTextEnabled : _colorTextDisabled;
			_textStrengthen.color = hasResources ? _colorTextEnabled : _colorTextDisabled;
		}
	}
}
