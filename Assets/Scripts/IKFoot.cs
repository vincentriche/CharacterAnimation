using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKFoot : MonoBehaviour
{
	private Animator animator;
	private Transform leftFoot, rightFoot;

	private Vector3 nextLeftFootPos, nextRightFootPos;
	private Quaternion nextLeftFootRot, nextRightFootRot;
	private RaycastHit hitInfo;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
		rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

		nextLeftFootRot = leftFoot.rotation;
		nextRightFootRot = rightFoot.rotation;
	}

	private void Update()
	{
		Vector3 leftPos = leftFoot.TransformPoint(Vector3.zero);
		Vector3 rightPos = rightFoot.TransformPoint(Vector3.zero);

		// Left
		if (Physics.Raycast(leftPos, Vector3.down, out hitInfo) == true)
		{
			nextLeftFootPos = hitInfo.point;
			nextLeftFootRot = Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation;
		}

		// Right
		if (Physics.Raycast(rightPos, Vector3.down, out hitInfo) == true)
		{
			nextRightFootPos = hitInfo.point;
			nextRightFootRot = Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation;
		}
	}

	private void OnAnimatorIK()
	{
		float leftFootWeight = animator.GetFloat("LeftFoot");
		float rightFootWeight = animator.GetFloat("RightFoot");

		// Only change position & rotation if significant difference between frames
		float dPosLeft = Vector3.Distance(leftFoot.position, nextLeftFootPos);
		float dPosRight = Vector3.Distance(rightFoot.position, nextRightFootPos);
		float dRotLeft = Quaternion.Angle(leftFoot.rotation, nextLeftFootRot);
		float dRotRight = Quaternion.Angle(rightFoot.rotation, nextRightFootRot);

		// Left foot
		if (dPosLeft > 0.5f)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
			animator.SetIKPosition(AvatarIKGoal.LeftFoot, nextLeftFootPos);
		}
		animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
		animator.SetIKRotation(AvatarIKGoal.LeftFoot, nextLeftFootRot);

		// Right foot
		if (dPosRight > 0.5f)
		{
			animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
			animator.SetIKPosition(AvatarIKGoal.RightFoot, nextRightFootPos);
		}
		animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
		animator.SetIKRotation(AvatarIKGoal.RightFoot, nextRightFootRot);
	}
}
