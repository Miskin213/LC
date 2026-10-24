using UnityEngine;

public class ShipSecurityCamShot : Shot
{
	public Transform[] shipCamPositions;

	public Transform shipMountedCam;

	public override void Start()
	{
		base.Start();
		MaxShotAttempts = shipCamPositions.Length;
		shipMountedCam.SetParent(StartOfRound.Instance.elevatorTransform);
		for (int i = 0; i < shipCamPositions.Length; i++)
		{
			shipCamPositions[i].SetParent(StartOfRound.Instance.elevatorTransform);
		}
	}

	public override void AddSpecificShotConditions()
	{
		ShotConditions.Add(IsPlayerInShip);
	}

	public new float IsPlayerInShip()
	{
		if (CinespecManager.Instance.spectatedPlayer.isInElevator)
		{
			if (CinespecManager.Instance.spectatedPlayer.isInHangarShipRoom)
			{
				return 1.5f;
			}
			return 1f;
		}
		return 0f;
	}

	public override void AddSpecificShotQualityEvaluations()
	{
		ShotQualityEvaluations.Add(CanSeeOtherSubjects);
	}

	public override void MoveCameraForShot()
	{
		CinespecManager.Instance.currentAngle.position = shipMountedCam.position;
		CinespecManager.Instance.currentAngle.rotation = shipMountedCam.rotation;
	}

	public override (Vector3 shotPos, Vector3 shotRot) PlaceShot()
	{
		return (shotPos: shipCamPositions[attemptNumber].position, shotRot: shipCamPositions[attemptNumber].eulerAngles);
	}

	public override void OnChoseShot(Vector3 pos, Quaternion rot)
	{
		shipMountedCam.position = pos;
		shipMountedCam.rotation = rot;
		CinespecManager.Instance.camPoint.SetParent(shipMountedCam, worldPositionStays: true);
		CinespecManager.Instance.camPoint.localPosition = Vector3.zero;
		CinespecManager.Instance.camPoint.localEulerAngles = Vector3.zero;
	}

	public override void OnExitShot(Shot nextShot, bool choseNewAngle)
	{
		base.OnExitShot(nextShot, choseNewAngle);
		if (nextShot != this)
		{
			CinespecManager.Instance.camPoint.SetParent(CinespecManager.Instance.gameObject.transform);
		}
	}
}
