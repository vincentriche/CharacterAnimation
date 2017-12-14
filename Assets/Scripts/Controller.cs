using UnityEngine;

// TODO
// Run animation speed to adapt + transition between run and walk
// Running Jump
// Inverse Kinematic
public class Controller : MonoBehaviour
{
	public static Controller Instance;

	[Header("Movements")]
	public float accelerationSpeed = 50000f;
	public float maxWalkingSpeed = 4.0f;
	public float maxRunningSpeed = 8.0f;
	public float maxCrouchingSpeed = 1.0f;
	public float jumpVelocity = 20000f;
	public State state;
	public GameObject spawnCube;

	private Animator animator;
	private float cap;
	private Vector3 groundSpeed;
	private float walkAndRunTransitionSpeed = 2.0f;
	private Rigidbody m_rigidbody;
	private float turn;

	private void Awake()
	{
		Instance = this;
		m_rigidbody = GetComponent<Rigidbody>();
		animator = GetComponent<Animator>();
		cap = maxWalkingSpeed;
	}

	private void Update()
	{
		UpdateState();
		SetAnimatorValues();
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

		// Spawn Cubes
		if (Input.GetKeyDown(KeyCode.L))
		{
			Vector3 p = transform.position + transform.forward * 2.0f + transform.up * 2.0f;
			GameObject o = Instantiate(spawnCube, p, Quaternion.identity);
			o.GetComponent<Rigidbody>().AddForce(transform.forward * 500.0f);
		}

		groundSpeed = m_rigidbody.velocity;
		groundSpeed.y = 0.0f;
	}

	private void Move(Vector3 move)
	{
		// Move
		if (move.x != 0f || move.z != 0f)
		{
			m_rigidbody.AddRelativeForce(move * accelerationSpeed * Time.deltaTime);
		}
		if (Input.GetAxisRaw("Vertical") == 0f && Input.GetAxisRaw("Horizontal") == 0f)
		{
			m_rigidbody.velocity = Vector3.zero;
		}

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
		float mX = Mathf.Clamp(Input.GetAxisRaw("Mouse X"), -1, 1);
		if (mX != 0)
		{
			turn += mX * 10.0f * Time.deltaTime;
			turn = Mathf.Clamp(turn, -1.0f, 1.0f);
		}
		else
			turn = Mathf.Lerp(turn, 0.0f, 10.0f * Time.deltaTime);
		turn += Input.GetAxis("Horizontal");
		float f = Input.GetAxis("Vertical");
		if (state != State.Running)
			f = Mathf.Clamp(f, 0.0f, 0.5f);

		animator.SetFloat("Upspeed", m_rigidbody.velocity.y);
		animator.SetFloat("Forward", f);
		animator.SetFloat("Horizontal", turn);
		animator.SetBool("Grounded", state == State.Grounded);
		animator.SetBool("Jump", Input.GetKeyDown(KeyCode.Space));
		animator.SetBool("Crouch", state == State.Crouched ? true : false);
	}

	void OnCollisionEnter(Collision collision)
	{
		if (state == State.Jumped)
			state = State.Grounded;
	}
}

public enum State
{
	Grounded,
	Running,
	Jumped,
	Crouched
};