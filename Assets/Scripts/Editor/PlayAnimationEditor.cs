using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class PlayAnimationEditor : EditorWindow
{
	// Current GameObject Selected on the Scene
	[SerializeField]
	protected GameObject skeleton;

	// List of all body joints of the current Skeleton
	// Transform -> Unity Type (Position, Rotation, Scale)
	// Matrix Transform
	[SerializeField]
	public List<Transform> bodyJoints;

	// Current AnimationClip Selected by the user
	// This AnimationClip will be played 
	[SerializeField]
	protected AnimationClip clipToPlay;

	// Stocks the current Time
	protected float currentFrameTime = 0.0f;

	// Vector2 used to get the save the position of the scroll in the Window
	Vector2 scrollPosition = Vector2.zero;

	// First frame in seconds (here it will be 0.0f)
	private float firstFrameTime = 0.0f;
	// Length of the AnimationClip in seconds 
	private float clipLength;
	// accelerate or slow the animation
	protected float scaleTime = 1f;


	// Dictionnary of string & List<Vector3> which contains all the position of one body Joint
	private Dictionary<string, List<Vector3>> trajectories;

	// Dictionnary of string & bool which contains all the bool used by the toggle button
	protected Dictionary<string, bool> toogleTrajectories;



	// The new item in the menu
	[MenuItem("Window/PlayAnimation", false, 2000)]
	public static void DoWindow()
	{
		PlayAnimationEditor m_window = GetWindow<PlayAnimationEditor>();
		m_window.Show();
	}

	// Called when the user change the selected object in the scene windows
	public void OnSelectionChange()
	{
		// Pick the current selected gameObject in the Scene.
		skeleton = Selection.activeGameObject;
		// The user can pick in the void
		if (skeleton != null)
		{
			// Check if the current gameObject has an animator component or an animation            
			if (skeleton.GetComponent<Animation>() || skeleton.GetComponent<Animator>())
			{
				if (bodyJoints == null)
					bodyJoints = new List<Transform>();

				// Get all the bodyJoints -> This is specific to the skeleton that i used
				// One body joint is defined when 
				bodyJoints = skeleton.GetComponentsInChildren<Transform>().Where(x => x.childCount != 0).ToList();
				//Repaint the currentWindow -> Call the OnGui function
				Repaint();
			}
		}
	}



	// This function enables the editor to handle an event in the scene view.
	// We need to redraw the curve & points when the user is interacting with the SceneView
	// In this function, you will have to code the drawing of the trajectories
	public void OnSceneGUI(SceneView sceneView)
	{
		if (bodyJoints == null || clipToPlay == null)
			return;

		for (int i = 0; i < bodyJoints.Count; i++)
		{
			if (toogleTrajectories[bodyJoints[i].name] == true)
			{
				for (int j = 1; j < trajectories[bodyJoints[i].name].Count; j++)
				{
					Vector3 oldPoint = trajectories[bodyJoints[i].name][j - 1];
					Vector3 currentPoint = trajectories[bodyJoints[i].name][j];
					Handles.color = Color.magenta;
					Handles.DrawLine(oldPoint, currentPoint);
				}
			}
		}
	}


	// Init the trajectories for each body joints for the current Animation Clip
	private void InitTrajectories()
	{
		// Enable the Animation Mode if disabled
		if (!AnimationMode.InAnimationMode())
			AnimationMode.StartAnimationMode();

		// Init Dictionnaries
		if (trajectories == null)
			trajectories = new Dictionary<string, List<Vector3>>();
		if (toogleTrajectories == null)
			toogleTrajectories = new Dictionary<string, bool>();
		trajectories.Clear();
		toogleTrajectories.Clear();
		for (int i = 0; i < bodyJoints.Count; i++)
		{
			trajectories.Add(bodyJoints[i].name, new List<Vector3>());
			toogleTrajectories.Add(bodyJoints[i].name, true);
		}

		// Sample Animation & Add new bones positions
		for (float sampleTime = firstFrameTime; sampleTime < clipLength; sampleTime += 0.01f)
		{
			AnimationMode.BeginSampling();
			AnimationMode.SampleAnimationClip(skeleton, clipToPlay, sampleTime);
			for (int i = 0; i < trajectories.Count; i++)
				trajectories[bodyJoints[i].name].Add(bodyJoints[i].position);
			AnimationMode.EndSampling();
		}

		// Reset gameobject to base position
		AnimationMode.BeginSampling();
		AnimationMode.SampleAnimationClip(skeleton, clipToPlay, 0.0f);
		AnimationMode.EndSampling();
	}


	// OnGUI is called for rendering and handling GUI events.
	// Use OnGUI to draw all the controls of your window.
	public void OnGUI()
	{
		// We need to select a GameObject in the Scene
		if (skeleton == null)
		{
			EditorGUILayout.HelpBox("Please select a GameObject.", MessageType.Info);
			return;
		}
		// Check if the current GameObject is active
		if (!skeleton.activeSelf)
		{
			EditorGUILayout.HelpBox("Please select a GameObject that is active.", MessageType.Info);
			return;
		}
		// Check if the current GameObject has an Animator or Animation Component
		if (skeleton.GetComponent<Animator>() == null && skeleton.GetComponent<Animation>() == null)
		{
			EditorGUILayout.HelpBox("Please select a GameObject with an Animator Component or Animation.", MessageType.Info);
			return;
		}

		// Update the scroll Position
		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

		// Begin a vertical group that will contains all the gui element we will declare between the BeginVertical and the EndVertical
		EditorGUILayout.BeginVertical();
		AnimationClip old = clipToPlay;
		clipToPlay = EditorGUILayout.ObjectField("Current Animation Clip", clipToPlay, typeof(AnimationClip), false) as AnimationClip;
		if (old != clipToPlay)
			InitTrajectories();

		// If the user has selected an AnimationClip
		if (clipToPlay != null)
		{
			// Get the Length of the current Animation
			clipLength = clipToPlay.length;
			// An example for a Slider with change detect
			EditorGUI.BeginChangeCheck();
			// Then we create the Object that we want to track some change on 
			currentFrameTime = EditorGUILayout.Slider("Time (seconds)", currentFrameTime, firstFrameTime, clipLength);
			// If the user has modified the Slider Precision here, we can detect it and call a fonction for example
			if (EditorGUI.EndChangeCheck())
				SamplePosture(currentFrameTime);

			EditorGUI.BeginChangeCheck();
			// Then we create the Object that we want to track some change on 
			scaleTime = EditorGUILayout.Slider("Scale Time", scaleTime, 0.0f, 2.0f);

			// Toogles for each bone
			for (int i = 0; i < toogleTrajectories.Count; i++)
			{
				bool v = toogleTrajectories[bodyJoints[i].name];
				toogleTrajectories[bodyJoints[i].name] = EditorGUILayout.Toggle(bodyJoints[i].name, v);
			}
		}

		// End the vertical group
		EditorGUILayout.EndVertical();

		// Stop the Scroll
		GUILayout.EndScrollView();
	}


	// Call at each frame
	// In this function, we will play the Animation
	private void Update()
	{
		// TODO
		// Verifier que m_skeleton m_animationClip, m_b_isRunning sont init
		// modifier le temps : m_f_time
		// appeler samplePosture qui est ue fonction un peu plus bas



		SceneView.RepaintAll();
	}


	// Sample our Skeleton at the time given in parameter for the currentAnimationClip
	private void SamplePosture(float p_f_time)
	{
		// Check if the Game isn't running & the Animation Mode is enabled
		if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
		{
			// We need to BeginSampling before the SampleAnimationClip is called
			AnimationMode.BeginSampling();
			// Samples the animationClip (m_animation) at the time (m_f_time) for the skeleton (m_skeleton)
			// If the GameObject & the AnimationClip are different -> no errors are trigger but nothing happen 
			AnimationMode.SampleAnimationClip(skeleton, clipToPlay, p_f_time);
			// Ending the Sampling of the Animation
			AnimationMode.EndSampling();
			// Repaint The SceneView as the skeleton has changed
			SceneView.RepaintAll();
			// Repaint the GUI as we are changing the variable m_f_time on which we have a slider
			Repaint();
		}
	}

	void OnFocus()
	{
		// Remove delegate listener if it has previously
		// been assigned.
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		// Add (or re-add) the delegate.
		SceneView.onSceneGUIDelegate += this.OnSceneGUI;
	}

	void OnDestroy()
	{
		// When the window is destroyed, remove the delegate
		// so that it will no longer do any drawing.
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}
}