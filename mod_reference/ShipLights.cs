using Unity.Netcode;
using UnityEngine;

public class ShipLights : NetworkBehaviour
{
	public bool areLightsOn = true;

	public Animator shipLightsAnimator;

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLightsServerRpc(bool setLightsOn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1625678258u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 1625678258u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetShipLightsClientRpc(setLightsOn);
			}
		}
	}

	[ClientRpc]
	public void SetShipLightsClientRpc(bool setLightsOn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1484401029u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1484401029u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				areLightsOn = setLightsOn;
				shipLightsAnimator.SetBool("lightsOn", areLightsOn);
				Debug.Log($"Received set ship lights RPC. Lights on?: {areLightsOn}");
			}
		}
	}

	public void ToggleShipLights()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Toggling ship lights RPC. lights now: {areLightsOn}");
	}

	public void SetShipLightsBoolean(bool setLights)
	{
		areLightsOn = setLights;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Calling ship lights boolean RPC: {areLightsOn}");
	}

	public void ToggleShipLightsOnLocalClientOnly()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}

	public void SetShipLightsOnLocalClientOnly(bool setLightsOn)
	{
		areLightsOn = setLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1625678258u, __rpc_handler_1625678258, "SetShipLightsServerRpc");
		__registerRpc(1484401029u, __rpc_handler_1484401029, "SetShipLightsClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1625678258(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipLights)target).SetShipLightsServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1484401029(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((ShipLights)target).SetShipLightsClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ShipLights";
	}
}
