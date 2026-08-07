using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterAnimationBuilder
{
	const string GeneratedRoot = "Assets/Resources/Animations/Active";
	const string ControllerRoot = GeneratedRoot + "/Controllers";
	const string PlayerControllerPath = ControllerRoot + "/PlayerAnimator.controller";

	static readonly string[] Directions = { "s", "sw", "w", "nw", "n", "ne", "e", "se" };
	static readonly Vector2[] DirectionVectors =
	{
		new Vector2(0, -1),
		new Vector2(-1, -1),
		new Vector2(-1, 0),
		new Vector2(-1, 1),
		new Vector2(0, 1),
		new Vector2(1, 1),
		new Vector2(1, 0),
		new Vector2(1, -1),
	};

	[InitializeOnLoadMethod]
	static void BuildWhenNewSpritesAreImported()
	{
		if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath) != null)
			return;

		EditorApplication.delayCall += () => Build(false);
	}

	[MenuItem("Tools/LampLight/Rebuild Character Animations")]
	public static void Rebuild()
	{
		Build(true);
	}

	static void Build(bool replaceGeneratedAssets)
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;

		if (replaceGeneratedAssets && AssetDatabase.IsValidFolder(GeneratedRoot))
			AssetDatabase.DeleteAsset(GeneratedRoot);

		EnsureFolder(GeneratedRoot);
		EnsureFolder(ControllerRoot);

		CharacterSpec player = new CharacterSpec(
			"Player",
			"Assets/Resources/Art/CharacterFrames/Player",
			"player",
			PlayerControllerPath,
			new StateSpec("Idle", "idle", 1, 1, false),
			new StateSpec("Walk", "walk", 2, 6, true)
		);

		ConfigureSpriteImports(player);
		BuildController(player);

		ConnectPrefab("Assets/Resources/Prefabs/Player.prefab", player);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Character animations rebuilt: Player.");
	}

	static void ConfigureSpriteImports(CharacterSpec spec)
	{
		string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spec.SpriteFolder });
		foreach (string guid in spriteGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer == null)
				continue;

			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.spritePixelsPerUnit = 32;
			importer.spritePivot = new Vector2(0.5f, 0.25f);
			importer.filterMode = FilterMode.Point;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.mipmapEnabled = false;
			importer.SaveAndReimport();
		}
	}

	static void BuildController(CharacterSpec spec)
	{
		string clipFolder = GeneratedRoot + "/" + spec.Name;
		EnsureFolder(clipFolder);

		if (AssetDatabase.LoadAssetAtPath<AnimatorController>(spec.ControllerPath) != null)
			AssetDatabase.DeleteAsset(spec.ControllerPath);

		AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(spec.ControllerPath);
		controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
		controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
		controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

		bool hasChase = spec.States.Any(state => state.Name == "Chase");
		if (hasChase)
			controller.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);

		AnimatorStateMachine machine = controller.layers[0].stateMachine;
		Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();

		foreach (StateSpec stateSpec in spec.States)
		{
			BlendTree tree = new BlendTree
			{
				name = stateSpec.Name + " Direction",
				blendType = BlendTreeType.FreeformDirectional2D,
				blendParameter = "MoveX",
				blendParameterY = "MoveY",
				useAutomaticThresholds = false,
			};
			AssetDatabase.AddObjectToAsset(tree, controller);

			for (int i = 0; i < Directions.Length; i++)
			{
				AnimationClip clip = BuildClip(spec, stateSpec, Directions[i], clipFolder);
				tree.AddChild(clip, DirectionVectors[i]);
			}

			AnimatorState state = machine.AddState(stateSpec.Name);
			state.motion = tree;
			states.Add(stateSpec.Name, state);
		}

		machine.defaultState = states["Idle"];
		AddTransition(states["Idle"], states["Walk"], "IsMoving", true);
		AddTransition(states["Walk"], states["Idle"], "IsMoving", false);

		if (hasChase)
		{
			AddTransition(states["Idle"], states["Chase"], "IsChasing", true);
			AddTransition(states["Walk"], states["Chase"], "IsChasing", true);
			AddTransition(states["Chase"], states["Walk"], "IsChasing", false);
		}

		EditorUtility.SetDirty(controller);
	}

	static AnimationClip BuildClip(CharacterSpec spec, StateSpec state, string direction, string folder)
	{
		string clipPath = $"{folder}/{spec.Name}_{state.Name}_{direction.ToUpperInvariant()}.anim";
		AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
		if (existing != null)
			AssetDatabase.DeleteAsset(clipPath);

		List<Sprite> sprites = new List<Sprite>();
		for (int frame = 0; frame < state.FrameCount; frame++)
		{
			string spritePath = $"{spec.SpriteFolder}/{spec.FilePrefix}_{state.FileState}_{direction}_{frame:00}.png";
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
			if (sprite == null)
				throw new InvalidOperationException("Missing animation sprite: " + spritePath);

			sprites.Add(sprite);
		}

		AnimationClip clip = new AnimationClip { name = $"{spec.Name}_{state.Name}_{direction.ToUpperInvariant()}" };
		clip.frameRate = state.Fps;

		List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
		for (int i = 0; i < sprites.Count; i++)
			keys.Add(new ObjectReferenceKeyframe { time = (float)i / state.Fps, value = sprites[i] });

		if (state.Loop && sprites.Count > 1)
			keys.Add(new ObjectReferenceKeyframe { time = (float)sprites.Count / state.Fps, value = sprites[0] });

		EditorCurveBinding binding = new EditorCurveBinding
		{
			path = "",
			type = typeof(SpriteRenderer),
			propertyName = "m_Sprite",
		};
		AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

		SerializedObject serializedClip = new SerializedObject(clip);
		serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = state.Loop;
		serializedClip.ApplyModifiedProperties();

		AssetDatabase.CreateAsset(clip, clipPath);
		return clip;
	}

	static void AddTransition(AnimatorState from, AnimatorState to, string parameter, bool expected)
	{
		AnimatorStateTransition transition = from.AddTransition(to);
		transition.hasExitTime = false;
		transition.hasFixedDuration = true;
		transition.duration = 0;
		transition.AddCondition(
			expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
			0,
			parameter
		);
	}

	static void ConnectPrefab(string prefabPath, CharacterSpec spec)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (prefab == null)
			return;

		Animator animator = prefab.GetComponent<Animator>();
		SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
		animator.runtimeAnimatorController =
			AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.ControllerPath);
		renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
			$"{spec.SpriteFolder}/{spec.FilePrefix}_idle_s_00.png"
		);

		EditorUtility.SetDirty(animator);
		EditorUtility.SetDirty(renderer);
		PrefabUtility.SavePrefabAsset(prefab);
	}

	static void EnsureFolder(string path)
	{
		if (AssetDatabase.IsValidFolder(path))
			return;

		string parent = path.Substring(0, path.LastIndexOf('/'));
		string name = path.Substring(path.LastIndexOf('/') + 1);
		EnsureFolder(parent);
		AssetDatabase.CreateFolder(parent, name);
	}

	sealed class CharacterSpec
	{
		public readonly string Name;
		public readonly string SpriteFolder;
		public readonly string FilePrefix;
		public readonly string ControllerPath;
		public readonly StateSpec[] States;

		public CharacterSpec(
			string name,
			string spriteFolder,
			string filePrefix,
			string controllerPath,
			params StateSpec[] states)
		{
			Name = name;
			SpriteFolder = spriteFolder;
			FilePrefix = filePrefix;
			ControllerPath = controllerPath;
			States = states;
		}
	}

	sealed class StateSpec
	{
		public readonly string Name;
		public readonly string FileState;
		public readonly int FrameCount;
		public readonly int Fps;
		public readonly bool Loop;

		public StateSpec(string name, string fileState, int frameCount, int fps, bool loop)
		{
			Name = name;
			FileState = fileState;
			FrameCount = frameCount;
			Fps = fps;
			Loop = loop;
		}
	}
}
