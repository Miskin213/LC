using System;
using System.Text;
using GameNetcodeStuff;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CinespecManager : NetworkBehaviour
{
	public float MinimumShotLength = 0.5f;

	public float MinimumShotLengthWhenSatisfied = 2f;

	[Tooltip("Lower values will cause it to become more sensitive to differences in player interest level and switch more frantically to catch more interesting moments")]
	public float SwitchPlayerFrequencyMultiplier = 1f;

	[Tooltip("Lower values will make it more eager to change shots if there is a shot that is higher quality than the current")]
	[Range(1f, 10f)]
	public float ChangeShotsEagerness = 1.5f;

	public Camera tempCamera;

	public Transform camPoint;

	public CameraAngle currentAngle;

	public PlayerControllerB spectatedPlayer;

	public Transform spectatedPlayerBody;

	public Vector3 spectatedPlayerPosition;

	public float currentPlayerInterest;

	public Shot currentShot;

	public Shot[] shotTypes;

	private Shot[] shotTypesSyncedOrder;

	public float timeSpentOnPlayer;

	public float timeSpentOnShot;

	public float shotLengthOffset;

	private System.Random spectateRandom;

	public PlayerCinespecInfo[] playersInfo;

	public int currentPlayerInfoId;

	private float updatePlayerInfoSubjectsTimer;

	private int updateSubjectsIndex;

	private float checkPlayerInterestInterval;

	private float checkCompositionInterval;

	public float subjectNotVisibleTime;

	public bool checkingCurrentShot;

	public bool watchingCinespec;

	private bool wasWatchingCinespecLastFrame;

	public bool cinespecActive;

	private bool wasCinespecActiveLastFrame;

	public bool localClientIsCalculating;

	public int clientCalculating = -1;

	private bool isCameraOutside;

	private int cameraMask = 1124075777;

	[Header("Debugging")]
	public bool logPlayerInterestLevel;

	public bool logShotConditions;

	public bool logShotQualityEvaluators;

	public bool logPotentialShotPositions;

	public bool testMode;

	public bool enableLogText;

	public TextMeshProUGUI logText;

	public GameObject logTextContainer;

	private StringBuilder logString = new StringBuilder();

	public int currentShotQualityFail = -1;

	public Image gruggaIndicator;

	public Sprite[] gruggaMoods;

	private Vector3[] checkPlayerVisiblePositions;

	[Header("Misc")]
	public EnemyType maskEnemyType;

	public Vector3 insideMainEntrancePos;

	public Vector3 outsideMainEntrancePos;

	private float interest;

	[HideInInspector]
	public DistanceToPointComparer distanceToPointComparer = new DistanceToPointComparer();

	public static CinespecManager Instance { get; private set; }

	public void EnableTestMode()
	{
		testMode = true;
		GameNetworkManager.Instance.localPlayerController.ChangeAudioListenerToObject(StartOfRound.Instance.spectateCamera.gameObject);
		StartOfRound.Instance.SwitchCamera(StartOfRound.Instance.spectateCamera);
		EnableCinespec(setActive: true);
		SetWatchCinespec(enable: true);
		StartOfRound.Instance.spectateCamera.transform.localPosition = Vector3.zero;
		StartOfRound.Instance.spectateCamera.transform.SetParent(camPoint, worldPositionStays: false);
		GameNetworkManager.Instance.localPlayerController.thisPlayerModelArms.enabled = false;
		GameNetworkManager.Instance.localPlayerController.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
		GameNetworkManager.Instance.localPlayerController.localVisor.position = StartOfRound.Instance.notSpawnedPosition.position;
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void GetClientCalculatingCinespecRpc(int fromClient)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				RequireOwnership = false
			};
			RpcParams rpcParams = default;
			FastBufferWriter bufferWriter = __beginSendRpc(309464285u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, fromClient);
			__endSendRpc(ref bufferWriter, 309464285u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			if (clientCalculating == -1)
			{
				clientCalculating = fromClient;
			}
			localClientIsCalculating = (int)GameNetworkManager.Instance.localPlayerController.playerClientId == clientCalculating;
			Debug.Log($"Cinespec: changed clientCalculating on host to {clientCalculating}");
			SendClientCalculatingRpc(clientCalculating);
		}
	}

	[Rpc(SendTo.NotServer, RequireOwnership = false)]
	public void SendClientCalculatingRpc(int setClientCalculating)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
				{
					RequireOwnership = false
				};
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(3491501310u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, setClientCalculating);
				__endSendRpc(ref bufferWriter, 3491501310u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				clientCalculating = setClientCalculating;
				localClientIsCalculating = (int)GameNetworkManager.Instance.localPlayerController.playerClientId == setClientCalculating;
			}
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void ResetClientCalculatingCinespecRpc(int fromClient)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
				{
					RequireOwnership = false
				};
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(3603250065u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, fromClient);
				__endSendRpc(ref bufferWriter, 3603250065u, rpcParams, attributeParams, SendTo.Server, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				clientCalculating = -1;
				localClientIsCalculating = false;
				ResetClientCalculatingRpc();
			}
		}
	}

	[Rpc(SendTo.NotServer, RequireOwnership = false)]
	public void ResetClientCalculatingRpc()
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Execute)
			{
				RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
				{
					RequireOwnership = false
				};
				RpcParams rpcParams = default;
				FastBufferWriter bufferWriter = __beginSendRpc(1558673845u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
				__endSendRpc(ref bufferWriter, 1558673845u, rpcParams, attributeParams, SendTo.NotServer, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Execute)
			{
				__rpc_exec_stage = __RpcExecStage.Send;
				clientCalculating = -1;
				localClientIsCalculating = false;
			}
		}
	}

	[Rpc(SendTo.NotMe, RequireOwnership = false)]
	public void SyncNewShotRpc(Vector3 shotPos, Quaternion shotRot, int shotTypeIndex, int spectatedPlayerIndex, int chosenAttemptNumber)
	{
		NetworkManager networkManager = NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = new RpcAttribute.RpcAttributeParams
			{
				RequireOwnership = false
			};
			RpcParams rpcParams = default;
			FastBufferWriter bufferWriter = __beginSendRpc(3334207452u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in shotPos);
			bufferWriter.WriteValueSafe(in shotRot);
			BytePacker.WriteValueBitPacked(bufferWriter, shotTypeIndex);
			BytePacker.WriteValueBitPacked(bufferWriter, spectatedPlayerIndex);
			BytePacker.WriteValueBitPacked(bufferWriter, chosenAttemptNumber);
			__endSendRpc(ref bufferWriter, 3334207452u, rpcParams, attributeParams, SendTo.NotMe, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			PlayerControllerB playerControllerB = spectatedPlayer;
			spectatedPlayer = StartOfRound.Instance.allPlayerScripts[spectatedPlayerIndex];
			currentPlayerInfoId = (int)spectatedPlayer.playerClientId;
			if (spectatedPlayer != playerControllerB)
			{
				timeSpentOnPlayer = 0f;
				playersInfo[spectatedPlayerIndex].timeSinceSpectatedPlayer = 0f;
			}
			if (currentShot != null)
			{
				currentShot.OnExitShot(shotTypesSyncedOrder[shotTypeIndex], choseNewAngle: true);
			}
			currentShot = shotTypesSyncedOrder[shotTypeIndex];
			currentShot.currentShotChosenAttempt = chosenAttemptNumber;
			currentAngle.position = shotPos;
			currentAngle.rotation = shotRot;
			currentShot.OnChoseShot(shotPos, shotRot);
			timeSpentOnShot = 0f;
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(Instance.gameObject);
		}
	}

	private void Start()
	{
		shotTypes = UnityEngine.Object.FindObjectsByType<Shot>(FindObjectsSortMode.None);
		shotTypesSyncedOrder = UnityEngine.Object.FindObjectsByType<Shot>(FindObjectsSortMode.None);
		Array.Sort(shotTypesSyncedOrder, (Shot a, Shot b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
		playersInfo = new PlayerCinespecInfo[StartOfRound.Instance.allPlayerScripts.Length];
		for (int num = 0; num < StartOfRound.Instance.allPlayerScripts.Length; num++)
		{
			playersInfo[num] = new PlayerCinespecInfo(num);
		}
		spectateRandom = new System.Random();
		checkPlayerVisiblePositions = new Vector3[4];
	}

	private void Update()
	{
		if (!cinespecActive)
		{
			wasCinespecActiveLastFrame = false;
			return;
		}
		if (camPoint == null)
		{
			camPoint = new GameObject("CameraPoint").transform;
		}
		if (!testMode && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			Debug.LogError("Cinespec was enabled while local player is alive; this is not supposed to occur!");
			EnableCinespec(setActive: false);
		}
		if (!wasCinespecActiveLastFrame)
		{
			GetClientCalculatingCinespecRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
		if (spectatedPlayer != null)
		{
			if (spectatedPlayer.isPlayerDead)
			{
				if (spectatedPlayer.deadBody != null)
				{
					spectatedPlayerPosition = spectatedPlayer.deadBody.bodyParts[5].transform.position;
				}
			}
			else
			{
				spectatedPlayerPosition = spectatedPlayer.bodyParts[5].transform.position;
			}
		}
		if (localClientIsCalculating || clientCalculating == -1)
		{
			RunMiscQualityVariables();
			DecideWhenToChangePlayers();
			if (spectatedPlayer == null)
			{
				Debug.LogError("Cinespec: No player spectated! Cannot change shots.");
				return;
			}
			DecideWhenToChangeShots();
		}
		if (watchingCinespec)
		{
			UpdateCameraOutsideOrInside();
		}
	}

	private void RunMiscQualityVariables()
	{
		if (currentShot == null)
		{
			return;
		}
		if (spectatedPlayer != null)
		{
			Vector3 end = spectatedPlayer.transform.position + Vector3.up * 0.25f;
			Vector3 position = spectatedPlayer.playerEye.transform.position;
			bool flag = !Physics.Linecast(camPoint.position, position, cameraMask, QueryTriggerInteraction.Ignore);
			bool flag2 = !Physics.Linecast(camPoint.position, end, cameraMask, QueryTriggerInteraction.Ignore);
			if (!flag && !flag2)
			{
				Vector3 position2 = spectatedPlayer.bodyParts[5].transform.position;
				checkPlayerVisiblePositions[0] = position2 + spectatedPlayer.transform.forward + Vector3.up * 0.45f;
				checkPlayerVisiblePositions[1] = position2 - spectatedPlayer.transform.forward * 0.4f + Vector3.up * 0.45f;
				checkPlayerVisiblePositions[2] = position2 + spectatedPlayer.transform.right * 0.75f + Vector3.up * 0.45f;
				checkPlayerVisiblePositions[3] = position2 - spectatedPlayer.transform.right * 0.75f + Vector3.up * 0.45f;
				bool flag3 = false;
				for (int i = 0; i < checkPlayerVisiblePositions.Length; i++)
				{
					if (!Physics.Linecast(camPoint.position, checkPlayerVisiblePositions[i], cameraMask, QueryTriggerInteraction.Ignore))
					{
						flag3 = true;
						break;
					}
				}
				if (flag3)
				{
					subjectNotVisibleTime += Time.deltaTime * 1.5f * currentShot.subjectNotVisibleTolerance;
				}
				else
				{
					subjectNotVisibleTime += Time.deltaTime * 5f * currentShot.subjectNotVisibleTolerance;
				}
			}
			else
			{
				subjectNotVisibleTime = Mathf.Max(0f, subjectNotVisibleTime - Time.deltaTime * 10f);
			}
		}
		else if (Physics.Linecast(camPoint.position, spectatedPlayerPosition, cameraMask, QueryTriggerInteraction.Ignore) || Physics.Linecast(spectatedPlayerPosition, camPoint.position, cameraMask, QueryTriggerInteraction.Ignore))
		{
			subjectNotVisibleTime += Time.deltaTime * 3f;
		}
		else
		{
			subjectNotVisibleTime = Mathf.Max(0f, subjectNotVisibleTime - Time.deltaTime * 10f);
		}
	}

	private void UpdateCameraOutsideOrInside()
	{
		bool flag = isCameraOutside;
		isCameraOutside = camPoint.transform.position.y > -100f;
		if (isCameraOutside != flag || !wasWatchingCinespecLastFrame)
		{
			if (!isCameraOutside)
			{
				TimeOfDay.Instance.DisableAllWeather();
			}
			else if (TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.None)
			{
				TimeOfDay.Instance.effects[(int)TimeOfDay.Instance.currentLevelWeather].effectEnabled = true;
			}
		}
	}

	private void DecideWhenToChangePlayers()
	{
		if (spectatedPlayer != null && !spectatedPlayer.isPlayerControlled && !spectatedPlayer.isPlayerDead && spectatedPlayer.deadBody == null)
		{
			spectatedPlayer = null;
			currentPlayerInfoId = -1;
		}
		updatePlayerInfoSubjectsTimer -= Time.deltaTime;
		if (updatePlayerInfoSubjectsTimer < 0f)
		{
			updatePlayerInfoSubjectsTimer = 0.45f;
			updateSubjectsIndex = (updateSubjectsIndex + 1) % playersInfo.Length;
			playersInfo[updateSubjectsIndex].UpdateNearbySubjects();
		}
		LookForPlayersToSwitchTo();
		timeSpentOnPlayer += Time.deltaTime;
		IncrementPlayerInfoValues();
	}

	private void IncrementPlayerInfoValues()
	{
		for (int i = 0; i < playersInfo.Length; i++)
		{
			playersInfo[i].timeSincePlayerDied += Time.deltaTime;
			if (spectatedPlayer != null && spectatedPlayer == playersInfo[i].playerScript)
			{
				playersInfo[i].timeSinceSpectatedPlayer = 0f;
			}
			else
			{
				playersInfo[i].timeSinceSpectatedPlayer += Time.deltaTime;
			}
			playersInfo[i].timeSincePlayerMetAnotherPlayer += Time.deltaTime;
		}
	}

	private void LookForPlayersToSwitchTo()
	{
		if (spectatedPlayer != null)
		{
			if (!(checkPlayerInterestInterval < 0f))
			{
				checkPlayerInterestInterval -= Time.deltaTime;
				return;
			}
			checkPlayerInterestInterval = 0.5f;
			if (timeSpentOnPlayer < MinimumShotLength || (timeSpentOnPlayer < 1.5f && currentPlayerInterest >= 10f))
			{
				return;
			}
		}
		float num = -1000f;
		int num2 = -1;
		for (int i = 0; i < playersInfo.Length; i++)
		{
			playersInfo[i].currentInterestLevel = GetPlayerInterestLevel(playersInfo[i].playerScript);
			if (playersInfo[i].playerScript == spectatedPlayer)
			{
				currentPlayerInterest = playersInfo[i].currentInterestLevel;
			}
			if (playersInfo[i].currentInterestLevel > num)
			{
				num = playersInfo[i].currentInterestLevel;
				num2 = i;
			}
		}
		if (logPlayerInterestLevel)
		{
			HUDManager.Instance.SetDebugText($"Player interest levels:\n{playersInfo[0].currentInterestLevel}\n{playersInfo[1].currentInterestLevel}\n{playersInfo[2].currentInterestLevel}\n{playersInfo[3].currentInterestLevel}");
		}
		if (num2 == -1)
		{
			Debug.LogError("Cinespec: Found no players with interest level above -1000!");
			return;
		}
		float a = ((!(currentShot != null)) ? (-100f) : ((currentShot.MinShotLength + currentShot.MaxShotLength) / 2f));
		bool flag = timeSpentOnShot > MinimumShotLength + Mathf.Min(a, shotLengthOffset - 0.5f);
		bool flag2 = (flag && num - currentPlayerInterest >= 10f * SwitchPlayerFrequencyMultiplier) || (!flag && num - currentPlayerInterest > 16f * SwitchPlayerFrequencyMultiplier);
		if (!((spectatedPlayer == null) | flag2) && !(currentPlayerInterest <= -10f))
		{
			return;
		}
		PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[num2];
		if (playersInfo[num2].playersNearby.Count - playersInfo[num2].deadPlayersNearby > 0 && MultiplePlayersHoldingWalkieTalkies())
		{
			for (int j = 0; j < playersInfo[num2].playersNearby.Count; j++)
			{
				PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playersInfo[num2].playersNearby[j]];
				if (playerControllerB.isPlayerControlled && playerControllerB.holdingWalkieTalkie)
				{
					player = StartOfRound.Instance.allPlayerScripts[playersInfo[num2].playersNearby[j]];
				}
			}
		}
		SwitchSpectateToPlayer(player);
	}

	private bool MultiplePlayersHoldingWalkieTalkies()
	{
		int num = 0;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie)
			{
				num++;
				if (num > 1)
				{
					return true;
				}
			}
		}
		return false;
	}

	private float GetPlayerInterestLevel(PlayerControllerB player)
	{
		interest = 0f;
		int num = (int)player.playerClientId;
		if (!playersInfo[num].playerScript.isPlayerControlled && !playersInfo[num].playerScript.isPlayerDead)
		{
			return -1000f;
		}
		interest += Mathf.Clamp(playersInfo[num].timeSinceSpectatedPlayer / 4f, 0f, 5f);
		playersInfo[num].FindPlayersNearby(15f, includeLOS: true);
		if (playersInfo[num].timeSincePlayerMetAnotherPlayer < 4f)
		{
			interest += 5f;
		}
		PlayerDied(playersInfo[num]);
		EnemiesNearPlayer(playersInfo[num]);
		ItemsNearPlayer(playersInfo[num]);
		PlayersTakingDamage(playersInfo[num]);
		ReactToShipLeaving(playersInfo[num]);
		return interest;
	}

	private void PlayerDied(PlayerCinespecInfo playerInfo)
	{
		if (playerInfo.timeSincePlayerDied < 1.25f)
		{
			interest += 5f;
		}
		else if (playerInfo.playerScript.isPlayerDead)
		{
			if (playerInfo.playersNearby.Count - playerInfo.deadPlayersNearby > 0)
			{
				interest -= 5f;
			}
			else
			{
				interest -= 30f;
			}
		}
	}

	private void EnemiesNearPlayer(PlayerCinespecInfo playerInfo)
	{
		if (playerInfo.nearbySubjects.enemySubjects[0].ai != null)
		{
			float num = 1f;
			for (int i = 0; i < playerInfo.nearbySubjects.enemySubjects.Length && !(playerInfo.nearbySubjects.enemySubjects[i].ai == null); i++)
			{
				num = Mathf.Max(playerInfo.nearbySubjects.enemySubjects[i].ai.enemyType.PowerLevel, 1f);
				if (playerInfo.playerScript.isInHangarShipRoom)
				{
					if (playerInfo.nearbySubjects.enemySubjects[i].ai.enemyType.SizeLimit != NavSizeLimit.MediumSpaces)
					{
						if (playerInfo.nearbySubjects.enemySubjects[i].ai.isInsidePlayerShip)
						{
							num *= 2f;
						}
						else
						{
							num = ((!StartOfRound.Instance.hangarDoorsClosed) ? (num * 0.33f) : (num * 0.25f));
						}
					}
					else
					{
						num *= 0.25f;
					}
				}
				interest += Mathf.Lerp(3f * num, 0.3f * num, playerInfo.nearbySubjects.enemySubjects[i].dist / 30f);
			}
		}
		else
		{
			interest *= 0.75f;
		}
	}

	private void ItemsNearPlayer(PlayerCinespecInfo playerInfo)
	{
		if (!(playerInfo.nearbySubjects.itemSubjects[0] != null))
		{
			return;
		}
		for (int i = 0; i < playerInfo.nearbySubjects.itemSubjects.Length && !(playerInfo.nearbySubjects.itemSubjects[i] == null); i++)
		{
			if (!playerInfo.nearbySubjects.itemSubjects[i].isInElevator || (!playerInfo.nearbySubjects.itemSubjects[i].isInShipRoom && StartOfRound.Instance.shipIsLeaving))
			{
				interest += 1.25f;
			}
		}
	}

	private void PlayersTakingDamage(PlayerCinespecInfo playerInfo)
	{
		bool flag = playerInfo.nearbySubjects.enemySubjects[0].ai != null;
		if (Time.realtimeSinceStartup - playerInfo.playerScript.timeSinceTakingDamage < 4f)
		{
			if (playerInfo.playerScript.criticallyInjured)
			{
				interest += 10f;
			}
			else
			{
				interest += 5f;
			}
			if (flag)
			{
				interest += 15f;
			}
		}
	}

	private void ReactToShipLeaving(PlayerCinespecInfo playerInfo)
	{
		if (!StartOfRound.Instance.shipIsLeaving)
		{
			return;
		}
		if (Time.realtimeSinceStartup - StartOfRound.Instance.timeWhenShipStartedLeaving < 2f)
		{
			if (playerInfo.playerScript.isInElevator)
			{
				interest += 5f;
			}
		}
		else if (!playerInfo.playerScript.isInElevator)
		{
			interest += 5f;
		}
	}

	public void SwitchSpectateToPlayer(PlayerControllerB player)
	{
		if (!(player == spectatedPlayer))
		{
			spectatedPlayer = player;
			currentPlayerInfoId = (int)spectatedPlayer.playerClientId;
			spectatedPlayerBody = player.bodyParts[5];
			if (player.isPlayerDead && player.deadBody != null)
			{
				spectatedPlayerBody = player.deadBody.bodyParts[5].transform;
			}
			GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript = player;
			timeSpentOnPlayer = 0f;
			checkCompositionInterval = 0f;
			GameNetworkManager.Instance.localPlayerController.SetSpectatedPlayerEffects();
		}
	}

	private void DecideWhenToChangeShots()
	{
		if (spectatedPlayer == null)
		{
			Debug.LogError("Cinespec error: Spectated player is null when attempting to change shots!");
		}
		if (!wasCinespecActiveLastFrame)
		{
			if (currentShot != null)
			{
				currentShot.OnExitShot(null, choseNewAngle: true);
			}
			currentShot = null;
			currentShot = ChangeShot(out var changeAngle);
			if (changeAngle)
			{
				currentAngle.position = currentShot.potentialShotPosition.shotPos;
				currentAngle.rotation = Quaternion.Euler(currentShot.potentialShotPosition.shotRot);
				OnShotChanged();
			}
		}
		else if (currentShot != null)
		{
			timeSpentOnShot += Time.deltaTime;
			if (checkCompositionInterval <= 0f)
			{
				checkCompositionInterval = 0.25f;
				Shot shot = ChangeShot(out var changeAngle2);
				currentShot.OnExitShot(shot, changeAngle2);
				bool flag = currentShot != shot;
				currentShot = shot;
				if (changeAngle2 && (flag || !currentShot.alwaysTheSameShot))
				{
					currentAngle.position = currentShot.potentialShotPosition.shotPos;
					currentAngle.rotation = Quaternion.Euler(currentShot.potentialShotPosition.shotRot);
					OnShotChanged();
				}
				if (enableLogText)
				{
					logTextContainer.SetActive(value: true);
					logString.Clear();
					logString.Append("Grugga Cinematography\n");
					logString.AppendLine("Player interest meters:");
					for (int i = 0; i < playersInfo.Length; i++)
					{
						logString.AppendLine($"{playersInfo[i].playerScript.playerUsername}: {playersInfo[i].currentInterestLevel}");
					}
					if (currentShot != null)
					{
						logString.AppendLine();
						logString.AppendLine();
						logString.AppendLine("Current shot: " + currentShot.gameObject.name);
						logString.AppendLine($"Quality: {currentShot.shotQuality}");
						if (currentShot.shotQuality <= 0f)
						{
							logString.Append(" (Failed on: '" + currentShot.ShotQualityEvaluations[currentShotQualityFail].Method.Name + "')");
						}
						float shotQuality = currentShot.shotQuality;
						if (shotQuality < 0.8f)
						{
							if (!(shotQuality <= 0.4f))
							{
								if (shotQuality > 0.4f)
								{
									gruggaIndicator.sprite = gruggaMoods[1];
								}
							}
							else
							{
								gruggaIndicator.sprite = gruggaMoods[0];
							}
						}
						else if (shotQuality < 1.1f)
						{
							gruggaIndicator.sprite = gruggaMoods[2];
						}
						else
						{
							gruggaIndicator.sprite = gruggaMoods[3];
						}
					}
					logText.text = logString.ToString();
				}
				else if (logTextContainer.activeSelf)
				{
					logTextContainer.SetActive(value: false);
				}
			}
			else
			{
				checkCompositionInterval -= Time.deltaTime;
			}
		}
		else
		{
			currentShot = ChangeShot(out var changeAngle3);
			if (changeAngle3 && currentShot != null)
			{
				currentAngle.position = currentShot.potentialShotPosition.shotPos;
				currentAngle.rotation = Quaternion.Euler(currentShot.potentialShotPosition.shotRot);
				OnShotChanged();
			}
		}
	}

	private void OnShotChanged()
	{
		timeSpentOnShot = 0f;
		subjectNotVisibleTime = 0f;
		Debug.Log("Cinespec: Changed shot");
		if (currentShot != null)
		{
			shotLengthOffset = UnityEngine.Random.Range(currentShot.MinShotLength, currentShot.MaxShotLength);
			currentShot.OnChoseShot(currentAngle.position, currentAngle.rotation);
		}
		if (!localClientIsCalculating)
		{
			return;
		}
		int shotTypeIndex = -1;
		for (int i = 0; i < shotTypesSyncedOrder.Length; i++)
		{
			if (shotTypesSyncedOrder[i] == currentShot)
			{
				shotTypeIndex = i;
				break;
			}
		}
		SyncNewShotRpc(currentAngle.position, currentAngle.rotation, shotTypeIndex, (int)spectatedPlayer.playerClientId, currentShot.currentShotChosenAttempt);
	}

	private Shot ChangeShot(out bool changeAngle)
	{
		changeAngle = false;
		float num = 0f;
		float num2 = 0f;
		if (currentShot != null)
		{
			checkingCurrentShot = true;
			currentShot.testAngle.shotPos = currentAngle.position;
			currentShot.testAngle.shotRot = currentAngle.rotation.eulerAngles;
			currentShot.UpdateCurrentInfo();
			currentShot.UpdateCalculationVariables(camPoint.position, camPoint.eulerAngles);
			if (logShotQualityEvaluators)
			{
				Debug.Log("CHECKING CURRENT SHOT! -----------");
			}
			num = currentShot.GetShotQuality(camPoint.position, camPoint.eulerAngles);
			num2 = currentShot.GetShotPotential();
			currentShot.shotQuality = num;
			checkingCurrentShot = false;
		}
		else
		{
			num = 0f;
		}
		if (currentShot == null || ((num <= 0f || num2 <= 0f) && timeSpentOnShot > MinimumShotLength) || (num <= 0.4f && timeSpentOnShot > MinimumShotLength + shotLengthOffset) || timeSpentOnShot > MinimumShotLengthWhenSatisfied + shotLengthOffset)
		{
			int[] array = new int[shotTypes.Length];
			for (int i = 0; i < shotTypes.Length; i++)
			{
				if (shotTypes[i].shotDisabled)
				{
					array[i] = -1;
					continue;
				}
				if (shotTypes[i] == currentShot)
				{
					array[i] = (int)(num2 * 100f * shotTypes[i].ShotRarityMultiplier);
				}
				else
				{
					array[i] = (int)(shotTypes[i].GetShotPotential() * 100f * shotTypes[i].ShotRarityMultiplier);
				}
				Debug.Log($"Shot type #{i} '{shotTypes[i].gameObject.name} potential: {array[i]}");
			}
			int randomWeightedIndex = RoundManager.Instance.GetRandomWeightedIndex(array, spectateRandom);
			if (randomWeightedIndex == -1)
			{
				Debug.LogError("Cinespec: Could not get a shot type! Returned -1");
				return currentShot;
			}
			Shot shot = shotTypes[randomWeightedIndex];
			float num3 = shot.TestShotsForHighestQuality(num, out var chosenAttemptNumber);
			bool flag = ShotQualityIsSatisfactory(num, num2, num3);
			if (!flag && num >= 0.8f)
			{
				Debug.Log($"Chose Angle A0; current quality: {num} ; new: {num3}");
				changeAngle = false;
				return currentShot;
			}
			if (flag && (num <= 0.4f || num2 <= 0f))
			{
				Debug.Log($"Chose Angle A 2; current quality: {num}; new: {num3} pos: {shot.potentialShotPosition.shotPos}");
				shot.shotQuality = num3;
				shot.currentShotChosenAttempt = chosenAttemptNumber;
				changeAngle = true;
				return shot;
			}
			float num4 = 0f;
			Shot shot2 = ((!flag) ? null : shot);
			Shot shot3 = shot;
			Array.Sort(shotTypes, (Shot a, Shot b) => b.shotPotential.CompareTo(a.shotPotential));
			for (int num5 = 0; num5 < shotTypes.Length; num5++)
			{
				if (!(shotTypes[num5] == shot3) && !shotTypes[num5].shotDisabled && !(shotTypes[num5].shotPotential <= 0f))
				{
					shot = shotTypes[num5];
					num3 = shot.TestShotsForHighestQuality(num, out chosenAttemptNumber);
					Debug.Log($"Got quality for shot type #{num5} : {num3} (i:{num5})");
					if (num <= 0.4f && ShotQualityIsSatisfactory(num, num2, num3))
					{
						Debug.Log($"Chose Angle B; current quality: {num}; new: {num3}");
						shot.shotQuality = num3;
						shot.currentShotChosenAttempt = chosenAttemptNumber;
						changeAngle = true;
						return shot;
					}
					Debug.Log($"Is quality ({num3}) higher than highest quality ({num4})? : {num3 > num4}");
					if (num3 > num4)
					{
						num4 = num3;
						shot2 = shotTypes[num5];
						Debug.Log($"highestQualityOfAllShotTypes now: {num4} (i:{num5})");
					}
				}
			}
			Debug.Log($"Finished testing all shots loop. highestQualityOfAllShotTypes: {num4}");
			if (shot2 != null && ShotQualityIsSatisfactory(num, num2, num4))
			{
				Debug.Log($"Chose Angle C; current quality: {num} ; new: {num4}");
				shot2.shotQuality = num4;
				shot.currentShotChosenAttempt = chosenAttemptNumber;
				changeAngle = true;
				return shot2;
			}
			Debug.Log($"Did not choose different angle; current quality: {num}; highest quality: {num4}");
			changeAngle = false;
			return currentShot;
		}
		changeAngle = false;
		return currentShot;
	}

	private bool ShotQualityIsSatisfactory(float currentShotQuality, float currentShotPotential, float newQuality)
	{
		if (newQuality > 0f)
		{
			if (!(currentShotQuality <= 0f) && !(currentShotPotential <= 0f))
			{
				return newQuality >= currentShotQuality * ChangeShotsEagerness;
			}
			return true;
		}
		return false;
	}

	private void LateUpdate()
	{
		if (cinespecActive)
		{
			if (camPoint == null)
			{
				camPoint = new GameObject("CameraPoint").transform;
			}
			if (!wasCinespecActiveLastFrame)
			{
				wasCinespecActiveLastFrame = true;
			}
			wasWatchingCinespecLastFrame = watchingCinespec;
			if (currentShot != null)
			{
				Instance.tempCamera.fieldOfView = currentShot.shotFOV;
				StartOfRound.Instance.spectateCamera.fieldOfView = currentShot.shotFOV;
				currentShot.UpdateCurrentInfo();
				currentShot.MoveCameraForShot();
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (logPotentialShotPositions && cinespecActive)
		{
			VisionConeGizmo.DrawLOS(camPoint, currentShot.shotFOV, 30f, Color.cyan);
		}
	}

	public void ResetCinespec()
	{
		cinespecActive = false;
		watchingCinespec = false;
		localClientIsCalculating = false;
		clientCalculating = -1;
		for (int i = 0; i < playersInfo.Length; i++)
		{
			playersInfo[i].ResetInfo();
		}
		StartOfRound.Instance.spectateCamera.fieldOfView = 66f;
		currentShot = null;
	}

	public void SetWatchCinespec(bool enable)
	{
		if (!cinespecActive)
		{
			watchingCinespec = false;
		}
		else
		{
			watchingCinespec = enable;
		}
	}

	public void EnableCinespec(bool setActive)
	{
		bool flag = cinespecActive;
		cinespecActive = setActive;
		if (!flag && GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			watchingCinespec = true;
		}
		insideMainEntrancePos = RoundManager.FindMainEntrancePosition(getTeleportPosition: true);
		outsideMainEntrancePos = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, getOutsideEntrance: true);
		if (!setActive)
		{
			StartOfRound.Instance.spectateCamera.fieldOfView = 66f;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		__registerRpc(309464285u, __rpc_handler_309464285, "GetClientCalculatingCinespecRpc");
		__registerRpc(3491501310u, __rpc_handler_3491501310, "SendClientCalculatingRpc");
		__registerRpc(3603250065u, __rpc_handler_3603250065, "ResetClientCalculatingCinespecRpc");
		__registerRpc(1558673845u, __rpc_handler_1558673845, "ResetClientCalculatingRpc");
		__registerRpc(3334207452u, __rpc_handler_3334207452, "SyncNewShotRpc");
		base.__initializeRpcs();
	}

	private static void __rpc_handler_309464285(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CinespecManager)target).GetClientCalculatingCinespecRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3491501310(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CinespecManager)target).SendClientCalculatingRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3603250065(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CinespecManager)target).ResetClientCalculatingCinespecRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_1558673845(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CinespecManager)target).ResetClientCalculatingRpc();
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	private static void __rpc_handler_3334207452(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out Quaternion value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			ByteUnpacker.ReadValueBitPacked(reader, out int value4);
			ByteUnpacker.ReadValueBitPacked(reader, out int value5);
			target.__rpc_exec_stage = __RpcExecStage.Execute;
			((CinespecManager)target).SyncNewShotRpc(value, value2, value3, value4, value5);
			target.__rpc_exec_stage = __RpcExecStage.Send;
		}
	}

	protected internal override string __getTypeName()
	{
		return "CinespecManager";
	}
}
