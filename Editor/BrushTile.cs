using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.EditorTools
{
	// Creates tiles based on mouse input, used for level creation
	// Controlled by LevelEditor.cs
    public class BrushTile : Editor
    {
		private static float _defaultTileModelChance = 0.5f;	// how likely (0-1) when creating a tile that it will have the default model for its type

		public static Dictionary<TileType, Object[]> Tileset;	// the tiles that are created by the tile brush

		// Rules to determine what Tile will be placed based on the adjacent tiles
		public static Dictionary<TileType, byte> TileRules = new Dictionary<TileType, byte>()
		{
			{ TileType.Flat, 0b_10101010 },			// tiles on all 4 sides
			{ TileType.Straight, 0b_10101000 },		// tiles on 3 sides
			{ TileType.Corner, 0b_10100000 },		// tiles on 2 adjacent sides
			{ TileType.Corridor, 0b_10001000 },		// tiles on 2 opposite sides
			{ TileType.End, 0b_10000000 },			// tile on 1 side
			{ TileType.Standalone, 0b_00000000 },	// tile on no sides
		};

		// How many ways the Tile can be rotated
		public static Dictionary<TileType, int> TilePermutations = new Dictionary<TileType, int>()
		{
			{ TileType.Flat, 1 },
			{ TileType.Straight, 4 },
			{ TileType.Corner, 4 },
			{ TileType.Corridor, 2 },
			{ TileType.End, 4 },
			{ TileType.Standalone, 1 },
		};

		public static void LoadBrush()
		{
			LoadTilesetFromResources();
		}

		public static void HandleInput(Event current)
		{
			if (LevelEditor.LevelHasFillTiles)
			{
				if (current.type == EventType.MouseDown && current.button == 0)
					Debug.Log("Level Editor: Tile Brush cannot be used while tiles are merged. Unmerge tiles before editing tiles.");
				return;
			}
			// Place tiles when not holding shift
			if (!current.shift)
			{
				// Left Mouse Button - Down
				if (current.type == EventType.MouseDown && current.button == 0)
				{
					TileBrush();
					current.Use();
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
				}
				// Left Mouse Button - Hold
				else if (current.type == EventType.MouseDrag && current.button == 0)
				{
					TileBrush();   // call the tile brush every frame
					current.Use();
				}
				// Left Mouse Button - Up
				else if (current.type == EventType.MouseUp && current.button == 0)
				{
					current.Use(); // prevent objects in the scene from being selected
				}
			}
			// Delete tiles when holding shift
			else
			{
				// Left Mouse Button - Down
				if (current.type == EventType.MouseDown && current.button == 0)
				{
					TileDeleteBrush();
					current.Use();
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
				}
				// Left Mouse Button - Hold
				else if (current.type == EventType.MouseDrag && current.button == 0)
				{
					TileDeleteBrush();   // call the tile brush every frame
					current.Use();
				}
				// Left Mouse Button - Up
				else if (current.type == EventType.MouseUp && current.button == 0)
				{
					current.Use(); // prevent objects in the scene from being selected
				}
			}
		}

		// Creates tiles under the brush
		private static void TileBrush()
		{
			var pos = LevelEditor.BrushPositions;
			if (pos == null) return;

			for (int i = 0; i < pos.Count; i++)
			{
				// a tile already exists at this location
				if (LevelEditor.ActiveTiles.ContainsKey(pos[i])) continue;

				var tileData = GetTile(pos[i]);

				// a valid tile wasn't found for this position (this should never happen)
				if (tileData == null) continue;

				CreateTileAtLocation(tileData.tileType, pos[i], tileData.rotation);

				// loop through each adjacent tile on the cardinal directions and update them
				for (int j = 0; j < 4; j++)
					UpdateExistingTile(pos[i] + LevelEditor.DirectionVectors[(Direction)j]);
			}
		}

		// Delete any tiles under the brush
		private static void TileDeleteBrush()
		{
			var pos = LevelEditor.BrushPositions;
			if (pos == null) return;

			for (int i = 0; i < pos.Count; i++)
			{
				// there is no tile at this location
				if (!LevelEditor.ActiveTiles.ContainsKey(pos[i])) continue;

				DeleteTile(pos[i], true);

				// loop through each adjacent tile on the cardinal directions and update them
				for (int j = 0; j < 4; j++)
					UpdateExistingTile(pos[i] + LevelEditor.DirectionVectors[(Direction)j]);
			}
		}

		// Destroy the tile at the passed position
		public static void DeleteTile(Vector3 pos, bool removeProp)
		{
			DestroyImmediate(LevelEditor.ActiveTiles[pos].gameObject);
			LevelEditor.ActiveTiles.Remove(pos);

			// delete the prop on this tile if there is one
			if (!removeProp) return;
			if (!LevelEditor.ActiveProps.ContainsKey(pos)) return;
			BrushProps.DeleteProp(pos);
		}

		// Delete the existing tile and create a new one
		private static void UpdateExistingTile(Vector3 pos)
		{
			// there is no tile at this location
			if (!LevelEditor.ActiveTiles.ContainsKey(pos)) return;

			var tileData = GetTile(pos);
			if (tileData == null) return;

			DeleteTile(pos, false);
			CreateTileAtLocation(tileData.tileType, pos, tileData.rotation);
		}

		// Create a new tile at the passed location
		public static void CreateTileAtLocation(TileType tileType, Vector3 pos, Quaternion rotation)
		{
			// cannot create tiles outside the levels boundaries (based on the size of bedrock)
			if (Mathf.Abs(pos.x) > LevelEditor.HalfLevelSize.x - 1) return;
			if (Mathf.Abs(pos.z) > LevelEditor.HalfLevelSize.y - 1) return;

			int tileIndex = 0;																	// the default model for each tile will be at index 0
			if (tileType != TileType.Flat && Random.Range(0, 1f) > _defaultTileModelChance)		// is not a default tile
				tileIndex = Random.Range(1, Tileset[tileType].Length);							// select a random tile variation

			var prefab = Tileset[tileType][tileIndex];
			var tile = (PrefabUtility.InstantiatePrefab(prefab) as GameObject).GetComponent<TileEntity>();
			tile.transform.position = pos;
			tile.transform.rotation = rotation;
			tile.transform.parent = LevelEditor.TileContainer;
			tile.name = prefab.name;
			LevelEditor.ActiveTiles.Add(pos, tile);

			// disable the tile model if there is a hardpoint on this tile
			if (LevelEditor.ActiveProps.ContainsKey(pos))
			{
				var prop = LevelEditor.ActiveProps[pos];
				if (prop.BrushType == BrushType.Hardpoint)
					tile.SetModelEnabled(false);
			}
		}

		// Return the tiletype and rotation that fits the passed position
		private static TileData GetTile(Vector3 pos)
		{
			byte adjacentTiles = GetAdjacentTiles(pos);

			// loop through each tiletype (except fill) based on the tiletypes position in the enum
			for (int i = 0; i < System.Enum.GetValues(typeof(TileType)).Length - 1; i++)
			{
				TileType tileType = (TileType)i;
				for (int j = 0; j < TilePermutations[tileType]; j++)
				{
					// circularly shift the tilerule to check if any permutation fits the adjacent tiles
					byte rule = (byte)(TileRules[tileType] << (j * 2) | TileRules[tileType] >> (8 - (j * 2)));

					// if the tilerule fits the adjacent tiles, return this tile and rotation
					if (adjacentTiles == rule)
						return new TileData() { tileType = tileType, rotation = Quaternion.Euler(new Vector3(0, j * -90f, 0)) };
				}
			}
			return null;
		}

		class TileData
		{
			public TileType tileType;
			public Quaternion rotation;
		}

		// If there is an adjacent tile in a direction, the corresponding bit is set to 1. Each bit is spaced out by a 0 so it can be rotated
		// Ex. There is an adjacent tile to the North and South -> (10001000)
		private static byte GetAdjacentTiles(Vector3 pos)
		{
			byte adjacentTiles = 0;

			// will count as an adjacent tile if there is a tile present, or the position is off the level
			adjacentTiles += (byte)(LevelEditor.ActiveTiles.ContainsKey(pos + LevelEditor.DirectionVectors[Direction.North]) ? 0b_10000000 : 0);
			adjacentTiles += (byte)(!LevelEditor.IsTileWithinLevel(pos + LevelEditor.DirectionVectors[Direction.North]) ? 0b_10000000 : 0);

			adjacentTiles += (byte)(LevelEditor.ActiveTiles.ContainsKey(pos + LevelEditor.DirectionVectors[Direction.East]) ? 0b_00100000 : 0);
			adjacentTiles += (byte)(!LevelEditor.IsTileWithinLevel(pos + LevelEditor.DirectionVectors[Direction.East]) ? 0b_00100000 : 0);

			adjacentTiles += (byte)(LevelEditor.ActiveTiles.ContainsKey(pos + LevelEditor.DirectionVectors[Direction.South]) ? 0b_00001000 : 0);
			adjacentTiles += (byte)(!LevelEditor.IsTileWithinLevel(pos + LevelEditor.DirectionVectors[Direction.South]) ? 0b_00001000 : 0);

			adjacentTiles += (byte)(LevelEditor.ActiveTiles.ContainsKey(pos + LevelEditor.DirectionVectors[Direction.West]) ? 0b_00000010 : 0);
			adjacentTiles += (byte)(!LevelEditor.IsTileWithinLevel(pos + LevelEditor.DirectionVectors[Direction.West]) ? 0b_00000010 : 0);

			//Debug.Log("Tile Editor: Adjacent Tiles: " + StringFormat.ToBinaryString(_adjacentTiles));
			return adjacentTiles;
		}

		// Load each tile from the resource folders
		public static void LoadTilesetFromResources()
		{
			Tileset = new Dictionary<TileType, Object[]>();

			// Loads each tile variant (multiple tile variations per tile type) (enable if using tile variants)
			// loop through the TileType enum (except fill) and load each resource folder of tiles (folder names must match enum)
			for (int i = 0; i < System.Enum.GetValues(typeof(TileType)).Length - 1; i++)
				Tileset.Add((TileType)i, Resources.LoadAll("Tileset/" + ((TileType)i).ToString()));

			// Load only the simple tiles (only one tile per tile type)
			//for (int i = 0; i < System.Enum.GetValues(typeof(TileType)).Length - 1; i++)
			//	Tileset.Add((TileType)i, new Object[] { Resources.Load("Tileset/Default/tile_" + ((TileType)i).ToString()) });   // still stored in an array to support variants if needed later on
		}

		// Utility: Delete all tiles in the level
		public static void DeleteActiveTilesFromLevel()
		{
			// Destroy individual tiles
			for (int i = LevelEditor.TileContainer.childCount - 1; i >= 0; i--)
				DestroyImmediate(LevelEditor.TileContainer.GetChild(i).gameObject);

			// Destroy fill tiles
			for (int i = LevelEditor.TileFillContainer.childCount - 1; i >= 0; i--)
				DestroyImmediate(LevelEditor.TileFillContainer.GetChild(i).gameObject);

			// Create a new dictionary as the previous one will be full of null references
			LevelEditor.ActiveTiles = new Dictionary<Vector3, TileEntity>();

			// Destroy all props
			BrushProps.DeleteAllProps(true);

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}

		// Utility: Replace all individual tiles in the scene with a new tile of the same type and rotation
		public static void ReplaceAllTilesInLevel()
		{
			foreach (var _pos in LevelEditor.ActiveTiles.Keys.ToList())
			{
				var tileType = LevelEditor.ActiveTiles[_pos].TileType;
				if (tileType == TileType.Fill) return;	// don't replace fill tiles

				var rotation = LevelEditor.ActiveTiles[_pos].transform.rotation;

				DeleteTile(_pos, false);
				CreateTileAtLocation(tileType, _pos, rotation);
			}

			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}
	}
}
