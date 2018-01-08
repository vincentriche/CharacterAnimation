using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EditAnimationEditor : EditorWindow
{
	private AnimationClip originalClip;
	private bool multiresolutionMode = false;
	private int N = 0;

	// Store all the N curves for each bone of the clip
	private List<List<MyAnimationCurve>> curves;

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

				if (levelCoefficients != null)
				{
					for (int i = 0; i < levelCoefficients.Count; i++)
						levelCoefficients[i] = EditorGUILayout.FloatField("Level " + i.ToString(), levelCoefficients[i]);

					if (GUILayout.Button("Compute Animation"))
						MultiresolutionFilter();
				}
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
		AssetDatabase.CreateAsset(clip, "Assets/GaussianAnim.anim");
	}

	private void SetupMultiresolutionEdition()
	{
		if (N == 0)
			return;

		levelCoefficients = new List<float>();
		for (int i = 0; i < N; i++)
			levelCoefficients.Add(1.0f);

		curves = new List<List<MyAnimationCurve>>();
		for (int i = 0; i < N; i++)
			curves.Add(new List<MyAnimationCurve>());
		foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(originalClip))
		{
			AnimationCurve boneCurve = AnimationUtility.GetEditorCurve(originalClip, binding);

			// First level : Base curve
			curves[0].Add(new MyAnimationCurve(boneCurve));

			// Calculate each curve for the bone
			AnimationCurve lastCurve = boneCurve;
			for (int i = 1; i < N; i++)
			{
				AnimationCurve currentCurve = new AnimationCurve();
				List<float> diffs = new List<float>();
				for (int j = 0; j < lastCurve.length - 1; j += 2)
				{
					float time = (lastCurve[j].time + lastCurve[j + 1].time) / 2.0f;
					float value = (lastCurve[j].value + lastCurve[j + 1].value) / 2.0f;
					float diff = value - lastCurve[j].value;

					currentCurve.AddKey(time, value);
					diffs.Add(diff);
				}
				curves[i].Add(new MyAnimationCurve(currentCurve, diffs));
				lastCurve = currentCurve;
			}
		}
	}

	private void MultiresolutionFilter()
	{
		// Move each curve according to user defined coef, for each frequency.
		for (int i = 0; i < curves.Count; i++)
		{
			float coef = levelCoefficients[i];
			if (coef == 1.0f)
				continue;
			for (int j = 0; j < curves[i].Count; j++)
			{
				MyAnimationCurve c = curves[i][j];
				for (int k = 0; k < c.diffs.Count; k++)
					c.diffs[i] *= coef;
			}
		}

		// Reconstruct the original clip - with coefficient applied for each frequency
		// And the diffs between levels, stored earlier in SetupMultiresolutionFilter()
		// For each frequency
		for (int i = curves.Count - 1; i >= 1; i--)
		{
			List<MyAnimationCurve> curveCurrentLevel = curves[i];
			List<MyAnimationCurve> curveHigherLevel = curves[i - 1];

			// For each bone in this level
			for (int j = 0; j < curveCurrentLevel.Count; j++)
			{
				// For each key in this bone's curve
				int indexInHigherCurve = 0;
				for (int k = 0; k < curveCurrentLevel[j].curve.length; k++)
				{
					// Calculate the two values in higher curve
					float diff = curveCurrentLevel[j].diffs[k];
					float t1 = curveHigherLevel[j].curve[indexInHigherCurve].time;
					float v1 = curveHigherLevel[j].curve[indexInHigherCurve].value;
					float t2 = curveHigherLevel[j].curve[indexInHigherCurve + 1].time;
					float v2 = curveHigherLevel[j].curve[indexInHigherCurve + 1].value;
					if (v1 > v2)
					{
						v1 = v1 + diff;
						v2 = v2 - diff;
					}
					else
					{
						v2 = v2 + diff;
						v1 = v1 - diff;
					}
					curveHigherLevel[j].curve.MoveKey(indexInHigherCurve, new Keyframe(t1, v1));
					curveHigherLevel[j].curve.MoveKey(indexInHigherCurve + 1, new Keyframe(t2, v2));
					indexInHigherCurve += 2;
				}
			}
		}

		// Store the new clip in Asset/filteredAnim
		AnimationClip clip = new AnimationClip();
		clip.legacy = originalClip.legacy;
		EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(originalClip);
		for (int i = 0; i < bindings.Length; i++)
			AnimationUtility.SetEditorCurve(clip, bindings[i], curves[0][i].curve);
		AssetDatabase.CreateAsset(clip, "Assets/FilteredAnim.anim");
	}
}

[System.Serializable]
public struct MyAnimationCurve
{
	public AnimationCurve curve;
	public List<float> diffs;

	public MyAnimationCurve(AnimationCurve c)
	{
		curve = c;
		diffs = new List<float>();
	}

	public MyAnimationCurve(AnimationCurve c, List<float> d)
	{
		curve = c;
		diffs = d;
	}
}
