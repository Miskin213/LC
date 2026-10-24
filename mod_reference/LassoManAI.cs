using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class LassoManAI : EnemyAI
{
	public AISearchRoutine searchForPlayers;

	private float checkLineOfSightInterval;

	public float maxSearchAndRoamRadius = 100f;

	[Space(5f)]
	public float noticePlayerTimer;

	private bool hasEnteredChaseMode;

	private bool lostPlayerInChase;

	private bool beginningChasingThisClient;

	private float timeSinceHittingPlayer;

	public DeadBodyInfo currentlyHeldBody;

	public override void Start()
	{
		base.Start();
		searchForPlayers.searchWidth = maxSearchAndRoamRadius;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (!searchForPlayers.inProgress)
			{
				StartSearch(transform.position, searchForPlayers);
				Debug.Log($"Crawler: Started new search; is searching?: {searchForPlayers.inProgress}");
			}
			break;
		case 1:
			if (lostPlayerInChase)
			{
				if (!searchForPlayers.inProgress)
				{
					searchForPlayers.searchWidth = 30f;
					StartSearch(targetPlayer.transform.position, searchForPlayers);
					Debug.Log("Crawler: Lost player in chase; beginning search where the player was last seen");
				}
			}
			else if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
				movingTowardsTargetPlayer = true;
				Debug.Log("Crawler: Found player during chase; stopping search coroutine and moving after target player");
			}
			break;
		}
	}

	public override void FinishedCurrentSearchRoutine()
	{
		base.FinishedCurrentSearchRoutine();
		searchForPlayers.searchWidth = Mathf.Clamp(searchForPlayers.searchWidth + 20f, 1f, maxSearchAndRoamRadius);
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 60f, 8, 5f))
		{
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position) < 7f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.5f, 0.6f);
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (hasEnteredChaseMode)
			{
				hasEnteredChaseMode = false;
				beginningChasingThisClient = false;
				noticePlayerTimer = 0f;
				useSecondaryAudiosOnAnimatedObjects = false;
				openDoorSpeedMultiplier = 0.6f;
				agent.speed = 5f;
			}
			if (checkLineOfSightInterval <= 0.05f)
			{
				checkLineOfSightInterval += Time.deltaTime;
				break;
			}
			checkLineOfSightInterval = 0f;
			PlayerControllerB playerControllerB2;
			if (stunnedByPlayer != null)
			{
				playerControllerB2 = stunnedByPlayer;
				noticePlayerTimer = 1f;
			}
			else
			{
				playerControllerB2 = CheckLineOfSightForPlayer(55f);
			}
			if (playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
			{
				Debug.Log($"Seeing player; {noticePlayerTimer}");
				noticePlayerTimer = Mathf.Clamp(noticePlayerTimer + 0.05f, 0f, 10f);
				if (noticePlayerTimer > 0.1f && !beginningChasingThisClient)
				{
					beginningChasingThisClient = true;
					BeginChasingPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
					Debug.Log("Begin chasing local client");
				}
			}
			else
			{
				noticePlayerTimer -= Time.deltaTime;
			}
			break;
		}
		case 1:
		{
			if (!hasEnteredChaseMode)
			{
				hasEnteredChaseMode = true;
				lostPlayerInChase = false;
				checkLineOfSightInterval = 0f;
				noticePlayerTimer = 0f;
				beginningChasingThisClient = false;
				useSecondaryAudiosOnAnimatedObjects = true;
				openDoorSpeedMultiplier = 1.5f;
				agent.speed = 6f;
			}
			if (!IsOwner || stunNormalizedTimer > 0f)
			{
				break;
			}
			if (checkLineOfSightInterval <= 0.075f)
			{
				checkLineOfSightInterval += Time.deltaTime;
				break;
			}
			checkLineOfSightInterval = 0f;
			if (lostPlayerInChase)
			{
				if ((bool)CheckLineOfSightForPlayer(55f))
				{
					noticePlayerTimer = 0f;
					lostPlayerInChase = false;
					MakeScreechNoiseServerRpc();
					break;
				}
				noticePlayerTimer -= 0.075f;
				if (noticePlayerTimer < -15f)
				{
					SwitchToBehaviourState(0);
				}
				break;
			}
			PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(55f);
			if (playerControllerB != null)
			{
				Debug.Log("Seeing player!!!!");
				noticePlayerTimer = 0f;
				if (playerControllerB != targetPlayer)
				{
					targetPlayer = playerControllerB;
				}
			}
			else
			{
				noticePlayerTimer += 0.075f;
				if (noticePlayerTimer > 2.5f)
				{
					lostPlayerInChase = true;
				}
			}
			break;
		}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BeginChasingPlayerServerRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1325919844u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
				__endSendServerRpc(ref bufferWriter, 1325919844u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				BeginChasingPlayerClientRpc(playerObjectId);
			}
		}
	}

	[ClientRpc]
	public void BeginChasingPlayerClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1984359406u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
				__endSendClientRpc(ref bufferWriter, 1984359406u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SwitchToBehaviourStateOnLocalClient(1);
				SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerObjectId]);
			}
		}
	}

	[ServerRpc]
	public void MakeScreechNoiseServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3259100395u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 3259100395u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			MakeScreechNoiseClientRpc();
		}
	}

	[ClientRpc]
	public void MakeScreechNoiseClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(668114799u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 668114799u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
			}
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
		if (!isEnemyDead && component != null && component == GameNetworkManager.Instance.localPlayerController && component.inAnimationWithEnemy == null && !component.isPlayerDead && timeSinceHittingPlayer > 0.5f)
		{
			timeSinceHittingPlayer = 0f;
			component.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Strangulation);
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy();
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("HurtEnemy");
			enemyHP--;
			if (enemyHP <= 0 && IsOwner)
			{
				KillEnemyOnOwnerClient();
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1325919844u, __rpc_handler_1325919844, "BeginChasingPlayerServerRpc");
		__registerRpc(1984359406u, __rpc_handler_1984359406, "BeginChasingPlayerClientRpc");
		__registerRpc(3259100395u, __rpc_handler_3259100395, "MakeScreechNoiseServerRpc");
		__registerRpc(668114799u, __rpc_handler_668114799, "MakeScreechNoiseClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1325919844(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LassoManAI)target).BeginChasingPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1984359406(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LassoManAI)target).BeginChasingPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3259100395(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((LassoManAI)target).MakeScreechNoiseServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_668114799(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((LassoManAI)target).MakeScreechNoiseClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "LassoManAI";
	}
}
