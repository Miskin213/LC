using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SpringManAI : EnemyAI
{
	public AISearchRoutine searchForPlayers;

	private bool stoppingMovement;

	private bool hasStopped;

	public AnimationStopPoints animStopPoints;

	private float currentChaseSpeed = 14.5f;

	private float currentAnimSpeed = 1f;

	private PlayerControllerB previousTarget;

	private bool wasOwnerLastFrame;

	private float stopAndGoMinimumInterval;

	private float hitPlayerTimer;

	public AudioClip[] springNoises;

	public AudioClip enterCooldownSFX;

	public Collider mainCollider;

	private float loseAggroTimer;

	private float timeSinceHittingPlayer;

	private Coroutine offMeshLinkCoroutine;

	private float stopMovementTimer;

	public float timeSpentMoving;

	public float onCooldownPhase;

	private bool setOnCooldown;

	public float timeAtLastCooldown;

	private bool inCooldownAnimation;

	private Vector3 previousPosition;

	private float checkPositionInterval;

	private bool isMakingDistance;

	[ServerRpc(RequireOwnership = false)]
	public void SetCoilheadOnCooldownServerRpc(bool setTrue)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1507778120u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setTrue, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 1507778120u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetCoilheadOnCooldownClientRpc(setTrue);
			}
		}
	}

	[ClientRpc]
	public void SetCoilheadOnCooldownClientRpc(bool setTrue)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1116073473u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setTrue, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1116073473u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			timeSpentMoving = 0f;
			if (setTrue)
			{
				onCooldownPhase = 20f;
				setOnCooldown = true;
				inCooldownAnimation = true;
				SwitchToBehaviourStateOnLocalClient(0);
				creatureVoice.PlayOneShot(enterCooldownSFX);
			}
			else
			{
				onCooldownPhase = 0f;
				setOnCooldown = false;
				timeAtLastCooldown = Time.realtimeSinceStartup;
			}
		}
	}

	public bool PlayerHasHorizontalLOS(PlayerControllerB player)
	{
		Vector3 to = transform.position - player.transform.position;
		to.y = 0f;
		return Vector3.Angle(player.transform.forward, to) < 68f;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (!IsServer)
			{
				ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				break;
			}
			if (onCooldownPhase > 0f)
			{
				agent.speed = 0f;
				SetDestinationToPosition(transform.position);
				onCooldownPhase -= AIIntervalTime;
				break;
			}
			if (setOnCooldown)
			{
				setOnCooldown = false;
				SetCoilheadOnCooldownClientRpc(setTrue: false);
			}
			loseAggroTimer = 0f;
			for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
			{
				if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[j]))
				{
					if ((StartOfRound.Instance.allPlayerScripts[j].HasLineOfSightToPosition(transform.position + Vector3.up * 0.25f, 68f) || StartOfRound.Instance.allPlayerScripts[j].HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[j]))
					{
						targetPlayer = StartOfRound.Instance.allPlayerScripts[j];
						SwitchToBehaviourState(1);
					}
					if (!PathIsIntersectedByLineOfSight(StartOfRound.Instance.allPlayerScripts[j].transform.position, calculatePathDistance: false, avoidLineOfSight: false) && !Physics.Linecast(transform.position + Vector3.up * 0.5f, StartOfRound.Instance.allPlayerScripts[j].gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[j].transform.position) < 30f)
					{
						SwitchToBehaviourState(1);
						return;
					}
				}
			}
			agent.speed = 6f;
			if (!searchForPlayers.inProgress)
			{
				movingTowardsTargetPlayer = false;
				SetDestinationToPosition(transform.position);
				StartSearch(transform.position, searchForPlayers);
			}
			break;
		}
		case 1:
		{
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			if (TargetClosestPlayer())
			{
				if (previousTarget != targetPlayer)
				{
					previousTarget = targetPlayer;
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
				}
				if (!(Time.realtimeSinceStartup - timeSinceHittingPlayer > 7f) || stoppingMovement)
				{
					break;
				}
				if (Vector3.Distance(targetPlayer.transform.position, transform.position) > 40f && !CheckLineOfSightForPosition(targetPlayer.gameplayCamera.transform.position, 180f, 140))
				{
					loseAggroTimer += AIIntervalTime;
					if (loseAggroTimer > 4.5f)
					{
						SwitchToBehaviourState(0);
						ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
					}
				}
				else
				{
					loseAggroTimer = 0f;
				}
				break;
			}
			bool flag = false;
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if ((StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(transform.position + Vector3.up * 0.6f, 68f) || StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(transform.position + Vector3.up * 3f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				loseAggroTimer += AIIntervalTime;
				if (loseAggroTimer > 1f)
				{
					SwitchToBehaviourState(0);
					ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				}
			}
			break;
		}
		}
	}

	private IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
	{
		OffMeshLinkData data = agent.currentOffMeshLinkData;
		Vector3 startPos = agent.transform.position;
		Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
		float normalizedTime = 0f;
		while (normalizedTime < 1f && data.valid && data.activated && IsOwner)
		{
			float num = height * 4f * (normalizedTime - normalizedTime * normalizedTime);
			agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + num * Vector3.up;
			normalizedTime += Time.deltaTime / duration;
			yield return null;
		}
		agent.CompleteOffMeshLink();
		offMeshLinkCoroutine = null;
	}

	private void StopOffMeshLinkMovement()
	{
		if (offMeshLinkCoroutine == null)
		{
			return;
		}
		StopCoroutine(offMeshLinkCoroutine);
		offMeshLinkCoroutine = null;
		OffMeshLinkData currentOffMeshLinkData = agent.currentOffMeshLinkData;
		agent.CompleteOffMeshLink();
		if (currentOffMeshLinkData.valid)
		{
			if (Vector3.Distance(transform.position, currentOffMeshLinkData.startPos) < Vector3.Distance(transform.position, currentOffMeshLinkData.endPos))
			{
				agent.Warp(currentOffMeshLinkData.startPos);
			}
			else
			{
				agent.Warp(currentOffMeshLinkData.endPos);
			}
		}
	}

	private void DoSpringAnimation(bool springPopUp = false)
	{
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.6f, 70f, 25))
		{
			float num = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
			if (num < 4f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
			}
			else if (num < 9f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
			}
		}
		if ((currentAnimSpeed > 2f) | springPopUp)
		{
			RoundManager.PlayRandomClip(creatureVoice, springNoises, randomize: false);
			if (animStopPoints.animationPosition == 1)
			{
				creatureAnimator.SetTrigger("springBoing");
			}
			else
			{
				creatureAnimator.SetTrigger("springBoingPosition2");
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		if (hitPlayerTimer >= 0f)
		{
			hitPlayerTimer -= Time.deltaTime;
		}
		if (!IsOwner)
		{
			stopMovementTimer = 5f;
			loseAggroTimer = 0f;
			if (offMeshLinkCoroutine != null)
			{
				StopCoroutine(offMeshLinkCoroutine);
			}
		}
		if (IsOwner)
		{
			if (agent.isOnOffMeshLink)
			{
				if (!stoppingMovement && agent.currentOffMeshLinkData.activated)
				{
					if (offMeshLinkCoroutine == null)
					{
						offMeshLinkCoroutine = StartCoroutine(Parabola(agent, 0.6f, 0.5f));
					}
				}
				else
				{
					StopOffMeshLinkMovement();
				}
			}
			else if (offMeshLinkCoroutine != null)
			{
				StopOffMeshLinkMovement();
			}
		}
		creatureAnimator.SetBool("OnCooldown", setOnCooldown);
		if (setOnCooldown)
		{
			mainCollider.isTrigger = true;
			stoppingMovement = true;
			hasStopped = true;
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			agent.autoTraverseOffMeshLink = false;
			creatureAnimator.SetFloat("walkSpeed", 4.7f);
			stoppingMovement = false;
			hasStopped = false;
			break;
		case 1:
		{
			if (IsOwner)
			{
				if (onCooldownPhase > 0f)
				{
					SwitchToBehaviourState(0);
					break;
				}
				if (stopAndGoMinimumInterval > 0f)
				{
					stopAndGoMinimumInterval -= Time.deltaTime;
				}
				if (!wasOwnerLastFrame)
				{
					wasOwnerLastFrame = true;
					if (!stoppingMovement && hitPlayerTimer < 0.12f)
					{
						agent.speed = currentChaseSpeed;
					}
					else
					{
						agent.speed = 0f;
					}
				}
				bool flag = false;
				for (int i = 0; i < 4; i++)
				{
					if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(transform.position + Vector3.up * 0.6f, 68f) || StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(transform.position + Vector3.up * 3f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[i]) && Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, eye.position) > 0.3f)
					{
						flag = true;
					}
				}
				if (stunNormalizedTimer > 0f)
				{
					flag = true;
				}
				if (flag != stoppingMovement && stopAndGoMinimumInterval <= 0f)
				{
					stopAndGoMinimumInterval = 0.15f;
					if (flag)
					{
						SetAnimationStopServerRpc();
					}
					else
					{
						SetAnimationGoServerRpc();
					}
					stoppingMovement = flag;
				}
			}
			float num = 0f;
			if (stoppingMovement)
			{
				if (animStopPoints.canAnimationStop || stopMovementTimer > 0.27f)
				{
					if (!hasStopped)
					{
						hasStopped = true;
						DoSpringAnimation();
					}
					else if (inCooldownAnimation)
					{
						inCooldownAnimation = false;
						DoSpringAnimation(springPopUp: true);
					}
					if (RoundManager.Instance.currentMineshaftElevator != null && Vector3.Distance(transform.position, RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position) < 1f)
					{
						num = 0.5f;
					}
					if (mainCollider.isTrigger && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position) > 0.75f)
					{
						mainCollider.isTrigger = false;
					}
					creatureAnimator.SetFloat("walkSpeed", 0f);
					currentAnimSpeed = 0f;
					if (IsOwner)
					{
						agent.speed = 0f;
						movingTowardsTargetPlayer = false;
						SetDestinationToPosition(transform.position);
					}
				}
				else
				{
					stopMovementTimer += Time.deltaTime;
				}
			}
			else
			{
				stopMovementTimer = 0f;
				if (hasStopped)
				{
					hasStopped = false;
					mainCollider.isTrigger = true;
					isMakingDistance = true;
				}
				currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, 6f, 5f * Time.deltaTime);
				creatureAnimator.SetFloat("walkSpeed", currentAnimSpeed);
				inCooldownAnimation = false;
				if (IsServer)
				{
					if (checkPositionInterval <= 0f)
					{
						checkPositionInterval = 0.65f;
						isMakingDistance = Vector3.Distance(transform.position, previousPosition) > 0.5f;
						previousPosition = transform.position;
					}
					else
					{
						checkPositionInterval -= Time.deltaTime;
					}
					num = ((!isMakingDistance) ? 0.2f : 1f);
				}
				if (IsOwner)
				{
					agent.speed = Mathf.Lerp(agent.speed, currentChaseSpeed, 4.5f * Time.deltaTime);
					movingTowardsTargetPlayer = true;
				}
			}
			if (IsServer)
			{
				if (num > 0f)
				{
					timeSpentMoving += Time.deltaTime * num;
				}
				if (timeSpentMoving > 9f)
				{
					onCooldownPhase = 11f;
					setOnCooldown = true;
					inCooldownAnimation = true;
					SetCoilheadOnCooldownClientRpc(setTrue: true);
					SwitchToBehaviourStateOnLocalClient(0);
				}
			}
			break;
		}
		}
	}

	[ServerRpc]
	public void SetAnimationStopServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1502362896u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1502362896u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetAnimationStopClientRpc();
		}
	}

	[ClientRpc]
	public void SetAnimationStopClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(718630829u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 718630829u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				stoppingMovement = true;
			}
		}
	}

	[ServerRpc]
	public void SetAnimationGoServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(339140592u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 339140592u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetAnimationGoClientRpc();
		}
	}

	[ClientRpc]
	public void SetAnimationGoClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3626523253u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3626523253u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				stoppingMovement = false;
			}
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!stoppingMovement && currentBehaviourStateIndex == 1 && !(hitPlayerTimer >= 0f) && !setOnCooldown && !((double)(Time.realtimeSinceStartup - timeAtLastCooldown) < 0.45))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				hitPlayerTimer = 0.2f;
				playerControllerB.DamagePlayer(90, hasDamageSFX: true, callRPC: true, CauseOfDeath.Strangulation, 2);
				playerControllerB.JumpToFearLevel(1f);
				timeSinceHittingPlayer = Time.realtimeSinceStartup;
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1507778120u, __rpc_handler_1507778120, "SetCoilheadOnCooldownServerRpc");
		__registerRpc(1116073473u, __rpc_handler_1116073473, "SetCoilheadOnCooldownClientRpc");
		__registerRpc(1502362896u, __rpc_handler_1502362896, "SetAnimationStopServerRpc");
		__registerRpc(718630829u, __rpc_handler_718630829, "SetAnimationStopClientRpc");
		__registerRpc(339140592u, __rpc_handler_339140592, "SetAnimationGoServerRpc");
		__registerRpc(3626523253u, __rpc_handler_3626523253, "SetAnimationGoClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1507778120(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SpringManAI)target).SetCoilheadOnCooldownServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1116073473(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SpringManAI)target).SetCoilheadOnCooldownClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1502362896(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((SpringManAI)target).SetAnimationStopServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_718630829(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SpringManAI)target).SetAnimationStopClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_339140592(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((SpringManAI)target).SetAnimationGoServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3626523253(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((SpringManAI)target).SetAnimationGoClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "SpringManAI";
	}
}
