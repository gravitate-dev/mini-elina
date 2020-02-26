// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
namespace MagicaCloth
{
    /// <summary>
    /// FixedChunkNativeListで使用するチャンク情報
    /// </summary>
    public struct ChunkData
    {
        public int chunkNo;

        /// <summary>
        /// データ配列のこのチャンクの開始インデックス
        /// </summary>
        public int startIndex;

        /// <summary>
        /// データ数
        /// </summary>
        public int dataLength;

        public void Clear()
        {
            chunkNo = 0;
            startIndex = 0;
            dataLength = 0;
        }

        public override string ToString()
        {
            string str = string.Empty;
            str += "[chunkNo=" + chunkNo + ",startIndex=" + startIndex + ",dataLength=" + dataLength + "\n";
            return str;
        }
    }
}
