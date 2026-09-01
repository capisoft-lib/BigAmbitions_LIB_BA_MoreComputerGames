using System;
using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    // Compatibility helper for consumers loaded from the same resolution context. Separate Workshop
    // mods should keep the RegisterModClass target BAModAPI-only and call ComputerGames.Register from
    // OnLoadAsync; Mono may resolve a registered base type before the dependency assembly is bound.
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
