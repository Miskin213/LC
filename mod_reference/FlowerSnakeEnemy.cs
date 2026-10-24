using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class FlowerSnakeEnemy : EnemyAI
{
	public AISearchRoutine snakeRoam;

	private float timeSinceSeeingTarget;

	private bool leaping;

	private Vector3 leapDirection;

	public AnimationCurve leapVerticalCurve;

	public Transform meshContainer;

	private Vector3 startLeapPosition;

	private float leapTime;

	public float leapTimeMultiplier = 5f;

	public float leapSpeedMultiplier = 1f;

	public float leapVerticalMultiplier = 4f;

	public PlayerControllerB clingingToPlayer;

	private bool waitingForHitPlayerRPC;

	public int clingPosition;

	private float collideWithPlayerInterval;

	private Vector3 previousPosition;

	public float spinePositionUpOffset;

	public float spinePositionRightOffset;

	private float timeOfLastCling;

	private bool choseFarawayNode;

	private float clingingPlayerFlapInterval;

	public static FlowerSnakeEnemy[] mainSnakes;

	private bool flapping;

	private float flapIntervalTimeOffset = 4f;

	private RaycastHit hit;

	private float leapVerticalPosition;

	private bool hitWallInLeap;

	private float fallFromLeapTimer = 1f;

	private bool fallingFromLeap;

	public Vector3 landingPoint;

	private Vector3 vel;

	public Material[] randomSkinColor;

	public int snakesOnPlayer;

	public bool activatedFlight;

	public float flightPower;

	private Vector3 forces;

	public float chuckleInterval;

	public AudioSource flappingAudio;

	public float clingToPlayerTimer;

	public float timeOfLastLeap;

	public override void Start()
	{
		base.Start();
		if (mainSnakes == null)
		{
			mainSnakes = new FlowerSnakeEnemy[StartOfRound.Instance.allPlayerScripts.Length];
		}
		Material material = null;
		int num = new System.Random(StartOfRound.Instance.randomMapSeed + (int)NetworkObjectId).Next(0, 100);
		if (num < 33)
		{
			material = randomSkinColor[0];
		}
		else
		{
			material = ((num <= 66) ? randomSkinColor[2] : randomSkinColor[1]);
		}
		for (int i = 0; i < skinnedMeshRenderers.Length; i++)
		{
			skinnedMeshRenderers[i].sharedMaterial = material;
		}
		flappingAudio.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		flappingAudio.clip = enemyType.audioClips[9];
		flappingAudio.Play();
	}

	[ClientRpc]
	public void StartLeapClientRpc(Vector3 setLeapDir)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2859615571u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setLeapDir);
			__endSendClientRpc(ref bufferWriter, 2859615571u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				StartLeapOnLocalClient(setLeapDir);
			}
		}
	}

	public void StartLeapOnLocalClient(Vector3 leapDir)
	{
		leaping = true;
		leapTime = 0f;
		hitWallInLeap = false;
		inSpecialAnimation = true;
		agent.enabled = false;
		leapVerticalPosition = transform.position.y;
		creatureAnimator.SetBool("Leaping", value: true);
		flappingAudio.Stop();
		leapDirection = leapDir;
		startLeapPosition = transform.position;
		creatureVoice.Stop();
		int num = UnityEngine.Random.Range(0, 100);
		if (num < 33)
		{
			creatureVoice.PlayOneShot(enemyType.audioClips[6]);
		}
		else if (num < 66)
		{
			creatureVoice.PlayOneShot(enemyType.audioClips[10]);
		}
		else
		{
			creatureVoice.PlayOneShot(enemyType.audioClips[7]);
		}
	}

	public void StopLeapOnLocalClient(bool landOnGround = false, Vector3 overrideLandingPosition = default(Vector3))
	{
		leaping = false;
		leapTime = 0f;
		creatureAnimator.SetBool("Leaping", value: false);
		if (!landOnGround)
		{
			return;
		}
		if (!isEnemyDead)
		{
			flappingAudio.clip = enemyType.audioClips[9];
			flappingAudio.Play();
		}
		if (!IsServer)
		{
			landingPoint = overrideLandingPosition;
		}
		else
		{
			Vector3 vector = RoundManager.Instance.GetNavMeshPosition(base.transform.position, default, 15f, agentMask);
			if (!RoundManager.Instance.GotNavMeshPositionResult)
			{
				Transform transform = ChooseClosestNodeToPosition(base.transform.position);
				Debug.Log($"Tulip snake: Could not get nav mesh position from pos: {base.transform.position}; agentMask: {agentMask}; instead chose node at pos: {transform.transform.position}");
				vector = transform.transform.position;
			}
			landingPoint = vector;
		}
		SetEnemyOutside(landingPoint.y > -100f);
		serverPosition = landingPoint;
		fallingFromLeap = true;
		fallFromLeapTimer = 1f;
		timeOfLastLeap = Time.realtimeSinceStartup;
		if (IsOwner)
		{
			StopLeapClientRpc(landingPoint);
		}
	}

	public void StopClingingOnLocalClient(bool isMainSnake = false)
	{
		inSpecialAnimation = false;
		clingPosition = 0;
		creatureAnimator.SetInteger("clingType", 0);
		SetFlappingLocalClient(setFlapping: false, isMainSnake);
		if (!isEnemyDead)
		{
			flappingAudio.clip = enemyType.audioClips[9];
			flappingAudio.Play();
		}
		flightPower = 0f;
		timeOfLastCling = Time.realtimeSinceStartup;
		Vector3 vector = RoundManager.Instance.GetNavMeshPosition(transform.position, default, 15f);
		if (!RoundManager.Instance.GotNavMeshPositionResult)
		{
			vector = ChooseClosestNodeToPosition(transform.position).transform.position;
		}
		SetEnemyOutside(vector.y > -100f);
		transform.position = vector;
		serverPosition = transform.position;
		if (isMainSnake)
		{
			FlowerSnakeEnemy[] array = UnityEngine.Object.FindObjectsByType<FlowerSnakeEnemy>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] == this) && array[i].clingingToPlayer != null && array[i].clingingToPlayer == clingingToPlayer)
				{
					array[i].StopClingingOnLocalClient();
				}
			}
		}
		if (clingingToPlayer != null)
		{
			isInsidePlayerShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(vector);
			clingingToPlayer.carryWeight = Mathf.Max(clingingToPlayer.carryWeight - 0.03f, 1f);
			StartOfRound.Instance.SendChangedWeightEvent();
			clingingToPlayer.enemiesOnPerson = Mathf.Max(clingingToPlayer.enemiesOnPerson - 1, 0);
			clingingToPlayer = null;
		}
		if (IsOwner)
		{
			agent.enabled = true;
			SwitchToBehaviourState(0);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopClingingServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(3381700831u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 3381700831u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				StopClingingOnLocalClient(isMainSnake: true);
			}
			StopClingingClientRpc(playerId);
		}
	}

	[ClientRpc]
	public void StopClingingClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3098650349u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 3098650349u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer && playerId != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				StopClingingOnLocalClient(isMainSnake: true);
			}
		}
	}

	[ClientRpc]
	public void StopLeapClientRpc(Vector3 landingPoint)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2302911734u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in landingPoint);
			__endSendClientRpc(ref bufferWriter, 2302911734u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer && leaping)
			{
				StopLeapOnLocalClient(landOnGround: true, landingPoint);
			}
		}
	}

	[ClientRpc]
	public void SetFlappingClientRpc(bool setFlapping)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1135936035u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setFlapping, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1135936035u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetFlappingLocalClient(setFlapping, isMainSnake: true);
			}
		}
	}

	private void SetFlappingLocalClient(bool setFlapping, bool isMainSnake = false)
	{
		if (flapping != setFlapping)
		{
			flapping = setFlapping;
			creatureAnimator.SetBool("Flapping", setFlapping);
		}
		if (flapping)
		{
			if (UnityEngine.Random.Range(0, 100) < 50)
			{
				flappingAudio.clip = enemyType.audioClips[5];
			}
			else
			{
				flappingAudio.clip = enemyType.audioClips[4];
			}
			flappingAudio.volume = 0.85f;
			flappingAudio.pitch = UnityEngine.Random.Range(0.85f, 1.1f);
			flappingAudio.Play();
		}
		else
		{
			flappingAudio.Stop();
		}
		if (!isMainSnake)
		{
			return;
		}
		if (setFlapping && clingingToPlayer != null && clingingToPlayer.enemiesOnPerson >= 2)
		{
			StartLiftingClungPlayer();
		}
		else
		{
			if (clingingToPlayer != null && clingingToPlayer.jetpackControls)
			{
				clingingToPlayer.disablingJetpackControls = true;
			}
			activatedFlight = false;
		}
		FlowerSnakeEnemy[] array = UnityEngine.Object.FindObjectsByType<FlowerSnakeEnemy>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].clingingToPlayer == clingingToPlayer)
			{
				array[i].SetFlappingLocalClient(setFlapping);
			}
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead)
		{
			return;
		}
		if (daytimeEnemyLeaving)
		{
			agent.speed = 0f;
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			movingTowardsTargetPlayer = false;
			if (Time.realtimeSinceStartup - timeOfLastCling < 15f)
			{
				if (snakeRoam.inProgress)
				{
					StopSearch(snakeRoam);
				}
				if (!choseFarawayNode)
				{
					if (SetDestinationToPosition(ChooseFarthestNodeFromPosition(transform.position).transform.position, checkForPath: true))
					{
						choseFarawayNode = true;
					}
					else
					{
						agent.enabled = false;
						transform.position = ChooseClosestNodeToPosition(transform.position).position;
						agent.enabled = true;
					}
				}
				agent.speed = 7f;
				break;
			}
			if (!snakeRoam.inProgress)
			{
				StartSearch(transform.position, snakeRoam);
			}
			agent.speed = 4.5f;
			PlayerControllerB playerControllerB = null;
			int allPlayersInLineOfSightNonAlloc2 = GetAllPlayersInLineOfSightNonAlloc(110f, 25, eye, 3f);
			if (allPlayersInLineOfSightNonAlloc2 <= 0)
			{
				break;
			}
			for (int j = 0; j < allPlayersInLineOfSightNonAlloc2; j++)
			{
				if (RoundManager.Instance.tempPlayersArray[j].enemiesOnPerson > 0)
				{
					playerControllerB = RoundManager.Instance.tempPlayersArray[j];
					break;
				}
				if (!RoundManager.Instance.tempPlayersArray[j].inAnimationWithEnemy)
				{
					playerControllerB = RoundManager.Instance.tempPlayersArray[j];
				}
			}
			if (playerControllerB != null)
			{
				SetMovingTowardsTargetPlayer(playerControllerB);
				timeSinceSeeingTarget = 0f;
				SwitchToBehaviourState(1);
			}
			break;
		}
		case 1:
		{
			if (targetPlayer == null)
			{
				SwitchToBehaviourState(0);
			}
			choseFarawayNode = false;
			if (snakeRoam.inProgress)
			{
				StopSearch(snakeRoam);
			}
			if (leaping || fallingFromLeap)
			{
				break;
			}
			if (targetPlayer.enemiesOnPerson == 0)
			{
				PlayerControllerB playerControllerB = null;
				int allPlayersInLineOfSightNonAlloc = GetAllPlayersInLineOfSightNonAlloc(110f, 25, eye, 3f);
				if (allPlayersInLineOfSightNonAlloc > 0)
				{
					for (int i = 0; i < allPlayersInLineOfSightNonAlloc; i++)
					{
						if (RoundManager.Instance.tempPlayersArray[i].enemiesOnPerson > 0)
						{
							playerControllerB = RoundManager.Instance.tempPlayersArray[i];
							break;
						}
					}
					if (playerControllerB != null)
					{
						SetMovingTowardsTargetPlayer(playerControllerB);
					}
				}
			}
			SetMovingTowardsTargetPlayer(targetPlayer);
			agent.speed = 6f;
			bool flag = CheckLineOfSightForPosition(targetPlayer.gameplayCamera.transform.position, 100f, 30, 5f);
			if (flag)
			{
				timeSinceSeeingTarget = 0f;
			}
			else
			{
				timeSinceSeeingTarget += AIIntervalTime;
			}
			float num = Vector3.Distance(targetPlayer.transform.position, transform.position);
			if (num > 35f || timeSinceSeeingTarget > 3f)
			{
				SwitchToBehaviourState(0);
			}
			else if (((num < UnityEngine.Random.Range(12f, 22f)) & flag) && Time.realtimeSinceStartup - timeOfLastLeap > UnityEngine.Random.Range(0.25f, 1.1f))
			{
				Vector3 vector = targetPlayer.transform.position - transform.position;
				vector += UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0.05f, 0.15f);
				vector.y = Mathf.Clamp(vector.y, -16f, 16f);
				vector = Vector3.Normalize(vector * 1000f);
				StartLeapOnLocalClient(vector);
				StartLeapClientRpc(vector);
			}
			break;
		}
		}
	}

	public void OnEnable()
	{
		StartOfRound.Instance.LocalPlayerDamagedEvent.AddListener(LocalPlayerDamaged);
	}

	public void OnDisable()
	{
		StartOfRound.Instance.LocalPlayerDamagedEvent.RemoveListener(LocalPlayerDamaged);
		if (clingingToPlayer != null)
		{
			StopClingingOnLocalClient(clingPosition == 4);
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (IsOwner && !daytimeEnemyLeaving)
		{
			KillEnemyOnOwnerClient();
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		creatureSFX.Stop();
		flappingAudio.Stop();
		creatureVoice.Stop();
		if (clingingToPlayer != null)
		{
			StopClingingOnLocalClient(clingPosition == 4);
		}
		if (leaping && IsServer)
		{
			StopLeapOnLocalClient(landOnGround: true);
		}
	}

	public override void DaytimeEnemyLeave()
	{
		base.DaytimeEnemyLeave();
		if (IsServer && stunNormalizedTimer <= 0f && !isEnemyDead)
		{
			SetEnemyLeavingClientRpc();
			StartCoroutine(flyAwayThenDespawn());
		}
	}

	private IEnumerator flyAwayThenDespawn()
	{
		yield return new WaitForSeconds(4f);
		if (IsOwner)
		{
			KillEnemyOnOwnerClient(overrideDestroy: true);
		}
	}

	[ClientRpc]
	public void SetEnemyLeavingClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(4240465152u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 4240465152u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				creatureAnimator.SetBool("leaving", value: true);
			}
		}
	}

	private void LocalPlayerDamaged()
	{
		if (clingingToPlayer == GameNetworkManager.Instance.localPlayerController && clingPosition == 4)
		{
			StopClingingOnLocalClient(isMainSnake: true);
			StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc]
	public void StartFlyingServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1804766282u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1804766282u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			StartFlyingClientRpc();
		}
	}

	[ClientRpc]
	public void StartFlyingClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2596148101u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 2596148101u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!(clingingToPlayer == GameNetworkManager.Instance.localPlayerController) && !(clingingToPlayer == null))
			{
				StartLiftingClungPlayer();
			}
		}
	}

	private void StartLiftingClungPlayer()
	{
		if (!(clingingToPlayer == null))
		{
			clingingToPlayer.jetpackTurnCompass.rotation = clingingToPlayer.transform.rotation;
			clingingToPlayer.disablingJetpackControls = false;
			clingingToPlayer.jetpackControls = true;
			activatedFlight = true;
			clingingToPlayer.syncFullRotation = clingingToPlayer.transform.eulerAngles;
			clingingToPlayer.maxJetpackAngle = 60f;
			clingingToPlayer.jetpackRandomIntensity = 120f;
		}
	}

	[ClientRpc]
	public void MakeChuckleClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1473701276u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1473701276u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (clingingToPlayer != null)
			{
				isInsidePlayerShip = clingingToPlayer.isInHangarShipRoom;
			}
			RoundManager.PlayRandomClip(creatureVoice, enemyType.audioClips, randomize: true, 1f, 0, 5);
			bool noiseIsInsideClosedShip = (isInsidePlayerShip || (clingingToPlayer != null && clingingToPlayer.isInHangarShipRoom)) && StartOfRound.Instance.hangarDoorsClosed;
			RoundManager.Instance.PlayAudibleNoise(creatureVoice.transform.position, 10f, 0.7f, 0, noiseIsInsideClosedShip);
		}
	}

	private void MainSnakeActAsConductor()
	{
		clingToPlayerTimer -= Time.deltaTime;
		if (clingingToPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			if ((clingingToPlayer.isInElevator && StartOfRound.Instance.shipIsLeaving) || clingingToPlayer.inAnimationWithEnemy != null || clingToPlayerTimer <= 0f || daytimeEnemyLeaving)
			{
				StopClingingOnLocalClient(isMainSnake: true);
				StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
				return;
			}
			if (activatedFlight)
			{
				flightPower = Mathf.Clamp(flightPower + Time.deltaTime * 1.7f, 12f, (float)clingingToPlayer.enemiesOnPerson * 4f);
			}
			else
			{
				flightPower = Mathf.Clamp(flightPower - Time.deltaTime * 50f, 0f, 1000f);
				if (clingingToPlayer.thisController.isGrounded)
				{
					flightPower = 0f;
				}
			}
			forces = Vector3.Lerp(forces, Vector3.ClampMagnitude(clingingToPlayer.transform.up * flightPower, 400f), Time.deltaTime);
			if (!clingingToPlayer.jetpackControls)
			{
				forces = Vector3.zero;
			}
			clingingToPlayer.externalForces += forces;
		}
		else if (IsServer && !clingingToPlayer.isPlayerControlled && !clingingToPlayer.isPlayerDead)
		{
			clingPosition = 0;
			StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
		if (!IsServer)
		{
			return;
		}
		if (!flapping)
		{
			clingingPlayerFlapInterval = Mathf.Min(1f, clingingPlayerFlapInterval + Time.deltaTime / flapIntervalTimeOffset);
			if (clingingPlayerFlapInterval >= 1f)
			{
				if (clingingToPlayer.enemiesOnPerson <= 1)
				{
					flapIntervalTimeOffset = UnityEngine.Random.Range(0.6f, 4f);
				}
				else if (UnityEngine.Random.Range(0, 100) < 20)
				{
					flapIntervalTimeOffset = UnityEngine.Random.Range(6f, 25f);
				}
				else
				{
					flapIntervalTimeOffset = UnityEngine.Random.Range(6f, 15f);
				}
				SetFlappingLocalClient(setFlapping: true, isMainSnake: true);
				SetFlappingClientRpc(setFlapping: true);
			}
		}
		else
		{
			clingingPlayerFlapInterval = Mathf.Max(0f, clingingPlayerFlapInterval - Time.deltaTime / flapIntervalTimeOffset);
			if (clingingPlayerFlapInterval <= 0f)
			{
				flapIntervalTimeOffset = UnityEngine.Random.Range(4f, 9f);
				SetFlappingLocalClient(setFlapping: false, isMainSnake: true);
				SetFlappingClientRpc(setFlapping: false);
			}
		}
	}

	private void DoChuckleOnInterval()
	{
		if (flapping)
		{
			chuckleInterval -= Time.deltaTime * 2f;
		}
		else
		{
			chuckleInterval -= Time.deltaTime;
		}
		if (chuckleInterval <= 0f)
		{
			MakeChuckleClientRpc();
			chuckleInterval = UnityEngine.Random.Range(2f, 15f);
		}
	}

	private void DoLeapAndDropPhysics()
	{
		if (fallingFromLeap)
		{
			fallFromLeapTimer -= Time.deltaTime;
			transform.position = Vector3.MoveTowards(transform.position, landingPoint, 5f * Time.deltaTime);
			if (fallFromLeapTimer <= 0f || transform.position == landingPoint)
			{
				if (IsOwner)
				{
					agent.enabled = true;
				}
				inSpecialAnimation = false;
				fallingFromLeap = false;
			}
		}
		if (!leaping || !(clingingToPlayer == null))
		{
			return;
		}
		if (hitWallInLeap)
		{
			leapTime += Time.deltaTime * 2f / leapTimeMultiplier;
		}
		else
		{
			leapTime += Time.deltaTime / leapTimeMultiplier;
		}
		if (leapTime > 1f)
		{
			StopLeapOnLocalClient(landOnGround: true);
			return;
		}
		bool flag = false;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, 50f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			flag = true;
			leapVerticalPosition = Mathf.Lerp(leapVerticalPosition, Mathf.Lerp(startLeapPosition.y + leapVerticalMultiplier, hit.point.y + 0.1f, leapTime), Time.deltaTime * 12f);
		}
		else
		{
			leapVerticalPosition = Mathf.Lerp(leapVerticalPosition, startLeapPosition.y + leapVerticalCurve.Evaluate(leapTime) * leapVerticalMultiplier, 4f * Time.deltaTime);
		}
		Vector3 position = transform.position;
		position.y = leapVerticalPosition;
		if (hitWallInLeap)
		{
			return;
		}
		position += leapDirection * leapSpeedMultiplier * Time.deltaTime;
		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(leapDirection, Vector3.up), 5f * Time.deltaTime);
		if (!flag || Physics.Linecast(transform.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			if (leapTime > 0.45f)
			{
				hitWallInLeap = true;
			}
		}
		else
		{
			transform.position = position;
		}
	}

	public override void Update()
	{
		base.Update();
		if (daytimeEnemyLeaving)
		{
			if (stunNormalizedTimer < 0f && !isEnemyDead)
			{
				creatureAnimator.SetBool("leaving", value: true);
			}
			if (clingingToPlayer == GameNetworkManager.Instance.localPlayerController)
			{
				StopClingingOnLocalClient(isMainSnake: true);
				StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
		else if (isEnemyDead || StartOfRound.Instance.livingPlayers == 0)
		{
			if (fallingFromLeap)
			{
				fallFromLeapTimer -= Time.deltaTime;
				transform.position = Vector3.MoveTowards(transform.position, landingPoint, 5f * Time.deltaTime);
				if (fallFromLeapTimer <= 0f || transform.position == landingPoint)
				{
					inSpecialAnimation = false;
					fallingFromLeap = false;
				}
			}
		}
		else
		{
			if (IsServer)
			{
				DoChuckleOnInterval();
			}
			if ((bool)clingingToPlayer && clingPosition == 4)
			{
				MainSnakeActAsConductor();
			}
			DoLeapAndDropPhysics();
			CalculateAnimationSpeed();
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!clingingToPlayer && !isEnemyDead && !waitingForHitPlayerRPC && !(Time.realtimeSinceStartup - collideWithPlayerInterval < 1f) && !(Time.realtimeSinceStartup - timeOfLastCling < 4f) && !daytimeEnemyLeaving)
		{
			collideWithPlayerInterval = Time.realtimeSinceStartup;
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null && !playerControllerB.inAnimationWithEnemy && (!playerControllerB.isInElevator || !StartOfRound.Instance.shipIsLeaving))
			{
				waitingForHitPlayerRPC = true;
				FSHitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void FSHitPlayerServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(963676545u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 963676545u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
		if (daytimeEnemyLeaving || clingingToPlayer != null || (bool)playerControllerB.inAnimationWithEnemy || (playerControllerB.isInElevator && StartOfRound.Instance.shipIsLeaving))
		{
			FSHitPlayerCancelClientRpc(playerId);
			return;
		}
		if (playerId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId && waitingForHitPlayerRPC)
		{
			waitingForHitPlayerRPC = false;
		}
		int num = 4;
		FlowerSnakeEnemy[] array = UnityEngine.Object.FindObjectsByType<FlowerSnakeEnemy>(FindObjectsSortMode.None);
		bool flag = false;
		for (int i = 1; i < 6; i++)
		{
			bool flag2 = false;
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].clingingToPlayer == playerControllerB && array[j].clingPosition == num)
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				flag = true;
				break;
			}
			num = Mathf.Max((num + 1) % 6, 1);
		}
		if (!flag)
		{
			FSHitPlayerCancelClientRpc(playerId);
			return;
		}
		float clingTime = UnityEngine.Random.Range(30f, 60f);
		SetClingToPlayer(playerControllerB, num, clingTime);
		ClingToPlayerClientRpc(playerId, num, clingTime);
	}

	[ClientRpc]
	public void FSHitPlayerCancelClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3147834348u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 3147834348u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (playerId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId && waitingForHitPlayerRPC)
			{
				waitingForHitPlayerRPC = false;
			}
		}
	}

	[ClientRpc]
	public void ClingToPlayerClientRpc(int playerId, int setClingPosition, float clingTime)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2927112307u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			BytePacker.WriteValueBitPacked(bufferWriter, setClingPosition);
			bufferWriter.WriteValueSafe(in clingTime, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2927112307u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!IsServer)
		{
			if (playerId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId && waitingForHitPlayerRPC)
			{
				waitingForHitPlayerRPC = false;
			}
			StopLeapOnLocalClient();
			PlayerControllerB playerToCling = StartOfRound.Instance.allPlayerScripts[playerId];
			SetClingToPlayer(playerToCling, setClingPosition, clingTime);
		}
	}

	private void SetClingToPlayer(PlayerControllerB playerToCling, int setClingPosition, float clingTime)
	{
		clingPosition = setClingPosition;
		clingingToPlayer = playerToCling;
		inSpecialAnimation = true;
		playerToCling.enemiesOnPerson++;
		creatureAnimator.SetInteger("clingType", setClingPosition);
		playerToCling.carryWeight += 0.03f;
		StartOfRound.Instance.SendChangedWeightEvent();
		if (playerToCling == GameNetworkManager.Instance.localPlayerController)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			SoundManager.Instance.misc2DAudio.volume = 1f;
			SoundManager.Instance.misc2DAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
			SoundManager.Instance.misc2DAudio.PlayOneShot(enemyType.audioClips[8]);
		}
		clingToPlayerTimer = clingTime;
	}

	public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false, bool tamperWithMeshes = false)
	{
		if (clingingToPlayer != null)
		{
			isInsidePlayerShip = clingingToPlayer.isInHangarShipRoom;
		}
		base.EnableEnemyMesh(!isInsidePlayerShip || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, overrideDoNotSet, tamperWithMeshes);
	}

	private void LateUpdate()
	{
		if ((bool)clingingToPlayer)
		{
			SetClingingAnimationPosition();
		}
	}

	private void CalculateAnimationSpeed(float maxSpeed = 1f)
	{
		float magnitude = (Vector3.ClampMagnitude(transform.position - previousPosition, 1f) / (Time.deltaTime * 2f)).magnitude;
		creatureAnimator.SetFloat("Moving", Mathf.Clamp(magnitude, 0f, 1f));
		previousPosition = transform.position;
		if (!flapping)
		{
			if (magnitude > 0.1f)
			{
				flappingAudio.volume = Mathf.Lerp(flappingAudio.volume, 1f, Time.deltaTime * 3f);
			}
			else
			{
				flappingAudio.volume = Mathf.Lerp(flappingAudio.volume, 0f, Time.deltaTime * 3f);
			}
		}
	}

	private void SetClingingAnimationPosition()
	{
		if (clingingToPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			if (clingPosition >= 4)
			{
				transform.position = clingingToPlayer.gameplayCamera.transform.position;
				transform.position += transform.up * -0.172f;
				transform.position += transform.right * -0.013f;
				transform.rotation = clingingToPlayer.gameplayCamera.transform.rotation;
			}
			else
			{
				transform.position = clingingToPlayer.upperSpineLocalPoint.transform.position;
				transform.position += transform.up * spinePositionUpOffset;
				transform.position += transform.right * spinePositionRightOffset;
				transform.rotation = clingingToPlayer.upperSpineLocalPoint.transform.rotation;
			}
		}
		else if (clingPosition >= 4)
		{
			bool flag = false;
			if (clingPosition == 4 && flapping && Vector3.Angle(clingingToPlayer.headCostumeContainer.up, Vector3.up) > 70f)
			{
				flag = true;
			}
			transform.position = clingingToPlayer.headCostumeContainer.position;
			if (!flag)
			{
				transform.rotation = clingingToPlayer.headCostumeContainer.rotation;
			}
		}
		else
		{
			transform.position = clingingToPlayer.upperSpine.position;
			transform.rotation = clingingToPlayer.upperSpine.rotation;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2859615571u, __rpc_handler_2859615571, "StartLeapClientRpc");
		__registerRpc(3381700831u, __rpc_handler_3381700831, "StopClingingServerRpc");
		__registerRpc(3098650349u, __rpc_handler_3098650349, "StopClingingClientRpc");
		__registerRpc(2302911734u, __rpc_handler_2302911734, "StopLeapClientRpc");
		__registerRpc(1135936035u, __rpc_handler_1135936035, "SetFlappingClientRpc");
		__registerRpc(4240465152u, __rpc_handler_4240465152, "SetEnemyLeavingClientRpc");
		__registerRpc(1804766282u, __rpc_handler_1804766282, "StartFlyingServerRpc");
		__registerRpc(2596148101u, __rpc_handler_2596148101, "StartFlyingClientRpc");
		__registerRpc(1473701276u, __rpc_handler_1473701276, "MakeChuckleClientRpc");
		__registerRpc(963676545u, __rpc_handler_963676545, "FSHitPlayerServerRpc");
		__registerRpc(3147834348u, __rpc_handler_3147834348, "FSHitPlayerCancelClientRpc");
		__registerRpc(2927112307u, __rpc_handler_2927112307, "ClingToPlayerClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2859615571(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).StartLeapClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3381700831(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).StopClingingServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3098650349(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).StopClingingClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2302911734(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).StopLeapClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1135936035(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).SetFlappingClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4240465152(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).SetEnemyLeavingClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1804766282(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((FlowerSnakeEnemy)target).StartFlyingServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2596148101(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).StartFlyingClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1473701276(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).MakeChuckleClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_963676545(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).FSHitPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3147834348(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).FSHitPlayerCancelClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2927112307(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlowerSnakeEnemy)target).ClingToPlayerClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "FlowerSnakeEnemy";
	}
}
