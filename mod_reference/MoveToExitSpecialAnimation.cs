using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class MoveToExitSpecialAnimation : NetworkBehaviour
{
	public InteractTrigger interactTrigger;

	public InteractTrigger strapsInteractTrigger;

	public AnimatedObjectTrigger animatedObjectTrigger;

	private float timeSinceAnimationStarted;

	private bool listeningForInput;

	public bool exitingDisabled;

	[Header("Electric chair variables")]
	public bool electricChair;

	private Coroutine shockChairCoroutine;

	public ParticleSystem shockChairParticle;

	public AudioSource shockChairSound;

	private void OnEnable()
	{
		if (electricChair)
		{
			StartOfRound.Instance.ShipPowerSurgedEvent.AddListener(OnShipPowerSurge);
		}
	}

	private void OnDisable()
	{
		if (electricChair)
		{
			StartOfRound.Instance.ShipPowerSurgedEvent.RemoveListener(OnShipPowerSurge);
		}
		if (listeningForInput)
		{
			listeningForInput = false;
			StartOfRound.Instance.PlayerMoveInputEvent.RemoveListener(OnMoveInputDetected);
			if (animatedObjectTrigger.boolValue)
			{
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
		}
	}

	private void OnShipPowerSurge()
	{
		if (shockChairCoroutine == null)
		{
			shockChairCoroutine = StartCoroutine(shockChair());
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PowerSurgeChairServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2269800271u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2269800271u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PowerSurgeChairClientRpc();
			}
		}
	}

	[ClientRpc]
	public void PowerSurgeChairClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1254697919u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1254697919u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				OnShipPowerSurge();
			}
		}
	}

	private IEnumerator shockChair()
	{
		shockChairParticle.Play(withChildren: true);
		shockChairSound.Play();
		strapsInteractTrigger.interactable = false;
		if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, gameObject.transform.position) < 8f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
		}
		else if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, gameObject.transform.position) < 14f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		for (int i = 0; i < 11; i++)
		{
			if (interactTrigger.lockedPlayer != null)
			{
				interactTrigger.lockedPlayer.GetComponent<PlayerControllerB>().DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Electrocution, 3);
			}
			yield return new WaitForSeconds(0.3f);
			if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, gameObject.transform.position) < 14f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			}
		}
		yield return new WaitForSeconds(0.7f);
		shockChairParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		shockChairSound.Stop();
		strapsInteractTrigger.interactable = true;
		yield return new WaitForSeconds(0.5f);
		shockChairCoroutine = null;
	}

	public void SetExitingDisabled(bool disabled)
	{
		exitingDisabled = disabled;
		interactTrigger.interactable = !disabled;
	}

	public void Update()
	{
		if (interactTrigger.isPlayingSpecialAnimation && interactTrigger.lockedPlayer != null && interactTrigger.lockedPlayer == GameNetworkManager.Instance.localPlayerController.transform)
		{
			if (!listeningForInput)
			{
				timeSinceAnimationStarted += Time.deltaTime;
				if (timeSinceAnimationStarted > 1f)
				{
					timeSinceAnimationStarted = 0f;
					StartOfRound.Instance.PlayerMoveInputEvent.AddListener(OnMoveInputDetected);
					listeningForInput = true;
				}
			}
		}
		else if (listeningForInput)
		{
			listeningForInput = false;
			StartOfRound.Instance.PlayerMoveInputEvent.RemoveListener(OnMoveInputDetected);
			if (animatedObjectTrigger.boolValue)
			{
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
			timeSinceAnimationStarted = 0f;
		}
	}

	public void OnMoveInputDetected(PlayerControllerB playerWhoMoved)
	{
		Debug.Log("Sofa chair detecting movement");
		if (!exitingDisabled && !(GameNetworkManager.Instance.localPlayerController != playerWhoMoved) && interactTrigger.isPlayingSpecialAnimation && interactTrigger.lockedPlayer != null && interactTrigger.lockedPlayer == GameNetworkManager.Instance.localPlayerController.transform)
		{
			interactTrigger.CancelAnimationExternally();
			listeningForInput = false;
			StartOfRound.Instance.PlayerMoveInputEvent.RemoveListener(OnMoveInputDetected);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2269800271u, __rpc_handler_2269800271, "PowerSurgeChairServerRpc");
		__registerRpc(1254697919u, __rpc_handler_1254697919, "PowerSurgeChairClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2269800271(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MoveToExitSpecialAnimation)target).PowerSurgeChairServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1254697919(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((MoveToExitSpecialAnimation)target).PowerSurgeChairClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "MoveToExitSpecialAnimation";
	}
}
