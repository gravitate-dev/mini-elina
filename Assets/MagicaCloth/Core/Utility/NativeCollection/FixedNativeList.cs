// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// 固定インデックスNativeList
    /// 一度確保したインデックスはズレない（ここ重要）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedNativeList<T> : IDisposable where T : struct
    {
        /// <summary>
        /// ネイティブリスト
        /// </summary>
        NativeList<T> nativeList;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        Queue<int> emptyStack = new Queue<int>();

        /// <summary>
        /// 使用インデックスセット
        /// </summary>
        HashSet<int> useIndexSet = new HashSet<int>();

        //=========================================================================================
        public FixedNativeList()
        {
            nativeList = new NativeList<T>(Allocator.Persistent);
            nativeLength = nativeList.Length;
        }

        public FixedNativeList(int capacity)
        {
            nativeList = new NativeList<T>(capacity, Allocator.Persistent);
            nativeLength = nativeList.Length;
        }

        public FixedNativeList(int capacity, T fill)
        {
            nativeList = new NativeList<T>(capacity, Allocator.Persistent);
            for (int i = 0; i < capacity; i++)
            {
                Add(fill);
            }
            nativeLength = nativeList.Length;
        }

        public void Dispose()
        {
            if (nativeList.IsCreated)
            {
                nativeList.Dispose();
            }
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// データ追加
        /// 追加したインデックスを返す
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public int Add(T element)
        {
            int index = 0;

            if (emptyStack.Count > 0)
            {
                // 再利用
                index = emptyStack.Dequeue();
                nativeList[index] = element;
            }
            else
            {
                // 新規
                index = nativeList.Length;
                nativeList.Add(element);
                nativeLength = nativeList.Length;
            }
            useIndexSet.Add(index);

            return index;
        }

        /// <summary>
        /// データ削除
        /// 削除されたインデックスは再利用される
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            if (useIndexSet.Contains(index))
            {
                // 削除データはデフォルト値で埋める
                nativeList[index] = default(T);

                emptyStack.Enqueue(index);
                useIndexSet.Remove(index);
            }
        }

        public bool Exists(int index)
        {
            return useIndexSet.Contains(index);
        }

        /// <summary>
        /// 確保されているネイティブ配列の要素数を返す
        /// </summary>
        public int Length
        {
            get
            {
                //return nativeList.Length;
                return nativeLength;
            }
        }

        /// <summary>
        /// 実際に利用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                return useIndexSet.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return nativeList[index];
            }
            set
            {
                nativeList[index] = value;
            }
        }

        public void Clear()
        {
            nativeList.Clear();
            nativeLength = 0;
            emptyStack.Clear();
            useIndexSet.Clear();
        }

        public T[] ToArray()
        {
            return nativeList.ToArray();
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> ToJobArray()
        {
            return nativeList.AsArray();
        }
    }
}
