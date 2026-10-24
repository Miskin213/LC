using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MouthDogAI : EnemyAI, INoiseListener, IVisibleThreat
{
	public float noiseApproximation = 14f;

	public int suspicionLevel;

	private Vector3 previousPosition;

	public DampedTransform neckDampedTransform;

	private RoundManager roundManager;

	private float AITimer;

	private List<GameObject> allAINodesWithinRange = new List<GameObject>();

	private bool hasEnteredChaseModeFully;

	private bool startedChaseModeCoroutine;

	public AudioClip screamSFX;

	public AudioClip breathingSFX;

	public AudioClip killPlayerSFX;

	private float hearNoiseCooldown;

	private bool inLunge;

	private float lungeCooldown;

	private bool inKillAnimation;

	public Transform mouthGrip;

	public bool endingLunge;

	private Ray ray;

	private RaycastHit rayHit;

	private Vector3 lastHeardNoisePosition;

	private Vector3 noisePositionGuess;

	private float lastHeardNoiseDistanceWhenHeard;

	private bool heardOtherHowl;

	private DeadBodyInfo carryingBody;

	private System.Random enemyRandom;

	private Coroutine killPlayerCoroutine;

	private const int suspicionThreshold = 5;

	private const int alertThreshold = 9;

	private const int maxSuspicionLevel = 11;

	public AISearchRoutine roamPlanet;

	private Collider debugCollider;

	private float timeSinceHittingOtherEnemy;

	private float coweringMeter;

	private bool coweringOnFloor;

	private bool coweringOnFloorDebounce;

	ThreatType IVisibleThreat.type => ThreatType.EyelessDog;

	bool IVisibleThreat.IsThreatDead()
	{
		return isEnemyDead;
	}

	GrabbableObject IVisibleThreat.GetHeldObject()
	{
		return null;
	}

	int IVisibleThreat.SendSpecialBehaviour(int id)
	{
		return 0;
	}

	int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
	{
		int num = 0;
		num = ((enemyHP >= 2) ? 5 : 3);
		if (creatureAnimator.GetBool("StartedChase"))
		{
			num += 3;
		}
		return num;
	}

	int IVisibleThreat.GetInterestLevel()
	{
		return 0;
	}

	Transform IVisibleThreat.GetThreatLookTransform()
	{
		return eye;
	}

	Transform IVisibleThreat.GetThreatTransform()
	{
		return transform;
	}

	Vector3 IVisibleThreat.GetThreatVelocity()
	{
		if (IsOwner)
		{
			return agent.velocity;
		}
		return Vector3.zero;
	}

	float IVisibleThreat.GetVisibility()
	{
		if (isEnemyDead)
		{
			return 0f;
		}
		if (creatureAnimator.GetBool("StartedChase"))
		{
			return 1f;
		}
		return 0.75f;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		_ = StartOfRound.Instance.livingPlayers;
	}

	public override void Start()
	{
		base.Start();
		roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
		useSecondaryAudiosOnAnimatedObjects = true;
		if (UnityEngine.Random.Range(0, 10) < 2)
		{
			creatureVoice.pitch = UnityEngine.Random.Range(0.6f, 1.3f);
		}
		else
		{
			creatureVoice.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		}
		enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			creatureAnimator.SetLayerWeight(1, 0f);
		}
		else
		{
			if (!ventAnimationFinished)
			{
				return;
			}
			if (stunNormalizedTimer > 0f && !isEnemyDead)
			{
				if (stunnedByPlayer != null && currentBehaviourStateIndex != 2 && IsOwner)
				{
					EnrageDogOnLocalClient(stunnedByPlayer.transform.position, Vector3.Distance(transform.position, stunnedByPlayer.transform.position));
				}
				creatureAnimator.SetLayerWeight(1, 1f);
			}
			else
			{
				creatureAnimator.SetLayerWeight(1, 0f);
			}
			hitsPhysicsObjects = currentBehaviourStateIndex >= 2;
			if (!coweringOnFloor)
			{
				if (!coweringOnFloorDebounce)
				{
					coweringOnFloorDebounce = true;
					creatureAnimator.SetBool("Cower", value: false);
				}
				if (coweringMeter >= 0f)
				{
					coweringMeter -= Time.deltaTime;
				}
			}
			else
			{
				if (coweringOnFloorDebounce)
				{
					coweringOnFloorDebounce = false;
					creatureAnimator.SetBool("Cower", value: true);
					creatureAnimator.SetTrigger("StartCowering");
				}
				if (coweringMeter < 0.7f)
				{
					coweringMeter += Time.deltaTime;
				}
			}
			hearNoiseCooldown -= Time.deltaTime;
			timeSinceHittingOtherEnemy += Time.deltaTime;
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime / 4f));
			previousPosition = transform.position;
			if (currentBehaviourStateIndex == 2 || currentBehaviourStateIndex == 3)
			{
				if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 50f, 25, 10f))
				{
					GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.4f, 0.5f);
				}
			}
			else if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 50f, 30, 5f))
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.25f, 0.3f);
			}
			switch (currentBehaviourStateIndex)
			{
			case 0:
				neckDampedTransform.weight = 1f;
				creatureAnimator.SetInteger("BehaviourState", 0);
				if (IsOwner)
				{
					agent.speed = 3.5f;
					if (stunNormalizedTimer > 0f || coweringOnFloor)
					{
						agent.speed = 0f;
					}
					if (IsOwner && !roamPlanet.inProgress)
					{
						StartSearch(transform.position, roamPlanet);
					}
				}
				break;
			case 1:
				if (hasEnteredChaseModeFully)
				{
					hasEnteredChaseModeFully = false;
					creatureVoice.Stop();
					startedChaseModeCoroutine = false;
					creatureAnimator.SetBool("StartedChase", value: false);
				}
				neckDampedTransform.weight = Mathf.Lerp(neckDampedTransform.weight, 1f, 8f * Time.deltaTime);
				creatureAnimator.SetInteger("BehaviourState", 1);
				if (!IsOwner)
				{
					break;
				}
				if (IsOwner && roamPlanet.inProgress)
				{
					StopSearch(roamPlanet);
				}
				agent.speed = 4.5f;
				if (stunNormalizedTimer > 0f || coweringOnFloor)
				{
					agent.speed = 0f;
				}
				AITimer -= Time.deltaTime;
				if (AITimer <= 0f)
				{
					AITimer = 4f;
					suspicionLevel--;
					if (suspicionLevel <= 1)
					{
						SwitchToBehaviourState(0);
					}
				}
				break;
			case 2:
				if (!hasEnteredChaseModeFully)
				{
					if (!startedChaseModeCoroutine)
					{
						startedChaseModeCoroutine = true;
						StartCoroutine(enterChaseMode());
					}
					break;
				}
				neckDampedTransform.weight = Mathf.Lerp(neckDampedTransform.weight, 0.2f, 8f * Time.deltaTime);
				creatureAnimator.SetInteger("BehaviourState", 2);
				if (!IsOwner)
				{
					break;
				}
				if (IsOwner && roamPlanet.inProgress)
				{
					StopSearch(roamPlanet);
				}
				if (!inLunge)
				{
					lungeCooldown -= Time.deltaTime;
					if (Vector3.Distance(transform.position, noisePositionGuess) < 4f && lungeCooldown <= 0f)
					{
						inLunge = true;
						EnterLunge();
						break;
					}
				}
				agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime, 13f, 18f);
				if (stunNormalizedTimer > 0f || coweringOnFloor)
				{
					agent.speed = 0f;
				}
				AITimer -= Time.deltaTime;
				if (AITimer <= 0f)
				{
					AITimer = 3f;
					suspicionLevel--;
					if (Vector3.Distance(transform.position, agent.destination) < 3f)
					{
						SearchForPreviouslyHeardSound();
					}
					if (suspicionLevel <= 8)
					{
						SwitchToBehaviourState(1);
					}
				}
				break;
			case 3:
				if (IsOwner)
				{
					agent.speed -= Time.deltaTime * 5f;
					if (!endingLunge && agent.speed < 1.5f && !inKillAnimation)
					{
						endingLunge = true;
						lungeCooldown = 0.25f;
						EndLungeServerRpc();
					}
				}
				break;
			}
		}
	}

	private void SearchForPreviouslyHeardSound()
	{
		int num = 0;
		Vector3 vector = transform.position;
		while (num < 5 && Vector3.Distance(vector, transform.position) < 4f)
		{
			num++;
			vector = roundManager.GetRandomNavMeshPositionInRadius(lastHeardNoisePosition, lastHeardNoiseDistanceWhenHeard / noiseApproximation);
		}
		SetDestinationToPosition(vector);
		noisePositionGuess = vector;
	}

	private IEnumerator enterChaseMode()
	{
		if (IsOwner)
		{
			agent.speed = 0.05f;
		}
		DropCarriedBody();
		creatureVoice.PlayOneShot(screamSFX);
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("ChaseHowl");
		}
		if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 16f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
		}
		yield return new WaitForSeconds(0.5f);
		if (!heardOtherHowl)
		{
			CallAllDogsWithHowl();
		}
		heardOtherHowl = false;
		yield return new WaitForSeconds(0.2f);
		if (!isEnemyDead)
		{
			creatureVoice.clip = breathingSFX;
			creatureVoice.Play();
			creatureAnimator.SetBool("StartedChase", value: true);
			hasEnteredChaseModeFully = true;
			creatureVoice.PlayOneShot(breathingSFX);
		}
	}

	private void CallAllDogsWithHowl()
	{
		MouthDogAI[] array = UnityEngine.Object.FindObjectsOfType<MouthDogAI>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == this))
			{
				array[i].ReactToOtherDogHowl(transform.position);
			}
		}
	}

	public void ReactToOtherDogHowl(Vector3 howlPosition)
	{
		heardOtherHowl = true;
		lastHeardNoiseDistanceWhenHeard = Vector3.Distance(transform.position, howlPosition);
		noisePositionGuess = roundManager.GetRandomNavMeshPositionInRadius(howlPosition, lastHeardNoiseDistanceWhenHeard / noiseApproximation);
		SetDestinationToPosition(noisePositionGuess);
		if (currentBehaviourStateIndex < 2)
		{
			SwitchToBehaviourStateOnLocalClient(2);
		}
		suspicionLevel = 8;
		lastHeardNoisePosition = howlPosition;
		Debug.Log($"Setting lastHeardNoisePosition to {howlPosition}");
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
		if (stunNormalizedTimer > 0f || noiseID == 675189 || noiseID == 7 || noiseID == 546 || inKillAnimation || hearNoiseCooldown >= 0f || timesNoisePlayedInOneSpot > 15)
		{
			return;
		}
		hearNoiseCooldown = 0.03f;
		float num = Vector3.Distance(transform.position, noisePosition);
		Debug.Log($"dog '{gameObject.name}': Heard noise! Distance: {num} meters");
		float num2 = 18f * noiseLoudness;
		if (Physics.Linecast(transform.position, noisePosition, 256))
		{
			noiseLoudness /= 2f;
			num2 /= 2f;
		}
		if (noiseLoudness < 0.25f)
		{
			return;
		}
		if (currentBehaviourStateIndex < 2 && num < num2)
		{
			suspicionLevel = 9;
		}
		else
		{
			suspicionLevel++;
		}
		bool fullyEnrage = false;
		if (suspicionLevel >= 9)
		{
			if (currentBehaviourStateIndex < 2)
			{
				fullyEnrage = true;
			}
		}
		else if (suspicionLevel >= 5 && currentBehaviourStateIndex == 0)
		{
			fullyEnrage = false;
		}
		AITimer = 3f;
		EnrageDogOnLocalClient(noisePosition, num, approximatePosition: true, fullyEnrage);
	}

	private void EnrageDogOnLocalClient(Vector3 targetPosition, float distanceToNoise, bool approximatePosition = true, bool fullyEnrage = false)
	{
		Debug.Log($"Mouth dog targetPos 1: {targetPosition}; distanceToNoise: {distanceToNoise}");
		if (approximatePosition)
		{
			targetPosition = roundManager.GetRandomNavMeshPositionInRadius(targetPosition, distanceToNoise / noiseApproximation);
		}
		noisePositionGuess = targetPosition;
		Debug.Log($"Mouth dog targetPos 2: {targetPosition}");
		if (fullyEnrage)
		{
			if (currentBehaviourStateIndex < 2)
			{
				SwitchToBehaviourState(2);
				hearNoiseCooldown = 1f;
				suspicionLevel = 12;
			}
			suspicionLevel = Mathf.Clamp(suspicionLevel, 0, 11);
		}
		else if (currentBehaviourStateIndex == 0)
		{
			SwitchToBehaviourState(1);
		}
		if (!IsOwner)
		{
			ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		}
		if (!inLunge)
		{
			SetDestinationToPosition(noisePositionGuess);
		}
		lastHeardNoiseDistanceWhenHeard = distanceToNoise;
		lastHeardNoisePosition = targetPosition;
		Debug.Log($"Dog lastheardnoisePosition: {lastHeardNoisePosition}");
	}

	private void EnterLunge()
	{
		if (!IsOwner)
		{
			ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		}
		SwitchToBehaviourState(3);
		endingLunge = false;
		ray = new Ray(transform.position + Vector3.up, transform.forward);
		Vector3 pos = ((!Physics.Raycast(ray, out rayHit, 17f, StartOfRound.Instance.collidersAndRoomMask)) ? ray.GetPoint(17f) : rayHit.point);
		pos = roundManager.GetNavMeshPosition(pos);
		SetDestinationToPosition(pos);
		agent.speed = 13f;
	}

	[ServerRpc(RequireOwnership = false)]
	public void EndLungeServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(43708451u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 43708451u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				EndLungeClientRpc();
			}
		}
	}

	[ClientRpc]
	public void EndLungeClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(4130373844u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 4130373844u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SwitchToBehaviourStateOnLocalClient(2);
			if (!isEnemyDead)
			{
				creatureAnimator.SetTrigger("EndLungeNoKill");
			}
			inLunge = false;
		}
	}

	private void ChaseLocalPlayer()
	{
		SwitchToBehaviourState(2);
		ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		SetDestinationToPosition(GameNetworkManager.Instance.localPlayerController.transform.position);
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		enemyHP -= force;
		if (IsOwner)
		{
			if (enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
				return;
			}
			if (inKillAnimation)
			{
				StopKillAnimationServerRpc();
			}
		}
		if (playerWhoHit != null && currentBehaviourStateIndex != 2 && IsOwner)
		{
			EnrageDogOnLocalClient(playerWhoHit.transform.position, Vector3.Distance(transform.position, playerWhoHit.transform.position));
		}
	}

	public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null)
	{
		base.OnCollideWithEnemy(other, collidedEnemy);
		if (!(collidedEnemy.enemyType == enemyType) && !(timeSinceHittingOtherEnemy < 1f))
		{
			if (currentBehaviourStateIndex == 2 && !inLunge)
			{
				transform.LookAt(other.transform.position);
				transform.localEulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
				inLunge = true;
				EnterLunge();
			}
			timeSinceHittingOtherEnemy = 0f;
			collidedEnemy.HitEnemy(2, null, playHitSFX: true);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation);
		if (!(playerControllerB != null))
		{
			return;
		}
		VehicleController vehicleController = UnityEngine.Object.FindObjectOfType<VehicleController>();
		bool num = vehicleController != null && playerControllerB.physicsParent != null && playerControllerB.physicsParent == vehicleController.transform && !vehicleController.backDoorOpen;
		bool flag = StartOfRound.Instance.hangarDoorsClosed && isInsidePlayerShip != playerControllerB.isInHangarShipRoom;
		if (num | flag)
		{
			return;
		}
		Vector3 vector = Vector3.Normalize((transform.position + Vector3.up - playerControllerB.gameplayCamera.transform.position) * 100f);
		if (Physics.Linecast(transform.position + Vector3.up + vector * 0.5f, playerControllerB.gameplayCamera.transform.position, out var hitInfo, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
		{
			if (!(hitInfo.collider == debugCollider))
			{
				debugCollider = hitInfo.collider;
			}
		}
		else if (!playerControllerB.inVehicleAnimation || !(Vector3.Distance(transform.position, playerControllerB.transform.position) > 3f))
		{
			if (currentBehaviourStateIndex == 3)
			{
				playerControllerB.inAnimationWithEnemy = this;
				KillPlayerServerRpc((int)playerControllerB.playerClientId);
			}
			else if (currentBehaviourStateIndex == 0 || currentBehaviourStateIndex == 1)
			{
				ChaseLocalPlayer();
			}
			else if (currentBehaviourStateIndex == 2 && !inLunge)
			{
				transform.LookAt(other.transform.position);
				transform.localEulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
				inLunge = true;
				EnterLunge();
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(998670557u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 998670557u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!inKillAnimation)
			{
				inKillAnimation = true;
				KillPlayerClientRpc(playerId);
			}
			else
			{
				CancelKillAnimationWithPlayerClientRpc(playerId);
			}
		}
	}

	[ClientRpc]
	public void CancelKillAnimationWithPlayerClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2798326268u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
				__endSendClientRpc(ref bufferWriter, 2798326268u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StartOfRound.Instance.allPlayerScripts[playerObjectId].inAnimationWithEnemy = null;
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
			FastBufferWriter bufferWriter = __beginSendClientRpc(2252497379u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 2252497379u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Debug.Log("Kill player rpc");
			if (killPlayerCoroutine != null)
			{
				StopCoroutine(killPlayerCoroutine);
			}
			killPlayerCoroutine = StartCoroutine(KillPlayer(playerId));
		}
	}

	private IEnumerator KillPlayer(int playerId)
	{
		if (IsOwner)
		{
			agent.speed = Mathf.Clamp(agent.speed, 2f, 0f);
		}
		Debug.Log("killing player A");
		creatureVoice.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
		creatureVoice.PlayOneShot(killPlayerSFX, 1f);
		PlayerControllerB killPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
		inKillAnimation = true;
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("EndLungeKill");
		}
		Debug.Log("killing player B");
		if (GameNetworkManager.Instance.localPlayerController == killPlayer)
		{
			killPlayer.enemyWaitingForBodyRagdoll = this;
			killPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Mauling);
		}
		float startTime = Time.timeSinceLevelLoad;
		yield return new WaitUntil(() => killPlayer.deadBody != null || Time.timeSinceLevelLoad - startTime > 2f);
		if (killPlayer.deadBody == null)
		{
			Debug.Log("Giant dog: Player body was not spawned or found within 2 seconds.");
			killPlayer.inAnimationWithEnemy = null;
			inKillAnimation = false;
			yield break;
		}
		TakeBodyInMouth(killPlayer.deadBody);
		startTime = Time.timeSinceLevelLoad;
		Quaternion rotateTo = Quaternion.Euler(new Vector3(0f, RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(transform.position + Vector3.up * 0.6f), 0f));
		Quaternion rotateFrom = transform.rotation;
		while (Time.timeSinceLevelLoad - startTime < 2f)
		{
			yield return null;
			if (IsOwner)
			{
				transform.rotation = Quaternion.RotateTowards(rotateFrom, rotateTo, 60f * Time.deltaTime);
			}
		}
		yield return new WaitForSeconds(3.01f);
		DropCarriedBody();
		suspicionLevel = 2;
		SwitchToBehaviourStateOnLocalClient(2);
		endingLunge = true;
		inKillAnimation = false;
	}

	private void StopKillAnimation()
	{
		if (killPlayerCoroutine != null)
		{
			StopCoroutine(killPlayerCoroutine);
		}
		creatureVoice.Stop();
		DropCarriedBody();
		suspicionLevel = 2;
		SwitchToBehaviourStateOnLocalClient(2);
		endingLunge = true;
		inKillAnimation = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopKillAnimationServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(19183128u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 19183128u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StopKillAnimationClientRpc();
			}
		}
	}

	[ClientRpc]
	public void StopKillAnimationClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(4189041149u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 4189041149u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StopKillAnimation();
			}
		}
	}

	private void TakeBodyInMouth(DeadBodyInfo body)
	{
		carryingBody = body;
		carryingBody.attachedTo = mouthGrip;
		carryingBody.attachedLimb = body.bodyParts[5];
		carryingBody.matchPositionExactly = true;
	}

	private void DropCarriedBody()
	{
		if (!(carryingBody == null))
		{
			carryingBody.speedMultiplier = 12f;
			carryingBody.attachedTo = null;
			carryingBody.attachedLimb = null;
			carryingBody.matchPositionExactly = false;
			carryingBody = null;
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		StopKillAnimation();
		creatureVoice.Stop();
		creatureSFX.Stop();
		base.KillEnemy(destroy);
	}

	public override void ReceiveLoudNoiseBlast(Vector3 position, float angle)
	{
		base.ReceiveLoudNoiseBlast(position, angle);
		if (angle < 30f)
		{
			coweringOnFloor = true;
		}
	}

	public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false, bool tamperWithMeshes = false)
	{
		base.EnableEnemyMesh(enable);
		ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			ParticleSystem.MainModule main = componentsInChildren[i].main;
			main.playOnAwake = enabled;
		}
	}

	public override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		if (debugEnemyAI)
		{
			Gizmos.DrawCube(noisePositionGuess, Vector3.one);
			Gizmos.DrawLine(noisePositionGuess, transform.position + Vector3.up);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(43708451u, __rpc_handler_43708451, "EndLungeServerRpc");
		__registerRpc(4130373844u, __rpc_handler_4130373844, "EndLungeClientRpc");
		__registerRpc(998670557u, __rpc_handler_998670557, "KillPlayerServerRpc");
		__registerRpc(2798326268u, __rpc_handler_2798326268, "CancelKillAnimationWithPlayerClientRpc");
		__registerRpc(2252497379u, __rpc_handler_2252497379, "KillPlayerClientRpc");
		__registerRpc(19183128u, __rpc_handler_19183128, "StopKillAnimationServerRpc");
		__registerRpc(4189041149u, __rpc_handler_4189041149, "StopKillAnimationClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_43708451(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).EndLungeServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4130373844(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).EndLungeClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_998670557(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).KillPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2798326268(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).CancelKillAnimationWithPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2252497379(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).KillPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_19183128(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).StopKillAnimationServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4189041149(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MouthDogAI)target).StopKillAnimationClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "MouthDogAI";
	}
}
