using Unity.Netcode;
using UnityEngine;

public class RandomPeriodicAudioPlayer : NetworkBehaviour
{
	public GrabbableObject attachedGrabbableObject;

	public AudioClip[] randomClips;

	public AudioSource thisAudio;

	public float audioMinInterval;

	public float audioMaxInterval;

	public float audioChancePercent;

	private float currentInterval;

	private float lastIntervalTime;

	private void Update()
	{
		if (IsServer && !(GameNetworkManager.Instance.localPlayerController == null) && (!(attachedGrabbableObject != null) || !attachedGrabbableObject.deactivated) && Time.realtimeSinceStartup - lastIntervalTime > currentInterval)
		{
			lastIntervalTime = Time.realtimeSinceStartup;
			currentInterval = Random.Range(audioMinInterval, audioMaxInterval);
			if (Random.Range(0f, 100f) < audioChancePercent)
			{
				PlayRandomAudioClientRpc(Random.Range(0, randomClips.Length));
			}
		}
	}

	[ClientRpc]
	public void PlayRandomAudioClientRpc(int clipIndex)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1557920159u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, clipIndex);
				__endSendClientRpc(ref bufferWriter, 1557920159u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PlayAudio(clipIndex);
			}
		}
	}

	private void PlayAudio(int clipIndex)
	{
		AudioClip clip = randomClips[clipIndex];
		thisAudio.PlayOneShot(clip, 1f);
		WalkieTalkie.TransmitOneShotAudio(thisAudio, clip);
		RoundManager.Instance.PlayAudibleNoise(thisAudio.transform.position, 7f, 0.6f);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1557920159u, __rpc_handler_1557920159, "PlayRandomAudioClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1557920159(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((RandomPeriodicAudioPlayer)target).PlayRandomAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "RandomPeriodicAudioPlayer";
	}
}
