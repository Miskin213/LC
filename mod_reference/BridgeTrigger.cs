using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BridgeTrigger : NetworkBehaviour
{
	public float bridgeDurability = 1f;

	private PlayerControllerB playerOnBridge;

	private List<PlayerControllerB> playersOnBridge = new List<PlayerControllerB>();

	public AudioSource bridgeAudioSource;

	public AudioClip[] bridgeCreakSFX;

	public AudioClip bridgeFallSFX;

	public Animator bridgeAnimator;

	private bool hasBridgeFallen;

	public Transform bridgePhysicsPartsContainer;

	private bool giantOnBridge;

	private bool giantOnBridgeLastFrame;

	public Collider[] fallenBridgeColliders;

	public int fallType;

	public float weightCapacityAmount = 0.04f;

	public float playerCapacityAmount = 0.02f;

	public EnemyType[] giantTypes;

	private void OnEnable()
	{
		StartOfRound.Instance.playerTeleportedEvent.AddListener(RemovePlayerFromBridge);
	}

	private void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(RemovePlayerFromBridge);
	}

	private void Update()
	{
		if (hasBridgeFallen)
		{
			return;
		}
		if (giantOnBridge)
		{
			bridgeDurability -= Time.deltaTime / 4.25f;
		}
		if (playersOnBridge.Count > 0)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability - Time.deltaTime * (playerCapacityAmount * (float)(playersOnBridge.Count * playersOnBridge.Count)), 0f, 1f);
			for (int i = 0; i < playersOnBridge.Count; i++)
			{
				if (playersOnBridge[i].carryWeight > 1.1f)
				{
					bridgeDurability -= Time.deltaTime * (weightCapacityAmount * playersOnBridge[i].carryWeight);
				}
			}
		}
		else if (bridgeDurability < 1f && !giantOnBridge)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability + Time.deltaTime * 0.2f, 0f, 1f);
		}
		if (IsServer && bridgeDurability <= 0f && !hasBridgeFallen)
		{
			hasBridgeFallen = true;
			BridgeFallServerRpc();
			Debug.Log("Bridge collapsed! On server");
		}
		bridgeAnimator.SetFloat("durability", Mathf.Clamp(Mathf.Abs(bridgeDurability - 1f), 0f, 1f));
	}

	private void LateUpdate()
	{
		if (giantOnBridge)
		{
			if (giantOnBridgeLastFrame)
			{
				giantOnBridge = false;
				giantOnBridgeLastFrame = false;
			}
			else
			{
				giantOnBridgeLastFrame = true;
			}
		}
	}

	[ServerRpc]
	public void BridgeFallServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2883846656u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 2883846656u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			BridgeFallClientRpc();
		}
	}

	[ClientRpc]
	public void BridgeFallClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(123213822u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 123213822u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			hasBridgeFallen = true;
			switch (fallType)
			{
			case 0:
				bridgeAnimator.SetTrigger("Fall");
				break;
			case 2:
				bridgeAnimator.SetTrigger("FallType2");
				break;
			}
			EnableFallenBridgeColliders();
			bridgeAudioSource.PlayOneShot(bridgeFallSFX);
			float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, bridgeAudioSource.transform.position);
			if (num < 30f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
			}
			else if (num < 50f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			}
		}
	}

	private void EnableFallenBridgeColliders()
	{
		for (int i = 0; i < fallenBridgeColliders.Length; i++)
		{
			fallenBridgeColliders[i].enabled = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			if (playerOnBridge != null && !playersOnBridge.Contains(playerOnBridge))
			{
				playersOnBridge.Add(playerOnBridge);
				if (Random.Range(playersOnBridge.Count * 25, 100) > 60)
				{
					RoundManager.PlayRandomClip(bridgeAudioSource, bridgeCreakSFX);
				}
			}
		}
		else
		{
			if (!other.gameObject.CompareTag("Enemy"))
			{
				return;
			}
			EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
			if (component == null)
			{
				return;
			}
			for (int i = 0; i < giantTypes.Length; i++)
			{
				if (giantTypes[i] == component.mainScript.enemyType)
				{
					giantOnBridge = true;
					giantOnBridgeLastFrame = false;
				}
			}
		}
	}

	public void RemovePlayerFromBridge(PlayerControllerB playerOnBridge)
	{
		if (playerOnBridge != null && playersOnBridge.Contains(playerOnBridge))
		{
			playersOnBridge.Remove(playerOnBridge);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			RemovePlayerFromBridge(playerOnBridge);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2883846656u, __rpc_handler_2883846656, "BridgeFallServerRpc");
		__registerRpc(123213822u, __rpc_handler_123213822, "BridgeFallClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2883846656(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((BridgeTrigger)target).BridgeFallServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_123213822(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((BridgeTrigger)target).BridgeFallClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "BridgeTrigger";
	}
}
