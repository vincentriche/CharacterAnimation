using UnityEngine;
using UnityEditor;

public class EditAnimationEditor : EditorWindow
{
	private AnimationClip originalClip;

	[MenuItem("Window/Edit Animation", false, 2000)]
	public static void ShowWindow()
	{
		EditAnimationEditor window = GetWindow<EditAnimationEditor>();
		window.Show();
	}

	public void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		originalClip = EditorGUILayout.ObjectField("Current Animation Clip", originalClip, typeof(AnimationClip), false) as AnimationClip;

		if (originalClip != null)
		{
			if (GUILayout.Button("Gaussian Filter"))
				GaussianFilter();
		}
		EditorGUILayout.EndVertical();
	}

	public void GaussianFilter()
	{
		AnimationClip clip = new AnimationClip();
		clip.legacy = originalClip.legacy;
		float kernel = 0.33f;
		foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(originalClip))
		{
			AnimationCurve curve = AnimationUtility.GetEditorCurve(originalClip, binding);
			for (int i = 1; i < curve.length - 1; i++)
			{
				float time = curve.keys[i].time;

				float beforeValue = curve.keys[i - 1].value;
				float value = curve.keys[i].value;
				float afterValue = curve.keys[i].value;

				float newValue = (beforeValue * kernel) + (value * kernel) + (afterValue * kernel);
				curve.MoveKey(i, new Keyframe(time, newValue));
			}
			AnimationUtility.SetEditorCurve(clip, binding, curve);
		}
		AssetDatabase.CreateAsset(clip, "Assets/Gaussian.anim");
	}
}
