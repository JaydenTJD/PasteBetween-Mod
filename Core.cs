using Il2CppSystem.Xml.Linq;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppSystem.Collections;
using Il2CppNcp.Game;
using HarmonyLib;

[assembly: MelonInfo(typeof(PasteBetween.Core), "PasteBetween", "2.0.0", "Jayden", null)]
[assembly: MelonGame("Notional Games", "Beltmatic")]

namespace PasteBetween
{
    public class Core : MelonMod
    {
        private readonly string filepath = Path.Combine(MelonEnvironment.UserDataDirectory, "Clipboard.txt");
        public Il2CppSystem.Collections.Generic.List<Blueprint> clipboardHistory = new();
        private bool loaded = false;

        public override void OnUpdate()
        {
            if (GameManager.Instance?.AnySimulation != null && !loaded)
            {
                if (File.Exists(filepath))
                {
                    XElement rootElement = XElement.Load(filepath);
                    var enumerator = rootElement.Elements().GetEnumerator().Cast<IEnumerator>();
                    while (enumerator.MoveNext())
                    {
                        Blueprint blueprint = Blueprint.Deserialize(GameManager.Instance.AnySimulation.Cast<ISimulation>(), enumerator.Current.Cast<XElement>());
                        if (blueprint != null)
                        {
                            clipboardHistory.Add(blueprint);
                        }
                    }

                    loaded = true;

                    GameManager.Instance.simulationData.ClipboardHistory = clipboardHistory;
                }
            }
        }

        public override void OnDeinitializeMelon()
        {
            XElement rootElement = new((XName)"Root");
            foreach (Blueprint blueprint in GameManager.Instance.simulationData.ClipboardHistory)
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
                Melon<Core>.Instance.clipboardHistory = GameManager.Instance.simulationData.ClipboardHistory;
            }

            return true;
        }

        public static void Postfix(string name)
        {
            if (name == "NewGame" || name == "Continue")
            {
                GameManager.Instance.simulationData.ClipboardHistory = Melon<Core>.Instance.clipboardHistory;
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
                Melon<Core>.Instance.clipboardHistory = GameManager.Instance.simulationData.ClipboardHistory;
            }

            return true;
        }

        public static void Postfix(string name)
        {
            if (name == "Load")
            {
                GameManager.Instance.simulationData.ClipboardHistory = Melon<Core>.Instance.clipboardHistory;
            }
        }
    }
}