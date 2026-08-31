using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Capisoft.Lib.BaComputerGames
{
    public sealed class ComputerGameDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Version { get; }
        public string TitleKey { get; }
        public string DescriptionKey { get; }
        public string Ruleset { get; }
        public bool UseNativeRetroEffects { get; private set; } = true;
        public IComputerGameLoader Loader { get; }
        internal Func<GameObject, IComputerGame> Factory { get; }

        public ComputerGameDefinition(string id, string title, string description, string version,
            Func<GameObject, IComputerGame> factory, IComputerGameLoader loader = null,
            string titleKey = null, string descriptionKey = null, string ruleset = "default-v1")
        {
            if (!ValidId(id)) throw new ArgumentException("Use a lowercase namespaced id, e.g. author:flappy-ambition.", nameof(id));
            if (string.IsNullOrWhiteSpace(title) || title.Length > 100) throw new ArgumentException("Title must contain 1-100 characters.", nameof(title));
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32) throw new ArgumentException("A short game version is required.", nameof(version));
            if (string.IsNullOrWhiteSpace(ruleset) || ruleset.Length > 64) throw new ArgumentException("A short ruleset id is required.", nameof(ruleset));
            Id = id; Title = title; Description = description ?? ""; Version = version;
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Loader = loader; TitleKey = titleKey; DescriptionKey = descriptionKey; Ruleset = ruleset;
        }

        public static ComputerGameDefinition Create<TGame>(string id, string title, string description,
            string version = "0.1.0", IComputerGameLoader loader = null, string titleKey = null,
            string descriptionKey = null, string ruleset = "default-v1") where TGame : MonoBehaviour, IComputerGame =>
            new ComputerGameDefinition(id, title, description, version, root => root.AddComponent<TGame>(), loader, titleKey, descriptionKey, ruleset);

        // Return a new descriptor; never mutate an already registered game's metadata.
        public ComputerGameDefinition WithNativeRetroEffects(bool enabled) =>
            new ComputerGameDefinition(Id, Title, Description, Version, Factory, Loader, TitleKey, DescriptionKey, Ruleset)
                { UseNativeRetroEffects = enabled };

        private static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 96) return false;
            int colon = id.IndexOf(':');
            if (colon <= 0 || colon == id.Length - 1 || colon != id.LastIndexOf(':')) return false;
            for (int i = 0; i < id.Length; i++)
                if (id[i] != ':' && id[i] != '-' && id[i] != '_' && id[i] != '.' &&
                    !(id[i] >= 'a' && id[i] <= 'z') && !(id[i] >= '0' && id[i] <= '9')) return false;
            return true;
        }
    }

    public interface IComputerGameLoader
    {
        // Called on Unity's main thread only after selection. Keep Unity continuations on that thread.
        Task<ComputerGameAssets> LoadAsync(ComputerGameLoadContext context, CancellationToken cancellationToken);
    }

    public abstract class ComputerGameAssets : IDisposable
    {
        public abstract void Dispose();
    }

    public sealed class ComputerGameLoadContext
    {
        public string GameId { get; }
        public string ModRootPath { get; }
        internal ComputerGameLoadContext(string gameId, string modRootPath) { GameId = gameId; ModRootPath = modRootPath; }
    }
}
