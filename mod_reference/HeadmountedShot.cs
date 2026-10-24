using UnityEngine;

public class HeadmountedShot : Shot
{
	public bool faceOutward;

	private bool testFaceOutward;

	public Transform mountedCamPoint;

	public Transform testMountedCamPoint;

	private Vector3 shakeTurn;

	private float subjectYRotLastFrame;

	public EnemyType SnareFleaEnemyType;

	public bool CreateMountedCamPoint()
	{
		bool result = false;
		if (mountedCamPoint == null)
		{
			mountedCamPoint = new GameObject("MountedCamPoint").transform;
			result = true;
		}
		if (testMountedCamPoint == null)
		{
			testMountedCamPoint = new GameObject("TestMountedCamPoint").transform;
			result = true;
		}
		return result;
	}

	public override void AddSpecificShotConditions()
	{
		ShotConditions.Add(IsPlayerBodyNull);
		ShotConditions.Add(IsPlayerInSteamOrInDanger);
		ShotConditions.Add(PlayerHasSnareFlea);
	}

	private float PlayerHasSnareFlea()
	{
		for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++)
		{
			if (!RoundManager.Instance.SpawnedEnemies[i].isOutside && !(RoundManager.Instance.SpawnedEnemies[i].enemyType != SnareFleaEnemyType) && RoundManager.Instance.SpawnedEnemies[i].currentBehaviourStateIndex == 3)
			{
				CentipedeAI centipedeAI = RoundManager.Instance.SpawnedEnemies[i] as CentipedeAI;
				if (centipedeAI != null && centipedeAI.inSpecialAnimation && centipedeAI.clingingToPlayer == CinespecManager.Instance.spectatedPlayer)
				{
					return 1.75f;
				}
			}
		}
		return 1f;
	}

	private float IsPlayerInSteamOrInDanger()
	{
		float num = 0f;
		PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.currentPlayerInfoId];
		for (int i = 0; i < playerCinespecInfo.nearbySubjects.enemySubjects.Length && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai == null) && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist > 20f); i++)
		{
			if (!(Mathf.Abs(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai.transform.position.y - playerCinespecInfo.playerScript.transform.position.y) > 12.5f * RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].MapTileSize))
			{
				num = Mathf.Lerp(1.3f, 0.1f, playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 20f);
			}
		}
		if (RoundManager.Instance.currentDungeonType != 2)
		{
			for (int j = 0; j < RoundManager.Instance.steamValves.Length; j++)
			{
				float num2 = RoundManager.Instance.steamValves[j].currentFogSize * RoundManager.Instance.steamValves[j].fogSizeMultiplier * 22f;
				num2 *= num2;
				if ((subject - RoundManager.Instance.steamValves[j].transform.position).sqrMagnitude < num2)
				{
					num += 1.75f;
				}
			}
		}
		return num;
	}

	private float IsPlayerBodyNull()
	{
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead && CinespecManager.Instance.spectatedPlayer.deadBody == null)
		{
			return 0f;
		}
		return 1f;
	}

	public override void AddSpecificShotQualityEvaluations()
	{
		ShotQualityEvaluations.Add(CanSeeOtherSubjects);
		ShotQualityEvaluations.Add(FaceInwardWithSnareFlea);
		ShotQualityEvaluations.Remove(IsSubjectVisible);
		ShotQualityEvaluations.Remove(SubjectDistance);
		ShotQualityEvaluations.Remove(IsSubjectInFrame);
		ShotQualityEvaluations.Remove(SeesWhereSubjectIsLooking);
	}

	private float FaceInwardWithSnareFlea(Vector3 shotPos, Vector3 shotRot)
	{
		if (!testFaceOutward && PlayerHasSnareFlea() > 1f)
		{
			return 1.05f;
		}
		return 1f;
	}

	public override void MoveCameraForShot()
	{
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead && CinespecManager.Instance.spectatedPlayer.deadBody == null)
		{
			return;
		}
		if (CreateMountedCamPoint())
		{
			SetMountedCamPosition(currentShotChosenAttempt == 0);
		}
		if (!CinespecManager.Instance.spectatedPlayer.isPlayerDead)
		{
			_ = CinespecManager.Instance.spectatedPlayer.bodyParts[0];
		}
		else
		{
			_ = CinespecManager.Instance.spectatedPlayer.deadBody.bodyParts[0].transform;
		}
		if (!faceOutward)
		{
			mountedCamPoint.localPosition = new Vector3(0f, 0.27f, 0.41f);
		}
		else
		{
			mountedCamPoint.localPosition = new Vector3(0f, 0.05f, 0.25f);
		}
		mountedCamPoint.localEulerAngles = Vector3.zero;
		if (!faceOutward)
		{
			mountedCamPoint.Rotate(27.381f, 180f, 0f, Space.Self);
		}
		if (CinespecManager.Instance.spectatedPlayer != null)
		{
			float num = 110f;
			if (faceOutward)
			{
				num = 60f;
			}
			float b = Mathf.DeltaAngle(subjectYRotLastFrame, CinespecManager.Instance.spectatedPlayer.transform.localEulerAngles.y) / (Time.deltaTime * num);
			float t = 1f - Mathf.Exp(-30f * Time.deltaTime);
			shakeTurn = new Vector3(0f, Mathf.LerpAngle(shakeTurn.y, b, t), 0f);
			mountedCamPoint.localEulerAngles += shakeTurn;
			subjectYRotLastFrame = CinespecManager.Instance.spectatedPlayer.transform.localEulerAngles.y;
		}
		else
		{
			shakeTurn = Vector3.zero;
		}
		CinespecManager.Instance.currentAngle.position = mountedCamPoint.position;
		CinespecManager.Instance.currentAngle.rotation = mountedCamPoint.rotation;
		CinespecManager.Instance.camPoint.position = mountedCamPoint.position;
		CinespecManager.Instance.camPoint.rotation = mountedCamPoint.rotation;
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -180f)
		{
			angle += 360f;
		}
		if (angle > 180f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}

	public override void OnChoseShot(Vector3 pos, Quaternion rot)
	{
		base.OnChoseShot(pos, rot);
		SetMountedCamPosition(pos, rot);
		shakeTurn = Vector3.zero;
		faceOutward = currentShotChosenAttempt == 0;
	}

	public override void Start()
	{
		base.Start();
		MaxShotAttempts = 2;
		CreateMountedCamPoint();
	}

	private void SetMountedCamPosition(bool faceOutward, bool test = false)
	{
		Transform transform = mountedCamPoint;
		if (test)
		{
			transform = testMountedCamPoint;
		}
		Transform parent = (CinespecManager.Instance.spectatedPlayer.isPlayerDead ? CinespecManager.Instance.spectatedPlayer.deadBody.bodyParts[0].transform : CinespecManager.Instance.spectatedPlayer.bodyParts[0]);
		transform.SetParent(parent, worldPositionStays: true);
		if (!faceOutward)
		{
			transform.localPosition = new Vector3(0f, 0.27f, 0.41f);
		}
		else
		{
			transform.localPosition = new Vector3(0f, -0.1f, 0.25f);
		}
		transform.localEulerAngles = Vector3.zero;
		if (!faceOutward)
		{
			transform.Rotate(27.381f, 180f, 0f, Space.Self);
		}
	}

	private void SetMountedCamPosition(Vector3 pos, Quaternion rot)
	{
		Transform parent = (CinespecManager.Instance.spectatedPlayer.isPlayerDead ? CinespecManager.Instance.spectatedPlayer.deadBody.bodyParts[0].transform : CinespecManager.Instance.spectatedPlayer.bodyParts[0]);
		mountedCamPoint.SetParent(parent, worldPositionStays: true);
		mountedCamPoint.position = pos;
		mountedCamPoint.rotation = rot;
	}

	public override (Vector3 shotPos, Vector3 shotRot) PlaceShot()
	{
		CreateMountedCamPoint();
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead && CinespecManager.Instance.spectatedPlayer.deadBody == null)
		{
			return (shotPos: Vector3.zero, shotRot: Vector3.zero);
		}
		SetMountedCamPosition(attemptNumber == 0, test: true);
		testFaceOutward = attemptNumber == 0;
		return (shotPos: testMountedCamPoint.position, shotRot: testMountedCamPoint.eulerAngles);
	}
}
