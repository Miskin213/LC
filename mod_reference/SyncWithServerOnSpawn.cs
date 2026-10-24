using Unity.Netcode;
using UnityEngine;

public class SyncWithServerOnSpawn : NetworkBehaviour
{
	public RoundManager roundManager;

	private bool hasSynced;

	private void Start()
	{
	}

	public void SyncWithServer()
	{
		if (!IsServer)
		{
			Object.Destroy(gameObject);
			return;
		}
		NetworkObject component = gameObject.GetComponent<NetworkObject>();
		if (component != null)
		{
			component.Spawn(destroyWithScene: true);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "SyncWithServerOnSpawn";
	}
}
