using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

public class VehicleController : NetworkBehaviour
{
	public int vehicleID;

	[Header("Vehicle Physics")]
	public WheelCollider FrontLeftWheel;

	public WheelCollider FrontRightWheel;

	public WheelCollider BackLeftWheel;

	public WheelCollider BackRightWheel;

	public WheelCollider[] otherWheels;

	public Rigidbody mainRigidbody;

	public Transform[] driverSideExitPoints;

	public Transform[] passengerSideExitPoints;

	public InteractTrigger driverSeatTrigger;

	public InteractTrigger passengerSeatTrigger;

	public float stability = 0.3f;

	public float speed = 2f;

	public float EngineTorque = 230f;

	public float MaxEngineRPM = 3000f;

	public float MinEngineRPM = 1000f;

	public float EngineRPM;

	[Header("Vehicle Control")]
	public bool ignitionStarted;

	public Vector2 moveInputVector;

	public float steeringInput;

	public float steeringWheelTurnSpeed = 5f;

	public float carAcceleration = 5f;

	public float carMaxSpeed = 7f;

	public float brakeSpeed = 5f;

	public float idleSpeed = 2f;

	[Space(5f)]
	[Header("Car Damage")]
	public float carFragility = 1f;

	public float minimalBumpForce = 10f;

	public float mediumBumpForce = 40f;

	public float maximumBumpForce = 80f;

	public int baseCarHP;

	public int carHP = 12;

	private float timeAtLastDamage;

	public bool carDestroyed;

	[Space(5f)]
	[Header("Effects")]
	public MeshRenderer leftWheelMesh;

	public MeshRenderer rightWheelMesh;

	public MeshRenderer backLeftWheelMesh;

	public MeshRenderer backRightWheelMesh;

	public Animator steeringWheelAnimator;

	public Animator gearStickAnimator;

	public AudioSource engineAudio1;

	public AudioSource engineAudio2;

	public AudioSource rollingAudio;

	public AudioSource turbulenceAudio;

	private float turbulenceAmount;

	public bool carEngine1AudioActive;

	public bool carEngine2AudioActive;

	public bool carRollingAudioActive;

	public AudioClip revEngineStart;

	public AudioClip engineStartSuccessful;

	public AudioClip engineRun;

	public AudioClip engineRev;

	public AudioClip engineRun2;

	public AudioClip insertKey;

	public AudioClip twistKey;

	public AudioClip removeKey;

	public AudioClip sitDown;

	public AudioSource tireAudio;

	public AudioSource vehicleEngineAudio;

	public AudioSource steeringWheelAudio;

	public AudioSource skiddingAudio;

	public AudioSource gearStickAudio;

	public AudioClip[] gearStickAudios;

	public AnimatedObjectTrigger driverSideDoor;

	public AnimatedObjectTrigger passengerSideDoor;

	public InteractTrigger driverSideDoorTrigger;

	public InteractTrigger passengerSideDoorTrigger;

	public PlayerInput input;

	public bool localPlayerInControl;

	public bool localPlayerInPassengerSeat;

	public bool drivePedalPressed;

	public bool brakePedalPressed;

	public CarGearShift gear;

	public float gearStickAnimValue;

	public float steeringAnimValue;

	private float steeringWheelAnimFloat;

	public float carStress;

	public float carStressChange;

	public PlayerControllerB currentDriver;

	public PlayerControllerB currentPassenger;

	public bool testingVehicleInEditor;

	public float engineIntensityPercentage = 350f;

	public Vector3 syncedPosition;

	public Quaternion syncedRotation;

	public float syncSpeedMultiplier = 2f;

	public float syncRotationSpeed = 1f;

	public float syncCarPositionInterval;

	private bool enabledCollisionForAllPlayers = true;

	public ContactPoint[] contacts;

	public PlayerPhysicsRegion physicsRegion;

	private int exitCarLayerMask = 2305;

	private Coroutine keyIgnitionCoroutine;

	public MeshRenderer keyObject;

	public Transform ignitionTurnedPosition;

	public Transform ignitionNotTurnedPosition;

	private bool keyIsInDriverHand;

	private bool keyIsInIgnition;

	public Vector3 positionOffset;

	public Vector3 rotationOffset;

	public GameObject startKeyIgnitionTrigger;

	public GameObject removeKeyIgnitionTrigger;

	private float chanceToStartIgnition = 1f;

	private float timeAtLastGearShift;

	private RaycastHit hit;

	public AudioSource hoodAudio;

	public AudioSource pushAudio;

	public AudioSource collisionAudio1;

	public AudioSource collisionAudio2;

	public AudioClip[] maxCollisions;

	public AudioClip[] medCollisions;

	public AudioClip[] minCollisions;

	public AudioClip[] obstacleCollisions;

	public AudioClip windshieldBreak;

	public AudioSource miscAudio;

	private float audio1Time;

	private float audio2Time;

	private int audio1Type;

	private int audio2Type;

	public MeshRenderer mainBodyMesh;

	public MeshRenderer lod1Mesh;

	public MeshRenderer lod2Mesh;

	public Material windshieldBrokenMat;

	public ParticleSystem glassParticle;

	private bool windshieldBroken;

	private Vector3 truckVelocityLastFrame;

	public bool useVel;

	[Header("Radio")]
	public AudioClip[] radioClips;

	public AudioSource radioAudio;

	public AudioSource radioInterference;

	private float radioSignalQuality = 3f;

	private float radioSignalDecreaseThreshold = 50f;

	public float radioSignalTurbulence = 4f;

	private float changeRadioSignalTime;

	private bool radioOn;

	private int currentRadioClip;

	private float currentSongTime;

	private bool radioTurnedOnBefore;

	public float carHitPlayerForceFraction = 30f;

	public float carReactToPlayerHitMultiplier;

	public ParticleSystem carExhaustParticle;

	public Vector3 averageVelocity;

	public int movingAverageLength = 20;

	public int averageCount;

	private DecalProjector[] decals;

	private int decalIndex;

	public ParticleSystem hoodFireParticle;

	public AudioSource hoodFireAudio;

	private bool isHoodOnFire;

	private bool carHoodOpen;

	public Animator carHoodAnimator;

	private AudioClip carHoodOpenSFX;

	private AudioClip carHoodCloseSFX;

	public GameObject backDoorContainer;

	public GameObject destroyedTruckMesh;

	public GameObject truckDestroyedExplosion;

	private bool hoodPoppedUp;

	public float pushForceMultiplier;

	public float pushVerticalOffsetAmount;

	public GameObject headlightsContainer;

	public Material headlightsOnMat;

	public Material headlightsOffMat;

	public AudioClip headlightsToggleSFX;

	public bool magnetedToShip;

	private Vector3 magnetTargetPosition;

	private Quaternion magnetTargetRotation;

	private Quaternion magnetStartRotation;

	private Vector3 magnetStartPosition;

	public float magnetTime;

	private bool finishedMagneting;

	private bool loadedVehicleFromSave;

	private float magnetRotationTime;

	public BoxCollider boundsCollider;

	private Vector3 averageVelocityAtMagnetStart;

	public AnimationCurve magnetPositionCurve;

	public AnimationCurve magnetRotationCurve;

	private bool destroyNextFrame;

	private string lastStressType;

	private string lastDamageType;

	private float stressPerSecond;

	public Rigidbody ragdollPhysicsBody;

	public Rigidbody windwiperPhysicsBody1;

	public Rigidbody windwiperPhysicsBody2;

	public Transform windwiper1;

	public Transform windwiper2;

	public BoxCollider windshieldPhysicsCollider;

	public Animator driverSeatSpringAnimator;

	public float timeSinceSpringingDriverSeat;

	public AudioSource springAudio;

	public float springForce = 70f;

	public GameObject backLightsContainer;

	public GameObject frontCabinLightContainer;

	public MeshRenderer backLightsMesh;

	public MeshRenderer frontCabinLightMesh;

	public Material backLightOnMat;

	public bool backLightsOn;

	public AudioSource hornAudio;

	public bool honkingHorn;

	private float timeAtLastHornPing;

	private float timeAtLastEngineAudioPing;

	public float torqueForce;

	public bool backDoorOpen;

	public bool hasBeenSpawned;

	public bool inDropshipAnimation;

	private ItemDropship itemShip;

	public ParticleSystem tireSparks;

	public AudioSource extremeStressAudio;

	private bool underExtremeStress;

	private bool syncedExtremeStress;

	public Transform healthMeter;

	public Transform turboMeter;

	public Transform clipboardPosition;

	private float limitTruckVelocityTimer;

	[Header("Boost ability")]
	private bool switchGearsBoostEnabled;

	private int turboBoosts;

	private bool pressedTurbo;

	public float turboBoostForce;

	public float turboBoostUpwardForce;

	public ParticleSystem turboBoostParticle;

	public AudioSource turboBoostAudio;

	public AudioClip turboBoostSFX;

	public AudioClip turboBoostSFX2;

	public Transform oilPipePoint;

	private bool jumpingInCar;

	public float jumpForce = 4000f;

	public AudioClip jumpInCarSFX;

	private string[] carTooltips = new string[3] { "Gas pedal: [W]", "Brake pedal: [S]", "Boost: [Space]" };

	public AudioClip pourOil;

	public AudioClip pourTurbo;

	private bool setControlTips;

	public Collider ontopOfTruckCollider;

	public void SetBackDoorOpen(bool open)
	{
		backDoorOpen = open;
	}

	private void SetFrontCabinLightOn(bool setOn)
	{
		frontCabinLightContainer.SetActive(setOn);
		if (setOn)
		{
			frontCabinLightMesh.material = headlightsOnMat;
		}
		else
		{
			frontCabinLightMesh.material = headlightsOffMat;
		}
	}

	private void SetWheelFriction()
	{
		WheelFrictionCurve wheelFrictionCurve = new WheelFrictionCurve
		{
			extremumSlip = 0.2f,
			extremumValue = 1f,
			asymptoteSlip = 0.8f,
			asymptoteValue = 1f,
			stiffness = 2f
		};
		FrontRightWheel.forwardFriction = wheelFrictionCurve;
		FrontLeftWheel.forwardFriction = wheelFrictionCurve;
		wheelFrictionCurve.stiffness = 0.75f;
		BackRightWheel.forwardFriction = wheelFrictionCurve;
		BackLeftWheel.forwardFriction = wheelFrictionCurve;
		wheelFrictionCurve.stiffness = 2f;
		wheelFrictionCurve.asymptoteValue = 1f;
		wheelFrictionCurve.extremumSlip = 0.7f;
		FrontRightWheel.sidewaysFriction = wheelFrictionCurve;
		FrontLeftWheel.sidewaysFriction = wheelFrictionCurve;
		BackRightWheel.sidewaysFriction = wheelFrictionCurve;
		BackLeftWheel.sidewaysFriction = wheelFrictionCurve;
	}

	private void Awake()
	{
		contacts = new ContactPoint[24];
		ragdollPhysicsBody.transform.SetParent(RoundManager.Instance.VehiclesContainer);
		windwiperPhysicsBody1.transform.SetParent(RoundManager.Instance.VehiclesContainer);
		windwiperPhysicsBody2.transform.SetParent(RoundManager.Instance.VehiclesContainer);
		mainRigidbody.maxLinearVelocity = 50f;
		mainRigidbody.maxAngularVelocity = 4f;
		syncedPosition = transform.position;
		syncedRotation = transform.rotation;
	}

