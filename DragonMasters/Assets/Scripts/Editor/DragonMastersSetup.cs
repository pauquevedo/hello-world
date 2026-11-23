using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DragonMasters.EditorTools
{
    /// <summary>
    /// Configura el proyecto automáticamente la primera vez que se abre en Unity:
    /// crea la escena principal, la añade a Build Settings y deja listos los ajustes de iOS.
    /// Menú: Dragon Masters → ...
    /// </summary>
    [InitializeOnLoad]
    public static class DragonMastersSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string BundleId = "com.brand01.dragonmasters";

        static DragonMastersSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath))
                {
                    CreateScene();
                    ConfigureIOS();
                    Debug.Log("[Dragon Masters] Proyecto configurado: escena creada y ajustes de iOS aplicados. ¡Dale a Play!");
                }
            };
        }

        [MenuItem("Dragon Masters/1. Configurar proyecto (escena + iOS)", priority = 1)]
        public static void SetupAll()
        {
            CreateScene();
            ConfigureIOS();
            Debug.Log("[Dragon Masters] Escena y ajustes de iOS listos.");
        }

        [MenuItem("Dragon Masters/2. Generar proyecto Xcode (iOS)", priority = 2)]
        public static void BuildIOS()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            ConfigureIOS();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Dragon Masters] Proyecto Xcode generado en Builds/iOS. Ábrelo con Xcode (Unity-iPhone.xcodeproj), elige tu equipo de firma y dale a Run.");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder("Builds/iOS");
            }
            else
            {
                Debug.LogError("[Dragon Masters] La build de iOS ha fallado: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Dragon Masters/Borrar progreso guardado", priority = 20)]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("[Dragon Masters] Progreso borrado (monedas, nivel y dragones desbloqueados).");
        }

        static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var c = cam.AddComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 5;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
            cam.transform.position = new Vector3(0, 3, -10);
            cam.AddComponent<AudioListener>();
            new GameObject("DragonMasters").AddComponent<GameManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
        }

        static void ConfigureIOS()
        {
            PlayerSettings.companyName = "Brand 01";
            PlayerSettings.productName = "Dragon Masters";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";

            // Solo horizontal
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = true;

            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // ARM64
            PlayerSettings.SplashScreen.showUnityLogo = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>Ajustes de importación de los sprites recortados (transparencia, sin mipmaps, ASTC en iOS).</summary>
    public class DragonMastersSpriteImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("/Resources/Sprites/"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.maxTextureSize = 512;
                ti.textureCompression = TextureImporterCompression.Compressed;
                var ios = ti.GetPlatformTextureSettings("iPhone");
                ios.overridden = true;
                ios.maxTextureSize = 512;
                ios.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(ios);
            }
            else if (assetPath == "Assets/Art/AppIcon.png")
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 1024;
                ti.npotScale = TextureImporterNPOTScale.None;
            }
        }
    }
}
