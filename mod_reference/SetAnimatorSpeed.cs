using UnityEngine;

public class SetAnimatorSpeed : MonoBehaviour
{
	public float setSpeed = 1f;

	private void Start()
	{
		gameObject.GetComponent<Animator>().SetFloat("animatorSpeed", setSpeed);
		Object.Destroy(this);
	}
}
