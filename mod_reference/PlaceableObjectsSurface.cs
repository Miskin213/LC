using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PlaceableObjectsSurface : NetworkBehaviour
{
	public NetworkObject parentTo;

	public Collider placeableBounds;

	public InteractTrigger triggerScript;

	private float checkHoverTipInterval;

	private void Update()
	{
		if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
		{
			triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.isHoldingObject;
		}
	}

	public void PlaceObject(PlayerControllerB playerWhoTriggered)
	{
		if (playerWhoTriggered.isHoldingObject && !playerWhoTriggered.isGrabbingObjectAnimation && playerWhoTriggered.currentlyHeldObjectServer != null)
		{
			Debug.Log("Placing object in storage; asking server for verification");
			if (!(itemPlacementPosition(playerWhoTriggered.gameplayCamera.transform, playerWhoTriggered.currentlyHeldObjectServer) == Vector3.zero))
			{
				CheckIfFurnitureIsAvailableForPlacingServerRpc((int)playerWhoTriggered.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CheckIfFurnitureIsAvailableForPlacingServerRpc(int playerWhoTriggered)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(2624827950u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
			__endSendServerRpc(ref bufferWriter, 2624827950u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (parentTo.IsSpawned)
		{
			PlaceableShipObject componentInChildren = parentTo.gameObject.GetComponentInChildren<PlaceableShipObject>();
			if (componentInChildren != null)
			{
				componentInChildren.lastTimeScrapWasPlaced = Time.realtimeSinceStartup;
			}
			ConfirmPlaceOnFurnitureClientRpc(playerWhoTriggered);
		}
	}

	[ClientRpc]
	public void ConfirmPlaceOnFurnitureClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3757423328u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 3757423328u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
		{
			return;
		}
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		if (!localPlayerController.isHoldingObject || localPlayerController.isGrabbingObjectAnimation || !(localPlayerController.currentlyHeldObjectServer != null))
		{
			return;
		}
		Debug.Log("Placing object in storage");
		Vector3 vector = itemPlacementPosition(localPlayerController.gameplayCamera.transform, localPlayerController.currentlyHeldObjectServer);
		if (!(vector == Vector3.zero))
		{
			if (parentTo != null)
			{
				vector = parentTo.transform.InverseTransformPoint(vector);
			}
			localPlayerController.DiscardHeldObject(placeObject: true, parentTo, vector, matchRotationOfParent: false);
			Debug.Log("discard held object called from placeobject");
		}
	}

	private Vector3 itemPlacementPosition(Transform gameplayCamera, GrabbableObject heldObject)
	{
		if (Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out var hitInfo, 7f, 1073744640, QueryTriggerInteraction.Ignore))
		{
			if (placeableBounds.ClosestPoint(hitInfo.point) == hitInfo.point)
			{
				Debug.DrawLine(hitInfo.point, hitInfo.point + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset, Color.red, 10f);
				return hitInfo.point + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset;
			}
			Debug.DrawLine(placeableBounds.ClosestPoint(hitInfo.point), placeableBounds.ClosestPoint(hitInfo.point) + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset, Color.green, 10f);
			return placeableBounds.ClosestPoint(hitInfo.point) + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset;
		}
		return Vector3.zero;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2624827950u, __rpc_handler_2624827950, "CheckIfFurnitureIsAvailableForPlacingServerRpc");
		__registerRpc(3757423328u, __rpc_handler_3757423328, "ConfirmPlaceOnFurnitureClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2624827950(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((PlaceableObjectsSurface)target).CheckIfFurnitureIsAvailableForPlacingServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3757423328(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((PlaceableObjectsSurface)target).ConfirmPlaceOnFurnitureClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "PlaceableObjectsSurface";
	}
}
