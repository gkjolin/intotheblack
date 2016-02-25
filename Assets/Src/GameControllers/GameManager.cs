﻿using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Collections;
using System;

public class GameManager : MonoBehaviour {

	public static GameObject Player;
	public static GameManager Instance;
	public static int PlayerLevel = 1;
	private static int _playerExperience;
	public static int PlayerExperience {
		get	{
			return _playerExperience;
		}
		set	{
			PlayerScore += value - _playerExperience;
			_playerExperience = value;
			if (_playerExperience >= ExperienceToLevel) {
				
				LevelUp();
			}
		}
	}

	public static int ExperienceToLevel = 125;
	public static int PlayerScore;
	public static List<GameObject> ActiveEnemies = new List<GameObject>();

	public int UpgradePoints = 0;
	public Objective CurrentObjective;
	public int ObjectivesCollected;
	public int ObjectiveDistance = 50;
	public int MaxEnemies = 100;

	private GameObject _enemyPrefab;
	private OverlayUI _overlayUI;
	private float minSpawnDelay = 3f;
	private float maxSpawnDelay = 11f;
	private GameObject _asteroidPrefab;
	private List<GameObject> _asteroidList = new List<GameObject>();

	[SerializeField]
	private Transform ProjectileContainer;


	private static void LevelUp() {

		int remaining = _playerExperience - ExperienceToLevel;
		_playerExperience = remaining;
		ExperienceToLevel = Mathf.RoundToInt(ExperienceToLevel * 1.5f);
		PlayerLevel++;
		Instance.UpgradePoints++;
		Instance._overlayUI.DisplayMessage("You have leveled up! (Press ESC to Upgrade)");
	}

	private void Awake() {

		if (Instance == null) {
			Instance = this;
		} else {
			Destroy(gameObject);
			return;
		}

		_enemyPrefab = (GameObject)Resources.Load("Prefabs/EnemyShip");
		_asteroidPrefab = (GameObject)Resources.Load("Prefabs/Asteroid");
		Player = GameObject.FindGameObjectWithTag("Player");
		_overlayUI = GetComponent<OverlayUI>();
		_overlayUI.Manager = this;

		if (CurrentObjective != null) {
			CurrentObjective.Manager = this;
			CurrentObjective.GameUI = _overlayUI;
		} else {
			Debug.LogException(new Exception("CurrentObjective not set!"));
		}

		Projectile.Container = ProjectileContainer;

	}

	private void Start() {

		StartCoroutine(SpawnEnemyShipsRoutine());
		//StartCoroutine(SpawnObjectiveRoutine());
		StartCoroutine(SpawnAsteroidRoutine());
	}

