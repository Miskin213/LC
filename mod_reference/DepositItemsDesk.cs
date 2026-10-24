using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class DepositItemsDesk : NetworkBehaviour, INoiseListener
{
	public bool inGrabbingObjectsAnimation = true;

	public bool attacking;

	public bool doorOpen;

	private float noiseBehindWallVolume = 1f;

	[Space(3f)]
	public CompanyMood[] allMoodPresets;

	public CompanyMood currentMood;

	public float patienceLevel;

	public float timesHearingNoise;

	[Space(3f)]
	public float grabObjectsWaitTime = 10f;

	private float grabObjectsTimer = 10f;

	[Space(5f)]
	public NetworkObject deskObjectsContainer;

	public BoxCollider triggerCollider;

	public InteractTrigger triggerScript;

	public List<GrabbableObject> itemsOnCounter = new List<GrabbableObject>();

	public List<NetworkObject> itemsOnCounterNetworkObjects = new List<NetworkObject>();

	public int itemsOnCounterAmount;

	public Animator depositDeskAnimator;

	private NetworkObject lastObjectAddedToDesk;

	private Coroutine acceptItemsCoroutine;

	private int angerSFXindex;

	private int clientsRecievedSellItemsRPC;

	private float updateInterval;

	private System.Random CompanyLevelRandom;

	[Header("AUDIOS")]
	public AudioSource deskAudio;

	[Header("AUDIOS")]
	public AudioSource wallAudio;

	[Header("AUDIOS")]
	public AudioSource constantWallAudio;

	[Header("AUDIOS")]
	public AudioSource doorWindowAudio;

	public AudioClip[] microphoneAudios;

	public AudioClip[] rareMicrophoneAudios;

	public AudioClip doorOpenSFX;

	public AudioClip doorShutSFX;

	public AudioClip rumbleSFX;

	public AudioClip rewardGood;

	public AudioClip rewardBad;

	public AudioSource rewardsMusic;

	public AudioSource speakerAudio;

	[Header("Attack animations")]
	public MonsterAnimation[] monsterAnimations;

	public float killAnimationTimer;

	public float timeSinceAttacking;

	public int playersKilled;

	private float timeSinceMakingWarningNoise;

	private float waitingWithDoorOpenTimer;

	private float timeSinceLoweringPatience;

	private bool inSellingItemsAnimation;

	private void Start()
	{
		grabObjectsTimer = grabObjectsWaitTime;
		CompanyLevelRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 39);
		SetCompanyMood(TimeOfDay.Instance.currentCompanyMood);
	}

	private void SetCompanyMood(CompanyMood mood)
	{
		currentMood = mood;
		doorWindowAudio.clip = mood.insideWindowSFX;
		doorWindowAudio.Play();
		patienceLevel = mood.startingPatience;
		StartCoroutine(waitForRoundToStart(mood));
	}

	private IEnumerator waitForRoundToStart(CompanyMood mood)
	{
		yield return new WaitUntil(() => StartOfRound.Instance.shipDoorsEnabled);
		yield return null;
		if (mood.behindWallSFX != null)
		{
			constantWallAudio.clip = mood.behindWallSFX;
			constantWallAudio.Play();
		}
	}

	public void PlaceItemOnCounter(PlayerControllerB playerWhoTriggered)
	{
		if (deskObjectsContainer.GetComponentsInChildren<GrabbableObject>().Length < 150 && !inGrabbingObjectsAnimation && GameNetworkManager.Instance != null && playerWhoTriggered == GameNetworkManager.Instance.localPlayerController && !GameNetworkManager.Instance.localPlayerController.isGrabbingObjectAnimation)
		{
			Vector3 vector = RoundManager.RandomPointInBounds(triggerCollider.bounds);
			vector.y = triggerCollider.bounds.min.y;
			if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out var hitInfo, 8f, 1048640, QueryTriggerInteraction.Collide))
			{
				vector = hitInfo.point;
			}
			vector.y += playerWhoTriggered.currentlyHeldObjectServer.itemProperties.verticalOffset;
			vector = deskObjectsContainer.transform.InverseTransformPoint(vector);
			AddObjectToDeskServerRpc(playerWhoTriggered.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
			playerWhoTriggered.DiscardHeldObject(placeObject: true, deskObjectsContainer, vector, matchRotationOfParent: false);
			Debug.Log("discard held object called from deposit items desk");
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddObjectToDeskServerRpc(NetworkObjectReference grabbableObjectNetObject)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(4150038830u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in grabbableObjectNetObject, default(FastBufferWriter.ForNetworkSerializable));
			__endSendServerRpc(ref bufferWriter, 4150038830u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (grabbableObjectNetObject.TryGet(out lastObjectAddedToDesk))
		{
			if (!itemsOnCounter.Contains(lastObjectAddedToDesk.GetComponentInChildren<GrabbableObject>()))
			{
				itemsOnCounterNetworkObjects.Add(lastObjectAddedToDesk);
				itemsOnCounter.Add(lastObjectAddedToDesk.GetComponentInChildren<GrabbableObject>());
				AddObjectToDeskClientRpc(grabbableObjectNetObject);
				grabObjectsTimer = Mathf.Clamp(grabObjectsTimer + 6f, 0f, 10f);
				if (!doorOpen && (!currentMood.mustBeWokenUp || timesHearingNoise >= 5f))
				{
					OpenShutDoorClientRpc();
				}
			}
		}
		else
		{
			Debug.LogError("ServerRpc: Could not find networkobject in the object that was placed on desk.");
		}
	}

	[ClientRpc]
	public void AddObjectToDeskClientRpc(NetworkObjectReference grabbableObjectNetObject)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3889142070u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in grabbableObjectNetObject, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 3889142070u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (grabbableObjectNetObject.TryGet(out lastObjectAddedToDesk))
			{
				lastObjectAddedToDesk.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
			}
			else
			{
				Debug.LogError("ClientRpc: Could not find networkobject in the object that was placed on desk.");
			}
		}
	}

	private void Update()
	{
		if (NetworkManager.Singleton == null)
		{
			return;
		}
		UpdateEffects();
		if (attacking)
		{
			if (killAnimationTimer <= 0f)
			{
				FinishKillAnimation();
			}
			else
			{
				TimeOfDay.Instance.TimeOfDayMusic.volume = Mathf.Lerp(TimeOfDay.Instance.TimeOfDayMusic.volume, 0f, 10f * Time.deltaTime);
				killAnimationTimer -= Time.deltaTime;
			}
		}
		triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.isHoldingObject;
		GrabbableObject[] componentsInChildren = gameObject.GetComponentsInChildren<GrabbableObject>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].grabbable)
			{
				componentsInChildren[i].grabbable = false;
			}
		}
		if (!IsServer)
		{
			return;
		}
		timeSinceAttacking += Time.deltaTime;
		if (itemsOnCounter.Count > 0 && !inGrabbingObjectsAnimation && !attacking)
		{
			if (doorOpen)
			{
				if (grabObjectsTimer >= 0f)
				{
					Debug.Log($"Desk: Waiting to grab the items on the desk; {grabObjectsTimer}");
					grabObjectsTimer -= Time.deltaTime;
				}
				else
				{
					grabObjectsTimer = grabObjectsWaitTime;
					TakeItemsOffCounterOnServer();
				}
			}
		}
		else
		{
			if (!(timeSinceAttacking > 25f) || attacking || !doorOpen || itemsOnCounter.Count > 0)
			{
				return;
			}
			waitingWithDoorOpenTimer += Time.deltaTime;
			Debug.Log($"Desk: no objects on counter, waiting with door open; {waitingWithDoorOpenTimer}");
			if (waitingWithDoorOpenTimer > 8f / currentMood.irritability)
			{
				waitingWithDoorOpenTimer = 0f;
				float num = patienceLevel;
				SetPatienceServerRpc(-1f * currentMood.irritability);
				if (num - currentMood.irritability > 0f)
				{
					OpenShutDoorClientRpc(open: false);
				}
			}
		}
	}

	private void UpdateEffects()
	{
		timeSinceLoweringPatience += Time.deltaTime;
		timeSinceMakingWarningNoise += Time.deltaTime;
		if (doorOpen)
		{
			doorWindowAudio.volume = Mathf.Lerp(doorWindowAudio.volume, 1f * noiseBehindWallVolume, 3f * Time.deltaTime);
		}
		else
		{
			doorWindowAudio.volume = Mathf.Lerp(doorWindowAudio.volume, 0f, 10f * Time.deltaTime);
		}
		if (attacking || (currentMood.stopWallSFXWhenOpening && doorOpen))
		{
			constantWallAudio.volume = Mathf.Lerp(constantWallAudio.volume, 0f, 15f * Time.deltaTime);
		}
		else
		{
			constantWallAudio.volume = Mathf.Lerp(constantWallAudio.volume, 1f, Time.deltaTime);
		}
	}

	private void TakeItemsOffCounterOnServer()
	{
		inGrabbingObjectsAnimation = true;
		TakeObjectsClientRpc();
	}

	[ClientRpc]
	public void TakeObjectsClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3132539150u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 3132539150u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			inGrabbingObjectsAnimation = true;
			depositDeskAnimator.SetBool("GrabbingItems", value: true);
			deskAudio.PlayOneShot(currentMood.grabItemsSFX[UnityEngine.Random.Range(0, currentMood.grabItemsSFX.Length)]);
			GrabbableObject[] componentsInChildren = deskObjectsContainer.GetComponentsInChildren<GrabbableObject>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].ReactToSellingItemOnCounter();
			}
		}
	}

	public void SellItemsOnServer()
	{
		if (!IsServer)
		{
			return;
		}
		inSellingItemsAnimation = true;
		int num = 0;
		for (int i = 0; i < itemsOnCounter.Count; i++)
		{
			if (!itemsOnCounter[i].itemProperties.isScrap)
			{
				if (itemsOnCounter[i].itemProperties.itemId == 14)
				{
					num += itemsOnCounter[i].scrapValue;
				}
			}
			else
			{
				num += itemsOnCounter[i].scrapValue;
			}
		}
		num = (int)((float)num * StartOfRound.Instance.companyBuyingRate);
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		terminal.groupCredits += num;
		SellItemsClientRpc(num, terminal.groupCredits, itemsOnCounterAmount, StartOfRound.Instance.companyBuyingRate);
		SellAndDisplayItemProfits(num, terminal.groupCredits);
	}

	[ClientRpc]
	public void SellItemsClientRpc(int itemProfit, int newGroupCredits, int itemsSold, float buyingRate)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3628265478u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, itemProfit);
			BytePacker.WriteValueBitPacked(bufferWriter, newGroupCredits);
			BytePacker.WriteValueBitPacked(bufferWriter, itemsSold);
			bufferWriter.WriteValueSafe(in buyingRate, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3628265478u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				itemsOnCounterAmount = itemsSold;
				StartOfRound.Instance.companyBuyingRate = buyingRate;
				SellAndDisplayItemProfits(itemProfit, newGroupCredits);
			}
		}
	}

	private void SellAndDisplayItemProfits(int profit, int newGroupCredits)
	{
		UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCredits;
		StartOfRound.Instance.gameStats.scrapValueCollected += profit;
		TimeOfDay.Instance.quotaFulfilled += profit;
		GrabbableObject[] componentsInChildren = deskObjectsContainer.GetComponentsInChildren<GrabbableObject>();
		if (acceptItemsCoroutine != null)
		{
			StopCoroutine(acceptItemsCoroutine);
		}
		acceptItemsCoroutine = StartCoroutine(delayedAcceptanceOfItems(profit, componentsInChildren, newGroupCredits));
		CheckAllPlayersSoldItemsServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	public void CheckAllPlayersSoldItemsServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(1114072420u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1114072420u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		clientsRecievedSellItemsRPC++;
		if (clientsRecievedSellItemsRPC < GameNetworkManager.Instance.connectedPlayers)
		{
			return;
		}
		clientsRecievedSellItemsRPC = 0;
		for (int i = 0; i < itemsOnCounterNetworkObjects.Count; i++)
		{
			if (itemsOnCounterNetworkObjects[i].IsSpawned)
			{
				itemsOnCounterNetworkObjects[i].Despawn();
			}
		}
		itemsOnCounterNetworkObjects.Clear();
		itemsOnCounter.Clear();
		FinishSellingItemsClientRpc();
	}

	[ClientRpc]
	public void FinishSellingItemsClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2469293577u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 2469293577u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				depositDeskAnimator.SetBool("GrabbingItems", value: false);
				inGrabbingObjectsAnimation = false;
			}
		}
	}

	private IEnumerator delayedAcceptanceOfItems(int profit, GrabbableObject[] objectsOnDesk, int newGroupCredits)
	{
		yield return new WaitUntil(() => !inGrabbingObjectsAnimation);
		noiseBehindWallVolume = 0.3f;
		yield return new WaitForSeconds(currentMood.judgementSpeed);
		if ((float)(profit / Mathf.Max(objectsOnDesk.Length, 1)) <= 3f && patienceLevel <= 2f)
		{
			System.Random random = new System.Random(objectsOnDesk.Length + newGroupCredits);
			if (!attacking && random.Next(0, 100) < 30)
			{
				Attack();
				yield return new WaitUntil(() => !attacking);
				yield return new WaitForSeconds(2f);
			}
		}
		else
		{
			patienceLevel += 3f;
		}
		OpenShutDoor(open: false);
		yield return new WaitForSeconds(0.5f);
		noiseBehindWallVolume = 1f;
		HUDManager.Instance.DisplayCreditsEarning(profit, objectsOnDesk, newGroupCredits);
		PlayRewardEffects(profit);
		yield return new WaitForSeconds(1.25f);
		MicrophoneSpeak();
		inSellingItemsAnimation = false;
		itemsOnCounterAmount = 0;
	}

	private void PlayRewardEffects(int profit)
	{
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
		if ((float)profit < (float)terminal.groupCredits / 4f)
		{
			rewardsMusic.PlayOneShot(rewardBad);
		}
		else
		{
			rewardsMusic.PlayOneShot(rewardGood);
		}
	}

	private void MicrophoneSpeak()
	{
		AudioClip clip = ((!(CompanyLevelRandom.NextDouble() < 0.029999999329447746)) ? microphoneAudios[CompanyLevelRandom.Next(0, microphoneAudios.Length)] : rareMicrophoneAudios[CompanyLevelRandom.Next(0, rareMicrophoneAudios.Length)]);
		speakerAudio.PlayOneShot(clip, 1f);
	}

	[ServerRpc(RequireOwnership = false)]
	public void AttackPlayersServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(3230280218u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 3230280218u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!attacking && !inGrabbingObjectsAnimation)
			{
				attacking = true;
				AttackPlayersClientRpc();
			}
		}
	}

	[ClientRpc]
	public void AttackPlayersClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3277367259u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3277367259u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				Attack();
			}
		}
	}

	public void Attack()
	{
		attacking = true;
		timeSinceAttacking = 0f;
		patienceLevel += 6f;
		if (!doorOpen)
		{
			OpenShutDoor(open: true);
		}
		for (int i = 0; i < monsterAnimations.Length; i++)
		{
			if (currentMood.enableMonsterAnimationIndex == null)
			{
				Debug.Log("Current company monster mood has no monster animations to enable.");
				attacking = false;
				return;
			}
			if (i == currentMood.enableMonsterAnimationIndex[i])
			{
				monsterAnimations[i].monsterAnimator.SetBool("visible", value: true);
			}
		}
		switch (currentMood.manifestation)
		{
		case CompanyMonster.Tentacles:
			Debug.Log("Tentacles appear");
			killAnimationTimer = 3f;
			break;
		case CompanyMonster.Tongue:
			Debug.Log("Giant tongue appears");
			killAnimationTimer = 2f;
			break;
		case CompanyMonster.GiantHand:
			Debug.Log("Giant hand appears and searches");
			killAnimationTimer = 3f;
			break;
		}
		MakeLoudNoise(2);
	}

	public void CollisionDetect(int monsterAnimationID)
	{
		if (attacking && !monsterAnimations[monsterAnimationID].animatorCollidedOnClient)
		{
			monsterAnimations[monsterAnimationID].animatorCollidedOnClient = true;
			if (IsServer)
			{
				ConfirmAnimationGrabPlayerClientRpc(monsterAnimationID, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			else
			{
				CheckAnimationGrabPlayerServerRpc(monsterAnimationID, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			switch (currentMood.manifestation)
			{
			case CompanyMonster.Tentacles:
				Debug.Log("Tentacle collision");
				break;
			case CompanyMonster.Tongue:
				Debug.Log("Tongue collision");
				break;
			case CompanyMonster.GiantHand:
				Debug.Log("Hand collision");
				break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CheckAnimationGrabPlayerServerRpc(int monsterAnimationID, int playerID)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(1392297385u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, monsterAnimationID);
			BytePacker.WriteValueBitPacked(bufferWriter, playerID);
			__endSendServerRpc(ref bufferWriter, 1392297385u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!monsterAnimations[monsterAnimationID].animatorCollidedOnClient)
			{
				monsterAnimations[monsterAnimationID].animatorCollidedOnClient = true;
				ConfirmAnimationGrabPlayerClientRpc(monsterAnimationID, playerID);
			}
		}
	}

	[ClientRpc]
	public void ConfirmAnimationGrabPlayerClientRpc(int monsterAnimationID, int playerID)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3034180067u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, monsterAnimationID);
				BytePacker.WriteValueBitPacked(bufferWriter, playerID);
				__endSendClientRpc(ref bufferWriter, 3034180067u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				monsterAnimations[monsterAnimationID].animatorCollidedOnClient = true;
				StartCoroutine(AnimationGrabPlayer(monsterAnimationID, playerID));
			}
		}
	}

	private IEnumerator AnimationGrabPlayer(int monsterAnimationID, int playerID)
	{
		Animator monsterAnimator = monsterAnimations[monsterAnimationID].monsterAnimator;
		Transform monsterAnimatorGrabTarget = monsterAnimations[monsterAnimationID].monsterAnimatorGrabTarget;
		PlayerControllerB playerDying = StartOfRound.Instance.allPlayerScripts[playerID];
		monsterAnimator.SetBool("grabbingPlayer", value: true);
		monsterAnimatorGrabTarget.position = playerDying.transform.position;
		yield return new WaitForSeconds(0.05f);
		if (playerDying.IsOwner)
		{
			playerDying.KillPlayer(Vector3.zero);
		}
		float startTime = Time.timeSinceLevelLoad;
		yield return new WaitUntil(() => playerDying.deadBody != null || Time.timeSinceLevelLoad - startTime > 4f);
		if (playerDying.deadBody != null)
		{
			playerDying.deadBody.attachedTo = monsterAnimations[monsterAnimationID].monsterAnimatorGrabPoint;
			playerDying.deadBody.attachedLimb = playerDying.deadBody.bodyParts[6];
			playerDying.deadBody.matchPositionExactly = true;
		}
		else
		{
			Debug.Log("Player body was not spawned in time for animation.");
		}
		monsterAnimator.SetBool("grabbingPlayer", value: false);
		yield return new WaitForSeconds(currentMood.grabPlayerAnimationTime);
		if (playerDying.deadBody != null)
		{
			playerDying.deadBody.attachedTo = null;
			playerDying.deadBody.attachedLimb = null;
			playerDying.deadBody.matchPositionExactly = false;
			playerDying.deadBody.gameObject.SetActive(value: false);
		}
		playersKilled++;
		if (playersKilled >= currentMood.maxPlayersToKillBeforeSatisfied)
		{
			FinishKillAnimation();
		}
	}

	public void FinishKillAnimation()
	{
		attacking = false;
		for (int i = 0; i < monsterAnimations.Length; i++)
		{
			monsterAnimations[i].animatorCollidedOnClient = false;
			monsterAnimations[i].monsterAnimator.SetBool("visible", value: false);
		}
		switch (currentMood.manifestation)
		{
		case CompanyMonster.Tentacles:
			Debug.Log("Tentacles finishing animation");
			break;
		case CompanyMonster.Tongue:
			Debug.Log("Tongue finishing animation");
			break;
		case CompanyMonster.GiantHand:
			Debug.Log("Hand finishing animation");
			break;
		}
		StartCoroutine(closeDoorAfterDelay());
	}

	private IEnumerator closeDoorAfterDelay()
	{
		yield return new WaitForSeconds(1f);
		OpenShutDoor(open: false);
	}

	void INoiseListener.DetectNoise(Vector3 noisePosition, float noiseLoudness = 0.5f, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		if (noiseID != 941 && noiseID != 2692 && !(Vector3.Distance(triggerCollider.transform.position, noisePosition) > 9f) && !(noiseLoudness <= 0.4f))
		{
			if (currentMood.mustBeWokenUp && !doorOpen)
			{
				SetTimesHeardNoiseServerRpc(currentMood.sensitivity * (noiseLoudness + 0.3f) / (float)(StartOfRound.Instance.connectedPlayersAmount + 1));
			}
			else if (currentMood.desiresSilence && timeSinceLoweringPatience > 1f)
			{
				SetPatienceServerRpc(-1f * (currentMood.irritability / 2f) * noiseLoudness);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetPatienceServerRpc(float valueChange)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(892728304u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in valueChange, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 892728304u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (inSellingItemsAnimation)
		{
			return;
		}
		patienceLevel += valueChange;
		if (patienceLevel <= 0f)
		{
			if (attacking || inGrabbingObjectsAnimation)
			{
				return;
			}
			if (UnityEngine.Random.Range(0, 100) < 50)
			{
				attacking = true;
				AttackPlayersClientRpc();
				return;
			}
			patienceLevel += 3f;
			if (itemsOnCounter.Count <= 0 && timeSinceLoweringPatience > 2f)
			{
				OpenShutDoorClientRpc(open: false);
			}
		}
		else if (valueChange < 0f && patienceLevel < 1f && timeSinceMakingWarningNoise > 1f)
		{
			MakeWarningNoiseClientRpc();
		}
	}

	[ClientRpc]
	public void MakeWarningNoiseClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(103348088u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 103348088u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				timeSinceMakingWarningNoise = 0f;
				MakeLoudNoise(1);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetTimesHeardNoiseServerRpc(float valueChange)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(745684781u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in valueChange, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 745684781u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			Debug.Log("NOISE D");
			timesHearingNoise += valueChange;
			if (timesHearingNoise >= 5f && !doorOpen)
			{
				timesHearingNoise = 0f;
				doorOpen = true;
				OpenShutDoorClientRpc();
				timeSinceLoweringPatience = 2.6f;
			}
		}
	}

	[ClientRpc]
	public void OpenShutDoorClientRpc(bool open = true)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1191125720u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in open, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1191125720u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				OpenShutDoor(open);
			}
		}
	}

	public void OpenShutDoor(bool open)
	{
		doorOpen = open;
		depositDeskAnimator.SetBool("doorOpen", open);
		if (open)
		{
			deskAudio.PlayOneShot(doorOpenSFX);
		}
		else
		{
			deskAudio.PlayOneShot(doorShutSFX);
		}
	}

	public void MakeLoudNoise(int noise)
	{
		switch (noise)
		{
		case 2:
		{
			int num = UnityEngine.Random.Range(0, currentMood.attackSFX.Length);
			deskAudio.PlayOneShot(currentMood.attackSFX[num]);
			WalkieTalkie.TransmitOneShotAudio(deskAudio, currentMood.attackSFX[num]);
			wallAudio.PlayOneShot(currentMood.wallAttackSFX);
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, deskAudio.transform.position) < 12f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
			}
			else
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
			break;
		}
		case 1:
			wallAudio.PlayOneShot(rumbleSFX);
			if (doorOpen)
			{
				deskAudio.PlayOneShot(currentMood.angerSFX[angerSFXindex]);
				angerSFXindex = (angerSFXindex + 1) % currentMood.angerSFX.Length;
			}
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			break;
		default:
			wallAudio.PlayOneShot(currentMood.noiseBehindWallSFX);
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			break;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(4150038830u, __rpc_handler_4150038830, "AddObjectToDeskServerRpc");
		__registerRpc(3889142070u, __rpc_handler_3889142070, "AddObjectToDeskClientRpc");
		__registerRpc(3132539150u, __rpc_handler_3132539150, "TakeObjectsClientRpc");
		__registerRpc(3628265478u, __rpc_handler_3628265478, "SellItemsClientRpc");
		__registerRpc(1114072420u, __rpc_handler_1114072420, "CheckAllPlayersSoldItemsServerRpc");
		__registerRpc(2469293577u, __rpc_handler_2469293577, "FinishSellingItemsClientRpc");
		__registerRpc(3230280218u, __rpc_handler_3230280218, "AttackPlayersServerRpc");
		__registerRpc(3277367259u, __rpc_handler_3277367259, "AttackPlayersClientRpc");
		__registerRpc(1392297385u, __rpc_handler_1392297385, "CheckAnimationGrabPlayerServerRpc");
		__registerRpc(3034180067u, __rpc_handler_3034180067, "ConfirmAnimationGrabPlayerClientRpc");
		__registerRpc(892728304u, __rpc_handler_892728304, "SetPatienceServerRpc");
		__registerRpc(103348088u, __rpc_handler_103348088, "MakeWarningNoiseClientRpc");
		__registerRpc(745684781u, __rpc_handler_745684781, "SetTimesHeardNoiseServerRpc");
		__registerRpc(1191125720u, __rpc_handler_1191125720, "OpenShutDoorClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_4150038830(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).AddObjectToDeskServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3889142070(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).AddObjectToDeskClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3132539150(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).TakeObjectsClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3628265478(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).SellItemsClientRpc(value, value2, value3, value4);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1114072420(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).CheckAllPlayersSoldItemsServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2469293577(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).FinishSellingItemsClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3230280218(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).AttackPlayersServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3277367259(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).AttackPlayersClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1392297385(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).CheckAnimationGrabPlayerServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3034180067(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).ConfirmAnimationGrabPlayerClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_892728304(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).SetPatienceServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_103348088(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).MakeWarningNoiseClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_745684781(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).SetTimesHeardNoiseServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1191125720(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((DepositItemsDesk)target).OpenShutDoorClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "DepositItemsDesk";
	}
}
