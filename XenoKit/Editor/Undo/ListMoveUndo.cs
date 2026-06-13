using System.Collections.Generic;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor.Undo
{
    internal sealed class ListMoveUndo<T> : IUndoRedo
    {
        private readonly IList<T> list;
        private readonly int oldIndex;
        private readonly int newIndex;

        public bool doLast { get; set; } = true;
        public string Message { get; set; }

        public ListMoveUndo(IList<T> list, int oldIndex, int newIndex, string message)
        {
            this.list = list;
            this.oldIndex = oldIndex;
            this.newIndex = newIndex;
            Message = message;
            Redo();
        }

        public void Undo()
        {
            Move(newIndex, oldIndex);
        }

        public void Redo()
        {
            Move(oldIndex, newIndex);
        }

        private void Move(int from, int to)
        {
            if (list == null || from < 0 || to < 0 || from >= list.Count || to >= list.Count)
                return;

            T item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }
    }
}
