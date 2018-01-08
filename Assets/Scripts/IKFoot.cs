﻿using UnityEngine;

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

		animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);
		animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
		animator.SetIKPosition(AvatarIKGoal.RightFoot, nextRightFootPos);
		animator.SetIKRotation(AvatarIKGoal.RightFoot, nextRightFootRot);

		animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
		animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
		animator.SetIKPosition(AvatarIKGoal.LeftFoot, nextLeftFootPos);
		animator.SetIKRotation(AvatarIKGoal.LeftFoot, nextLeftFootRot);
	}
}
