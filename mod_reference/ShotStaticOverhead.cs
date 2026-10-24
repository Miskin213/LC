using System.Collections.Generic;
using DunGen;
using UnityEngine;

public class ShotStaticOverhead : Shot
{
	private Vector3 pos;

	private (Vector3 shotPos, Vector3 shotRot) previousShot;

	private bool panToNearShot;

	private List<Tile> adjacentTiles = new List<Tile>();

	private Quaternion panningStartRot;

	private float panTimer;

	public AnimationCurve rotateCurve;

	private Vector3 vel = Vector3.zero;

	private Vector3 previousSubjectViewportPosition;

	public override void Start()
	{
		base.Start();
	}

	public override void AddSpecificShotConditions()
	{
		ShotConditions.Add(IsPlayerInShip);
	}

	public override void AddSpecificShotQualityEvaluations()
	{
		ShotQualityEvaluations.Add(WatchingDoorAheadOfSubject);
		ShotQualityEvaluations.Add(OnReadablePositionOnScreen);
		ShotQualityEvaluations.Add(FollowsRuleOfThirds);
		ShotQualityEvaluations.Add(CanSeeOtherSubjects);
	}

	public override void MoveCameraForShot()
	{
		if (panToNearShot)
		{
			CinespecManager.Instance.camPoint.rotation = Quaternion.Slerp(panningStartRot, CinespecManager.Instance.currentAngle.rotation, rotateCurve.Evaluate(panTimer));
			panTimer = Mathf.Clamp(panTimer + Time.deltaTime * 1.33f, 0f, 1f);
		}
		else
		{
			CinespecManager.Instance.camPoint.position = CinespecManager.Instance.currentAngle.position;
			CinespecManager.Instance.camPoint.rotation = CinespecManager.Instance.currentAngle.rotation;
		}
	}

	public override void UpdateCalculationVariables(Vector3 shotPos, Vector3 shotRot)
	{
		base.UpdateCalculationVariables(shotPos, shotRot);
		if (previousSubjectViewportPosition == Vector3.zero)
		{
			previousSubjectViewportPosition = subjectViewportPos;
		}
	}

