using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ColoringPixelsMod {
    [BepInPlugin("com.midnight.customimages", "CustomImageMod", "1.0.0")]
    public class CustomImagesPlugin : BaseUnityPlugin {
        private const string ModName = "CustomImageMod";
        private const string ModVersion = "1.0.0";
        private const string ModGuid = "com.midnight.customimages";

        public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("CustomImagesPlugin");
        
        private void Awake() {
            Harmony harmony = new Harmony(CustomImagesPlugin.ModGuid);
            harmony.PatchAll();
            Log.LogInfo(CustomImagesPlugin.ModName + " (" + CustomImagesPlugin.ModGuid + ") @ " + CustomImagesPlugin.ModVersion + " loaded!");
        }
    }
}
