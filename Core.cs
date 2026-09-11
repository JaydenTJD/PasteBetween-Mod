using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine.SceneManagement;
using UnityEngine;
using Il2CppSystem.Xml.Linq;
using Il2CppSystem.Collections;
using Il2CppNcp.Game;
using HarmonyLib;

[assembly: MelonInfo(typeof(PasteBetween.Core), "PasteBetween", "1.0.0", "Jayden", null)]
[assembly: MelonGame("Notional Games", "Beltmatic")]

namespace PasteBetween
{
    public class Core : MelonMod
    {
        private readonly string filepath = Path.Combine(MelonEnvironment.UserDataDirectory, "Clipboard.txt");
        public GameManager gameManager;
        public Il2CppSystem.Collections.Generic.List<Blueprint> clipboardHistory;
        private bool loaded = false;
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            clipboardHistory = new();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameObject dummy = new();
            UnityEngine.Object.DontDestroyOnLoad(dummy);
            Scene ddolScene = dummy.scene;
            UnityEngine.Object.Destroy(dummy);
            GameObject gameManagerGO = null;
            var rootObjects = ddolScene.GetRootGameObjects();
            foreach (var GO in rootObjects)
            {
                if (GO != null && GO.name == "GameManager")
                {
                    gameManagerGO = GO;
                    break;
                }
            }
            gameManager = gameManagerGO.GetComponent<GameManager>();
        }

        public override void OnUpdate()
        {
            if (gameManager?.demoSimulation != null && !loaded)
            {
                if (File.Exists(filepath))
                {
                    XElement rootElement = XElement.Load(filepath);
                    var enumerator = rootElement.Elements().GetEnumerator().Cast<IEnumerator>();
                    while (enumerator.MoveNext())
                    {
                        Blueprint blueprint = Blueprint.Deserialize(gameManager.demoSimulation.Cast<ISimulation>(), enumerator.Current.Cast<XElement>());
                        if (blueprint != null)
                        {
                            clipboardHistory.Add(blueprint);
                        }
                    }

                    loaded = true;

                    gameManager.simulationData.ClipboardHistory = clipboardHistory;
                }
            }
        }

        public override void OnDeinitializeMelon()
        {
            XElement rootElement = new((XName)"Root");
            foreach (Blueprint blueprint in gameManager.simulationData.ClipboardHistory)
            {
                XElement serializedBlueprint = blueprint.Serialize();
                if (serializedBlueprint != null)
                {
                    rootElement.Add(serializedBlueprint);
                }
            }

            rootElement.Save(filepath);
        }
    }

    [HarmonyPatch(typeof(MainMenu), "OnButtonPress")]
    public static class Patch_MainMenu_OnButtonPress
    {
        public static bool Prefix(string name)
        {
            if (name == "NewGame")
            {
                Melon<Core>.Instance.clipboardHistory = Melon<Core>.Instance.gameManager.simulationData.ClipboardHistory;
            }
            return true;
        }

        public static void Postfix(string name)
        {
            if (name == "NewGame" || name == "Continue")
            {
                Melon<Core>.Instance.gameManager.simulationData.ClipboardHistory = Melon<Core>.Instance.clipboardHistory;
            }
        }
    }

    [HarmonyPatch(typeof(LoadGameMenu), "OnButtonPress")]
    public static class Patch_LoadGameMenu_OnButtonPress
    {
        public static bool Prefix(string name)
        {
            if (name == "Load")
            {
                Melon<Core>.Instance.clipboardHistory = Melon<Core>.Instance.gameManager.simulationData.ClipboardHistory;
            }
            return true;
        }

        public static void Postfix(string name)
        {
            if (name == "Load")
            {
                Melon<Core>.Instance.gameManager.simulationData.ClipboardHistory = Melon<Core>.Instance.clipboardHistory;
            }
        }
    }
}