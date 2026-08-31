using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    public sealed class AssetBundleGameAssets : ComputerGameAssets
    {
        public AssetBundle Bundle { get; private set; }
        private Action _release;
        internal AssetBundleGameAssets(AssetBundle bundle, Action release) { Bundle = bundle; _release = release; }
        public override void Dispose()
        {
            var release = _release; _release = null;
            try { if (Bundle != null) Bundle.Unload(true); }
            finally { Bundle = null; release?.Invoke(); }
        }
    }

    public sealed class AssetBundleGameLoader : IComputerGameLoader
    {
        private readonly string _relativePath;
        private readonly SemaphoreSlim _lease = new SemaphoreSlim(1, 1);
        public AssetBundleGameLoader(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new ArgumentException("Use a path relative to your mod package.", nameof(relativePath));
            _relativePath = relativePath;
        }
        public async Task<ComputerGameAssets> LoadAsync(ComputerGameLoadContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string root = Path.GetFullPath(context.ModRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string path = Path.GetFullPath(Path.Combine(root, _relativePath));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Bundle path escapes the mod package.");
            // Unity cannot load the same bundle twice. A cancelled selection may still be finishing disk I/O.
            await _lease.WaitAsync(cancellationToken);
            bool transferred = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = AssetBundle.LoadFromFileAsync(path);
                if (!request.isDone)
                {
                    var completed = new TaskCompletionSource<bool>();
                    request.completed += _ => completed.TrySetResult(true);
                    await completed.Task;
                }
                var bundle = request.assetBundle;
                if (cancellationToken.IsCancellationRequested)
                {
                    if (bundle != null) bundle.Unload(true);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (bundle == null) throw new IOException("Cannot load game AssetBundle: " + _relativePath);
                var assets = new AssetBundleGameAssets(bundle, () => _lease.Release());
                transferred = true;
                return assets;
            }
            finally { if (!transferred) _lease.Release(); }
        }
    }
}
