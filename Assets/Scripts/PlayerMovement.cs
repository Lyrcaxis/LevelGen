using UnityEngine;

public class PlayerMovement : MonoBehaviour {
	[SerializeField] float speed = 1;
	[SerializeField] Animator anim;
	[SerializeField] SpriteRenderer spr;
	[SerializeField] Rigidbody2D rb;

	Vector2 moveDir;

	void Update() {
		var rawDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		if (rawDir != Vector2.zero) {
			anim.SetFloat("dirX", Mathf.Abs(rawDir.x));
			anim.SetFloat("dirY", rawDir.y);
			spr.flipX = rawDir.x < 0;
		}

		moveDir = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
		anim.SetFloat("speed", speed * moveDir.magnitude);
	}
	void FixedUpdate() => rb.MovePosition(rb.position + moveDir * (speed * Time.fixedDeltaTime));
}
