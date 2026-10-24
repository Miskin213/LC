using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ButlerEnemyAI : EnemyAI
{
	private Vector3[] lastSeenPlayerPositions;

	private bool[] seenPlayers;

	private float[] timeOfLastSeenPlayers;

	private float timeSinceSeeingMultiplePlayers;

	private float timeSinceCheckingForMultiplePlayers;

	private float timeUntilNextCheck;

	private int playersInVicinity;

	private int currentSpecialAnimation;

	private float timeSinceLastSpecialAnimation;

	private bool doingKillAnimation;

	private int previousBehaviourState = -1;

	private int playersInView;

	private Vector3 agentLocalVelocity;

	public Transform animationContainer;

	private float velX;

	private float velZ;

	private Vector3 previousPosition;

	private PlayerControllerB watchingPlayer;

	public Transform lookTarget;

	public MultiAimConstraint headLookRig;

	public Transform turnCompass;

	public Transform headLookTarget;

	private float sweepFloorTimer;

	private bool isSweeping;

	public AISearchRoutine roamAndSweepFloor;

	public AISearchRoutine hoverAroundTargetPlayer;

	public float idleMovementSpeedBase = 3.5f;

	public float timeSinceChangingItem;

	private float timeSinceHittingPlayer;

	public AudioSource ambience1;

	public AudioSource buzzingAmbience;

	public AudioSource sweepingAudio;

	public AudioClip[] footsteps;

	public AudioClip[] broomSweepSFX;

	private float timeAtLastFootstep;

	private float pingAttentionTimer;

	private int focusLevel;

	private Vector3 pingAttentionPosition;

	private float timeSincePingingAttention;

	private Coroutine checkForPlayersCoroutine;

	private bool hasPlayerInSight;

	private float timeSinceNoticingFirstPlayer;

	private bool lostPlayerInChase;

	private float loseInChaseTimer;

	private bool startedMurderMusic;

	private PlayerControllerB targetedPlayerAlonePreviously;

	private bool checkedForTargetedPlayerPosition;

	private float timeAtLastTargetPlayerSync;

	private PlayerControllerB syncedTargetPlayer;

	private bool trackingTargetPlayerDownToMurder;

	private float premeditationTimeMultiplier = 1f;

	private float timeSpentWaitingForPlayer;

	[Space(3f)]
	[Header("Death sequence")]
	private bool startedButlerDeathAnimation;

	public ParticleSystem popParticle;

	public AudioSource popAudio;

	public AudioSource popAudioFar;

	public EnemyType butlerBeesEnemyType;

	private float timeAtLastButlerDamage;

	public ParticleSystem stabBloodParticle;

	private float timeAtLastHeardNoise;

	private bool killedLastTarget;

	private bool startedCrimeSceneTimer;

	private float leaveCrimeSceneTimer;

	private PlayerControllerB lastMurderedTarget;

	public GameObject knifePrefab;

	public AudioSource ambience2;

	public static AudioSource murderMusicAudio;

	public static bool increaseMurderMusicVolume;

	public static float murderMusicVolume;

	public bool madlySearchingForPlayers;

	private float ambushSpeedMeter;

	private float timeSinceStealthStab;

	private float berserkModeTimer;

	private void LateUpdate()
	{
		if (ambience2 == murderMusicAudio)
		{
			if (increaseMurderMusicVolume)
			{
				increaseMurderMusicVolume = false;
			}
			else
			{
				murderMusicVolume = Mathf.Max(murderMusicVolume - Time.deltaTime * 0.4f, 0f);
			}
			if (murderMusicAudio != null)
			{
				murderMusicAudio.volume = murderMusicVolume;
			}
		}
	}

	public override void Start()
	{
		base.Start();
		lastSeenPlayerPositions = new Vector3[4];
		seenPlayers = new bool[4];
		timeOfLastSeenPlayers = new float[4];
		if (murderMusicAudio == null)
		{
			ambience2.transform.SetParent(RoundManager.Instance.spawnedScrapContainer);
			murderMusicAudio = ambience2;
		}
		if (StartOfRound.Instance.connectedPlayersAmount == 0)
		{
			enemyHP = 2;
			idleMovementSpeedBase *= 0.75f;
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		if (currentSearch.inProgress)
		{
			StopSearch(currentSearch);
		}
		ambience1.Stop();
		ambience1.volume = 0f;
		ambience2.Stop();
		ambience2.volume = 0f;
		agent.speed = 0f;
		agent.acceleration = 1000f;
		if (!startedButlerDeathAnimation)
		{
			startedButlerDeathAnimation = true;
			StartCoroutine(ButlerBlowUpAndPop());
		}
	}

	private IEnumerator ButlerBlowUpAndPop()
	{
		creatureAnimator.SetTrigger("Popping");
		creatureAnimator.SetLayerWeight(1, 0f);
		popAudio.PlayOneShot(enemyType.audioClips[3]);
		yield return new WaitForSeconds(1.1f);
		creatureAnimator.SetBool("popFinish", value: true);
		popAudio.Play();
		popAudioFar.Play();
		popParticle.Play(withChildren: true);
		float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position);
		if (num < 8f)
		{
			Landmine.SpawnExplosion(transform.position + Vector3.up * 0.15f, spawnExplosionEffect: false, 0f, 2f, 30, 80f);
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			SoundManager.Instance.earsRingingTimer = 0.8f;
		}
		else if (num < 27f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
		if (IsServer)
		{
			RoundManager.Instance.SpawnEnemyGameObject(transform.position, 0f, -1, butlerBeesEnemyType);
			Object.Instantiate(knifePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer).GetComponent<NetworkObject>().Spawn();
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		enemyHP -= force;
		if (hitID == 5)
		{
			enemyHP -= 100;
		}
		if (playerWhoHit != null)
		{
			berserkModeTimer = 8f;
		}
		if (enemyHP <= 0 && IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
		else if (playerWhoHit != null && (currentBehaviourStateIndex != 2 || playerWhoHit != targetPlayer))
		{
			PingAttention(5, 0.6f, playerWhoHit.transform.position, sync: false);
			timeAtLastButlerDamage = Time.realtimeSinceStartup;
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead || previousBehaviourState != currentBehaviourStateIndex)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (killedLastTarget && !startedCrimeSceneTimer)
			{
				Debug.Log("Starting leave crime scene timer");
				Debug.Log($"Target player: {targetPlayer.playerClientId}");
				startedCrimeSceneTimer = true;
				leaveCrimeSceneTimer = 15f;
				movingTowardsTargetPlayer = false;
				if (hoverAroundTargetPlayer.inProgress)
				{
					StopSearch(hoverAroundTargetPlayer);
				}
				if (roamAndSweepFloor.inProgress)
				{
					StopSearch(roamAndSweepFloor);
				}
				SetDestinationToPosition(ChooseFarthestNodeFromPosition(transform.position).position);
			}
			LookForChanceToMurder(2f);
			break;
		case 1:
			LookForChanceToMurder(8f);
			break;
		case 2:
		{
			if (GameNetworkManager.Instance.localPlayerController != targetPlayer || !IsOwner)
			{
				break;
			}
			if (targetPlayer.isPlayerDead && CheckLineOfSightForPosition(targetPlayer.deadBody.bodyParts[5].transform.position, 120f, 60, 2f))
			{
				if (playersInVicinity < 2 || berserkModeTimer > 0f)
				{
					PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(100f, 50, 2);
					if (playerControllerB != null)
					{
						targetPlayer = playerControllerB;
						SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId, berserkModeTimer);
						Debug.Log("State 2, changing ownership A");
						break;
					}
				}
				Debug.Log("State 2, Switching to state 0, killed target");
				SyncKilledLastTargetServerRpc((int)targetPlayer.playerClientId);
				SwitchToBehaviourState(0);
				ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				break;
			}
			if (timeSinceChangingItem < 1.7f || (pingAttentionTimer > 0f && berserkModeTimer <= 0f) || stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
				break;
			}
			agent.speed = 8f;
			creatureAnimator.SetBool("Running", value: true);
			if (lostPlayerInChase)
			{
				if (!hoverAroundTargetPlayer.inProgress)
				{
					movingTowardsTargetPlayer = false;
					StartSearch(lastSeenPlayerPositions[targetPlayer.playerClientId], hoverAroundTargetPlayer);
				}
				PlayerControllerB playerControllerB2 = CheckLineOfSightForPlayer(100f, 50, 2);
				if (playerControllerB2 == null)
				{
					loseInChaseTimer += AIIntervalTime;
					if (loseInChaseTimer > 12f)
					{
						targetedPlayerAlonePreviously = targetPlayer;
						Debug.Log("State 2, Switching to state 0, lost in chase");
						SwitchToBehaviourState(0);
						ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
					}
				}
				else if (playerControllerB2 == targetPlayer && playersInVicinity < 2)
				{
					lostPlayerInChase = false;
					loseInChaseTimer = 0f;
				}
				else if (berserkModeTimer <= 0f)
				{
					targetedPlayerAlonePreviously = targetPlayer;
					Debug.Log("State 2, Switching to state 0, found another player or multiple players 1");
					SwitchToBehaviourState(0);
					ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				}
				break;
			}
			PlayerControllerB playerControllerB3 = CheckLineOfSightForPlayer(100f, 50, 2);
			if (playerControllerB3 == null)
			{
				loseInChaseTimer += AIIntervalTime;
				if (loseInChaseTimer > 3.5f)
				{
					lostPlayerInChase = true;
				}
			}
			else
			{
				if ((playerControllerB3 != targetPlayer || playersInVicinity > 1) && berserkModeTimer <= 0f)
				{
					targetedPlayerAlonePreviously = targetPlayer;
					Debug.Log("State 2, Switching to state 0, found another player or multiple players 2");
					SwitchToBehaviourState(0);
					ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
					break;
				}
				if (playerControllerB3 == targetPlayer)
				{
					loseInChaseTimer = 0f;
				}
			}
			SetMovingTowardsTargetPlayer(targetPlayer);
			break;
		}
		}
	}

	public void LookForChanceToMurder(float waitForTime = 5f)
	{
		if (!IsOwner)
		{
			return;
		}
		if (currentBehaviourStateIndex == 1 && playersInVicinity > 1)
		{
			SwitchToBehaviourState(0);
		}
		if (playersInVicinity <= 0)
		{
			if (killedLastTarget)
			{
				leaveCrimeSceneTimer -= AIIntervalTime;
				if (leaveCrimeSceneTimer <= 0f)
				{
					killedLastTarget = false;
					startedCrimeSceneTimer = false;
					creatureAnimator.SetInteger("HeldItem", 1);
					creatureAnimator.SetBool("Running", value: false);
					SyncKilledLastTargetFalseClientRpc();
					Debug.Log("Exiting leave crime scene mode, 0");
				}
				return;
			}
			if (!roamAndSweepFloor.inProgress)
			{
				StartSearch(transform.position, roamAndSweepFloor);
				sweepFloorTimer = 5f;
			}
			hasPlayerInSight = false;
			checkedForTargetedPlayerPosition = false;
			timeSinceSeeingMultiplePlayers = 0f;
			timeSinceCheckingForMultiplePlayers = 3f;
			timeSpentWaitingForPlayer += AIIntervalTime;
			if (timeSpentWaitingForPlayer > 6f && !madlySearchingForPlayers)
			{
				madlySearchingForPlayers = true;
				roamAndSweepFloor.searchPrecision = 16f;
				SyncSearchingMadlyServerRpc(isSearching: true);
			}
			return;
		}
		if (!hasPlayerInSight)
		{
			hasPlayerInSight = true;
			timeSpentWaitingForPlayer = 0f;
			timeSinceNoticingFirstPlayer = 0f;
			ButlerNoticePlayerServerRpc();
			if (killedLastTarget)
			{
				if (playersInVicinity < 2 && !Physics.Linecast(targetPlayer.gameplayCamera.transform.position, lastMurderedTarget.deadBody.bodyParts[6].transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					killedLastTarget = false;
					startedCrimeSceneTimer = false;
					SwitchToBehaviourStateOnLocalClient(2);
					SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId);
					Debug.Log("Found a player; Caught red-handed, entering murder state");
					for (int i = 0; i < seenPlayers.Length; i++)
					{
						Debug.Log($"Seen player {i}: {seenPlayers[i]}");
					}
				}
				else
				{
					killedLastTarget = false;
					startedCrimeSceneTimer = false;
					leaveCrimeSceneTimer = 0f;
					creatureAnimator.SetInteger("HeldItem", 1);
					creatureAnimator.SetBool("Running", value: false);
					SyncKilledLastTargetFalseClientRpc();
					PingAttention(4, 0.6f, lastMurderedTarget.deadBody.bodyParts[0].transform.position);
				}
			}
			if (roamAndSweepFloor.inProgress)
			{
				StopSearch(roamAndSweepFloor);
			}
			if (!hoverAroundTargetPlayer.inProgress)
			{
				StartSearch(targetPlayer.transform.position, hoverAroundTargetPlayer);
			}
			if (currentBehaviourStateIndex == 1 && CheckLineOfSightForPlayer(120f, 50, 2) == targetPlayer && playersInVicinity < 2 && timeSpentWaitingForPlayer > 8f)
			{
				SwitchToBehaviourStateOnLocalClient(2);
				SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId);
				return;
			}
			premeditationTimeMultiplier -= AIIntervalTime * 0.04f;
			if (!checkedForTargetedPlayerPosition && Time.realtimeSinceStartup - timeOfLastSeenPlayers[targetPlayer.playerClientId] > 4f)
			{
				checkedForTargetedPlayerPosition = true;
				PingAttention(4, 0.8f, lastSeenPlayerPositions[targetPlayer.playerClientId]);
			}
		}
		if (madlySearchingForPlayers)
		{
			madlySearchingForPlayers = false;
			SyncSearchingMadlyServerRpc(isSearching: false);
			roamAndSweepFloor.searchPrecision = 10f;
		}
		if (Time.realtimeSinceStartup - timeAtLastButlerDamage < 3f)
		{
			SwitchToBehaviourStateOnLocalClient(2);
			SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId);
			return;
		}
		if (berserkModeTimer <= 0f)
		{
			ButlerEnemyAI[] array = Object.FindObjectsByType<ButlerEnemyAI>(FindObjectsSortMode.None);
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].berserkModeTimer > 2f && Vector3.Distance(array[j].transform.position, transform.position) < 15f && !Physics.Linecast(eye.position, array[j].eye.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					berserkModeTimer = array[j].berserkModeTimer;
					break;
				}
			}
		}
		if (playersInVicinity > 1 && currentBehaviourStateIndex == 1 && berserkModeTimer <= 0f)
		{
			SwitchToBehaviourState(0);
		}
		else if (trackingTargetPlayerDownToMurder || berserkModeTimer > 0f)
		{
			PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(100f, 50, 2);
			if (playerControllerB != null)
			{
				if (playerControllerB == targetPlayer)
				{
					SwitchToBehaviourStateOnLocalClient(2);
					SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId, berserkModeTimer);
				}
				else
				{
					SwitchToBehaviourState(0);
				}
			}
		}
		hoverAroundTargetPlayer.currentSearchStartPosition = targetPlayer.transform.position;
		if (timeSinceCheckingForMultiplePlayers > timeUntilNextCheck && timeSinceNoticingFirstPlayer > 1.5f)
		{
			StartCheckForPlayers();
		}
		else
		{
			timeSinceCheckingForMultiplePlayers += AIIntervalTime;
		}
		if (timeSinceSeeingMultiplePlayers > waitForTime * premeditationTimeMultiplier && !trackingTargetPlayerDownToMurder)
		{
			if (timeSinceCheckingForMultiplePlayers > 5f)
			{
				StartCheckForPlayers();
			}
			else
			{
				if (!(timeSinceCheckingForMultiplePlayers > 2f) || !(timeSinceCheckingForMultiplePlayers < 4f))
				{
					return;
				}
				if (currentBehaviourStateIndex == 0)
				{
					if (targetPlayer == targetedPlayerAlonePreviously)
					{
						SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId);
					}
					else
					{
						SwitchToBehaviourState(1);
					}
				}
				else if (currentBehaviourStateIndex == 1)
				{
					trackingTargetPlayerDownToMurder = true;
					PingAttention(4, 0.5f, lastSeenPlayerPositions[targetPlayer.playerClientId]);
				}
			}
		}
		else
		{
			timeSinceSeeingMultiplePlayers += AIIntervalTime;
		}
	}

	private void ForgetSeenPlayers()
	{
		for (int i = 0; i < timeOfLastSeenPlayers.Length; i++)
		{
			if (seenPlayers[i])
			{
				if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead)
				{
					seenPlayers[i] = false;
				}
				float num = Time.realtimeSinceStartup - timeOfLastSeenPlayers[i];
				float num2 = ((i != (int)targetPlayer.playerClientId) ? 6f : 12f);
				if (num > num2 || (Time.realtimeSinceStartup - timeOfLastSeenPlayers[i] > 2f && timeSinceCheckingForMultiplePlayers > 2f && timeSinceCheckingForMultiplePlayers < 4f))
				{
					seenPlayers[i] = false;
				}
			}
		}
		if (currentBehaviourStateIndex != 2 && targetPlayer != null && Physics.Linecast(transform.position + Vector3.up * 0.7f, targetPlayer.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && isSweeping)
		{
			isSweeping = false;
			SetSweepingAnimServerRpc(sweeping: false);
		}
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if ((!IsOwner && noiseID != 75) || isEnemyDead || Time.realtimeSinceStartup - timeAtLastHeardNoise < 3f || Vector3.Distance(noisePosition, transform.position + Vector3.up * 0.4f) < 0.75f || (targetPlayer != null && Time.realtimeSinceStartup - timeOfLastSeenPlayers[targetPlayer.playerClientId] < 7f && Vector3.Distance(noisePosition + Vector3.up * 0.4f, targetPlayer.transform.position) < 1f) || (currentBehaviourStateIndex == 2 && (Vector3.Angle(transform.forward, noisePosition - transform.position) < 60f || (!lostPlayerInChase && Vector3.Distance(targetPlayer.transform.position, noisePosition) < 2f))))
		{
			return;
		}
		float num = Vector3.Distance(noisePosition, transform.position);
		float num2 = noiseLoudness / num;
		if (Physics.Linecast(transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			num2 *= 0.5f;
		}
		if (pingAttentionTimer > 0f)
		{
			if (focusLevel >= 3)
			{
				if (num > 3f || num2 <= 0.12f)
				{
					return;
				}
			}
			else if (focusLevel == 2)
			{
				if (num > 25f || num2 <= 0.09f)
				{
					return;
				}
			}
			else if (focusLevel <= 1 && (num > 40f || num2 <= 0.06f))
			{
				return;
			}
		}
		if (!(num2 <= 0.03f))
		{
			timeAtLastHeardNoise = Time.realtimeSinceStartup;
			PingAttention(3, 0.5f, noisePosition + Vector3.up * 0.6f);
		}
	}

	public override void Update()
	{
		base.Update();
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			ambience2.volume = Mathf.Lerp(ambience2.volume, 0f, AIIntervalTime * 9f);
		}
		if (!ventAnimationFinished || isEnemyDead)
		{
			creatureAnimator.SetLayerWeight(1, 0f);
		}
		else
		{
			creatureAnimator.SetLayerWeight(1, 1f);
		}
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		timeSinceLastSpecialAnimation += Time.deltaTime;
		if (berserkModeTimer > 0f)
		{
			timeSinceChangingItem += Time.deltaTime * 6f;
		}
		else
		{
			timeSinceChangingItem += Time.deltaTime;
		}
		timeSinceHittingPlayer += Time.deltaTime;
		timeSinceNoticingFirstPlayer += Time.deltaTime;
		pingAttentionTimer -= Time.deltaTime;
		timeSincePingingAttention += Time.deltaTime;
		berserkModeTimer -= Time.deltaTime;
		CalculateAnimationDirection();
		if (IsOwner)
		{
			CheckLOS();
			ForgetSeenPlayers();
		}
		AnimateLooking();
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				if (targetPlayer != null)
				{
					Debug.Log($"Target player: {targetPlayer.playerClientId}; is dead?: {targetPlayer.isPlayerDead}");
				}
				creatureSFX.PlayOneShot(enemyType.audioClips[1]);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[1]);
				timeSinceChangingItem = 0f;
				buzzingAmbience.pitch = 1f;
				buzzingAmbience.volume = 0.6f;
				checkedForTargetedPlayerPosition = false;
				timeSinceSeeingMultiplePlayers = 0f;
				movingTowardsTargetPlayer = false;
				trackingTargetPlayerDownToMurder = false;
				addPlayerVelocityToDestination = 0f;
				agent.acceleration = 26f;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (killedLastTarget || madlySearchingForPlayers)
			{
				creatureAnimator.SetInteger("HeldItem", 0);
				creatureAnimator.SetBool("Running", value: true);
			}
			else
			{
				creatureAnimator.SetInteger("HeldItem", 1);
				creatureAnimator.SetBool("Running", value: false);
			}
			if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.75f, 70f, 30, 2f))
			{
				ambience1.volume = Mathf.Lerp(ambience1.volume, 1f, Time.deltaTime * 1.5f);
			}
			else
			{
				ambience1.volume = Mathf.Lerp(ambience1.volume, 0f, Time.deltaTime * 8f);
			}
			if (murderMusicAudio.isPlaying && murderMusicAudio.volume <= 0.01f)
			{
				murderMusicAudio.Stop();
			}
			if (!ambience1.isPlaying)
			{
				ambience1.Play();
			}
			startedMurderMusic = false;
			if (!IsOwner)
			{
				break;
			}
			SetButlerWalkSpeed();
			if (timeSinceNoticingFirstPlayer > 1f && !madlySearchingForPlayers)
			{
				if (sweepFloorTimer <= 0f)
				{
					sweepFloorTimer = Random.Range(3.5f, 8f);
					isSweeping = !isSweeping;
					SetSweepingAnimServerRpc(isSweeping);
				}
				else
				{
					sweepFloorTimer -= Time.deltaTime;
				}
			}
			break;
		case 1:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				creatureAnimator.SetInteger("HeldItem", 0);
				creatureSFX.PlayOneShot(enemyType.audioClips[1]);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[1]);
				creatureAnimator.SetBool("Sweeping", value: false);
				creatureAnimator.SetBool("Running", value: false);
				sweepingAudio.Stop();
				isSweeping = false;
				buzzingAmbience.pitch = 1.15f;
				buzzingAmbience.volume = 0.8f;
				checkedForTargetedPlayerPosition = false;
				movingTowardsTargetPlayer = false;
				startedCrimeSceneTimer = false;
				killedLastTarget = false;
				madlySearchingForPlayers = false;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (murderMusicAudio.isPlaying && murderMusicAudio.volume <= 0.01f)
			{
				murderMusicAudio.Stop();
			}
			if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.75f, 70f, 30, 2f))
			{
				ambience1.volume = Mathf.Lerp(ambience1.volume, 1f, Time.deltaTime * 0.75f);
			}
			else
			{
				ambience1.volume = Mathf.Lerp(ambience1.volume, 0f, Time.deltaTime * 8f);
			}
			SetButlerWalkSpeed();
			break;
		case 2:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				creatureAnimator.SetInteger("HeldItem", 2);
				creatureSFX.PlayOneShot(enemyType.audioClips[2]);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[2]);
				timeSinceChangingItem = 0f;
				creatureAnimator.SetBool("Sweeping", value: false);
				sweepingAudio.Stop();
				buzzingAmbience.pitch = 1.5f;
				buzzingAmbience.volume = 1f;
				if (roamAndSweepFloor.inProgress)
				{
					StopSearch(roamAndSweepFloor);
				}
				if (hoverAroundTargetPlayer.inProgress)
				{
					StopSearch(hoverAroundTargetPlayer);
				}
				ambushSpeedMeter = 1f;
				startedCrimeSceneTimer = false;
				killedLastTarget = false;
				madlySearchingForPlayers = false;
				hasPlayerInSight = false;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			addPlayerVelocityToDestination = Mathf.Lerp(addPlayerVelocityToDestination, 2f, Time.deltaTime);
			if (timeSinceChangingItem > 1.7f)
			{
				ambushSpeedMeter = Mathf.Max(ambushSpeedMeter - Time.deltaTime * 1.2f, 0f);
			}
			if (lostPlayerInChase && IsOwner)
			{
				if (startedMurderMusic && murderMusicAudio.isPlaying && murderMusicAudio.volume <= 0.01f)
				{
					murderMusicAudio.Stop();
				}
				break;
			}
			if (!startedMurderMusic)
			{
				if (GameNetworkManager.Instance.localPlayerController.isInsideFactory && GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.7f, 100f, 18, 1f))
				{
					startedMurderMusic = true;
				}
				break;
			}
			ambience1.volume = Mathf.Lerp(ambience1.volume, 0f, Time.deltaTime * 7f);
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
			{
				if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 0.7f, 100f, 18, 1f))
				{
					murderMusicVolume = Mathf.Max(murderMusicVolume, Mathf.Lerp(murderMusicVolume, 0.7f, Time.deltaTime * 3f));
				}
				else
				{
					murderMusicVolume = Mathf.Max(murderMusicVolume, Mathf.Lerp(murderMusicVolume, 0.36f, Time.deltaTime * 3f));
				}
				increaseMurderMusicVolume = true;
			}
			if (ambience1.isPlaying && ambience1.volume <= 0.01f)
			{
				ambience1.Stop();
			}
			if (!murderMusicAudio.isPlaying)
			{
				murderMusicAudio.Play();
			}
			break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncSearchingMadlyServerRpc(bool isSearching)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2190308677u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in isSearching, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 2190308677u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				madlySearchingForPlayers = isSearching;
				SyncSearchingMadlyClientRpc(isSearching);
			}
		}
	}

	[ClientRpc]
	public void SyncSearchingMadlyClientRpc(bool isSearching)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2530454374u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in isSearching, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2530454374u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				madlySearchingForPlayers = isSearching;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncKilledLastTargetServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(975656535u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendServerRpc(ref bufferWriter, 975656535u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				killedLastTarget = true;
				lastMurderedTarget = StartOfRound.Instance.allPlayerScripts[playerId];
				SyncKilledLastTargetClientRpc();
			}
		}
	}

	[ClientRpc]
	public void SyncKilledLastTargetClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(670254571u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 670254571u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				killedLastTarget = true;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncKilledLastTargetFalseServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1587139060u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 1587139060u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				killedLastTarget = false;
				SyncKilledLastTargetClientRpc();
			}
		}
	}

	[ClientRpc]
	public void SyncKilledLastTargetFalseClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(657734107u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 657734107u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				Debug.Log("Client received sync killed last target false client rpc");
				killedLastTarget = false;
			}
		}
	}

	[ServerRpc]
	public void SwitchOwnershipAndSetToStateServerRpc(int state, ulong newOwner, float berserkTimer = -1f)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3982620855u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, state);
			BytePacker.WriteValueBitPacked(bufferWriter, newOwner);
			bufferWriter.WriteValueSafe(in berserkTimer, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3982620855u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (StartOfRound.Instance.ClientPlayerList.TryGetValue(newOwner, out var value))
		{
			if (gameObject.GetComponent<NetworkObject>().OwnerClientId != newOwner)
			{
				thisNetworkObject.ChangeOwnership(newOwner);
			}
			currentOwnershipOnThisClient = value;
			targetPlayer = StartOfRound.Instance.allPlayerScripts[value];
			watchingPlayer = targetPlayer;
			if (berserkTimer != -1f)
			{
				berserkModeTimer = berserkTimer;
			}
			SwitchOwnershipAndSetToStateClientRpc(value, state, berserkModeTimer);
		}
	}

	[ClientRpc]
	public void SwitchOwnershipAndSetToStateClientRpc(int playerVal, int state, float berserkTimer)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2060562543u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerVal);
				BytePacker.WriteValueBitPacked(bufferWriter, state);
				bufferWriter.WriteValueSafe(in berserkTimer, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 2060562543u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				currentOwnershipOnThisClient = playerVal;
				SwitchToBehaviourStateOnLocalClient(state);
				targetPlayer = StartOfRound.Instance.allPlayerScripts[playerVal];
				watchingPlayer = targetPlayer;
				berserkModeTimer = berserkTimer;
			}
		}
	}

	public void SetButlerWalkSpeed()
	{
		if (!IsOwner)
		{
			return;
		}
		if (timeSinceCheckingForMultiplePlayers < 2f || timeSinceChangingItem < 2f || isSweeping || stunNormalizedTimer > 0f)
		{
			agent.speed = 0f;
		}
		else if (trackingTargetPlayerDownToMurder || creatureAnimator.GetBool("Running"))
		{
			if (currentBehaviourStateIndex == 2)
			{
				agent.speed = idleMovementSpeedBase + 5f + 4f * ambushSpeedMeter;
				agent.acceleration = 38f + 16f * ambushSpeedMeter;
			}
			agent.speed = idleMovementSpeedBase + 5f;
		}
		else
		{
			agent.speed = idleMovementSpeedBase;
		}
	}

	private void StartCheckForPlayers()
	{
		timeSinceCheckingForMultiplePlayers = 0f;
		timeUntilNextCheck = Random.Range(8f, 11f);
		TurnAndCheckForPlayers();
		Debug.Log("Butler: Checking for players");
	}

	[ServerRpc]
	public void SetButlerRunningServerRpc(bool isRunning)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3907946013u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in isRunning, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3907946013u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetButlerRunningClientRpc(isRunning);
		}
	}

	[ClientRpc]
	public void SetButlerRunningClientRpc(bool isRunning)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(4157200979u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in isRunning, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 4157200979u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				creatureAnimator.SetBool("Running", isRunning);
			}
		}
	}

	[ServerRpc]
	public void SetSweepingAnimServerRpc(bool sweeping)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2751847406u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in sweeping, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2751847406u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetSweepingAnimClientRpc(sweeping);
		}
	}

	[ClientRpc]
	public void SetSweepingAnimClientRpc(bool sweeping)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3652150845u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in sweeping, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3652150845u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				creatureAnimator.SetBool("Sweeping", sweeping);
				sweepingAudio.Play();
			}
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("MoveX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("MoveZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = transform.position;
	}

	public void PingAttention(int newFocusLevel, float timeToLook, Vector3 attentionPosition, bool sync = true)
	{
		if ((pingAttentionTimer >= 0f && newFocusLevel < focusLevel) || (currentBehaviourStateIndex == 0 && timeSincePingingAttention < 0.5f) || (currentBehaviourStateIndex == 1 && timeSincePingingAttention < 0.2f) || (currentBehaviourStateIndex == 2 && timeSincePingingAttention < 1f))
		{
			return;
		}
		if (berserkModeTimer > 0f)
		{
			if (timeSincePingingAttention < 4f)
			{
				return;
			}
			timeToLook *= 0.5f;
		}
		Debug.Log("Butler: pinged attention to position");
		Debug.DrawLine(eye.position, attentionPosition, Color.yellow, timeToLook);
		focusLevel = newFocusLevel;
		pingAttentionTimer = timeToLook;
		pingAttentionPosition = attentionPosition;
		if (sync)
		{
			PingButlerAttentionRpc(timeToLook, attentionPosition);
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	public void PingButlerAttentionRpc(float timeToLook, Vector3 attentionPosition)
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
				FastBufferWriter bufferWriter = __beginSendRpc(1997964409u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in timeToLook, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in attentionPosition);
				__endSendRpc(ref bufferWriter, 1997964409u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				pingAttentionTimer = timeToLook;
				pingAttentionPosition = attentionPosition;
			}
		}
	}

	[ServerRpc]
	public void ButlerNoticePlayerServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2544993960u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 2544993960u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			ButlerNoticePlayerClientRpc();
		}
	}

	[ClientRpc]
	public void ButlerNoticePlayerClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(515028741u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 515028741u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				timeSinceNoticingFirstPlayer = 0f;
				hasPlayerInSight = true;
				pingAttentionTimer = -1f;
			}
		}
	}

	public void TurnAndCheckForPlayers()
	{
		RoundManager.Instance.tempTransform.position = transform.position;
		float num = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(transform.position, 40f, 8);
		CheckForPlayersServerRpc(Random.Range(0.4f, 0.8f), Random.Range(0.4f, 0.8f), (int)num);
	}

	[ServerRpc]
	public void CheckForPlayersServerRpc(float timeToCheck, float timeToCheckB, int yRot)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3725839143u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in timeToCheck, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in timeToCheckB, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, yRot);
			__endSendServerRpc(ref bufferWriter, 3725839143u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			CheckForPlayersClientRpc(timeToCheck, timeToCheckB, yRot);
		}
	}

	[ClientRpc]
	public void CheckForPlayersClientRpc(float timeToCheck, float timeToCheckB, int yRot)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2363265612u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in timeToCheck, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in timeToCheckB, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, yRot);
			__endSendClientRpc(ref bufferWriter, 2363265612u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (checkForPlayersCoroutine != null)
			{
				StopCoroutine(checkForPlayersCoroutine);
			}
			checkForPlayersCoroutine = StartCoroutine(CheckForPlayersAnim(timeToCheck, timeToCheckB, yRot));
		}
	}

	private IEnumerator CheckForPlayersAnim(float timeToCheck, float timeToCheckB, int yRot)
	{
		RoundManager.Instance.tempTransform.position = transform.position;
		RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, yRot, 0f);
		PingAttention(2, 2f, transform.position + Vector3.up * 0.4f + RoundManager.Instance.tempTransform.forward * 14f, sync: false);
		yield return new WaitForSeconds(timeToCheck);
		RoundManager.Instance.tempTransform.position = transform.position;
		RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, (float)yRot + 144f * timeToCheckB, 0f);
		PingAttention(2, 2f, transform.position + Vector3.up * 0.4f + RoundManager.Instance.tempTransform.forward * 14f, sync: false);
	}

	private void AnimateLooking()
	{
		if (stunNormalizedTimer > 0f)
		{
			agent.angularSpeed = 220f;
			headLookRig.weight = Mathf.Lerp(headLookRig.weight, 0f, Time.deltaTime * 16f);
			return;
		}
		bool flag = false;
		if ((bool)watchingPlayer && currentBehaviourStateIndex != 2 && timeSinceNoticingFirstPlayer > 1f && Time.realtimeSinceStartup - timeSinceStealthStab > 3f)
		{
			flag = watchingPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 0.7f, 30f, 50);
		}
		if (pingAttentionTimer >= 0f && timeSinceNoticingFirstPlayer > 1f)
		{
			lookTarget.position = Vector3.Lerp(lookTarget.position, pingAttentionPosition, 12f * Time.deltaTime);
			flag = false;
		}
		else
		{
			if (!(watchingPlayer != null) || Physics.Linecast(transform.position + Vector3.up * 0.6f, watchingPlayer.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				agent.angularSpeed = 220f;
				headLookRig.weight = Mathf.Lerp(headLookRig.weight, 0f, Time.deltaTime * 16f);
				return;
			}
			lookTarget.position = Vector3.Lerp(lookTarget.position, watchingPlayer.gameplayCamera.transform.position, 10f * Time.deltaTime);
		}
		if (IsOwner)
		{
			if (flag)
			{
				float num = Vector3.Angle(transform.forward, Vector3.Scale(new Vector3(1f, 0f, 1f), lookTarget.position - transform.position));
				if (num < 22f)
				{
					if (velZ >= 0f)
					{
						agent.angularSpeed = 0f;
						if (Vector3.Dot(new Vector3(lookTarget.position.x, transform.position.y, lookTarget.position.z) - transform.position, transform.right) > 0f)
						{
							transform.rotation *= Quaternion.Euler(0f, -55f * Time.deltaTime, 0f);
						}
						else
						{
							transform.rotation *= Quaternion.Euler(0f, 55f * Time.deltaTime, 0f);
						}
					}
					else
					{
						agent.angularSpeed = 220f;
					}
				}
				else if (num > 30f)
				{
					agent.angularSpeed = 220f;
				}
				else
				{
					agent.angularSpeed = 25f;
				}
			}
			else if (currentBehaviourStateIndex == 2 || Vector3.Angle(transform.forward, Vector3.Scale(new Vector3(1f, 0f, 1f), lookTarget.position - transform.position)) > 15f)
			{
				agent.angularSpeed = 0f;
				turnCompass.LookAt(lookTarget);
				turnCompass.eulerAngles = new Vector3(0f, turnCompass.eulerAngles.y, 0f);
				float num2 = 3f;
				if (berserkModeTimer > 0f && timeSinceChangingItem < 3f)
				{
					num2 = 10f;
				}
				transform.rotation = Quaternion.Lerp(transform.rotation, turnCompass.rotation, num2 * Time.deltaTime);
				transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y, 0f);
			}
		}
		if (flag)
		{
			headLookRig.weight = Mathf.Lerp(headLookRig.weight, 0f, 15f * Time.deltaTime);
		}
		else
		{
			float num3 = Vector3.Angle(transform.forward, lookTarget.position - transform.position);
			if (num3 > 22f)
			{
				headLookRig.weight = Mathf.Lerp(headLookRig.weight, 1f * (Mathf.Abs(num3 - 180f) / 180f), Time.deltaTime * 11f);
			}
			else
			{
				headLookRig.weight = Mathf.Lerp(headLookRig.weight, 1f, Time.deltaTime * 11f);
			}
		}
		headLookTarget.position = Vector3.Lerp(headLookTarget.position, lookTarget.position, 8f * Time.deltaTime);
	}

	private void StartAnimation(int anim)
	{
		if (!isEnemyDead)
		{
			timeSinceLastSpecialAnimation = 0f;
			StartAnimationServerRpc(anim);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (isEnemyDead)
		{
			return;
		}
		if (currentBehaviourStateIndex != 2)
		{
			if (Time.realtimeSinceStartup - timeSinceStealthStab < 10f)
			{
				return;
			}
			timeSinceStealthStab = Time.realtimeSinceStartup;
			if (Random.Range(0, 100) < 95)
			{
				return;
			}
		}
		if (timeSinceHittingPlayer < 0.25f)
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
		if (!(playerControllerB != null))
		{
			return;
		}
		timeSinceHittingPlayer = 0f;
		if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
		{
			if (currentBehaviourStateIndex != 2)
			{
				berserkModeTimer = 3f;
			}
			playerControllerB.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Stabbing);
			StabPlayerServerRpc((int)playerControllerB.playerClientId, currentBehaviourStateIndex != 2);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StabPlayerServerRpc(int playerId, bool setBerserkMode)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(939171591u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			bufferWriter.WriteValueSafe(in setBerserkMode, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 939171591u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (setBerserkMode)
			{
				berserkModeTimer = 3f;
				targetPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
				watchingPlayer = targetPlayer;
				SwitchOwnershipAndSetToStateServerRpc(2, targetPlayer.actualClientId);
				seenPlayers[playerId] = true;
			}
			StabPlayerClientRpc(playerId, setBerserkMode);
		}
	}

	[ClientRpc]
	public void StabPlayerClientRpc(int playerId, bool setBerserkMode)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3839735893u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				bufferWriter.WriteValueSafe(in setBerserkMode, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3839735893u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				timeSinceStealthStab = Time.realtimeSinceStartup;
				creatureAnimator.SetTrigger("Stab");
				stabBloodParticle.Play(withChildren: true);
				creatureSFX.PlayOneShot(enemyType.audioClips[0]);
			}
		}
	}

	[ServerRpc]
	public void StartAnimationServerRpc(int animationId)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(581084606u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, animationId);
			__endSendServerRpc(ref bufferWriter, 581084606u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!isEnemyDead && enemyType.miscAnimations.Length > animationId && !(creatureVoice == null) && (currentSpecialAnimation == -1 || enemyType.miscAnimations[currentSpecialAnimation].priority <= enemyType.miscAnimations[animationId].priority))
			{
				StartAnimationClientRpc(animationId);
			}
		}
	}

	[ClientRpc]
	public void StartAnimationClientRpc(int animationId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2091884461u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, animationId);
			__endSendClientRpc(ref bufferWriter, 2091884461u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!isEnemyDead && enemyType.miscAnimations.Length > animationId && !(creatureVoice == null) && (currentSpecialAnimation == -1 || enemyType.miscAnimations[currentSpecialAnimation].priority <= enemyType.miscAnimations[animationId].priority))
		{
			currentSpecialAnimation = animationId;
			if (!inSpecialAnimation || doingKillAnimation)
			{
				creatureVoice.pitch = Random.Range(0.8f, 1.2f);
				creatureVoice.PlayOneShot(enemyType.miscAnimations[animationId].AnimVoiceclip, Random.Range(0.6f, 1f));
				WalkieTalkie.TransmitOneShotAudio(creatureVoice, enemyType.miscAnimations[animationId].AnimVoiceclip, 0.7f);
				creatureAnimator.ResetTrigger(enemyType.miscAnimations[animationId].AnimString);
				creatureAnimator.SetTrigger(enemyType.miscAnimations[animationId].AnimString);
			}
		}
	}

	public void CheckLOS()
	{
		int num = 0;
		float num2 = 10000f;
		int num3 = -1;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead || !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled)
			{
				seenPlayers[i] = false;
				continue;
			}
			if (CheckLineOfSightForPosition(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, 110f, 60, 2f))
			{
				num++;
				lastSeenPlayerPositions[i] = StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position;
				seenPlayers[i] = true;
				timeOfLastSeenPlayers[i] = Time.realtimeSinceStartup;
			}
			else if (seenPlayers[i])
			{
				num++;
			}
			if (seenPlayers[i])
			{
				float num4 = Vector3.Distance(eye.position, StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position);
				if (num4 < num2)
				{
					num2 = num4;
					num3 = i;
				}
			}
		}
		if (num > 1)
		{
			timeSinceSeeingMultiplePlayers = 0f;
		}
		playersInVicinity = num;
		if (currentBehaviourStateIndex == 2)
		{
			return;
		}
		if (num3 != -1)
		{
			watchingPlayer = StartOfRound.Instance.allPlayerScripts[num3];
			if (currentBehaviourStateIndex != 2)
			{
				targetPlayer = watchingPlayer;
			}
		}
		if (Time.realtimeSinceStartup - timeAtLastTargetPlayerSync > 0.25f && syncedTargetPlayer != targetPlayer)
		{
			timeAtLastTargetPlayerSync = Time.realtimeSinceStartup;
			syncedTargetPlayer = targetPlayer;
			SyncTargetServerRpc((int)targetPlayer.playerClientId);
		}
	}

	[ServerRpc]
	public void SyncTargetServerRpc(int playerId)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1143004490u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 1143004490u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SyncTargetClientRpc(playerId);
		}
	}

	[ClientRpc]
	public void SyncTargetClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1535807017u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendClientRpc(ref bufferWriter, 1535807017u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				watchingPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
				targetPlayer = watchingPlayer;
				syncedTargetPlayer = targetPlayer;
			}
		}
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		if (!(Mathf.Abs(velX + velZ) < 0.14f) && !(Time.realtimeSinceStartup - timeAtLastFootstep < 0.07f))
		{
			timeAtLastFootstep = Time.realtimeSinceStartup;
			int num = Random.Range(0, footsteps.Length);
			if (!(footsteps[num] == null))
			{
				creatureSFX.PlayOneShot(footsteps[num]);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, footsteps[num]);
			}
		}
	}

	public override void AnimationEventB()
	{
		base.AnimationEventB();
		int num = Random.Range(0, broomSweepSFX.Length);
		if (!(broomSweepSFX[num] == null))
		{
			creatureSFX.PlayOneShot(broomSweepSFX[num]);
			WalkieTalkie.TransmitOneShotAudio(creatureSFX, broomSweepSFX[num]);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2190308677u, __rpc_handler_2190308677, "SyncSearchingMadlyServerRpc");
		__registerRpc(2530454374u, __rpc_handler_2530454374, "SyncSearchingMadlyClientRpc");
		__registerRpc(975656535u, __rpc_handler_975656535, "SyncKilledLastTargetServerRpc");
		__registerRpc(670254571u, __rpc_handler_670254571, "SyncKilledLastTargetClientRpc");
		__registerRpc(1587139060u, __rpc_handler_1587139060, "SyncKilledLastTargetFalseServerRpc");
		__registerRpc(657734107u, __rpc_handler_657734107, "SyncKilledLastTargetFalseClientRpc");
		__registerRpc(3982620855u, __rpc_handler_3982620855, "SwitchOwnershipAndSetToStateServerRpc");
		__registerRpc(2060562543u, __rpc_handler_2060562543, "SwitchOwnershipAndSetToStateClientRpc");
		__registerRpc(3907946013u, __rpc_handler_3907946013, "SetButlerRunningServerRpc");
		__registerRpc(4157200979u, __rpc_handler_4157200979, "SetButlerRunningClientRpc");
		__registerRpc(2751847406u, __rpc_handler_2751847406, "SetSweepingAnimServerRpc");
		__registerRpc(3652150845u, __rpc_handler_3652150845, "SetSweepingAnimClientRpc");
		__registerRpc(1997964409u, __rpc_handler_1997964409, "PingButlerAttentionRpc");
		__registerRpc(2544993960u, __rpc_handler_2544993960, "ButlerNoticePlayerServerRpc");
		__registerRpc(515028741u, __rpc_handler_515028741, "ButlerNoticePlayerClientRpc");
		__registerRpc(3725839143u, __rpc_handler_3725839143, "CheckForPlayersServerRpc");
		__registerRpc(2363265612u, __rpc_handler_2363265612, "CheckForPlayersClientRpc");
		__registerRpc(939171591u, __rpc_handler_939171591, "StabPlayerServerRpc");
		__registerRpc(3839735893u, __rpc_handler_3839735893, "StabPlayerClientRpc");
		__registerRpc(581084606u, __rpc_handler_581084606, "StartAnimationServerRpc");
		__registerRpc(2091884461u, __rpc_handler_2091884461, "StartAnimationClientRpc");
		__registerRpc(1143004490u, __rpc_handler_1143004490, "SyncTargetServerRpc");
		__registerRpc(1535807017u, __rpc_handler_1535807017, "SyncTargetClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2190308677(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncSearchingMadlyServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2530454374(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncSearchingMadlyClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_975656535(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncKilledLastTargetServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_670254571(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncKilledLastTargetClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1587139060(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncKilledLastTargetFalseServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_657734107(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncKilledLastTargetFalseClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3982620855(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
		ByteUnpacker.ReadValueBitPacked(reader, out int value);
		ByteUnpacker.ReadValueBitPacked(reader, out ulong value2);
		reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((ButlerEnemyAI)target).SwitchOwnershipAndSetToStateServerRpc(value, value2, value3);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_2060562543(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SwitchOwnershipAndSetToStateClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3907946013(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((ButlerEnemyAI)target).SetButlerRunningServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4157200979(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SetButlerRunningClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2751847406(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((ButlerEnemyAI)target).SetSweepingAnimServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3652150845(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SetSweepingAnimClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1997964409(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).PingButlerAttentionRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2544993960(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((ButlerEnemyAI)target).ButlerNoticePlayerServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_515028741(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).ButlerNoticePlayerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3725839143(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
		reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
		reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
		ByteUnpacker.ReadValueBitPacked(reader, out int value3);
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((ButlerEnemyAI)target).CheckForPlayersServerRpc(value, value2, value3);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_2363265612(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).CheckForPlayersClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_939171591(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).StabPlayerServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3839735893(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).StabPlayerClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_581084606(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((ButlerEnemyAI)target).StartAnimationServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2091884461(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).StartAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1143004490(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((ButlerEnemyAI)target).SyncTargetServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1535807017(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ButlerEnemyAI)target).SyncTargetClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ButlerEnemyAI";
	}
}