	private void Start()
	{
		carHP = baseCarHP;
		SetWheelFriction();
		FrontLeftWheel.brakeTorque = 1000f;
		FrontRightWheel.brakeTorque = 1000f;
		BackLeftWheel.brakeTorque = 1000f;
		BackRightWheel.brakeTorque = 1000f;
		if (testingVehicleInEditor)
		{
			ActivateControl();
		}
		Debug.Log($"Max linear velocity: {mainRigidbody.maxLinearVelocity}; dep: {mainRigidbody.maxDepenetrationVelocity}");
		mainRigidbody.automaticCenterOfMass = false;
		mainRigidbody.centerOfMass += new Vector3(0f, -1f, 0.25f);
		mainRigidbody.automaticInertiaTensor = false;
		mainRigidbody.maxDepenetrationVelocity = 1f;
		currentRadioClip = new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, radioClips.Length);
		decals = new DecalProjector[24];
		if (StartOfRound.Instance.inShipPhase)
		{
			hasBeenSpawned = true;
			magnetedToShip = true;
			loadedVehicleFromSave = true;
			transform.position = StartOfRound.Instance.magnetPoint.position + StartOfRound.Instance.magnetPoint.forward * 7f;
			StartMagneting();
		}
	}

	public void RemoveKeyFromIgnition()
	{
		if (localPlayerInControl)
		{
			if (keyIgnitionCoroutine != null)
			{
				StopCoroutine(keyIgnitionCoroutine);
			}
			keyIgnitionCoroutine = StartCoroutine(RemoveKey());
			GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 6);
			RemoveKeyFromIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void RemoveKeyFromIgnitionServerRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(269855870u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, driverId);
				__endSendServerRpc(ref bufferWriter, 269855870u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				RemoveKeyFromIgnitionClientRpc(driverId);
			}
		}
	}

	[ClientRpc]
	public void RemoveKeyFromIgnitionClientRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1127926854u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, driverId);
			__endSendClientRpc(ref bufferWriter, 1127926854u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != driverId)
		{
			if (keyIgnitionCoroutine != null)
			{
				StopCoroutine(keyIgnitionCoroutine);
			}
			keyIgnitionCoroutine = StartCoroutine(RemoveKey());
		}
	}

	private IEnumerator RemoveKey()
	{
		yield return new WaitForSeconds(0.26f);
		if (currentDriver != null)
		{
			currentDriver.movementAudio.PlayOneShot(removeKey);
		}
		keyIsInDriverHand = true;
		SetIgnition(started: false);
		SetFrontCabinLightOn(setOn: false);
		yield return new WaitForSeconds(0.73f);
		keyIsInDriverHand = false;
		keyIgnitionCoroutine = null;
	}

	public void CancelTryCarIgnition()
	{
		if (localPlayerInControl && !ignitionStarted)
		{
			if (keyIsInIgnition)
			{
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 3);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 0);
			}
			carEngine1AudioActive = false;
			CancelIgnitionAnimation();
			CancelTryIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, keyIsInIgnition);
		}
	}

	public void StartTryCarIgnition()
	{
		if (localPlayerInControl && !ignitionStarted)
		{
			if (keyIgnitionCoroutine != null)
			{
				StopCoroutine(keyIgnitionCoroutine);
			}
			keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: true));
			TryIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, keyIsInIgnition);
		}
	}

	private IEnumerator TryIgnition(bool isLocalDriver)
	{
		Debug.Log("Starting ignition!!!");
		if (keyIsInIgnition)
		{
			if (isLocalDriver)
			{
				if (GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.GetInteger("SA_CarAnim") == 3)
				{
					GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 2);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 12);
				}
			}
			yield return new WaitForSeconds(0.035f);
			keyIsInDriverHand = true;
			currentDriver.movementAudio.PlayOneShot(twistKey);
			if (!isLocalDriver)
			{
				keyIgnitionCoroutine = null;
				yield break;
			}
			yield return new WaitForSeconds(0.1467f);
		}
		else
		{
			if (isLocalDriver)
			{
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 2);
			}
			keyIsInDriverHand = true;
			yield return new WaitForSeconds(0.66f);
			currentDriver.movementAudio.PlayOneShot(insertKey);
			if (!isLocalDriver)
			{
				keyIgnitionCoroutine = null;
				yield break;
			}
			yield return new WaitForSeconds(0.3f);
		}
		keyIsInIgnition = true;
		SetFrontCabinLightOn(keyIsInIgnition);
		RevCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		engineAudio1.clip = revEngineStart;
		engineAudio1.volume = 0.7f;
		engineAudio1.PlayOneShot(engineRev);
		carEngine1AudioActive = true;
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.4f, 1.1f));
		if ((float)UnityEngine.Random.Range(0, 100) < chanceToStartIgnition)
		{
			SetIgnition(started: true);
			keyIsInDriverHand = false;
			GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 1);
			StartIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
		else
		{
			chanceToStartIgnition += 24f;
		}
		keyIgnitionCoroutine = null;
	}

	[ServerRpc(RequireOwnership = false)]
	public void RevCarServerRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1319663544u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, driverId);
				__endSendServerRpc(ref bufferWriter, 1319663544u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				RevCarClientRpc(driverId);
			}
		}
	}

	[ClientRpc]
	public void RevCarClientRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3214494774u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, driverId);
			__endSendClientRpc(ref bufferWriter, 3214494774u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != driverId)
			{
				engineAudio1.clip = revEngineStart;
				engineAudio1.volume = 0.7f;
				engineAudio1.PlayOneShot(engineRev);
				carEngine1AudioActive = true;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StartIgnitionServerRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2403570091u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, driverId);
				__endSendServerRpc(ref bufferWriter, 2403570091u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StartIgnitionClientRpc(driverId);
			}
		}
	}

	[ClientRpc]
	public void StartIgnitionClientRpc(int driverId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3273216474u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, driverId);
			__endSendClientRpc(ref bufferWriter, 3273216474u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != driverId)
			{
				SetIgnition(started: true);
				CancelIgnitionAnimation();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TryIgnitionServerRpc(int driverId, bool keyIsIn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(835626980u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, driverId);
				bufferWriter.WriteValueSafe(in keyIsIn, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 835626980u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				TryIgnitionClientRpc(driverId, keyIsIn);
			}
		}
	}

	[ClientRpc]
	public void TryIgnitionClientRpc(int driverId, bool keyIsIn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3548459446u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, driverId);
			bufferWriter.WriteValueSafe(in keyIsIn, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3548459446u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != driverId && !ignitionStarted)
		{
			keyIsInIgnition = keyIsIn;
			if (keyIgnitionCoroutine != null)
			{
				StopCoroutine(keyIgnitionCoroutine);
			}
			keyIgnitionCoroutine = StartCoroutine(TryIgnition(isLocalDriver: false));
			if (!keyIsInIgnition)
			{
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 0);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 3);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CancelTryIgnitionServerRpc(int driverId, bool setKeyInSlot)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(4283235241u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, driverId);
				bufferWriter.WriteValueSafe(in setKeyInSlot, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 4283235241u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				CancelTryIgnitionClientRpc(driverId, setKeyInSlot);
			}
		}
	}

	[ClientRpc]
	public void CancelTryIgnitionClientRpc(int driverId, bool setKeyInSlot)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2096620717u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, driverId);
			bufferWriter.WriteValueSafe(in setKeyInSlot, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2096620717u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != driverId)
		{
			if (ignitionStarted)
			{
				SetIgnition(started: false);
			}
			if (keyIgnitionCoroutine != null)
			{
				StopCoroutine(keyIgnitionCoroutine);
			}
			carEngine1AudioActive = false;
			keyIsInIgnition = setKeyInSlot;
			SetFrontCabinLightOn(keyIsInIgnition);
		}
	}

	public void CancelIgnitionAnimation()
	{
		if (keyIgnitionCoroutine != null)
		{
			StopCoroutine(keyIgnitionCoroutine);
			keyIgnitionCoroutine = null;
		}
		keyIsInDriverHand = false;
	}

	public void SetIgnition(bool started)
	{
		keyIsInIgnition = started;
		SetFrontCabinLightOn(keyIsInIgnition);
		if (started)
		{
			startKeyIgnitionTrigger.SetActive(value: false);
			removeKeyIgnitionTrigger.SetActive(value: true);
		}
		else
		{
			startKeyIgnitionTrigger.SetActive(value: true);
			removeKeyIgnitionTrigger.SetActive(value: false);
		}
		if (started != ignitionStarted)
		{
			ignitionStarted = started;
			carEngine1AudioActive = started;
			if (started)
			{
				carExhaustParticle.Play();
				engineAudio1.PlayOneShot(engineStartSuccessful);
				engineAudio1.clip = engineRun;
			}
			else
			{
				carExhaustParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			}
		}
	}

	public void ExitDriverSideSeat()
	{
		Debug.Log($"local player in control? : {localPlayerInControl}");
		if (!localPlayerInControl)
		{
			return;
		}
		GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 0);
		int num = CanExitCar(passengerSide: true);
		if (num != -1)
		{
			GameNetworkManager.Instance.localPlayerController.TeleportPlayer(driverSideExitPoints[num].position);
			return;
		}
		if (!driverSideDoor.boolValue)
		{
			driverSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
		}
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(driverSideExitPoints[1].position);
	}

	public void OnPassengerExit()
	{
		passengerSeatTrigger.interactable = true;
		localPlayerInPassengerSeat = false;
		currentPassenger = null;
		SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
		PassengerLeaveVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, GameNetworkManager.Instance.localPlayerController.transform.position);
	}

	public void ExitPassengerSideSeat()
	{
		if (localPlayerInPassengerSeat)
		{
			int num = CanExitCar(passengerSide: true);
			if (num != -1)
			{
				GameNetworkManager.Instance.localPlayerController.TeleportPlayer(passengerSideExitPoints[num].position);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.TeleportPlayer(passengerSideExitPoints[1].position);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PassengerLeaveVehicleServerRpc(int playerId, Vector3 exitPoint)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2150817317u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				bufferWriter.WriteValueSafe(in exitPoint);
				__endSendServerRpc(ref bufferWriter, 2150817317u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PassengerLeaveVehicleClientRpc(playerId, exitPoint);
			}
		}
	}

	[ClientRpc]
	public void PassengerLeaveVehicleClientRpc(int playerId, Vector3 exitPoint)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(46680660u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			bufferWriter.WriteValueSafe(in exitPoint);
			__endSendClientRpc(ref bufferWriter, 46680660u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
		if (!(playerControllerB == GameNetworkManager.Instance.localPlayerController))
		{
			playerControllerB.TeleportPlayer(exitPoint);
			currentPassenger = null;
			if (!IsOwner)
			{
				SetVehicleCollisionForPlayer(setEnabled: true, GameNetworkManager.Instance.localPlayerController);
			}
			passengerSeatTrigger.interactable = true;
		}
	}

	public void SetPlayerInCar(PlayerControllerB player)
	{
		player.movementAudio.PlayOneShot(sitDown);
	}

	public void SetPassengerInCar(PlayerControllerB player)
	{
		if (passengerSideDoor.boolValue)
		{
			passengerSideDoor.SetBoolOnClientOnly(setTo: false);
		}
		if (player == GameNetworkManager.Instance.localPlayerController)
		{
			localPlayerInPassengerSeat = true;
		}
		else
		{
			passengerSeatTrigger.interactable = false;
		}
		currentPassenger = player;
	}

	private int CanExitCar(bool passengerSide)
	{
		if (passengerSide)
		{
			for (int i = 0; i < driverSideExitPoints.Length; i++)
			{
				if (!Physics.Linecast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, driverSideExitPoints[i].position, exitCarLayerMask, QueryTriggerInteraction.Ignore))
				{
					return i;
				}
			}
			return -1;
		}
		for (int j = 0; j < passengerSideExitPoints.Length; j++)
		{
			if (!Physics.Linecast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, passengerSideExitPoints[j].position, exitCarLayerMask, QueryTriggerInteraction.Ignore))
			{
				return j;
			}
		}
		return -1;
	}

	public void TakeControlOfVehicle()
	{
		if (currentDriver != null)
		{
			GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
			return;
		}
		if (driverSideDoor.boolValue)
		{
			driverSideDoor.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
		}
		ActivateControl();
		GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetFloat("animationSpeed", 0.5f);
		if (ignitionStarted)
		{
			GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 1);
		}
		else
		{
			GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 0);
		}
		driverSideDoorTrigger.hoverTip = "Exit : [LMB]";
		SetVehicleCollisionForPlayer(setEnabled: false, GameNetworkManager.Instance.localPlayerController);
		if (!testingVehicleInEditor)
		{
			SetPlayerInControlOfVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	public void LoseControlOfVehicle()
	{
		driverSideDoorTrigger.hoverTip = "Use door : [LMB]";
		GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 0);
		localPlayerInControl = false;
		DisableVehicleCollisionForAllPlayers();
		if (averageVelocity.magnitude < 3f)
		{
			limitTruckVelocityTimer = 0.7f;
		}
		if (!(currentDriver != GameNetworkManager.Instance.localPlayerController))
		{
			DisableControl();
			steeringAnimValue = 0f;
			keyIsInDriverHand = false;
			CancelIgnitionAnimation();
			chanceToStartIgnition = 20f;
			if (!testingVehicleInEditor)
			{
				RemovePlayerControlOfVehicleServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, transform.position, transform.rotation, ignitionStarted);
			}
			if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null)
			{
				GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.SetControlTipsForItem();
			}
			else
			{
				HUDManager.Instance.ClearControlTips();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetPlayerInControlOfVehicleServerRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(2687785832u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 2687785832u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
		if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled)
		{
			return;
		}
		if (currentDriver != null && !playerControllerB.IsServer)
		{
			CancelPlayerInControlOfVehicleClientRpc(playerId);
			return;
		}
		currentDriver = StartOfRound.Instance.allPlayerScripts[playerId];
		NetworkObject.ChangeOwnership(StartOfRound.Instance.allPlayerScripts[playerId].actualClientId);
		if (averageVelocity.magnitude < 3f)
		{
			limitTruckVelocityTimer = 0.7f;
		}
		SetPlayerInControlOfVehicleClientRpc(playerId);
	}

	[ClientRpc]
	public void CancelPlayerInControlOfVehicleClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1621098866u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 1621098866u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (playerId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				ExitDriverSideSeat();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void RemovePlayerControlOfVehicleServerRpc(int playerId, Vector3 carLocation, Quaternion carRotation, bool setKeyInIgnition)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(2345405857u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			bufferWriter.WriteValueSafe(in carLocation);
			bufferWriter.WriteValueSafe(in carRotation);
			bufferWriter.WriteValueSafe(in setKeyInIgnition, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2345405857u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!(StartOfRound.Instance.allPlayerScripts[playerId] == null))
			{
				syncedPosition = carLocation;
				syncedRotation = carRotation;
				NetworkObject.ChangeOwnership(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				RemovePlayerControlOfVehicleClientRpc(playerId, setKeyInIgnition);
			}
		}
	}

	[ClientRpc]
	public void SetPlayerInControlOfVehicleClientRpc(int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2569589533u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 2569589533u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
		{
			if (averageVelocity.magnitude < 3f)
			{
				limitTruckVelocityTimer = 0.7f;
			}
			Debug.Log($"Set player in control: {playerId} ");
			currentDriver = StartOfRound.Instance.allPlayerScripts[playerId];
			StartOfRound.Instance.allPlayerScripts[playerId].playerBodyAnimator.SetFloat("animationSpeed", 0.5f);
			SetVehicleCollisionForPlayer(setEnabled: false, currentDriver);
		}
	}

	[ClientRpc]
	public void RemovePlayerControlOfVehicleClientRpc(int playerId, bool setIgnitionStarted)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1936869671u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			bufferWriter.WriteValueSafe(in setIgnitionStarted, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1936869671u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				CancelIgnitionAnimation();
				SetIgnition(setIgnitionStarted);
				currentDriver = null;
				steeringAnimValue = 0f;
			}
		}
	}

	public void DisableVehicleCollisionForAllPlayers()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (!localPlayerInControl && StartOfRound.Instance.allPlayerScripts[i] == GameNetworkManager.Instance.localPlayerController)
			{
				StartOfRound.Instance.allPlayerScripts[i].GetComponent<CharacterController>().excludeLayers = 0;
			}
			else
			{
				StartOfRound.Instance.allPlayerScripts[i].GetComponent<CharacterController>().excludeLayers = 1073741824;
			}
		}
	}

	public void EnableVehicleCollisionForAllPlayers()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i] != currentPassenger)
			{
				StartOfRound.Instance.allPlayerScripts[i].GetComponent<CharacterController>().excludeLayers = 0;
			}
		}
	}

	public void SetVehicleCollisionForPlayer(bool setEnabled, PlayerControllerB player)
	{
		if (!setEnabled)
		{
			player.GetComponent<CharacterController>().excludeLayers = 1073741824;
		}
		else
		{
			player.GetComponent<CharacterController>().excludeLayers = 0;
		}
	}

	private void ActivateControl()
	{
		InputActionAsset inputActionAsset = ((!testingVehicleInEditor) ? InputSystem.actions : input.actions);
		inputActionAsset.FindAction("Jump").performed += DoTurboBoost;
		localPlayerInControl = true;
		if (!testingVehicleInEditor)
		{
			currentDriver = GameNetworkManager.Instance.localPlayerController;
		}
	}

	private void DisableControl()
	{
		InputActionAsset inputActionAsset = ((!testingVehicleInEditor) ? InputSystem.actions : input.actions);
		inputActionAsset.FindAction("Jump").performed -= DoTurboBoost;
		localPlayerInControl = false;
		drivePedalPressed = false;
		brakePedalPressed = false;
		currentDriver = null;
	}

	public void ShiftGearForward()
	{
		if (gear != CarGearShift.Park)
		{
			if (gear == CarGearShift.Reverse)
			{
				Debug.Log("Switching gears forward; 3");
				ShiftToGear(3);
			}
			else if (gear == CarGearShift.Drive)
			{
				Debug.Log("Switching gears forward; 2");
				ShiftToGear(2);
			}
		}
	}

	private void ShiftGearForwardInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			ShiftGearForward();
		}
	}

	private void ShiftGearBack()
	{
		if (gear != CarGearShift.Drive)
		{
			if (gear == CarGearShift.Park)
			{
				Debug.Log("Switching gears; 2");
				ShiftToGear(2);
			}
			else if (gear == CarGearShift.Reverse)
			{
				Debug.Log("Switching gears; 1");
				ShiftToGear(1);
			}
		}
	}

	private void ShiftToGear(int setGear)
	{
		gear = (CarGearShift)setGear;
		gearStickAudio.PlayOneShot(gearStickAudios[setGear - 1]);
		if (gear != CarGearShift.Park)
		{
			Debug.Log("Enabling switch gears boost");
			switchGearsBoostEnabled = true;
		}
		else
		{
			Debug.Log("Disabling switch gears boost");
			switchGearsBoostEnabled = false;
		}
	}

	public void ShiftToGearAndSync(int setGear)
	{
		if (gear != (CarGearShift)setGear)
		{
			timeAtLastGearShift = Time.realtimeSinceStartup;
			gear = (CarGearShift)setGear;
			gearStickAudio.PlayOneShot(gearStickAudios[setGear - 1]);
			if (gear != CarGearShift.Park)
			{
				Debug.Log("Shift to gear and sync; gear is not park");
				switchGearsBoostEnabled = true;
			}
			ShiftToGearServerRpc(setGear, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ShiftToGearServerRpc(int setGear, int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1427257619u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, setGear);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendServerRpc(ref bufferWriter, 1427257619u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				ShiftToGearClientRpc(setGear, playerId);
			}
		}
	}

	[ClientRpc]
	public void ShiftToGearClientRpc(int setGear, int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2335366121u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, setGear);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 2335366121u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
		{
			timeAtLastGearShift = Time.realtimeSinceStartup;
			gear = (CarGearShift)setGear;
			gearStickAudio.PlayOneShot(gearStickAudios[setGear - 1]);
			if (gear != CarGearShift.Park)
			{
				switchGearsBoostEnabled = true;
			}
		}
	}

	private void ShiftGearBackInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			ShiftGearBack();
		}
	}

	private void GetVehicleInput()
	{
		if (localPlayerInControl)
		{
			if (testingVehicleInEditor)
			{
				moveInputVector = input.actions.FindAction("Move").ReadValue<Vector2>();
			}
			else
			{
				moveInputVector = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
			}
			float num = steeringWheelTurnSpeed;
			steeringInput = Mathf.Clamp(steeringInput + moveInputVector.x * num * Time.deltaTime, -3f, 3f);
			if (Mathf.Abs(moveInputVector.x) > 0.1f)
			{
				steeringWheelAudio.volume = Mathf.Lerp(steeringWheelAudio.volume, Mathf.Abs(moveInputVector.x), 5f * Time.deltaTime);
			}
			else
			{
				steeringWheelAudio.volume = Mathf.Lerp(steeringWheelAudio.volume, 0f, 5f * Time.deltaTime);
			}
			steeringAnimValue = moveInputVector.x;
			drivePedalPressed = moveInputVector.y > 0.1f;
			brakePedalPressed = moveInputVector.y < -0.1f;
		}
	}

	[ServerRpc]
	public void SyncCarPositionServerRpc(Vector3 carPosition, Vector3 carRotation, float steeringInput, float EngineRPM)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2803421723u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in carPosition);
			bufferWriter.WriteValueSafe(in carRotation);
			bufferWriter.WriteValueSafe(in steeringInput, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in EngineRPM, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2803421723u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SyncCarPositionClientRpc(carPosition, carRotation, steeringInput, EngineRPM);
		}
	}

	[ClientRpc]
	public void SyncCarPositionClientRpc(Vector3 carPosition, Vector3 carRotation, float steeringInput, float engineSpeed)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3105401376u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in carPosition);
			bufferWriter.WriteValueSafe(in carRotation);
			bufferWriter.WriteValueSafe(in steeringInput, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in engineSpeed, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3105401376u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				gameObject.GetComponent<Rigidbody>().isKinematic = true;
				syncedPosition = carPosition;
				syncedRotation = Quaternion.Euler(carRotation);
				steeringAnimValue = steeringInput;
				EngineRPM = engineSpeed;
			}
		}
	}

	[ServerRpc]
	public void MagnetCarServerRpc(Vector3 targetPosition, Vector3 targetRotation, int playerWhoSent)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2451439781u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in targetPosition);
			bufferWriter.WriteValueSafe(in targetRotation);
			BytePacker.WriteValueBitPacked(bufferWriter, playerWhoSent);
			__endSendServerRpc(ref bufferWriter, 2451439781u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			MagnetCarClientRpc(targetPosition, targetRotation, playerWhoSent);
		}
	}

	[ClientRpc]
	public void MagnetCarClientRpc(Vector3 targetPosition, Vector3 targetRotation, int playerWhoSent)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2845017736u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in targetPosition);
			bufferWriter.WriteValueSafe(in targetRotation);
			BytePacker.WriteValueBitPacked(bufferWriter, playerWhoSent);
			__endSendClientRpc(ref bufferWriter, 2845017736u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerWhoSent)
			{
				magnetedToShip = true;
				magnetTime = 0f;
				magnetRotationTime = 0f;
				StartOfRound.Instance.isObjectAttachedToMagnet = true;
				StartOfRound.Instance.attachedVehicle = this;
				magnetStartPosition = transform.position;
				magnetStartRotation = transform.rotation;
				magnetTargetPosition = targetPosition;
				magnetTargetRotation = Quaternion.Euler(targetRotation);
				CollectItemsInTruck();
			}
		}
	}

	public void SetHonkingLocalClient(bool honk)
	{
		honkingHorn = honk;
		SetHonkServerRpc(honk, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetHonkServerRpc(bool honk, int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(735895017u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in honk, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendServerRpc(ref bufferWriter, 735895017u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetHonkClientRpc(honk, playerId);
			}
		}
	}

	[ClientRpc]
	public void SetHonkClientRpc(bool honk, int playerId)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2121824285u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in honk, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendClientRpc(ref bufferWriter, 2121824285u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				honkingHorn = honk;
			}
		}
	}

	public void CollectItemsInTruck()
	{
		Debug.Log("Collect items in truck A");
		Collider[] array = Physics.OverlapSphere(transform.position, 25f, 64, QueryTriggerInteraction.Ignore);
		Debug.Log($"Collect items in truck B; {array.Length}");
		for (int i = 0; i < array.Length; i++)
		{
			GrabbableObject component = array[i].GetComponent<GrabbableObject>();
			Debug.Log($"Collect items in truck C; {component != null}");
			if (component != null)
			{
				Debug.Log($"{!component.isHeld}; {!component.isHeldByEnemy}; {array[i].transform.parent == transform}");
				Debug.Log($"Magneted? : {magnetedToShip}");
			}
			if (component != null && !component.isHeld && !component.isHeldByEnemy && array[i].transform.parent == transform)
			{
				GameNetworkManager.Instance.localPlayerController.SetItemInElevator(magnetedToShip, magnetedToShip, component);
			}
		}
	}

	public void StartMagneting()
	{
		if (!IsOwner)
		{
			return;
		}
		magnetTime = 0f;
		magnetRotationTime = 0f;
		StartOfRound.Instance.isObjectAttachedToMagnet = true;
		StartOfRound.Instance.attachedVehicle = this;
		magnetedToShip = true;
		averageVelocityAtMagnetStart = averageVelocity;
		gameObject.GetComponent<Rigidbody>().isKinematic = true;
		RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
		Debug.DrawRay(transform.position, RoundManager.Instance.tempTransform.forward * 5f, Color.green, 2f);
		Debug.DrawRay(transform.position, -StartOfRound.Instance.magnetPoint.forward * 5f, Color.red, 2f);
		float num = Vector3.Angle(RoundManager.Instance.tempTransform.forward, -StartOfRound.Instance.magnetPoint.forward);
		Debug.Log($"Truck initial angle to magnet: {num}");
		Vector3 eulerAngles = transform.eulerAngles;
		if (num < 47f || num > 133f)
		{
			if (eulerAngles.y < 0f)
			{
				eulerAngles.y -= 46f - num;
			}
			else
			{
				eulerAngles.y += 46f - num;
			}
		}
		Debug.DrawRay(transform.position, eulerAngles * 4f, Color.white, 2f);
		eulerAngles.y = Mathf.Round(eulerAngles.y / 90f) * 90f;
		eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;
		eulerAngles.x += UnityEngine.Random.Range(-5f, 5f);
		Debug.DrawRay(transform.position, eulerAngles * 4f, Color.cyan, 2f);
		magnetTargetRotation = Quaternion.Euler(eulerAngles);
		magnetStartRotation = transform.rotation;
		Quaternion rotation = transform.rotation;
		transform.rotation = magnetTargetRotation;
		magnetTargetPosition = boundsCollider.ClosestPoint(StartOfRound.Instance.magnetPoint.position) - transform.position;
		if (magnetTargetPosition.y >= boundsCollider.bounds.extents.y)
		{
			magnetTargetPosition.y -= boundsCollider.bounds.extents.y / 2f;
		}
		else if (magnetTargetPosition.y <= boundsCollider.bounds.extents.y * 0.4f)
		{
			magnetTargetPosition.y += boundsCollider.bounds.extents.y / 2f;
		}
		magnetTargetPosition = StartOfRound.Instance.magnetPoint.position - magnetTargetPosition;
		Debug.DrawLine(transform.position, magnetTargetPosition);
		magnetTargetPosition.z = Mathf.Min(-20.4f, magnetTargetPosition.z);
		magnetTargetPosition.y = Mathf.Max(2.5f, magnetStartPosition.y);
		magnetTargetPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(magnetTargetPosition);
		transform.rotation = rotation;
		magnetStartPosition = transform.position;
		CollectItemsInTruck();
		MagnetCarServerRpc(magnetTargetPosition, eulerAngles, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	public void AddTurboBoost()
	{
		int setTurboBoosts = Mathf.Min(turboBoosts + 1, 5);
		AddTurboBoostOnLocalClient(setTurboBoosts);
		AddTurboBoostServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, setTurboBoosts);
	}

	public void AddTurboBoostOnLocalClient(int setTurboBoosts)
	{
		hoodAudio.PlayOneShot(pourTurbo);
		turboBoosts = setTurboBoosts;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddTurboBoostServerRpc(int playerId, int setTurboBoosts)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2416458891u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				BytePacker.WriteValueBitPacked(bufferWriter, setTurboBoosts);
				__endSendServerRpc(ref bufferWriter, 2416458891u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				AddTurboBoostClientRpc(playerId, setTurboBoosts);
			}
		}
	}

	[ClientRpc]
	public void AddTurboBoostClientRpc(int playerId, int setTurboBoosts)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(4268487771u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			BytePacker.WriteValueBitPacked(bufferWriter, setTurboBoosts);
			__endSendClientRpc(ref bufferWriter, 4268487771u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				AddTurboBoostOnLocalClient(setTurboBoosts);
			}
		}
	}

	public void AddEngineOil()
	{
		int num = Mathf.Min(carHP + 4, baseCarHP);
		AddEngineOilOnLocalClient(num);
		AddEngineOilServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, num);
	}

	public void AddEngineOilOnLocalClient(int setCarHP)
	{
		hoodAudio.PlayOneShot(pourOil);
		carHP = setCarHP;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddEngineOilServerRpc(int playerId, int setHP)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(548979688u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				BytePacker.WriteValueBitPacked(bufferWriter, setHP);
				__endSendServerRpc(ref bufferWriter, 548979688u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				AddEngineOilClientRpc(playerId, setHP);
			}
		}
	}

	[ClientRpc]
	public void AddEngineOilClientRpc(int playerId, int setHP)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2079068163u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			BytePacker.WriteValueBitPacked(bufferWriter, setHP);
			__endSendClientRpc(ref bufferWriter, 2079068163u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				AddEngineOilOnLocalClient(setHP);
			}
		}
	}

	private void DoTurboBoost(InputAction.CallbackContext context)
	{
		if (context.performed && localPlayerInControl && !jumpingInCar && !keyIsInDriverHand)
		{
			Vector2 dir = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
			UseTurboBoostLocalClient(dir);
			UseTurboBoostServerRpc();
		}
	}

	public void UseTurboBoostLocalClient(Vector2 dir = default(Vector2))
	{
		if (IsOwner)
		{
			if (turboBoosts == 0 || !ignitionStarted)
			{
				jumpingInCar = true;
				GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetTrigger("SA_JumpInCar");
				StartCoroutine(jerkCarUpward(dir));
			}
			else
			{
				Vector3 vector = transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
				mainRigidbody.AddForce(vector * turboBoostForce + Vector3.up * turboBoostUpwardForce * 0.6f, ForceMode.Impulse);
			}
		}
		if (turboBoosts > 0 && ignitionStarted)
		{
			turboBoosts = Mathf.Max(0, turboBoosts - 1);
			turboBoostAudio.PlayOneShot(turboBoostSFX);
			engineAudio1.PlayOneShot(turboBoostSFX2);
			turboBoostParticle.Play(withChildren: true);
			if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, turboBoostAudio.transform.position) < 10f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
		}
		else
		{
			springAudio.PlayOneShot(jumpInCarSFX);
		}
	}

	private IEnumerator jerkCarUpward(Vector3 dir)
	{
		yield return new WaitForSeconds(0.16f);
		if (!IsOwner)
		{
			jumpingInCar = false;
			yield break;
		}
		Vector3 vector = transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
		Debug.DrawRay(transform.position, vector * 20f, Color.blue, 5f);
		mainRigidbody.AddForce(vector * turboBoostForce * 0.22f + Vector3.up * turboBoostUpwardForce * 0.1f, ForceMode.Impulse);
		mainRigidbody.AddForceAtPosition(Vector3.up * jumpForce, hoodFireAudio.transform.position - Vector3.up * 2f, ForceMode.Impulse);
		yield return new WaitForSeconds(0.15f);
		jumpingInCar = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void UseTurboBoostServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1878146525u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 1878146525u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				UseTurboBoostClientRpc();
			}
		}
	}

	[ClientRpc]
	public void UseTurboBoostClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(4076738570u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 4076738570u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				UseTurboBoostLocalClient();
			}
		}
	}

	public void OnDisable()
	{
		DisableControl();
		if (localPlayerInControl || localPlayerInPassengerSeat)
		{
			GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
		}
		GrabbableObject[] componentsInChildren = physicsRegion.physicsTransform.GetComponentsInChildren<GrabbableObject>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (RoundManager.Instance.mapPropsContainer != null)
			{
				componentsInChildren[i].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
			}
			else
			{
				componentsInChildren[i].transform.SetParent(null, worldPositionStays: true);
			}
			if (!componentsInChildren[i].isHeld)
			{
				componentsInChildren[i].FallToGround();
			}
		}
		physicsRegion.disablePhysicsRegion = true;
		if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(physicsRegion))
		{
			StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(physicsRegion);
		}
	}

	private void Update()
	{
		if (destroyNextFrame)
		{
			if (IsOwner)
			{
				Debug.Log($"Is networkobject spawned: {NetworkObject.IsSpawned}");
				Debug.Log("Destroying car on local client");
				UnityEngine.Object.Destroy(windwiperPhysicsBody1.gameObject);
				UnityEngine.Object.Destroy(windwiperPhysicsBody2.gameObject);
				UnityEngine.Object.Destroy(ragdollPhysicsBody.gameObject);
				UnityEngine.Object.Destroy(gameObject);
			}
			return;
		}
		if (NetworkObject != null && !NetworkObject.IsSpawned)
		{
			physicsRegion.disablePhysicsRegion = true;
			if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(physicsRegion))
			{
				StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(physicsRegion);
			}
			if (localPlayerInControl || localPlayerInPassengerSeat)
			{
				GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
			}
			GrabbableObject[] componentsInChildren = physicsRegion.physicsTransform.GetComponentsInChildren<GrabbableObject>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (RoundManager.Instance.mapPropsContainer != null)
				{
					componentsInChildren[i].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
				}
				else
				{
					componentsInChildren[i].transform.SetParent(null, worldPositionStays: true);
				}
				if (!componentsInChildren[i].isHeld)
				{
					componentsInChildren[i].FallToGround();
				}
			}
			Debug.Log("Destroying car on local client, next frame");
			destroyNextFrame = true;
			return;
		}
		ReactToDamage();
		driverSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
		passengerSideDoorTrigger.interactable = Time.realtimeSinceStartup - timeSinceSpringingDriverSeat > 1.6f;
		if (magnetedToShip)
		{
			if (!StartOfRound.Instance.magnetOn)
			{
				magnetedToShip = false;
				StartOfRound.Instance.isObjectAttachedToMagnet = false;
				CollectItemsInTruck();
				return;
			}
			pressedTurbo = false;
			limitTruckVelocityTimer = 1.3f;
			EngineRPM = Mathf.Lerp(EngineRPM, 0f, 3f * Time.deltaTime);
			physicsRegion.priority = 0;
			magnetTime = Mathf.Min(magnetTime + Time.deltaTime, 1f);
			magnetRotationTime = Mathf.Min(magnetTime + Time.deltaTime * 0.75f, 1f);
			if (StartOfRound.Instance.inShipPhase)
			{
				carHP = baseCarHP;
			}
			if (!finishedMagneting && magnetTime > 0.7f)
			{
				finishedMagneting = true;
				if (!loadedVehicleFromSave)
				{
					turbulenceAmount = 2f;
					turbulenceAudio.volume = 0.6f;
					turbulenceAudio.PlayOneShot(maxCollisions[UnityEngine.Random.Range(0, maxCollisions.Length)]);
				}
			}
		}
		else
		{
			physicsRegion.priority = 1;
			finishedMagneting = false;
			if (StartOfRound.Instance.attachedVehicle == this)
			{
				StartOfRound.Instance.attachedVehicle = null;
			}
			if (IsOwner && !StartOfRound.Instance.isObjectAttachedToMagnet && StartOfRound.Instance.magnetOn && Vector3.Distance(base.transform.position, StartOfRound.Instance.magnetPoint.position) < 10f && !Physics.Linecast(base.transform.position, StartOfRound.Instance.magnetPoint.position, 256, QueryTriggerInteraction.Ignore))
			{
				StartMagneting();
				return;
			}
			if (IsOwner)
			{
				if (enabledCollisionForAllPlayers)
				{
					enabledCollisionForAllPlayers = false;
					DisableVehicleCollisionForAllPlayers();
				}
				SyncCarPhysicsToOtherClients();
			}
			else
			{
				if (!enabledCollisionForAllPlayers)
				{
					enabledCollisionForAllPlayers = true;
					EnableVehicleCollisionForAllPlayers();
				}
				gameObject.GetComponent<Rigidbody>().isKinematic = true;
			}
		}
		if (honkingHorn)
		{
			if (!hornAudio.isPlaying)
			{
				hornAudio.Play();
				hornAudio.pitch = 1f;
			}
			else if (Time.realtimeSinceStartup - timeAtLastHornPing > 2f)
			{
				timeAtLastHornPing = Time.realtimeSinceStartup;
				RoundManager.Instance.PlayAudibleNoise(hornAudio.transform.position, 28f, 0.85f, 0, noiseIsInsideClosedShip: false, 106217);
			}
		}
		else
		{
			hornAudio.pitch = Mathf.Max(hornAudio.pitch - Time.deltaTime * 6f, 0.01f);
			if (hornAudio.pitch < 0.02f)
			{
				hornAudio.Stop();
			}
		}
		FrontLeftWheel.steerAngle = 15f * steeringInput;
		FrontRightWheel.steerAngle = 15f * steeringInput;
		if (carDestroyed)
		{
			return;
		}
		if (keyIsInDriverHand && currentDriver != null)
		{
			keyObject.enabled = true;
			Transform transform = ((!localPlayerInControl) ? currentDriver.serverItemHolder : currentDriver.localItemHolder);
			keyObject.transform.rotation = transform.rotation;
			keyObject.transform.Rotate(rotationOffset);
			keyObject.transform.position = transform.position;
			Vector3 vector = positionOffset;
			vector = transform.rotation * vector;
			keyObject.transform.position += vector;
		}
		else
		{
			if (Time.realtimeSinceStartup - timeAtLastGearShift < 1.7f && currentDriver != null)
			{
				currentDriver.playerBodyAnimator.SetFloat("SA_CarMotionTime", gearStickAnimValue);
			}
			if (localPlayerInControl && ignitionStarted && keyIgnitionCoroutine == null)
			{
				if (GameNetworkManager.Instance.localPlayerController.ladderCameraHorizontal > 52f)
				{
					if (Time.realtimeSinceStartup - timeAtLastGearShift < 1.7f)
					{
						GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 5);
					}
					else
					{
						GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 4);
					}
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("SA_CarAnim", 1);
				}
			}
			if (keyIsInIgnition)
			{
				keyObject.enabled = true;
				if (ignitionStarted)
				{
					keyObject.transform.position = ignitionTurnedPosition.position;
					keyObject.transform.rotation = ignitionTurnedPosition.rotation;
				}
				else
				{
					keyObject.transform.position = ignitionNotTurnedPosition.position;
					keyObject.transform.rotation = ignitionNotTurnedPosition.rotation;
				}
			}
			else
			{
				keyObject.enabled = false;
			}
		}
		if (!localPlayerInControl || !IsOwner)
		{
			SetCarEffects(steeringAnimValue);
			return;
		}
		if (ignitionStarted)
		{
			GetVehicleInput();
		}
		if (limitTruckVelocityTimer <= 0f)
		{
			mainRigidbody.maxAngularVelocity = 4f;
			mainRigidbody.maxLinearVelocity = 50f;
			mainRigidbody.maxDepenetrationVelocity = 7f;
		}
		else
		{
			limitTruckVelocityTimer -= Time.deltaTime * 0.5f;
			mainRigidbody.maxDepenetrationVelocity = Mathf.Lerp(0.3f, 7f, Mathf.Clamp(limitTruckVelocityTimer, 0f, 1f));
			mainRigidbody.maxAngularVelocity = Mathf.Lerp(0.1f, 4f, Mathf.Clamp(limitTruckVelocityTimer, 0f, 1f));
			mainRigidbody.maxLinearVelocity = Mathf.Lerp(0.1f, 60f, Mathf.Clamp(limitTruckVelocityTimer, 0f, 1f));
		}
		SetCarEffects(steeringAnimValue);
		float num = 0f;
		EngineRPM = (FrontLeftWheel.rpm + FrontRightWheel.rpm) / 2f;
		vehicleEngineAudio.pitch = Mathf.Min(Mathf.Abs(EngineRPM / MaxEngineRPM) + 1f, 1.3f);
		if (brakePedalPressed)
		{
			if (drivePedalPressed)
			{
				num += 2f;
				lastStressType += "; Accelerating while braking";
			}
			FrontLeftWheel.brakeTorque = 2000f;
			FrontRightWheel.brakeTorque = 2000f;
			BackLeftWheel.brakeTorque = 2000f;
			BackRightWheel.brakeTorque = 2000f;
			for (int j = 0; j < otherWheels.Length; j++)
			{
				otherWheels[j].brakeTorque = 2000f;
			}
		}
		else
		{
			FrontLeftWheel.brakeTorque = 0f;
			FrontRightWheel.brakeTorque = 0f;
			BackLeftWheel.brakeTorque = 0f;
			BackRightWheel.brakeTorque = 0f;
			for (int k = 0; k < otherWheels.Length; k++)
			{
				otherWheels[k].brakeTorque = 0f;
			}
		}
		if (drivePedalPressed && ignitionStarted)
		{
			switch (gear)
			{
			case CarGearShift.Drive:
			{
				if (switchGearsBoostEnabled)
				{
					mainRigidbody.AddForce(0.35f * turboBoostForce * base.transform.TransformDirection(new Vector3(0f, 0f, 1f)) + 0.06f * turboBoostUpwardForce * Vector3.up, ForceMode.Impulse);
					switchGearsBoostEnabled = false;
				}
				FrontLeftWheel.motorTorque = Mathf.Clamp(Mathf.MoveTowards(FrontLeftWheel.motorTorque, EngineTorque, carAcceleration * Time.deltaTime), 325f, 1000f);
				FrontRightWheel.motorTorque = FrontLeftWheel.motorTorque;
				BackLeftWheel.motorTorque = FrontLeftWheel.motorTorque;
				BackRightWheel.motorTorque = FrontLeftWheel.motorTorque;
				for (int m = 0; m < otherWheels.Length; m++)
				{
					otherWheels[m].motorTorque = FrontLeftWheel.motorTorque;
				}
				break;
			}
			case CarGearShift.Reverse:
			{
				if (switchGearsBoostEnabled)
				{
					mainRigidbody.AddForce(base.transform.TransformDirection(new Vector3(0f, 0f, -1f)) * turboBoostForce * 0.3f + Vector3.up * turboBoostUpwardForce * 0.06f, ForceMode.Impulse);
					switchGearsBoostEnabled = false;
				}
				if (EngineRPM > 5000f)
				{
					num += Mathf.Min((EngineRPM - 5000f) / 10000f, 0f);
					lastStressType += "; Reversing while at high speed";
				}
				FrontLeftWheel.motorTorque = 0f - EngineTorque;
				FrontRightWheel.motorTorque = 0f - EngineTorque;
				BackLeftWheel.motorTorque = 0f - EngineTorque;
				BackRightWheel.motorTorque = 0f - EngineTorque;
				for (int l = 0; l < otherWheels.Length; l++)
				{
					otherWheels[l].motorTorque = 0f - EngineTorque;
				}
				break;
			}
			}
		}
		else if (pressedTurbo)
		{
			pressedTurbo = false;
		}
		if (gear == CarGearShift.Park || !ignitionStarted)
		{
			if (Mathf.Abs(EngineRPM) > 3000f && ignitionStarted)
			{
				num += Mathf.Clamp((EngineRPM - 200f) / 350f, 0f, 1.3f);
				lastStressType += "; In park while at high speed";
			}
			if (drivePedalPressed && ignitionStarted)
			{
				num += 1.2f;
				lastStressType += "; Accelerating while in park";
			}
			FrontLeftWheel.motorTorque = 0f;
			FrontRightWheel.motorTorque = 0f;
			BackLeftWheel.motorTorque = 0f;
			BackRightWheel.motorTorque = 0f;
			FrontLeftWheel.brakeTorque = 2000f;
			FrontRightWheel.brakeTorque = 2000f;
			BackLeftWheel.brakeTorque = 2000f;
			BackRightWheel.brakeTorque = 2000f;
			for (int n = 0; n < otherWheels.Length; n++)
			{
				otherWheels[n].brakeTorque = 2000f;
				otherWheels[n].motorTorque = 0f;
			}
		}
		else if (!drivePedalPressed)
		{
			float num2 = 1f;
			if (gear == CarGearShift.Reverse)
			{
				num2 = -1f;
			}
			FrontLeftWheel.motorTorque = idleSpeed * num2;
			FrontRightWheel.motorTorque = idleSpeed * num2;
			BackLeftWheel.motorTorque = idleSpeed * num2;
			BackRightWheel.motorTorque = idleSpeed * num2;
			for (int num3 = 0; num3 < otherWheels.Length; num3++)
			{
				otherWheels[num3].motorTorque = idleSpeed * num2;
			}
		}
		SetInternalStress(num);
		stressPerSecond = num;
	}

	public void ChangeRadioStation()
	{
		if (!radioOn)
		{
			SetRadioOnLocalClient(on: true, setClip: false);
		}
		currentRadioClip = (currentRadioClip + 1) % radioClips.Length;
		radioAudio.clip = radioClips[currentRadioClip];
		radioAudio.time = Mathf.Clamp(currentSongTime % radioAudio.clip.length, 0.01f, radioAudio.clip.length - 0.1f);
		radioAudio.Play();
		int num = (int)Mathf.Round(radioSignalQuality);
		switch (num)
		{
		case 3:
			radioSignalQuality = 1f;
			radioSignalDecreaseThreshold = 10f;
			break;
		case 0:
			radioSignalQuality = 3f;
			radioSignalDecreaseThreshold = 90f;
			break;
		case 1:
			radioSignalQuality = 2f;
			radioSignalDecreaseThreshold = 70f;
			break;
		case 2:
			radioSignalQuality = 1f;
			radioSignalDecreaseThreshold = 30f;
			break;
		}
		SetRadioStationServerRpc(currentRadioClip, num);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetRadioStationServerRpc(int radioStation, int signalQuality)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(721150963u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, radioStation);
				BytePacker.WriteValueBitPacked(bufferWriter, signalQuality);
				__endSendServerRpc(ref bufferWriter, 721150963u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetRadioStationClientRpc(radioStation, signalQuality);
			}
		}
	}

	[ClientRpc]
	public void SetRadioStationClientRpc(int radioStation, int signalQuality)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3091363772u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, radioStation);
			BytePacker.WriteValueBitPacked(bufferWriter, signalQuality);
			__endSendClientRpc(ref bufferWriter, 3091363772u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (currentRadioClip != radioStation)
		{
			if (!radioOn)
			{
				SetRadioOnLocalClient(on: true, setClip: false);
			}
			currentRadioClip = radioStation;
			radioSignalQuality = signalQuality;
			radioAudio.Play();
		}
	}

	public void SwitchRadio()
	{
		radioOn = !radioOn;
		if (radioOn)
		{
			radioAudio.clip = radioClips[currentRadioClip];
			radioAudio.Play();
			radioInterference.Play();
		}
		else
		{
			radioAudio.Stop();
			radioInterference.Stop();
		}
		SetRadioOnServerRpc(radioOn);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetRadioOnServerRpc(bool on)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2416589835u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in on, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 2416589835u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetRadioOnClientRpc(on);
			}
		}
	}

	[ClientRpc]
	public void SetRadioOnClientRpc(bool on)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(2165949877u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in on, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 2165949877u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (radioOn != on)
			{
				SetRadioOnLocalClient(on);
			}
		}
	}

	private void SetRadioOnLocalClient(bool on, bool setClip = true)
	{
		radioOn = on;
		if (on)
		{
			if (setClip)
			{
				radioAudio.clip = radioClips[currentRadioClip];
			}
			radioAudio.Play();
			radioInterference.Play();
		}
		else
		{
			radioAudio.Stop();
			radioInterference.Stop();
		}
	}

	public void SetRadioValues()
	{
		if (radioTurnedOnBefore)
		{
			currentSongTime += Time.deltaTime;
		}
		if (!radioOn)
		{
			return;
		}
		if (!radioAudio.isPlaying)
		{
			radioAudio.Play();
		}
		radioTurnedOnBefore = true;
		if (IsOwner)
		{
			float num = UnityEngine.Random.Range(0, 100);
			float num2 = (3f - radioSignalQuality - 1.5f) * radioSignalTurbulence;
			radioSignalDecreaseThreshold = Mathf.Clamp(radioSignalDecreaseThreshold + Time.deltaTime * num2, 0f, 100f);
			if (num > radioSignalDecreaseThreshold)
			{
				radioSignalQuality = Mathf.Clamp(radioSignalQuality - Time.deltaTime, 0f, 3f);
			}
			else
			{
				radioSignalQuality = Mathf.Clamp(radioSignalQuality + Time.deltaTime, 0f, 3f);
			}
			if (Time.realtimeSinceStartup - changeRadioSignalTime > 0.3f)
			{
				changeRadioSignalTime = Time.realtimeSinceStartup;
				if (radioSignalQuality < 1.2f && UnityEngine.Random.Range(0, 100) < 6)
				{
					radioSignalQuality = Mathf.Min(radioSignalQuality + 1.5f, 3f);
					radioSignalDecreaseThreshold = Mathf.Min(radioSignalDecreaseThreshold + 30f, 100f);
				}
				SetRadioSignalQualityServerRpc((int)Mathf.Round(radioSignalQuality));
			}
		}
		switch ((int)Mathf.Round(radioSignalQuality))
		{
		case 3:
			radioAudio.volume = Mathf.Lerp(radioAudio.volume, 1f, 2f * Time.deltaTime);
			radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0f, 2f * Time.deltaTime);
			break;
		case 2:
			radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.85f, 2f * Time.deltaTime);
			radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0.4f, 2f * Time.deltaTime);
			break;
		case 1:
			radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.6f, 2f * Time.deltaTime);
			radioInterference.volume = Mathf.Lerp(radioInterference.volume, 0.8f, 2f * Time.deltaTime);
			break;
		case 0:
			radioAudio.volume = Mathf.Lerp(radioAudio.volume, 0.4f, 2f * Time.deltaTime);
			radioInterference.volume = Mathf.Lerp(radioInterference.volume, 1f, 2f * Time.deltaTime);
			break;
		}
	}

	[ServerRpc]
	public void SetRadioSignalQualityServerRpc(int signalQuality)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2043456042u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, signalQuality);
			__endSendServerRpc(ref bufferWriter, 2043456042u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SetRadioSignalQualityClientRpc(signalQuality);
		}
	}

	[ClientRpc]
	public void SetRadioSignalQualityClientRpc(int signalQuality)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(894646603u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, signalQuality);
			__endSendClientRpc(ref bufferWriter, 894646603u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				radioSignalQuality = signalQuality;
			}
		}
	}

	private void MatchWheelMeshToCollider(MeshRenderer wheelMesh, WheelCollider wheelCollider)
	{
		Vector3 position = wheelCollider.transform.position;
		if (Physics.Raycast(position, -wheelCollider.transform.up, out hit, wheelCollider.suspensionDistance + wheelCollider.radius, 2304))
		{
			wheelMesh.transform.position = hit.point + wheelCollider.transform.up * wheelCollider.radius;
		}
		else
		{
			wheelMesh.transform.position = position - wheelCollider.transform.up * wheelCollider.suspensionDistance;
		}
		wheelMesh.transform.Rotate(Vector3.right, EngineRPM * 0.5f, Space.Self);
	}

	[ServerRpc]
	public void SyncExtremeStressServerRpc(bool underStress)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3603115648u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in underStress, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3603115648u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			SyncExtremeStressClientRpc(underStress);
		}
	}

	[ClientRpc]
	public void SyncExtremeStressClientRpc(bool underStress)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3722438677u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in underStress, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3722438677u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (carDestroyed)
			{
				underExtremeStress = false;
			}
			else
			{
				underExtremeStress = underStress;
			}
		}
	}

	private void SetCarEffects(float setSteering)
	{
		steeringWheelAnimFloat = Mathf.Clamp(steeringWheelAnimFloat + setSteering * steeringWheelTurnSpeed * Time.deltaTime / 6f, -0.99f, 0.99f);
		float num = Mathf.Clamp((steeringWheelAnimFloat + 1f) / 2f, 0.01f, 0.99f) - steeringWheelAnimator.GetFloat("steeringWheelTurnSpeed");
		steeringWheelAnimator.SetFloat("steeringWheelTurnSpeed", Mathf.Clamp((steeringWheelAnimFloat + 1f) / 2f, 0.01f, 0.99f));
		if (currentDriver != null)
		{
			currentDriver.playerBodyAnimator.SetFloat("animationSpeed", currentDriver.playerBodyAnimator.GetFloat("animationSpeed") + num * 2f);
		}
		leftWheelMesh.transform.localEulerAngles = new Vector3(leftWheelMesh.transform.localEulerAngles.x, steeringWheelAnimFloat * 50f, 0f);
		MatchWheelMeshToCollider(leftWheelMesh, FrontLeftWheel);
		rightWheelMesh.transform.localEulerAngles = new Vector3(rightWheelMesh.transform.localEulerAngles.x, steeringWheelAnimFloat * 50f, 0f);
		MatchWheelMeshToCollider(rightWheelMesh, FrontRightWheel);
		MatchWheelMeshToCollider(backLeftWheelMesh, BackLeftWheel);
		MatchWheelMeshToCollider(backRightWheelMesh, BackRightWheel);
		if (gear == CarGearShift.Reverse)
		{
			gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0.5f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
		}
		else if (gear == CarGearShift.Park)
		{
			gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 1f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
		}
		else
		{
			gearStickAnimValue = Mathf.MoveTowards(gearStickAnimValue, 0f, 15f * Time.deltaTime * (Time.realtimeSinceStartup - timeAtLastGearShift));
		}
		gearStickAnimator.SetFloat("gear", Mathf.Clamp(gearStickAnimValue, 0.01f, 0.99f));
		if (EngineRPM < -5f)
		{
			if (!backLightsOn)
			{
				backLightsOn = true;
				backLightsMesh.material = backLightOnMat;
				backLightsContainer.SetActive(value: true);
			}
		}
		else if (backLightsOn)
		{
			backLightsOn = false;
			backLightsMesh.material = headlightsOffMat;
			backLightsContainer.SetActive(value: false);
		}
		SetVehicleAudioProperties(extremeStressAudio, underExtremeStress, 0.2f, 1f, 3f, useVolumeInsteadOfPitch: true);
		if (IsOwner)
		{
			if (!syncedExtremeStress && underExtremeStress && extremeStressAudio.volume > 0.35f)
			{
				syncedExtremeStress = true;
				SyncExtremeStressServerRpc(underExtremeStress);
			}
			else if (syncedExtremeStress && !underExtremeStress && extremeStressAudio.volume < 0.5f)
			{
				syncedExtremeStress = false;
				SyncExtremeStressServerRpc(underExtremeStress);
			}
		}
		SetRadioValues();
		float num2 = Vector3.Dot(Vector3.Normalize(mainRigidbody.velocity * 1000f), transform.forward);
		bool audioActive = num2 > -0.6f && num2 < 0.4f && (averageVelocity.magnitude > 4f || EngineRPM > 400f);
		if ((!FrontLeftWheel.isGrounded && !FrontRightWheel.isGrounded) || (!BackLeftWheel.isGrounded && !BackRightWheel.isGrounded))
		{
			audioActive = false;
		}
		if (FrontLeftWheel.motorTorque > 900f && FrontRightWheel.motorTorque > 900f)
		{
			audioActive = true;
			num2 = Mathf.Max(num2, 0.8f);
			if (averageVelocity.magnitude > 8f && !tireSparks.isPlaying)
			{
				tireSparks.Play(withChildren: true);
			}
		}
		else
		{
			tireSparks.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
		SetVehicleAudioProperties(skiddingAudio, audioActive, 0f, num2, 3f, useVolumeInsteadOfPitch: true);
		carEngine2AudioActive = ignitionStarted;
		float num3 = Mathf.Abs(EngineRPM);
		float highest = Mathf.Clamp(num3 / engineIntensityPercentage, 0.7f, 1.5f);
		SetVehicleAudioProperties(engineAudio2, carEngine2AudioActive, 0.7f, highest, 3f, useVolumeInsteadOfPitch: false, 0.5f);
		highest = Mathf.Clamp(num3 / engineIntensityPercentage, 0.65f, 1.15f);
		float highest2 = highest;
		if (!ignitionStarted)
		{
			highest2 = 1f;
		}
		SetVehicleAudioProperties(engineAudio1, carEngine1AudioActive, 0.7f, highest2, 2f, useVolumeInsteadOfPitch: false, 0.7f);
		if (engineAudio1.volume > 0.3f && engineAudio1.isPlaying && Time.realtimeSinceStartup - timeAtLastEngineAudioPing > 2f)
		{
			timeAtLastEngineAudioPing = Time.realtimeSinceStartup;
			if (EngineRPM > 130f)
			{
				RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 32f, 0.75f, 0, noiseIsInsideClosedShip: false, 2692);
			}
			if (EngineRPM > 60f)
			{
				RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 25f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
			}
			else if (!ignitionStarted)
			{
				RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 15f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
			}
			else
			{
				RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 11f, 0.5f, 0, noiseIsInsideClosedShip: false, 2692);
			}
		}
		carRollingAudioActive = num3 > 10f;
		highest = Mathf.Clamp(num3 / (engineIntensityPercentage * 0.35f), 0f, 1f);
		SetVehicleAudioProperties(rollingAudio, carRollingAudioActive, 0f, highest, 5f, useVolumeInsteadOfPitch: true);
		turbulenceAudio.volume = Mathf.Lerp(turbulenceAudio.volume, Mathf.Min(1f, turbulenceAmount), 10f * Time.deltaTime);
		turbulenceAmount = Mathf.Max(turbulenceAmount - Time.deltaTime, 0f);
		if (turbulenceAudio.volume > 0.02f)
		{
			if (!turbulenceAudio.isPlaying)
			{
				turbulenceAudio.Play();
			}
		}
		else if (turbulenceAudio.isPlaying)
		{
			turbulenceAudio.Stop();
		}
	}

	private void SetVehicleAudioProperties(AudioSource audio, bool audioActive, float lowest, float highest, float lerpSpeed, bool useVolumeInsteadOfPitch = false, float onVolume = 1f)
	{
		if (audioActive)
		{
			if (!audio.isPlaying)
			{
				audio.Play();
			}
			if (useVolumeInsteadOfPitch)
			{
				audio.volume = Mathf.Max(Mathf.Lerp(audio.volume, highest, lerpSpeed * Time.deltaTime), lowest);
				return;
			}
			audio.volume = Mathf.Lerp(audio.volume, onVolume, 20f * Time.deltaTime);
			audio.pitch = Mathf.Lerp(audio.pitch, highest, lerpSpeed * Time.deltaTime);
		}
		else
		{
			if (useVolumeInsteadOfPitch)
			{
				audio.volume = Mathf.Lerp(audio.volume, 0f, lerpSpeed * Time.deltaTime);
			}
			else
			{
				audio.volume = Mathf.Lerp(audio.volume, 0f, 4f * Time.deltaTime);
				audio.pitch = Mathf.Lerp(audio.pitch, lowest, 4f * Time.deltaTime);
			}
			if (audio.isPlaying && audio.volume == 0f)
			{
				audio.Stop();
			}
		}
	}

	private void FixedUpdate()
	{
		if (!StartOfRound.Instance.inShipPhase)
		{
			if (itemShip == null)
			{
				itemShip = UnityEngine.Object.FindObjectOfType<ItemDropship>();
			}
			if (itemShip != null && !hasBeenSpawned)
			{
				if (itemShip.untetheredVehicle)
				{
					mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
					mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);
					hasBeenSpawned = true;
				}
				else if (itemShip.deliveringVehicle)
				{
					mainRigidbody.isKinematic = true;
					mainRigidbody.MovePosition(itemShip.deliverVehiclePoint.position);
					mainRigidbody.MoveRotation(itemShip.deliverVehiclePoint.rotation);
					if (IsOwner)
					{
						syncedPosition = transform.position;
						syncedRotation = transform.rotation;
					}
				}
				else
				{
					mainRigidbody.isKinematic = true;
					mainRigidbody.MovePosition(StartOfRound.Instance.notSpawnedPosition.position + Vector3.forward * 30f);
				}
			}
		}
		Vector3 vector = Vector3.Cross(Quaternion.AngleAxis(mainRigidbody.angularVelocity.magnitude * 57.29578f * stability / speed, mainRigidbody.angularVelocity) * transform.up, Vector3.up);
		mainRigidbody.AddTorque(vector * speed * speed);
		if (magnetedToShip)
		{
			mainRigidbody.MovePosition(Vector3.Lerp(magnetStartPosition, StartOfRound.Instance.elevatorTransform.position + magnetTargetPosition, magnetPositionCurve.Evaluate(magnetTime)));
			mainRigidbody.MoveRotation(Quaternion.Lerp(magnetStartRotation, magnetTargetRotation, magnetRotationCurve.Evaluate(magnetRotationTime)));
			averageVelocityAtMagnetStart = Vector3.Lerp(averageVelocityAtMagnetStart, Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 4f), 4f * Time.deltaTime);
			if (!finishedMagneting)
			{
				magnetStartPosition += Vector3.ClampMagnitude(averageVelocityAtMagnetStart, 5f) * Time.fixedDeltaTime;
			}
		}
		if (!IsOwner && !testingVehicleInEditor && !magnetedToShip)
		{
			gameObject.GetComponent<Rigidbody>().isKinematic = true;
			Mathf.Clamp(syncSpeedMultiplier * Vector3.Distance(transform.position, syncedPosition), 1.3f, 300f);
			Vector3 position = Vector3.Lerp(transform.position, syncedPosition, Time.fixedDeltaTime * syncSpeedMultiplier);
			mainRigidbody.MovePosition(position);
			mainRigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, syncedRotation, syncRotationSpeed));
			truckVelocityLastFrame = mainRigidbody.velocity;
		}
		ragdollPhysicsBody.Move(transform.position, transform.rotation);
		windwiperPhysicsBody1.Move(windwiper1.position, windwiper1.rotation);
		windwiperPhysicsBody2.Move(windwiper2.position, windwiper2.rotation);
		averageCount++;
		if (averageCount > movingAverageLength)
		{
			averageVelocity += (mainRigidbody.velocity - averageVelocity) / (movingAverageLength + 1);
		}
		else
		{
			averageVelocity += mainRigidbody.velocity;
			if (averageCount == movingAverageLength)
			{
				averageVelocity /= (float)averageCount;
			}
		}
		Debug.DrawRay(transform.position, averageVelocity);
	}

	private void SyncCarPhysicsToOtherClients()
	{
		gameObject.GetComponent<Rigidbody>().isKinematic = false;
		if (syncCarPositionInterval > 0.12f)
		{
			if (Vector3.Distance(syncedPosition, transform.position) > 0.06f)
			{
				syncCarPositionInterval = 0f;
				syncedPosition = transform.position;
				syncedRotation = transform.rotation;
				SyncCarPositionServerRpc(transform.position, transform.eulerAngles, moveInputVector.x, EngineRPM);
			}
			else if (Vector3.Angle(transform.forward, syncedRotation * Vector3.forward) > 2f)
			{
				syncCarPositionInterval = 0f;
				syncedPosition = transform.position;
				syncedRotation = transform.rotation;
				SyncCarPositionServerRpc(transform.position, transform.eulerAngles, moveInputVector.x, EngineRPM);
			}
		}
		else
		{
			syncCarPositionInterval += Time.deltaTime;
		}
	}

	public bool CarReactToObstacle(Vector3 vel, Vector3 position, Vector3 impulse, CarObstacleType type, float obstacleSize = 1f, EnemyAI enemyScript = null, bool dealDamage = true)
	{
		switch (type)
		{
		case CarObstacleType.Object:
			if (carHP < 10)
			{
				mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce + vel, position, ForceMode.Impulse);
			}
			else
			{
				mainRigidbody.AddForceAtPosition((Vector3.up * torqueForce + vel) * 0.5f, position, ForceMode.Impulse);
			}
			CarBumpServerRpc(averageVelocity * 0.7f);
			if (dealDamage)
			{
				DealPermanentDamage(1, position);
			}
			return true;
		case CarObstacleType.Player:
			PlayCollisionAudio(position, 5, Mathf.Clamp(vel.magnitude / 7f, 0.65f, 1f));
			if (vel.magnitude < 4.25f)
			{
				mainRigidbody.velocity = Vector3.Normalize(-impulse * 100000000f) * 9f;
				DealPermanentDamage(1);
				return true;
			}
			mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce, position, ForceMode.VelocityChange);
			return false;
		case CarObstacleType.Enemy:
		{
			if (obstacleSize <= 1f)
			{
				return false;
			}
			float num;
			if (obstacleSize <= 2f)
			{
				num = 9f;
				_ = carReactToPlayerHitMultiplier;
			}
			else
			{
				num = 15f;
				_ = carReactToPlayerHitMultiplier;
			}
			vel = Vector3.Scale(vel, new Vector3(1f, 0f, 1f));
			mainRigidbody.AddForceAtPosition(Vector3.up * torqueForce, position, ForceMode.VelocityChange);
			bool result = false;
			if (vel.magnitude < num)
			{
				Debug.DrawRay(transform.position, Vector3.Normalize(-impulse * 100000000f) * carReactToPlayerHitMultiplier);
				if (obstacleSize <= 1f)
				{
					mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * 4f, ForceMode.Impulse);
					if (vel.magnitude > 1f)
					{
						enemyScript.KillEnemyOnOwnerClient();
					}
				}
				else
				{
					CarBumpServerRpc(averageVelocity);
					mainRigidbody.velocity = Vector3.Normalize(-impulse * 100000000f) * 9f;
					if (currentDriver != null)
					{
						_ = currentDriver;
					}
					if ((vel.magnitude > 2f) & dealDamage)
					{
						enemyScript.HitEnemy(2, currentDriver, playHitSFX: true, 331);
					}
					result = true;
					if (obstacleSize > 2f)
					{
						DealPermanentDamage(1, position);
					}
				}
				Debug.DrawRay(position, -impulse * Mathf.Max(carReactToPlayerHitMultiplier, 500f), Color.red, 5f);
			}
			else
			{
				mainRigidbody.AddForce(Vector3.Normalize(-impulse * 1E+09f) * (carReactToPlayerHitMultiplier - 220f), ForceMode.Impulse);
				if (dealDamage)
				{
					DealPermanentDamage(1, position);
				}
				PlayerControllerB playerWhoHit = ((!(currentDriver != null)) ? currentPassenger : currentDriver);
				enemyScript.HitEnemy(12, playerWhoHit);
				Debug.DrawRay(position, (Vector3.up + -impulse / 2f) * Mathf.Max(vel.magnitude * carReactToPlayerHitMultiplier, 500f), Color.magenta, 5f);
			}
			PlayCollisionAudio(position, 5, 1f);
			return result;
		}
		default:
			return false;
		}
	}

	private void LateUpdate()
	{
		if (localPlayerInControl && !setControlTips)
		{
			setControlTips = true;
			HUDManager.Instance.ChangeControlTipMultiple(carTooltips);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!IsOwner || magnetedToShip || !hasBeenSpawned || collision.collider.gameObject.layer != 8 || averageCount < 18)
		{
			return;
		}
		float num = 0f;
		int num2 = collision.GetContacts(contacts);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < num2; i++)
		{
			if (contacts[i].impulse.magnitude > num)
			{
				num = contacts[i].impulse.magnitude;
			}
			zero += contacts[i].point;
		}
		zero /= (float)num2;
		Debug.DrawRay(zero, Vector3.up, Color.blue, 2f);
		num /= Time.fixedDeltaTime;
		if (num < minimalBumpForce || averageVelocity.magnitude < 4f)
		{
			if (num2 > 3 && averageVelocity.magnitude > 2.5f)
			{
				SetInternalStress(0.2f);
				lastStressType = "Scraping";
			}
			return;
		}
		float setVolume = 0.5f;
		int num3 = -1;
		if (averageVelocity.magnitude > 31f)
		{
			if (carHP < 3)
			{
				DestroyCar();
				DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
				return;
			}
			DealPermanentDamage(carHP - 2);
		}
		else if (averageVelocity.magnitude > 28f)
		{
			DealPermanentDamage(2);
		}
		if (num > maximumBumpForce && averageVelocity.magnitude > 11f)
		{
			num3 = 2;
			setVolume = Mathf.Clamp((num - maximumBumpForce) / 20000f, 0.8f, 1f);
			setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.7f, 1f);
			DealPermanentDamage(1);
		}
		else if (num > mediumBumpForce && averageVelocity.magnitude > 3f)
		{
			num3 = 1;
			setVolume = Mathf.Clamp((num - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.67f, 1f);
			setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.5f, 1f);
		}
		else if (averageVelocity.magnitude > 1.5f)
		{
			num3 = 0;
			setVolume = Mathf.Clamp((num - mediumBumpForce) / (maximumBumpForce - mediumBumpForce), 0.25f, 1f);
			setVolume = Mathf.Clamp(setVolume + UnityEngine.Random.Range(-0.15f, 0.25f), 0.25f, 1f);
		}
		if (num3 != -1)
		{
			PlayCollisionAudio(zero, num3, setVolume);
			if (num > maximumBumpForce + 10000f && averageVelocity.magnitude > 19f)
			{
				DamagePlayerInVehicle(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f), averageVelocity.magnitude);
				BreakWindshield();
				CarCollisionServerRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 60f), averageVelocity.magnitude);
				DealPermanentDamage(2);
			}
			else
			{
				CarBumpServerRpc(Vector3.ClampMagnitude(-collision.relativeVelocity, 40f));
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CarBumpServerRpc(Vector3 vel)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2627964612u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in vel);
				__endSendServerRpc(ref bufferWriter, 2627964612u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				CarBumpClientRpc(vel);
			}
		}
	}

	[ClientRpc]
	public void CarBumpClientRpc(Vector3 vel)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3157053164u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in vel);
			__endSendClientRpc(ref bufferWriter, 3157053164u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (physicsRegion.physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent && ((!localPlayerInControl && !localPlayerInPassengerSeat) || !(vel.magnitude < 50f)))
			{
				GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CarCollisionServerRpc(Vector3 vel, float magn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(2778459828u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in vel);
				bufferWriter.WriteValueSafe(in magn, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 2778459828u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				CarCollisionClientRpc(vel, magn);
			}
		}
	}

	[ClientRpc]
	public void CarCollisionClientRpc(Vector3 vel, float magn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1258964565u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in vel);
			bufferWriter.WriteValueSafe(in magn, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 1258964565u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				DamagePlayerInVehicle(vel, magn);
				BreakWindshield();
			}
		}
	}

	private void DamagePlayerInVehicle(Vector3 vel, float magnitude)
	{
		if (localPlayerInPassengerSeat || localPlayerInControl)
		{
			Debug.DrawRay(GameNetworkManager.Instance.localPlayerController.transform.position, vel, Color.yellow, 10f);
			if (magnitude > 28f)
			{
				GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, transform.up * 0.77f);
			}
			else if (magnitude > 24f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
				if (GameNetworkManager.Instance.localPlayerController.health < 20)
				{
					GameNetworkManager.Instance.localPlayerController.KillPlayer(vel, spawnBody: true, CauseOfDeath.Inertia, 0, transform.up * 0.77f);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
				}
			}
			else
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
				GameNetworkManager.Instance.localPlayerController.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
			}
		}
		else if (physicsRegion.physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent && GameNetworkManager.Instance.localPlayerController.overridePhysicsParent == null)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			GameNetworkManager.Instance.localPlayerController.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Inertia, 0, fallDamage: false, vel);
			GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += vel;
		}
	}

	private void BreakWindshield()
	{
		if (!windshieldBroken)
		{
			windshieldBroken = true;
			windshieldPhysicsCollider.enabled = false;
			Material[] materials = mainBodyMesh.materials;
			materials[2] = windshieldBrokenMat;
			mainBodyMesh.materials = materials;
			glassParticle.Play();
			miscAudio.PlayOneShot(windshieldBreak);
		}
	}

	public void PlayCollisionAudio(Vector3 setPosition, int audioType, float setVolume)
	{
		Debug.Log($"Play collision audio with type {audioType} A");
		if (Time.realtimeSinceStartup - audio1Time > Time.realtimeSinceStartup - audio2Time)
		{
			bool flag = Time.realtimeSinceStartup - audio1Time >= collisionAudio1.clip.length * 0.8f;
			if ((audio1Type <= audioType) | flag)
			{
				audio1Time = Time.realtimeSinceStartup;
				audio1Type = audioType;
				collisionAudio1.transform.position = setPosition;
				PlayRandomClipAndPropertiesFromAudio(collisionAudio1, setVolume, flag, audioType);
				CarCollisionSFXServerRpc(collisionAudio1.transform.localPosition, 0, audioType, setVolume);
			}
		}
		else
		{
			bool flag = Time.realtimeSinceStartup - audio2Time >= collisionAudio2.clip.length * 0.8f;
			if ((audio1Type <= audioType) | flag)
			{
				audio2Time = Time.realtimeSinceStartup;
				audio2Type = audioType;
				collisionAudio2.transform.position = setPosition;
				PlayRandomClipAndPropertiesFromAudio(collisionAudio2, setVolume, flag, audioType);
				CarCollisionSFXServerRpc(collisionAudio2.transform.localPosition, 1, audioType, setVolume);
			}
		}
	}

	[ServerRpc]
	public void CarCollisionSFXServerRpc(Vector3 audioPosition, int audio, int audioType, float vol)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1149255829u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in audioPosition);
			BytePacker.WriteValueBitPacked(bufferWriter, audio);
			BytePacker.WriteValueBitPacked(bufferWriter, audioType);
			bufferWriter.WriteValueSafe(in vol, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 1149255829u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			CarCollisionSFXClientRpc(audioPosition, audio, audioType, vol);
		}
	}

	[ClientRpc]
	public void CarCollisionSFXClientRpc(Vector3 audioPosition, int audio, int audioType, float vol)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(794211159u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in audioPosition);
			BytePacker.WriteValueBitPacked(bufferWriter, audio);
			BytePacker.WriteValueBitPacked(bufferWriter, audioType);
			bufferWriter.WriteValueSafe(in vol, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 794211159u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsOwner)
			{
				AudioSource audioSource = ((audio != 0) ? collisionAudio2 : collisionAudio1);
				bool audioFinished = audioSource.clip.length - audioSource.time < 0.2f;
				audioSource.transform.localPosition = audioPosition;
				PlayRandomClipAndPropertiesFromAudio(audioSource, vol, audioFinished, audioType);
			}
		}
	}

	private void PlayRandomClipAndPropertiesFromAudio(AudioSource audio, float setVolume, bool audioFinished, int audioType)
	{
		if (!audioFinished)
		{
			audio.Stop();
		}
		AudioClip[] array;
		switch (audioType)
		{
		case 0:
			array = minCollisions;
			turbulenceAmount = Mathf.Min(turbulenceAmount + 0.4f, 2f);
			break;
		case 1:
			array = medCollisions;
			turbulenceAmount = Mathf.Min(turbulenceAmount + 0.75f, 2f);
			break;
		case 2:
			array = maxCollisions;
			turbulenceAmount = Mathf.Min(turbulenceAmount + 1.4f, 2f);
			break;
		default:
			array = obstacleCollisions;
			turbulenceAmount = Mathf.Min(turbulenceAmount + 0.75f, 2f);
			break;
		}
		AudioClip audioClip = array[UnityEngine.Random.Range(0, array.Length)];
		if (audioClip == audio.clip && UnityEngine.Random.Range(0, 10) <= 5)
		{
			audioClip = array[UnityEngine.Random.Range(0, array.Length)];
		}
		if (audioFinished)
		{
			audio.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		}
		audio.clip = audioClip;
		audio.PlayOneShot(audioClip, setVolume);
		if (audioType >= 2)
		{
			RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 18f + setVolume / 1f * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
		}
		else if (audioType >= 1)
		{
			RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 12f + setVolume / 1f * 7f, 0.6f, 0, noiseIsInsideClosedShip: false, 2692);
		}
		if (audioType == -1)
		{
			array = minCollisions;
			audioClip = array[UnityEngine.Random.Range(0, array.Length)];
			audio.PlayOneShot(audioClip);
		}
	}

	private void SetInternalStress(float carStressIncrease = 0f)
	{
		if (!(StartOfRound.Instance.testRoom == null) || !StartOfRound.Instance.inShipPhase)
		{
			if (carStressIncrease <= 0f)
			{
				carStressChange = Mathf.Clamp(carStressChange - Time.deltaTime, -0.25f, 0.5f);
			}
			else
			{
				carStressChange = Mathf.Clamp(carStressChange + Time.deltaTime * carStressIncrease, 0f, 10f);
			}
			underExtremeStress = carStressIncrease >= 1f;
			carStress = Mathf.Clamp(carStress + carStressChange, 0f, 100f);
			if (carStress > 7f)
			{
				carStress = 0f;
				DealPermanentDamage(2);
				lastDamageType = "Stress";
			}
		}
	}

	private void DealPermanentDamage(int damageAmount, Vector3 damagePosition = default(Vector3))
	{
		if ((!(StartOfRound.Instance.testRoom == null) || !StartOfRound.Instance.inShipPhase) && !magnetedToShip && !carDestroyed && IsOwner)
		{
			timeAtLastDamage = Time.realtimeSinceStartup;
			carHP -= damageAmount;
			if (carHP <= 0)
			{
				DestroyCar();
				DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			else
			{
				DealDamageServerRpc(damageAmount, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	[ServerRpc]
	public void DealDamageServerRpc(int amount, int sentByClient)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(410929414u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, amount);
			BytePacker.WriteValueBitPacked(bufferWriter, sentByClient);
			__endSendServerRpc(ref bufferWriter, 410929414u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			DealDamageClientRpc(amount, sentByClient);
		}
	}

	[ClientRpc]
	public void DealDamageClientRpc(int amount, int sentByClient)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(625279901u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, amount);
			BytePacker.WriteValueBitPacked(bufferWriter, sentByClient);
			__endSendClientRpc(ref bufferWriter, 625279901u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != sentByClient)
			{
				carHP -= amount;
				timeAtLastDamage = Time.realtimeSinceStartup;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void DestroyCarServerRpc(int sentByClient)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(4012624473u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, sentByClient);
				__endSendServerRpc(ref bufferWriter, 4012624473u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				DestroyCarClientRpc(sentByClient);
			}
		}
	}

	[ClientRpc]
	public void DestroyCarClientRpc(int sentByClient)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1707315732u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, sentByClient);
			__endSendClientRpc(ref bufferWriter, 1707315732u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!carDestroyed)
			{
				DestroyCar();
			}
		}
	}

	private void DestroyCar()
	{
		if (!carDestroyed)
		{
			carDestroyed = true;
			magnetedToShip = false;
			StartOfRound.Instance.isObjectAttachedToMagnet = false;
			CollectItemsInTruck();
			underExtremeStress = false;
			Debug.Log("Destroy truck");
			keyObject.enabled = false;
			engineAudio1.Stop();
			engineAudio2.Stop();
			turbulenceAudio.Stop();
			rollingAudio.Stop();
			radioAudio.Stop();
			extremeStressAudio.Stop();
			honkingHorn = false;
			hornAudio.Stop();
			tireSparks.Stop();
			skiddingAudio.Stop();
			turboBoostAudio.Stop();
			turboBoostParticle.Stop();
			RoundManager.Instance.PlayAudibleNoise(engineAudio1.transform.position, 20f, 0.8f, 0, noiseIsInsideClosedShip: false, 2692);
			FrontLeftWheel.motorTorque = 0f;
			FrontRightWheel.motorTorque = 0f;
			FrontRightWheel.brakeTorque = 0f;
			FrontLeftWheel.brakeTorque = 0f;
			for (int i = 0; i < otherWheels.Length; i++)
			{
				otherWheels[i].motorTorque = 0f;
				otherWheels[i].brakeTorque = 0f;
			}
			leftWheelMesh.enabled = false;
			rightWheelMesh.enabled = false;
			backLeftWheelMesh.enabled = false;
			backRightWheelMesh.enabled = false;
			carHoodAnimator.gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
			backDoorContainer.SetActive(value: false);
			headlightsContainer.SetActive(value: false);
			BreakWindshield();
			destroyedTruckMesh.SetActive(value: true);
			mainBodyMesh.gameObject.SetActive(value: false);
			WheelCollider[] componentsInChildren = gameObject.GetComponentsInChildren<WheelCollider>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].enabled = false;
			}
			mainRigidbody.ResetCenterOfMass();
			mainRigidbody.AddForceAtPosition(Vector3.up * 1560f, hoodFireAudio.transform.position - Vector3.up, ForceMode.Impulse);
			if (localPlayerInControl || localPlayerInPassengerSeat)
			{
				Debug.Log($"Killing player with force magnitude of : {(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere).magnitude}");
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 27f + 20f * UnityEngine.Random.insideUnitSphere, spawnBody: true, CauseOfDeath.Blast, 6, Vector3.up * 1.5f);
			}
			InteractTrigger[] componentsInChildren2 = gameObject.GetComponentsInChildren<InteractTrigger>();
			for (int k = 0; k < componentsInChildren2.Length; k++)
			{
				componentsInChildren2[k].interactable = false;
				componentsInChildren2[k].CancelAnimationExternally();
			}
			Landmine.SpawnExplosion(transform.position + transform.forward + Vector3.up * 1.5f, spawnExplosionEffect: true, 6f, 10f, 30, 200f, truckDestroyedExplosion, goThroughCar: true);
		}
	}

	private void ReactToDamage()
	{
		healthMeter.localScale = new Vector3(1f, 1f, Mathf.Lerp(healthMeter.localScale.z, Mathf.Clamp((float)carHP / (float)baseCarHP, 0.01f, 1f), 6f * Time.deltaTime));
		turboMeter.localScale = new Vector3(1f, 1f, Mathf.Lerp(turboMeter.localScale.z, Mathf.Clamp((float)turboBoosts / 5f, 0.01f, 1f), 6f * Time.deltaTime));
		if (carHP < 16 && Time.realtimeSinceStartup - timeAtLastDamage > 8f)
		{
			timeAtLastDamage = Time.realtimeSinceStartup;
			carHP++;
		}
		if (carHP < 3)
		{
			if (!isHoodOnFire)
			{
				if (!hoodPoppedUp && IsOwner)
				{
					hoodPoppedUp = true;
					SetHoodOpenLocalClient(setOpen: true);
				}
				isHoodOnFire = true;
				hoodFireAudio.Play();
				hoodFireParticle.Play();
			}
		}
		else if (isHoodOnFire)
		{
			isHoodOnFire = false;
			hoodFireAudio.Stop();
			hoodFireParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	public void PushTruckWithArms()
	{
		if (!(GameNetworkManager.Instance.localPlayerController.physicsParent == physicsRegion.transform) && !localPlayerInControl && !localPlayerInPassengerSeat && Physics.Raycast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, out hit, 10f, 1073742656, QueryTriggerInteraction.Ignore))
		{
			Vector3 point = hit.point;
			Vector3 forward = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward;
			if (IsOwner)
			{
				mainRigidbody.AddForceAtPosition(Vector3.Normalize(forward * 1000f) * UnityEngine.Random.Range(40f, 50f) * pushForceMultiplier, point - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
				PushTruckFromOwnerServerRpc(point);
			}
			else
			{
				PushTruckServerRpc(point, forward);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PushTruckServerRpc(Vector3 pos, Vector3 dir)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(4058179333u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in pos);
				bufferWriter.WriteValueSafe(in dir);
				__endSendServerRpc(ref bufferWriter, 4058179333u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PushTruckClientRpc(pos, dir);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PushTruckFromOwnerServerRpc(Vector3 pos)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(1326342869u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in pos);
				__endSendServerRpc(ref bufferWriter, 1326342869u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				PushTruckFromOwnerClientRpc(pos);
			}
		}
	}

	[ClientRpc]
	public void PushTruckClientRpc(Vector3 pushPosition, Vector3 dir)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3138260670u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pushPosition);
			bufferWriter.WriteValueSafe(in dir);
			__endSendClientRpc(ref bufferWriter, 3138260670u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			pushAudio.transform.position = pushPosition;
			pushAudio.Play();
			turbulenceAmount = Mathf.Min(turbulenceAmount + 0.5f, 2f);
			if (IsOwner)
			{
				mainRigidbody.AddForceAtPosition(Vector3.Normalize(dir * 1000f) * UnityEngine.Random.Range(40f, 50f) * pushForceMultiplier, pushPosition - mainRigidbody.transform.up * pushVerticalOffsetAmount, ForceMode.Impulse);
			}
		}
	}

	[ClientRpc]
	public void PushTruckFromOwnerClientRpc(Vector3 pos)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(4025368226u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in pos);
				__endSendClientRpc(ref bufferWriter, 4025368226u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				pushAudio.transform.position = pos;
				pushAudio.Play();
				turbulenceAmount = Mathf.Min(turbulenceAmount + 0.5f, 2f);
			}
		}
	}

	public void ToggleHoodOpenLocalClient()
	{
		carHoodOpen = !carHoodOpen;
		carHoodAnimator.SetBool("hoodOpen", carHoodOpen);
		miscAudio.transform.position = carHoodAnimator.transform.position;
		if (carHoodOpen)
		{
			miscAudio.PlayOneShot(carHoodOpenSFX);
		}
		else
		{
			miscAudio.PlayOneShot(carHoodCloseSFX);
		}
		SetHoodOpenServerRpc(carHoodOpen);
	}

	public void SetHoodOpenLocalClient(bool setOpen)
	{
		if (carHoodOpen != setOpen)
		{
			SetHoodOpenServerRpc(open: true);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetHoodOpenServerRpc(bool open)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(3804995530u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in open, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 3804995530u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SetHoodOpenClientRpc(open);
			}
		}
	}

	[ClientRpc]
	public void SetHoodOpenClientRpc(bool open)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(673717576u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in open, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 673717576u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (carHoodOpen != open)
		{
			carHoodOpen = open;
			carHoodAnimator.SetBool("hoodOpen", open);
			miscAudio.transform.position = carHoodAnimator.transform.position;
			if (open)
			{
				miscAudio.PlayOneShot(carHoodOpenSFX);
			}
			else
			{
				miscAudio.PlayOneShot(carHoodCloseSFX);
			}
		}
	}

	public void ToggleHeadlightsLocalClient()
	{
		headlightsContainer.SetActive(!headlightsContainer.activeSelf);
		miscAudio.transform.position = headlightsContainer.transform.position;
		miscAudio.PlayOneShot(headlightsToggleSFX);
		SetHeadlightMaterial(headlightsContainer.activeSelf);
		ToggleHeadlightsServerRpc(headlightsContainer.activeSelf);
	}

	[ServerRpc(RequireOwnership = false)]
	public void ToggleHeadlightsServerRpc(bool setLightsOn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(369816798u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 369816798u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				ToggleHeadlightsClientRpc(setLightsOn);
			}
		}
	}

	[ClientRpc]
	public void ToggleHeadlightsClientRpc(bool setLightsOn)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(3014013968u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
			__endSendClientRpc(ref bufferWriter, 3014013968u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (headlightsContainer.activeSelf != setLightsOn)
			{
				headlightsContainer.SetActive(setLightsOn);
				miscAudio.transform.position = headlightsContainer.transform.position;
				miscAudio.PlayOneShot(headlightsToggleSFX);
				SetHeadlightMaterial(setLightsOn);
			}
		}
	}

	private void SetHeadlightMaterial(bool on)
	{
		Material material = ((!on) ? headlightsOffMat : headlightsOnMat);
		Material[] sharedMaterials = mainBodyMesh.sharedMaterials;
		sharedMaterials[1] = material;
		mainBodyMesh.sharedMaterials = sharedMaterials;
		sharedMaterials = lod1Mesh.sharedMaterials;
		sharedMaterials[1] = material;
		mainBodyMesh.sharedMaterials = sharedMaterials;
		sharedMaterials = mainBodyMesh.sharedMaterials;
		sharedMaterials[1] = material;
		mainBodyMesh.sharedMaterials = sharedMaterials;
	}

	public void SpringDriverSeatLocalClient()
	{
		if (!(Time.realtimeSinceStartup - timeSinceSpringingDriverSeat < 3f))
		{
			timeSinceSpringingDriverSeat = Time.realtimeSinceStartup;
			SpringDriverSeatServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SpringDriverSeatServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendServerRpc(46143233u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 46143233u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				SpringDriverSeatClientRpc();
			}
		}
	}

	[ClientRpc]
	public void SpringDriverSeatClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(818134562u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 818134562u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			timeSinceSpringingDriverSeat = Time.realtimeSinceStartup;
			springAudio.Play();
			driverSeatSpringAnimator.SetTrigger("spring");
			if (localPlayerInControl || Vector3.Distance(transform.position, springAudio.transform.position) < 1.25f)
			{
				currentDriver.externalForceAutoFade += transform.up * springForce;
			}
			RoundManager.Instance.PlayAudibleNoise(springAudio.transform.position, 30f, 1f, 0, noiseIsInsideClosedShip: false, 2692);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(269855870u, __rpc_handler_269855870, "RemoveKeyFromIgnitionServerRpc");
		__registerRpc(1127926854u, __rpc_handler_1127926854, "RemoveKeyFromIgnitionClientRpc");
		__registerRpc(1319663544u, __rpc_handler_1319663544, "RevCarServerRpc");
		__registerRpc(3214494774u, __rpc_handler_3214494774, "RevCarClientRpc");
		__registerRpc(2403570091u, __rpc_handler_2403570091, "StartIgnitionServerRpc");
		__registerRpc(3273216474u, __rpc_handler_3273216474, "StartIgnitionClientRpc");
		__registerRpc(835626980u, __rpc_handler_835626980, "TryIgnitionServerRpc");
		__registerRpc(3548459446u, __rpc_handler_3548459446, "TryIgnitionClientRpc");
		__registerRpc(4283235241u, __rpc_handler_4283235241, "CancelTryIgnitionServerRpc");
		__registerRpc(2096620717u, __rpc_handler_2096620717, "CancelTryIgnitionClientRpc");
		__registerRpc(2150817317u, __rpc_handler_2150817317, "PassengerLeaveVehicleServerRpc");
		__registerRpc(46680660u, __rpc_handler_46680660, "PassengerLeaveVehicleClientRpc");
		__registerRpc(2687785832u, __rpc_handler_2687785832, "SetPlayerInControlOfVehicleServerRpc");
		__registerRpc(1621098866u, __rpc_handler_1621098866, "CancelPlayerInControlOfVehicleClientRpc");
		__registerRpc(2345405857u, __rpc_handler_2345405857, "RemovePlayerControlOfVehicleServerRpc");
		__registerRpc(2569589533u, __rpc_handler_2569589533, "SetPlayerInControlOfVehicleClientRpc");
		__registerRpc(1936869671u, __rpc_handler_1936869671, "RemovePlayerControlOfVehicleClientRpc");
		__registerRpc(1427257619u, __rpc_handler_1427257619, "ShiftToGearServerRpc");
		__registerRpc(2335366121u, __rpc_handler_2335366121, "ShiftToGearClientRpc");
		__registerRpc(2803421723u, __rpc_handler_2803421723, "SyncCarPositionServerRpc");
		__registerRpc(3105401376u, __rpc_handler_3105401376, "SyncCarPositionClientRpc");
		__registerRpc(2451439781u, __rpc_handler_2451439781, "MagnetCarServerRpc");
		__registerRpc(2845017736u, __rpc_handler_2845017736, "MagnetCarClientRpc");
		__registerRpc(735895017u, __rpc_handler_735895017, "SetHonkServerRpc");
		__registerRpc(2121824285u, __rpc_handler_2121824285, "SetHonkClientRpc");
		__registerRpc(2416458891u, __rpc_handler_2416458891, "AddTurboBoostServerRpc");
		__registerRpc(4268487771u, __rpc_handler_4268487771, "AddTurboBoostClientRpc");
		__registerRpc(548979688u, __rpc_handler_548979688, "AddEngineOilServerRpc");
		__registerRpc(2079068163u, __rpc_handler_2079068163, "AddEngineOilClientRpc");
		__registerRpc(1878146525u, __rpc_handler_1878146525, "UseTurboBoostServerRpc");
		__registerRpc(4076738570u, __rpc_handler_4076738570, "UseTurboBoostClientRpc");
		__registerRpc(721150963u, __rpc_handler_721150963, "SetRadioStationServerRpc");
		__registerRpc(3091363772u, __rpc_handler_3091363772, "SetRadioStationClientRpc");
		__registerRpc(2416589835u, __rpc_handler_2416589835, "SetRadioOnServerRpc");
		__registerRpc(2165949877u, __rpc_handler_2165949877, "SetRadioOnClientRpc");
		__registerRpc(2043456042u, __rpc_handler_2043456042, "SetRadioSignalQualityServerRpc");
		__registerRpc(894646603u, __rpc_handler_894646603, "SetRadioSignalQualityClientRpc");
		__registerRpc(3603115648u, __rpc_handler_3603115648, "SyncExtremeStressServerRpc");
		__registerRpc(3722438677u, __rpc_handler_3722438677, "SyncExtremeStressClientRpc");
		__registerRpc(2627964612u, __rpc_handler_2627964612, "CarBumpServerRpc");
		__registerRpc(3157053164u, __rpc_handler_3157053164, "CarBumpClientRpc");
		__registerRpc(2778459828u, __rpc_handler_2778459828, "CarCollisionServerRpc");
		__registerRpc(1258964565u, __rpc_handler_1258964565, "CarCollisionClientRpc");
		__registerRpc(1149255829u, __rpc_handler_1149255829, "CarCollisionSFXServerRpc");
		__registerRpc(794211159u, __rpc_handler_794211159, "CarCollisionSFXClientRpc");
		__registerRpc(410929414u, __rpc_handler_410929414, "DealDamageServerRpc");
		__registerRpc(625279901u, __rpc_handler_625279901, "DealDamageClientRpc");
		__registerRpc(4012624473u, __rpc_handler_4012624473, "DestroyCarServerRpc");
		__registerRpc(1707315732u, __rpc_handler_1707315732, "DestroyCarClientRpc");
		__registerRpc(4058179333u, __rpc_handler_4058179333, "PushTruckServerRpc");
		__registerRpc(1326342869u, __rpc_handler_1326342869, "PushTruckFromOwnerServerRpc");
		__registerRpc(3138260670u, __rpc_handler_3138260670, "PushTruckClientRpc");
		__registerRpc(4025368226u, __rpc_handler_4025368226, "PushTruckFromOwnerClientRpc");
		__registerRpc(3804995530u, __rpc_handler_3804995530, "SetHoodOpenServerRpc");
		__registerRpc(673717576u, __rpc_handler_673717576, "SetHoodOpenClientRpc");
		__registerRpc(369816798u, __rpc_handler_369816798, "ToggleHeadlightsServerRpc");
		__registerRpc(3014013968u, __rpc_handler_3014013968, "ToggleHeadlightsClientRpc");
		__registerRpc(46143233u, __rpc_handler_46143233, "SpringDriverSeatServerRpc");
		__registerRpc(818134562u, __rpc_handler_818134562, "SpringDriverSeatClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_269855870(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RemoveKeyFromIgnitionServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1127926854(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RemoveKeyFromIgnitionClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1319663544(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RevCarServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3214494774(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RevCarClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2403570091(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).StartIgnitionServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3273216474(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).StartIgnitionClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_835626980(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).TryIgnitionServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3548459446(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).TryIgnitionClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4283235241(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CancelTryIgnitionServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2096620717(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CancelTryIgnitionClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2150817317(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PassengerLeaveVehicleServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_46680660(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PassengerLeaveVehicleClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2687785832(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetPlayerInControlOfVehicleServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1621098866(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CancelPlayerInControlOfVehicleClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2345405857(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out Vector3 value2);
			reader.ReadValueSafe(out Quaternion value3);
			reader.ReadValueSafe(out bool value4, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RemovePlayerControlOfVehicleServerRpc(value, value2, value3, value4);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2569589533(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetPlayerInControlOfVehicleClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1936869671(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).RemovePlayerControlOfVehicleClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1427257619(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).ShiftToGearServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2335366121(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).ShiftToGearClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2803421723(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			return;
		}
		reader.ReadValueSafe(out Vector3 value);
		reader.ReadValueSafe(out Vector3 value2);
		reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
		reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((VehicleController)target).SyncCarPositionServerRpc(value, value2, value3, value4);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_3105401376(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SyncCarPositionClientRpc(value, value2, value3, value4);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2451439781(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			return;
		}
		reader.ReadValueSafe(out Vector3 value);
		reader.ReadValueSafe(out Vector3 value2);
		ByteUnpacker.ReadValueBitPacked(reader, out int value3);
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((VehicleController)target).MagnetCarServerRpc(value, value2, value3);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_2845017736(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).MagnetCarClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_735895017(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetHonkServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2121824285(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetHonkClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2416458891(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).AddTurboBoostServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4268487771(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).AddTurboBoostClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_548979688(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).AddEngineOilServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2079068163(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).AddEngineOilClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1878146525(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).UseTurboBoostServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4076738570(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).UseTurboBoostClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_721150963(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetRadioStationServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3091363772(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetRadioStationClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2416589835(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetRadioOnServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2165949877(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetRadioOnClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2043456042(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((VehicleController)target).SetRadioSignalQualityServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_894646603(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetRadioSignalQualityClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3603115648(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((VehicleController)target).SyncExtremeStressServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3722438677(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SyncExtremeStressClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2627964612(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CarBumpServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3157053164(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CarBumpClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_2778459828(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CarCollisionServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1258964565(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CarCollisionClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1149255829(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			return;
		}
		reader.ReadValueSafe(out Vector3 value);
		ByteUnpacker.ReadValueBitPacked(reader, out int value2);
		ByteUnpacker.ReadValueBitPacked(reader, out int value3);
		reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
		target.__rpc_exec_stage = __RpcExecStage.Execute;
		((VehicleController)target).CarCollisionSFXServerRpc(value, value2, value3, value4);
		target.__rpc_exec_stage = __RpcExecStage.Send;
	}

	private static void __rpc_handler_794211159(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			reader.ReadValueSafe(out float value4, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).CarCollisionSFXClientRpc(value, value2, value3, value4);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_410929414(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).DealDamageServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_625279901(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).DealDamageClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4012624473(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).DestroyCarServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1707315732(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).DestroyCarClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4058179333(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PushTruckServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1326342869(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PushTruckFromOwnerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3138260670(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Vector3 value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PushTruckClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_4025368226(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).PushTruckFromOwnerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3804995530(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetHoodOpenServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_673717576(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SetHoodOpenClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_369816798(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).ToggleHeadlightsServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3014013968(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).ToggleHeadlightsClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_46143233(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SpringDriverSeatServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_818134562(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((VehicleController)target).SpringDriverSeatClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "VehicleController";
	}
}
