using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

public abstract class GrabbableObject : NetworkBehaviour
{
	public bool grabbable;

	public bool isHeld;

	public bool isHeldByEnemy;

	public bool deactivated;

	[Space(3f)]
	public Transform parentObject;

	public Vector3 targetFloorPosition;

	public Vector3 startFallingPosition;

	public int floorYRot;

	public float fallTime;

	public bool hasHitGround;

	[Space(5f)]
	public int scrapValue;

	public bool itemUsedUp;

	public PlayerControllerB playerHeldBy;

	public bool isPocketed;

	public bool isBeingUsed;

	public bool isInElevator;

	public bool isInShipRoom;

	public bool isInFactory = true;

	[Space(10f)]
	public float useCooldown;

	public float currentUseCooldown;

	[Space(10f)]
	public Item itemProperties;

	public Battery insertedBattery;

	public string customGrabTooltip;

	[HideInInspector]
	public Rigidbody propBody;

	[HideInInspector]
	public Collider[] propColliders;

	[HideInInspector]
	public Vector3 originalScale;

	public bool wasOwnerLastFrame;

	public MeshRenderer mainObjectRenderer;

	public bool scrapPersistedThroughRounds;

	public bool heldByPlayerOnServer;

	[HideInInspector]
	public Transform radarIcon;

	public bool reachedFloorTarget;

	[Space(3f)]
	public bool grabbableToEnemies = true;

	public bool hasBeenHeld;

	public bool rotateObject;

	public virtual int GetItemDataToSave()
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("GetItemDataToSave is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
		return 0;
	}

	public virtual void LoadItemSaveData(int saveData)
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("LoadItemSaveData is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
	}

	public virtual void InitializeAfterPositioning()
	{
	}

