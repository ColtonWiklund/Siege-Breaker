using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Progress
{
	// Stores information about what items and resources the player has in order to be saved and loaded by the saving system
    public static class ItemProgress
    {
		[Serializable]
		private class ItemProgressSave
		{
			public Dictionary<ItemType, int> PlayerItems;           // what items the player has
			public Dictionary<ResourceType, int> Resources;			// what resources the player has
			public int TotalAnimaShardsHarvested;                   // how many Anima Shards the player has harvested
			public int TotalAnimaHarvested;                         // how much Anima the player has harvested from all shards
		}
		private static ItemProgressSave _saveFile;

		// Dev Settings
		private static DevProgressSettings _devSettings;
		private static DevProgressSettings DevSettings
		{
			get
			{
				if (_devSettings == null)
					_devSettings = Resources.Load("DevProgressSettings") as DevProgressSettings;

				return _devSettings;
			}
		}

		// Create a new save file
		public static void InitializeNewState()
		{
			_saveFile = new ItemProgressSave
			{
				TotalAnimaShardsHarvested = DevSettings.AnimaShardsHarvested,
				TotalAnimaHarvested = DevSettings.AnimaHarvested
			};

			GenerateItems();
			GenerateResources();
			PlayerProgress.CheckNexusAbilityUnlocks(_saveFile.TotalAnimaHarvested);

			Debug.Log("Item Progress: Initialize New State");
		}

		// Return the active save file or initialize a new one if it doesn't already exist
		public static object SaveStateToFile()
		{
			if (_saveFile == null)
				InitializeNewState();
			else
				OverrideExistingState();

			return _saveFile;
		}

		public static void LoadStateFromFile(object state)
		{
			_saveFile = (ItemProgressSave)state;
			PlayerProgress.CheckNexusAbilityUnlocks(_saveFile.TotalAnimaHarvested);
		}

		// Development Only: Override save file parameters with dev settings
		private static void OverrideExistingState()
		{
			if (DevSettings.ItemsAmount > 0)
				GenerateItems();

			if (DevSettings.ResourcesAmount > 0)
				GenerateResources();

			if (DevSettings.AnimaShardsHarvested > 0)
				_saveFile.TotalAnimaShardsHarvested = DevSettings.AnimaShardsHarvested;

			if (DevSettings.AnimaHarvested > 0)
				_saveFile.TotalAnimaHarvested = DevSettings.AnimaHarvested;
		}

		// Generate Items
		private static void GenerateItems()
		{
			// player items
			_saveFile.PlayerItems = new Dictionary<ItemType, int>
			{
				{ ItemType.OrbOfScrying, DevSettings.ItemsAmount},
				{ ItemType.AnimaShard, DevSettings.ItemsAmount },
				{ ItemType.Anima, DevSettings.AnimaAmount },
				{ ItemType.Star, 0 }
			};
		}

		// Generate Resources
		private static void GenerateResources()
		{
			_saveFile.Resources = new Dictionary<ResourceType, int>();
			foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
				_saveFile.Resources.Add(resource, DevSettings.ResourcesAmount);
		}

		// Add an item of this type
		public static void AddItem(ItemType itemType, int quantity)
		{
			_saveFile.PlayerItems[itemType] += quantity;

			switch (itemType)
			{
				case ItemType.Anima:
					_saveFile.TotalAnimaHarvested += quantity;
					PlayerProgress.CheckNexusAbilityUnlocks(_saveFile.TotalAnimaHarvested);
					OnAnimaChangedAction?.Invoke(_saveFile.PlayerItems[ItemType.Anima], quantity);
					break;
				case ItemType.Star:
					OnStarGainedAction?.Invoke(_saveFile.PlayerItems[ItemType.Star]);
					break;
			}
		}

		// Consume a single item of this type
		public static void SpendItem(ItemType itemType)
		{
			SpendItem(itemType, 1);
		}

		// Consume multiple items of this type
		public static void SpendItem(ItemType itemType, int quantity)
		{
			if (quantity <= _saveFile.PlayerItems[itemType])
			{
				_saveFile.PlayerItems[itemType] -= quantity;

				switch (itemType)
				{
					// keep track of when an Anima Shard is harvested
					case ItemType.AnimaShard:
						_saveFile.TotalAnimaShardsHarvested += quantity;
						break;
					case ItemType.Anima:
						OnAnimaChangedAction?.Invoke(_saveFile.PlayerItems[ItemType.Anima], quantity);
						break;
				}
			}

			else
				Debug.Log("Player Progress: Tried to spend " + quantity + " " + itemType + " with only " + _saveFile.PlayerItems[itemType] + " in inventory");
		}

		// How many items of this type does the player have?
		public static int GetItemCount(ItemType itemType)
		{
			return _saveFile.PlayerItems[itemType];
		}

		// How many Anima Shards have been harvested in total
		public static int GetTotalAnimaShardsHarvested()
		{
			return _saveFile.TotalAnimaShardsHarvested;
		}

		// How much Anima has been acquired in total
		public static int GetTotalAnimaHarvested()
		{
			return _saveFile != null ? _saveFile.TotalAnimaHarvested : 0;
		}

		// Add the passed Resources to the player
		public static void AddResources(Dictionary<ResourceType, int> addResources)
		{
			if (addResources == null) return;	// function is called after every level ends, regardless of whether any resources were gained

			foreach (var addResource in addResources)
				_saveFile.Resources[addResource.Key] += addResource.Value;

			OnResourceChangedAction?.Invoke(_saveFile.Resources);
		}

		// Subtract the passed Resources from the player
		public static void SpendResources(Dictionary<ResourceType, int> spentResources)
		{
			if (!HasResources(spentResources)) return;      // verify the player has enough resources
			foreach (var spentResource in spentResources)	// subtract each spent resource from the player
				_saveFile.Resources[spentResource.Key] -= spentResource.Value;

			OnResourceChangedAction?.Invoke(_saveFile.Resources);
		}

		// Does the player have all of the passed Resources?
		public static bool HasResources(Dictionary<ResourceType, int> hasResources)
		{
			foreach (var hasResource in hasResources)
				if (hasResource.Value > _saveFile.Resources[hasResource.Key])
					return false;
			
			return true;
		}

		// Returns all resources the player has
		public static Dictionary<ResourceType, int> GetResources()
		{
			return _saveFile.Resources;
		}

		// ---- Events ----

		// Call this event when the player's Anima has increased or decreased (used for updating the UI)
		public static Action<int, int> OnAnimaChangedAction;

		// The player has gained at least 1 star from completing a level
		public static Action<int> OnStarGainedAction;

		// Call this event when the player's Resources have increased or decreased (used for updating the UI)
		public static Action<Dictionary<ResourceType, int>> OnResourceChangedAction;
	}
}
