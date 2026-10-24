using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class Turret : NetworkBehaviour, IHittable, IIndoorMapHazard
{
	[Header("Effects")]
	public AudioSource mainAudio;

	[Header("Effects")]
	public AudioSource bulletCollisionAudio;

	[Header("Effects")]
	public AudioSource farAudio;

	public AudioClip firingSFX;

	public AudioClip chargingSFX;

	public AudioClip detectPlayerSFX;

	public AudioClip firingFarSFX;

	public AudioClip bulletsHitWallSFX;

	public AudioClip turretActivate;

	public AudioClip turretDeactivate;

	public ParticleSystem bulletParticles;

	public Animator turretAnimator;

	[Header("Variables")]
	public bool turretActive = true;

	[Space(5f)]
	public TurretMode turretMode;

	private TurretMode turretModeLastFrame;

	public Transform turretRod;

	public float targetRotation;

	public float rotationSpeed = 20f;

	public Transform turnTowardsObjectCompass;

	public Transform forwardFacingPos;

	public Transform aimPoint;

	public Transform centerPoint;

	public PlayerControllerB targetPlayerWithRotation;

	public Transform targetTransform;

	private bool targetingDeadPlayer;

	public float rotationRange = 45f;

	public float currentRotation;

	public bool rotatingOnInterval = true;

	private bool rotatingRight;

	private float switchRotationTimer;

	private bool hasLineOfSight;

	private float lostLOSTimer;

	private bool wasTargetingPlayerLastFrame;

	private RaycastHit hit;

	private int wallAndPlayerMask = 2824;

	private float turretInterval;

	private string previousHitLog;

	private bool rotatingSmoothly = true;

	private Ray shootRay;

	private Coroutine fadeBulletAudioCoroutine;

	public Transform tempTransform;

	private bool rotatingClockwise;

	private float berserkTimer;

	public AudioSource berserkAudio;

	private bool enteringBerserkMode;

	public IndoorMapHazardType mapHazardType;

	bool IIndoorMapHazard.IsActivated()
	{
		return turretActive;
	}

	IndoorMapHazardType IIndoorMapHazard.GetMapHazardType()
	{
		return mapHazardType;
	}

	bool IIndoorMapHazard.IsExtremelyHazardous()
	{
		if (turretMode != TurretMode.Charging && turretMode != TurretMode.Firing)
		{
			return enteringBerserkMode;
		}
		return true;
	}

	private void Start()
	{
		rotationRange = Mathf.Abs(rotationRange);
		rotationSpeed = 28f;
	}

	private IEnumerator FadeBulletAudio()
	{
		float initialVolume = bulletCollisionAudio.volume;
		for (int i = 0; i <= 30; i++)
		{
			yield return new WaitForSeconds(0.012f);
			bulletCollisionAudio.volume = Mathf.Lerp(initialVolume, 0f, (float)i / 30f);
		}
		bulletCollisionAudio.Stop();
	}

	private void Update()
	{
		if (!turretActive)
		{
			wasTargetingPlayerLastFrame = false;
			turretMode = TurretMode.Detection;
			targetPlayerWithRotation = null;
			return;
		}
		if (targetPlayerWithRotation != null)
		{
			if (!wasTargetingPlayerLastFrame)
			{
				wasTargetingPlayerLastFrame = true;
				if (turretMode == TurretMode.Detection)
				{
					turretMode = TurretMode.Charging;
				}
			}
			SetTargetToPlayerBody();
			TurnTowardsTargetIfHasLOS();
		}
		else if (wasTargetingPlayerLastFrame)
		{
			wasTargetingPlayerLastFrame = false;
			turretMode = TurretMode.Detection;
		}
		switch (turretMode)
		{
		case TurretMode.Detection:
			if (turretModeLastFrame != TurretMode.Detection)
			{
				turretModeLastFrame = TurretMode.Detection;
				rotatingClockwise = false;
				mainAudio.Stop();
				farAudio.Stop();
				berserkAudio.Stop();
				if (fadeBulletAudioCoroutine != null)
				{
					StopCoroutine(fadeBulletAudioCoroutine);
				}
				fadeBulletAudioCoroutine = StartCoroutine(FadeBulletAudio());
				bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				rotationSpeed = 28f;
				rotatingSmoothly = true;
				turretAnimator.SetInteger("TurretMode", 0);
				turretInterval = Random.Range(0f, 0.15f);
			}
			if (!IsServer)
			{
				break;
			}
			if (switchRotationTimer >= 7f)
			{
				switchRotationTimer = 0f;
				bool setRotateRight = !rotatingRight;
				SwitchRotationClientRpc(setRotateRight);
				SwitchRotationOnInterval(setRotateRight);
			}
			else
			{
				switchRotationTimer += Time.deltaTime;
			}
			if (turretInterval >= 0.25f)
			{
				turretInterval = 0f;
				PlayerControllerB playerControllerB = CheckForPlayersInLineOfSight(1.35f, angleRangeCheck: true);
				if (playerControllerB != null && !playerControllerB.isPlayerDead)
				{
					targetPlayerWithRotation = playerControllerB;
					SwitchTurretMode(1);
					SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId, setModeToCharging: true);
				}
			}
			else
			{
				turretInterval += Time.deltaTime;
			}
			break;
		case TurretMode.Charging:
			if (turretModeLastFrame != TurretMode.Charging)
			{
				turretModeLastFrame = TurretMode.Charging;
				rotatingClockwise = false;
				mainAudio.PlayOneShot(detectPlayerSFX);
				berserkAudio.Stop();
				WalkieTalkie.TransmitOneShotAudio(mainAudio, detectPlayerSFX);
				rotationSpeed = 95f;
				rotatingSmoothly = false;
				lostLOSTimer = 0f;
				turretAnimator.SetInteger("TurretMode", 1);
			}
			if (!IsServer)
			{
				break;
			}
			if (turretInterval >= 1.5f)
			{
				turretInterval = 0f;
				Debug.Log("Charging timer is up, setting to firing mode");
				if (!hasLineOfSight)
				{
					Debug.Log("hasLineOfSight is false");
					targetPlayerWithRotation = null;
					RemoveTargetedPlayerClientRpc();
				}
				else
				{
					SwitchTurretMode(2);
					SetToModeClientRpc(2);
				}
			}
			else
			{
				turretInterval += Time.deltaTime;
			}
			break;
		case TurretMode.Firing:
			if (turretModeLastFrame != TurretMode.Firing)
			{
				turretModeLastFrame = TurretMode.Firing;
				berserkAudio.Stop();
				RoundManager.Instance.PlayAudibleNoise(mainAudio.transform.position, 15f, 0.9f);
				mainAudio.clip = firingSFX;
				mainAudio.Play();
				farAudio.clip = firingFarSFX;
				farAudio.Play();
				bulletParticles.Play(withChildren: true);
				bulletCollisionAudio.Play();
				if (fadeBulletAudioCoroutine != null)
				{
					StopCoroutine(fadeBulletAudioCoroutine);
				}
				bulletCollisionAudio.volume = 1f;
				rotatingSmoothly = false;
				lostLOSTimer = 0f;
				turretAnimator.SetInteger("TurretMode", 2);
			}
			if (turretInterval >= 0.21f)
			{
				turretInterval = 0f;
				if (CheckForPlayersInLineOfSight(3f) == GameNetworkManager.Instance.localPlayerController)
				{
					if (GameNetworkManager.Instance.localPlayerController.health > 50)
					{
						GameNetworkManager.Instance.localPlayerController.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
					}
					else
					{
						GameNetworkManager.Instance.localPlayerController.KillPlayer(aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
					}
				}
				shootRay = new Ray(aimPoint.position, aimPoint.forward);
				if (Physics.Raycast(shootRay, out hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
				{
					bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
				}
			}
			else
			{
				turretInterval += Time.deltaTime;
			}
			break;
		case TurretMode.Berserk:
			if (turretModeLastFrame != TurretMode.Berserk)
			{
				turretModeLastFrame = TurretMode.Berserk;
				turretAnimator.SetInteger("TurretMode", 1);
				berserkTimer = 1.3f;
				berserkAudio.Play();
				rotationSpeed = 77f;
				enteringBerserkMode = true;
				rotatingSmoothly = true;
				lostLOSTimer = 0f;
				wasTargetingPlayerLastFrame = false;
				targetPlayerWithRotation = null;
			}
			if (enteringBerserkMode)
			{
				berserkTimer -= Time.deltaTime;
				if (berserkTimer <= 0f)
				{
					enteringBerserkMode = false;
					rotatingClockwise = true;
					berserkTimer = 9f;
					turretAnimator.SetInteger("TurretMode", 2);
					mainAudio.clip = firingSFX;
					mainAudio.Play();
					farAudio.clip = firingFarSFX;
					farAudio.Play();
					bulletParticles.Play(withChildren: true);
					bulletCollisionAudio.Play();
					if (fadeBulletAudioCoroutine != null)
					{
						StopCoroutine(fadeBulletAudioCoroutine);
					}
					bulletCollisionAudio.volume = 1f;
				}
				break;
			}
			if (turretInterval >= 0.21f)
			{
				turretInterval = 0f;
				if (CheckForPlayersInLineOfSight(3f) == GameNetworkManager.Instance.localPlayerController)
				{
					if (GameNetworkManager.Instance.localPlayerController.health > 50)
					{
						GameNetworkManager.Instance.localPlayerController.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots);
					}
					else
					{
						GameNetworkManager.Instance.localPlayerController.KillPlayer(aimPoint.forward * 40f, spawnBody: true, CauseOfDeath.Gunshots);
					}
				}
				shootRay = new Ray(aimPoint.position, aimPoint.forward);
				if (Physics.Raycast(shootRay, out hit, 30f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
				{
					bulletCollisionAudio.transform.position = shootRay.GetPoint(hit.distance - 0.5f);
				}
			}
			else
			{
				turretInterval += Time.deltaTime;
			}
			if (IsServer)
			{
				berserkTimer -= Time.deltaTime;
				if (berserkTimer <= 0f)
				{
					SwitchTurretMode(0);
					SetToModeClientRpc(0);
				}
			}
			break;
		}
		if (rotatingClockwise)
		{
			turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, turretRod.localEulerAngles.y - Time.deltaTime * 20f, 180f);
			turretRod.rotation = Quaternion.RotateTowards(turretRod.rotation, turnTowardsObjectCompass.rotation, rotationSpeed * Time.deltaTime);
			return;
		}
		if (rotatingSmoothly)
		{
			turnTowardsObjectCompass.localEulerAngles = new Vector3(-180f, Mathf.Clamp(targetRotation, 0f - rotationRange, rotationRange), 180f);
		}
		turretRod.rotation = Quaternion.RotateTowards(turretRod.rotation, turnTowardsObjectCompass.rotation, rotationSpeed * Time.deltaTime);
	}

	private void SetTargetToPlayerBody()
	{
		if (targetPlayerWithRotation.isPlayerDead)
		{
			if (!targetingDeadPlayer)
			{
				targetingDeadPlayer = true;
			}
			if (targetPlayerWithRotation.deadBody != null)
			{
				targetTransform = targetPlayerWithRotation.deadBody.bodyParts[5].transform;
			}
		}
		else
		{
			targetingDeadPlayer = false;
			targetTransform = targetPlayerWithRotation.gameplayCamera.transform;
		}
	}

	private void TurnTowardsTargetIfHasLOS()
	{
		bool flag = true;
		if (targetingDeadPlayer || Vector3.Angle(targetTransform.position - centerPoint.position, forwardFacingPos.forward) > rotationRange)
		{
			flag = false;
		}
		if (Physics.Linecast(aimPoint.position, targetTransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
		{
			flag = false;
		}
		if (flag)
		{
			hasLineOfSight = true;
			lostLOSTimer = 0f;
			tempTransform.position = targetTransform.position;
			tempTransform.position -= Vector3.up * 0.15f;
			turnTowardsObjectCompass.LookAt(tempTransform);
			return;
		}
		if (hasLineOfSight)
		{
			hasLineOfSight = false;
			lostLOSTimer = 0f;
		}
		if (!IsServer)
		{
			return;
		}
		lostLOSTimer += Time.deltaTime;
		if (lostLOSTimer >= 2f)
		{
			lostLOSTimer = 0f;
			Debug.Log("Turret: LOS timer ended on server. checking for new player target");
			PlayerControllerB playerControllerB = CheckForPlayersInLineOfSight();
			if (playerControllerB != null)
			{
				targetPlayerWithRotation = playerControllerB;
				SwitchTargetedPlayerClientRpc((int)playerControllerB.playerClientId);
				Debug.Log("Turret: Got new player target");
			}
			else
			{
				Debug.Log("Turret: No new player to target; returning to detection mode.");
				targetPlayerWithRotation = null;
				RemoveTargetedPlayerClientRpc();
			}
		}
	}

	public PlayerControllerB CheckForPlayersInLineOfSight(float radius = 2f, bool angleRangeCheck = false)
	{
		Vector3 forward = aimPoint.forward;
		forward = Quaternion.Euler(0f, (float)(int)(0f - rotationRange) / radius, 0f) * forward;
		float num = rotationRange / radius * 2f;
		for (int i = 0; i <= 6; i++)
		{
			shootRay = new Ray(centerPoint.position, forward);
			if (Physics.Raycast(shootRay, out hit, 30f, 1051400, QueryTriggerInteraction.Ignore))
			{
				if (hit.transform.CompareTag("Player"))
				{
					PlayerControllerB component = hit.transform.GetComponent<PlayerControllerB>();
					if (!(component == null))
					{
						if (angleRangeCheck && Vector3.Angle(component.transform.position + Vector3.up * 1.75f - centerPoint.position, forwardFacingPos.forward) > rotationRange)
						{
							return null;
						}
						return component;
					}
					continue;
				}
				if ((turretMode == TurretMode.Firing || (turretMode == TurretMode.Berserk && !enteringBerserkMode)) && hit.transform.tag.StartsWith("PlayerRagdoll"))
				{
					Rigidbody component2 = hit.transform.GetComponent<Rigidbody>();
					if (component2 != null)
					{
						component2.AddForce(forward.normalized * 42f, ForceMode.Impulse);
					}
				}
			}
			forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
		}
		return null;
	}

	[ClientRpc]
	public void SwitchRotationClientRpc(bool setRotateRight)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2426770061u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setRotateRight, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2426770061u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				SwitchRotationOnInterval(setRotateRight);
			}
		}
	}

	public void SwitchRotationOnInterval(bool setRotateRight)
	{
		if (rotatingRight)
		{
			rotatingRight = false;
			targetRotation = rotationRange;
		}
		else
		{
			rotatingRight = true;
			targetRotation = 0f - rotationRange;
		}
	}

	[ClientRpc]
	public void SwitchTargetedPlayerClientRpc(int playerId, bool setModeToCharging = false)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(866050294u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			bufferWriter.WriteValueSafe(in setModeToCharging, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 866050294u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (!IsServer)
		{
			targetPlayerWithRotation = StartOfRound.Instance.allPlayerScripts[playerId];
			if (setModeToCharging)
			{
				SwitchTurretMode(1);
			}
		}
	}

	[ClientRpc]
	public void RemoveTargetedPlayerClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(2800017671u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 2800017671u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				targetPlayerWithRotation = null;
			}
		}
	}

	[ClientRpc]
	public void SetToModeClientRpc(int mode)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3335553538u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, mode);
			__endSendClientRpc(ref bufferWriter, 3335553538u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				SwitchTurretMode(mode);
			}
		}
	}

	private void SwitchTurretMode(int mode)
	{
		turretMode = (TurretMode)mode;
	}

	public void ToggleTurretEnabled(bool enabled)
	{
		if (turretActive != enabled)
		{
			ToggleTurretEnabledLocalClient(enabled);
			ToggleTurretServerRpc(enabled);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ToggleTurretServerRpc(bool enabled)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2339273208u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in enabled, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 2339273208u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				ToggleTurretClientRpc(enabled);
			}
		}
	}

	[ClientRpc]
	public void ToggleTurretClientRpc(bool enabled)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1135819343u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in enabled, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1135819343u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (turretActive != enabled)
			{
				ToggleTurretEnabledLocalClient(enabled);
			}
		}
	}

	private void ToggleTurretEnabledLocalClient(bool enabled)
	{
		turretActive = enabled;
		turretAnimator.SetBool("turretActive", turretActive);
		if (enabled)
		{
			mainAudio.PlayOneShot(turretActivate);
			WalkieTalkie.TransmitOneShotAudio(mainAudio, turretActivate);
		}
		else
		{
			mainAudio.PlayOneShot(turretDeactivate);
			WalkieTalkie.TransmitOneShotAudio(mainAudio, turretDeactivate);
		}
	}

	bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID = -1)
	{
		if (turretMode == TurretMode.Berserk || turretMode == TurretMode.Firing)
		{
			return false;
		}
		SwitchTurretMode(3);
		EnterBerserkModeServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		return true;
	}

	[ServerRpc(RequireOwnership = false)]
	public void EnterBerserkModeServerRpc(int playerWhoTriggered)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(4195711963u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
				__endSendServerRpc(ref bufferWriter, 4195711963u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				EnterBerserkModeClientRpc(playerWhoTriggered);
			}
		}
	}

	[ClientRpc]
	public void EnterBerserkModeClientRpc(int playerWhoTriggered)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1436540455u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
			__endSendClientRpc(ref bufferWriter, 1436540455u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (playerWhoTriggered != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				SwitchTurretMode(3);
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2426770061u, __rpc_handler_2426770061, "SwitchRotationClientRpc");
		__registerRpc(866050294u, __rpc_handler_866050294, "SwitchTargetedPlayerClientRpc");
		__registerRpc(2800017671u, __rpc_handler_2800017671, "RemoveTargetedPlayerClientRpc");
		__registerRpc(3335553538u, __rpc_handler_3335553538, "SetToModeClientRpc");
		__registerRpc(2339273208u, __rpc_handler_2339273208, "ToggleTurretServerRpc");
		__registerRpc(1135819343u, __rpc_handler_1135819343, "ToggleTurretClientRpc");
		__registerRpc(4195711963u, __rpc_handler_4195711963, "EnterBerserkModeServerRpc");
		__registerRpc(1436540455u, __rpc_handler_1436540455, "EnterBerserkModeClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2426770061(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).SwitchRotationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_866050294(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).SwitchTargetedPlayerClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2800017671(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).RemoveTargetedPlayerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3335553538(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).SetToModeClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2339273208(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).ToggleTurretServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1135819343(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).ToggleTurretClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4195711963(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).EnterBerserkModeServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1436540455(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((Turret)target).EnterBerserkModeClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "Turret";
	}
}