	public virtual void Start()
	{
		propColliders = gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!propColliders[i].CompareTag("InteractTrigger"))
			{
				propColliders[i].excludeLayers = -2621449;
			}
		}
		originalScale = transform.localScale;
		if (itemProperties.itemSpawnsOnGround)
		{
			RandomScrapSpawn[] array = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].spawnWithParent != null && array[j].spawnWithParent.transform.position == transform.position)
				{
					transform.SetParent(array[j].spawnWithParent, worldPositionStays: true);
					break;
				}
			}
			Debug.DrawRay(transform.position, Vector3.up * 10f, Color.cyan, 10f);
			startFallingPosition = transform.position;
			if (transform.parent != null)
			{
				startFallingPosition = transform.parent.InverseTransformPoint(startFallingPosition);
			}
			Debug.DrawRay(transform.position, Vector3.up * 10f, Color.cyan, 10f);
			FallToGround(randomizePosition: false, justSpawned: true, startFallingPosition);
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
			radarIcon = UnityEngine.Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		else if (itemProperties.itemId == 14 && RoundManager.Instance.mapPropsContainer != null)
		{
			radarIcon = UnityEngine.Object.Instantiate(StartOfRound.Instance.keyRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		if (!itemProperties.isScrap)
		{
			HoarderBugAI.grabbableObjectsInMap.Add(gameObject);
		}
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int k = 0; k < componentsInChildren.Length; k++)
		{
			componentsInChildren[k].renderingLayerMask = 1u;
		}
		SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int l = 0; l < componentsInChildren2.Length; l++)
		{
			componentsInChildren2[l].renderingLayerMask = 1u;
		}
	}

	private IEnumerator fallToGroundOnFrameDelay(Vector3 startPosition)
	{
		yield return null;
		if (!(startFallingPosition != startPosition))
		{
			FallToGround(randomizePosition: false, justSpawned: false, startPosition);
		}
	}

	public void FallToGround(bool randomizePosition = false, bool justSpawned = false, Vector3 overrideStartPos = default(Vector3))
	{
		Vector3 startPosition = transform.position;
		if (overrideStartPos != Vector3.zero)
		{
			startPosition = overrideStartPos;
		}
		if (justSpawned)
		{
			StartCoroutine(fallToGroundOnFrameDelay(startPosition));
			return;
		}
		fallTime = 0f;
		transform.localPosition = startFallingPosition;
		startPosition = transform.position;
		if (Physics.Raycast(startPosition, Vector3.down, out var hitInfo, 80f, 268437760, QueryTriggerInteraction.Ignore))
		{
			targetFloorPosition = hitInfo.point + itemProperties.verticalOffset * Vector3.up;
			if (transform.parent != null)
			{
				targetFloorPosition = transform.parent.InverseTransformPoint(targetFloorPosition);
			}
		}
		else
		{
			targetFloorPosition = transform.localPosition;
		}
		if (randomizePosition)
		{
			targetFloorPosition += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
		}
		InitializeAfterPositioning();
	}

	public void EnablePhysics(bool enable)
	{
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet") && !propColliders[i].gameObject.CompareTag("Enemy"))
			{
				propColliders[i].enabled = enable;
			}
		}
	}

	public virtual void InspectItem()
	{
		if (IsOwner && playerHeldBy != null && itemProperties.canBeInspected)
		{
			playerHeldBy.IsInspectingItem = !playerHeldBy.IsInspectingItem;
			HUDManager.Instance.SetNearDepthOfFieldEnabled(!playerHeldBy.IsInspectingItem);
		}
	}

	public virtual void InteractItem()
	{
	}

	public void GrabItemOnClient()
	{
		if (!IsOwner)
		{
			Debug.LogError("GrabItemOnClient was called but player was not the owner.");
			return;
		}
		SetControlTipsForItem();
		GrabItem();
		if (itemProperties.syncGrabFunction)
		{
			GrabServerRpc();
		}
	}

	public virtual void SetControlTipsForItem()
	{
		HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
	}

	public virtual void GrabItem()
	{
	}

	public void UseItemOnClient(bool buttonDown = true)
	{
		if (!IsOwner)
		{
			Debug.Log("Can't use item; not owner");
		}
		else if (!RequireCooldown() && UseItemBatteries(!itemProperties.holdButtonUse, buttonDown))
		{
			if (itemProperties.syncUseFunction)
			{
				ActivateItemRpc(isBeingUsed, buttonDown);
			}
			ItemActivate(isBeingUsed, buttonDown);
		}
	}

	public bool UseItemBatteries(bool isToggle, bool buttonDown = true)
	{
		if (itemProperties.requiresBattery && (insertedBattery == null || insertedBattery.empty))
		{
			return false;
		}
		if (itemProperties.itemIsTrigger)
		{
			insertedBattery.charge = Mathf.Clamp(insertedBattery.charge - itemProperties.batteryUsage, 0f, 1f);
			if (insertedBattery.charge <= 0f)
			{
				insertedBattery.empty = true;
			}
			isBeingUsed = false;
		}
		else if (itemProperties.automaticallySetUsingPower)
		{
			if (isToggle)
			{
				isBeingUsed = !isBeingUsed;
			}
			else
			{
				isBeingUsed = buttonDown;
			}
		}
		return true;
	}

	public virtual void ItemActivate(bool used, bool buttonDown = true)
	{
	}

	public void ItemInteractLeftRightOnClient(bool right)
	{
		if (!IsOwner)
		{
			Debug.Log("InteractLeftRight was called but player was not the owner.");
		}
		else if (!RequireCooldown() && UseItemBatteries(isToggle: true))
		{
			ItemInteractLeftRight(right);
			if (itemProperties.syncInteractLRFunction)
			{
				InteractLeftRightServerRpc(right);
			}
		}
	}

	public virtual void ItemInteractLeftRight(bool right)
	{
	}

	public virtual void ActivatePhysicsTrigger(Collider other)
	{
	}

	public virtual void UseUpBatteries()
	{
		Debug.Log("Use up batteries on local client");
		isBeingUsed = false;
	}

	public virtual void GrabItemFromEnemy(EnemyAI enemy)
	{
	}

	public virtual void DiscardItemFromEnemy()
	{
	}

	public virtual void ChargeBatteries()
	{
	}

	public virtual void DestroyObjectInHand(PlayerControllerB playerHolding)
	{
		grabbable = false;
		grabbableToEnemies = false;
		deactivated = true;
		if (playerHolding != null)
		{
			playerHolding.activatingItem = false;
		}
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Collider[] componentsInChildren2 = gameObject.GetComponentsInChildren<Collider>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[j]);
		}
		if (IsOwner && isHeld && !isPocketed && playerHolding != null && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			playerHeldBy.DiscardHeldObject();
		}
	}

	public virtual void EquipItem()
	{
		if (IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			SetControlTipsForItem();
		}
		rotateObject = false;
		EnableItemMeshes(enable: true);
		isPocketed = false;
		if (!hasBeenHeld)
		{
			hasBeenHeld = true;
			if (!isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
			{
				RoundManager.Instance.valueOfFoundScrapItems += scrapValue;
			}
		}
	}

	public virtual void PocketItem()
	{
		if (IsOwner && playerHeldBy != null)
		{
			playerHeldBy.IsInspectingItem = false;
		}
		isPocketed = true;
		EnableItemMeshes(enable: false);
		gameObject.GetComponent<AudioSource>().PlayOneShot(itemProperties.pocketSFX, 1f);
	}

	public void DiscardItemOnClient()
	{
		if (IsOwner)
		{
			DiscardItem();
			HUDManager.Instance.ClearControlTips();
			SyncBatteryServerRpc((int)(insertedBattery.charge * 100f));
			DiscardItemRpc();
		}
	}

	[ServerRpc]
	public void SyncBatteryServerRpc(int charge)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3484508350u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, charge);
			__endSendServerRpc(ref bufferWriter, 3484508350u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SyncBatteryClientRpc(charge);
		}
	}

	[ClientRpc]
	public void SyncBatteryClientRpc(int charge)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2670202430u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, charge);
				__endSendClientRpc(ref bufferWriter, 2670202430u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				float num = (float)charge / 100f;
				insertedBattery = new Battery(num <= 0f, num);
				ChargeBatteries();
			}
		}
	}

	public virtual void DiscardItem()
	{
		if (IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			if (playerHeldBy != null)
			{
				playerHeldBy.IsInspectingItem = false;
				playerHeldBy.activatingItem = false;
			}
		}
		if (itemProperties.isScrap && !scrapPersistedThroughRounds && !StartOfRound.Instance.inShipPhase)
		{
			if (playerHeldBy != null)
			{
				if (playerHeldBy.isInHangarShipRoom)
				{
					RoundManager.Instance.CollectNewScrapForThisRound(this);
				}
			}
			else if (transform.position.y > -100f && StartOfRound.Instance.shipInnerRoomBounds.bounds.ClosestPoint(transform.position) == transform.position)
			{
				RoundManager.Instance.CollectNewScrapForThisRound(this);
			}
		}
		playerHeldBy = null;
	}

	public virtual void LateUpdate()
	{
		if (parentObject != null)
		{
			transform.rotation = parentObject.rotation;
			transform.Rotate(itemProperties.rotationOffset);
			transform.position = parentObject.position;
			Vector3 positionOffset = itemProperties.positionOffset;
			positionOffset = parentObject.rotation * positionOffset;
			transform.position += positionOffset;
		}
		if (rotateObject)
		{
			transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
		}
		if (radarIcon != null)
		{
			radarIcon.position = transform.position;
		}
	}

	public virtual void FallWithCurve()
	{
		float num = startFallingPosition.y - targetFloorPosition.y;
		if (floorYRot == -1)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, transform.eulerAngles.y, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		else
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		if (num > 5f)
		{
			transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurveNoBounce.Evaluate(fallTime));
		}
		else
		{
			transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurve.Evaluate(fallTime));
		}
		fallTime += Mathf.Abs(Time.deltaTime * 6f / num);
	}

	public virtual void OnPlaceObject()
	{
	}

	public virtual void OnBroughtToShip()
	{
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
	}

	public virtual void Update()
	{
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
			transform.localPosition = targetFloorPosition;
		}
		else
		{
			if (!isHeld && !isHeldByEnemy)
			{
				return;
			}
			reachedFloorTarget = false;
			if (playerHeldBy != null && !isPocketed && playerHeldBy.scrapJiggleAudioDelay > -4f)
			{
				if (playerHeldBy.scrapJiggleAudioDelay <= 0f)
				{
					playerHeldBy.scrapJiggleAudioDelay = -5f;
					JiggleItemEffect(UnityEngine.Random.Range(0.18f, 0.36f));
				}
				else
				{
					playerHeldBy.scrapJiggleAudioDelay -= Time.deltaTime;
				}
			}
		}
	}

	public virtual void OnHitGround()
	{
	}

	public virtual void JiggleItemEffect(float audioVolume)
	{
		if (itemProperties.clinkAudios != null && itemProperties.clinkAudios.Length != 0)
		{
			RoundManager.PlayRandomClip(gameObject.GetComponent<AudioSource>(), itemProperties.clinkAudios, randomize: true, audioVolume, -1);
		}
	}

	public virtual void PlayDropSFX()
	{
		if (itemProperties.dropSFX != null)
		{
			AudioSource component = gameObject.GetComponent<AudioSource>();
			component.PlayOneShot(itemProperties.dropSFX);
			WalkieTalkie.TransmitOneShotAudio(component, itemProperties.dropSFX);
			if (IsOwner)
			{
				RoundManager.Instance.PlayAudibleNoise(transform.position, 8f, 0.5f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
			}
		}
		hasHitGround = true;
	}

	public void SetScrapValue(int setValueTo)
	{
		scrapValue = setValueTo;
		ScanNodeProperties componentInChildren = gameObject.GetComponentInChildren<ScanNodeProperties>();
		if (componentInChildren == null)
		{
			Debug.LogError("Scan node is missing for item!: " + gameObject.name);
			return;
		}
		componentInChildren.subText = $"Value: ${setValueTo}";
		componentInChildren.scrapValue = setValueTo;
	}

	public bool RequireCooldown()
	{
		if (useCooldown > 0f)
		{
			if (itemProperties.holdButtonUse && isBeingUsed)
			{
				return false;
			}
			if (currentUseCooldown <= 0f)
			{
				currentUseCooldown = useCooldown;
				return false;
			}
			return true;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	private void InteractLeftRightServerRpc(bool right)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1469591241u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in right, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 1469591241u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				InteractLeftRightClientRpc(right);
			}
		}
	}

	[ClientRpc]
	private void InteractLeftRightClientRpc(bool right)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3081511085u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in right, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3081511085u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				ItemInteractLeftRight(right);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void GrabServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2618697776u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2618697776u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				GrabClientRpc();
			}
		}
	}

	[ClientRpc]
	private void GrabClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1334815929u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1334815929u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				GrabItem();
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	private void ActivateItemRpc(bool onOff, bool buttonDown)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				RequireOwnership = false
			};
			RpcParams rpcParams = default;
			FastBufferWriter bufferWriter = __beginSendRpc(319375719u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in onOff, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in buttonDown, default(FastBufferWriter.ForPrimitives));
			__endSendRpc(ref bufferWriter, 319375719u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				Debug.Log($"Is being used set to {onOff} by RPC");
				isBeingUsed = onOff;
				ItemActivate(onOff, buttonDown);
			}
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	private void DiscardItemRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				RequireOwnership = false
			};
			RpcParams rpcParams = default;
			FastBufferWriter bufferWriter = __beginSendRpc(2250513698u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 2250513698u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				DiscardItem();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void UseUpItemBatteriesServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2025123357u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2025123357u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				UseUpItemBatteriesClientRpc();
			}
		}
	}

	[ClientRpc]
	private void UseUpItemBatteriesClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(738171084u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 738171084u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				UseUpBatteries();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void EquipItemServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(947748389u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 947748389u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				EquipItemClientRpc();
			}
		}
	}

	[ClientRpc]
	private void EquipItemClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1898191537u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1898191537u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				EquipItem();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void PocketItemServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(101807903u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 101807903u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PocketItemClientRpc();
			}
		}
	}

	[ClientRpc]
	private void PocketItemClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3399384424u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 3399384424u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				PocketItem();
			}
		}
	}

	public void ChangeOwnershipOfProp(ulong clientId)
	{
		ChangeOwnershipOfPropServerRpc(clientId);
	}

	[ServerRpc(RequireOwnership = false)]
	private void ChangeOwnershipOfPropServerRpc(ulong NewOwner)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(1391130874u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, NewOwner);
			__endSendServerRpc(ref bufferWriter, 1391130874u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		try
		{
			gameObject.GetComponent<NetworkRigidbodyModifiable>().kinematicOnOwner = true;
			transform.SetParent(playerHeldBy.localItemHolder, worldPositionStays: true);
			gameObject.GetComponent<ClientNetworkTransform>().InLocalSpace = true;
			transform.localPosition = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			playerHeldBy.grabSetParentServer = false;
			gameObject.GetComponent<NetworkObject>().ChangeOwnership(NewOwner);
		}
		catch (Exception arg)
		{
			Debug.Log($"Failed to transfer ownership of prop to client: {arg}");
		}
	}

	public virtual void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger"))
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

	public Vector3 GetItemFloorPosition(Vector3 startPosition = default(Vector3))
	{
		if (startPosition == Vector3.zero)
		{
			startPosition = transform.position + Vector3.up * 0.15f;
		}
		Debug.DrawRay(startPosition, -Vector3.up * 10f, Color.yellow, 5f);
		if (Physics.Raycast(startPosition, -Vector3.up, out var hitInfo, 80f, 268437761, QueryTriggerInteraction.Ignore))
		{
			Debug.DrawRay(hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up, Vector3.up * 0.25f, Color.cyan, 5f);
			return hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up;
		}
		Debug.DrawRay(startPosition, Vector3.up * 0.25f, Color.white, 5f);
		return startPosition;
	}

	public NetworkObject GetPhysicsRegionOfDroppedObject(PlayerControllerB playerDropping, out Vector3 hitPoint, bool disallowDroppingAhead = false)
	{
		Transform transform = null;
		RaycastHit hitInfo;
		if (playerDropping != null && itemProperties.allowDroppingAheadOfPlayer && !disallowDroppingAhead)
		{
			Debug.DrawRay(playerDropping.transform.position + Vector3.up * 0.4f, playerDropping.gameplayCamera.transform.forward * 1.7f, Color.yellow, 1f);
			Ray ray = new Ray(playerDropping.transform.position + Vector3.up * 0.4f, playerDropping.gameplayCamera.transform.forward);
			Vector3 vector = ((!Physics.Raycast(ray, out hitInfo, 1.7f, 1342179585, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hitInfo.distance - 0.3f, 0.01f, 2f)));
			if (Physics.Raycast(vector, -Vector3.up, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(vector, -Vector3.up * 80f, Color.yellow, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		else
		{
			Ray ray = new Ray(base.transform.position, -Vector3.up);
			if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(base.transform.position, -Vector3.up * 80f, Color.blue, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		if (transform != null)
		{
			PlayerPhysicsRegion componentInChildren = transform.GetComponentInChildren<PlayerPhysicsRegion>();
			if (componentInChildren != null && componentInChildren.allowDroppingItems && componentInChildren.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
			{
				NetworkObject parentNetworkObject = componentInChildren.parentNetworkObject;
				if (parentNetworkObject != null)
				{
					Vector3 addPositionOffsetToItems = componentInChildren.addPositionOffsetToItems;
					hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
					return parentNetworkObject;
				}
				Debug.LogError("Error: physics region transform does not have network object?: " + transform.gameObject.name);
			}
		}
		hitPoint = Vector3.zero;
		return null;
	}

	public NetworkObject GetPhysicsRegionOfDroppedObjectSynced(PlayerControllerB playerDropping, out Vector3 hitPoint, bool disallowDroppingAhead, Vector3 playerPosition, Vector3 dropPosition, Vector3 playerCamPosition, Vector3 playerCamRotation)
	{
		Transform transform = null;
		RaycastHit hitInfo;
		if (playerDropping != null && itemProperties.allowDroppingAheadOfPlayer && !disallowDroppingAhead)
		{
			RoundManager.Instance.tempTransform.position = playerCamPosition;
			RoundManager.Instance.tempTransform.eulerAngles = playerCamRotation;
			Debug.DrawRay(playerPosition + Vector3.up * 0.4f, RoundManager.Instance.tempTransform.forward * 1.7f, Color.yellow, 1f);
			Ray ray = new Ray(playerPosition + Vector3.up * 0.4f, RoundManager.Instance.tempTransform.forward);
			Vector3 vector = ((!Physics.Raycast(ray, out hitInfo, 1.7f, 1342179585, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hitInfo.distance - 0.3f, 0.01f, 2f)));
			if (Physics.Raycast(vector, -Vector3.up, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(vector, -Vector3.up * 80f, Color.yellow, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		else
		{
			Ray ray = new Ray(dropPosition, -Vector3.up);
			if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(dropPosition, -Vector3.up * 80f, Color.blue, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		if (transform != null)
		{
			PlayerPhysicsRegion componentInChildren = transform.GetComponentInChildren<PlayerPhysicsRegion>();
			if (componentInChildren != null && componentInChildren.allowDroppingItems && componentInChildren.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
			{
				NetworkObject parentNetworkObject = componentInChildren.parentNetworkObject;
				if (parentNetworkObject != null)
				{
					Vector3 addPositionOffsetToItems = componentInChildren.addPositionOffsetToItems;
					hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
					return parentNetworkObject;
				}
				Debug.LogError("Error: physics region transform does not have network object?: " + transform.gameObject.name);
			}
		}
		hitPoint = Vector3.zero;
		return null;
	}

	public virtual void ReactToSellingItemOnCounter()
	{
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3484508350u, __rpc_handler_3484508350, "SyncBatteryServerRpc");
		__registerRpc(2670202430u, __rpc_handler_2670202430, "SyncBatteryClientRpc");
		__registerRpc(1469591241u, __rpc_handler_1469591241, "InteractLeftRightServerRpc");
		__registerRpc(3081511085u, __rpc_handler_3081511085, "InteractLeftRightClientRpc");
		__registerRpc(2618697776u, __rpc_handler_2618697776, "GrabServerRpc");
		__registerRpc(1334815929u, __rpc_handler_1334815929, "GrabClientRpc");
		__registerRpc(319375719u, __rpc_handler_319375719, "ActivateItemRpc");
		__registerRpc(2250513698u, __rpc_handler_2250513698, "DiscardItemRpc");
		__registerRpc(2025123357u, __rpc_handler_2025123357, "UseUpItemBatteriesServerRpc");
		__registerRpc(738171084u, __rpc_handler_738171084, "UseUpItemBatteriesClientRpc");
		__registerRpc(947748389u, __rpc_handler_947748389, "EquipItemServerRpc");
		__registerRpc(1898191537u, __rpc_handler_1898191537, "EquipItemClientRpc");
		__registerRpc(101807903u, __rpc_handler_101807903, "PocketItemServerRpc");
		__registerRpc(3399384424u, __rpc_handler_3399384424, "PocketItemClientRpc");
		__registerRpc(1391130874u, __rpc_handler_1391130874, "ChangeOwnershipOfPropServerRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3484508350(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).SyncBatteryServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2670202430(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).SyncBatteryClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1469591241(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).InteractLeftRightServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3081511085(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).InteractLeftRightClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2618697776(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).GrabServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1334815929(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).GrabClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_319375719(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).ActivateItemRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2250513698(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).DiscardItemRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2025123357(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).UseUpItemBatteriesServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_738171084(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).UseUpItemBatteriesClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_947748389(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).EquipItemServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1898191537(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).EquipItemClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_101807903(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).PocketItemServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3399384424(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).PocketItemClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1391130874(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out ulong value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GrabbableObject)target).ChangeOwnershipOfPropServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "GrabbableObject";
	}
}
