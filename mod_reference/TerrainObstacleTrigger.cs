using UnityEngine;

public class TerrainObstacleTrigger : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		VehicleController component = other.GetComponent<VehicleController>();
		if (!(component == null) && component.IsOwner && component.averageVelocity.magnitude > 5f && Vector3.Angle(component.averageVelocity, transform.position - component.mainRigidbody.position) < 80f)
		{
			RoundManager.Instance.DestroyTreeOnLocalClient(transform.position);
			component.CarReactToObstacle(component.mainRigidbody.position - transform.position, transform.position, Vector3.zero, CarObstacleType.Object, 1f, null, dealDamage: false);
		}
	}
}
