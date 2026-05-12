using BrokenMirrorInstallation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BrokenMirrorInstallation.Editor
{
    public static class BrokenMirrorSceneBuilder
    {
        private const string Root = "Assets/BrokenMirrorInstallation";
        private const string MaterialPath = Root + "/Materials/BrokenMirror_Material.mat";
        private const string ScenePath = Root + "/Scenes/BrokenMirrorInstallation.unity";
        private const string CrackTexturePath = Root + "/Textures/crack_mask_cinematic.png";

        [MenuItem("Tools/Broken Mirror/Build Prototype Scene")]
        [MenuItem("Broken Mirror/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            CleanMissingScriptsInOpenScene();
            EnsureFolders();
            Material mirrorMaterial = EnsureMirrorMaterial();
            if (mirrorMaterial == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BrokenMirrorInstallation";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.018f, 0.022f, 0.028f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.018f, 0.024f);
            RenderSettings.fogDensity = 0.022f;

            GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mirror.name = "Webcam Broken Mirror";
            mirror.transform.position = new Vector3(0f, 1.6f, 0f);
            mirror.transform.localScale = new Vector3(7.15f, 4.02f, 1f);
            Renderer mirrorRenderer = mirror.GetComponent<Renderer>();
            mirrorRenderer.sharedMaterial = mirrorMaterial;
            mirror.AddComponent<WebcamReflectionSource>();
            mirror.AddComponent<MirrorFractureController>();
            mirror.AddComponent<MirrorKeyboardInput>();

            CreateFrame("Mirror Frame Top", new Vector3(0f, 3.66f, 0.045f), new Vector3(7.42f, 0.075f, 0.08f));
            CreateFrame("Mirror Frame Bottom", new Vector3(0f, -0.46f, 0.045f), new Vector3(7.42f, 0.075f, 0.08f));
            CreateFrame("Mirror Frame Left", new Vector3(-3.68f, 1.6f, 0.045f), new Vector3(0.075f, 4.18f, 0.08f));
            CreateFrame("Mirror Frame Right", new Vector3(3.68f, 1.6f, 0.045f), new Vector3(0.075f, 4.18f, 0.08f));

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
            camera.transform.position = new Vector3(0f, 1.6f, -4.2f);
            camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            camera.fieldOfView = 50f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.008f, 0.012f);

            Light keyLight = new GameObject("Cold Gallery Key Light").AddComponent<Light>();
            keyLight.type = LightType.Rectangle;
            keyLight.transform.position = new Vector3(-2.4f, 4.2f, -2.1f);
            keyLight.transform.rotation = Quaternion.Euler(64f, -22f, 0f);
            keyLight.color = new Color(0.68f, 0.83f, 1f);
            keyLight.intensity = 260f;
            keyLight.areaSize = new Vector2(4f, 4f);

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

        [MenuItem("Tools/Broken Mirror/Clean Missing Scripts In Open Scene")]
        [MenuItem("Broken Mirror/Clean Missing Scripts In Open Scene")]
        public static void CleanMissingScriptsInOpenScene()
        {
            int removedCount = 0;
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in transforms)
                {
                    removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                }
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("Broken Mirror removed " + removedCount + " missing script reference(s).");
            }
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

        private static Material EnsureMirrorMaterial()
        {
            ConfigureCrackTextureImport();

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("BrokenMirror/URP/Broken Mirror Webcam");
                if (shader == null)
                {
                    Debug.LogError("Broken Mirror shader was not found. Wait for Unity to finish compiling, then run this menu item again.");
                    return null;
                }

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

            material.SetColor("_Tint", Color.white);
            material.SetColor("_CrackColor", new Color(0.72f, 0.9f, 1f, 1f));
            material.SetFloat("_MirrorState", 0f);
            material.SetFloat("_CrackStrength", 0f);
            material.SetFloat("_DistortionStrength", 0f);
            material.SetFloat("_RippleStrength", 0f);
            material.SetFloat("_BlurStrength", 0f);
            material.SetFloat("_ChromaticStrength", 0f);
            material.SetFloat("_Instability", 0f);
            material.SetFloat("_Darken", 0f);
            material.SetFloat("_Contrast", 1f);
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

            Shader shader = Shader.Find("BrokenMirror/Utility/Simple Atmosphere");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else
            {
                material.color = color;
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateGlobalVolume()
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "BrokenMirror_VolumeProfile";
            AssetDatabase.CreateAsset(profile, Root + "/Settings/BrokenMirror_VolumeProfile.asset");
        }
    }
}
