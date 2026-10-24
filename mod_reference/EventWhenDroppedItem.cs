using System;
using UnityEngine;

public class EventWhenDroppedItem : GrabbableObject
{
	public float noiseLoudness;

	public float noiseRange;

	[Space(3f)]
	private int timesPlayedInSameSpot;

	private Vector3 lastPositionDropped;

	public float lastPositionDroppedThresholdDistance = 25f;

	public int effectWearOffMultiplier = 1;

	public AudioSource itemAudio;

	private System.Random bellPitchRandom;

	public override void Start()
	{
		base.Start();
		bellPitchRandom = new System.Random((int)(transform.position.x + transform.position.z));
	}

	public override void PlayDropSFX()
	{
		if (itemProperties.dropSFX != null)
		{
			itemAudio.pitch = 1f;
			switch (bellPitchRandom.Next(0, 7))
			{
			case 1:
				itemAudio.pitch *= Mathf.Pow(1.05946f, 3f);
				break;
			case 2:
				itemAudio.pitch *= Mathf.Pow(1.05946f, 5f);
				break;
			case 3:
				itemAudio.pitch /= Mathf.Pow(1.05946f, 3f);
				break;
			case 4:
				itemAudio.pitch /= Mathf.Pow(1.05946f, 5f);
				break;
			case 5:
				itemAudio.pitch /= Mathf.Pow(1.05946f, 7f);
				break;
			case 6:
				itemAudio.pitch /= Mathf.Pow(1.05946f, 10f);
				break;
			}
			itemAudio.PlayOneShot(itemProperties.dropSFX);
			WalkieTalkie.TransmitOneShotAudio(itemAudio, itemProperties.dropSFX);
			RoundManager.Instance.PlayAudibleNoise(transform.position, noiseRange, noiseLoudness, timesPlayedInSameSpot, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
			if (Vector3.Distance(transform.position, lastPositionDropped) < lastPositionDroppedThresholdDistance)
			{
				timesPlayedInSameSpot += effectWearOffMultiplier;
			}
			else
			{
				timesPlayedInSameSpot = 0;
			}
		}
		hasHitGround = true;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "EventWhenDroppedItem";
	}
}
