using Unity.Netcode;
using UnityEngine;

public class ShipAlarmCord : NetworkBehaviour
{
	private bool hornBlaring;

	private float cordPulledDownTimer;

	public Animator cordAnimator;

	public AudioSource hornClose;

	public AudioSource hornFar;

	public AudioSource cordAudio;

	public AudioClip cordPullSFX;

	private bool otherClientHoldingCord;

	private float playAudibleNoiseInterval;

	private int timesPlayingAtOnce;

	public PlaceableShipObject shipObjectScript;

	private int unlockableID;

	private bool localClientHoldingCord;

	private void Start()
	{
		unlockableID = shipObjectScript.unlockableID;
	}

	public void HoldCordDown()
	{
		if (otherClientHoldingCord)
		{
			return;
		}
		Debug.Log("HOLD horn local client called");
		cordPulledDownTimer = 0.3f;
		if (!hornBlaring)
		{
			Debug.Log("Hornblaring setting to true!");
			localClientHoldingCord = true;
			cordAnimator.SetBool("pulled", value: true);
			cordAudio.PlayOneShot(cordPullSFX);
			WalkieTalkie.TransmitOneShotAudio(cordAudio, cordPullSFX);
			RoundManager.Instance.PlayAudibleNoise(cordAudio.transform.position, 4.5f, 0.5f, 0, StartOfRound.Instance.hangarDoorsClosed);
			hornBlaring = true;
			if (!hornClose.isPlaying)
			{
				hornClose.Play();
				hornFar.Play();
			}
			PullCordServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	public void StopHorn()
	{
		if (hornBlaring)
		{
			Debug.Log("Stop horn local client called");
			localClientHoldingCord = false;
			hornBlaring = false;
			cordAnimator.SetBool("pulled", value: false);
			StopPullingCordServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	private void Update()
	{
		if (hornBlaring)
		{
			hornFar.volume = Mathf.Min(hornFar.volume + Time.deltaTime * 0.45f, 1f);
			hornFar.pitch = Mathf.Lerp(hornFar.pitch, 0.97f, Time.deltaTime * 0.8f);
			hornClose.volume = Mathf.Min(hornClose.volume + Time.deltaTime * 0.45f, 1f);
			hornClose.pitch = Mathf.Lerp(hornClose.pitch, 0.97f, Time.deltaTime * 0.8f);
			if (hornClose.volume > 0.6f && playAudibleNoiseInterval <= 0f)
			{
				playAudibleNoiseInterval = 1f;
				RoundManager.Instance.PlayAudibleNoise(hornClose.transform.position, 30f, 0.8f, timesPlayingAtOnce, noiseIsInsideClosedShip: false, 14155);
				timesPlayingAtOnce++;
			}
			else
			{
				playAudibleNoiseInterval -= Time.deltaTime;
			}
		}
		else
		{
			hornFar.volume = Mathf.Max(hornFar.volume - Time.deltaTime * 0.3f, 0f);
			hornFar.pitch = Mathf.Lerp(hornFar.pitch, 0.88f, Time.deltaTime * 0.5f);
			hornClose.volume = Mathf.Max(hornClose.volume - Time.deltaTime * 0.3f, 0f);
			hornClose.pitch = Mathf.Lerp(hornClose.pitch, 0.88f, Time.deltaTime * 0.5f);
			if (hornClose.volume <= 0f)
			{
				hornClose.Stop();
				hornFar.Stop();
				timesPlayingAtOnce = 0;
			}
		}
		if (localClientHoldingCord)
		{
			if (cordPulledDownTimer >= 0f && !StartOfRound.Instance.unlockablesList.unlockables[unlockableID].inStorage)
			{
				cordPulledDownTimer -= Time.deltaTime;
			}
			else if (hornBlaring)
			{
				StopHorn();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PullCordServerRpc(int playerPullingCord)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(504098657u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerPullingCord);
				__endSendServerRpc(ref bufferWriter, 504098657u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PullCordClientRpc(playerPullingCord);
			}
		}
	}

	[ClientRpc]
	public void PullCordClientRpc(int playerPullingCord)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1428666593u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerPullingCord);
			__endSendClientRpc(ref bufferWriter, 1428666593u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Debug.Log("Received pull cord client rpc");
		if (!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerPullingCord)
		{
			otherClientHoldingCord = true;
			hornBlaring = true;
			cordAnimator.SetBool("pulled", value: true);
			cordAudio.PlayOneShot(cordPullSFX);
			WalkieTalkie.TransmitOneShotAudio(cordAudio, cordPullSFX);
			if (!hornClose.isPlaying)
			{
				hornClose.Play();
			}
			if (!hornFar.isPlaying)
			{
				hornFar.Play();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopPullingCordServerRpc(int playerPullingCord)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(967408504u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerPullingCord);
				__endSendServerRpc(ref bufferWriter, 967408504u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StopPullingCordClientRpc(playerPullingCord);
			}
		}
	}

	[ClientRpc]
	public void StopPullingCordClientRpc(int playerPullingCord)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2882145839u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerPullingCord);
			__endSendClientRpc(ref bufferWriter, 2882145839u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Debug.Log("Received STOP pull cord client rpc");
		if (!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerPullingCord)
		{
			otherClientHoldingCord = false;
			hornBlaring = false;
			cordAnimator.SetBool("pulled", value: false);
			if (StartOfRound.Instance.unlockablesList.unlockables[unlockableID].inStorage)
			{
				hornFar.volume = 0f;
				hornFar.pitch = 0.8f;
				hornClose.volume = 0f;
				hornClose.pitch = 0.8f;
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(504098657u, __rpc_handler_504098657, "PullCordServerRpc");
		__registerRpc(1428666593u, __rpc_handler_1428666593, "PullCordClientRpc");
		__registerRpc(967408504u, __rpc_handler_967408504, "StopPullingCordServerRpc");
		__registerRpc(2882145839u, __rpc_handler_2882145839, "StopPullingCordClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_504098657(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipAlarmCord)target).PullCordServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1428666593(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipAlarmCord)target).PullCordClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_967408504(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipAlarmCord)target).StopPullingCordServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2882145839(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipAlarmCord)target).StopPullingCordClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ShipAlarmCord";
	}
}
