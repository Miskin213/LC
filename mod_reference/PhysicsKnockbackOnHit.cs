using GameNetcodeStuff;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsKnockbackOnHit : MonoBehaviour, IHittable
{
	public AudioClip playSFX;

	public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		if (gameObject.GetComponent<Rigidbody>() == null)
		{
			return false;
		}
		gameObject.GetComponent<Rigidbody>().AddForce(hitDirection * force * 10f, ForceMode.Impulse);
		if (playSFX != null && (bool)gameObject.GetComponent<AudioSource>())
		{
			gameObject.GetComponent<AudioSource>().PlayOneShot(playSFX);
		}
		return true;
	}
}
