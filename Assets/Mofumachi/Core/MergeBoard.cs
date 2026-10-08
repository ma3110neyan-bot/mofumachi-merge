using System.Collections.Generic;
using System.Linq;

namespace Mofumachi.Core
{
    public sealed class MergeBoard
    {
        private readonly GameState state;
        private HashSet<int> locked => state.MergeLocks;
        public MergeBoard(GameState state) { this.state = state; }
        public BoardItem At(int cell) => state.mergeBoard.FirstOrDefault(i => i.cellIndex == cell);
        private static bool Valid(int cell) => cell >= 0 && cell < GameState.Columns * GameState.Rows;
        public bool IsLocked(int cell) => locked.Contains(cell);
        public void ReleaseLocks() => locked.Clear();
        public bool AddItem(string itemId, int level)
        {
            if (string.IsNullOrWhiteSpace(itemId) || level < 1 || level > GameState.MaxLevel) return false;
            for (int cell = 0; cell < GameState.Columns * GameState.Rows; cell++)
                if (At(cell) == null && !IsLocked(cell)) { state.mergeBoard.Add(new BoardItem(itemId, level, cell)); return true; }
            return false;
        }
        public bool TryMove(int from, int to)
        {
            if (!Valid(from) || !Valid(to) || from == to || IsLocked(from) || IsLocked(to) || At(to) != null) return false;
            var item = At(from); if (item == null) return false;
            item.cellIndex = to; return true;
        }
        public bool TryMerge(int from, int to)
        {
            if (!Valid(from) || !Valid(to) || from == to || IsLocked(from) || IsLocked(to)) return false;
            var source = At(from); var target = At(to);
            if (source == null || target == null || source.itemId != target.itemId || source.level != target.level || source.level >= GameState.MaxLevel) return false;
            locked.Add(from); locked.Add(to);
            state.mergeBoard.Remove(source); target.level++;
            return true;
        }
    }
}
