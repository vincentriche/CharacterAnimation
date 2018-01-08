using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKLook : MonoBehaviour
{
	[SerializeField]
	private Transform objectToLookAt;
	private Animator animator;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	void OnAnimatorIK()
	{
		if (objectToLookAt != null)
		{
			animator.SetLookAtWeight(1);
			animator.SetLookAtPosition(objectToLookAt.position);
		}
	}
}
