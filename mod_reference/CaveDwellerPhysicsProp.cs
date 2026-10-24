using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class CaveDwellerPhysicsProp : GrabbableObject
{
	public CaveDwellerAI caveDwellerScript;

	public PlayerControllerB previousPlayerHeldBy;

	private float timeSinceRockingBaby;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!IsOwner)
		{
			return;
		}
		if (buttonDown)
		{
			if (Time.realtimeSinceStartup - timeSinceRockingBaby < 0.25f)
			{
				SetRockingBabyServerRpc(rockHard: true);
				caveDwellerScript.rockingBaby = 2;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 2);
			}
			else
			{
				SetRockingBabyServerRpc(rockHard: false);
				caveDwellerScript.rockingBaby = 1;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 1);
			}
			timeSinceRockingBaby = Time.realtimeSinceStartup;
		}
		else
		{
			playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 0);
			caveDwellerScript.rockingBaby = 0;
			StopRockingBabyServerRpc();
		}
	}

	[ServerRpc]
	public void SetRockingBabyServerRpc(bool rockHard)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			if (OwnerClientId != networkManager.LocalClientId)
			{
				if (networkManager.LogLevel <= LogLevel.Normal)
				{
					Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
				}
				return;
			}
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(565448931u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in rockHard, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 565448931u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetRockingBabyClientRpc(rockHard);
		}
	}

	[ClientRpc]
	public void SetRockingBabyClientRpc(bool rockHard)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2063180704u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in rockHard, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2063180704u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!IsOwner)
		{
			if (rockHard)
			{
				caveDwellerScript.rockingBaby = 2;
			}
			else
			{
				caveDwellerScript.rockingBaby = 1;
			}
		}
	}

	[ServerRpc]
	public void StopRockingBabyServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			if (OwnerClientId != networkManager.LocalClientId)
			{
				if (networkManager.LogLevel <= LogLevel.Normal)
				{
					Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
				}
				return;
			}
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(1666766308u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1666766308u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			StopRockingBabyClientRpc();
		}
	}

	[ClientRpc]
	public void StopRockingBabyClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(156507906u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 156507906u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				caveDwellerScript.rockingBaby = 0;
			}
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		Debug.Log("Equip item function");
		caveDwellerScript.PickUpBabyLocalClient();
		previousPlayerHeldBy = playerHeldBy;
		Debug.Log($"Baby prop script reached floor target Equipped : {reachedFloorTarget} ");
	}

	[ServerRpc(RequireOwnership = false)]
	public void DropBabyServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(3482843676u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendServerRpc(ref bufferWriter, 3482843676u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				DropBabyClientRpc(playerId);
			}
		}
	}

	[ClientRpc]
	public void DropBabyClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1216205938u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 1216205938u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (playerId != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				caveDwellerScript.DropBabyLocalClient();
			}
		}
	}

	public override void FallWithCurve()
	{
		if (caveDwellerScript.inSpecialAnimation)
		{
			base.FallWithCurve();
		}
	}

	public override void Start()
	{
		propColliders = gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!propColliders[i].CompareTag("DoNotSet") && !propColliders[i].CompareTag("Enemy"))
			{
				propColliders[i].excludeLayers = -2621449;
			}
		}
		originalScale = transform.localScale;
		if (itemProperties.itemSpawnsOnGround)
		{
			startFallingPosition = transform.position;
			if (transform.parent != null)
			{
				startFallingPosition = transform.parent.InverseTransformPoint(startFallingPosition);
			}
			FallToGround();
		}
		else
		{
			fallTime = 1f;
			hasHitGround = true;
			reachedFloorTarget = true;
			targetFloorPosition = transform.localPosition;
		}
		if (itemProperties.isScrap)
		{
			fallTime = 1f;
			hasHitGround = true;
		}
		if (itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
		{
			radarIcon = Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		if (!itemProperties.isScrap)
		{
			HoarderBugAI.grabbableObjectsInMap.Add(gameObject);
		}
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].renderingLayerMask = 1u;
		}
		SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int k = 0; k < componentsInChildren2.Length; k++)
		{
			componentsInChildren2[k].renderingLayerMask = 1u;
		}
	}

	public override void Update()
	{
		if (isHeld && playerHeldBy == GameNetworkManager.Instance.localPlayerController && caveDwellerScript.rockingBaby > 0)
		{
			if (caveDwellerScript.rockingBaby < 2 && StartOfRound.Instance.fearLevel > 0.75f)
			{
				caveDwellerScript.rockingBaby = 2;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 2);
				SetRockingBabyServerRpc(rockHard: true);
			}
			else if (StartOfRound.Instance.fearLevel < 0.6f && caveDwellerScript.rockingBaby > 2)
			{
				caveDwellerScript.rockingBaby = 1;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 1);
				SetRockingBabyServerRpc(rockHard: false);
			}
		}
		if (currentUseCooldown >= 0f)
		{
			currentUseCooldown -= Time.deltaTime;
		}
		if (IsOwner)
		{
			if (isBeingUsed && itemProperties.requiresBattery)
			{
				if (insertedBattery.charge > 0f)
				{
					if (!itemProperties.itemIsTrigger)
					{
						insertedBattery.charge -= Time.deltaTime / itemProperties.batteryUsage;
					}
				}
				else if (!insertedBattery.empty)
				{
					insertedBattery.empty = true;
					if (isBeingUsed)
					{
						Debug.Log("Use up batteries local");
						isBeingUsed = false;
						UseUpBatteries();
						UseUpItemBatteriesServerRpc();
					}
				}
			}
			if (!wasOwnerLastFrame)
			{
				wasOwnerLastFrame = true;
			}
		}
		else if (wasOwnerLastFrame)
		{
			wasOwnerLastFrame = false;
		}
		if (!isHeld && parentObject == null)
		{
			if (fallTime < 1f)
			{
				reachedFloorTarget = false;
				FallWithCurve();
				if (transform.localPosition.y - targetFloorPosition.y < 0.05f && !hasHitGround)
				{
					PlayDropSFX();
					OnHitGround();
				}
				return;
			}
			if (!reachedFloorTarget)
			{
				if (!hasHitGround)
				{
					PlayDropSFX();
					OnHitGround();
				}
				reachedFloorTarget = true;
				if (floorYRot == -1)
				{
					transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, transform.eulerAngles.y, itemProperties.restingRotation.z);
				}
				else
				{
					transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z);
				}
			}
			if (caveDwellerScript.inSpecialAnimation)
			{
				transform.localPosition = targetFloorPosition;
			}
		}
		else if (isHeld || isHeldByEnemy)
		{
			reachedFloorTarget = false;
		}
	}

	public override void LateUpdate()
	{
		if (caveDwellerScript.inSpecialAnimation && parentObject != null)
		{
			transform.rotation = parentObject.rotation;
			transform.Rotate(itemProperties.rotationOffset);
			transform.position = parentObject.position;
			Vector3 positionOffset = itemProperties.positionOffset;
			positionOffset = parentObject.rotation * positionOffset;
			transform.position += positionOffset;
		}
		if (radarIcon != null)
		{
			radarIcon.position = transform.position;
		}
	}

	public override void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger") && !componentsInChildren[i].gameObject.CompareTag("Enemy"))
			{
				componentsInChildren[i].enabled = enable;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].enabled = enable;
			Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + componentsInChildren2[j].gameObject.name);
		}
	}

	public override void DiscardItem()
	{
		GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("RockBaby", 0);
		Debug.Log("Maneater: Discard function called");
		caveDwellerScript.DropBabyLocalClient();
		DropBabyServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		previousPlayerHeldBy = playerHeldBy;
		base.DiscardItem();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(565448931u, __rpc_handler_565448931, "SetRockingBabyServerRpc");
		__registerRpc(2063180704u, __rpc_handler_2063180704, "SetRockingBabyClientRpc");
		__registerRpc(1666766308u, __rpc_handler_1666766308, "StopRockingBabyServerRpc");
		__registerRpc(156507906u, __rpc_handler_156507906, "StopRockingBabyClientRpc");
		__registerRpc(3482843676u, __rpc_handler_3482843676, "DropBabyServerRpc");
		__registerRpc(1216205938u, __rpc_handler_1216205938, "DropBabyClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_565448931(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
		{
			if (networkManager.LogLevel <= LogLevel.Normal)
			{
				Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
			}
		}
		else
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).SetRockingBabyServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2063180704(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).SetRockingBabyClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1666766308(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
		{
			if (networkManager.LogLevel <= LogLevel.Normal)
			{
				Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
			}
		}
		else
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).StopRockingBabyServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_156507906(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).StopRockingBabyClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3482843676(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).DropBabyServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1216205938(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CaveDwellerPhysicsProp)target).DropBabyClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "CaveDwellerPhysicsProp";
	}
}
