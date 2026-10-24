using Unity.Netcode;
using UnityEngine;

public abstract class Anomaly : NetworkBehaviour
{
	public AnomalyType anomalyType;

	public float initialInstability = 10f;

	public float difficultyMultiplier;

	public float normalizedHealth;

	public NetworkObject thisNetworkObject;

	public float maxHealth;

	[HideInInspector]
	public float health;

	[HideInInspector]
	public float removingHealth;

	[HideInInspector]
	public float usedInstability;

	public RoundManager roundManager;

	[Header("Misc properties")]
	public bool raycastToSurface;

	private bool addingInstability;

	public virtual void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>(includeInactive: false);
		thisNetworkObject = gameObject.GetComponent<NetworkObject>();
		addingInstability = true;
		_ = roundManager.hasInitializedLevelRandomSeed;
	}

	public virtual void AnomalyDespawn(bool removedByPatcher = false)
	{
		if (!IsServer)
		{
			DespawnAnomalyServerRpc();
			return;
		}
		addingInstability = false;
		gameObject.GetComponent<NetworkObject>().Despawn();
		roundManager.SpawnedAnomalies.Remove(this);
		roundManager.SpawnedAnomalies.TrimExcess();
	}

	[ServerRpc(RequireOwnership = false)]
	public void DespawnAnomalyServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(3450772816u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 3450772816u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (gameObject.GetComponent<NetworkObject>().IsSpawned)
			{
				AnomalyDespawn();
			}
		}
	}

	public virtual void Update()
	{
		if (removingHealth > 0f)
		{
			health -= removingHealth * Time.deltaTime;
			if (IsServer && health <= 0f)
			{
				AnomalyDespawn(removedByPatcher: true);
			}
		}
		else
		{
			health = Mathf.Clamp(health += Time.deltaTime * 1.5f, anomalyType.anomalyMaxHealth / 3f, anomalyType.anomalyMaxHealth);
			if (IsServer && addingInstability)
			{
				if (usedInstability <= initialInstability)
				{
					usedInstability += Time.deltaTime;
				}
				else
				{
					usedInstability += Time.deltaTime / 3f;
				}
			}
		}
		normalizedHealth = Mathf.Abs(anomalyType.anomalyMaxHealth / health - 1f);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(3450772816u, __rpc_handler_3450772816, "DespawnAnomalyServerRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_3450772816(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Anomaly)target).DespawnAnomalyServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "Anomaly";
	}
}
