using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class FlowermanAI : EnemyAI
{
	private bool evadeModeStareDown;

	private bool stopTurningTowardsPlayers;

	public float evadeStealthTimer;

	private int stareDownChanceIncrease;

	public PlayerControllerB lookAtPlayer;

	private Transform localPlayerCamera;

	private RaycastHit rayHit;

	private Ray playerRay;

	public Transform turnCompass;

	private int roomAndEnemiesMask = 8915200;

	private Vector3 agentLocalVelocity;

	public Collider thisEnemyCollider;

	private Vector3 previousPosition;

	private float velX;

	private float velZ;

	[Header("Kill animation")]
	public bool inKillAnimation;

	private Coroutine killAnimationCoroutine;

	public bool carryingPlayerBody;

	public DeadBodyInfo bodyBeingCarried;

	public Transform rightHandGrip;

	public Transform animationContainer;

	private bool wasInEvadeMode;

	public List<Transform> ignoredNodes = new List<Transform>();

	private Vector3 mainEntrancePosition;

	[Header("Anger phase")]
	public float angerMeter;

	public float angerCheckInterval;

	public bool isInAngerMode;

	public AudioSource creatureAngerVoice;

	public AudioSource crackNeckAudio;

	public AudioClip crackNeckSFX;

	public int timesThreatened;

	private Vector3 waitAroundEntrancePosition;

	private int timesFoundSneaking;

	private bool stunnedByPlayerLastFrame;

	private bool startingKillAnimationLocalClient;

	private float getPathToFavoriteNodeInterval;

	private bool gettingFarthestNodeFromPlayerAsync;

	private Transform farthestNodeFromTargetPlayer;

	[Space(5f)]
	public int maxAsync = 50;

	public override void Start()
	{
		base.Start();
		movingTowardsTargetPlayer = true;
		localPlayerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform;
		mainEntrancePosition = RoundManager.FindMainEntrancePosition();
	}

	public override void DoAIInterval()
	{
		if (StartOfRound.Instance.livingPlayers == 0)
		{
			base.DoAIInterval();
			return;
		}
		if (TargetClosestPlayer())
		{
			if (currentBehaviourStateIndex == 2)
			{
				SetMovingTowardsTargetPlayer(targetPlayer);
				if (!inKillAnimation && targetPlayer != GameNetworkManager.Instance.localPlayerController)
				{
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
				}
				base.DoAIInterval();
				return;
			}
			if (currentBehaviourStateIndex == 1)
			{
				if (favoriteSpot != null && carryingPlayerBody)
				{
					if (mostOptimalDistance < 5f || PathIsIntersectedByLineOfSight(favoriteSpot.position))
					{
						AvoidClosestPlayer();
					}
					else
					{
						targetNode = favoriteSpot;
						if (Time.realtimeSinceStartup - getPathToFavoriteNodeInterval > 1f)
						{
							SetDestinationToPosition(favoriteSpot.position, checkForPath: true);
							getPathToFavoriteNodeInterval = Time.realtimeSinceStartup;
						}
					}
				}
				else
				{
					AvoidClosestPlayer();
				}
			}
			else
			{
				ChooseClosestNodeToPlayer();
			}
		}
		else
		{
			if (currentBehaviourStateIndex == 2)
			{
				SetDestinationToPosition(waitAroundEntrancePosition);
				base.DoAIInterval();
				return;
			}
			Transform transform = ChooseFarthestNodeFromPosition(mainEntrancePosition);
			if (favoriteSpot == null)
			{
				favoriteSpot = transform;
			}
			targetNode = transform;
			SetDestinationToPosition(transform.position, checkForPath: true);
		}
		base.DoAIInterval();
	}

	public void AvoidClosestPlayer()
	{
		if (farthestNodeFromTargetPlayer == null)
		{
			gettingFarthestNodeFromPlayerAsync = true;
			return;
		}
		Transform transform = farthestNodeFromTargetPlayer;
		farthestNodeFromTargetPlayer = null;
		float num = Vector3.Distance(transform.transform.position, base.transform.position);
		if (transform != null && num > 5f)
		{
			targetNode = transform;
			SetDestinationToPosition(targetNode.position);
			return;
		}
		if (carryingPlayerBody)
		{
			DropPlayerBody();
			DropPlayerBodyServerRpc();
		}
		if (!Physics.Linecast(transform.transform.position, targetPlayer.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			AddToAngerMeter(AIIntervalTime);
		}
		agent.speed = 0f;
		destination = base.transform.position;
	}

	public void AddToAngerMeter(float amountToAdd)
	{
		Debug.Log($"Add to anger meter called! amount to add: {amountToAdd}; Time of day: {TimeOfDay.Instance.normalizedTimeOfDay}");
		if (stunNormalizedTimer > 0f)
		{
			if (stunnedByPlayer != null)
			{
				stunnedByPlayerLastFrame = true;
				angerMeter = 12f;
			}
			else
			{
				angerMeter = 2f;
			}
			return;
		}
		angerMeter += amountToAdd;
		if (angerMeter <= 0.4f)
		{
			return;
		}
		angerCheckInterval += amountToAdd;
		if (!(angerCheckInterval > 1f))
		{
			return;
		}
		angerCheckInterval = 0f;
		float num = Mathf.Clamp(0.09f * angerMeter, 0f, 0.99f);
		if (Random.Range(0f, 1f) < num)
		{
			if (angerMeter < 2.5f)
			{
				timesThreatened++;
			}
			angerMeter += (float)timesThreatened / 1.75f;
			SwitchToBehaviourStateOnLocalClient(2);
			EnterAngerModeServerRpc(angerMeter);
		}
	}

	[ServerRpc]
	public void EnterAngerModeServerRpc(float angerTime)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(80027368u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in angerTime, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 80027368u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			EnterAngerModeClientRpc(angerTime);
		}
	}

	[ClientRpc]
	public void EnterAngerModeClientRpc(float angerTime)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2307050878u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in angerTime, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 2307050878u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				angerMeter = angerTime;
				SwitchToBehaviourStateOnLocalClient(2);
				waitAroundEntrancePosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(mainEntrancePosition, 6f);
			}
		}
	}

	public void ChooseClosestNodeToPlayer()
	{
		if (targetNode == null)
		{
			targetNode = allAINodes[0].transform;
		}
		Transform transform = ChooseClosestNodeToPosition(targetPlayer.transform.position, avoidLineOfSight: true);
		if (transform != null)
		{
			targetNode = transform;
		}
		float num = Vector3.Distance(targetPlayer.transform.position, base.transform.position);
		if (num - mostOptimalDistance < 0.1f && (num < 4f || !PathIsIntersectedByLineOfSight(targetPlayer.transform.position, calculatePathDistance: true)))
		{
			if (pathDistance > 10f && !ignoredNodes.Contains(targetNode) && ignoredNodes.Count < 4)
			{
				ignoredNodes.Add(targetNode);
			}
			movingTowardsTargetPlayer = true;
		}
		else
		{
			SetDestinationToPosition(targetNode.position);
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead || inKillAnimation || GameNetworkManager.Instance == null)
		{
			return;
		}
		if (IsOwner && gettingFarthestNodeFromPlayerAsync && targetPlayer != null)
		{
			float num = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
			if (num < 26f)
			{
				maxAsync = 80;
			}
			else if (num < 40f)
			{
				maxAsync = 35;
			}
			else
			{
				maxAsync = 4;
			}
			Transform transform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true, 0, doAsync: true, maxAsync, 90);
			if (!gotFarthestNodeAsync)
			{
				return;
			}
			farthestNodeFromTargetPlayer = transform;
			gettingFarthestNodeFromPlayerAsync = false;
		}
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.5f, 30f))
		{
			Debug.DrawLine(GameNetworkManager.Instance.localPlayerController.playerEye.transform.position, base.transform.position, Color.yellow);
			if (currentBehaviourStateIndex == 0)
			{
				SwitchToBehaviourState(1);
				if (!thisNetworkObject.IsOwner)
				{
					ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
				}
				if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 5f)
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.6f);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.3f);
				}
				agent.speed = 0f;
				evadeStealthTimer = 0f;
			}
			else if (evadeStealthTimer > 0.5f)
			{
				int playerObj = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
				LookAtFlowermanTrigger(playerObj);
				ResetFlowermanStealthTimerServerRpc(playerObj);
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 1:
			if (isInAngerMode)
			{
				isInAngerMode = false;
				creatureAnimator.SetBool("anger", value: false);
			}
			if (!wasInEvadeMode)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 7f, 0.8f);
				wasInEvadeMode = true;
				movingTowardsTargetPlayer = false;
				if (favoriteSpot != null && !carryingPlayerBody && Vector3.Distance(base.transform.position, favoriteSpot.position) < 7f)
				{
					favoriteSpot = null;
				}
			}
			if (stunNormalizedTimer > 0f)
			{
				creatureAnimator.SetLayerWeight(2, 1f);
			}
			else
			{
				creatureAnimator.SetLayerWeight(2, 0f);
			}
			evadeStealthTimer += Time.deltaTime;
			if (thisNetworkObject.IsOwner)
			{
				float num2 = ((timesFoundSneaking % 3 != 0) ? 11f : 24f);
				if (favoriteSpot != null && carryingPlayerBody)
				{
					num2 = ((!(Vector3.Distance(base.transform.position, favoriteSpot.position) > 8f)) ? 3f : 24f);
				}
				if (evadeStealthTimer > num2)
				{
					evadeStealthTimer = 0f;
					SwitchToBehaviourState(0);
				}
				if (!carryingPlayerBody && evadeModeStareDown && evadeStealthTimer < 1.25f)
				{
					AddToAngerMeter(Time.deltaTime * 1.5f);
					agent.speed = 0f;
					destination = base.transform.position;
				}
				else
				{
					evadeModeStareDown = false;
					if (stunNormalizedTimer > 0f)
					{
						DropPlayerBody();
						AddToAngerMeter(0f);
						agent.speed = 0f;
						destination = base.transform.position;
					}
					else
					{
						if (stunnedByPlayerLastFrame)
						{
							stunnedByPlayerLastFrame = false;
							AddToAngerMeter(0f);
						}
						if (carryingPlayerBody)
						{
							agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 7.25f, 4f, 9f);
						}
						else
						{
							agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 4.25f, 0f, 6f);
						}
					}
				}
				if (!carryingPlayerBody && ventAnimationFinished)
				{
					LookAtPlayerOfInterest();
				}
			}
			if (!carryingPlayerBody)
			{
				CalculateAnimationDirection();
				break;
			}
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime * 2f));
			previousPosition = base.transform.position;
			break;
		case 0:
			if (isInAngerMode)
			{
				isInAngerMode = false;
				creatureAnimator.SetBool("anger", value: false);
			}
			if (wasInEvadeMode)
			{
				wasInEvadeMode = false;
				evadeStealthTimer = 0f;
				if (carryingPlayerBody)
				{
					DropPlayerBody();
					agent.enabled = true;
					favoriteSpot = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: true);
					if (!IsOwner)
					{
						agent.enabled = false;
					}
					Debug.Log("Flowerman: Dropped player body");
				}
			}
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime / 4f));
			previousPosition = base.transform.position;
			agent.speed = 6f;
			break;
		case 2:
		{
			bool flag = false;
			if (!isInAngerMode)
			{
				isInAngerMode = true;
				DropPlayerBody();
				creatureAngerVoice.Play();
				creatureAngerVoice.pitch = Random.Range(0.9f, 1.3f);
				creatureAnimator.SetBool("anger", value: true);
				creatureAnimator.SetBool("sneak", value: false);
				if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 15, 2.5f))
				{
					flag = true;
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
				}
			}
			if (!flag && GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 13, 4f))
			{
				Debug.DrawLine(GameNetworkManager.Instance.localPlayerController.playerEye.transform.position, base.transform.position, Color.green);
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.8f);
			}
			CalculateAnimationDirection(3f);
			if (stunNormalizedTimer > 0f)
			{
				creatureAnimator.SetLayerWeight(2, 1f);
				agent.speed = 0f;
				angerMeter = 6f;
				Debug.Log("BRACKEN STUNNED!");
			}
			else
			{
				creatureAnimator.SetLayerWeight(2, 0f);
				agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 1.2f, 3f, 12f);
				Debug.Log($"Increasing agent speed by {Time.deltaTime * 1.2f}; AGENT SPEED: {agent.speed}");
			}
			angerMeter -= Time.deltaTime;
			if (IsOwner && angerMeter <= 0f)
			{
				SwitchToBehaviourState(1);
			}
			break;
		}
		}
		if (isInAngerMode)
		{
			creatureAngerVoice.volume = Mathf.Lerp(creatureAngerVoice.volume, 1f, 10f * Time.deltaTime);
		}
		else
		{
			creatureAngerVoice.volume = Mathf.Lerp(creatureAngerVoice.volume, 0f, 2f * Time.deltaTime);
		}
		Vector3 localEulerAngles = animationContainer.localEulerAngles;
		if (carryingPlayerBody)
		{
			agent.angularSpeed = 50f;
			localEulerAngles.z = Mathf.Lerp(localEulerAngles.z, 179f, 10f * Time.deltaTime);
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 1f, 10f * Time.deltaTime));
		}
		else
		{
			agent.angularSpeed = 220f;
			localEulerAngles.z = Mathf.Lerp(localEulerAngles.z, 0f, 10f * Time.deltaTime);
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 0f, 10f * Time.deltaTime));
		}
		animationContainer.localEulerAngles = localEulerAngles;
	}

	[ServerRpc]
	public void DropPlayerBodyServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2817453984u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 2817453984u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			DropPlayerBodyClientRpc();
		}
	}

	[ClientRpc]
	public void DropPlayerBodyClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1942952026u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1942952026u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				DropPlayerBody();
			}
		}
	}

	private void DropPlayerBody()
	{
		if (carryingPlayerBody)
		{
			carryingPlayerBody = false;
			bodyBeingCarried.matchPositionExactly = false;
			bodyBeingCarried.attachedTo = null;
			bodyBeingCarried = null;
			creatureAnimator.SetBool("carryingBody", value: false);
		}
	}

	private void LookAtPlayerOfInterest()
	{
		if (isInAngerMode)
		{
			lookAtPlayer = targetPlayer;
		}
		else
		{
			lookAtPlayer = GetClosestPlayer();
		}
		if (lookAtPlayer != null)
		{
			turnCompass.LookAt(lookAtPlayer.gameplayCamera.transform.position);
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 30f * Time.deltaTime);
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, 0f - agentLocalVelocity.y, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = transform.position;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation || startingKillAnimationLocalClient || carryingPlayerBody);
		if (playerControllerB != null)
		{
			KillPlayerAnimationServerRpc((int)playerControllerB.playerClientId);
			startingKillAnimationLocalClient = true;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerAnimationServerRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(2920701539u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
			__endSendServerRpc(ref bufferWriter, 2920701539u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!inKillAnimation && !carryingPlayerBody)
			{
				inKillAnimation = true;
				inSpecialAnimation = true;
				isClientCalculatingAI = false;
				inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
				inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
				KillPlayerAnimationClientRpc(playerObjectId);
			}
			else
			{
				CancelKillAnimationClientRpc(playerObjectId);
			}
		}
	}

	[ClientRpc]
	public void CancelKillAnimationClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2215703370u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
			__endSendClientRpc(ref bufferWriter, 2215703370u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerObjectId)
			{
				startingKillAnimationLocalClient = false;
			}
		}
	}

	[ClientRpc]
	public void KillPlayerAnimationClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(114605325u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
			__endSendClientRpc(ref bufferWriter, 114605325u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
			if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
			{
				startingKillAnimationLocalClient = false;
			}
			if (inSpecialAnimationWithPlayer == null || inSpecialAnimationWithPlayer.isPlayerDead || !inSpecialAnimationWithPlayer.isInsideFactory)
			{
				FinishKillAnimation(carryingBody: false);
			}
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
			inKillAnimation = true;
			inSpecialAnimation = true;
			creatureAnimator.SetBool("killing", value: true);
			agent.enabled = false;
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
			inSpecialAnimationWithPlayer.snapToServerPosition = true;
			Vector3 vector = ((!inSpecialAnimationWithPlayer.IsOwner) ? inSpecialAnimationWithPlayer.transform.parent.TransformPoint(inSpecialAnimationWithPlayer.serverPlayerPosition) : inSpecialAnimationWithPlayer.transform.position);
			Vector3 position = transform.position;
			position.y = inSpecialAnimationWithPlayer.transform.position.y;
			playerRay = new Ray(vector, position - inSpecialAnimationWithPlayer.transform.position);
			turnCompass.LookAt(vector);
			position = transform.eulerAngles;
			position.y = turnCompass.eulerAngles.y;
			transform.eulerAngles = position;
			if (killAnimationCoroutine != null)
			{
				StopCoroutine(killAnimationCoroutine);
			}
			killAnimationCoroutine = StartCoroutine(killAnimation());
		}
	}

	private IEnumerator killAnimation()
	{
		WalkieTalkie.TransmitOneShotAudio(crackNeckAudio, crackNeckSFX);
		crackNeckAudio.PlayOneShot(crackNeckSFX);
		Vector3 endPosition = playerRay.GetPoint(1f);
		if (endPosition.y < -80f)
		{
			Vector3 startingPosition = transform.position;
			for (int i = 0; i < 5; i++)
			{
				transform.position = Vector3.Lerp(startingPosition, endPosition, (float)i / 5f);
				yield return null;
			}
			transform.position = endPosition;
		}
		creatureAnimator.SetBool("killing", value: false);
		creatureAnimator.SetBool("carryingBody", value: true);
		yield return new WaitForSeconds(0.65f);
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Strangulation);
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			float startTime = Time.timeSinceLevelLoad;
			yield return new WaitUntil(() => inSpecialAnimationWithPlayer.deadBody != null || Time.timeSinceLevelLoad - startTime > 2f);
		}
		if (inSpecialAnimationWithPlayer == null || inSpecialAnimationWithPlayer.deadBody == null)
		{
			Debug.Log("Flowerman: Player body was not spawned or found within 2 seconds.");
			FinishKillAnimation(carryingBody: false);
		}
		else
		{
			inSpecialAnimationWithPlayer.deadBody.bodyBleedingHeavily = true;
			FinishKillAnimation();
		}
	}

	public void FinishKillAnimation(bool carryingBody = true)
	{
		if (killAnimationCoroutine != null)
		{
			StopCoroutine(killAnimationCoroutine);
		}
		inSpecialAnimation = false;
		inKillAnimation = false;
		startingKillAnimationLocalClient = false;
		creatureAnimator.SetBool("killing", value: false);
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
			if (carryingBody)
			{
				bodyBeingCarried = inSpecialAnimationWithPlayer.deadBody;
				bodyBeingCarried.attachedTo = rightHandGrip;
				bodyBeingCarried.attachedLimb = inSpecialAnimationWithPlayer.deadBody.bodyParts[0];
				bodyBeingCarried.matchPositionExactly = true;
				carryingPlayerBody = true;
			}
		}
		evadeStealthTimer = 0f;
		movingTowardsTargetPlayer = false;
		ignoredNodes.Clear();
		if (!carryingBody)
		{
			creatureAnimator.SetBool("carryingBody", value: false);
		}
		if (IsOwner)
		{
			Vector3 position = transform.position;
			position = RoundManager.Instance.GetNavMeshPosition(position, default, 10f);
			if (!RoundManager.Instance.GotNavMeshPositionResult)
			{
				position = ((!Physics.Raycast(transform.position, -Vector3.up, out var hitInfo, 50f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? allAINodes[Random.Range(0, allAINodes.Length)].transform.position : RoundManager.Instance.GetNavMeshPosition(hitInfo.point, default, 10f));
			}
			transform.position = position;
			agent.enabled = true;
			isClientCalculatingAI = true;
		}
		SwitchToBehaviourStateOnLocalClient(1);
		if (IsServer)
		{
			SwitchToBehaviourState(1);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ResetFlowermanStealthTimerServerRpc(int playerObj)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(843847125u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
				__endSendServerRpc(ref bufferWriter, 843847125u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				ResetFlowermanStealthClientRpc(playerObj);
			}
		}
	}

	[ClientRpc]
	public void ResetFlowermanStealthClientRpc(int playerObj)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3273050u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
			__endSendClientRpc(ref bufferWriter, 3273050u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (playerObj != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				LookAtFlowermanTrigger(playerObj);
			}
		}
	}

	public void LookAtFlowermanTrigger(int playerObj)
	{
		if (!IsOwner)
		{
			return;
		}
		if (!evadeModeStareDown)
		{
			if (Random.Range(0, 70) < stareDownChanceIncrease)
			{
				stareDownChanceIncrease = -6;
				evadeModeStareDown = true;
			}
			else
			{
				stareDownChanceIncrease++;
			}
			evadeStealthTimer = 0f;
		}
		if (carryingPlayerBody && favoriteSpot != null && Vector3.Distance(transform.position, favoriteSpot.transform.position) < 5f)
		{
			DropPlayerBody();
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		if (creatureVoice != null)
		{
			creatureVoice.Stop();
		}
		creatureSFX.Stop();
		creatureAngerVoice.Stop();
		creatureAnimator.SetLayerWeight(2, 0f);
		base.KillEnemy();
		if (carryingPlayerBody)
		{
			carryingPlayerBody = false;
			if (bodyBeingCarried != null)
			{
				bodyBeingCarried.matchPositionExactly = false;
				bodyBeingCarried.attachedTo = null;
			}
		}
		if (inKillAnimation)
		{
			FinishKillAnimation(carryingBody: false);
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (isEnemyDead)
		{
			return;
		}
		enemyHP -= force;
		if (IsOwner)
		{
			if (enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
				return;
			}
			angerMeter = 11f;
			angerCheckInterval = 1f;
			AddToAngerMeter(0.1f);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(80027368u, __rpc_handler_80027368, "EnterAngerModeServerRpc");
		__registerRpc(2307050878u, __rpc_handler_2307050878, "EnterAngerModeClientRpc");
		__registerRpc(2817453984u, __rpc_handler_2817453984, "DropPlayerBodyServerRpc");
		__registerRpc(1942952026u, __rpc_handler_1942952026, "DropPlayerBodyClientRpc");
		__registerRpc(2920701539u, __rpc_handler_2920701539, "KillPlayerAnimationServerRpc");
		__registerRpc(2215703370u, __rpc_handler_2215703370, "CancelKillAnimationClientRpc");
		__registerRpc(114605325u, __rpc_handler_114605325, "KillPlayerAnimationClientRpc");
		__registerRpc(843847125u, __rpc_handler_843847125, "ResetFlowermanStealthTimerServerRpc");
		__registerRpc(3273050u, __rpc_handler_3273050, "ResetFlowermanStealthClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_80027368(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).EnterAngerModeServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2307050878(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).EnterAngerModeClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2817453984(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((FlowermanAI)target).DropPlayerBodyServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1942952026(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).DropPlayerBodyClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2920701539(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).KillPlayerAnimationServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2215703370(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).CancelKillAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_114605325(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).KillPlayerAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_843847125(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).ResetFlowermanStealthTimerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3273050(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowermanAI)target).ResetFlowermanStealthClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "FlowermanAI";
	}
}
