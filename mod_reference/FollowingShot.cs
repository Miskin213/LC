using DunGen.Tags;
using UnityEngine;

public class FollowingShot : Shot
{
	private Vector3[] followPoints;

	private Vector3 subjectPosition;

	private Vector3 prevSubjectPosition;

	private int currentCamPoint = -1;

	private float trackPlace;

	public float lerpSpeed = 2f;

	public float trackSpeed = 1f;

	private Quaternion rotAtStartOfTrackPlace;

	private Vector3 vel;

	private float timeSinceSpectatedPlayerSprinted;

	private float timeSpentTooClose;

	private float timeSpentOutOfFrame;

	public Tag caveTileTag;

	private Vector3 setCamPointTo;

	public override void AddSpecificShotConditions()
	{
		ShotConditions.Add(IsPlayerInDanger);
		ShotConditions.Add(IsPlayerIndoors);
		ShotConditions.Add(IsPlayerRunningOrInCaves);
		ShotConditions.Add(IsPlayerInShip);
	}

	public float IsPlayerInDanger()
	{
		PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.currentPlayerInfoId];
		for (int i = 0; i < playerCinespecInfo.nearbySubjects.enemySubjects.Length && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai == null) && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist > 20f); i++)
		{
			if (!(Mathf.Abs(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai.transform.position.y - playerCinespecInfo.playerScript.transform.position.y) > 12.5f * RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].MapTileSize))
			{
				return Mathf.Lerp(1.3f, 0.1f, playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 20f);
			}
		}
		return 0f;
	}

	private float IsPlayerIndoors()
	{
		if (CinespecManager.Instance.spectatedPlayer == null || !CinespecManager.Instance.spectatedPlayer.isInsideFactory)
		{
			return 0.04f;
		}
		return 1f;
	}

	private float IsPlayerRunningOrInCaves()
	{
		float num = 1f;
		if (timeSinceSpectatedPlayerSprinted < 3f)
		{
			float num2 = 1f;
			if (CinespecManager.Instance.spectatedPlayer.IsOwner)
			{
				num2 *= 0.6f;
			}
			float b = 0f;
			PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.currentPlayerInfoId];
			if (playerCinespecInfo.nearbySubjects.enemySubjects[0].ai != null && playerCinespecInfo.nearbySubjects.enemySubjects[0].dist < 14f)
			{
				b = 0.6f;
			}
			num = Mathf.Max((float)CinespecManager.Instance.spectatedPlayer.stepsTakenWhileSprinting / (6f * num2), b);
		}
		else
		{
			num = 0f;
		}
		Debug.Log($"!!! FOLLOW CAM:   Is sprinting? : {num > 0f}");
		if (num == 0f && CinespecManager.Instance.spectatedPlayer.isInsideFactory && CinespecManager.Instance.spectatedPlayer.isCrouching && RoundManager.Instance.currentDungeonType == 4 && StartOfRound.Instance.occlusionCuller.currentTile != null && StartOfRound.Instance.occlusionCuller.currentTile.Tags.Tags.Contains(caveTileTag))
		{
			num = 1f;
		}
		return num;
	}

	public override void AddSpecificShotQualityEvaluations()
	{
		ShotQualityEvaluations.Add(CamTooCloseToSubject);
		ShotQualityEvaluations.Add(CanSeeEnemyBehind);
	}

	private float CanSeeEnemyBehind(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.currentPlayerInfoId == -1)
		{
			return 1f;
		}
		PlayerCinespecInfo playerCinespecInfo = CinespecManager.Instance.playersInfo[CinespecManager.Instance.currentPlayerInfoId];
		float num = 1f;
		for (int i = 0; i < playerCinespecInfo.nearbySubjects.enemySubjects.Length && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai == null) && !(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist > 12f); i++)
		{
			num = ((!(Vector3.Distance(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai.transform.position, shotPos) < 4.5f) && !(Vector3.Angle(playerCinespecInfo.nearbySubjects.enemySubjects[i].ai.transform.position - shotPos, GetForwardFromEuler(shotRot)) < 80f)) ? (num * Mathf.Lerp(0.75f, 1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 50f, 1f))) : (num * Mathf.Lerp(1.6f, 1.1f, Mathf.Min(playerCinespecInfo.nearbySubjects.enemySubjects[i].dist / 50f, 1f))));
		}
		return num;
	}

	private float CamTooCloseToSubject(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.currentShot == this)
		{
			if (timeSpentTooClose > 0.35f)
			{
				return 0f;
			}
			return 1f;
		}
		if (Vector3.Distance(shotPos, CinespecManager.Instance.spectatedPlayer.bodyParts[5].transform.position) < 1f)
		{
			return 0f;
		}
		return 1f;
	}

	private void AvoidOutOfBounds()
	{
		if (!Physics.Linecast(CinespecManager.Instance.spectatedPlayer.bodyParts[5].transform.position, CinespecManager.Instance.camPoint.position, cameraMask, QueryTriggerInteraction.Ignore) || Physics.Linecast(CinespecManager.Instance.camPoint.position, CinespecManager.Instance.spectatedPlayer.bodyParts[5].transform.position, cameraMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		int num = -1;
		float num2 = 1000f;
		for (int i = 0; i < followPoints.Length; i++)
		{
			float num3 = Vector3.Distance(followPoints[i], CinespecManager.Instance.camPoint.position);
			if (num3 < num2)
			{
				num2 = num3;
				num = i;
			}
		}
		CinespecManager.Instance.currentAngle.position = followPoints[num];
		CinespecManager.Instance.camPoint.position = followPoints[num];
	}

	public override void MoveCameraForShot()
	{
		if (currentCamPoint == -1 || currentCamPoint >= followPoints.Length - 1)
		{
			currentCamPoint = followPoints.Length / 2;
		}
		if (currentCamPoint > 1)
		{
			float num = 1f;
			if (currentCamPoint <= 2)
			{
				num = 0.5f - trackPlace;
			}
			trackPlace += Time.deltaTime * trackSpeed * num * (Vector3.Distance(CinespecManager.Instance.currentAngle.position, subject) * 5f);
			AvoidOutOfBounds();
			float t = Mathf.Clamp(trackPlace, 0f, 0.5f);
			setCamPointTo = Vector3.MoveTowards(setCamPointTo, GetPointBezier(followPoints[currentCamPoint + 1], followPoints[currentCamPoint], followPoints[currentCamPoint - 1], t), lerpSpeed * 2f);
			CinespecManager.Instance.currentAngle.position = Vector3.Lerp(CinespecManager.Instance.currentAngle.position, setCamPointTo, 20f * Time.deltaTime);
			CinespecManager.Instance.camPoint.position = Vector3.Lerp(CinespecManager.Instance.camPoint.position, CinespecManager.Instance.currentAngle.position, Time.deltaTime * 3f * num);
			RoundManager.Instance.tempTransform.position = CinespecManager.Instance.currentAngle.position;
			RoundManager.Instance.tempTransform.forward = GetFirstDerivative(followPoints[currentCamPoint + 1], followPoints[currentCamPoint], followPoints[currentCamPoint - 1], t);
			Quaternion quaternion = Quaternion.RotateTowards(rotAtStartOfTrackPlace, RoundManager.Instance.tempTransform.rotation, lerpSpeed * 270f);
			RoundManager.Instance.tempTransform.rotation = quaternion;
			float num2 = Vector3.Angle(RoundManager.Instance.tempTransform.forward, subject - CinespecManager.Instance.camPoint.position);
			if (num2 > 66f)
			{
				float t2 = 66f / num2;
				quaternion = Quaternion.Lerp(quaternion, Quaternion.Euler(subject - CinespecManager.Instance.camPoint.position), t2);
			}
			CinespecManager.Instance.currentAngle.rotation = Quaternion.Slerp(CinespecManager.Instance.currentAngle.rotation, quaternion, 18f * Time.deltaTime * num);
			if (trackPlace >= 0.5f)
			{
				trackPlace = 0f;
				currentCamPoint--;
				rotAtStartOfTrackPlace = CinespecManager.Instance.currentAngle.rotation;
			}
			CinespecManager.Instance.camPoint.rotation = Quaternion.Slerp(CinespecManager.Instance.camPoint.rotation, CinespecManager.Instance.currentAngle.rotation, Time.deltaTime * 5f);
		}
	}

	public override (Vector3 shotPos, Vector3 shotRot) PlaceShot()
	{
		if (CinespecManager.Instance.currentShot == this)
		{
			return (shotPos: CinespecManager.Instance.camPoint.position, shotRot: CinespecManager.Instance.camPoint.rotation.eulerAngles);
		}
		RoundManager.Instance.tempTransform.position = followPoints[3];
		RoundManager.Instance.tempTransform.LookAt(subject);
		setCamPointTo = RoundManager.Instance.tempTransform.position;
		return (shotPos: RoundManager.Instance.tempTransform.position, shotRot: RoundManager.Instance.tempTransform.eulerAngles);
	}

	public override void OnChoseShot(Vector3 pos, Quaternion rot)
	{
		base.OnChoseShot(pos, rot);
		CinespecManager.Instance.camPoint.position = CinespecManager.Instance.currentAngle.position;
		CinespecManager.Instance.camPoint.rotation = CinespecManager.Instance.currentAngle.rotation;
		setCamPointTo = CinespecManager.Instance.currentAngle.position;
		rotAtStartOfTrackPlace = CinespecManager.Instance.camPoint.rotation;
	}

	public override void Start()
	{
		base.Start();
		followPoints = new Vector3[16];
		ShotQualityEvaluations.Remove(IsSubjectInFrame);
	}

	private bool IsInFrame()
	{
		CinespecManager.Instance.tempCamera.fieldOfView = shotFOV;
		CinespecManager.Instance.tempCamera.transform.position = CinespecManager.Instance.camPoint.position;
		CinespecManager.Instance.tempCamera.transform.eulerAngles = CinespecManager.Instance.camPoint.eulerAngles;
		subjectViewportPos = CinespecManager.Instance.tempCamera.WorldToViewportPoint(subject);
		float num = 0.1f;
		if (subjectViewportPos.z < 0f || subjectViewportPos.x < 0f + num || subjectViewportPos.x > 1f - num || subjectViewportPos.y < 0f + num || subjectViewportPos.y > 1f - num)
		{
			return false;
		}
		return true;
	}

	public float IsSubjectInFrameLenient()
	{
		if (CinespecManager.Instance.checkingCurrentShot)
		{
			if (timeSpentOutOfFrame > 0.33f)
			{
				return 0f;
			}
			return 1f - timeSpentOutOfFrame / 0.58000004f;
		}
		float num = 0.1f;
		if (subjectViewportPos.z < 0f || subjectViewportPos.x < 0f + num || subjectViewportPos.x > 1f - num || subjectViewportPos.y < 0f + num || subjectViewportPos.y > 1f - num)
		{
			return 0f;
		}
		return 1f;
	}

	public Vector3 GetPointBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		return Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t);
	}

	public Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
	}

	public Vector3 GetVelocity(Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		return transform.TransformPoint(GetFirstDerivative(p1, p2, p3, t));
	}

	public void Update()
	{
		if (!(CinespecManager.Instance.spectatedPlayer == null) && CinespecManager.Instance.cinespecActive)
		{
			IncrementValues();
			UpdatePlayerTrail();
		}
	}

	private void IncrementValues()
	{
		if (!CinespecManager.Instance.localClientIsCalculating)
		{
			return;
		}
		if ((CinespecManager.Instance.spectatedPlayer.IsOwner && CinespecManager.Instance.spectatedPlayer.isSprinting) || (!CinespecManager.Instance.spectatedPlayer.IsOwner && CinespecManager.Instance.spectatedPlayer.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting")))
		{
			timeSinceSpectatedPlayerSprinted = 0f;
		}
		else
		{
			timeSinceSpectatedPlayerSprinted += Time.deltaTime;
		}
		if (CinespecManager.Instance.currentShot == this)
		{
			if (!IsInFrame())
			{
				timeSpentOutOfFrame = Mathf.Min(timeSpentOutOfFrame + Time.deltaTime, 0.33f);
			}
			else
			{
				timeSpentOutOfFrame = 0f;
			}
		}
		else
		{
			timeSpentOutOfFrame = 0f;
		}
		if (CinespecManager.Instance.currentShot == this && Vector3.Distance(CinespecManager.Instance.camPoint.position, CinespecManager.Instance.spectatedPlayer.bodyParts[5].transform.position) < 1f)
		{
			timeSpentTooClose += Time.deltaTime;
		}
		else
		{
			timeSpentTooClose = 0f;
		}
	}

	private void UpdatePlayerTrail()
	{
		if ((subjectPosition - CinespecManager.Instance.spectatedPlayer.transform.position).sqrMagnitude > 2.8f)
		{
			prevSubjectPosition = subjectPosition;
			subjectPosition = CinespecManager.Instance.spectatedPlayer.bodyParts[5].transform.position;
			for (int num = followPoints.Length - 1; num > 0; num--)
			{
				followPoints[num] = followPoints[num - 1];
			}
			followPoints[0] = prevSubjectPosition;
			currentCamPoint++;
			rotAtStartOfTrackPlace = CinespecManager.Instance.currentAngle.rotation;
		}
	}
}
