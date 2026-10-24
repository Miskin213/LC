using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class FlashlightItem : GrabbableObject
{
	[Space(15f)]
	public bool usingPlayerHelmetLight;

	public int flashlightInterferenceLevel;

	public static int globalFlashlightInterferenceLevel;

	public Light flashlightBulb;

	public Light flashlightBulbGlow;

	public AudioSource flashlightAudio;

	public AudioClip[] flashlightClips;

	public AudioClip outOfBatteriesClip;

	public AudioClip flashlightFlicker;

	public Material bulbLight;

	public Material bulbDark;

	public MeshRenderer flashlightMesh;

	public int flashlightTypeID;

	public bool changeMaterial = true;

	private float initialIntensity;

	private PlayerControllerB previousPlayerHeldBy;

	public override void Start()
	{
		base.Start();
		initialIntensity = flashlightBulb.intensity;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (flashlightInterferenceLevel < 2)
		{
			SwitchFlashlight(used);
		}
		flashlightAudio.PlayOneShot(flashlightClips[Random.Range(0, flashlightClips.Length)]);
		RoundManager.Instance.PlayAudibleNoise(transform.position, 7f, 0.4f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
	}

	public override void UseUpBatteries()
	{
		base.UseUpBatteries();
		SwitchFlashlight(on: false);
		flashlightAudio.PlayOneShot(outOfBatteriesClip, 1f);
		RoundManager.Instance.PlayAudibleNoise(transform.position, 13f, 0.65f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
	}

	public override void PocketItem()
	{
		if (!IsOwner)
		{
			base.PocketItem();
			return;
		}
		if (previousPlayerHeldBy != null)
		{
			flashlightBulb.enabled = false;
			flashlightBulbGlow.enabled = false;
			GrabbableObject grabbableObject = null;
			grabbableObject = ((previousPlayerHeldBy.currentItemSlot != 50) ? previousPlayerHeldBy.ItemSlots[previousPlayerHeldBy.currentItemSlot] : previousPlayerHeldBy.ItemOnlySlot);
			if (isBeingUsed && (grabbableObject == null || grabbableObject.itemProperties.itemId != 1 || grabbableObject.itemProperties.itemId != 6))
			{
				previousPlayerHeldBy.helmetLight.enabled = true;
				previousPlayerHeldBy.pocketedFlashlight = this;
				usingPlayerHelmetLight = true;
				PocketFlashlightServerRpc(stillUsingFlashlight: true);
			}
			else
			{
				isBeingUsed = false;
				usingPlayerHelmetLight = false;
				flashlightBulbGlow.enabled = false;
				SwitchFlashlight(on: false);
				PocketFlashlightServerRpc();
			}
		}
		else
		{
			Debug.LogError("Could not find what player was holding this flashlight item: '" + gameObject.name + "'", gameObject);
		}
		base.PocketItem();
	}

	[ServerRpc]
	public void PocketFlashlightServerRpc(bool stillUsingFlashlight = false)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(461510128u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in stillUsingFlashlight, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 461510128u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			PocketFlashlightClientRpc(stillUsingFlashlight);
		}
	}

	[ClientRpc]
	public void PocketFlashlightClientRpc(bool stillUsingFlashlight)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(4121415408u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in stillUsingFlashlight, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 4121415408u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (IsOwner)
		{
			return;
		}
		flashlightBulb.enabled = false;
		flashlightBulbGlow.enabled = false;
		if (stillUsingFlashlight)
		{
			if (!(previousPlayerHeldBy == null))
			{
				previousPlayerHeldBy.helmetLight.enabled = true;
				previousPlayerHeldBy.pocketedFlashlight = this;
				usingPlayerHelmetLight = true;
			}
		}
		else
		{
			isBeingUsed = false;
			usingPlayerHelmetLight = false;
			flashlightBulbGlow.enabled = false;
			SwitchFlashlight(on: false);
		}
	}

	public override void DiscardItem()
	{
		if (previousPlayerHeldBy != null)
		{
			previousPlayerHeldBy.helmetLight.enabled = false;
			flashlightBulb.enabled = isBeingUsed;
			flashlightBulbGlow.enabled = isBeingUsed;
		}
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		previousPlayerHeldBy = playerHeldBy;
		playerHeldBy.ChangeHelmetLight(flashlightTypeID);
		playerHeldBy.helmetLight.enabled = false;
		usingPlayerHelmetLight = false;
		if (isBeingUsed)
		{
			SwitchFlashlight(on: true);
		}
		base.EquipItem();
	}

	public void SwitchFlashlight(bool on)
	{
		isBeingUsed = on;
		if (!IsOwner)
		{
			Debug.Log($"Flashlight click. playerheldby null?: {playerHeldBy != null}");
			Debug.Log($"Flashlight being disabled or enabled: {on}");
			if (playerHeldBy != null)
			{
				playerHeldBy.ChangeHelmetLight(flashlightTypeID, on);
			}
			flashlightBulb.enabled = false;
			flashlightBulbGlow.enabled = false;
		}
		else
		{
			flashlightBulb.enabled = on;
			flashlightBulbGlow.enabled = on;
		}
		if (usingPlayerHelmetLight && playerHeldBy != null)
		{
			playerHeldBy.helmetLight.enabled = on;
		}
		if (changeMaterial)
		{
			Material[] sharedMaterials = flashlightMesh.sharedMaterials;
			if (on)
			{
				sharedMaterials[1] = bulbLight;
			}
			else
			{
				sharedMaterials[1] = bulbDark;
			}
			flashlightMesh.sharedMaterials = sharedMaterials;
		}
	}

	public override void Update()
	{
		base.Update();
		int num = ((flashlightInterferenceLevel <= globalFlashlightInterferenceLevel) ? globalFlashlightInterferenceLevel : flashlightInterferenceLevel);
		if (num >= 2)
		{
			flashlightBulb.intensity = 0f;
		}
		else if (num == 1)
		{
			flashlightBulb.intensity = Random.Range(0f, 200f);
		}
		else
		{
			flashlightBulb.intensity = initialIntensity;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(461510128u, __rpc_handler_461510128, "PocketFlashlightServerRpc");
		__registerRpc(4121415408u, __rpc_handler_4121415408, "PocketFlashlightClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_461510128(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((FlashlightItem)target).PocketFlashlightServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4121415408(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((FlashlightItem)target).PocketFlashlightClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "FlashlightItem";
	}
}
