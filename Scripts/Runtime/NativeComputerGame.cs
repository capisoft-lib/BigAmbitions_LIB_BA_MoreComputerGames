using System;
using System.Threading;
using System.Threading.Tasks;
using Items.SpecialItems.VideoGames;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Capisoft.Lib.BaComputerGames
{
    // Own the Addressables operation, including completion after the player has left.
    internal sealed class NativeComputerGame : IDisposable
    {
        private AsyncOperationHandle<GameObject> _handle;
        private GameObject _storage;
        private bool _disposed;
        internal IVideoGame Game { get; private set; }
        internal GameObject Root => _storage;
        internal static async Task<NativeComputerGame> LoadAsync(object key, Transform parent, CancellationToken token)
        {
            var lease = new NativeComputerGame();
            try
            {
                token.ThrowIfCancellationRequested();
                // Inactive staging outlives the launcher while loading is still in flight.
                lease._storage = new GameObject("MCG_NativeGameLoading");
                lease._storage.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(lease._storage);
                lease._handle = Addressables.InstantiateAsync(key, lease._storage.transform);
                await lease._handle.Task;
                token.ThrowIfCancellationRequested();
                if (parent == null) throw new OperationCanceledException();
                if (lease._handle.Status != AsyncOperationStatus.Succeeded || lease._handle.Result == null)
                    throw new InvalidOperationException("Native computer game could not be loaded.");
                lease.Game = lease._handle.Result.GetComponent<IVideoGame>();
                if (lease.Game == null) throw new InvalidOperationException("Native computer game has no IVideoGame component.");
                lease._storage.transform.SetParent(parent, false);
                return lease;
            }
            catch { lease.Dispose(); throw; }
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if (_storage != null) _storage.SetActive(false);
            if (_handle.IsValid())
            {
                if (_handle.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(_handle);
                else Addressables.Release(_handle);
            }
            if (_storage != null) UnityEngine.Object.Destroy(_storage);
            Game = null; _storage = null;
        }
    }
}
