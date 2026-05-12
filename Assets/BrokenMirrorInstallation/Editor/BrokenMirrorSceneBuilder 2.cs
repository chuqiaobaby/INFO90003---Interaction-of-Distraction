using BrokenMirrorInstallation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BrokenMirrorInstallation.Editor
{
    public static class BrokenMirrorSceneBuilder
    {
        private const string Root = "Assets/BrokenMirrorInstallation";
        private const string MaterialPath = Root + "/Materials/BrokenMirror_Material.mat";
        private const string ScenePath = Root + "/Scenes/BrokenMirrorInstallation.unity";
        private const string CrackTexturePath = Root + "/Textures/crack_mask_cinematic.png";
        private const string UrpAssetPath = Root + "/Settings/BrokenMirror_URP.asset";
        private const string RendererAssetPath = Root + "/Settings/BrokenMirror_ForwardRenderer.asset";

        [MenuItem("Tools/Broken Mirror/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureFolders();
            EnsureUrp();
            Material mirrorMaterial = EnsureMirrorMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BrokenMirrorInstallation";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.018f, 0.022f, 0.028f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.018f, 0.024f);
            RenderSettings.fogDensity = 0.022f;

            GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mirror.name = "Webcam Broken Mirror";
            mirror.transform.position = new Vector3(0f, 1.65f, 0f);
            mirror.transform.localScale = new Vector3(6.4f, 3.6f, 1f);
            Renderer mirrorRenderer = mirror.GetComponent<Renderer>();
            mirrorRenderer.sharedMaterial = mirrorMaterial;
            mirror.AddComponent<WebcamReflectionSource>();
            mirror.AddComponent<MirrorFractureController>();
            mirror.AddComponent<MirrorKeyboardInput>();

            CreateFrame("Mirror Frame Top", new Vector3(0f, 3.55f, 0.045f), new Vector3(6.75f, 0.08f, 0.08f));
            CreateFrame("Mirror Frame Bottom", new Vector3(0f, -0.25f, 0.045f), new Vector3(6.75f, 0.08f, 0.08f));
            CreateFrame("Mirror Frame Left", new Vector3(-3.42f, 1.65f, 0.045f), new Vector3(0.08f, 3.9f, 0.08f));
            CreateFrame("Mirror Frame Right", new Vector3(3.42f, 1.65f, 0.045f), new Vector3(0.08f, 3.9f, 0.08f));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Dark Gallery Floor";
            floor.transform.position = new Vector3(0f, -0.56f, 1.65f);
            floor.transform.localScale = new Vector3(9f, 0.08f, 7f);
            floor.GetComponent<Renderer>().sharedMaterial = CreateSimpleMaterial("GalleryFloor_Material", new Color(0.018f, 0.02f, 0.025f), 0.2f);

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Shadow Wall";
            backWall.transform.position = new Vector3(0f, 1.55f, 0.18f);
            backWall.transform.localScale = new Vector3(8.5f, 5.5f, 0.08f);
            backWall.GetComponent<Renderer>().sharedMaterial = CreateSimpleMaterial("ShadowWall_Material", new Color(0.011f, 0.013f, 0.018f), 0.05f);

            Camera camera = new GameObject("Installation Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 1.6f, -5.6f);
            camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.008f, 0.012f);
            UniversalAdditionalCameraData cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            Light keyLight = new GameObject("Cold Gallery Key Light").AddComponent<Light>();
            keyLight.type = LightType.Area;
            keyLight.transform.position = new Vector3(-2.4f, 4.2f, -2.1f);
            keyLight.transform.rotation = Quaternion.Euler(64f, -22f, 0f);
            keyLight.color = new Color(0.68f, 0.83f, 1f);
            keyLight.intensity = 260f;
            keyLight.areaSize = 4f;

            Light edgeLight = new GameObject("Low Reflection Edge Light").AddComponent<Light>();
            edgeLight.type = LightType.Point;
            edgeLight.transform.position = new Vector3(2.8f, 0.65f, -1.4f);
            edgeLight.color = new Color(0.5f, 0.76f, 0.92f);
            edgeLight.intensity = 30f;
            edgeLight.range = 4.5f;

            CreateGlobalVolume();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = mirror;
            Debug.Log("Broken Mirror prototype scene built at " + ScenePath);
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "BrokenMirrorInstallation");
            CreateFolder(Root, "Materials");
            CreateFolder(Root, "Scenes");
            CreateFolder(Root, "Settings");
            CreateFolder(Root, "Textures");
        }

        private static void CreateFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureUrp()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                return;
            }

            UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            UniversalRenderPipelineAsset pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, UrpAssetPath);

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
        }

        private static Material EnsureMirrorMaterial()
        {
            ConfigureCrackTextureImport();

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("BrokenMirror/URP/Broken Mirror Webcam");
                material = new Material(shader)
                {
                    name = "BrokenMirror_Material"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Texture2D crackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CrackTexturePath);
            if (crackTexture != null)
            {
                material.SetTexture("_CrackTex", crackTexture);
            }

            material.SetColor("_Tint", new Color(0.78f, 0.86f, 0.92f, 1f));
            material.SetColor("_CrackColor", new Color(0.72f, 0.9f, 1f, 1f));
            material.SetFloat("_Darken", 0.12f);
            material.SetFloat("_Contrast", 1.05f);
            material.SetFloat("_FlipX", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureCrackTextureImport()
        {
            TextureImporter importer = AssetImporter.GetAtPath(CrackTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void CreateFrame(string name, Vector3 position, Vector3 scale)
        {
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = name;
            frame.transform.position = position;
            frame.transform.localScale = scale;
            frame.GetComponent<Renderer>().sharedMaterial = CreateSimpleMaterial("MirrorFrame_Material", new Color(0.035f, 0.042f, 0.052f), 0.5f);
        }

        private static Material CreateSimpleMaterial(string name, Color color, float smoothness)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = name
            };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateGlobalVolume()
        {
            GameObject volumeObject = new GameObject("Atmospheric Post Processing");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "BrokenMirror_VolumeProfile";

            Vignette vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.62f);

            Bloom bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.18f);
            bloom.threshold.Override(1.08f);

            FilmGrain filmGrain = profile.Add<FilmGrain>();
            filmGrain.intensity.Override(0.18f);
            filmGrain.response.Override(0.72f);

            ChromaticAberration chromaticAberration = profile.Add<ChromaticAberration>();
            chromaticAberration.intensity.Override(0.05f);

            AssetDatabase.CreateAsset(profile, Root + "/Settings/BrokenMirror_VolumeProfile.asset");
            volume.sharedProfile = profile;
        }
    }
}
