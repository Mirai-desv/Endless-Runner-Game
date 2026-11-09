using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Run.World
{
    /// <summary>Pool chunk: mỗi loại prefab có một kho riêng. Chunk không dùng nữa được tắt đi và cất lại, không Destroy.</summary>
    public class ChunkPool
    {
        private readonly Transform poolRoot;
        private readonly Dictionary<ChunkView, Stack<ChunkView>> free = new();
        private readonly Dictionary<ChunkView, ChunkView> prefabOf = new();

        public ChunkPool(Transform poolRoot)
        {
            this.poolRoot = poolRoot;
        }

        public void Prewarm(ChunkView prefab, int count)
        {
            for (int i = 0; i < count; i++)
                Release(Get(prefab, poolRoot));
        }

        public ChunkView Get(ChunkView prefab, Transform parent)
        {
            if (!free.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<ChunkView>();
                free[prefab] = stack;
            }

            ChunkView inst = null;
            while (stack.Count > 0 && inst == null)
                inst = stack.Pop();

            if (inst == null)
            {
                inst = Object.Instantiate(prefab, parent);
                inst.name = prefab.name;
                prefabOf[inst] = prefab;
            }

            inst.transform.SetParent(parent, false);
            inst.gameObject.SetActive(true);
            inst.ResetForReuse();
            return inst;
        }

        public void Release(ChunkView view)
        {
            if (view == null) return;

            if (!prefabOf.TryGetValue(view, out var prefab))
            {
                Object.Destroy(view.gameObject);
                return;
            }

            view.gameObject.SetActive(false);
            view.transform.SetParent(poolRoot, false);
            free[prefab].Push(view);
        }
    }
}