using System.Collections.Generic;
using TowerDefense.Units;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Towers
{
    // Stores shared information about each tower upgrade (name, description, values, etc)
    [CreateAssetMenu(menuName = "Upgrades/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        // Naming
        [SerializeField] string _nameInternal;
        public string NameInternal => _nameInternal;

        [SerializeField] string _nameDisplay;
        public string NameDisplay => _nameDisplay;

        [SerializeField] TowerType _towerType;  // what type of tower this upgrade is for
        public TowerType TowerType => _towerType;

        // Rank
        [SerializeField] int _rank;
        public int Rank => _rank;

        public void IncreaseRank()
		{
            _rank++;
		}

        public void SetRank(int rank)
		{
            _rank = rank;
		}

        // Description
        [SerializeField] Sprite _icon;
        public Sprite Icon => _icon;

        [TextArea(3, 3)]
        [SerializeField] string _description;

        // Replace placeholder values with the current rank values (and optionally the next rank values)
        public string GetDescription(bool showNextRankValues)
        {
            string description = _description;

            for (int i = 0; i < _startingValues.Length; i++)
            {
                string replace = "#" + i;

                if (!showNextRankValues)
                {
                    description = description.Replace(replace, GetValue(i).ToString());
                }
                else
                {
                    string currentRankValue = GetValue(i).ToString();
                    string nextRankValue;

                    // Is the value a percentage? (Ex. #1% = true, #1 = false)
                    // Looks for a percentage sign 2 characters past where the value is to be replaced
                    if (description.Substring(description.IndexOf(replace) + 2, 1) == "%")
                    {
                        replace += "%"; // replace the existing %
                        currentRankValue += "%";
                        nextRankValue = " (+" + ValueIncreasePerRank[i] + "%)";
                    }
                    else // not a percentage
                    {
                        nextRankValue = " (+" + ValueIncreasePerRank[i] + ")";
                    }

                    nextRankValue = FontStyling.AddGreenHighlight(nextRankValue);
                    description = description.Replace(replace, currentRankValue + (ValueIncreasePerRank[i] != 0 ? nextRankValue : ""));
                }
            }

            return description;
        }

        // Values
        [SerializeField] int[] _startingValues;   // the starting value for each variable used by the modifier
        public int[] StartingValues => _startingValues;

        [SerializeField] int[] valueIncreasePerRank;    // how much the starting values increase every rank
        public int[] ValueIncreasePerRank => valueIncreasePerRank;

        // Returns the value at the current rank
        public int GetValue(int valueIndex)
		{
            return _startingValues[valueIndex] + (_rank * valueIncreasePerRank[valueIndex]);
        }

		// Cost
		[SerializeField] UpgradeCostProfile _cost;          // the default distribution and tiers of resource costs
        [Range(0.5f, 4f)]
        [SerializeField] float _costScaler = 1f;            // linearly scale the cost profile
        [Range(10f, 100f)]
        [SerializeField] float _costGrowthPercent = 20f;    // how quickly the costs increase per rank (exponential)

        // Get the cost to strengthen this upgrade
        public Dictionary<ResourceType, int> GetStrengthenCost()
        {
            var rankCost = new Dictionary<ResourceType, int>();

            foreach (var resource in _cost.ResourcesCost)
            {
	            if (_rank < resource.StartingRank) continue;

	            float amount = resource.BaseCost * Mathf.Pow(1f + (_costGrowthPercent / 100), _rank - resource.StartingRank);
	            amount *= _costScaler;
	            rankCost.Add(resource.Resource, Mathf.RoundToInt(amount));
            }

            return rankCost;
        }

        // Modifiers
        [SerializeField] ModifierData[] _modifiers;
        public ModifierData[] Modifiers => _modifiers;

        // Prefab Components
        [SerializeField] GameObject[] _prefabComponents;    // prefabs used by the upgrade
        public GameObject[] PrefabComponents => _prefabComponents;

        // Upgrade Implementation
        [SerializeField] protected MonoScript _upgradeScript;

        // Add this Upgrade to a tower
        public void AddUpgrade(TowerAttributes towerAttributes)
        {
            Upgrade upgrade = towerAttributes.gameObject.AddComponent(_upgradeScript.GetClass()) as Upgrade;
            upgrade.InitializeUpgrade(this, towerAttributes.GetComponent<TowerBase>());
            towerAttributes.ActiveUpgrades.Add(this);   // add Upgrade to tower
        }
    }
}

