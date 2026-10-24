using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

public class TVScript : NetworkBehaviour
{
	public bool tvOn;

	private bool wasTvOnLastFrame;

	public MeshRenderer tvMesh;

	public VideoPlayer video;

	[Space(5f)]
	public VideoClip[] tvClips;

	public AudioClip[] tvAudioClips;

	[Space(5f)]
	private float currentClipTime;

	private int currentClip;

	public Material tvOnMaterial;

	public Material tvOffMaterial;

	public AudioClip switchTVOn;

	public AudioClip switchTVOff;

	public AudioSource tvSFX;

	private float timeSinceTurningOffTV;

	public Light tvLight;

	public void TurnTVOnOff(bool on)
	{
		tvOn = on;
		if (on)
		{
			tvSFX.clip = tvAudioClips[currentClip];
			tvSFX.time = currentClipTime;
			tvSFX.Play();
			tvSFX.PlayOneShot(switchTVOn);
			WalkieTalkie.TransmitOneShotAudio(tvSFX, switchTVOn);
		}
		else
		{
			tvSFX.Stop();
			tvSFX.PlayOneShot(switchTVOff);
			WalkieTalkie.TransmitOneShotAudio(tvSFX, switchTVOff);
		}
	}

	public void SwitchTVLocalClient()
	{
		if (tvOn)
		{
			TurnOffTVServerRpc();
		}
		else
		{
			TurnOnTVServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TurnOnTVServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(4276612883u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 4276612883u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			timeSinceTurningOffTV = 0f;
			if (timeSinceTurningOffTV > 7f)
			{
				TurnOnTVAndSyncClientRpc(currentClip, currentClipTime);
			}
			else
			{
				TurnOnTVClientRpc();
			}
		}
	}

	[ClientRpc]
	public void TurnOnTVClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3163094487u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3163094487u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				TurnTVOnOff(on: true);
			}
		}
	}

	[ClientRpc]
	public void TurnOnTVAndSyncClientRpc(int clipIndex, float clipTime)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(90711347u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, clipIndex);
				bufferWriter.WriteValueSafe(in clipTime, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 90711347u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				currentClip = clipIndex;
				currentClipTime = clipTime;
				TurnTVOnOff(on: true);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TurnOffTVServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1273566447u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 1273566447u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				TurnOffTVClientRpc();
			}
		}
	}

	[ClientRpc]
	public void TurnOffTVClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3106289039u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3106289039u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				TurnTVOnOff(on: false);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncTVServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(3782954741u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 3782954741u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SyncTVClientRpc(currentClip, currentClipTime, tvOn);
			}
		}
	}

	[ClientRpc]
	public void SyncTVClientRpc(int clipIndex, float clipTime, bool isOn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1554186895u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, clipIndex);
				bufferWriter.WriteValueSafe(in clipTime, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in isOn, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1554186895u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SyncTimeAndClipWithClients(clipIndex, clipTime, isOn);
			}
		}
	}

	private void SyncTimeAndClipWithClients(int clipIndex, float clipTime, bool isOn)
	{
		currentClip = clipIndex;
		currentClipTime = clipTime;
		tvOn = isOn;
	}

	private void OnEnable()
	{
		video.loopPointReached += TVFinishedClip;
	}

	private void OnDisable()
	{
		video.loopPointReached -= TVFinishedClip;
	}

	private void TVFinishedClip(VideoPlayer source)
	{
		if (tvOn && !GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		{
			currentClip = (currentClip + 1) % tvClips.Length;
			video.clip = tvClips[currentClip];
			video.Play();
			tvSFX.clip = tvAudioClips[currentClip];
			tvSFX.time = 0f;
			tvSFX.Play();
		}
	}

	private void Update()
	{
		if (NetworkManager.Singleton.ShutdownInProgress || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		if (!tvOn || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		{
			if (wasTvOnLastFrame)
			{
				wasTvOnLastFrame = false;
				SetTVScreenMaterial(on: false);
				currentClipTime = (float)video.time;
				video.Stop();
			}
			if (IsServer && !tvOn)
			{
				timeSinceTurningOffTV += Time.deltaTime;
			}
			currentClipTime += Time.deltaTime;
			if ((double)currentClipTime > tvClips[currentClip].length)
			{
				currentClip = (currentClip + 1) % tvClips.Length;
				currentClipTime = 0f;
				if (tvOn)
				{
					tvSFX.clip = tvAudioClips[currentClip];
					tvSFX.Play();
				}
			}
		}
		else
		{
			if (!wasTvOnLastFrame)
			{
				wasTvOnLastFrame = true;
				SetTVScreenMaterial(on: true);
				video.clip = tvClips[currentClip];
				video.time = currentClipTime;
				video.Play();
			}
			currentClipTime = (float)video.time;
		}
	}

	private void SetTVScreenMaterial(bool on)
	{
		Material[] sharedMaterials = tvMesh.sharedMaterials;
		if (on)
		{
			sharedMaterials[1] = tvOnMaterial;
		}
		else
		{
			sharedMaterials[1] = tvOffMaterial;
		}
		tvMesh.sharedMaterials = sharedMaterials;
		tvLight.enabled = on;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(4276612883u, __rpc_handler_4276612883, "TurnOnTVServerRpc");
		__registerRpc(3163094487u, __rpc_handler_3163094487, "TurnOnTVClientRpc");
		__registerRpc(90711347u, __rpc_handler_90711347, "TurnOnTVAndSyncClientRpc");
		__registerRpc(1273566447u, __rpc_handler_1273566447, "TurnOffTVServerRpc");
		__registerRpc(3106289039u, __rpc_handler_3106289039, "TurnOffTVClientRpc");
		__registerRpc(3782954741u, __rpc_handler_3782954741, "SyncTVServerRpc");
		__registerRpc(1554186895u, __rpc_handler_1554186895, "SyncTVClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_4276612883(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).TurnOnTVServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3163094487(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).TurnOnTVClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_90711347(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).TurnOnTVAndSyncClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1273566447(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).TurnOffTVServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3106289039(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).TurnOffTVClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3782954741(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).SyncTVServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1554186895(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value3, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TVScript)target).SyncTVClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "TVScript";
	}
}
