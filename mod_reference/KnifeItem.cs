using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class KnifeItem : GrabbableObject
{
	public AudioSource knifeAudio;

	private List<RaycastHit> objectsHitByKnifeList = new List<RaycastHit>();

	public PlayerControllerB previousPlayerHeldBy;

	private RaycastHit[] objectsHitByKnife;

	public int knifeHitForce;

	public AudioClip[] hitSFX;

	public AudioClip[] swingSFX;

	private int knifeMask = 1084754248;

	private float timeAtLastDamageDealt;

	public ParticleSystem bloodParticle;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		RoundManager.PlayRandomClip(knifeAudio, swingSFX);
		if (playerHeldBy != null)
		{
			previousPlayerHeldBy = playerHeldBy;
			if (playerHeldBy.IsOwner)
			{
				playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
			}
		}
		if (IsOwner)
		{
			HitKnife();
		}
	}

	public override void PocketItem()
	{
		base.PocketItem();
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		base.EquipItem();
	}

	public void HitKnife(bool cancel = false)
	{
		if (previousPlayerHeldBy == null)
		{
			Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
			return;
		}
		previousPlayerHeldBy.activatingItem = false;
		bool flag = false;
		bool flag2 = false;
		int num = -1;
		bool flag3 = false;
		if (!cancel && Time.realtimeSinceStartup - timeAtLastDamageDealt > 0.43f)
		{
			previousPlayerHeldBy.twoHanded = false;
			objectsHitByKnife = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, 0.75f, knifeMask, QueryTriggerInteraction.Collide);
			objectsHitByKnifeList = objectsHitByKnife.OrderBy((RaycastHit x) => x.distance).ToList();
			List<EnemyAI> list = new List<EnemyAI>();
			for (int num2 = 0; num2 < objectsHitByKnifeList.Count; num2++)
			{
				if (objectsHitByKnifeList[num2].transform.gameObject.layer == 8 || objectsHitByKnifeList[num2].transform.gameObject.layer == 11)
				{
					if (objectsHitByKnifeList[num2].collider.isTrigger)
					{
						continue;
					}
					flag = true;
					string text = objectsHitByKnifeList[num2].collider.gameObject.tag;
					for (int num3 = 0; num3 < StartOfRound.Instance.footstepSurfaces.Length; num3++)
					{
						if (StartOfRound.Instance.footstepSurfaces[num3].surfaceTag == text)
						{
							num = num3;
							break;
						}
					}
				}
				else
				{
					if (!objectsHitByKnifeList[num2].transform.TryGetComponent<IHittable>(out var component) || objectsHitByKnifeList[num2].transform == previousPlayerHeldBy.transform || (!(objectsHitByKnifeList[num2].point == Vector3.zero) && Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, objectsHitByKnifeList[num2].point, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
					{
						continue;
					}
					flag = true;
					Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
					try
					{
						EnemyAICollisionDetect component2 = objectsHitByKnifeList[num2].transform.GetComponent<EnemyAICollisionDetect>();
						if (component2 != null)
						{
							if (!(component2.mainScript == null) && !list.Contains(component2.mainScript) && (!StartOfRound.Instance.hangarDoorsClosed || component2.mainScript.isInsidePlayerShip == previousPlayerHeldBy.isInHangarShipRoom))
							{
								goto IL_033f;
							}
							continue;
						}
						if (!(objectsHitByKnifeList[num2].transform.GetComponent<PlayerControllerB>() != null))
						{
							goto IL_033f;
						}
						if (!flag3)
						{
							flag3 = true;
							goto IL_033f;
						}
						goto end_IL_029e;
						IL_033f:
						bool flag4 = component.Hit(knifeHitForce, forward, previousPlayerHeldBy, playHitSFX: true, 5);
						if (flag4 && component2 != null)
						{
							list.Add(component2.mainScript);
						}
						if (!flag2 & flag4)
						{
							flag2 = true;
							timeAtLastDamageDealt = Time.realtimeSinceStartup;
							bloodParticle.Play(withChildren: true);
						}
						end_IL_029e:;
					}
					catch (Exception arg)
					{
						Debug.Log($"Exception caught when hitting object with shovel from player #{previousPlayerHeldBy.playerClientId}: {arg}");
					}
				}
			}
		}
		if (flag)
		{
			RoundManager.PlayRandomClip(knifeAudio, hitSFX);
			UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(transform.position, 17f, 0.8f);
			if (!flag2 && num != -1)
			{
				knifeAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
				WalkieTalkie.TransmitOneShotAudio(knifeAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
			}
			HitShovelServerRpc(num);
		}
	}

	[ServerRpc]
	public void HitShovelServerRpc(int hitSurfaceID)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2696735117u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, hitSurfaceID);
			__endSendServerRpc(ref bufferWriter, 2696735117u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			HitShovelClientRpc(hitSurfaceID);
		}
	}

	[ClientRpc]
	public void HitShovelClientRpc(int hitSurfaceID)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3250235443u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, hitSurfaceID);
			__endSendClientRpc(ref bufferWriter, 3250235443u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!IsOwner)
		{
			RoundManager.PlayRandomClip(knifeAudio, hitSFX);
			if (hitSurfaceID != -1)
			{
				HitSurfaceWithKnife(hitSurfaceID);
			}
		}
	}

	private void HitSurfaceWithKnife(int hitSurfaceID)
	{
		knifeAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
		WalkieTalkie.TransmitOneShotAudio(knifeAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2696735117u, __rpc_handler_2696735117, "HitShovelServerRpc");
		__registerRpc(3250235443u, __rpc_handler_3250235443, "HitShovelClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2696735117(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((KnifeItem)target).HitShovelServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3250235443(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((KnifeItem)target).HitShovelClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "KnifeItem";
	}
}
