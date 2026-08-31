using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Capisoft.Lib.BaComputerGames
{
    // Capture the operation created by vanilla so closing during its asynchronous callback cannot leak it.
    internal sealed class TrackedGameReference : AssetReferenceGameObject
    {
        private AsyncOperationHandle<GameObject> _handle;
        private bool _requested, _releasing;
        private int _completedFrame = int.MaxValue;
        internal bool Failed => _requested && (!_handle.IsValid() || _handle.IsDone && _handle.Status != AsyncOperationStatus.Succeeded);
        internal TrackedGameReference(string key) : base(key) { }
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            _requested = true;
            _handle = base.InstantiateAsync(parent, instantiateInWorldSpace);
            _handle.Completed += _ => _completedFrame = Time.frameCount;
            return _handle;
        }
        internal void ReleaseWhenSafe()
        {
            if (!_requested || _releasing || !_handle.IsValid()) return; _releasing = true;
            // This tiny reaper outlives a city/mod unload, but owns no gameplay or input.
            var reaper = new GameObject("BaComputerGames_DeferredRelease").AddComponent<GameInstanceReaper>();
            UnityEngine.Object.DontDestroyOnLoad(reaper.gameObject);
            reaper.StartCoroutine(Release(reaper.gameObject));
        }
        private IEnumerator Release(GameObject owner)
        {
            while (_handle.IsValid() && (!_handle.IsDone || Time.frameCount <= _completedFrame)) yield return null;
            if (_handle.IsValid())
            {
                if (_handle.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(_handle);
                else Addressables.Release(_handle);
            }
            UnityEngine.Object.Destroy(owner);
        }
    }
    internal sealed class GameInstanceReaper : MonoBehaviour { }
}
