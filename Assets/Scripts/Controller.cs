using UnityEngine;

public class Controller : MonoBehaviour
{
	public static Controller Instance;

	[Header("Movements")]
	public float accelerationSpeed = 50000f;
	public float maxWalkingSpeed = 4.0f;
	public float maxRunningSpeed = 8.0f;
	public float maxCrouchingSpeed = 1.0f;
	public float jumpVelocity = 20000f;
	public float walkAndRunTransitionSpeed = 2.0f;
	public State state;
	public GameObject spawnCube;

	private Animator animator;
	private float cap;
	private Vector3 groundSpeed;
	private Rigidbody m_rigidbody;
	private bool ragdollEnabled;
	private bool automaticRagdoll;


	private void Awake()
	{
		Instance = this;
		m_rigidbody = GetComponent<Rigidbody>();
		animator = GetComponent<Animator>();
		cap = maxWalkingSpeed;
		ragdollEnabled = false;
		SwitchRagdollMode();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.L))
		{
			ragdollEnabled = false;
			automaticRagdoll = false;
			SwitchRagdollMode();
			transform.position = new Vector3(0f, -1.14f, 0f);
		}
		
		bool ragdollKeyPressed = Input.GetKey(KeyCode.R);
		if (automaticRagdoll == false && ragdollKeyPressed == true && ragdollEnabled == false)
		{
			ragdollEnabled = true;
			SwitchRagdollMode();
		}
		else if (automaticRagdoll == false && ragdollKeyPressed == false && ragdollEnabled == true)
		{
			ragdollEnabled = false;
			SwitchRagdollMode();
		}
		if (Input.GetKeyDown(KeyCode.R) && automaticRagdoll == true)
		{
			automaticRagdoll = false;
			ragdollEnabled = false;
			SwitchRagdollMode();
		}

		if (ragdollEnabled == false)
		{
			UpdateState();
			SetAnimatorValues();
		}
	}

	private void FixedUpdate()
	{
		Vector3 m = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

		// Ground
		if (state != State.Jumped)
		{
			Move(m);
		}

		// Jump
		if (Input.GetKeyDown(KeyCode.Space)
			&& (state == State.Grounded || state == State.Running))
		{
			state = State.Jumped;
			m_rigidbody.AddRelativeForce(0f, jumpVelocity, 0f);
		}

		// Spawn Cube in front of player
		if (Input.GetKeyDown(KeyCode.F))
		{
			Vector3 p = transform.position + transform.forward * 2.0f + transform.up * 2.0f;
			GameObject o = Instantiate(spawnCube, p, Quaternion.identity);
			o.GetComponent<Rigidbody>().AddForce(transform.forward * 500.0f);
		}

		// Spawn Cube to Strike the player
		if (Input.GetKeyDown(KeyCode.E))
		{
			Vector3 p = transform.position + transform.forward * 10.0f + transform.up * 1.5f;
			GameObject o = Instantiate(spawnCube, p, Quaternion.identity);
			o.GetComponent<Rigidbody>().AddForce(-transform.forward * 3000.0f);
		}


		groundSpeed = m_rigidbody.velocity;
		groundSpeed.y = 0.0f;
	}


	private void Move(Vector3 move)
	{
		// Move
		if (move.x != 0f || move.z != 0f)
			m_rigidbody.AddRelativeForce(move * accelerationSpeed * Time.deltaTime);
		if (Input.GetAxisRaw("Vertical") == 0f && Input.GetAxisRaw("Horizontal") == 0f)
			m_rigidbody.velocity = Vector3.zero;

		// Cap Speed
		Vector2 rigidbody_movement = new Vector2(m_rigidbody.velocity.x, m_rigidbody.velocity.z);
		if (state == State.Grounded)
			cap = Mathf.Lerp(cap, maxWalkingSpeed, Time.deltaTime * walkAndRunTransitionSpeed);
		if (state == State.Crouched)
			cap = Mathf.Lerp(cap, maxCrouchingSpeed, Time.deltaTime * walkAndRunTransitionSpeed);
		if (state == State.Running)
			cap = Mathf.Lerp(cap, maxRunningSpeed, Time.deltaTime * walkAndRunTransitionSpeed);
		if (rigidbody_movement.magnitude > cap)
		{
			rigidbody_movement.Normalize();
			rigidbody_movement *= cap;
			m_rigidbody.velocity = new Vector3(rigidbody_movement.x, m_rigidbody.velocity.y, rigidbody_movement.y);
		}
	}

	private void UpdateState()
	{
		if (Input.GetKey(KeyCode.C) && state == State.Grounded)
			state = State.Crouched;
		else if (!Input.GetKey(KeyCode.C) && state == State.Crouched)
			state = State.Grounded;

		if (Input.GetKey(KeyCode.LeftShift) && state == State.Grounded)
			state = State.Running;
		else if (!Input.GetKey(KeyCode.LeftShift) && state == State.Running)
			state = State.Grounded;
	}

	private void SetAnimatorValues()
	{
		// Horizontal Axis
		float h = Input.GetAxis("Horizontal");

		// Vertical Axis
		Vector3 v = m_rigidbody.velocity;
		v.y = 0.0f;
		float f = v.magnitude / maxRunningSpeed;

		// Applying
		animator.SetFloat("Upspeed", m_rigidbody.velocity.y);
		animator.SetFloat("Forward", f);
		animator.SetFloat("Horizontal", h);
		animator.SetBool("Grounded", state == State.Grounded);
		animator.SetBool("Jump", Input.GetKeyDown(KeyCode.Space));
		animator.SetBool("Crouch", state == State.Crouched ? true : false);
	}

	void OnCollisionEnter(Collision collision)
	{
		if (state == State.Jumped)
			state = State.Grounded;
		if (collision.collider.CompareTag("Obstacle"))
		{
			automaticRagdoll = true;
			ragdollEnabled = true;
			SwitchRagdollMode();
		}
	}

	private void SwitchRagdollMode()
	{
		if (ragdollEnabled == true)
		{
			Vector3 velocity = m_rigidbody.velocity;

			var rigColliders = GetComponentsInChildren<Collider>();
			var rigRigidbodies = GetComponentsInChildren<Rigidbody>();
			foreach (Collider col in rigColliders)
				col.enabled = true;
			foreach (Rigidbody rb in rigRigidbodies)
			{
				rb.isKinematic = false;
				rb.velocity = velocity;
			}

			m_rigidbody.isKinematic = true;
			GetComponent<IKFoot>().enabled = false;
			GetComponent<IKLook>().enabled = false;
			GetComponentInChildren<MouseLook>().enabled = false;
			GetComponent<CapsuleCollider>().enabled = false;
			animator.enabled = false;
		}
		else
		{
			var rigColliders = GetComponentsInChildren<Collider>();
			var rigRigidbodies = GetComponentsInChildren<Rigidbody>();
			foreach (Collider col in rigColliders)
				col.enabled = false;
			foreach (Rigidbody rb in rigRigidbodies)
				rb.isKinematic = true;

			GetComponent<IKFoot>().enabled = true;
			GetComponent<IKLook>().enabled = true;
			GetComponentInChildren<MouseLook>().enabled = true;
			GetComponent<CapsuleCollider>().enabled = true;
			animator.enabled = true;
			m_rigidbody.isKinematic = false;
		}
	}
}

public enum State
{
	Grounded,
	Running,
	Jumped,
	Crouched
};
