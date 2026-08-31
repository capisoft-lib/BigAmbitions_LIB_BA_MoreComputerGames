using System;
using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    // The concrete mod still declares RegisterModClass + ModEntryOnCityLoad, per the official SDK.
    public abstract class ComputerGameMod<TGame> : IModBigAmbitions where TGame : MonoBehaviour, IComputerGame
    {
        private ComputerGameRegistration _registration;
        protected abstract ComputerGameDefinition Definition { get; }
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();
        public Task OnLoadAsync(ModContext context)
        {
            _registration?.Dispose(); _registration = null;
            _registration = ComputerGames.Register(context.ModId, context.ModRootPath, Definition);
            return Task.CompletedTask;
        }
        public Task OnUnloadAsync() { _registration?.Dispose(); _registration = null; return Task.CompletedTask; }
    }
}
