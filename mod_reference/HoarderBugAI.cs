using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HoarderBugAI : EnemyAI
{
	public AISearchRoutine searchForItems;

	public AISearchRoutine searchForPlayer;

	[Header("Tracking/Memory")]
	[Space(3f)]
	public Vector3 nestPosition;

	private bool choseNestPosition;

	[Space(3f)]
	public static List<HoarderBugItem> HoarderBugItems = new List<HoarderBugItem>();

	public static List<GameObject> grabbableObjectsInMap = new List<GameObject>();

	public float angryTimer;

	public GrabbableObject targetItem;

	public HoarderBugItem heldItem;

	[Header("Animations")]
	[Space(5f)]
	private Vector3 agentLocalVelocity;

	private Vector3 previousPosition;

	private float velX;

	private float velZ;

	public Transform turnCompass;

	private float armsHoldLayerWeight;

	[Space(5f)]
	public Transform animationContainer;

	public Transform grabTarget;

	public MultiAimConstraint headLookRig;

	public Transform headLookTarget;

	[Header("Special behaviour states")]
	private float annoyanceMeter;

	public bool watchingPlayerNearPosition;

	public PlayerControllerB watchingPlayer;

	public Transform lookTarget;

	public bool lookingAtPositionOfInterest;

	private Vector3 positionOfInterest;

	private bool isAngry;

	[Header("Misc logic")]
	private bool sendingGrabOrDropRPC;

	private float waitingAtNestTimer;

	private bool waitingAtNest;

	private float timeSinceSeeingAPlayer;

	[Header("Chase logic")]
	private bool lostPlayerInChase;

	private float noticePlayerTimer;

	public PlayerControllerB angryAtPlayer;

	private bool inChase;

	[Header("Audios")]
	public AudioClip[] chitterSFX;

	[Header("Audios")]
	public AudioClip[] angryScreechSFX;

	public AudioClip angryVoiceSFX;

	public AudioClip bugFlySFX;

	public AudioClip hitPlayerSFX;

	private float timeSinceHittingPlayer;

	private float timeSinceLookingTowardsNoise;

	private float detectPlayersInterval;

	private bool inReturnToNestMode;

	public GameObject ghostCostume;

	public override void Start()
	{
		base.Start();
		heldItem = null;
		RefreshGrabbableObjectsInMapList();
		DateTime dateTime = new DateTime(DateTime.Now.Year, 10, 31);
		if (DateTime.Today == dateTime)
		{
			ghostCostume.SetActive(value: true);
		}
	}

	public static void RefreshGrabbableObjectsInMapList()
	{
		grabbableObjectsInMap.Clear();
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].grabbableToEnemies && !array[i].deactivated)
			{
				grabbableObjectsInMap.Add(array[i].gameObject);
			}
		}
	}

	private bool GrabTargetItemIfClose()
	{
		if (targetItem != null && heldItem == null && Vector3.Distance(transform.position, targetItem.transform.position) < 0.75f)
		{
			if (!SetDestinationToPosition(nestPosition, checkForPath: true))
			{
				nestPosition = ChooseClosestNodeToPosition(transform.position).position;
				SetDestinationToPosition(nestPosition);
			}
			NetworkObject component = targetItem.GetComponent<NetworkObject>();
			SwitchToBehaviourStateOnLocalClient(1);
			GrabItem(component);
			sendingGrabOrDropRPC = true;
			GrabItemServerRpc(component);
			return true;
		}
		return false;
	}

	private void ChooseNestPosition()
	{
		HoarderBugAI[] array = UnityEngine.Object.FindObjectsOfType<HoarderBugAI>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != this && !PathIsIntersectedByLineOfSight(array[i].nestPosition, calculatePathDistance: false, avoidLineOfSight: false))
			{
				nestPosition = array[i].nestPosition;
				SyncNestPositionServerRpc(nestPosition);
				return;
			}
		}
		nestPosition = ChooseClosestNodeToPosition(transform.position).position;
		SyncNestPositionServerRpc(nestPosition);
	}

	[ServerRpc]
	private void SyncNestPositionServerRpc(Vector3 newNestPosition)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3689917697u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in newNestPosition);
			__endSendServerRpc(ref bufferWriter, 3689917697u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SyncNestPositionClientRpc(newNestPosition);
		}
	}

	[ClientRpc]
	private void SyncNestPositionClientRpc(Vector3 newNestPosition)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1841413947u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in newNestPosition);
				__endSendClientRpc(ref bufferWriter, 1841413947u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				nestPosition = newNestPosition;
			}
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		if (!choseNestPosition)
		{
			choseNestPosition = true;
			ChooseNestPosition();
			return;
		}
		if (CheckLineOfSightForPosition(nestPosition, 60f, 40, 0.5f))
		{
			for (int i = 0; i < HoarderBugItems.Count; i++)
			{
				if (HoarderBugItems[i].itemGrabbableObject.isHeld && HoarderBugItems[i].itemNestPosition == nestPosition)
				{
					HoarderBugItems[i].status = HoarderBugItemStatus.Stolen;
				}
			}
		}
		HoarderBugItem hoarderBugItem = CheckLineOfSightForItem(HoarderBugItemStatus.Stolen, 60f, 30, 3f);
		if (hoarderBugItem != null && !hoarderBugItem.itemGrabbableObject.isHeld)
		{
			hoarderBugItem.status = HoarderBugItemStatus.Returned;
			if (!grabbableObjectsInMap.Contains(hoarderBugItem.itemGrabbableObject.gameObject))
			{
				grabbableObjectsInMap.Add(hoarderBugItem.itemGrabbableObject.gameObject);
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			inReturnToNestMode = false;
			ExitChaseMode();
			if (GrabTargetItemIfClose())
			{
				break;
			}
			if (targetItem == null && !searchForItems.inProgress)
			{
				StartSearch(nestPosition, searchForItems);
				break;
			}
			if (targetItem != null)
			{
				SetGoTowardsTargetObject(targetItem.gameObject);
				break;
			}
			GameObject gameObject2 = CheckLineOfSight(grabbableObjectsInMap, 60f, 40, 5f);
			if ((bool)gameObject2)
			{
				GrabbableObject component = gameObject2.GetComponent<GrabbableObject>();
				if ((bool)component && !component.deactivated && (!component.isHeld || (UnityEngine.Random.Range(0, 100) < 4 && !component.isPocketed)))
				{
					SetGoTowardsTargetObject(gameObject2);
				}
			}
			break;
		}
		case 1:
			ExitChaseMode();
			if (!inReturnToNestMode)
			{
				inReturnToNestMode = true;
				SetReturningToNest();
			}
			GrabTargetItemIfClose();
			if (waitingAtNest)
			{
				if (heldItem != null)
				{
					DropItemAndCallDropRPC(heldItem.itemGrabbableObject.GetComponent<NetworkObject>());
				}
				else
				{
					GameObject gameObject = CheckLineOfSight(grabbableObjectsInMap, 60f, 40, 5f);
					if ((bool)gameObject && Vector3.Distance(eye.position, gameObject.transform.position) < 6f)
					{
						targetItem = gameObject.GetComponent<GrabbableObject>();
						if (targetItem != null && !targetItem.isHeld && !targetItem.deactivated)
						{
							waitingAtNest = false;
							SwitchToBehaviourState(0);
							break;
						}
					}
				}
				if (waitingAtNestTimer <= 0f && !watchingPlayerNearPosition)
				{
					waitingAtNest = false;
					SwitchToBehaviourStateOnLocalClient(0);
				}
			}
			else if (Vector3.Distance(transform.position, nestPosition) < 0.75f)
			{
				waitingAtNest = true;
				waitingAtNestTimer = 15f;
			}
			break;
		case 2:
			inReturnToNestMode = false;
			if (heldItem != null)
			{
				DropItemAndCallDropRPC(heldItem.itemGrabbableObject.GetComponent<NetworkObject>(), droppedInNest: false);
			}
			if (lostPlayerInChase)
			{
				if (!searchForPlayer.inProgress)
				{
					searchForPlayer.searchWidth = 30f;
					StartSearch(targetPlayer.transform.position, searchForPlayer);
					Debug.Log(base.gameObject.name + ": Lost player in chase; beginning search where the player was last seen");
				}
				break;
			}
			if (targetPlayer == null)
			{
				Debug.LogError("TargetPlayer is null even though bug is in chase; setting targetPlayer to watchingPlayer");
				if (watchingPlayer != null)
				{
					targetPlayer = watchingPlayer;
				}
			}
			if (searchForPlayer.inProgress)
			{
				StopSearch(searchForPlayer);
				Debug.Log(base.gameObject.name + ": Found player during chase; stopping search coroutine and moving after target player");
			}
			movingTowardsTargetPlayer = true;
			break;
		case 3:
			break;
		}
	}

	private void SetGoTowardsTargetObject(GameObject foundObject)
	{
		if (SetDestinationToPosition(foundObject.transform.position, checkForPath: true) && grabbableObjectsInMap.Contains(foundObject))
		{
			Debug.Log(gameObject.name + ": Setting target object and going towards it.");
			targetItem = foundObject.GetComponent<GrabbableObject>();
			StopSearch(searchForItems, clear: false);
		}
		else
		{
			targetItem = null;
		}
	}

	private void ExitChaseMode()
	{
		if (inChase)
		{
			inChase = false;
			Debug.Log(gameObject.name + ": Exiting chase mode");
			if (searchForPlayer.inProgress)
			{
				StopSearch(searchForPlayer);
			}
			movingTowardsTargetPlayer = false;
			creatureAnimator.SetBool("Chase", value: false);
			creatureSFX.Stop();
		}
	}

	private void SetReturningToNest()
	{
		if (SetDestinationToPosition(nestPosition, checkForPath: true))
		{
			targetItem = null;
			StopSearch(searchForItems, clear: false);
		}
		else
		{
			Debug.Log(gameObject.name + ": Return to nest was called, but nest is not accessible! Abandoning and choosing a new nest position.");
			ChooseNestPosition();
		}
	}

	private void LateUpdate()
	{
		if (!inSpecialAnimation && !isEnemyDead && !StartOfRound.Instance.allPlayersDead)
		{
			if (detectPlayersInterval <= 0f)
			{
				detectPlayersInterval = 0.2f;
				DetectAndLookAtPlayers();
			}
			else
			{
				detectPlayersInterval -= Time.deltaTime;
			}
			AnimateLooking();
			CalculateAnimationDirection();
			SetArmLayerWeight();
		}
	}

	private void SetArmLayerWeight()
	{
		if (heldItem != null)
		{
			armsHoldLayerWeight = Mathf.Lerp(armsHoldLayerWeight, 0.85f, 8f * Time.deltaTime);
		}
		else
		{
			armsHoldLayerWeight = Mathf.Lerp(armsHoldLayerWeight, 0f, 8f * Time.deltaTime);
		}
		creatureAnimator.SetLayerWeight(1, armsHoldLayerWeight);
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = transform.position;
	}

	private void AnimateLooking()
	{
		if (watchingPlayer != null)
		{
			lookTarget.position = watchingPlayer.gameplayCamera.transform.position;
		}
		else
		{
			if (!lookingAtPositionOfInterest)
			{
				agent.angularSpeed = 220f;
				headLookRig.weight = Mathf.Lerp(headLookRig.weight, 0f, 10f);
				return;
			}
			lookTarget.position = positionOfInterest;
		}
		if (IsOwner)
		{
			agent.angularSpeed = 0f;
			turnCompass.LookAt(lookTarget);
			transform.rotation = Quaternion.Lerp(transform.rotation, turnCompass.rotation, 6f * Time.deltaTime);
			transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y, 0f);
		}
		float num = Vector3.Angle(transform.forward, lookTarget.position - transform.position);
		if (num > 22f)
		{
			headLookRig.weight = Mathf.Lerp(headLookRig.weight, 1f * (Mathf.Abs(num - 180f) / 180f), 7f);
		}
		else
		{
			headLookRig.weight = Mathf.Lerp(headLookRig.weight, 1f, 7f);
		}
		headLookTarget.position = Vector3.Lerp(headLookTarget.position, lookTarget.position, 8f * Time.deltaTime);
	}

	private void DetectAndLookAtPlayers()
	{
		Vector3 b = ((currentBehaviourStateIndex != 1) ? transform.position : nestPosition);
		PlayerControllerB[] allPlayersInLineOfSight = GetAllPlayersInLineOfSight(70f, 30, eye, 1.2f);
		if (allPlayersInLineOfSight != null)
		{
			PlayerControllerB playerControllerB = watchingPlayer;
			timeSinceSeeingAPlayer = 0f;
			float num = 500f;
			bool flag = false;
			if (stunnedByPlayer != null)
			{
				flag = true;
				angryAtPlayer = stunnedByPlayer;
			}
			for (int i = 0; i < allPlayersInLineOfSight.Length; i++)
			{
				if (!flag && allPlayersInLineOfSight[i].currentlyHeldObjectServer != null)
				{
					for (int j = 0; j < HoarderBugItems.Count; j++)
					{
						if (HoarderBugItems[j].itemGrabbableObject == allPlayersInLineOfSight[i].currentlyHeldObjectServer)
						{
							HoarderBugItems[j].status = HoarderBugItemStatus.Stolen;
							angryAtPlayer = allPlayersInLineOfSight[i];
							flag = true;
						}
					}
				}
				if (IsHoarderBugAngry() && allPlayersInLineOfSight[i] == angryAtPlayer)
				{
					watchingPlayer = angryAtPlayer;
				}
				else
				{
					float num2 = Vector3.Distance(allPlayersInLineOfSight[i].transform.position, b);
					if (num2 < num)
					{
						num = num2;
						watchingPlayer = allPlayersInLineOfSight[i];
					}
				}
				float num3 = Vector3.Distance(allPlayersInLineOfSight[i].transform.position, nestPosition);
				bool flag2 = HoarderBugItems.Count == 1 && HoarderBugItems[0].status != HoarderBugItemStatus.Stolen && Vector3.Distance(HoarderBugItems[0].itemGrabbableObject.transform.position, nestPosition) > 4f;
				if (HoarderBugItems.Count > 0 && !flag2)
				{
					if ((num3 < 4f || (inChase && num3 < 8f)) && angryTimer < 3.25f)
					{
						angryAtPlayer = allPlayersInLineOfSight[i];
						watchingPlayer = allPlayersInLineOfSight[i];
						angryTimer = 3.25f;
						break;
					}
					if (!isAngry && currentBehaviourStateIndex == 0 && num3 < 8f && (targetItem == null || Vector3.Distance(targetItem.transform.position, transform.position) > 7.5f) && IsOwner)
					{
						SwitchToBehaviourState(1);
					}
				}
				if (currentBehaviourStateIndex != 2 && Vector3.Distance(transform.position, allPlayersInLineOfSight[i].transform.position) < 2.5f)
				{
					annoyanceMeter += 0.2f;
					if (annoyanceMeter > 2.5f)
					{
						angryAtPlayer = allPlayersInLineOfSight[i];
						watchingPlayer = allPlayersInLineOfSight[i];
						angryTimer = 3.25f;
					}
				}
			}
			watchingPlayerNearPosition = num < 6f;
			if (watchingPlayer != playerControllerB)
			{
				RoundManager.PlayRandomClip(creatureVoice, chitterSFX);
			}
			if (!IsOwner)
			{
				return;
			}
			if (currentBehaviourStateIndex != 2)
			{
				if (IsHoarderBugAngry())
				{
					lostPlayerInChase = false;
					targetPlayer = watchingPlayer;
					SwitchToBehaviourState(2);
				}
			}
			else
			{
				targetPlayer = watchingPlayer;
				if (lostPlayerInChase)
				{
					lostPlayerInChase = false;
				}
			}
			return;
		}
		timeSinceSeeingAPlayer += 0.2f;
		watchingPlayerNearPosition = false;
		if (currentBehaviourStateIndex != 2)
		{
			if (timeSinceSeeingAPlayer > 1.5f)
			{
				watchingPlayer = null;
			}
			return;
		}
		if (timeSinceSeeingAPlayer > 1.25f)
		{
			watchingPlayer = null;
		}
		if (IsOwner)
		{
			if (timeSinceSeeingAPlayer > 15f)
			{
				SwitchToBehaviourState(1);
			}
			else if (timeSinceSeeingAPlayer > 2.5f)
			{
				lostPlayerInChase = true;
			}
		}
	}

	private bool IsHoarderBugAngry()
	{
		if (stunNormalizedTimer > 0f)
		{
			angryTimer = 4f;
			if ((bool)stunnedByPlayer)
			{
				angryAtPlayer = stunnedByPlayer;
			}
			return true;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < HoarderBugItems.Count; i++)
		{
			if (HoarderBugItems[i].status == HoarderBugItemStatus.Stolen)
			{
				num2++;
			}
			else if (HoarderBugItems[i].status == HoarderBugItemStatus.Returned)
			{
				num++;
			}
		}
		if (!(angryTimer > 0f))
		{
			return num2 > 0;
		}
		return true;
	}

	public override void Update()
	{
		base.Update();
		timeSinceHittingPlayer += Time.deltaTime;
		timeSinceLookingTowardsNoise += Time.deltaTime;
		if (timeSinceLookingTowardsNoise > 0.6f)
		{
			lookingAtPositionOfInterest = false;
		}
		if (inSpecialAnimation || isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		if (angryTimer >= 0f)
		{
			angryTimer -= Time.deltaTime;
		}
		creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
		bool flag = IsHoarderBugAngry();
		if (!isAngry & flag)
		{
			isAngry = true;
			creatureVoice.clip = angryVoiceSFX;
			creatureVoice.Play();
		}
		else if (isAngry && !flag)
		{
			isAngry = false;
			angryAtPlayer = null;
			creatureVoice.Stop();
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			ExitChaseMode();
			addPlayerVelocityToDestination = 0f;
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = 6f;
			}
			waitingAtNest = false;
			break;
		case 1:
			ExitChaseMode();
			addPlayerVelocityToDestination = 0f;
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = 6f;
			}
			agent.acceleration = 30f;
			if (waitingAtNest && waitingAtNestTimer > 0f)
			{
				waitingAtNestTimer -= Time.deltaTime;
			}
			break;
		case 2:
			if (!inChase)
			{
				inChase = true;
				creatureSFX.clip = bugFlySFX;
				creatureSFX.Play();
				RoundManager.PlayRandomClip(creatureVoice, angryScreechSFX);
				creatureAnimator.SetBool("Chase", value: true);
				waitingAtNest = false;
				if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 10f)
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
				}
			}
			addPlayerVelocityToDestination = 2f;
			if (!IsOwner)
			{
				break;
			}
			if (!IsHoarderBugAngry())
			{
				HoarderBugItem hoarderBugItem = CheckLineOfSightForItem(HoarderBugItemStatus.Returned, 60f, 12, 3f);
				if (hoarderBugItem != null && !hoarderBugItem.itemGrabbableObject.isHeld)
				{
					SwitchToBehaviourState(0);
					SetGoTowardsTargetObject(hoarderBugItem.itemGrabbableObject.gameObject);
				}
				else
				{
					SwitchToBehaviourState(1);
				}
				ExitChaseMode();
				break;
			}
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = 18f;
			}
			agent.acceleration = 16f;
			if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.75f, 60f, 15))
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.4f);
			}
			break;
		}
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (timesPlayedInOneSpot <= 10 && !(timeSinceLookingTowardsNoise < 0.6f))
		{
			timeSinceLookingTowardsNoise = 0f;
			float num = Vector3.Distance(noisePosition, nestPosition);
			if (IsOwner && HoarderBugItems.Count > 0 && !isAngry && currentBehaviourStateIndex == 0 && num < 15f && (targetItem == null || Vector3.Distance(targetItem.transform.position, transform.position) > 4.5f))
			{
				SwitchToBehaviourState(1);
			}
			positionOfInterest = noisePosition;
			lookingAtPositionOfInterest = true;
		}
	}

	private void DropItemAndCallDropRPC(NetworkObject dropItemNetworkObject, bool droppedInNest = true)
	{
		Vector3 targetFloorPosition = RoundManager.Instance.RandomlyOffsetPosition(heldItem.itemGrabbableObject.GetItemFloorPosition(), 1.2f, 0.4f);
		DropItem(dropItemNetworkObject, targetFloorPosition);
		sendingGrabOrDropRPC = true;
		DropItemServerRpc(dropItemNetworkObject, targetFloorPosition, droppedInNest);
	}

	[ServerRpc]
	public void DropItemServerRpc(NetworkObjectReference objectRef, Vector3 targetFloorPosition, bool droppedInNest)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3510928244u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in objectRef, default(FastBufferWriter.ForNetworkSerializable));
			bufferWriter.WriteValueSafe(in targetFloorPosition);
			bufferWriter.WriteValueSafe(in droppedInNest, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3510928244u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			DropItemClientRpc(objectRef, targetFloorPosition, droppedInNest);
		}
	}

	[ClientRpc]
	public void DropItemClientRpc(NetworkObjectReference objectRef, Vector3 targetFloorPosition, bool droppedInNest)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(847487221u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in objectRef, default(FastBufferWriter.ForNetworkSerializable));
			bufferWriter.WriteValueSafe(in targetFloorPosition);
			bufferWriter.WriteValueSafe(in droppedInNest, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 847487221u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (objectRef.TryGet(out var networkObject))
			{
				DropItem(networkObject, targetFloorPosition, droppedInNest);
			}
			else
			{
				Debug.LogError(gameObject.name + ": Failed to get network object from network object reference (Drop item RPC)");
			}
		}
	}

	[ServerRpc]
	public void GrabItemServerRpc(NetworkObjectReference objectRef)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2358561451u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in objectRef, default(FastBufferWriter.ForNetworkSerializable));
			__endSendServerRpc(ref bufferWriter, 2358561451u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			GrabItemClientRpc(objectRef);
		}
	}

	[ClientRpc]
	public void GrabItemClientRpc(NetworkObjectReference objectRef)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1536760829u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in objectRef, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 1536760829u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SwitchToBehaviourStateOnLocalClient(1);
			if (objectRef.TryGet(out var networkObject))
			{
				GrabItem(networkObject);
			}
			else
			{
				Debug.LogError(gameObject.name + ": Failed to get network object from network object reference (Grab item RPC)");
			}
		}
	}

	private void DropItem(NetworkObject item, Vector3 targetFloorPosition, bool droppingInNest = true)
	{
		if (sendingGrabOrDropRPC)
		{
			sendingGrabOrDropRPC = false;
			return;
		}
		if (heldItem == null)
		{
			Debug.LogError("Hoarder bug: my held item is null when attempting to drop it!!");
			return;
		}
		GrabbableObject itemGrabbableObject = heldItem.itemGrabbableObject;
		itemGrabbableObject.parentObject = null;
		itemGrabbableObject.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
		itemGrabbableObject.EnablePhysics(enable: true);
		itemGrabbableObject.fallTime = 0f;
		itemGrabbableObject.startFallingPosition = itemGrabbableObject.transform.parent.InverseTransformPoint(itemGrabbableObject.transform.position);
		itemGrabbableObject.targetFloorPosition = itemGrabbableObject.transform.parent.InverseTransformPoint(targetFloorPosition);
		itemGrabbableObject.floorYRot = -1;
		itemGrabbableObject.DiscardItemFromEnemy();
		heldItem = null;
		if (!droppingInNest)
		{
			grabbableObjectsInMap.Add(itemGrabbableObject.gameObject);
		}
	}

	private void GrabItem(NetworkObject item)
	{
		if (sendingGrabOrDropRPC)
		{
			sendingGrabOrDropRPC = false;
			return;
		}
		if (heldItem != null)
		{
			Debug.Log(gameObject.name + ": Trying to grab another item (" + item.gameObject.name + ") while hands are already full with item (" + heldItem.itemGrabbableObject.gameObject.name + "). Dropping the currently held one.");
			DropItem(heldItem.itemGrabbableObject.GetComponent<NetworkObject>(), heldItem.itemGrabbableObject.GetItemFloorPosition());
		}
		targetItem = null;
		GrabbableObject component = item.gameObject.GetComponent<GrabbableObject>();
		HoarderBugItems.Add(new HoarderBugItem(component, HoarderBugItemStatus.Owned, nestPosition));
		heldItem = HoarderBugItems[HoarderBugItems.Count - 1];
		component.parentObject = grabTarget;
		component.hasHitGround = false;
		component.GrabItemFromEnemy(this);
		component.EnablePhysics(enable: false);
		grabbableObjectsInMap.Remove(component.gameObject);
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (inChase && !(timeSinceHittingPlayer < 0.5f))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				timeSinceHittingPlayer = 0f;
				playerControllerB.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
				HitPlayerServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void HitPlayerServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1884379629u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 1884379629u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				HitPlayerClientRpc();
			}
		}
	}

	[ClientRpc]
	public void HitPlayerClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1031891902u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1031891902u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!isEnemyDead)
			{
				creatureAnimator.SetTrigger("HitPlayer");
				creatureSFX.PlayOneShot(hitPlayerSFX);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, hitPlayerSFX);
			}
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		Debug.Log("HA");
		if (!isEnemyDead)
		{
			Debug.Log("HB");
			creatureAnimator.SetTrigger("damage");
			angryAtPlayer = playerWhoHit;
			angryTimer += 18f;
			Debug.Log("HC");
			enemyHP -= force;
			if (enemyHP <= 0 && IsOwner)
			{
				KillEnemyOnOwnerClient();
			}
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy();
		agent.speed = 0f;
		creatureVoice.Stop();
		creatureSFX.Stop();
		if (heldItem != null)
		{
			DropItemAndCallDropRPC(heldItem.itemGrabbableObject.GetComponent<NetworkObject>(), droppedInNest: false);
		}
	}

	public HoarderBugItem CheckLineOfSightForItem(HoarderBugItemStatus searchForItemsOfStatus = HoarderBugItemStatus.Any, float width = 45f, int range = 60, float proximityAwareness = -1f)
	{
		for (int i = 0; i < HoarderBugItems.Count; i++)
		{
			if (!HoarderBugItems[i].itemGrabbableObject.grabbableToEnemies || HoarderBugItems[i].itemGrabbableObject.isHeld || (searchForItemsOfStatus != HoarderBugItemStatus.Any && HoarderBugItems[i].status != searchForItemsOfStatus))
			{
				continue;
			}
			Vector3 position = HoarderBugItems[i].itemGrabbableObject.transform.position;
			if (!Physics.Linecast(eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
			{
				Vector3 to = position - eye.position;
				if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, position) < proximityAwareness)
				{
					Debug.Log("SEEING PLAYER");
					return HoarderBugItems[i];
				}
			}
		}
		return null;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3689917697u, __rpc_handler_3689917697, "SyncNestPositionServerRpc");
		__registerRpc(1841413947u, __rpc_handler_1841413947, "SyncNestPositionClientRpc");
		__registerRpc(3510928244u, __rpc_handler_3510928244, "DropItemServerRpc");
		__registerRpc(847487221u, __rpc_handler_847487221, "DropItemClientRpc");
		__registerRpc(2358561451u, __rpc_handler_2358561451, "GrabItemServerRpc");
		__registerRpc(1536760829u, __rpc_handler_1536760829, "GrabItemClientRpc");
		__registerRpc(1884379629u, __rpc_handler_1884379629, "HitPlayerServerRpc");
		__registerRpc(1031891902u, __rpc_handler_1031891902, "HitPlayerClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3689917697(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).SyncNestPositionServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1841413947(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).SyncNestPositionClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3510928244(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			return;
		}
		reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
		reader.ReadValueSafe(out Vector3 value2);
		reader.ReadValueSafe(out bool value3, default(FastBufferWriter.ForPrimitives));
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((HoarderBugAI)target).DropItemServerRpc(value, value2, value3);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_847487221(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out Vector3 value2);
			reader.ReadValueSafe(out bool value3, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).DropItemClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2358561451(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).GrabItemServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1536760829(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).GrabItemClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1884379629(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).HitPlayerServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1031891902(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((HoarderBugAI)target).HitPlayerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "HoarderBugAI";
	}
}
