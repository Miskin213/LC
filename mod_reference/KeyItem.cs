using UnityEngine;

public class KeyItem : GrabbableObject
{
	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (playerHeldBy == null || !IsOwner || !Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, 3f, 2816))
		{
			return;
		}
		DoorLock doorLock = hitInfo.transform.GetComponent<DoorLock>();
		if (doorLock == null)
		{
			TriggerPointToDoor component = hitInfo.transform.GetComponent<TriggerPointToDoor>();
			if (component != null)
			{
				doorLock = component.pointToDoor;
			}
		}
		if (doorLock != null && doorLock.isLocked && !doorLock.isPickingLock)
		{
			doorLock.UnlockDoorSyncWithServer();
			playerHeldBy.DespawnHeldObject();
		}
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
		return "KeyItem";
	}
}
