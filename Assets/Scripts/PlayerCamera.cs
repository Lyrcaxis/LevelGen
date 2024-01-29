using UnityEngine;

public class PlayerCamera : MonoBehaviour {
	[SerializeField] Transform player;

	void LateUpdate() => transform.position = new Vector3(player.position.x, player.position.y, -10);
}