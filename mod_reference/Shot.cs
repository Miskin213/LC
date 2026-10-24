using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Shot : MonoBehaviour
{
	[Tooltip("The amount of tries we give the shot from varying angles, etc.")]
	public int MaxShotAttempts = 5;

	[Tooltip("This shot is always placed the same, and thus it does not use MaxShotAttempts and only needs its quality checked once")]
	public bool alwaysTheSameShot;

	[Tooltip("A multiplier on the amount of time it takes for the shot to become disqualified after the subject becomes fully obstructed")]
	public float subjectNotVisibleTolerance = 1f;

	public float MaxDistanceFromSubject = 50f;

	public float MinDistanceFromSubject = 1f;

	[Space(3f)]
	public float MinShotLength = 0.5f;

	public float MaxShotLength = 10f;

	[Space(3f)]
	public float shotFOV;

	[Space(3f)]
	[Range(0f, 2f)]
	public float ShotRarityMultiplier = 1f;

	public (Vector3 shotPos, Vector3 shotRot) potentialShotPosition;

	public List<Func<Vector3, Vector3, float>> ShotQualityEvaluations = new List<Func<Vector3, Vector3, float>>();

	public List<Func<float>> ShotConditions = new List<Func<float>>();

	public (Vector3 shotPos, Vector3 shotRot) testAngle;

	public Vector3 subject;

	public Vector3 subjectViewportPos;

	public Vector3 mainSubjectVelocity;

	public Vector3 mainSubjectLookDir;

	public Vector3 shotDir;

	public float shotQuality = -1f;

	public float shotPotential = -1f;

	public int attemptNumber;

	public int currentShotChosenAttempt;

	public int cameraMask = 1124076289;

	public bool shotDisabled;

	public void UpdateCurrentInfo()
	{
		subject = CinespecManager.Instance.spectatedPlayerPosition;
	}

	public virtual void UpdateCalculationVariables(Vector3 shotPos, Vector3 shotRot)
	{
		CinespecManager.Instance.tempCamera.fieldOfView = shotFOV;
		CinespecManager.Instance.tempCamera.transform.position = shotPos;
		CinespecManager.Instance.tempCamera.transform.eulerAngles = shotRot;
		subjectViewportPos = CinespecManager.Instance.tempCamera.WorldToViewportPoint(subject);
		shotDir = GetForwardFromEuler(shotRot);
		GetSubjectVelocityOrLookDir();
	}

	public void GetSubjectVelocityOrLookDir()
	{
		if (CinespecManager.Instance.spectatedPlayer.timeSincePlayerMoving > 0.25f || !CinespecManager.Instance.spectatedPlayer.isPlayerControlled)
		{
			mainSubjectVelocity = Vector3.zero;
		}
		else if (CinespecManager.Instance.spectatedPlayer.IsOwner)
		{
			mainSubjectVelocity = Vector3.Normalize(CinespecManager.Instance.spectatedPlayer.thisController.velocity * 100f);
		}
		else
		{
			mainSubjectVelocity = Vector3.Normalize((CinespecManager.Instance.spectatedPlayer.serverPlayerPosition - CinespecManager.Instance.spectatedPlayer.oldPlayerPosition) * 100f);
		}
		mainSubjectLookDir = CinespecManager.Instance.spectatedPlayer.playerEye.transform.forward;
		if (mainSubjectVelocity == Vector3.zero || Vector3.Angle(CinespecManager.Instance.spectatedPlayer.transform.forward, mainSubjectVelocity) > 90f)
		{
			mainSubjectVelocity = mainSubjectLookDir;
		}
		mainSubjectVelocity = Vector3.Normalize(mainSubjectVelocity * 1000f);
		Debug.DrawRay(CinespecManager.Instance.spectatedPlayer.transform.position + Vector3.up * 0.15f, mainSubjectVelocity, Color.yellow, 0.25f);
	}

	public float TestShotsForHighestQuality(float currentShotQuality, out int chosenAttemptNumber)
	{
		float num = 0f;
		float num2 = 0f;
		int num3 = -1;
		attemptNumber = 0;
		UpdateCurrentInfo();
		if (alwaysTheSameShot && CinespecManager.Instance.currentShot == this)
		{
			if (CinespecManager.Instance.logShotQualityEvaluators)
			{
				Debug.Log("Shot type '{gameObject.name}' is already the current shot and only has one iteration; returning quality based on current shot");
			}
			chosenAttemptNumber = currentShotChosenAttempt;
			return currentShotQuality;
		}
		for (int i = 0; i < MaxShotAttempts; i++)
		{
			testAngle = PlaceShot();
			if (testAngle.shotPos == Vector3.zero)
			{
				continue;
			}
			UpdateCalculationVariables(testAngle.shotPos, testAngle.shotRot);
			num2 = GetShotQuality(testAngle.shotPos, testAngle.shotRot);
			if (num2 > num)
			{
				num = num2;
				potentialShotPosition = testAngle;
				num3 = attemptNumber;
				if (CinespecManager.Instance.logShotQualityEvaluators)
				{
					Debug.Log($"Shot type '{gameObject.name}' testing quality for all shots. Highest quality for this shot type so far: {num}");
				}
			}
			if (CinespecManager.Instance.logPotentialShotPositions)
			{
				if (num2 <= 0f)
				{
					Debug.DrawRay(testAngle.shotPos, Vector3.up * 0.25f, Color.white, 0.25f);
					Debug.DrawRay(testAngle.shotPos, shotDir, Color.red, 0.25f);
				}
				else
				{
					Debug.DrawRay(testAngle.shotPos, Vector3.up * 0.25f, Color.white, 0.25f);
					Debug.DrawRay(testAngle.shotPos, shotDir, Color.yellow, 0.25f);
				}
			}
			attemptNumber++;
		}
		if (CinespecManager.Instance.logPotentialShotPositions)
		{
			Debug.DrawRay(potentialShotPosition.shotPos, Vector3.up * 0.25f, Color.magenta, 0.25f);
			Debug.DrawRay(potentialShotPosition.shotPos + Vector3.up * 0.02f, shotDir * 1.5f, Color.green, 0.25f);
		}
		if (CinespecManager.Instance.logShotQualityEvaluators && num3 != -1)
		{
			Debug.Log($"Shot type '{gameObject.name}' Chose shot type #{num3}! With quality {num}");
		}
		chosenAttemptNumber = num3;
		return num;
	}

	public abstract (Vector3 shotPos, Vector3 shotRot) PlaceShot();

	public virtual void Start()
	{
		if (CinespecManager.Instance == null)
		{
			Debug.LogError("Cinematic spectate manager was not found!");
			return;
		}
		AddGlobalShotQualityEvaluations();
		AddSpecificShotQualityEvaluations();
		AddSpecificShotConditions();
	}

	public abstract void AddSpecificShotQualityEvaluations();

	public virtual void OnChoseShot(Vector3 pos, Quaternion rot)
	{
	}

	public virtual void OnExitShot(Shot nextShot, bool choseNewAngle)
	{
	}

	public float GetShotQuality(Vector3 camPos, Vector3 camRot)
	{
		if (shotDisabled)
		{
			return -1f;
		}
		if (CinespecManager.Instance.logShotQualityEvaluators)
		{
			Debug.Log($"Shot Type '{gameObject.name}' Attempt #{attemptNumber}: testing position: {camRot} euler: {camRot}  --------------- ");
		}
		float num = 0.5f;
		int num2 = 0;
		try
		{
			foreach (Func<Vector3, Vector3, float> shotQualityEvaluation in ShotQualityEvaluations)
			{
				float num3 = shotQualityEvaluation(camPos, camRot);
				num *= num3;
				if (CinespecManager.Instance.logShotQualityEvaluators)
				{
					Debug.Log($"Shot Type '{gameObject.name}' --- Evaluation '{shotQualityEvaluation.Method.Name}' result: {num3} ; quality after: {num}");
				}
				if (num <= 0f)
				{
					if (CinespecManager.Instance.logShotQualityEvaluators)
					{
						Debug.Log("Shot quality'" + gameObject.name + "' FAILED on evaluation '" + shotQualityEvaluation.Method.Name + "'");
					}
					if (CinespecManager.Instance.checkingCurrentShot)
					{
						CinespecManager.Instance.currentShotQualityFail = num2;
					}
					return 0f;
				}
				num2++;
			}
			return num;
		}
		catch (Exception arg)
		{
			Debug.Log($"Cinespec: ERROR getting shot '{gameObject.name}' quality! Errored at condition '{ShotQualityEvaluations[num2]}' : {arg}");
			return 0f;
		}
	}

	public abstract void AddSpecificShotConditions();

	public float GetShotPotential()
	{
		UpdateCurrentInfo();
		shotPotential = 0.5f;
		int num = 0;
		try
		{
			foreach (Func<float> shotCondition in ShotConditions)
			{
				float num2 = shotCondition();
				if (CinespecManager.Instance.logShotConditions)
				{
					Debug.Log($"Shot potential of '{gameObject.name}' --- Condition '{shotCondition.Method.Name}' result: {num2} ; potential after: {shotPotential}");
				}
				shotPotential *= num2;
				if (shotPotential <= 0f)
				{
					if (CinespecManager.Instance.logShotConditions)
					{
						Debug.Log("Shot potential '" + gameObject.name + "' FAILED on condition '" + shotCondition.Method.Name + "'");
					}
					return 0f;
				}
				num++;
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"Cinespec: ERROR getting shot '{gameObject.name}' potential! Errored at condition '{ShotConditions[num]}' : {arg}");
			return 0f;
		}
		return shotPotential;
	}

	public abstract void MoveCameraForShot();

	public float IsPlayerInShip()
	{
		if (CinespecManager.Instance.spectatedPlayer.isInElevator && (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded))
		{
			return 0f;
		}
		return 1f;
	}

	public Vector3 GetForwardFromEuler(Vector3 eulers)
	{
		float f = eulers.x * (MathF.PI / 180f);
		float f2 = eulers.y * (MathF.PI / 180f);
		float num = Mathf.Cos(f);
		return new Vector3(Mathf.Sin(f2) * num, 0f - Mathf.Sin(f), Mathf.Cos(f2) * num);
	}

	private void AddGlobalShotQualityEvaluations()
	{
		ShotQualityEvaluations.Add(IsSubjectVisible);
		ShotQualityEvaluations.Add(SubjectDistance);
		ShotQualityEvaluations.Add(IsSubjectInFrame);
		ShotQualityEvaluations.Add(SeesWhereSubjectIsLooking);
	}

	public float CanSeeOtherSubjects(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.currentPlayerInfoId == -1)
		{
			return 1f;
		}
		PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.currentPlayerInfoId];
		float num = 1f;
		for (int i = 0; i < playerCinespecInfo.nearbySubjects.otherPlayers.Count; i++)
		{
			if (!(Mathf.Abs(playerCinespecInfo.nearbySubjects.otherPlayers[i].transform.position.y - playerCinespecInfo.playerScript.transform.position.y) > 12.5f * RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].MapTileSize))
			{
				num = ((!(Vector3.Angle(playerCinespecInfo.nearbySubjects.otherPlayers[i].bodyParts[5].transform.position - shotPos, GetForwardFromEuler(shotRot)) < 68f) || Physics.Linecast(shotPos, playerCinespecInfo.nearbySubjects.otherPlayers[i].bodyParts[5].transform.position, cameraMask, QueryTriggerInteraction.Ignore)) ? (num * Mathf.Lerp(0.5f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 50f, 1f))) : (num * Mathf.Lerp(2f, 1.25f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 50f, 1f))));
			}
		}
		for (int j = 0; j < playerCinespecInfo.nearbySubjects.enemySubjects.Length && !(playerCinespecInfo.nearbySubjects.enemySubjects[j].ai == null); j++)
		{
			if ((playerCinespecInfo.nearbySubjects.enemySubjects[j].ai.isOutside != CinespecManager.Instance.spectatedPlayer.isInsideFactory || !(playerCinespecInfo.nearbySubjects.enemySubjects[j].ai.enemyType != CinespecManager.Instance.maskEnemyType)) && !(Mathf.Abs(playerCinespecInfo.nearbySubjects.enemySubjects[j].position.y - playerCinespecInfo.playerScript.transform.position.y) > 12.5f * RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].MapTileSize))
			{
				num = ((!(Vector3.Angle(playerCinespecInfo.nearbySubjects.enemySubjects[j].position - shotPos, GetForwardFromEuler(shotRot)) < 68f) || Physics.Linecast(shotPos, playerCinespecInfo.nearbySubjects.enemySubjects[j].position, cameraMask, QueryTriggerInteraction.Ignore)) ? (num * Mathf.Lerp(0.5f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[j].dist / 50f, 1f))) : (num * Mathf.Lerp(2f, 1.25f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[j].dist / 50f, 1f))));
			}
		}
		for (int k = 0; k < playerCinespecInfo.nearbySubjects.mapHazardSubjects.Length; k++)
		{
			if (!(playerCinespecInfo.nearbySubjects.mapHazardSubjects[k].position == Vector3.zero) && !(Mathf.Abs(playerCinespecInfo.nearbySubjects.mapHazardSubjects[k].position.y - playerCinespecInfo.playerScript.transform.position.y) > 12.5f * RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].MapTileSize))
			{
				bool flag = Vector3.Angle(playerCinespecInfo.nearbySubjects.mapHazardSubjects[k].position - shotPos, GetForwardFromEuler(shotRot)) < 68f && !Physics.Linecast(shotPos, playerCinespecInfo.nearbySubjects.mapHazardSubjects[k].position, cameraMask, QueryTriggerInteraction.Ignore);
				if (playerCinespecInfo.nearbySubjects.mapHazardSubjects[k].hazardous)
				{
					num = ((!flag) ? (num * Mathf.Lerp(0.5f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[k].dist / 50f, 1f))) : (num * Mathf.Lerp(1.6f, 1.05f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[k].dist / 50f, 1f))));
				}
				else
				{
					num = ((!flag) ? (num * Mathf.Lerp(0.8f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[k].dist / 50f, 1f))) : (num * Mathf.Lerp(1.3f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[k].dist / 50f, 1f))));
				}
			}
		}
		for (int l = 0; l < playerCinespecInfo.nearbySubjects.itemSubjects.Length && !(playerCinespecInfo.nearbySubjects.itemSubjects[l] == null); l++)
		{
			bool flag = Vector3.Angle(playerCinespecInfo.nearbySubjects.itemSubjects[l].transform.position - shotPos, GetForwardFromEuler(shotRot)) < 72f;
			if (!(StartOfRound.Instance.occlusionCuller.currentTile != null) || StartOfRound.Instance.occlusionCuller.currentTile.Bounds.Contains(playerCinespecInfo.nearbySubjects.itemSubjects[l].transform.position))
			{
				num = ((!flag) ? (num * 0.9f) : (num * 1.15f));
			}
		}
		return num;
	}

	public float IsSubjectVisible(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.checkingCurrentShot)
		{
			return Mathf.Lerp(1f, 0f, Mathf.Min(CinespecManager.Instance.subjectNotVisibleTime / 1.25f, 1f));
		}
		Vector3 end = CinespecManager.Instance.spectatedPlayer.transform.position + Vector3.up * 0.25f;
		Vector3 position = CinespecManager.Instance.spectatedPlayer.playerEye.transform.position;
		bool flag = !Physics.Linecast(shotPos, position, cameraMask, QueryTriggerInteraction.Ignore);
		Physics.Linecast(shotPos, end, cameraMask, QueryTriggerInteraction.Ignore);
		if (!flag && !flag)
		{
			return 0f;
		}
		if (!flag || !flag)
		{
			return 0.9f;
		}
		return 1f;
	}

	public float IsSubjectInFrame(Vector3 shotPos, Vector3 shotRot)
	{
		float num = 0.1f;
		if (subjectViewportPos.z < 0f || subjectViewportPos.x < 0f + num || subjectViewportPos.x > 1f - num || subjectViewportPos.y < 0f + num || subjectViewportPos.y > 1f - num)
		{
			return 0f;
		}
		return 1f;
	}

	public float SubjectDistance(Vector3 shotPos, Vector3 shotRot)
	{
		float sqrMagnitude = (subject - shotPos).sqrMagnitude;
		if (sqrMagnitude > MaxDistanceFromSubject * MaxDistanceFromSubject)
		{
			return 0f;
		}
		if (MinDistanceFromSubject > 0f && sqrMagnitude < MinDistanceFromSubject * MinDistanceFromSubject)
		{
			return Mathf.Min(1f, Vector3.Distance(shotPos, subject) / MinDistanceFromSubject);
		}
		return 1f;
	}

	public float SeesWhereSubjectIsLooking(Vector3 shotPos, Vector3 shotRot)
	{
		float num = 1f;
		float num2 = 1f;
		PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.spectatedPlayer.playerClientId];
		if (playerCinespecInfo.nearbySubjects.otherPlayers.Count > 0 || (playerCinespecInfo.nearbySubjects.enemySubjects[0].ai != null && playerCinespecInfo.nearbySubjects.enemySubjects[0].dist < 20f))
		{
			num2 = 0.45f;
		}
		Vector3 origin = ((!CinespecManager.Instance.spectatedPlayer.isPlayerDead) ? CinespecManager.Instance.spectatedPlayer.playerEye.position : subject);
		float num3 = ((!(shotPos.y > -100f)) ? 10f : 25f);
		Ray ray = new Ray(origin, mainSubjectLookDir);
		Vector3 vector = ((!Physics.Raycast(ray, out var hitInfo, num3, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(num3 - 0.5f) : ray.GetPoint(hitInfo.distance - 0.5f));
		float num4 = Vector3.Angle(shotDir, vector - shotPos);
		if (num4 > 65f)
		{
			num = (CinespecManager.Instance.spectatedPlayer.isInsideFactory ? (num - num * 0.4f * num2) : (num - num * 0.6f * num2));
		}
		else if (num4 < 50f)
		{
			num += num * 0.15f * num2;
			if (!Physics.Linecast(shotPos, vector, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				num += num * 0.15f * num2;
			}
		}
		return num;
	}
}
