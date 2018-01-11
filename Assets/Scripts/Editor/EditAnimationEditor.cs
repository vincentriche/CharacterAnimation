using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EditAnimationEditor : EditorWindow
{
	private AnimationClip originalClip;
	private bool multiresolutionMode = false;
	private int N = 0;

	// Store all the N curves for each bone of the clip
	private List<AnimationLevel> levels;

	// Store the user defined coefficients for each level N
	private List<float> levelCoefficients;

	private bool multiInitialized = false;


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
				if (EditorGUI.EndChangeCheck() == true && !multiInitialized)
					SetupMultiresolutionEdition();

				if (levelCoefficients != null)
				{
					for (int i = 0; i < levelCoefficients.Count; i++)
						levelCoefficients[i] = EditorGUILayout.FloatField("Level " + i.ToString(), levelCoefficients[i]);

					if (GUILayout.Button("Compute Animation"))
						MultiresolutionFilter();
				}
			}
			else
				multiInitialized = false;
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

		multiInitialized = true;

		levelCoefficients = new List<float>();
		for (int i = 0; i < N; i++)
			levelCoefficients.Add(1.0f);
		
		levels = new List<AnimationLevel>();
		for (int i = 0; i < N; i++)
			levels.Add(new AnimationLevel());

		// Init level 0 : original clip
		int boneCount = 0;
		foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(originalClip))
		{
			AnimationCurve boneCurve = AnimationUtility.GetEditorCurve(originalClip, binding);
			levels[0].bonesInfo.Add(new MyAnimationCurve(boneCurve));
			boneCount++;
		}
		levels[0].keyCount = levels[0].bonesInfo[0].curve.length;
		AnimationLevel.boneCount = boneCount;

		// Init other levels, each time with N / 2 values.
		for (int i = 1; i < N; i++)
		{
			// For each bone, compute the animation curve at lower frequency
			levels[i].keyCount = levels[i - 1].keyCount / 2;
			for (int j = 0; j < AnimationLevel.boneCount; j++)
			{
				// Compute each key based on : (upper level key) and (upper level key + 1)
				int upperLevelIndex = 0;
				for (int k = 0; k < levels[i].keyCount; k++)
				{
					Keyframe upperLevelKey1 = levels[i - 1].bonesInfo[j].curve[upperLevelIndex];
					Keyframe upperLevelKey2 = levels[i - 1].bonesInfo[j].curve[upperLevelIndex + 1];

					Keyframe newKey = new Keyframe();
					newKey.time = (upperLevelKey1.time + upperLevelKey2.time) / 2.0f;
					newKey.value = (upperLevelKey1.value + upperLevelKey2.value) / 2.0f;

					float diff = Mathf.Abs(newKey.value - upperLevelKey1.value);

					levels[i].bonesInfo[j].curve.AddKey(newKey);
					levels[i].bonesInfo[j].diffs.Add(diff);
				}
			}
		}
	}

	private void MultiresolutionFilter()
	{
		// Move each curve according to user defined coef, for each frequency.
		for (int i = 1; i < N; i++)
		{
			float coef = levelCoefficients[i];
			if (coef == 1.0f)
				continue;
			for (int j = 0; j < AnimationLevel.boneCount; j++)
			{
				for (int k = 0; k < levels[i].keyCount; k++)
					levels[i].bonesInfo[j].diffs[k] *= coef;
			}
		}

		for (int i = N - 1; i >= 1; i--)
		{
				
		}

		// Store the new clip in Asset
		AnimationClip clip = new AnimationClip();
		clip.legacy = originalClip.legacy;
		EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(originalClip);
		for (int i = 0; i < bindings.Length; i++)
			AnimationUtility.SetEditorCurve(clip, bindings[i], levels[0].bonesInfo[i].curve);
		AssetDatabase.CreateAsset(clip, "Assets/FilteredAnim.anim");
	}
}

public class AnimationLevel
{
	public static int boneCount;
	public int keyCount;
	public List<MyAnimationCurve> bonesInfo;
}

public class MyAnimationCurve
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
