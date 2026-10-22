using System;
using HarmonyLib;
using Mapify.Map;
using UnityModManagerNet;

namespace Mapify
{
    public static class Mapify
    {
        private static UnityModManager.ModEntry ModEntry { get; set; }
        public static Settings Settings { get; private set; }

        internal static Harmony Harmony { get; private set; }

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            Settings = UnityModManager.ModSettings.Load<Settings>(ModEntry);
            ModEntry.OnGUI = entry => Settings.Draw(entry);
            ModEntry.OnSaveGUI = entry => Settings.Save(entry);

            try
            {
                if (!Locale.LoadCSV(ModEntry.Path))
                {
                    return false;
                }
                Maps.Init();
                Patch();
            }
            catch (Exception ex)
            {
                LogException("Failed to load", ex);
                return false;
            }

            return true;
        }

        private static void Patch()
        {
            LogInfo("Patching...");
            Harmony = new Harmony(ModEntry.Info.Id);
            Harmony.PatchAll();
            LogInfo("Successfully patched");
        }

        #region Logging

        public static void LogDebugExtreme(object msg)
        {
	        if (Settings.ExtremelyVerboseLogging)
	        {
		        LogDebug(msg);
	        }
        }

        public static void LogDebug(object msg)
        {
	        if (Settings.VerboseLogging)
	        {
		        ModEntry.Logger.Log($"[Debug] {msg}");
	        }
        }

        public static void LogInfo(object msg)
        {
            ModEntry.Logger.Log($"[Info] {msg}");
        }

        public static void LogWarning(object msg)
        {
            ModEntry.Logger.Warning($"{msg}");
        }

        public static void LogError(object msg)
        {
            ModEntry.Logger.Error($"{msg}");
        }

        public static void LogCritical(object msg)
        {
            ModEntry.Logger.Critical($"{msg}");
        }

        public static void LogException(object msg, Exception e)
        {
            ModEntry.Logger.LogException($"{msg}", e);
        }

        #endregion
    }
}
