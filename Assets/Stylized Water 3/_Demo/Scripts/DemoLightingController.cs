// Stylized Water 3 by Staggart Creations (http://staggart.xyz)
// Modified Version: Added Singleton pattern and smooth weather transition support.
#if (ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_INSTALLED)
#define USE_INPUT_SYSTEM
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StylizedWater3.Demo
{
    [ExecuteAlways]
    public class DemoLightingController : MonoBehaviour
    {
        public static DemoLightingController Instance { get; private set; }

        [Serializable]
        public class Preset
        {
            public string name;
            public Material skybox;

            [Header("Direct light")]
            [Range(0f, 90f)]
            public float sunAngle = 45;
            [Range(0f, 360f)]
            public float sunRotation = 0f;

            public float intensity = 1f;
            public Color tint = Color.white;

            [Header("Indirect light")]
            public Color ambientColor = Color.gray;
            
            [Header("Fog")]
            public Color fogColor = Color.white;
            [Range(0.0001f, 0.01f)]
            public float fogDensity = 0.002f;
        }

        public static bool ShowGUI = true;

        [Min(0)]
        public int activeIndex = 0;
        public Preset[] presets = Array.Empty<Preset>();

        public ReflectionProbe reflectionProbe;
        
        [Header("Transition Settings")]
        [Tooltip("Smooth transition duration in seconds when changing weather presets.")]
        public float transitionDuration = 3f;

        [NonSerialized]
        private Material m_skybox;
        private Light sun;
        
        [SerializeField]
        private bool realtimeReflectionProbesDisabled;
        
        // --- Transition State Variables ---
        private bool isTransitioning = false;
        private float transitionProgress = 0f;
        private Preset startTransitionState = new Preset();
        // ----------------------------------

        #if USE_INPUT_SYSTEM
        private InputAction[] numberKeyActions;
        #endif  
        
        private void Awake()
        {
            // Set up singleton for easy access by trigger scripts
            if (Application.isPlaying)
            {
                if (Instance == null) Instance = this;
                else Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            realtimeReflectionProbesDisabled = QualitySettings.realtimeReflectionProbes;

            ApplyPreset(activeIndex, true); // Apply instantly on startup
            
            #if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
            #endif

            SetupInput();
        }
        
        private void SetupInput()
        {
            #if USE_INPUT_SYSTEM
            numberKeyActions = new InputAction[9];

            for (int i = 0; i < 9; i++)
            {
                int presetIndex = i;
                numberKeyActions[i] = new InputAction($"Preset{presetIndex + 1}", binding: $"<Keyboard>/{presetIndex + 1}");
                numberKeyActions[i].performed += ctx => OnNumberKeyPressed(presetIndex);
                numberKeyActions[i].Enable();
            }
            #endif
        }

        private void OnDisable()
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            
            #if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
            #endif
            
            if (realtimeReflectionProbesDisabled == false && QualitySettings.realtimeReflectionProbes == true) QualitySettings.realtimeReflectionProbes = false;
            
            #if USE_INPUT_SYSTEM
            if (numberKeyActions != null)
            {
                foreach (var action in numberKeyActions)
                {
                    action.Disable();
                    action.Dispose();
                }
            }
            #endif
        }
        
        private readonly int SkyboxTexID = Shader.PropertyToID("_Tex");

        // Called by external triggers to switch weather by name
        public void TransitionToPresetByName(string presetName, float overrideDuration = -1f)
        {
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].name == presetName)
                {
                    if (overrideDuration > 0) transitionDuration = overrideDuration;
                    ApplyPreset(i, false);
                    return;
                }
            }
            Debug.LogWarning($"Could not find a weather preset named: {presetName}");
        }

        public void ApplyPreset(int index = -1, bool instant = false)
        {
            if (index < 0) index = activeIndex;
            
            if (this.gameObject.activeInHierarchy == false) return;
            if (index >= presets.Length) return;

            Preset targetPreset = presets[index];

            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional) sun = lights[i];
            }

            if (sun == null) return;

            // Skybox material replacement (Texture changes instantly, but lighting/fog smooths out)
            if (m_skybox == null || targetPreset.skybox.GetTexture(SkyboxTexID) != RenderSettings.skybox.GetTexture(SkyboxTexID))
            {
                CreateSkyboxMat(targetPreset.skybox);
            }
            m_skybox.CopyPropertiesFromMaterial(targetPreset.skybox);
            m_skybox.SetTexture(SkyboxTexID, targetPreset.skybox.GetTexture(SkyboxTexID));
            RenderSettings.skybox = m_skybox;

            // Apply immediately if in editor mode or instant transition is requested
            if (instant || !Application.isPlaying)
            {
                sun.intensity = targetPreset.intensity;
                sun.color = targetPreset.tint;
                sun.transform.eulerAngles = new Vector3(targetPreset.sunAngle, targetPreset.sunRotation, 0f);
                m_skybox.SetFloat("_Rotation", -sun.transform.eulerAngles.y);
                
                RenderSettings.fogColor = targetPreset.fogColor;
                RenderSettings.fogDensity = targetPreset.fogDensity;
                RenderSettings.ambientLight = targetPreset.ambientColor;

                if (reflectionProbe) reflectionProbe.RenderProbe();
                
                activeIndex = index;
                isTransitioning = false;
                return;
            }

            // --- Start Smooth Transition ---
            if (Application.isPlaying && index != activeIndex)
            {
                startTransitionState.sunAngle = sun.transform.eulerAngles.x;
                startTransitionState.sunRotation = sun.transform.eulerAngles.y;
                startTransitionState.intensity = sun.intensity;
                startTransitionState.tint = sun.color;
                startTransitionState.ambientColor = RenderSettings.ambientLight;
                startTransitionState.fogColor = RenderSettings.fogColor;
                startTransitionState.fogDensity = RenderSettings.fogDensity;

                activeIndex = index;
                transitionProgress = 0f;
                isTransitioning = true;
            }
        }

        private void Update()
        {
            if (isTransitioning && Application.isPlaying)
            {
                transitionProgress += Time.deltaTime / transitionDuration;
                float t = Mathf.Clamp01(transitionProgress);
                float smoothT = Mathf.SmoothStep(0f, 1f, t); // Use smoothstep for a more natural curve

                Preset target = presets[activeIndex];

                // Lighting transition
                float currentAngle = Mathf.LerpAngle(startTransitionState.sunAngle, target.sunAngle, smoothT);
                float currentRot = Mathf.LerpAngle(startTransitionState.sunRotation, target.sunRotation, smoothT);
                sun.transform.eulerAngles = new Vector3(currentAngle, currentRot, 0f);
                
                sun.intensity = Mathf.Lerp(startTransitionState.intensity, target.intensity, smoothT);
                sun.color = Color.Lerp(startTransitionState.tint, target.tint, smoothT);
                m_skybox.SetFloat("_Rotation", -sun.transform.eulerAngles.y);

                // Ambient light and fog transition
                RenderSettings.ambientLight = Color.Lerp(startTransitionState.ambientColor, target.ambientColor, smoothT);
                RenderSettings.fogColor = Color.Lerp(startTransitionState.fogColor, target.fogColor, smoothT);
                RenderSettings.fogDensity = Mathf.Lerp(startTransitionState.fogDensity, target.fogDensity, smoothT);

                // Transition finished
                if (t >= 1f)
                {
                    isTransitioning = false;
                    if (reflectionProbe) reflectionProbe.RenderProbe();
                }
            }
        }
        
        private void CreateSkyboxMat(Material source)
        {
            m_skybox = new Material(source);
            m_skybox.name = "Temp skybox";
        }
        
        private void OnNumberKeyPressed(int index)
        {
            if (index < presets.Length)
            {
                ApplyPreset(index);
            }
        }
        
        #if UNITY_EDITOR
        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();
            OnGUI();
            Handles.EndGUI();
        }
        #endif

        private void OnGUI()
        {
            if (!ShowGUI) return;
            
            using (new GUILayout.HorizontalScope(GUILayout.Width(300f)))
            {
                GUILayout.Label("  Lighting Presets:", GUI.skin.label);

                for (int i = 0; i < presets.Length; i++)
                {
                    GUI.enabled = (activeIndex != i);
                    if (GUILayout.Button(presets[i].name))
                    {
                        ApplyPreset(i);
                        
                        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
                        #endif
                    }
                }
                
                GUI.enabled = true;
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(DemoLightingController))]
    public class DemoLightingControllerEditor : Editor
    {
        private DemoLightingController component;
        private SerializedProperty presets;
        private SerializedProperty reflectionProbe;
        private SerializedProperty transitionDuration; // Added property
        
        private string proSkinPrefix => EditorGUIUtility.isProSkin ? "d_" : "";
        
        private void OnEnable()
        {
            component = (DemoLightingController)target;
            presets = serializedObject.FindProperty("presets");
            reflectionProbe = serializedObject.FindProperty("reflectionProbe");
            transitionDuration = serializedObject.FindProperty("transitionDuration"); // Property binding
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(reflectionProbe);
            DemoLightingController.ShowGUI = EditorGUILayout.Toggle("Show GUI", DemoLightingController.ShowGUI);
            
            // Display transition duration in the inspector
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(transitionDuration);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            
            for (int i = 0; i < presets.arraySize; i++)
            {
                if (GUILayout.Button("Set Active"))
                {
                    component.activeIndex = i;
                    component.ApplyPreset(i, true); // Use instant transition for preview in editor
                    EditorUtility.SetDirty(component);
                }

                using (new EditorGUI.DisabledGroupScope(component.activeIndex != i))
                {
                    EditorGUI.BeginChangeCheck();
                    
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Space(5f);
                            SerializedProperty param = presets.GetArrayElementAtIndex(i);
                            EditorGUILayout.PropertyField(param);
                            GUILayout.Space(5f);
                        }

                        if (GUILayout.Button(new GUIContent("", EditorGUIUtility.IconContent(proSkinPrefix + "TreeEditor.Trash").image, "Remove parameter"), EditorStyles.miniButton, GUILayout.Width(30f))) presets.DeleteArrayElementAtIndex(i);
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (component.activeIndex == i)
                        {
                            component.ApplyPreset(i, true);
                        }
                    }
                }
                
                GUILayout.Space(3f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent(" Add", EditorGUIUtility.IconContent(proSkinPrefix + "Toolbar Plus").image, "Insert new parameter"), EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    presets.InsertArrayElementAtIndex(presets.arraySize);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
    #endif
}