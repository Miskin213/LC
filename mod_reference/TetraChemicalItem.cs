using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class TetraChemicalItem : GrabbableObject
{
	private PlayerControllerB previousPlayerHeldBy;

	private Coroutine useTZPCoroutine;

	private bool emittingGas;

	private float fuel = 1f;

	public AudioSource localHelmetSFX;

	public AudioSource thisAudioSource;

	public AudioClip twistCanSFX;

	public AudioClip releaseGasSFX;

	public AudioClip holdCanSFX;

	public AudioClip removeCanSFX;

	public AudioClip outOfGasSFX;

	private bool triedUsingWithoutFuel;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (buttonDown)
		{
			isBeingUsed = true;
			if (fuel <= 0f)
			{
				if (!triedUsingWithoutFuel)
				{
					triedUsingWithoutFuel = true;
					thisAudioSource.PlayOneShot(outOfGasSFX);
					WalkieTalkie.TransmitOneShotAudio(thisAudioSource, outOfGasSFX);
					previousPlayerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
				}
				return;
			}
			previousPlayerHeldBy = playerHeldBy;
			useTZPCoroutine = StartCoroutine(UseTZPAnimation());
		}
		else
		{
			isBeingUsed = false;
			if (triedUsingWithoutFuel)
			{
				triedUsingWithoutFuel = false;
			}
			else if (useTZPCoroutine != null)
			{
				StopCoroutine(useTZPCoroutine);
				emittingGas = false;
				previousPlayerHeldBy.activatingItem = false;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
				thisAudioSource.PlayOneShot(removeCanSFX);
			}
		}
		if (IsOwner)
		{
			previousPlayerHeldBy.activatingItem = buttonDown;
			previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
		}
	}

	private IEnumerator UseTZPAnimation()
	{
		thisAudioSource.PlayOneShot(holdCanSFX);
		WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
		yield return new WaitForSeconds(0.75f);
		emittingGas = true;
		HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: true);
		if (IsOwner)
		{
			localHelmetSFX.Play();
			localHelmetSFX.PlayOneShot(twistCanSFX);
		}
		else
		{
			thisAudioSource.clip = releaseGasSFX;
			thisAudioSource.Play();
			thisAudioSource.PlayOneShot(twistCanSFX);
		}
		WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, twistCanSFX);
	}

	public override void Update()
	{
		if (emittingGas)
		{
			if (previousPlayerHeldBy == null || !isHeld || fuel <= 0f)
			{
				emittingGas = false;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
				RunOutOfFuelServerRpc();
			}
			previousPlayerHeldBy.drunknessInertia = Mathf.Clamp(previousPlayerHeldBy.drunknessInertia + Time.deltaTime / 1.75f * previousPlayerHeldBy.drunknessSpeed, 0.1f, 3f);
			previousPlayerHeldBy.increasingDrunknessThisFrame = true;
			fuel -= Time.deltaTime / 38f;
			previousPlayerHeldBy.sprintMeter = Mathf.Clamp(previousPlayerHeldBy.sprintMeter + Time.deltaTime / (previousPlayerHeldBy.sprintTime + 9f), 0f, 1f);
		}
		base.Update();
	}

	public override void EquipItem()
	{
		base.EquipItem();
		StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
		if (playerHeldBy != null)
		{
			previousPlayerHeldBy = playerHeldBy;
		}
	}

	[ServerRpc]
	public void RunOutOfFuelServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1607080184u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1607080184u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			RunOutOfFuelClientRpc();
		}
	}

	[ClientRpc]
	public void RunOutOfFuelClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3625530963u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3625530963u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				itemUsedUp = true;
				emittingGas = false;
				fuel = 0f;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
			}
		}
	}

	public override void DiscardItem()
	{
		emittingGas = false;
		thisAudioSource.Stop();
		localHelmetSFX.Stop();
		playerHeldBy.playerBodyAnimator.ResetTrigger("shakeItem");
		previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", value: false);
		if (previousPlayerHeldBy != null)
		{
			previousPlayerHeldBy.activatingItem = false;
		}
		base.DiscardItem();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(1607080184u, __rpc_handler_1607080184, "RunOutOfFuelServerRpc");
		__registerRpc(3625530963u, __rpc_handler_3625530963, "RunOutOfFuelClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_1607080184(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((TetraChemicalItem)target).RunOutOfFuelServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3625530963(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TetraChemicalItem)target).RunOutOfFuelClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "TetraChemicalItem";
	}
}
