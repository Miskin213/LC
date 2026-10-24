using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class AutoParentToShip : NetworkBehaviour
{
	public bool disableObject;

	public Vector3 positionOffset;

	public Vector3 rotationOffset;

	[HideInInspector]
	public Vector3 startingPosition;

	[HideInInspector]
	public Vector3 startingRotation;

	public bool overrideOffset;

	public int unlockableID = -1;

	private void Awake()
	{
		if (!overrideOffset)
		{
			positionOffset = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(transform.position);
			rotationOffset = StartOfRound.Instance.elevatorTransform.InverseTransformDirection(transform.eulerAngles);
		}
		MoveToOffset();
		PlaceableShipObject component = gameObject.GetComponent<PlaceableShipObject>();
		if (component != null && component.parentObjectSecondary != null)
		{
			startingPosition = component.parentObjectSecondary.position;
			startingRotation = component.parentObjectSecondary.eulerAngles;
		}
		else
		{
			startingPosition = positionOffset;
			startingRotation = rotationOffset;
		}
		Debug.Log($"item awake: {gameObject.name}; id: {unlockableID}");
		if (unlockableID != -1)
		{
			Debug.Log($"placedPosition: {StartOfRound.Instance.unlockablesList.unlockables[unlockableID].placedPosition}");
		}
		if (unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[unlockableID].placedPosition != Vector3.zero)
		{
			Debug.Log($"awake for {gameObject.name}; IsServer? :{IsServer}");
			Debug.Log($"ShipBuildModeManager?: {ShipBuildModeManager.Instance != null}");
			Debug.Log($"unlockableID: {unlockableID}");
			Debug.Log($"Unlockables list count: {StartOfRound.Instance.unlockablesList.unlockables.Count}");
			if (!IsServer && ShipBuildModeManager.Instance != null)
			{
				ShipBuildModeManager.Instance.PlaceShipObject(StartOfRound.Instance.unlockablesList.unlockables[unlockableID].placedPosition, StartOfRound.Instance.unlockablesList.unlockables[unlockableID].placedRotation, gameObject.GetComponentInChildren<PlaceableShipObject>(), placementSFX: false);
			}
		}
	}

	private void LateUpdate()
	{
		if (!StartOfRound.Instance.suckingFurnitureOutOfShip)
		{
			if (disableObject)
			{
				transform.position = new Vector3(800f, -100f, 0f);
			}
			else
			{
				MoveToOffset();
			}
		}
	}

	public void StartSuckingOutOfShip()
	{
		StartCoroutine(SuckObjectOutOfShip());
	}

	private IEnumerator SuckObjectOutOfShip()
	{
		Vector3 dir = Vector3.Normalize((StartOfRound.Instance.middleOfSpaceNode.position - transform.position) * 10000f);
		Debug.Log(dir);
		Quaternion randomRotation = Random.rotation;
		while (StartOfRound.Instance.suckingFurnitureOutOfShip)
		{
			yield return null;
			transform.position += dir * (Time.deltaTime * Mathf.Clamp(StartOfRound.Instance.suckingPower, 1.1f, 100f) * 17f);
			transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * randomRotation, Time.deltaTime * StartOfRound.Instance.suckingPower);
			Debug.DrawRay(transform.position + Vector3.up * 0.2f, StartOfRound.Instance.middleOfSpaceNode.position - transform.position, Color.blue);
			Debug.DrawRay(transform.position, dir, Color.green);
		}
	}

	public void MoveToOffset()
	{
		transform.rotation = StartOfRound.Instance.elevatorTransform.rotation;
		transform.Rotate(rotationOffset);
		transform.position = StartOfRound.Instance.elevatorTransform.position;
		Vector3 vector = positionOffset;
		vector = StartOfRound.Instance.elevatorTransform.rotation * vector;
		transform.position += vector;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "AutoParentToShip";
	}
}
