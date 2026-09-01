using System;
using System.Threading.Tasks;
using BAModAPI;
using Localizor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

[assembly: RegisterModClass(typeof(Capisoft.Lib.BaComputerGames.ComputerGamesLibraryMod))]

namespace Capisoft.Lib.BaComputerGames
{
    [ModEntryOnCityLoad]
    public sealed class ComputerGamesLibraryMod : IModBigAmbitions
    {
        private ComputerGamesDriver _driver;
        private ComputerGameScoresRuntime _scores;
        private ComputerGameProvider _provider;
        private IResourceLocator _locator;
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();
        public Task OnLoadAsync(ModContext context)
        {
            ComputerGamesDriver.ValidateContract();
            ComputerGames.Error += LogError;
            ComputerGames.Translator = Text;
            McgShortcuts.Initialize(context);
            ComputerGames.BeginDisplaySession = NativeScreenEffects.BeginSession;
            ComputerGames.InputAllowed = () => !UI.MiniMenu.MiniMenu.IsOpen &&
                !Scenes.MainMenu.Options.IsVisible && !GameManager.isCitySceneBeingUnloaded;
            _provider = new ComputerGameProvider(); _locator = _provider.CreateLocator();
            Addressables.ResourceManager.ResourceProviders.Add(_provider); Addressables.AddResourceLocator(_locator);
            _driver = new GameObject("BaComputerGames_Driver").AddComponent<ComputerGamesDriver>();
            _scores = _driver.gameObject.AddComponent<ComputerGameScoresRuntime>(); _scores.Initialize();
            ComputerGames.SessionAllowed = _driver.OwnsActiveSession;
            ComputerGames.ActivateHost();
            Debug.Log("[BaComputerGames] MCG " + ComputerGames.ApiVersion + " ready; no gameplay objects or asset bundles preloaded.");
            return Task.CompletedTask;
        }
        public Task OnUnloadAsync()
        {
            McgShortcuts.Shutdown();
            if (_scores != null) { _scores.Shutdown(); _scores = null; }
            if (_driver != null) { _driver.Shutdown(); UnityEngine.Object.Destroy(_driver.gameObject); _driver = null; }
            ComputerGames.DeactivateHost();
            if (_locator != null) Addressables.RemoveResourceLocator(_locator);
            if (_provider != null) Addressables.ResourceManager.ResourceProviders.Remove(_provider);
            _locator = null; _provider = null;
            ComputerGames.SessionAllowed = null; ComputerGames.InputAllowed = null; ComputerGames.Translator = null;
            ComputerGames.BeginDisplaySession = null;
            ComputerGames.Error -= LogError;
            return Task.CompletedTask;
        }
        private static void LogError(Exception error) { Debug.LogException(error); }
        internal static string Text(string key, string fallback)
        {
            try { var value = key.GetLocalization(); return string.IsNullOrEmpty(value) || value == key ? fallback : value; }
            catch { return fallback; }
        }
    }
}
