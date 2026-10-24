using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

[Serializable]
public class PlayerCinespecInfo
{
	public PlayerControllerB playerScript;

	public Transform playerBody;

	public bool bodyDoesntExist;

	private bool isConnected;

	public float timeSinceSpectatedPlayer = 100f;

	public float timeSincePlayerMetAnotherPlayer;

	private bool isAlone;

	public float currentInterestLevel;

	public float timeSincePlayerDied;

	public List<int> playersNearby;

	public int deadPlayersNearby;

	public CameraSubjects nearbySubjects;

	public PlayerCinespecInfo(int playerIndex)
	{
		playerScript = StartOfRound.Instance.allPlayerScripts[playerIndex];
		playersNearby = new List<int>(StartOfRound.Instance.allPlayerScripts.Length);
		nearbySubjects = new CameraSubjects();
		nearbySubjects.mapHazardSubjects = new (bool, float, Vector3)[8];
		nearbySubjects.enemySubjects = new (EnemyAI, float, Vector3)[8];
		nearbySubjects.distances = new float[8];
		nearbySubjects.itemSubjects = new GrabbableObject[8];
		nearbySubjects.otherPlayers = new List<PlayerControllerB>(StartOfRound.Instance.allPlayerScripts.Length);
		ResetInfo();
	}

	public void ResetInfo()
	{
		playersNearby.Clear();
		deadPlayersNearby = 0;
		timeSinceSpectatedPlayer = 100f;
		timeSincePlayerMetAnotherPlayer = 0f;
		currentInterestLevel = 0f;
		timeSincePlayerDied = 100f;
	}

