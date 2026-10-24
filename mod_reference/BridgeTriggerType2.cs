using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BridgeTriggerType2 : NetworkBehaviour
{
	private int timesTriggered;

	public AnimatedObjectTrigger animatedObjectTrigger;

	private bool bridgeFell;

	private void OnTriggerEnter(Collider other)
	{
		if (!bridgeFell)
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && GameNetworkManager.Instance.localPlayerController == component)
			{
				AddToBridgeInstabilityServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddToBridgeInstabilityServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(1248555425u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1248555425u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			timesTriggered++;
			if (timesTriggered == 2)
			{
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
			if (timesTriggered >= 4)
			{
				bridgeFell = true;
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1248555425u, __rpc_handler_1248555425, "AddToBridgeInstabilityServerRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1248555425(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((BridgeTriggerType2)target).AddToBridgeInstabilityServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "BridgeTriggerType2";
	}
}
