using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class RagdollGrabbableObject : GrabbableObject
{
	public int bodyID = -1;

	public DeadBodyInfo ragdoll;

	private bool foundRagdollObject;

	private bool bodySetToHold;

	public bool testBody;

	private bool setBodyInElevator;

	private PlayerControllerB previousPlayerHeldBy;

	private bool hasBeenPlaced;

	public bool heldByEnemy;

	private bool heldByEnemyThisFrame;

	private bool clientRequestedBodyId;

	public override void Start()
	{
		base.Start();
		if (HoarderBugAI.grabbableObjectsInMap != null && !HoarderBugAI.grabbableObjectsInMap.Contains(gameObject))
		{
			HoarderBugAI.grabbableObjectsInMap.Add(gameObject);
		}
		if (radarIcon != null)
		{
			Object.Destroy(radarIcon.gameObject);
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
		hasBeenPlaced = false;
	}

	public override void OnPlaceObject()
	{
		base.OnPlaceObject();
		hasBeenPlaced = true;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (foundRagdollObject && ragdoll != null)
		{
			Object.Destroy(ragdoll.gameObject);
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void GetBodyIdRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
				{
					RequireOwnership = false
				};
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(2163292152u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
				__endSendRpc(ref bufferWriter, 2163292152u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SendClientsBodyIdRpc(bodyID);
			}
		}
	}

	[Rpc(SendTo.NotServer, RequireOwnership = false)]
	public void SendClientsBodyIdRpc(int setBodyId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
				{
					RequireOwnership = false
				};
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(1794737774u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, setBodyId);
				__endSendRpc(ref bufferWriter, 1794737774u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				bodyID = setBodyId;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (NetworkManager.Singleton.ShutdownInProgress)
		{
			return;
		}
		if (bodyID == -1)
		{
			if (!clientRequestedBodyId)
			{
				clientRequestedBodyId = true;
				GetBodyIdRpc();
			}
			grabbable = false;
			return;
		}
		if (!foundRagdollObject)
		{
			if (testBody)
			{
				DeadBodyInfo[] array = Object.FindObjectsOfType<DeadBodyInfo>();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].playerObjectId == 0)
					{
						ragdoll = array[i];
						break;
					}
				}
				ragdoll.grabBodyObject = this;
				parentObject = ragdoll.bodyParts[5].transform;
				transform.SetParent(ragdoll.bodyParts[5].transform);
				foundRagdollObject = true;
				grabbable = true;
			}
			else
			{
				if (!(StartOfRound.Instance.allPlayerScripts[bodyID].deadBody != null))
				{
					grabbable = false;
					return;
				}
				ragdoll = StartOfRound.Instance.allPlayerScripts[bodyID].deadBody;
				ragdoll.grabBodyObject = this;
				parentObject = ragdoll.bodyParts[5].transform;
				transform.SetParent(ragdoll.bodyParts[5].transform);
				foundRagdollObject = true;
				grabbable = true;
			}
		}
		if (ragdoll == null)
		{
			return;
		}
		grabbable = true;
		bool flag = hasBeenPlaced && StartOfRound.Instance.currentLevel.levelID == 3 && Object.FindObjectOfType<DepositItemsDesk>() != null && transform.parent == Object.FindObjectOfType<DepositItemsDesk>().deskObjectsContainer.transform;
		if ((isHeld || heldByEnemy) | flag)
		{
			if (flag)
			{
				ragdoll.matchPositionExactly = false;
				ragdoll.attachedLimb.isKinematic = false;
				ragdoll.speedMultiplier = 45f;
				ragdoll.maxVelocity = 0.75f;
			}
			if (!bodySetToHold)
			{
				if (heldByEnemy)
				{
					heldByEnemyThisFrame = true;
				}
				else
				{
					ragdoll.bodyBleedingHeavily = false;
				}
				grabbableToEnemies = false;
				bodySetToHold = true;
				ragdoll.gameObject.SetActive(value: true);
				ragdoll.SetBodyPartsKinematic(setKinematic: false);
				ragdoll.attachedTo = transform;
				ragdoll.attachedLimb = ragdoll.bodyParts[5];
				ragdoll.matchPositionExactly = true;
				ragdoll.lerpBeforeMatchingPosition = true;
				SetRagdollParentToMatchHoldingPlayer();
			}
		}
		else if (bodySetToHold)
		{
			bodySetToHold = false;
			grabbableToEnemies = true;
			ragdoll.attachedTo = null;
			parentObject = ragdoll.bodyParts[5].transform;
			transform.SetParent(ragdoll.bodyParts[5].transform);
			ragdoll.attachedLimb = null;
			ragdoll.matchPositionExactly = false;
			ragdoll.lerpBeforeMatchingPosition = false;
			ragdoll.StopFloatingBody();
			SetRagdollParentToMatchHoldingPlayer();
			heldByEnemyThisFrame = false;
		}
	}

	public override void GrabItemFromEnemy(EnemyAI enemy)
	{
		base.GrabItemFromEnemy(enemy);
		heldByEnemy = true;
	}

	public override void DiscardItemFromEnemy()
	{
		base.DiscardItemFromEnemy();
		heldByEnemy = false;
	}

	private void SetRagdollParentToMatchHoldingPlayer()
	{
		if (!heldByEnemyThisFrame && previousPlayerHeldBy != null)
		{
			if (previousPlayerHeldBy.isInElevator && !setBodyInElevator)
			{
				setBodyInElevator = true;
				ragdoll.transform.SetParent(StartOfRound.Instance.elevatorTransform);
			}
			else if (!previousPlayerHeldBy.isInElevator && setBodyInElevator)
			{
				setBodyInElevator = false;
				ragdoll.transform.SetParent(null);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2163292152u, __rpc_handler_2163292152, "GetBodyIdRpc");
		__registerRpc(1794737774u, __rpc_handler_1794737774, "SendClientsBodyIdRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2163292152(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((RagdollGrabbableObject)target).GetBodyIdRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1794737774(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((RagdollGrabbableObject)target).SendClientsBodyIdRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "RagdollGrabbableObject";
	}
}
