using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class TimeOfDay : NetworkBehaviour
{
	[Header("Time")]
	public SelectableLevel currentLevel;

	public float globalTimeSpeedMultiplier = 1f;

	public float currentDayTime;

	public int hour;

	private int previousHour;

	public float normalizedTimeOfDay;

	public bool timeHasStarted;

	[Space(5f)]
	public float globalTime;

	public float globalTimeAtEndOfDay;

	public bool movingGlobalTimeForward;

	[Space(10f)]
	public QuotaSettings quotaVariables;

	public int profitQuota;

	public int quotaFulfilled;

	public int timesFulfilledQuota;

	public float timeUntilDeadline;

	public int daysUntilDeadline;

	public int hoursUntilDeadline;

	[Space(5f)]
	public float lengthOfHours = 100f;

	public int numberOfHours = 7;

	public float totalTime;

	public const int startingGlobalTime = 100;

	[Space(3f)]
	public float shipLeaveAutomaticallyTime = 0.996f;

	[Space(5f)]
	public bool currentDayTimeStarted;

	private bool timeStartedThisFrame = true;

	public StartOfRound playersManager;

	public Animator sunAnimator;

	public Light sunIndirect;

	public Light sunDirect;

	public bool insideLighting = true;

	public DayMode dayMode;

	private DayMode dayModeLastTimePlayerWasOutside;

	public AudioClip[] timeOfDayCues;

	public AudioSource TimeOfDayMusic;

	private HDAdditionalLightData indirectLightData;

	[Header("Weather")]
	public WeatherEffect[] effects;

	public LevelWeatherType currentLevelWeather = LevelWeatherType.None;

	public float currentWeatherVariable;

	public float currentWeatherVariable2;

	public Color currentWeatherVariableColor;

	public Color defaultWeatherColor;

	[Space(2f)]
	public LocalVolumetricFog foggyWeather;

	[Space(4f)]
	public CompanyMood currentCompanyMood;

	public CompanyMood[] CommonCompanyMoods;

	[Space(4f)]
	private float changeHUDTimeInterval;

	private float nextTimeSync;

	public bool shipLeavingAlertCalled;

	public DialogueSegment[] shipLeavingSoonDialogue;

	public DialogueSegment[] shipLeavingEarlyDialogue;

	private bool shipLeavingOnMidnight;

	private Coroutine playDelayedMusicCoroutine;

	public int votesForShipToLeaveEarly;

	public bool votedShipToLeaveEarlyThisRound;

	public UnityEvent onTimeSync = new UnityEvent();

	public UnityEvent onHourChanged = new UnityEvent();

	public float meteorShowerAtTime = -1f;

	public MeteorShowers MeteorWeather;

	public int overrideMeteorChance = -1;

	public List<int> furniturePlacedAtQuotaStart = new List<int>();

	public float luckValue;

	public bool hasShownAdThisQuota;

	public float normalizedTimeToShowAd = -1f;

	private float adWaitInterval = 10f;

	private int gotInfoFromClientsForAd;

	private bool checkingIfClientsAreReadyForAd;

	private bool doClientsMeetRequirementsForAd;

	private Coroutine adGetClientInfoCoroutine;

	private Collider[] playerColliders = new Collider[4];

	public GameObject FloodWeatherNavArea;

	public ParticleSystem waterSplashParticle;

	public ParticleSystem waterSplashParticleSmall;

	private bool placedFloodNavMesh;

	public static TimeOfDay Instance { get; private set; }

	[Rpc(SendTo.NotMe)]
	public void SplashWaterRpc(Vector3 position, bool smallSplash = false)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = default;
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(2756840554u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in position);
				bufferWriter.WriteValueSafe(in smallSplash, default(FastBufferWriter.ForPrimitives));
				__endSendRpc(ref bufferWriter, 2756840554u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				WaterSplashEffect(position, smallSplash);
			}
		}
	}

	public void WaterSplashEffect(Vector3 splashPosition, bool smallSplash = false, bool syncToServer = false)
	{
		SoundManager.Instance.tempAudio2.transform.position = splashPosition;
		if (smallSplash)
		{
			waterSplashParticleSmall.gameObject.transform.position = splashPosition;
			waterSplashParticleSmall.Play(withChildren: true);
			RoundManager.PlayRandomClip(SoundManager.Instance.tempAudio2, SoundManager.Instance.waterSplashSFXSmall, randomize: true, 1f, 127058);
		}
		else
		{
			waterSplashParticle.gameObject.transform.position = splashPosition;
			waterSplashParticle.Play(withChildren: true);
			RoundManager.PlayRandomClip(SoundManager.Instance.tempAudio2, SoundManager.Instance.waterSplashSFX, randomize: true, 1f, 127058);
		}
		if (syncToServer)
		{
			SplashWaterRpc(splashPosition, smallSplash);
		}
	}

	public void SetWeatherBasedOnVariables(RandomWeatherWithVariables weatherVariables)
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 101);
		if (currentLevelWeather == LevelWeatherType.Foggy)
		{
			foggyWeather.parameters.meanFreePath = random.Next((int)currentWeatherVariable, (int)currentWeatherVariable2);
			foggyWeather.parameters.albedo = currentWeatherVariableColor;
		}
		if (currentLevelWeather != LevelWeatherType.Flooded)
		{
			return;
		}
		float y = Mathf.Lerp(weatherVariables.weatherVariable, weatherVariables.weatherVariable2, 0.225f);
		FloodWeatherNavArea.transform.position = new Vector3(FloodWeatherNavArea.transform.position.x, y, FloodWeatherNavArea.transform.position.x);
		if (!placedFloodNavMesh)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("OutsideLevelNavMesh");
			if (gameObject != null)
			{
				placedFloodNavMesh = true;
				UnityEngine.Object.Instantiate(FloodWeatherNavArea, gameObject.transform, worldPositionStays: true).SetActive(value: true);
			}
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			return;
		}
		UnityEngine.Object.Destroy(Instance.gameObject);
		Instance = this;
	}

	private void Start()
	{
		playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		totalTime = lengthOfHours * (float)numberOfHours;
		SetCompanyMood();
		try
		{
			hasShownAdThisQuota = ES3.Load("ShownAdThisQuota", GameNetworkManager.Instance.currentSaveFileName, defaultValue: false);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public void DecideRandomDayEvents()
	{
		if (IsServer)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 28);
			int num = 7;
			if ((float)overrideMeteorChance != -1f)
			{
				num = overrideMeteorChance;
			}
			if (random.Next(0, 1000) < num)
			{
				meteorShowerAtTime = (float)random.Next(5, 80) / 100f;
			}
			else
			{
				meteorShowerAtTime = -1f;
			}
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		_ = movingGlobalTimeForward;
		if (currentDayTimeStarted)
		{
			if (timeStartedThisFrame)
			{
				timeStartedThisFrame = false;
				nextTimeSync = 0f;
				TimeOfDayMusic.volume = 0.7f;
				dayModeLastTimePlayerWasOutside = DayMode.None;
				shipLeavingOnMidnight = false;
				shipLeavingAlertCalled = false;
				votedShipToLeaveEarlyThisRound = false;
				shipLeaveAutomaticallyTime = 0.998f;
				votesForShipToLeaveEarly = 0;
				currentDayTime = CalculatePlanetTime(currentLevel);
				hour = (int)(currentDayTime / lengthOfHours);
				previousHour = hour;
				indirectLightData = null;
				globalTimeAtEndOfDay = globalTime + (totalTime - currentDayTime) / currentLevel.DaySpeedMultiplier;
				normalizedTimeOfDay = currentDayTime / totalTime;
				SetTimeForAdToPlay();
				RefreshClockUI();
				if (IsServer)
				{
					DecideRandomDayEvents();
				}
				timeHasStarted = true;
			}
			else
			{
				MoveTimeOfDay();
				TimeOfDayEvents();
				SetWeatherEffects();
			}
		}
		else
		{
			timeStartedThisFrame = true;
			timeHasStarted = false;
			placedFloodNavMesh = false;
			if (MeteorWeather.meteorsEnabled)
			{
				MeteorWeather.ResetMeteorWeather();
			}
		}
	}

	public void MoveGlobalTime()
	{
		float num = globalTime;
		globalTime = Mathf.Clamp(globalTime + Time.deltaTime * globalTimeSpeedMultiplier, 0f, globalTimeAtEndOfDay);
		num = globalTime - num;
		timeUntilDeadline -= num;
	}

	public float CalculatePlanetTime(SelectableLevel level)
	{
		return (globalTime + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	public float CalculatePlanetTimeClampToEndOfDay(SelectableLevel level)
	{
		return (Mathf.Clamp(globalTime, 0f, globalTimeAtEndOfDay) + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	private void MoveTimeOfDay()
	{
		try
		{
			MoveGlobalTime();
			SyncGlobalTimeOnNetwork();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error updating time of day: {arg}");
		}
		currentDayTime = CalculatePlanetTime(currentLevel);
		hour = (int)(currentDayTime / lengthOfHours);
		if (hour != previousHour)
		{
			previousHour = hour;
			OnHourChanged();
		}
		if (sunAnimator != null)
		{
			normalizedTimeOfDay = currentDayTime / totalTime;
			sunAnimator.SetFloat("timeOfDay", Mathf.Clamp(normalizedTimeOfDay, 0f, 0.99f));
			if (changeHUDTimeInterval > 3f)
			{
				changeHUDTimeInterval = 0f;
				HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
			}
			else
			{
				changeHUDTimeInterval += Time.deltaTime;
			}
			SetInsideLightingDimness();
		}
		if (IsServer && meteorShowerAtTime > 0f && normalizedTimeOfDay >= meteorShowerAtTime)
		{
			meteorShowerAtTime = -1f;
			MeteorWeather.SetStartMeteorShower();
		}
	}

	public void SetInsideLightingDimness(bool doNotLerp = false, bool setValueTo = false)
	{
		if (sunDirect == null || sunIndirect == null)
		{
			return;
		}
		if (indirectLightData == null)
		{
			indirectLightData = sunIndirect.GetComponent<HDAdditionalLightData>();
		}
		HUDManager.Instance.SetClockVisible(!insideLighting);
		if (GameNetworkManager.Instance != null)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
				{
					if (StartOfRound.Instance.allPlayersDead || GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isPlayerDead)
					{
						sunDirect.enabled = true;
					}
					else
					{
						sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory;
					}
				}
			}
			else
			{
				sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.isInsideFactory;
			}
		}
		sunIndirect.enabled = sunDirect.enabled;
		if (StartOfRound.Instance.allPlayersDead)
		{
			insideLighting = false;
		}
		if (insideLighting)
		{
			indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 0f, 5f * Time.deltaTime);
		}
		else
		{
			indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 1f, 5f * Time.deltaTime);
		}
	}

	private int RoundUpToNearestTen(float x)
	{
		return (int)(x / 10f) * 10;
	}

	private void SyncGlobalTimeOnNetwork()
	{
		if (IsServer && (float)RoundUpToNearestTen(globalTime) >= nextTimeSync)
		{
			nextTimeSync = RoundUpToNearestTen(globalTime + 10f);
			SyncTimeClientRpc(globalTime, (int)timeUntilDeadline);
		}
	}

	[ClientRpc]
	public void SyncTimeClientRpc(float time, int deadline)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3168707752u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, deadline);
				__endSendClientRpc(ref bufferWriter, 3168707752u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				globalTime = time;
				timeUntilDeadline = deadline;
				onTimeSync.Invoke();
			}
		}
	}

	public void SetTimeForAdToPlay()
	{
		if (IsServer && !hasShownAdThisQuota && timesFulfilledQuota != 0)
		{
			float num = 33f;
			if (daysUntilDeadline <= 1)
			{
				num = 60f;
			}
			if ((float)UnityEngine.Random.Range(0, 100) < num)
			{
				normalizedTimeToShowAd = UnityEngine.Random.Range(0.04f, 0.7f);
			}
		}
	}

	private bool MeetsRequirementsToShowAd()
	{
		if (StartOfRound.Instance.timeSinceRoundStarted <= 15f)
		{
			return false;
		}
		if (timesFulfilledQuota == 0)
		{
			return false;
		}
		if (StartOfRound.Instance.livingPlayers <= 1)
		{
			return false;
		}
		return true;
	}

	private IEnumerator GetClientInfo()
	{
		float timeAtChecking = Time.realtimeSinceStartup;
		checkingIfClientsAreReadyForAd = true;
		doClientsMeetRequirementsForAd = true;
		Debug.Log("Ad system: Setting gotinfo to 0");
		gotInfoFromClientsForAd = 0;
		SendHostInfoForShowingAdClientRpc();
		yield return new WaitUntil(() => gotInfoFromClientsForAd >= StartOfRound.Instance.livingPlayers || Time.realtimeSinceStartup - timeAtChecking > 5f);
		if (doClientsMeetRequirementsForAd)
		{
			hasShownAdThisQuota = true;
			HUDManager.Instance.ChooseAdItem();
			normalizedTimeToShowAd = -1f;
		}
		else
		{
			checkingIfClientsAreReadyForAd = false;
			adWaitInterval = 15f;
		}
		adGetClientInfoCoroutine = null;
	}

	[ClientRpc]
	public void SendHostInfoForShowingAdClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(990741385u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 990741385u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		Debug.Log("Received client rpc Ad");
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			return;
		}
		bool doesntMeetRequirements = false;
		float num = Mathf.Max(GameNetworkManager.Instance.localPlayerController.timeSinceTakingDamage, GameNetworkManager.Instance.localPlayerController.timeSinceFearLevelUp);
		if (num > 12f)
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory && Physics.CheckSphere(GameNetworkManager.Instance.localPlayerController.transform.position, 20f, 524288, QueryTriggerInteraction.Collide))
			{
				doesntMeetRequirements = true;
				Debug.Log("Can't show ad client; 1");
			}
		}
		else if (num < 1.7f && GameNetworkManager.Instance.localPlayerController.isInsideFactory && Physics.CheckSphere(GameNetworkManager.Instance.localPlayerController.transform.position, 12f, 524288, QueryTriggerInteraction.Collide))
		{
			doesntMeetRequirements = true;
			Debug.Log("Can't show ad client; 2");
		}
		ReceiveInfoFromClientForShowingAdServerRpc(doesntMeetRequirements);
	}

	[ServerRpc(RequireOwnership = false)]
	public void ReceiveInfoFromClientForShowingAdServerRpc(bool doesntMeetRequirements)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(3562921089u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in doesntMeetRequirements, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3562921089u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		__rpc_exec_stage = __RpcExecStage.Send;
		if (checkingIfClientsAreReadyForAd)
		{
			Debug.Log("Adding 1 to gotInfoFromClients");
			gotInfoFromClientsForAd++;
			if (doesntMeetRequirements)
			{
				doClientsMeetRequirementsForAd = false;
			}
		}
	}

	private void DisplayAdAtScheduledTime()
	{
		if (!IsServer || hasShownAdThisQuota || normalizedTimeToShowAd == -1f || normalizedTimeOfDay >= 0.9f || checkingIfClientsAreReadyForAd || StartOfRound.Instance.livingPlayers <= 1)
		{
			return;
		}
		if (adWaitInterval <= 0f)
		{
			if (!(normalizedTimeOfDay > normalizedTimeToShowAd))
			{
				return;
			}
			if (MeetsRequirementsToShowAd())
			{
				if (!checkingIfClientsAreReadyForAd && adGetClientInfoCoroutine == null)
				{
					adGetClientInfoCoroutine = StartCoroutine(GetClientInfo());
				}
			}
			else
			{
				adWaitInterval = 15f;
			}
		}
		else
		{
			adWaitInterval -= Time.deltaTime;
		}
	}

	public void TimeOfDayEvents()
	{
		dayMode = GetDayPhase(currentDayTime / totalTime);
		if (currentLevel.planetHasTime && !StartOfRound.Instance.shipIsLeaving)
		{
			if (!shipLeavingAlertCalled && currentDayTime / totalTime > 0.9f)
			{
				shipLeavingAlertCalled = true;
				HUDManager.Instance.ReadDialogue(shipLeavingSoonDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}
			DisplayAdAtScheduledTime();
			if (IsServer && !shipLeavingOnMidnight && currentDayTime / totalTime >= shipLeaveAutomaticallyTime)
			{
				shipLeavingOnMidnight = true;
				SetShipToLeaveOnMidnightClientRpc();
			}
		}
		if (dayMode > dayModeLastTimePlayerWasOutside)
		{
			PlayerSeesNewTimeOfDay();
		}
	}

	public void CalculateLuckValue()
	{
		if (timesFulfilledQuota == 0)
		{
			AutoParentToShip[] array = UnityEngine.Object.FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
				{
					furniturePlacedAtQuotaStart.Add(array[i].unlockableID);
				}
			}
		}
		luckValue = 0f;
		for (int j = 0; j < furniturePlacedAtQuotaStart.Count; j++)
		{
			if (furniturePlacedAtQuotaStart[j] > StartOfRound.Instance.unlockablesList.unlockables.Count)
			{
				Debug.LogError($"'Lucky' furniture with id {furniturePlacedAtQuotaStart[j]} exceeded the unlockables list size; skipping");
			}
			luckValue = Mathf.Clamp(luckValue + StartOfRound.Instance.unlockablesList.unlockables[furniturePlacedAtQuotaStart[j]].luckValue, -0.5f, 1f);
		}
		Debug.Log($"Luck calculated: {luckValue}");
	}

	public void SetNewProfitQuota()
	{
		if (!IsServer)
		{
			return;
		}
		timesFulfilledQuota++;
		int num = quotaFulfilled - profitQuota;
		float num2 = Mathf.Clamp(1f + (float)timesFulfilledQuota * ((float)timesFulfilledQuota / quotaVariables.increaseSteepness), 0f, 10000f);
		CalculateLuckValue();
		float num3 = UnityEngine.Random.Range(0f, 1f);
		Debug.Log($"Randomizer amount before: {num3}; adding luck value which is {Mathf.Clamp(luckValue * 2f, 0f, 1f)}");
		num3 = Mathf.Clamp(num3 - luckValue * 1.5f, 0f, 1f);
		Debug.Log($"Randomizer amount after: {num3}");
		num2 = quotaVariables.baseIncrease * num2 * (quotaVariables.randomizerCurve.Evaluate(num3) * quotaVariables.randomizerMultiplier + 1f);
		Debug.Log($"Amount to increase quota:{num2}");
		profitQuota = (int)Mathf.Clamp((float)profitQuota + num2, 0f, 1E+09f);
		quotaFulfilled = 0;
		timeUntilDeadline = totalTime * 4f;
		int overtimeBonus = num / 5 + 15 * daysUntilDeadline;
		furniturePlacedAtQuotaStart.Clear();
		AutoParentToShip[] array = UnityEngine.Object.FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
			{
				furniturePlacedAtQuotaStart.Add(array[i].unlockableID);
			}
		}
		hasShownAdThisQuota = false;
		SyncNewProfitQuotaClientRpc(profitQuota, overtimeBonus, timesFulfilledQuota);
	}

	[ClientRpc]
	public void SyncNewProfitQuotaClientRpc(int newProfitQuota, int overtimeBonus, int fulfilledQuota)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(1041683203u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, newProfitQuota);
				BytePacker.WriteValueBitPacked(bufferWriter, overtimeBonus);
				BytePacker.WriteValueBitPacked(bufferWriter, fulfilledQuota);
				__endSendClientRpc(ref bufferWriter, 1041683203u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				quotaFulfilled = 0;
				profitQuota = newProfitQuota;
				timeUntilDeadline = totalTime * (float)quotaVariables.deadlineDaysAmount;
				timesFulfilledQuota = fulfilledQuota;
				StartOfRound.Instance.companyBuyingRate = 0.3f;
				Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
				terminal.groupCredits = Mathf.Clamp(terminal.groupCredits + overtimeBonus, terminal.groupCredits, 100000000);
				terminal.RotateShipDecorSelection();
				HUDManager.Instance.DisplayNewDeadline(overtimeBonus);
			}
		}
	}

	public void UpdateProfitQuotaCurrentTime()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		hoursUntilDeadline = (int)(timeUntilDeadline / lengthOfHours) - daysUntilDeadline * numberOfHours;
		if (StartOfRound.Instance.isChallengeFile)
		{
			StartOfRound.Instance.deadlineMonitorBGImage.color = new Color(135f / 255f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.profitQuotaMonitorBGImage.color = new Color(135f / 255f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.deadlineMonitorText.text = "AS MUCH PROFIT AS POSSIBLE";
			StartOfRound.Instance.profitQuotaMonitorText.text = "Welcome to\n" + GameNetworkManager.Instance.GetNameForWeekNumber();
			StartOfRound.Instance.profitQuotaMonitorText.fontSize = 62f;
		}
		else
		{
			if (timeUntilDeadline <= 0f)
			{
				StartOfRound.Instance.deadlineMonitorText.text = "DEADLINE:\n NOW";
			}
			else
			{
				StartOfRound.Instance.deadlineMonitorText.text = $"DEADLINE:\n{daysUntilDeadline} Days";
			}
			StartOfRound.Instance.profitQuotaMonitorText.text = $"PROFIT QUOTA:\n${quotaFulfilled} / ${profitQuota}";
		}
	}

	[ClientRpc]
	public void SetShipToLeaveOnMidnightClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(749416460u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 749416460u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				StartOfRound.Instance.ShipLeaveAutomatically(leavingOnMidnight: true);
			}
		}
	}

	public void VoteShipToLeaveEarly()
	{
		if (!votedShipToLeaveEarlyThisRound)
		{
			votedShipToLeaveEarlyThisRound = true;
			SetShipLeaveEarlyServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLeaveEarlyServerRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendServerRpc(543987598u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 543987598u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			votesForShipToLeaveEarly++;
			int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
			if (votesForShipToLeaveEarly >= num)
			{
				SetShipLeaveEarlyClientRpc(normalizedTimeOfDay + 0.1f, votesForShipToLeaveEarly);
			}
			else
			{
				AddVoteForShipToLeaveEarlyClientRpc();
			}
		}
	}

	[ClientRpc]
	public void AddVoteForShipToLeaveEarlyClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1359513530u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1359513530u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				votesForShipToLeaveEarly++;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votesForShipToLeaveEarly);
			}
		}
	}

	[ClientRpc]
	public void SetShipLeaveEarlyClientRpc(float timeToLeaveEarly, int votes)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(3001101610u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in timeToLeaveEarly, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, votes);
				__endSendClientRpc(ref bufferWriter, 3001101610u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				votesForShipToLeaveEarly = votes;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votes);
				shipLeaveAutomaticallyTime = timeToLeaveEarly;
				shipLeavingAlertCalled = true;
				shipLeavingEarlyDialogue[0].bodyText = "WARNING! Please return by " + HUDManager.Instance.GetClockTimeFormatted(timeToLeaveEarly, numberOfHours, createNewLine: false) + ". A vote has been cast, and the autopilot ship will leave early.";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}
		}
	}

	[ClientRpc]
	public void ShipFullCapacityMidnightClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default;
				FastBufferWriter bufferWriter = __beginSendClientRpc(711575688u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 711575688u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				shipLeavingEarlyDialogue[0].bodyText = "ALERT! The ship has reached full carrying capacity and cannot leave until items are removed!";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
			}
		}
	}

	public DayMode GetDayPhase(float time)
	{
		if (time >= 0.9f)
		{
			return DayMode.Midnight;
		}
		if (time >= 0.63f)
		{
			return DayMode.Sundown;
		}
		if (time >= 0.33f)
		{
			return DayMode.Noon;
		}
		return DayMode.Dawn;
	}

	private void PlayerSeesNewTimeOfDay()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory && !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && playersManager.shipHasLanded)
		{
			dayModeLastTimePlayerWasOutside = dayMode;
			HUDManager.Instance.SetClockIcon(dayMode);
			if (currentLevel.planetHasTime)
			{
				PlayTimeMusicDelayed(timeOfDayCues[(int)dayMode], 0.5f, playRandomDaytimeMusic: true);
			}
		}
	}

	public void PlayTimeMusicDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic = false)
	{
		if (playDelayedMusicCoroutine != null)
		{
			Debug.Log("Already playing music; cancelled starting new music");
		}
		else
		{
			playDelayedMusicCoroutine = StartCoroutine(playSoundDelayed(clip, delay, playRandomDaytimeMusic));
		}
	}

	private IEnumerator playSoundDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic)
	{
		yield return new WaitForSeconds(delay);
		TimeOfDayMusic.PlayOneShot(clip, 1f);
		if (!playRandomDaytimeMusic || !currentLevel.planetHasTime)
		{
			playDelayedMusicCoroutine = null;
			yield break;
		}
		yield return new WaitForSeconds(3f);
		yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 8f));
		if (insideLighting || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom || StartOfRound.Instance.fearLevel > 0.03f)
		{
			playDelayedMusicCoroutine = null;
			yield break;
		}
		if (UnityEngine.Random.Range(0, 100) < 20 || ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
		{
			if (ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
			{
				ES3.Save("TimesLanded", 2, "LCGeneralSaveData");
			}
			SoundManager.Instance.PlayRandomOutsideMusic(dayMode >= DayMode.Sundown);
		}
		playDelayedMusicCoroutine = null;
	}

	private IEnumerator fadeOutEffect(WeatherEffect effect, Vector3 moveFromPosition)
	{
		if (effect.effectObject != null)
		{
			for (int i = 0; i < 270; i++)
			{
				effect.effectObject.transform.position = Vector3.Lerp(effect.effectObject.transform.position, moveFromPosition - Vector3.up * 50f, (float)i / 270f);
				yield return null;
				if (effect.effectObject == null || !effect.transitioning)
				{
					yield break;
				}
			}
		}
		DisableWeatherEffect(effect);
	}

	private void SetWeatherEffects()
	{
		Vector3 vector = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead) ? StartOfRound.Instance.localPlayerController.transform.position : StartOfRound.Instance.spectateCamera.transform.position);
		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i].effectEnabled)
			{
				if (!string.IsNullOrEmpty(effects[i].sunAnimatorBool) && sunAnimator != null)
				{
					sunAnimator.SetBool(effects[i].sunAnimatorBool, value: true);
				}
				effects[i].transitioning = false;
				if (effects[i].effectObject != null)
				{
					effects[i].effectObject.SetActive(value: true);
					if (effects[i].lerpPosition)
					{
						effects[i].effectObject.transform.position = Vector3.Lerp(effects[i].effectObject.transform.position, vector, Time.deltaTime);
					}
					else
					{
						effects[i].effectObject.transform.position = vector;
					}
				}
			}
			else if (!effects[i].transitioning)
			{
				effects[i].transitioning = true;
				if (effects[i].lerpPosition)
				{
					StartCoroutine(fadeOutEffect(effects[i], vector));
				}
				else
				{
					DisableWeatherEffect(effects[i]);
				}
			}
		}
	}

	private void DisableWeatherEffect(WeatherEffect effect)
	{
		if (!(effect.effectObject == null))
		{
			effect.effectObject.SetActive(value: false);
		}
	}

	public void DisableAllWeather(bool deactivateObjects = false)
	{
		for (int i = 0; i < effects.Length; i++)
		{
			effects[i].effectEnabled = false;
		}
		if (!deactivateObjects)
		{
			return;
		}
		for (int j = 0; j < effects.Length; j++)
		{
			if (effects[j].effectObject != null)
			{
				effects[j].effectObject.SetActive(value: false);
			}
		}
	}

	public void RefreshClockUI()
	{
		HUDManager.Instance.SetClockIcon(dayMode);
		HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
	}

	public void OnHourChanged(int amount = 1)
	{
		onHourChanged.Invoke();
	}

	public void OnDayChanged()
	{
		if (!StartOfRound.Instance.isChallengeFile)
		{
			SetBuyingRateForDay();
			StartOfRound.Instance.SetPlanetsWeather();
			StartOfRound.Instance.SetPlanetsMold();
			SetCompanyMood();
		}
	}

	public void SetCompanyMood()
	{
		if (timesFulfilledQuota <= 0)
		{
			currentCompanyMood = CommonCompanyMoods[0];
			return;
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 164);
		currentCompanyMood = CommonCompanyMoods[random.Next(0, CommonCompanyMoods.Length)];
	}

	public void SetBuyingRateForDay()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		if (daysUntilDeadline == 0)
		{
			StartOfRound.Instance.companyBuyingRate = 1f;
			return;
		}
		float num = 0.3f;
		float num2 = (1f - num) / (float)quotaVariables.deadlineDaysAmount;
		StartOfRound.Instance.companyBuyingRate = num2 * (float)(quotaVariables.deadlineDaysAmount - daysUntilDeadline) + num;
	}

	[ClientRpc]
	public void SetBeginMeteorShowerClientRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default;
			FastBufferWriter bufferWriter = __beginSendClientRpc(1181254672u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1181254672u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute && (networkManager.IsClient || networkManager.IsHost))
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (!IsServer)
			{
				MeteorWeather.gameObject.SetActive(value: true);
				MeteorWeather.meteorsEnabled = true;
				HUDManager.Instance.MeteorShowerWarningHUD();
			}
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(2756840554u, __rpc_handler_2756840554, "SplashWaterRpc");
		__registerRpc(3168707752u, __rpc_handler_3168707752, "SyncTimeClientRpc");
		__registerRpc(990741385u, __rpc_handler_990741385, "SendHostInfoForShowingAdClientRpc");
		__registerRpc(3562921089u, __rpc_handler_3562921089, "ReceiveInfoFromClientForShowingAdServerRpc");
		__registerRpc(1041683203u, __rpc_handler_1041683203, "SyncNewProfitQuotaClientRpc");
		__registerRpc(749416460u, __rpc_handler_749416460, "SetShipToLeaveOnMidnightClientRpc");
		__registerRpc(543987598u, __rpc_handler_543987598, "SetShipLeaveEarlyServerRpc");
		__registerRpc(1359513530u, __rpc_handler_1359513530, "AddVoteForShipToLeaveEarlyClientRpc");
		__registerRpc(3001101610u, __rpc_handler_3001101610, "SetShipLeaveEarlyClientRpc");
		__registerRpc(711575688u, __rpc_handler_711575688, "ShipFullCapacityMidnightClientRpc");
		__registerRpc(1181254672u, __rpc_handler_1181254672, "SetBeginMeteorShowerClientRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_2756840554(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SplashWaterRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3168707752(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SyncTimeClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_990741385(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SendHostInfoForShowingAdClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3562921089(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).ReceiveInfoFromClientForShowingAdServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1041683203(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SyncNewProfitQuotaClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_749416460(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SetShipToLeaveOnMidnightClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_543987598(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SetShipLeaveEarlyServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1359513530(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).AddVoteForShipToLeaveEarlyClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3001101610(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SetShipLeaveEarlyClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_711575688(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).ShipFullCapacityMidnightClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1181254672(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((TimeOfDay)target).SetBeginMeteorShowerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "TimeOfDay";
	}
}
