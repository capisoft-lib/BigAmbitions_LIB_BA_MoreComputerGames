using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Capisoft.Lib.BaComputerGames
{
    // Change the native minigame volume for this session, never its shared prefab profile.
    internal sealed class RetroEffectScope : IDisposable
    {
        private const string EffectType = "CRTPostprocess.NTSCPostprocessHDRP";
        private Volume _volume;
        private VolumeProfile _previousProfile;
        private VolumeProfile _ownedProfile;

        internal static RetroEffectScope Create(Volume volume)
        {
            if (volume == null) return null;
            var previous = volume.HasInstantiatedProfile() ? volume.profile : null;
            var source = previous != null ? previous : volume.sharedProfile;
            if (FindEnable(source) == null) return null;

            var scope = new RetroEffectScope { _volume = volume, _previousProfile = previous };
            try
            {
                scope._ownedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                scope._ownedProfile.name = "MCG display";
                scope._ownedProfile.hideFlags = HideFlags.HideAndDontSave;
                foreach (var component in source.components)
                {
                    if (component == null) continue;
                    var copy = UnityEngine.Object.Instantiate(component);
                    copy.hideFlags = HideFlags.HideAndDontSave;
                    scope._ownedProfile.components.Add(copy);
                }
                FindEnable(scope._ownedProfile).Override(false);
                volume.profile = scope._ownedProfile;
                return scope;
            }
            catch { scope.Dispose(); throw; }
        }

        private static BoolParameter FindEnable(VolumeProfile profile)
        {
            if (profile == null) return null;
            foreach (var component in profile.components)
            {
                if (component == null || component.GetType().FullName != EffectType) continue;
                return component.GetType().GetField("enable", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(component) as BoolParameter;
            }
            return null;
        }

        public void Dispose()
        {
            if (_ownedProfile == null) return;
            if (_volume != null && _volume.HasInstantiatedProfile() && _volume.profile == _ownedProfile)
                _volume.profile = _previousProfile;
            foreach (var component in _ownedProfile.components)
                if (component != null) UnityEngine.Object.Destroy(component);
            UnityEngine.Object.Destroy(_ownedProfile);
            _ownedProfile = null; _previousProfile = null; _volume = null;
        }
    }
}
