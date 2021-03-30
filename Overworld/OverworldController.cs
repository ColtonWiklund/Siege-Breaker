using System.Collections;
using TowerDefense.Saving;
using TowerDefense.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Player;
using TowerDefense.Progress;
using TowerDefense.UI;

namespace TowerDefense.Overworld
{
	// Handles high-level tasks in the overworld (loading levels, updating player progress)
    public class OverworldController : MonoBehaviour
    {
		[Header("Saving")]	
		[SerializeField] SavingWrapper _savingWrapper;

		[Header("Levels")]
		[SerializeField] Transform _levelsContainer;

		[Header("Windows")]
		[SerializeField] WindowTowers _windowTowers;
		[SerializeField] WindowLevelFinished _windowLevelFinished;
		
		[Header("Camera")]
		[SerializeField] CameraController _cameraController;

		[Header("Canvas - UI")]
		[SerializeField] GameObject _canvasUI;		// only used to enable it on game start (might be hidden during development)
		[SerializeField] Transform _playerBase;		// the location of the player's castle (used to center the camera)
		[SerializeField] ScreenFader _screenFader;

		[Header("SFX")]
		[SerializeField] AudioClip _sfxStartLevel;

		private void Awake()
		{
			Debug.Log("--- Overworld : Enter ---");
			_canvasUI.SetActive(true);	// dev: enable the canvas if it was disabled during editing
			Time.timeScale = 1;
			_savingWrapper.Load();	// load the game state
			UpdateGameProgress();	// update the game state
			Save();					// save the game state
		}

		private void Start()
		{
			AudioListener.volume = PlayerPrefs.GetFloat("Master Volume");   // set the correct volume when the level is loaded
		}

		// Update the player's progress based on the level they just played
		private void UpdateGameProgress()
		{
			Debug.Log("--- Overworld : Update Game Progress ---");

			// Fade in the camera
			_screenFader.FadeIn();

			// Center the camera on the player's base (is overriden later if levelInfo is loaded)
			_cameraController.SetCameraPosition(_playerBase.position);

			// No level progress data was found
			var levelProgressData = LevelProgress.Data;
			if (levelProgressData == null) return;

			// No level info data was found
			var levelInfo = GetLevelInfoBySceneName(levelProgressData.SceneName);
			if (levelInfo == null) return;

			// Set the level as 'Complete' if it was passed and was not previously finished
			if (levelProgressData.LevelPassed && levelInfo.LevelState == LevelState.Unlocked)
				levelInfo.CompleteLevel();

			// Add any item rewards
			RewardItems(levelProgressData, levelInfo);

			// Add resource rewards
			ItemProgress.AddResources(levelProgressData.Resources);

			// Update tower lifetime stats
			TowerProgress.UpdateTowerLifetimeStats(levelProgressData.TowerStats);

			// Update highest level defeated
			levelInfo.UpdateHighestWaveDefeated(levelProgressData.WavesDefeated);

			// Open the level finished window
			_windowLevelFinished.WindowOpen(levelProgressData);

			// Set the camera to the location of the level on the map
			_cameraController.SetCameraPosition(levelInfo.transform.position);

			// Reset the progress data after it has been used
			LevelProgress.ResetLevelProgress();
		}

		// Reward any items gained from the level
		private static void RewardItems(LevelProgressData levelProgressData, LevelInfo levelInfo)
		{
			if (levelProgressData.Items == null) return;

			foreach (var reward in levelProgressData.Items)
			{
				// stars are always added to level progress, even though they may not be awarded to the player
				if (reward.Key == ItemType.Star1 || reward.Key == ItemType.Star2 || reward.Key == ItemType.Star3)
				{
					int newStarsGained = reward.Value - levelInfo.StarsAwarded;     // only award the stars if they are more than what was already been awarded
					if (newStarsGained <= 0) continue;

					levelInfo.AddLevelCompletionStars(newStarsGained);
					ItemProgress.AddItem(ItemType.Star, newStarsGained);
				}
				else if (reward.Key != ItemType.Star)
				{
					ItemProgress.AddItem(reward.Key, reward.Value);     // add any items gained 
				}
				else
				{
					Debug.Log("Warning: ItemType.Star should not be used for level rewards. Use Star1, Star2, Star3 instead");
				}
			}
		}

		// Return the LevelInfo for a given scene name
		public LevelInfo GetLevelInfoBySceneName(string name)
		{
			foreach (var level in _levelsContainer.GetComponentsInChildren<LevelInfo>())
				if (level.SceneName == name)
					return level;

			Debug.Log("Overworld Controller: Level: " + name + " not found");
			return null;
		}

		// Load the passed level, exiting the overworld
		public void LoadLevel(LevelInfo levelInfo)
		{
			// Check if the scene exists
			if (Application.CanStreamedLevelBeLoaded(levelInfo.SceneName))
				StartCoroutine(LoadLevelSequence(levelInfo));
			else
				Debug.Log("Level: " + levelInfo.SceneName + " not found.");
		}

		private IEnumerator LoadLevelSequence(LevelInfo levelInfo)
		{
			SoundManager.PlaySound(_sfxStartLevel);
			LevelProgress.CreateNewProgressData(levelInfo.DisplayName, levelInfo.SceneName, levelInfo.LevelRewards, levelInfo.HighestWaveDefeated);
			Save();
			Debug.Log("--- Overworld: Exit ---");

			yield return new WaitForSeconds(_screenFader.FadeOut());
			
			SceneManager.LoadScene(levelInfo.SceneName);
		}

		public void Save()
		{
			_savingWrapper.Save();
		}
	}
}
