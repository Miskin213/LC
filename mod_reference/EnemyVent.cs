using Unity.Netcode;
using UnityEngine;

public class EnemyVent : NetworkBehaviour
{
	public float spawnTime;

	public bool occupied;

	[Space(5f)]
	public EnemyType enemyType;

	public int enemyTypeIndex;

	[Space(10f)]
	public AudioSource ventAudio;

	public AudioLowPassFilter lowPassFilter;

	public AudioClip ventCrawlSFX;

	public Transform floorNode;

	private bool isPlayingAudio;

	private RoundManager roundManager;

	public Animator ventAnimator;

	public bool ventIsOpen;

	public bool caveVent;

	private void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
	}

	private void BeginVentSFX()
	{
		if (enemyType.overrideVentSFX != null)
		{
			ventAudio.clip = enemyType.overrideVentSFX;
			ventAudio.Play();
			ventAudio.volume = 0f;
		}
		else if (!caveVent)
		{
			ventAudio.clip = ventCrawlSFX;
			ventAudio.Play();
			ventAudio.volume = 0f;
		}
	}

	[ClientRpc]
	public void OpenVentClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2182253155u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 2182253155u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!ventIsOpen)
			{
				ventIsOpen = true;
				ventAnimator.SetTrigger("openVent");
				lowPassFilter.lowpassResonanceQ = 0f;
			}
			occupied = false;
		}
	}

	[ClientRpc]
	public void SyncVentSpawnTimeClientRpc(int time, int enemyIndex)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3841281693u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, time);
				BytePacker.WriteValueBitPacked(bufferWriter, enemyIndex);
				__endSendClientRpc(ref bufferWriter, 3841281693u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				enemyTypeIndex = enemyIndex;
				enemyType = roundManager.currentLevel.Enemies[enemyIndex].enemyType;
				spawnTime = time;
				occupied = true;
			}
		}
	}

	private void Update()
	{
		if (occupied)
		{
			if (!isPlayingAudio)
			{
				if (spawnTime - roundManager.timeScript.currentDayTime < enemyType.timeToPlayAudio)
				{
					isPlayingAudio = true;
					BeginVentSFX();
				}
			}
			else
			{
				ventAudio.volume = Mathf.Abs((spawnTime - roundManager.timeScript.currentDayTime) / enemyType.timeToPlayAudio - 1f);
				lowPassFilter.lowpassResonanceQ = Mathf.Abs(ventAudio.volume * 2f - 2f);
			}
		}
		else if (isPlayingAudio)
		{
			isPlayingAudio = false;
			ventAudio.Stop();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2182253155u, __rpc_handler_2182253155, "OpenVentClientRpc");
		__registerRpc(3841281693u, __rpc_handler_3841281693, "SyncVentSpawnTimeClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2182253155(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EnemyVent)target).OpenVentClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3841281693(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((EnemyVent)target).SyncVentSpawnTimeClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "EnemyVent";
	}
}
