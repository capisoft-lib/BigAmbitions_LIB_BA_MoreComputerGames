using System;
using System.Reflection;
using Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Capisoft.Lib.BaComputerGames
{
    internal static class NativeScreenEffects
    {
        private static readonly FieldInfo VolumeField = typeof(VideoGameSetup).GetField(
            "VideoGameVolumeInstance", BindingFlags.Static | BindingFlags.NonPublic);

        internal static IDisposable BeginSession()
        {
            try
            {
                // SetupScreen creates this exact volume before asking our view for its resolution.
                // Do not search/disable unrelated scene volumes or change the main camera settings.
                var root = VolumeField?.GetValue(null) as GameObject;
                var scope = RetroEffectScope.Create(root != null ? root.GetComponent<Volume>() : null);
                if (scope != null)
                    Debug.Log("[MCG] Native NTSC filter disabled for this game session; exposure preserved.");
                else
                    Debug.LogWarning("[MCG] Native NTSC volume not found; retaining native screen effects.");
                return scope;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[MCG] Native screen-effect adapter unavailable: " + exception.Message);
                return null; // A changed optional rendering hook must not prevent the game from loading.
            }
        }
    }
}
