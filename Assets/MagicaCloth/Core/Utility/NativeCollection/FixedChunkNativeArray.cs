// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// かたまり（チャンク）ごとに確保した固定インデックスNativeList
    /// 一度確保したインデックスはズレない（ここ重要）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedChunkNativeArray<T> : IDisposable where T : struct
    {
        /// <summary>
        /// ネイティブリスト
        /// </summary>
        NativeArray<T> nativeArray;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        List<ChunkData> emptyChunkList = new List<ChunkData>();

        /// <summary>
        /// 使用インデックスセット
        /// </summary>
        List<ChunkData> useChunkList = new List<ChunkData>();

        int chunkSeed;

        int initLength = 256;

        T emptyElement;

        int useLength;

        //=========================================================================================
        public FixedChunkNativeArray()
        {
            nativeArray = new NativeArray<T>(initLength, Allocator.Persistent);
            nativeLength = nativeArray.Length;
            useLength = 0;
        }

        public FixedChunkNativeArray(int length)
        {
            initLength = length;
            nativeArray = new NativeArray<T>(initLength, Allocator.Persistent);
            nativeLength = nativeArray.Length;
            useLength = 0;
        }

        public void Dispose()
        {
            if (nativeArray.IsCreated)
            {
                nativeArray.Dispose();
            }
            nativeLength = 0;
            useLength = 0;
            emptyChunkList.Clear();
            useChunkList.Clear();
        }

        public void SetEmptyElement(T empty)
        {
            emptyElement = empty;
        }

        //=========================================================================================
        /// <summary>
        /// データチャンクの追加
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ChunkData AddChunk(int length)
        {
            // 再利用チェック
            for (int i = 0; i < emptyChunkList.Count; i++)
            {
                var cdata = emptyChunkList[i];
                if (cdata.dataLength >= length)
                {
                    // このチャンクを再利用する
                    int remainder = cdata.dataLength - length;
                    if (remainder > 0)
                    {
                        // 分割
                        var rchunk = new ChunkData()
                        {
                            chunkNo = ++chunkSeed,
                            startIndex = cdata.startIndex + length,
                            dataLength = remainder,
                        };
                        emptyChunkList[i] = rchunk;
                    }
                    else
                    {
                        emptyChunkList.RemoveAt(i);
                    }
                    cdata.dataLength = length;

                    // 使用リストに追加
                    useChunkList.Add(cdata);

                    return cdata;
                }
            }

            // 新規追加
            var data = new ChunkData()
            {
                chunkNo = ++chunkSeed,
                startIndex = useLength,
                dataLength = length,
            };
            useChunkList.Add(data);
            useLength += length;

            if (nativeArray.Length < useLength)
            {
                // 拡張
                int len = nativeArray.Length;
                while (len < useLength)
                    len += len;
                //len += Mathf.Min(len, 65536);
                var nativeArray2 = new NativeArray<T>(len, Allocator.Persistent);
                nativeArray2.CopyFromFast(nativeArray);
                nativeArray.Dispose();

                nativeArray = nativeArray2;
                nativeLength = nativeArray.Length;
            }

            return data;
        }

        /// <summary>
        /// サイズ１のチャンクを追加しデータを設定する
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public ChunkData Add(T data)
        {
            var c = AddChunk(1);
            nativeArray[c.startIndex] = data;
            return c;
        }

        /// <summary>
        /// データチャンクの削除
        /// </summary>
        /// <param name="chunkNo"></param>
        public void RemoveChunk(int chunkNo)
        {
            for (int i = 0; i < useChunkList.Count; i++)
            {
                if (useChunkList[i].chunkNo == chunkNo)
                {
                    // このチャンクを削除する
                    var cdata = useChunkList[i];
                    useChunkList.RemoveAt(i);

                    // データクリア
                    //for (int index = 0; index < cdata.dataLength; index++)
                    //    nativeArray[cdata.startIndex + index] = emptyElement;
                    nativeArray.SetValue(cdata.startIndex, cdata.dataLength, emptyElement);

                    // 前後の未使用チャンクと接続できるなら結合する
                    for (int j = 0; j < emptyChunkList.Count;)
                    {
                        var edata = emptyChunkList[j];
                        if ((edata.startIndex + edata.dataLength) == cdata.startIndex)
                        {
                            // 結合
                            edata.dataLength += cdata.dataLength;
                            cdata = edata;
                            emptyChunkList.RemoveAt(j);
                            continue;
                        }
                        else if (edata.startIndex == (cdata.startIndex + cdata.dataLength))
                        {
                            // 結合
                            cdata.dataLength += edata.dataLength;
                            emptyChunkList.RemoveAt(j);
                            continue;
                        }

                        j++;
                    }

                    // チャンクを空リストに追加
                    emptyChunkList.Add(cdata);

                    return;
                }
            }
        }

        /// <summary>
        /// データチャンクの削除
        /// </summary>
        /// <param name="chunk"></param>
        public void RemoveChunk(ChunkData chunk)
        {
            RemoveChunk(chunk.chunkNo);
        }

        /// <summary>
        /// 指定データで埋める
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="data"></param>
        public void Fill(ChunkData chunk, T data)
        {
            int end = chunk.startIndex + chunk.dataLength;
            for (int i = chunk.startIndex; i < end; i++)
            {
                nativeArray[i] = data;
            }
        }

        /// <summary>
        /// 確保されているネイティブ配列の要素数を返す
        /// </summary>
        public int Length
        {
            get
            {
                return nativeLength;
            }
        }

        /// <summary>
        /// 実際に利用されているチャンク数を返す
        /// </summary>
        public int ChunkCount
        {
            get
            {
                return useChunkList.Count;
            }
        }

        /// <summary>
        /// 実際に使用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                int cnt = 0;
                foreach (var c in useChunkList)
                    cnt += c.dataLength;
                return cnt;
            }
        }

        public T this[int index]
        {
            get
            {
                return nativeArray[index];
            }
            set
            {
                nativeArray[index] = value;
            }
        }

        public void Clear()
        {
            nativeArray.Dispose();
            nativeArray = new NativeArray<T>(initLength, Allocator.Persistent);
            nativeLength = initLength;
            useLength = 0;
            emptyChunkList.Clear();
            useChunkList.Clear();
        }

        public T[] ToArray()
        {
            return nativeArray.ToArray();
        }

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> ToJobArray()
        {
            return nativeArray;
        }

        //=========================================================================================
        public override string ToString()
        {
            string str = string.Empty;

            str += "nativeList length=" + Length + "\n";
            str += "use chunk count=" + ChunkCount + "\n";
            str += "empty chunk count=" + emptyChunkList.Count + "\n";

            str += "<< use chunks >>\n";
            foreach (var cdata in useChunkList)
            {
                str += cdata;
            }

            str += "<< empty chunks >>\n";
            foreach (var cdata in emptyChunkList)
            {
                str += cdata;
            }

            return str;
        }
    }
}