	public override (Vector3 shotPos, Vector3 shotRot) PlaceShot()
	{
		RoundManager.Instance.tempTransform.position = subject;
		RoundManager.Instance.tempTransform.eulerAngles = Vector3.zero;
		RoundManager.Instance.tempTransform.Rotate(Vector3.up, 360f / (float)MaxShotAttempts * (float)(attemptNumber + 1));
		Vector3 forward = RoundManager.Instance.tempTransform.forward;
		forward.y = 0f;
		float num;
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead)
		{
			forward *= 0.35f;
			num = 4f;
		}
		else
		{
			num = Random.Range(5f, 12f);
		}
		Ray ray = new Ray(subject, Vector3.up * Random.Range(0.25f, 0.6f) + forward);
		if (Physics.Raycast(ray, out var hitInfo, num, cameraMask, QueryTriggerInteraction.Ignore))
		{
			pos = ray.GetPoint(hitInfo.distance - 0.5f);
		}
		else
		{
			pos = ray.GetPoint(num - 0.5f);
		}
		RoundManager.Instance.tempTransform.position = pos;
		RoundManager.Instance.tempTransform.LookAt(subject, Vector3.up);
		Camera tempCamera = CinespecManager.Instance.tempCamera;
		UpdateCalculationVariables(pos, RoundManager.Instance.tempTransform.eulerAngles);
		float num2 = ((!(mainSubjectVelocity == Vector3.zero)) ? Vector3.Dot(mainSubjectVelocity, RoundManager.Instance.tempTransform.right) : 0f);
		Debug.DrawRay(Vector3.zero, mainSubjectVelocity, Color.red, 2f);
		Debug.Log($"Moving right: {num2 > 0f} ; {num2}");
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead || (num2 > -0.25f && num2 < 0.25f))
		{
			RoundManager.Instance.tempTransform.localEulerAngles = new Vector3(RoundManager.Instance.tempTransform.localEulerAngles.x + Random.Range(-15f, 15f), RoundManager.Instance.tempTransform.localEulerAngles.y + 50f * num2, RoundManager.Instance.tempTransform.localEulerAngles.z);
			return (shotPos: pos, shotRot: RoundManager.Instance.tempTransform.eulerAngles);
		}
		float num3 = tempCamera.fieldOfView / 6f;
		float num4 = Camera.VerticalToHorizontalFieldOfView(tempCamera.fieldOfView, tempCamera.aspect) / 6f;
		float x = 0f - num3 + Random.Range(5f, 16f);
		bool flag = num2 > 0f;
		float num5 = 0f;
		num5 = ((!flag) ? (0f - num4) : num4);
		RoundManager.Instance.tempTransform.localEulerAngles += new Vector3(x, num5, 0f);
		return (shotPos: pos, shotRot: RoundManager.Instance.tempTransform.eulerAngles);
	}

	private float OnReadablePositionOnScreen(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.checkingCurrentShot)
		{
			return 1f;
		}
		RoundManager.Instance.tempTransform.position = CinespecManager.Instance.camPoint.position;
		RoundManager.Instance.tempTransform.eulerAngles = CinespecManager.Instance.camPoint.eulerAngles;
		CinespecManager.Instance.tempCamera.transform.position = CinespecManager.Instance.camPoint.position;
		CinespecManager.Instance.tempCamera.transform.eulerAngles = CinespecManager.Instance.camPoint.eulerAngles;
		bool flag = false;
		_ = CinespecManager.Instance.spectatedPlayer.isSprinting;
		Vector3 position = subject + mainSubjectVelocity * subjectViewportPos.z;
		Vector3 vector = CinespecManager.Instance.tempCamera.WorldToViewportPoint(position);
		Vector3 vector2 = CinespecManager.Instance.tempCamera.WorldToViewportPoint(subject);
		if (Mathf.Abs(vector.x - 0.5f) - Mathf.Abs(vector2.x - 0.5f) >= 0.2f && Mathf.Abs(vector2.x) > 0.7f)
		{
			flag = true;
		}
		float num = Vector3.Dot(mainSubjectVelocity, RoundManager.Instance.tempTransform.right);
		Vector3 vector3 = default;
		if (flag)
		{
			if (num >= 0.2f)
			{
				vector3.x = 0.2f;
			}
			else if (num <= -0.2f)
			{
				vector3.x = 0.8f;
			}
			else
			{
				vector3.x = subjectViewportPos.x / 2f;
			}
		}
		else
		{
			vector3.x = subjectViewportPos.x;
		}
		vector3.y = 0.35f;
		return Mathf.Clamp(1f - Vector2.Distance(new Vector2(vector3.x, vector3.y), new Vector2(subjectViewportPos.x, subjectViewportPos.y)), 0.5f, 1.5f);
	}

	private float WatchingDoorAheadOfSubject(Vector3 shotPos, Vector3 shotRot)
	{
		if (!CinespecManager.Instance.spectatedPlayer.isInsideFactory)
		{
			return 1f;
		}
		float num = 1f;
		Tile tileSubjectIsIn = GetTileSubjectIsIn();
		if (tileSubjectIsIn != null)
		{
			for (int i = 0; i < tileSubjectIsIn.UsedDoorways.Count; i++)
			{
				Vector3 position = tileSubjectIsIn.UsedDoorways[i].transform.position;
				if (Vector3.Angle(mainSubjectVelocity, position - subject) < 70f && Vector3.Distance(subject, position) < 15f)
				{
					if (Vector3.Angle(position, shotDir) < 70f)
					{
						if (Physics.Linecast(shotPos, position, out var hitInfo, cameraMask, QueryTriggerInteraction.Ignore))
						{
							num = ((!((hitInfo.point - position).sqrMagnitude < 16f)) ? (num * 1.05f) : (num * 1.2f));
						}
						else
						{
							num *= 1.2f;
						}
					}
					else
					{
						num *= 0.5f;
					}
				}
			}
			return num;
		}
		return 1f;
	}

	private Tile GetTileSubjectIsIn()
	{
		if (StartOfRound.Instance.occlusionCuller.currentTile == null)
		{
			return null;
		}
		if (StartOfRound.Instance.occlusionCuller.currentTile.Bounds.Contains(subject))
		{
			return StartOfRound.Instance.occlusionCuller.currentTile;
		}
		if (adjacentTiles.Count > 0)
		{
			for (int i = 0; i < adjacentTiles.Count; i++)
			{
				if (adjacentTiles[i].Bounds.Contains(subject))
				{
					return adjacentTiles[i];
				}
			}
		}
		foreach (Tile adjacentTile in StartOfRound.Instance.occlusionCuller.currentTile.GetAdjacentTiles())
		{
			adjacentTiles.Add(adjacentTile);
			if (adjacentTile.Bounds.Contains(subject))
			{
				return adjacentTile;
			}
		}
		return null;
	}

	private float FollowsRuleOfThirds(Vector3 shotPos, Vector3 shotRot)
	{
		if (CinespecManager.Instance.spectatedPlayer.isPlayerDead)
		{
			return 1f;
		}
		float num = Vector3.Dot(mainSubjectVelocity, RoundManager.Instance.tempTransform.right);
		if (num > -0.25f && num < 0.25f)
		{
			return 1f;
		}
		float num2 = Mathf.Min(Mathf.Abs(subjectViewportPos.x - 0.33f), Mathf.Abs(subjectViewportPos.x - 0.66f));
		float num3 = Mathf.Min(Mathf.Abs(subjectViewportPos.y - 0.33f), Mathf.Abs(subjectViewportPos.y - 0.66f));
		float num4 = 0.16f;
		return Mathf.Lerp(1f + num4, 1f - num4, (num2 + num3) / 2f / 0.33f);
	}

	public override void OnChoseShot(Vector3 pos, Quaternion rot)
	{
		IngamePlayerSettings.Instance.ResetMotionBlur();
		UpdateCalculationVariables(CinespecManager.Instance.currentAngle.position, CinespecManager.Instance.currentAngle.rotation.eulerAngles);
		previousSubjectViewportPosition = subjectViewportPos;
	}

	public override void OnExitShot(Shot nextShot, bool choseNewAngle)
	{
		base.OnExitShot(nextShot, choseNewAngle);
		adjacentTiles.Clear();
		if (nextShot == this)
		{
			if (!choseNewAngle)
			{
				return;
			}
			bool flag = panToNearShot;
			previousShot = (shotPos: CinespecManager.Instance.currentAngle.position, shotRot: CinespecManager.Instance.currentAngle.rotation.eulerAngles);
			bool flag2 = Vector3.Distance(previousShot.shotPos, potentialShotPosition.shotPos) < 4f;
			if (flag2)
			{
				CinespecManager.Instance.tempCamera.transform.position = previousShot.shotPos;
				CinespecManager.Instance.tempCamera.transform.eulerAngles = potentialShotPosition.shotRot;
				Vector3 vector = CinespecManager.Instance.tempCamera.WorldToViewportPoint(subject);
				if (vector.x < 0.3f || (double)vector.x > 0.7 || vector.y > 0.7f || vector.y < 0.3f)
				{
					flag2 = false;
				}
				else if (Physics.Linecast(previousShot.shotPos, subject, cameraMask, QueryTriggerInteraction.Ignore))
				{
					flag2 = false;
				}
			}
			panToNearShot = flag2;
			panningStartRot = CinespecManager.Instance.camPoint.rotation;
			panTimer = 0f;
			if (!flag)
			{
				vel = Vector3.zero;
			}
		}
		else
		{
			panToNearShot = false;
			previousShot.shotPos = Vector3.one;
		}
	}
}
