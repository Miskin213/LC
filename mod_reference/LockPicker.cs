using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LockPicker : GrabbableObject
{
	public AudioClip[] placeLockPickerClips;

	public AudioClip[] finishPickingLockClips;

	public Animator armsAnimator;

	private Ray ray;

	private RaycastHit hit;

	public bool isPickingLock;

	public bool isOnDoor;

	public DoorLock currentlyPickingDoor;

	private bool placeOnLockPicker1;

	private AudioSource lockPickerAudio;

	private Coroutine setRotationCoroutine;

	public override void EquipItem()
	{
		base.EquipItem();
		RetractClaws();
	}

	public override void Start()
	{
		base.Start();
		lockPickerAudio = gameObject.GetComponent<AudioSource>();
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (playerHeldBy == null || !IsOwner)
		{
			return;
		}
		ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
		if (!Physics.Raycast(ray, out hit, 3f, 2816))
		{
			return;
		}
		DoorLock doorLock = hit.transform.GetComponent<DoorLock>();
		if (doorLock == null)
		{
			TriggerPointToDoor component = hit.transform.GetComponent<TriggerPointToDoor>();
			if (component != null)
			{
				doorLock = component.pointToDoor;
			}
		}
		if (doorLock != null && doorLock.isLocked && !doorLock.isPickingLock)
		{
			playerHeldBy.DiscardHeldObject(placeObject: true, doorLock.NetworkObject, GetLockPickerDoorPosition(doorLock));
			Debug.Log("discard held object called from lock picker");
			PlaceLockPickerServerRpc(doorLock.NetworkObject, placeOnLockPicker1);
			PlaceOnDoor(doorLock, placeOnLockPicker1);
		}
	}

	private Vector3 GetLockPickerDoorPosition(DoorLock doorScript)
	{
		if (Vector3.Distance(doorScript.lockPickerPosition.position, playerHeldBy.transform.position) < Vector3.Distance(doorScript.lockPickerPosition2.position, playerHeldBy.transform.position))
		{
			placeOnLockPicker1 = true;
			return doorScript.lockPickerPosition.localPosition;
		}
		placeOnLockPicker1 = false;
		return doorScript.lockPickerPosition2.localPosition;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlaceLockPickerServerRpc(NetworkObjectReference doorObject, bool lockPicker1)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(345501982u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in doorObject, default(FastBufferWriter.ForNetworkSerializable));
				bufferWriter.WriteValueSafe(in lockPicker1, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 345501982u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PlaceLockPickerClientRpc(doorObject, lockPicker1);
			}
		}
	}

	[ClientRpc]
	public void PlaceLockPickerClientRpc(NetworkObjectReference doorObject, bool lockPicker1)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1656348772u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in doorObject, default(FastBufferWriter.ForNetworkSerializable));
			bufferWriter.WriteValueSafe(in lockPicker1, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1656348772u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (doorObject.TryGet(out var networkObject))
			{
				DoorLock componentInChildren = networkObject.gameObject.GetComponentInChildren<DoorLock>();
				PlaceOnDoor(componentInChildren, lockPicker1);
			}
			else
			{
				Debug.LogError("Lock picker was placed but we can't get the reference for the door it was placed on; placed by " + playerHeldBy.gameObject.name);
			}
		}
	}

	public void PlaceOnDoor(DoorLock doorScript, bool lockPicker1)
	{
		if (!isOnDoor)
		{
			gameObject.GetComponent<AudioSource>().PlayOneShot(placeLockPickerClips[Random.Range(0, placeLockPickerClips.Length)]);
			armsAnimator.SetBool("mounted", value: true);
			armsAnimator.SetBool("picking", value: true);
			lockPickerAudio.Play();
			Debug.Log("Playing lock picker audio");
			lockPickerAudio.pitch = Random.Range(0.94f, 1.06f);
			isOnDoor = true;
			isPickingLock = true;
			doorScript.isPickingLock = true;
			currentlyPickingDoor = doorScript;
			if (setRotationCoroutine != null)
			{
				StopCoroutine(setRotationCoroutine);
			}
			setRotationCoroutine = StartCoroutine(setRotationOnDoor(doorScript, lockPicker1));
		}
	}

	private IEnumerator setRotationOnDoor(DoorLock doorScript, bool lockPicker1)
	{
		float startTime = Time.timeSinceLevelLoad;
		yield return new WaitUntil(() => !isHeld || Time.timeSinceLevelLoad - startTime > 10f);
		Debug.Log("setting rotation of lock picker in lock picker script");
		if (lockPicker1)
		{
			transform.localEulerAngles = doorScript.lockPickerPosition.localEulerAngles;
		}
		else
		{
			transform.localEulerAngles = doorScript.lockPickerPosition2.localEulerAngles;
		}
		setRotationCoroutine = null;
	}

	private void FinishPickingLock()
	{
		if (isPickingLock)
		{
			RetractClaws();
			currentlyPickingDoor = null;
			Vector3 position = transform.position;
			transform.SetParent(null);
			startFallingPosition = position;
			FallToGround();
			lockPickerAudio.PlayOneShot(finishPickingLockClips[Random.Range(0, finishPickingLockClips.Length)]);
		}
	}

	private void RetractClaws()
	{
		isOnDoor = false;
		isPickingLock = false;
		armsAnimator.SetBool("mounted", value: false);
		armsAnimator.SetBool("picking", value: false);
		if (currentlyPickingDoor != null)
		{
			currentlyPickingDoor.isPickingLock = false;
			currentlyPickingDoor.lockPickTimeLeft = currentlyPickingDoor.maxTimeLeft;
			currentlyPickingDoor = null;
		}
		lockPickerAudio.Stop();
		Debug.Log("pausing lock picker audio");
	}

	public override void Update()
	{
		base.Update();
		if (IsServer && isPickingLock && currentlyPickingDoor != null && !currentlyPickingDoor.isLocked)
		{
			FinishPickingLock();
			FinishPickingClientRpc();
		}
	}

	[ClientRpc]
	public void FinishPickingClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2012404935u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 2012404935u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				FinishPickingLock();
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(345501982u, __rpc_handler_345501982, "PlaceLockPickerServerRpc");
		__registerRpc(1656348772u, __rpc_handler_1656348772, "PlaceLockPickerClientRpc");
		__registerRpc(2012404935u, __rpc_handler_2012404935, "FinishPickingClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_345501982(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LockPicker)target).PlaceLockPickerServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1656348772(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LockPicker)target).PlaceLockPickerClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2012404935(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LockPicker)target).FinishPickingClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "LockPicker";
	}
}
