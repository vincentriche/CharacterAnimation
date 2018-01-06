using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EditAnimationEditor : EditorWindow
{
	private AnimationClip originalClip;
	private bool multiresolutionMode = false;
	private int N = 0;
	// Store all the N curves for each bone of the clip
	private List<List<AnimationCurve>> curves;
	// Store the user defined coefficients for each level N
	private List<float> levelCoefficients;


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
			// Gaussian
			if (GUILayout.Button("Gaussian Filter"))
				GaussianFilter();
			GUILayout.Space(20);

			// Multiresolution edition
			if (GUILayout.Button("Multiresolution Edition"))
			{
				multiresolutionMode = !multiresolutionMode;
				if (multiresolutionMode)
					SetupMultiresolutionEdition();
			}

			if (multiresolutionMode == true)
			{
				EditorGUI.BeginChangeCheck();
				N = EditorGUILayout.IntField("N", N);
				if (EditorGUI.EndChangeCheck() == true)
					SetupMultiresolutionEdition();

				for (int i = 0; i < levelCoefficients.Count; i++)
					levelCoefficients[i] = EditorGUILayout.FloatField("Level " + i.ToString(), levelCoefficients[i]);

				if (GUILayout.Button("Compute Animation"))
					MultiresolutionFilter();
			}
		}
		EditorGUILayout.EndVertical();
	}


	private void GaussianFilter()
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

	private void SetupMultiresolutionEdition()
	{
		levelCoefficients = new List<float>();
		for (int i = 0; i < N; i++)
			levelCoefficients.Add(1.0f);

		curves = new List<List<AnimationCurve>>();
		foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(originalClip))
		{
			List<AnimationCurve> levelCurves = new List<AnimationCurve>();
			AnimationCurve boneCurve = AnimationUtility.GetEditorCurve(originalClip, binding);

			// First level : Base curve
			levelCurves.Add(boneCurve);

			// Calculate each curve for the bone
			AnimationCurve lastCurve = boneCurve;
			for (int i = 0; i < N; i++)
			{
				AnimationCurve currentCurve = new AnimationCurve();

				for (int j = 0; j < lastCurve.length - 1; j += 2)
				{
					float time = (lastCurve[j].time + lastCurve[j + 1].time) / 2.0f;
					float value = (lastCurve[j].value + lastCurve[j + 1].value) / 2.0f;
					float diff = value - lastCurve[j].value;
					//
					// Todo : store the diff to be able to get back the full clip modified
					// 
					currentCurve.AddKey(time, value);
				}

				levelCurves.Add(currentCurve);
				lastCurve = currentCurve;
			}

			// Add all the bone's curves to the global array
			curves.Add(levelCurves);
		}
	}

	private void MultiresolutionFilter()
	{
		// TODO : 
		// - Apply the level coefficient to each animation curves of each level
		//  -Reconstruct the full animation from all the modified curves thanks to the "diff" value. (to store)
		//  -Store the new clip in Assets/
	}
}