	private IEnumerator SpawnAsteroidRoutine() {

		float previousSize = 1f;
		float waitTime = 2f;

		while (gameObject.activeSelf) {
			yield return new WaitForSeconds(Random.Range(waitTime, waitTime + 2));
			GameObject roid = GetNextAsteroid();
			Vector3 heading = Player.transform.up * 2f;
			if (heading.x < 1 && heading.x > 0 && heading.y < 1 && heading.y > 0) {
				heading.x += 1;
			}
			Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(
				Random.Range(heading.x-0.5f, heading.x+0.5f),
				Random.Range(heading.y-0.5f, heading.y+0.5f),
				-Camera.main.transform.position.z));
			roid.transform.position = spawnPos;
			Rigidbody2D rb = roid.GetComponent<Rigidbody2D>();
			rb.velocity = new Vector3(Random.value, Random.value, 0) * Random.Range(1f, 2f);
			rb.angularVelocity = Random.value;
			float scale = Mathf.Clamp(Random.Range(previousSize - .85f, previousSize + .2f), .1f, 5f);
			roid.transform.localScale = new Vector3(scale, scale);
			rb.mass = roid.transform.localScale.magnitude;
			roid.SetActive(true);

			if (Random.value > 0.09f) {
				previousSize = roid.transform.localScale.x;
			} else {
				previousSize = Random.Range(.1f, 4f);
			}
			waitTime = previousSize * (previousSize * 0.5f);
			CleanOldAsteroids();
		}
	}

	private void CleanOldAsteroids() {

		GameObject roid;
		for (int i = 0; i < _asteroidList.Count; i++) {
			if (_asteroidList[i].activeSelf) {
				roid = _asteroidList[i];
				if (Vector3.Distance(roid.transform.position, Player.transform.position) > 100) {
					roid.SetActive(false);
				}
			}
		}
	}

	private GameObject GetNextAsteroid() {

		GameObject roid = null;
		for (int i = 0; i < _asteroidList.Count; i++) {
			if (!_asteroidList[i].activeSelf) {
				roid = _asteroidList[i];
				break;
			}
		}
		if (roid == null) {
			roid = Instantiate(_asteroidPrefab);
			_asteroidList.Add(roid);
		}
		return roid;
	}

	private IEnumerator SpawnEnemyShipsRoutine() {

		StarSystemData starData = StarSystemData.StarSystemLoaded;

		int fleetSize = Mathf.Max(8, GetEnemyFleetSize(starData));
		int remainingShips = fleetSize;
		Queue<int> spawnQueue = new Queue<int>();
		while (remainingShips > 0) {
			int squadSize = Random.Range(fleetSize / 8, fleetSize / 4);
			if (fleetSize >= squadSize) {
				remainingShips -= squadSize;
				spawnQueue.Enqueue(squadSize);
			} else {
				spawnQueue.Enqueue(remainingShips);
			}
		}
		Vector2 spawnPos;

		while (spawnQueue.Count > 0) {
			if (ActiveEnemies.Count == 0) {
				yield return new WaitForSeconds(3f);

				spawnPos = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
				int squadSize = spawnQueue.Dequeue();

				_overlayUI.DisplayMessage("Enemy squadron detected!");
				for (int i = 0; i < squadSize; i++) {
					InstantiateEnemy(spawnPos + (Random.insideUnitCircle * squadSize));
				} 
			}

			yield return null;
		}
		_overlayUI.DisplayMessage("Well done. This sector has been defended. Carry on, Captain.");
	}

	private int GetEnemyFleetSize(StarSystemData starData) {

		int size = Mathf.RoundToInt(starData.Hostility * 100f);
		// More modifiers to be added, such as system econonmy, size, etc. 

		return size;
	}

	private void InstantiateEnemy(Vector3 pos = default(Vector3)) {

		GameObject newEnemyGO = Instantiate(_enemyPrefab);
		newEnemyGO.GetComponent<Destructable>().Sethealth(Random.Range(8, 25));
		if (pos == default(Vector3)) {
			pos = Camera.main.ViewportToWorldPoint(
				new Vector3(Random.Range(-1, 2), Random.Range(-1, 2), -Camera.main.transform.position.z)); 
		}
		newEnemyGO.transform.position = pos;
		newEnemyGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.zero);
		_overlayUI.LinkNewArrow(newEnemyGO.transform);
		ActiveEnemies.Add(newEnemyGO);
	}

	private IEnumerator SpawnObjectiveRoutine() {

		while (gameObject.activeSelf) {
			if (!CurrentObjective.gameObject.activeSelf) {
				yield return new WaitForSeconds(Random.Range(10, 30));
				_overlayUI.DisplayMessage("Beacon located! Secure the objective!");
				Vector3 newPos = new Vector3(
					Random.Range(-ObjectiveDistance, ObjectiveDistance),
					Random.Range(-ObjectiveDistance, ObjectiveDistance),
					0);
				CurrentObjective.transform.position = newPos;
				CurrentObjective.EndLocation = new Vector3(
					Random.Range(-ObjectiveDistance, ObjectiveDistance),
					Random.Range(-ObjectiveDistance, ObjectiveDistance),
					0);
				CurrentObjective.gameObject.SetActive(true);
				CurrentObjective.BeaconLight.startColor = Color.white;
			}
			yield return null;
		}
	}

	internal static void GameOver() {

		Time.timeScale = 0f;
	}
}