	public void UpdateNearbySubjects()
	{
		if (isConnected && !playerScript.isPlayerControlled && !playerScript.isPlayerDead)
		{
			isConnected = false;
			nearbySubjects.ClearSubjectsInfo();
			ResetInfo();
			nearbySubjects.ClearDistances();
			return;
		}
		isConnected = true;
		float num = 0f;
		FindPlayersNearby(20f, includeLOS: true);
		nearbySubjects.otherPlayers.Clear();
		for (int i = 0; i < playersNearby.Count; i++)
		{
			nearbySubjects.otherPlayers.Add(StartOfRound.Instance.allPlayerScripts[playersNearby[i]]);
		}
		nearbySubjects.ClearSubjectsInfo();
		nearbySubjects.ClearDistances();
		bool flag = false;
		int num2 = Mathf.Min(RoundManager.Instance.SpawnedEnemies.Count, nearbySubjects.enemySubjects.Length);
		for (int j = 0; j < num2; j++)
		{
			num = 10000f;
			flag = false;
			if (RoundManager.Instance.SpawnedEnemies[j].isEnemyDead)
			{
				nearbySubjects.distances[j] = 10000f;
				nearbySubjects.enemySubjects[j].dist = 10000f;
				nearbySubjects.enemySubjects[j].ai = null;
				nearbySubjects.enemySubjects[j].position = Vector3.zero;
				continue;
			}
			if (RoundManager.Instance.SpawnedEnemies[j].isOutside == playerScript.isInsideFactory)
			{
				if (!RoundManager.Instance.SpawnedEnemies[j].enemyType.isDaytimeEnemy || RoundManager.Instance.SpawnedEnemies[j].enemyType.EnemySize != EnemySize.Tiny)
				{
					flag = true;
					if (RoundManager.Instance.SpawnedEnemies[j].isOutside)
					{
						num = Vector3.Distance(RoundManager.Instance.SpawnedEnemies[j].transform.position, CinespecManager.Instance.outsideMainEntrancePos);
						if (num < 20f)
						{
							num += Vector3.Distance(playerBody.position, CinespecManager.Instance.insideMainEntrancePos);
						}
					}
					else
					{
						num = Vector3.Distance(RoundManager.Instance.SpawnedEnemies[j].transform.position, CinespecManager.Instance.insideMainEntrancePos);
						if (num < 20f)
						{
							num += Vector3.Distance(playerBody.position, CinespecManager.Instance.outsideMainEntrancePos);
						}
					}
					if (RoundManager.Instance.SpawnedEnemies[j].enemyType == CinespecManager.Instance.maskEnemyType)
					{
						num *= 0.5f;
					}
				}
			}
			else
			{
				num = Vector3.Distance(RoundManager.Instance.SpawnedEnemies[j].transform.position, playerBody.transform.position);
			}
			if (num == 10000f || num < 30f || (num < 50f && RoundManager.Instance.SpawnedEnemies[j].eye != null && !Physics.Linecast(playerBody.position, RoundManager.Instance.SpawnedEnemies[j].eye.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
			{
				nearbySubjects.distances[j] = num;
				nearbySubjects.enemySubjects[j].dist = num;
				nearbySubjects.enemySubjects[j].ai = RoundManager.Instance.SpawnedEnemies[j];
				if (flag)
				{
					if (RoundManager.Instance.SpawnedEnemies[j].isOutside)
					{
						nearbySubjects.enemySubjects[j].position = CinespecManager.Instance.insideMainEntrancePos;
					}
					else
					{
						nearbySubjects.enemySubjects[j].position = CinespecManager.Instance.outsideMainEntrancePos;
					}
				}
				else
				{
					nearbySubjects.enemySubjects[j].position = RoundManager.Instance.SpawnedEnemies[j].transform.position + Vector3.up * 0.25f;
				}
			}
			else
			{
				nearbySubjects.distances[j] = 10000f;
				nearbySubjects.enemySubjects[j].dist = 10000f;
				nearbySubjects.enemySubjects[j].ai = null;
			}
		}
		Array.Sort(nearbySubjects.distances, nearbySubjects.enemySubjects);
		Array.Reverse(nearbySubjects.enemySubjects);
		nearbySubjects.ClearDistances();
		int a = Physics.OverlapSphereNonAlloc(playerBody.position, 12f, RoundManager.Instance.tempColliderResults, 64, QueryTriggerInteraction.Ignore);
		num2 = Mathf.Min(a, nearbySubjects.itemSubjects.Length);
		for (int k = 0; k < num2; k++)
		{
			GrabbableObject component = RoundManager.Instance.tempColliderResults[k].GetComponent<GrabbableObject>();
			if (component != null && !component.isHeld)
			{
				nearbySubjects.itemSubjects[k] = component;
				nearbySubjects.distances[k] = Vector3.Distance(component.transform.position, playerBody.position);
			}
			else
			{
				nearbySubjects.itemSubjects[k] = null;
				nearbySubjects.distances[k] = 100000f;
			}
		}
		Array.Sort(nearbySubjects.distances, nearbySubjects.itemSubjects);
		Array.Reverse(nearbySubjects.enemySubjects);
		nearbySubjects.ClearDistances();
		int length = Physics.OverlapSphereNonAlloc(playerBody.position, 18f, RoundManager.Instance.tempColliderResults, 2097152, QueryTriggerInteraction.Ignore);
		Array.Sort(RoundManager.Instance.tempColliderResults, 0, length, CinespecManager.Instance.distanceToPointComparer);
		num2 = Mathf.Min(a, nearbySubjects.mapHazardSubjects.Length);
		for (int l = 0; l < num2; l++)
		{
			if (RoundManager.Instance.tempColliderResults[l].TryGetComponent<IIndoorMapHazard>(out var component2) && component2.IsActivated())
			{
				nearbySubjects.mapHazardSubjects[l].hazardous = component2.IsExtremelyHazardous();
				nearbySubjects.mapHazardSubjects[l].position = RoundManager.Instance.tempColliderResults[l].transform.position;
				nearbySubjects.mapHazardSubjects[l].dist = Vector3.Distance(RoundManager.Instance.tempColliderResults[l].transform.position, playerBody.position);
			}
		}
	}

	public void UpdatePlayerBody()
	{
		bodyDoesntExist = false;
		if (playerScript.isPlayerDead)
		{
			if (playerScript.deadBody != null)
			{
				playerBody = playerScript.deadBody.bodyParts[5].transform;
			}
			else
			{
				bodyDoesntExist = true;
			}
		}
		else
		{
			playerBody = playerScript.bodyParts[5];
		}
	}

	public float FindPlayersNearby(float minDistance, bool includeLOS)
	{
		playersNearby.Clear();
		deadPlayersNearby = 0;
		UpdatePlayerBody();
		if (bodyDoesntExist)
		{
			deadPlayersNearby = 0;
			isAlone = true;
			return 10000f;
		}
		float num = -1f;
		bool flag = false;
		float num2 = 10000f;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i] == playerScript || !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled)
			{
				continue;
			}
			Transform transform = StartOfRound.Instance.allPlayerScripts[i].bodyParts[5].transform;
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && StartOfRound.Instance.allPlayerScripts[i].deadBody != null)
			{
				transform = StartOfRound.Instance.allPlayerScripts[i].deadBody.bodyParts[5].transform;
				flag = true;
			}
			else
			{
				flag = false;
			}
			num = Vector3.Distance(playerBody.position, transform.position);
			if (num < minDistance)
			{
				if (num < num2)
				{
					num2 = num;
				}
				playersNearby.Add(i);
				if (flag)
				{
					deadPlayersNearby++;
				}
			}
			else if (includeLOS && num < minDistance * 1.6f && !Physics.Linecast(StartOfRound.Instance.allPlayerScripts[i].playerEye.position, playerScript.playerEye.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				if (num < num2)
				{
					num2 = num;
				}
				playersNearby.Add(i);
				if (flag)
				{
					deadPlayersNearby++;
				}
			}
		}
		if (!playerScript.isPlayerDead && playersNearby.Count - deadPlayersNearby > 0)
		{
			if (isAlone)
			{
				isAlone = false;
				timeSincePlayerMetAnotherPlayer = 0f;
			}
			return num2;
		}
		isAlone = true;
		return 10000f;
	}
}
