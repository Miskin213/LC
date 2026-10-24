using UnityEngine;

public class MatchLocalPlayerPosition : MonoBehaviour
{
	private void LateUpdate()
	{
		if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
		{
			transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
		}
	}
}
