using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EntranceTeleport : NetworkBehaviour
{
	public bool isEntranceToBuilding;

	public Transform entrancePoint;

	public int entranceId;

	public StartOfRound playersManager;

	public int audioReverbPreset = -1;

	public EntranceTeleport exitScript;

	public AudioSource entrancePointAudio;

	public AudioClip[] doorAudios;

	private InteractTrigger triggerScript;

	private float checkForEnemiesInterval;

	private bool enemyNearLastCheck;

	private bool gotExitPoint;

	private bool checkedForFirstTime;

	public float timeAtLastUse;

	public Animator thisEntranceAnimator;

	private bool exitPointDoesntExist;

	private bool playingCreakAudio;

	private void Awake()
	{
		playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		triggerScript = gameObject.GetComponent<InteractTrigger>();
		checkForEnemiesInterval = 10f;
	}

	private void PlayCreakSFX()
	{
		AudioClip[] array = ((RoundManager.Instance.currentDungeonType != 1 || isEntranceToBuilding) ? StartOfRound.Instance.creakOpenDoorMetal : StartOfRound.Instance.creakOpenDoorWooden);
		int num = UnityEngine.Random.Range(0, array.Length);
		entrancePointAudio.clip = array[num];
		entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
		entrancePointAudio.Play();
		playingCreakAudio = true;
		if (exitScript != null && exitScript.thisEntranceAnimator != null)
		{
			array = ((RoundManager.Instance.currentDungeonType != 1 || !exitScript.isEntranceToBuilding) ? StartOfRound.Instance.creakOpenDoorMetal : StartOfRound.Instance.creakOpenDoorWooden);
			exitScript.entrancePointAudio.clip = array[num];
			exitScript.entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
			exitScript.entrancePointAudio.Play();
		}
	}

	public void StartOpeningEntrance()
	{
		if (entranceId != 0)
		{
			Debug.Log("Id is not 0");
		}
		else
		{
			if (!GetDoorAnimators() || Time.realtimeSinceStartup - timeAtLastUse < 0.5f)
			{
				return;
			}
			if (exitScript == null && (exitPointDoesntExist || !FindExitPoint()))
			{
				exitPointDoesntExist = true;
				return;
			}
			thisEntranceAnimator.SetBool("Open", value: true);
			if (!playingCreakAudio)
			{
				PlayCreakSFX();
			}
			if (exitScript.thisEntranceAnimator != null)
			{
				exitScript.thisEntranceAnimator.SetBool("Open", value: true);
			}
			SyncStartOpeningDoorRpc();
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	public void SyncStartOpeningDoorRpc()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3464572258u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3464572258u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (entranceId != 0)
		{
			Debug.Log("Id is not 0");
		}
		else
		{
			if (!GetDoorAnimators())
			{
				return;
			}
			if (exitScript == null && (exitPointDoesntExist || !FindExitPoint()))
			{
				exitPointDoesntExist = true;
				return;
			}
			thisEntranceAnimator.SetBool("Open", value: true);
			Debug.Log($"'{gameObject.name}' entrancePointAudio isPlaying: {entrancePointAudio.isPlaying}");
			if (!playingCreakAudio)
			{
				PlayCreakSFX();
			}
			if (exitScript.thisEntranceAnimator != null)
			{
				exitScript.thisEntranceAnimator.SetBool("Open", value: true);
			}
		}
	}

	public void FinishOpeningEntrance(bool playShutAudio = true)
	{
		Debug.Log($"Called finishopeningentrance ({playShutAudio})");
		if (entranceId != 0)
		{
			Debug.Log("Id is not 0");
			return;
		}
		if (!GetDoorAnimators())
		{
			Debug.Log("finishopeningentrance animator null");
			return;
		}
		if (!thisEntranceAnimator.GetBool("Open"))
		{
			if (playingCreakAudio)
			{
				entrancePointAudio.Stop();
				if (exitScript != null && exitScript.thisEntranceAnimator != null)
				{
					exitScript.entrancePointAudio.Stop();
				}
			}
			Debug.Log("Entrance teleport was not open; returning");
			return;
		}
		if (exitScript == null && (exitPointDoesntExist || !FindExitPoint()))
		{
			Debug.Log("Couldn't find exit script");
			exitPointDoesntExist = true;
			Debug.Log("End A");
			return;
		}
		thisEntranceAnimator.SetBool("Open", value: false);
		Debug.Log("'" + gameObject.name + "' STOPPING entrancePointAudio");
		entrancePointAudio.Stop();
		playingCreakAudio = false;
		if (exitScript.thisEntranceAnimator != null)
		{
			exitScript.entrancePointAudio.Stop();
		}
		if (playShutAudio)
		{
			if (Time.realtimeSinceStartup - timeAtLastUse > 0.5f)
			{
				PlayAudioAtTeleportPositions();
			}
			SyncFinishOpeningEntranceRpc();
		}
		if (exitScript.thisEntranceAnimator != null)
		{
			exitScript.thisEntranceAnimator.SetBool("Open", value: false);
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	public void SyncFinishOpeningEntranceRpc()
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
			FastBufferWriter bufferWriter = __beginSendRpc(3207210735u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 3207210735u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (entranceId != 0 || !GetDoorAnimators())
		{
			return;
		}
		if (exitScript == null && (exitPointDoesntExist || !FindExitPoint()))
		{
			exitPointDoesntExist = true;
			return;
		}
		thisEntranceAnimator.SetBool("Open", value: false);
		entrancePointAudio.Stop();
		playingCreakAudio = false;
		if (exitScript.thisEntranceAnimator != null)
		{
			exitScript.entrancePointAudio.Stop();
		}
		if (Time.realtimeSinceStartup - timeAtLastUse > 0.5f)
		{
			PlayAudioAtTeleportPositions();
		}
		if (exitScript.thisEntranceAnimator != null)
		{
			exitScript.thisEntranceAnimator.SetBool("Open", value: false);
		}
	}

	private bool GetDoorAnimators()
	{
		if (thisEntranceAnimator == null)
		{
			bool flag = false;
			if (!isEntranceToBuilding)
			{
				GameObject gameObject = GameObject.FindGameObjectWithTag("InsideEntranceDoor");
				if (gameObject != null && (bool)gameObject.GetComponent<Animator>())
				{
					thisEntranceAnimator = gameObject.GetComponent<Animator>();
					flag = true;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool FindExitPoint()
	{
		EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].isEntranceToBuilding != isEntranceToBuilding && array[i].entranceId == entranceId)
			{
				exitScript = array[i];
			}
		}
		if (exitScript == null)
		{
			return false;
		}
		return true;
	}

	public void TeleportPlayer()
	{
		bool flag = false;
		if (!FindExitPoint())
		{
			flag = true;
		}
		if (flag)
		{
			HUDManager.Instance.DisplayTip("???", "The entrance appears to be blocked.");
			return;
		}
		Transform thisPlayerBody = GameNetworkManager.Instance.localPlayerController.thisPlayerBody;
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitScript.entrancePoint.position);
		GameNetworkManager.Instance.localPlayerController.isInElevator = false;
		GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
		thisPlayerBody.eulerAngles = new Vector3(thisPlayerBody.eulerAngles.x, exitScript.entrancePoint.eulerAngles.y, thisPlayerBody.eulerAngles.z);
		FinishOpeningEntrance(playShutAudio: false);
		SetAudioPreset((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		if (!checkedForFirstTime)
		{
			checkedForFirstTime = true;
			if (RoundManager.Instance.currentDungeonType != -1 && isEntranceToBuilding && (RoundManager.Instance.currentDungeonType == 0 || RoundManager.Instance.currentDungeonType == 1 || RoundManager.Instance.currentDungeonType == 4))
			{
				try
				{
					if (!ES3.Load($"PlayedDungeonEntrance{RoundManager.Instance.currentDungeonType}", "LCGeneralSaveData", defaultValue: false))
					{
						StartCoroutine(playMusicOnDelay());
					}
				}
				catch (Exception arg)
				{
					Debug.LogError($"Error when checking PlayedDungeonEntrance with save data! : {arg}");
				}
			}
		}
		if (entranceId == 0 && isEntranceToBuilding && StartOfRound.Instance.occlusionCuller.enabled)
		{
			StartOfRound.Instance.occlusionCuller.SetToStartTile();
		}
		for (int i = 0; i < GameNetworkManager.Instance.localPlayerController.ItemSlots.Length; i++)
		{
			if (GameNetworkManager.Instance.localPlayerController.ItemSlots[i] != null)
			{
				GameNetworkManager.Instance.localPlayerController.ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		if (GameNetworkManager.Instance.localPlayerController.ItemOnlySlot != null)
		{
			GameNetworkManager.Instance.localPlayerController.ItemOnlySlot.isInFactory = isEntranceToBuilding;
		}
		timeAtLastUse = Time.realtimeSinceStartup;
		TeleportPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		GameNetworkManager.Instance.localPlayerController.isInsideFactory = isEntranceToBuilding;
	}

	private IEnumerator playMusicOnDelay()
	{
		yield return new WaitForSeconds(0.6f);
		ES3.Save($"PlayedDungeonEntrance{RoundManager.Instance.currentDungeonType}", value: true, "LCGeneralSaveData");
		HUDManager.Instance.UIAudio.PlayOneShot(RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].firstTimeAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	public void TeleportPlayerServerRpc(int playerObj)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(4279190381u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
				__endSendServerRpc(ref bufferWriter, 4279190381u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				TeleportPlayerClientRpc(playerObj);
			}
		}
	}

	[ClientRpc]
	public void TeleportPlayerClientRpc(int playerObj)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3168414823u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
			__endSendClientRpc(ref bufferWriter, 3168414823u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
		FindExitPoint();
		playersManager.allPlayerScripts[playerObj].TeleportPlayer(exitScript.entrancePoint.position, withRotation: true, exitScript.entrancePoint.eulerAngles.y);
		playersManager.allPlayerScripts[playerObj].isInElevator = false;
		playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
		FinishOpeningEntrance(playShutAudio: false);
		playersManager.allPlayerScripts[playerObj].isInsideFactory = isEntranceToBuilding;
		if (entranceId == 0 && isEntranceToBuilding && GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == playersManager.allPlayerScripts[playerObj] && StartOfRound.Instance.occlusionCuller.enabled)
		{
			StartOfRound.Instance.occlusionCuller.SetToStartTile();
		}
		for (int i = 0; i < playersManager.allPlayerScripts[playerObj].ItemSlots.Length; i++)
		{
			if (playersManager.allPlayerScripts[playerObj].ItemSlots[i] != null)
			{
				playersManager.allPlayerScripts[playerObj].ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		if (playersManager.allPlayerScripts[playerObj].ItemOnlySlot != null)
		{
			playersManager.allPlayerScripts[playerObj].ItemOnlySlot.isInFactory = isEntranceToBuilding;
		}
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript)
		{
			SetAudioPreset(playerObj);
		}
		else
		{
			PlayAudioAtTeleportPositions();
		}
		timeAtLastUse = Time.realtimeSinceStartup;
	}

	private void SetAudioPreset(int playerObj)
	{
		if (audioReverbPreset != -1)
		{
			UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[audioReverbPreset].ChangeAudioReverbForPlayer(StartOfRound.Instance.allPlayerScripts[playerObj]);
			if (entrancePointAudio != null)
			{
				PlayAudioAtTeleportPositions();
			}
		}
	}

	public void PlayAudioAtTeleportPositions()
	{
		if (StartOfRound.Instance.testRoom != null)
		{
			return;
		}
		if (entranceId == 0)
		{
			AudioClip[] shutDoorMetal;
			AudioClip[] array;
			if (RoundManager.Instance.currentDungeonType == 1)
			{
				shutDoorMetal = StartOfRound.Instance.shutDoorMetal;
				array = StartOfRound.Instance.shutDoorWooden;
			}
			else
			{
				shutDoorMetal = StartOfRound.Instance.shutDoorMetal;
				array = StartOfRound.Instance.shutDoorMetal;
			}
			if (isEntranceToBuilding)
			{
				entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
				entrancePointAudio.PlayOneShot(shutDoorMetal[UnityEngine.Random.Range(0, shutDoorMetal.Length)]);
				exitScript.entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
				exitScript.entrancePointAudio.PlayOneShot(array[UnityEngine.Random.Range(0, array.Length)]);
			}
			else
			{
				entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
				entrancePointAudio.PlayOneShot(array[UnityEngine.Random.Range(0, array.Length)]);
				exitScript.entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
				exitScript.entrancePointAudio.PlayOneShot(shutDoorMetal[UnityEngine.Random.Range(0, shutDoorMetal.Length)]);
			}
		}
		else if (doorAudios.Length != 0)
		{
			entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
			entrancePointAudio.PlayOneShot(doorAudios[UnityEngine.Random.Range(0, doorAudios.Length)]);
			exitScript.entrancePointAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
			exitScript.entrancePointAudio.PlayOneShot(doorAudios[UnityEngine.Random.Range(0, doorAudios.Length)]);
		}
	}

	private void Update()
	{
		if (triggerScript == null || !isEntranceToBuilding)
		{
			return;
		}
		if (checkForEnemiesInterval <= 0f)
		{
			if (!gotExitPoint)
			{
				if (FindExitPoint())
				{
					gotExitPoint = true;
				}
				return;
			}
			checkForEnemiesInterval = 1f;
			bool flag = false;
			for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++)
			{
				if (Vector3.Distance(RoundManager.Instance.SpawnedEnemies[i].transform.position, exitScript.entrancePoint.transform.position) < 7.7f && !RoundManager.Instance.SpawnedEnemies[i].isEnemyDead)
				{
					flag = true;
					break;
				}
			}
			if (flag && !enemyNearLastCheck)
			{
				enemyNearLastCheck = true;
				triggerScript.hoverTip = "[Near activity detected!]";
			}
			else if (enemyNearLastCheck)
			{
				enemyNearLastCheck = false;
				triggerScript.hoverTip = "Enter: [LMB]";
			}
		}
		else
		{
			checkForEnemiesInterval -= Time.deltaTime;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3464572258u, __rpc_handler_3464572258, "SyncStartOpeningDoorRpc");
		__registerRpc(3207210735u, __rpc_handler_3207210735, "SyncFinishOpeningEntranceRpc");
		__registerRpc(4279190381u, __rpc_handler_4279190381, "TeleportPlayerServerRpc");
		__registerRpc(3168414823u, __rpc_handler_3168414823, "TeleportPlayerClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3464572258(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EntranceTeleport)target).SyncStartOpeningDoorRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3207210735(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EntranceTeleport)target).SyncFinishOpeningEntranceRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4279190381(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EntranceTeleport)target).TeleportPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3168414823(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EntranceTeleport)target).TeleportPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "EntranceTeleport";
	}
}
