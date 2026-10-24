using Unity.Netcode;
using UnityEngine;

public class KiwiBabyItem : GrabbableObject
{
	public Animator babyAnimator;

	public bool screaming;

	public int currentAnimation;

	private float timeHeld;

	public AudioSource eggAudio;

	public AudioClip peepAudio;

	public AudioClip screamAudio;

	public AudioClip breakEggSFX;

	private float screamingOnFloorTimer;

	private bool staring;

	private float screamTimer;

	private float stopScreamTimer;

	private bool countingScreamTimer;

	private bool takenIntoOrbit;

	public GiantKiwiAI mamaAI;

	public float timeLastAbandoned;

	public Vector3 positionWhenLastAbandoned;

	public bool hasScreamed;

	private float makeNoiseInterval;

	public AudioClip scream3SFX;

	[ServerRpc]
	public void SetScreamingServerRpc(bool scream)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2654954731u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in scream, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2654954731u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetScreamingClientRpc(scream);
		}
	}

	[ClientRpc]
	public void SetScreamingClientRpc(bool scream)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1784909488u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in scream, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1784909488u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (screaming != scream)
		{
			screaming = scream;
			if (scream)
			{
				countingScreamTimer = false;
				stopScreamTimer = 3f;
				currentAnimation = 3;
				hasScreamed = true;
			}
			else
			{
				currentAnimation = 1;
			}
		}
	}

	[ServerRpc]
	public void BreakOutServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(84321064u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 84321064u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			BreakOutClientRpc();
		}
	}

	[ClientRpc]
	public void BreakOutClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(788004645u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 788004645u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (currentAnimation == 0)
			{
				currentAnimation = 1;
				eggAudio.PlayOneShot(breakEggSFX);
			}
		}
	}

	public override void Start()
	{
		base.Start();
		screamTimer = 13f;
		mamaAI = Object.FindObjectOfType<GiantKiwiAI>();
		if (StartOfRound.Instance.inShipPhase)
		{
			takenIntoOrbit = true;
			currentAnimation = 4;
		}
	}

	public override void ReactToSellingItemOnCounter()
	{
		base.ReactToSellingItemOnCounter();
		currentAnimation = 5;
		eggAudio.pitch = Random.Range(0.93f, 1.15f);
		eggAudio.PlayOneShot(scream3SFX);
		babyAnimator.SetInteger("babyAnimation", 5);
	}

	public override void Update()
	{
		base.Update();
		if (currentAnimation == 5)
		{
			return;
		}
		if (StartOfRound.Instance.inShipPhase || takenIntoOrbit)
		{
			currentAnimation = 4;
			if (screaming)
			{
				screaming = false;
				takenIntoOrbit = true;
			}
			if (currentAnimation == 4)
			{
				eggAudio.Stop();
			}
		}
		else if (!eggAudio.isPlaying)
		{
			if (screaming)
			{
				eggAudio.clip = screamAudio;
				eggAudio.Play();
			}
			else if (currentAnimation == 1)
			{
				eggAudio.clip = peepAudio;
				eggAudio.Play();
			}
		}
		else if (currentAnimation == 4)
		{
			eggAudio.Stop();
		}
		babyAnimator.SetInteger("babyAnimation", currentAnimation);
		if (mamaAI == null)
		{
			return;
		}
		if (isHeld || isHeldByEnemy)
		{
			float num = Vector3.Distance(transform.position, mamaAI.birdNest.transform.position);
			Debug.Log($"Baby bird distance to nest : {num}", gameObject);
			if (num > 5f)
			{
				if (!countingScreamTimer)
				{
					countingScreamTimer = true;
				}
			}
			else if (num < 4.8f && countingScreamTimer)
			{
				countingScreamTimer = false;
				screamTimer = 6f;
				screaming = false;
			}
		}
		if (countingScreamTimer)
		{
			Debug.Log($"Counting scream timer; timer: {screamTimer}");
			if (screamTimer > 0f)
			{
				screamTimer -= Time.deltaTime;
				if (countingScreamTimer)
				{
					switch (currentAnimation)
					{
					case 0:
						if (screamTimer < 7f && IsOwner)
						{
							eggAudio.PlayOneShot(breakEggSFX);
							currentAnimation = 1;
							BreakOutServerRpc();
						}
						break;
					case 1:
						makeNoiseInterval -= Time.deltaTime;
						if (makeNoiseInterval < 0f)
						{
							makeNoiseInterval = 0.5f;
							RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 0.6f, 0, StartOfRound.Instance.hangarDoorsClosed && isInShipRoom);
						}
						if (screamTimer < 3f)
						{
							currentAnimation = 2;
						}
						break;
					case 2:
						if (eggAudio.isPlaying)
						{
							eggAudio.Stop();
						}
						break;
					case 4:
						if (eggAudio.isPlaying)
						{
							eggAudio.Stop();
						}
						break;
					}
				}
			}
			else if (!screaming)
			{
				Debug.Log($"Start screaming; {screamTimer}");
				countingScreamTimer = false;
				screaming = true;
				hasScreamed = true;
				currentAnimation = 3;
				stopScreamTimer = 3f;
				if (IsOwner)
				{
					SetScreamingServerRpc(scream: true);
				}
			}
		}
		if (screaming && !isHeld)
		{
			stopScreamTimer -= Time.deltaTime;
			if (stopScreamTimer < 0f && IsOwner)
			{
				screaming = false;
				currentAnimation = 4;
				SetScreamingServerRpc(scream: false);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2654954731u, __rpc_handler_2654954731, "SetScreamingServerRpc");
		__registerRpc(1784909488u, __rpc_handler_1784909488, "SetScreamingClientRpc");
		__registerRpc(84321064u, __rpc_handler_84321064, "BreakOutServerRpc");
		__registerRpc(788004645u, __rpc_handler_788004645, "BreakOutClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2654954731(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((KiwiBabyItem)target).SetScreamingServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1784909488(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((KiwiBabyItem)target).SetScreamingClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_84321064(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((KiwiBabyItem)target).BreakOutServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_788004645(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((KiwiBabyItem)target).BreakOutClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "KiwiBabyItem";
	}
}
