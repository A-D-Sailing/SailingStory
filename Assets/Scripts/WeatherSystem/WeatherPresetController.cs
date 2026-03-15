using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using KToolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeatherSystem
{
    [ExecuteAlways]
    public class WeatherPresetController : MonoBehaviour
    {
        [Serializable]
        public class SceneObjectState
        {
            public GameObject target;
            public bool active = true;
        }

        [Serializable]
        public class BehaviourState
        {
            public Behaviour target;
            public bool enabled = true;
        }

        [Serializable]
        public class TransformState
        {
            public Transform target;
            public bool applyLocalPosition;
            public Vector3 localPosition;
            public bool applyLocalRotation = true;
            public Vector3 localEulerAngles;
            public bool applyLocalScale;
            public Vector3 localScale = Vector3.one;
        }

        [Serializable]
        public class ParticleState
        {
            public ParticleSystem target;
            public bool playing = true;

            public bool overrideEmissionRate;
            [Min(0f)]
            public float emissionRate = 32f;

            public bool overrideStartSpeed;
            [Min(0f)]
            public float startSpeedMultiplier = 1f;

            public bool overrideSimulationSpeed;
            [Min(0f)]
            public float simulationSpeed = 1f;
        }

        [Serializable]
        public class Preset
        {
            public string name = "New Weather";

            [Header("Scene States")]
            public SceneObjectState[] objectStates = Array.Empty<SceneObjectState>();
            public BehaviourState[] behaviourStates = Array.Empty<BehaviourState>();
            public TransformState[] transformStates = Array.Empty<TransformState>();
            public ParticleState[] particleStates = Array.Empty<ParticleState>();

            [Header("Lighting")]
            public bool overrideSkybox;
            public Material skybox;

            public bool overrideDirectionalLight;
            [Range(0f, 90f)]
            public float sunAngle = 45f;
            [Range(0f, 360f)]
            public float sunRotation;
            public float lightIntensity = 1f;
            public Color lightColor = Color.white;

            public bool overrideAmbientLight;
            public Color ambientLight = Color.gray;

            public bool overrideFog;
            public bool fogEnabled = true;
            public Color fogColor = Color.white;
            [Range(0.0001f, 0.05f)]
            public float fogDensity = 0.002f;

            public bool refreshReflectionProbe = true;
        }

        [Min(0)]
        public int activeIndex;
        public Preset[] presets = Array.Empty<Preset>();

        [Header("Shared References")]
        [SerializeField]
        private Light directionalLight;

        [SerializeField]
        private ReflectionProbe reflectionProbe;

        [SerializeField]
        private bool applyOnEnable = true;

        [SerializeField]
        private bool showRuntimeGUI = true;

        private Material runtimeSkybox;

        private static readonly int RotationId = Shader.PropertyToID("_Rotation");

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                ApplyActivePreset();
            }

#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
        }

        private void OnValidate()
        {
            if (presets == null || presets.Length == 0)
            {
                activeIndex = 0;
                return;
            }

            activeIndex = Mathf.Clamp(activeIndex, 0, presets.Length - 1);

            if (!isActiveAndEnabled || !applyOnEnable)
            {
                return;
            }

            ApplyActivePreset();
        }

        [ContextMenu("Apply Active Preset")]
        public void ApplyActivePreset()
        {
            if (presets == null || presets.Length == 0)
            {
                return;
            }

            ApplyPreset(activeIndex);
        }

        public void ApplyPreset(int index)
        {
            if (presets == null || presets.Length == 0)
            {
                return;
            }

            if (index < 0 || index >= presets.Length)
            {
                return;
            }

            activeIndex = index;

            Preset preset = presets[index];

            ApplySceneObjectStates(preset.objectStates);
            ApplyBehaviourStates(preset.behaviourStates);
            ApplyTransformStates(preset.transformStates);
            ApplyParticleStates(preset.particleStates);
            ApplyLighting(preset);
        }

        private void ApplySceneObjectStates(SceneObjectState[] states)
        {
            if (states == null)
            {
                return;
            }

            for (int i = 0; i < states.Length; i++)
            {
                SceneObjectState state = states[i];
                if (state == null || state.target == null)
                {
                    continue;
                }

                state.target.SetActive(state.active);
            }
        }

        private void ApplyBehaviourStates(BehaviourState[] states)
        {
            if (states == null)
            {
                return;
            }

            for (int i = 0; i < states.Length; i++)
            {
                BehaviourState state = states[i];
                if (state == null || state.target == null)
                {
                    continue;
                }

                state.target.enabled = state.enabled;
            }
        }

        private void ApplyTransformStates(TransformState[] states)
        {
            if (states == null)
            {
                return;
            }

            for (int i = 0; i < states.Length; i++)
            {
                TransformState state = states[i];
                if (state == null || state.target == null)
                {
                    continue;
                }

                if (state.applyLocalPosition)
                {
                    state.target.localPosition = state.localPosition;
                }

                if (state.applyLocalRotation)
                {
                    state.target.localEulerAngles = state.localEulerAngles;
                }

                if (state.applyLocalScale)
                {
                    state.target.localScale = state.localScale;
                }
            }
        }

        private void ApplyParticleStates(ParticleState[] states)
        {
            if (states == null)
            {
                return;
            }

            for (int i = 0; i < states.Length; i++)
            {
                ParticleState state = states[i];
                if (state == null || state.target == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = state.target.main;
                ParticleSystem.EmissionModule emission = state.target.emission;

                if (state.overrideEmissionRate)
                {
                    emission.rateOverTimeMultiplier = state.emissionRate;
                }

                if (state.overrideStartSpeed)
                {
                    main.startSpeedMultiplier = state.startSpeedMultiplier;
                }

                if (state.overrideSimulationSpeed)
                {
                    main.simulationSpeed = state.simulationSpeed;
                }

                if (state.playing)
                {
                    if (!state.target.isPlaying)
                    {
                        state.target.Play(true);
                    }
                }
                else
                {
                    state.target.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void ApplyLighting(Preset preset, float transitionDuration = 3f)
        {
            if (preset.overrideSkybox && preset.skybox != null)
            {
                EnsureSkyboxInstance(preset.skybox);
                RenderSettings.skybox = runtimeSkybox;
            }

            if (preset.overrideDirectionalLight)
            {
                Light targetLight = ResolveDirectionalLight();
                if (targetLight != null)
                {
                    targetLight.transform.DORotate(
                        new Vector3(preset.sunAngle, preset.sunRotation, 0f), transitionDuration);
                    // targetLight.transform.eulerAngles = new Vector3(preset.sunAngle, preset.sunRotation, 0f);
                    targetLight.DOColor(preset.lightColor, transitionDuration);
                    targetLight.DOIntensity(preset.lightIntensity, transitionDuration);
                }
            }

            if (preset.overrideSkybox && runtimeSkybox != null && runtimeSkybox.HasProperty(RotationId))
            {
                // todo 可改但不重要，目前没有明显看出来
                runtimeSkybox.SetFloat(RotationId, -preset.sunRotation);
            }

            if (preset.overrideAmbientLight)
            {
                // todo 可改但不重要，目前没有明显看出来
                RenderSettings.ambientLight = preset.ambientLight;
            }

            if (preset.overrideFog)
            {
                RenderSettings.fog = preset.fogEnabled;
                StartCoroutine(SetRenderSettingFog(preset, transitionDuration));
            }

            if (preset.refreshReflectionProbe && reflectionProbe != null)
            {
                reflectionProbe.RenderProbe();
            }
        }

        IEnumerator SetRenderSettingFog(Preset preset, float transitionDuration)
        {
            float transitionTime = 0f;
            Color currentFogColor = RenderSettings.fogColor;
            float currentFogDensity = RenderSettings.fogDensity;
            while (transitionTime < transitionDuration)
            {
                transitionTime += Time.deltaTime;
                RenderSettings.fogColor =
                    Color.Lerp(currentFogColor, preset.fogColor, transitionTime / transitionDuration);
                RenderSettings.fogDensity =
                    currentFogDensity + (preset.fogDensity - currentFogDensity) * transitionTime / transitionDuration;
                yield return null;
            }
        }

        private void EnsureSkyboxInstance(Material source)
        {
            if (source == null)
            {
                return;
            }

            if (runtimeSkybox == null || runtimeSkybox.shader != source.shader)
            {
                ReleaseRuntimeSkybox();
                runtimeSkybox = new Material(source)
                {
                    name = $"{source.name} (Weather Runtime)"
                };
            }

            runtimeSkybox.CopyPropertiesFromMaterial(source);
        }

        private Light ResolveDirectionalLight()
        {
            if (directionalLight != null)
            {
                return directionalLight;
            }

            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    directionalLight = lights[i];
                    break;
                }
            }

            return directionalLight;
        }

        private void ReleaseRuntimeSkybox()
        {
            if (runtimeSkybox == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeSkybox);
            }
            else
            {
                DestroyImmediate(runtimeSkybox);
            }

            runtimeSkybox = null;
        }

        private void OnGUI()
        {
            DrawPresetGUI();
        }

#if UNITY_EDITOR
        private void OnSceneGUI(SceneView sceneView)
        {
            if (Application.isPlaying)
            {
                return;
            }

            Handles.BeginGUI();
            DrawPresetGUI();
            Handles.EndGUI();
        }
#endif

        private void DrawPresetGUI()
        {
            if (!showRuntimeGUI || presets == null || presets.Length == 0)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, 720f, 48f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Weather Presets", GUILayout.Width(110f));

            for (int i = 0; i < presets.Length; i++)
            {
                bool isActive = activeIndex == i;
                GUI.enabled = !isActive;

                string label = string.IsNullOrWhiteSpace(presets[i].name) ? $"Preset {i + 1}" : presets[i].name;
                if (GUILayout.Button(label, GUILayout.Height(24f)))
                {
                    ApplyPreset(i);

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WeatherPresetController))]
    public class WeatherPresetControllerEditor : Editor
    {
        private SerializedProperty activeIndexProperty;
        private SerializedProperty presetsProperty;
        private SerializedProperty directionalLightProperty;
        private SerializedProperty reflectionProbeProperty;
        private SerializedProperty applyOnEnableProperty;
        private SerializedProperty showRuntimeGUIProperty;

        private void OnEnable()
        {
            activeIndexProperty = serializedObject.FindProperty("activeIndex");
            presetsProperty = serializedObject.FindProperty("presets");
            directionalLightProperty = serializedObject.FindProperty("directionalLight");
            reflectionProbeProperty = serializedObject.FindProperty("reflectionProbe");
            applyOnEnableProperty = serializedObject.FindProperty("applyOnEnable");
            showRuntimeGUIProperty = serializedObject.FindProperty("showRuntimeGUI");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WeatherPresetController controller = (WeatherPresetController)target;

            EditorGUILayout.PropertyField(directionalLightProperty);
            EditorGUILayout.PropertyField(reflectionProbeProperty);
            EditorGUILayout.PropertyField(applyOnEnableProperty);
            EditorGUILayout.PropertyField(showRuntimeGUIProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            for (int i = 0; i < presetsProperty.arraySize; i++)
            {
                SerializedProperty presetProperty = presetsProperty.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(controller.activeIndex == i))
                {
                    if (GUILayout.Button("Set Active", GUILayout.Width(90f)))
                    {
                        activeIndexProperty.intValue = i;
                        serializedObject.ApplyModifiedProperties();
                        controller.ApplyPreset(i);
                        EditorUtility.SetDirty(controller);
                        serializedObject.Update();
                    }
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    presetsProperty.DeleteArrayElementAtIndex(i);
                    if (controller.activeIndex >= presetsProperty.arraySize)
                    {
                        activeIndexProperty.intValue = Mathf.Max(0, presetsProperty.arraySize - 1);
                    }

                    serializedObject.ApplyModifiedProperties();

                    if (controller.isActiveAndEnabled)
                    {
                        controller.ApplyActivePreset();
                        EditorUtility.SetDirty(controller);
                    }

                    serializedObject.Update();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(presetProperty, true);
                bool changed = EditorGUI.EndChangeCheck();
                EditorGUILayout.EndVertical();

                if (changed)
                {
                    serializedObject.ApplyModifiedProperties();

                    if (controller.activeIndex == i && controller.isActiveAndEnabled)
                    {
                        controller.ApplyPreset(i);
                        EditorUtility.SetDirty(controller);
                    }

                    serializedObject.Update();
                }
            }

            if (GUILayout.Button("Add Preset"))
            {
                presetsProperty.InsertArrayElementAtIndex(presetsProperty.arraySize);
                SerializedProperty newPreset = presetsProperty.GetArrayElementAtIndex(presetsProperty.arraySize - 1);
                SerializedProperty nameProperty = newPreset.FindPropertyRelative("name");
                if (nameProperty != null)
                {
                    nameProperty.stringValue = $"Weather {presetsProperty.arraySize}";
                }
            }

            if (presetsProperty.arraySize > 0)
            {
                activeIndexProperty.intValue = Mathf.Clamp(activeIndexProperty.intValue, 0, presetsProperty.arraySize - 1);
            }
            else
            {
                activeIndexProperty.intValue = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
