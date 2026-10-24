using Unity.Netcode;
using UnityEngine;

public class GlobalEffects : NetworkBehaviour
{
	private StartOfRound playersManager;

	public bool ownedByPlayer;

	public static GlobalEffects Instance { get; private set; }

	private void Awake()
	{
		if (!ownedByPlayer)
		{
			if (!(Instance == null))
			{
				Object.Destroy(gameObject);
				return;
			}
			Instance = this;
		}
		playersManager = Object.FindObjectOfType<StartOfRound>();
	}

	public void PlayAnimAndAudioServer(ServerAnimAndAudio serverAnimAndAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimAndAudioServerFromSenderObject(serverAnimAndAudio);
	}

	public void PlayAnimAndAudioServerFromSenderObject(ServerAnimAndAudio serverAnimAndAudio)
	{
		PlayAnimAndAudioServerRpc(serverAnimAndAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimAndAudioServerRpc(ServerAnimAndAudio serverAnimAndAudio)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2259057361u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAnimAndAudio, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 2259057361u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PlayAnimAndAudioClientRpc(serverAnimAndAudio);
			}
		}
	}

	[ClientRpc]
	private void PlayAnimAndAudioClientRpc(ServerAnimAndAudio serverAnimAndAudio)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2993461149u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAnimAndAudio, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 2993461149u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (serverAnimAndAudio.animatorObj.TryGet(out var networkObject))
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimAndAudio.animationString);
			}
			if (serverAnimAndAudio.audioObj.TryGet(out var networkObject2))
			{
				networkObject2.GetComponent<AudioSource>().PlayOneShot(networkObject2.GetComponent<AudioSource>().clip);
			}
		}
	}

	public void PlayAnimationServer(ServerAnimation serverAnimation)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimationServerFromSenderObject(serverAnimation);
	}

	public void PlayAnimationServerFromSenderObject(ServerAnimation serverAnimation)
	{
		PlayAnimationServerRpc(serverAnimation);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimationServerRpc(ServerAnimation serverAnimation)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2698736096u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAnimation, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 2698736096u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PlayAnimationClientRpc(serverAnimation);
			}
		}
	}

	[ClientRpc]
	private void PlayAnimationClientRpc(ServerAnimation serverAnimation)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(780678780u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAnimation, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 780678780u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (IsOwner)
		{
			return;
		}
		if (serverAnimation.animatorObj.TryGet(out var networkObject))
		{
			if (serverAnimation.isTrigger)
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimation.animationString);
			}
			else
			{
				networkObject.GetComponent<Animator>().SetBool(serverAnimation.animationString, serverAnimation.setTrue);
			}
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; string " + serverAnimation.animationString);
		}
	}

	public void PlayAudioServer(ServerAudio serverAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAudioServerFromSenderObject(serverAudio);
	}

	public void PlayAudioServerFromSenderObject(ServerAudio serverAudio)
	{
		PlayAudioServerRpc(serverAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAudioServerRpc(ServerAudio serverAudio)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1842858504u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAudio, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 1842858504u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PlayAudioClientRpc(serverAudio);
			}
		}
	}

	[ClientRpc]
	private void PlayAudioClientRpc(ServerAudio serverAudio)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(182727243u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAudio, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 182727243u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (IsOwner)
		{
			return;
		}
		if (serverAudio.audioObj.TryGet(out var networkObject))
		{
			AudioSource component = networkObject.gameObject.GetComponent<AudioSource>();
			if (serverAudio.oneshot)
			{
				component.PlayOneShot(component.clip, 1f);
				return;
			}
			component.loop = serverAudio.looped;
			component.Play();
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; audio");
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2259057361u, __rpc_handler_2259057361, "PlayAnimAndAudioServerRpc");
		__registerRpc(2993461149u, __rpc_handler_2993461149, "PlayAnimAndAudioClientRpc");
		__registerRpc(2698736096u, __rpc_handler_2698736096, "PlayAnimationServerRpc");
		__registerRpc(780678780u, __rpc_handler_780678780, "PlayAnimationClientRpc");
		__registerRpc(1842858504u, __rpc_handler_1842858504, "PlayAudioServerRpc");
		__registerRpc(182727243u, __rpc_handler_182727243, "PlayAudioClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2259057361(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimAndAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAnimAndAudioServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2993461149(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimAndAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAnimAndAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2698736096(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimation value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAnimationServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_780678780(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimation value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1842858504(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAudioServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_182727243(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((GlobalEffects)target).PlayAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "GlobalEffects";
	}
}
