using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

[Serializable]
public class CameraSubjects
{
	public List<PlayerControllerB> otherPlayers;

	public (EnemyAI ai, float dist, Vector3 position)[] enemySubjects;

	public (bool hazardous, float dist, Vector3 position)[] mapHazardSubjects;

	public GrabbableObject[] itemSubjects;

	public float[] distances;

	public float nearestDistance;

	public void ClearSubjectsInfo()
	{
		for (int i = 0; i < enemySubjects.Length; i++)
		{
			enemySubjects[i].ai = null;
			enemySubjects[i].dist = 10000f;
			enemySubjects[i].position = Vector3.zero;
		}
		for (int j = 0; j < itemSubjects.Length; j++)
		{
			itemSubjects[j] = null;
		}
		for (int k = 0; k < mapHazardSubjects.Length; k++)
		{
			mapHazardSubjects[k].position = Vector3.zero;
			mapHazardSubjects[k].dist = 10000f;
			mapHazardSubjects[k].hazardous = false;
		}
	}

	public void ClearDistances()
	{
		for (int i = 0; i < distances.Length; i++)
		{
			distances[i] = 10000f;
		}
	}

	public int GetEnemiesAmount()
	{
		int num = 0;
		for (int i = 0; i < enemySubjects.Length && !(enemySubjects[i].ai == null); i++)
		{
			num++;
		}
		return num;
	}
}
