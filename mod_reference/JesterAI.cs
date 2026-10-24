using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class JesterAI : EnemyAI
{
	public AudioSource farAudio;

	public AISearchRoutine roamMap;

	private Vector3 spawnPosition;

	public float popUpTimer;

	public float beginCrankingTimer;

	private int previousState;

	public AudioClip popGoesTheWeaselTheme;

	public AudioClip popUpSFX;

	public AudioClip screamingSFX;

	public AudioClip killPlayerSFX;

	private Vector3 previousPosition;

	public float maxAnimSpeed;

	private float noPlayersToChaseTimer;

	private bool targetingPlayer;

	public Transform headRigTarget;

	public Transform lookForwardTarget;

	public Collider mainCollider;

	private bool inKillAnimation;

	private Coroutine killPlayerAnimCoroutine;

	public Transform grabBodyPoint;

	public override void Start()
	{
		base.Start();
		spawnPosition = transform.position;
		SetJesterInitialValues();
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		if (!IsServer && IsOwner && currentBehaviourStateIndex != 2)
		{
			ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = 5f;
			}
			agent.stoppingDistance = 4f;
			addPlayerVelocityToDestination = 0f;
			PlayerControllerB playerControllerB = targetPlayer;
			if (TargetClosestPlayer(3f, requireLineOfSight: true, 70f, doGroundCast: true))
			{
				if (roamMap.inProgress)
				{
					StopSearch(roamMap);
				}
				SetMovingTowardsTargetPlayer(targetPlayer);
			}
			else
			{
				targetPlayer = playerControllerB;
			}
			if (!(targetPlayer != null) && targetPlayer == null && !roamMap.inProgress)
			{
				StartSearch(spawnPosition, roamMap);
			}
			break;
		}
		case 1:
			agent.speed = 0f;
			break;
		case 2:
		{
			agent.stoppingDistance = 0f;
			PlayerControllerB playerControllerB = targetPlayer;
			bool flag = false;
			if (targetPlayer == null)
			{
				flag = true;
			}
			else if (!PlayerIsTargetable(targetPlayer) || PathIsIntersectedByLineOfSight(RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, default, 5f, agent.areaMask)))
			{
				flag = true;
			}
			addPlayerVelocityToDestination = 1f;
			if (!TargetClosestPlayer(4f) && playerControllerB != null && !flag)
			{
				targetPlayer = playerControllerB;
			}
			if (targetPlayer != null)
			{
				if (roamMap.inProgress)
				{
					StopSearch(roamMap);
				}
				SetMovingTowardsTargetPlayer(targetPlayer);
				if (targetPlayer != playerControllerB)
				{
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
				}
			}
			break;
		}
		}
	}

	private void CalculateAnimationSpeed(float maxSpeed = 1f)
	{
		float num = Vector3.ClampMagnitude(transform.position - previousPosition, maxSpeed).magnitude / (Time.deltaTime * 3f);
		creatureAnimator.SetFloat("speedOfMovement", num);
		previousPosition = transform.position;
		creatureAnimator.SetBool("walking", num > 0.05f);
	}

	private void SetJesterInitialValues()
	{
		targetPlayer = null;
		popUpTimer = Random.Range(35f, 40f);
		beginCrankingTimer = Random.Range(13f, 21f);
		if (StartOfRound.Instance.connectedPlayersAmount == 0)
		{
			beginCrankingTimer = Random.Range(25f, 42f);
		}
		creatureAnimator.SetBool("turningCrank", value: false);
		creatureAnimator.SetBool("poppedOut", value: false);
		creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);
		creatureAnimator.SetBool("stunned", value: false);
		mainCollider.isTrigger = false;
		noPlayersToChaseTimer = 0f;
		farAudio.Stop();
		creatureVoice.Stop();
		creatureSFX.Stop();
	}

	public override void Update()
	{
		if (isEnemyDead)
		{
			return;
		}
		CalculateAnimationSpeed(maxAnimSpeed);
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousState != 0)
			{
				previousState = 0;
				mainCollider.isTrigger = false;
				SetJesterInitialValues();
			}
			if (!IsOwner)
			{
				break;
			}
			if (stunNormalizedTimer > 0f)
			{
				beginCrankingTimer -= Time.deltaTime * 15f;
			}
			if (targetPlayer != null)
			{
				beginCrankingTimer -= Time.deltaTime;
				if (beginCrankingTimer <= 0f)
				{
					SwitchToBehaviourState(1);
				}
			}
			break;
		case 1:
			if (previousState != 1)
			{
				previousState = 1;
				creatureAnimator.SetBool("turningCrank", value: true);
				farAudio.clip = popGoesTheWeaselTheme;
				farAudio.Play();
				agent.speed = 0f;
			}
			if (stunNormalizedTimer > 0f)
			{
				farAudio.Pause();
				creatureAnimator.SetFloat("CrankSpeedMultiplier", 0f);
			}
			else
			{
				if (!farAudio.isPlaying)
				{
					farAudio.UnPause();
				}
				creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);
				popUpTimer -= Time.deltaTime;
				if (popUpTimer <= 0f && IsOwner)
				{
					SwitchToBehaviourState(2);
				}
			}
			if (IsOwner)
			{
			}
			break;
		case 2:
			if (previousState != 2)
			{
				previousState = 2;
				farAudio.Stop();
				creatureAnimator.SetBool("poppedOut", value: true);
				creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);
				creatureSFX.PlayOneShot(popUpSFX);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, popUpSFX);
				creatureVoice.clip = screamingSFX;
				creatureVoice.Play();
				agent.speed = 0f;
				mainCollider.isTrigger = true;
				agent.stoppingDistance = 0f;
			}
			if (IsOwner && targetPlayer != null && CheckLineOfSightForPosition(targetPlayer.gameplayCamera.transform.position, 80f, 80))
			{
				headRigTarget.rotation = Quaternion.Lerp(headRigTarget.rotation, Quaternion.LookRotation(targetPlayer.gameplayCamera.transform.position - headRigTarget.transform.position, Vector3.up), 5f * Time.deltaTime);
			}
			else
			{
				headRigTarget.rotation = Quaternion.Lerp(headRigTarget.rotation, Quaternion.LookRotation(lookForwardTarget.position - headRigTarget.transform.position, Vector3.up), 5f * Time.deltaTime);
			}
			if (!IsOwner)
			{
				break;
			}
			if (inKillAnimation || stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 1.45f, 0f, 18f);
			}
			creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
			if (!targetingPlayer)
			{
				bool flag = false;
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
				{
					if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].isInsideFactory)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					noPlayersToChaseTimer -= Time.deltaTime;
					if (noPlayersToChaseTimer <= 0f)
					{
						SwitchToBehaviourState(0);
					}
				}
			}
			else
			{
				noPlayersToChaseTimer = 5f;
			}
			break;
		}
		base.Update();
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		if (!other.gameObject.GetComponent<PlayerControllerB>())
		{
			return;
		}
		Debug.Log("Jester collided with player: " + other.gameObject.name);
		base.OnCollideWithPlayer(other);
		if (inKillAnimation)
		{
			return;
		}
		Debug.Log("Jester collided A");
		if (isEnemyDead)
		{
			return;
		}
		Debug.Log("Jester collided C");
		if (currentBehaviourStateIndex == 2)
		{
			Debug.Log("Jester collided D");
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				inKillAnimation = true;
				KillPlayerServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(3446243450u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 3446243450u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!inKillAnimation || StartOfRound.Instance.allPlayerScripts[playerId].IsOwnedByServer)
			{
				inKillAnimation = true;
				KillPlayerClientRpc(playerId);
			}
			else
			{
				CancelKillPlayerClientRpc();
			}
		}
	}

	[ClientRpc]
	public void CancelKillPlayerClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1851545498u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1851545498u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (killPlayerAnimCoroutine == null)
			{
				inKillAnimation = false;
			}
		}
	}

	[ClientRpc]
	public void KillPlayerClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(569892066u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 569892066u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (killPlayerAnimCoroutine != null)
			{
				StopCoroutine(killPlayerAnimCoroutine);
			}
			killPlayerAnimCoroutine = StartCoroutine(killPlayerAnimation(playerId));
		}
	}

	private IEnumerator killPlayerAnimation(int playerId)
	{
		creatureSFX.PlayOneShot(killPlayerSFX);
		inKillAnimation = true;
		PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
		playerScript.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Mauling);
		creatureAnimator.SetTrigger("KillPlayer");
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => playerScript.deadBody != null || Time.realtimeSinceStartup - startTime > 2f);
		DeadBodyInfo body = playerScript.deadBody;
		if (body != null && body.attachedTo == null)
		{
			body.attachedLimb = body.bodyParts[5];
			body.attachedTo = grabBodyPoint;
			body.matchPositionExactly = true;
		}
		yield return new WaitForSeconds(1.8f);
		if (body != null && body.attachedTo == grabBodyPoint)
		{
			body.attachedLimb = null;
			body.attachedTo = null;
			body.matchPositionExactly = false;
		}
		yield return new WaitForSeconds(0.4f);
		inKillAnimation = false;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3446243450u, __rpc_handler_3446243450, "KillPlayerServerRpc");
		__registerRpc(1851545498u, __rpc_handler_1851545498, "CancelKillPlayerClientRpc");
		__registerRpc(569892066u, __rpc_handler_569892066, "KillPlayerClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3446243450(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((JesterAI)target).KillPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1851545498(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((JesterAI)target).CancelKillPlayerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_569892066(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((JesterAI)target).KillPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "JesterAI";
	}
}
