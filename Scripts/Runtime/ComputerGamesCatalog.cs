using System;
using System.Collections.Generic;

namespace Capisoft.Lib.BaComputerGames
{
    // Metadata only; the launcher itself is not a catalog entry.
    internal sealed class ComputerGamesCatalog
    {
        private readonly List<ComputerGameDefinition> _games = new List<ComputerGameDefinition>();
        internal int SelectedIndex { get; private set; }
        internal int Count => _games.Count;
        internal ComputerGameDefinition Selected => _games[SelectedIndex];
        internal ComputerGameDefinition this[int index] => _games[index];
        internal void Refresh()
        {
            string selected = _games.Count == 0 ? null : Selected.Id;
            _games.Clear();
            _games.Add(new ComputerGameDefinition(ComputerGames.VanillaBrickBreakerId, "Brick Breaker",
                "Included with Big Ambitions.", "1.0", _ => throw new NotSupportedException(),
                descriptionKey: "bacg_original_desc", ruleset: ComputerGames.VanillaBrickBreakerRuleset));
            _games.AddRange(ComputerGames.Catalog);
            SelectedIndex = Math.Max(0, _games.FindIndex(game => game.Id == selected));
        }
        internal void Move(int direction)
        { if (Count != 0) SelectedIndex = (SelectedIndex + Math.Sign(direction) + Count) % Count; }
    }
}
