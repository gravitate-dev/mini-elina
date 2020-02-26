// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp

using System.Text;

namespace MagicaCloth
{
    public static partial class Define
    {
        /// <summary>
        /// エラーコード
        /// </summary>
        public enum Error
        {
            None = 0, // なし

            InvalidDataHash = 100,
            DataVersionMismatch = 101,

            MeshDataNull = 200,
            MeshDataHashMismatch = 201,
            MeshDataVersionMismatch = 202,

            ClothDataNull = 300,
            ClothDataHashMismatch = 301,
            ClothDataVersionMismatch = 302,

            ClothSelectionHashMismatch = 400,
            ClothSelectionVersionMismatch = 401,

            ClothTargetRootCountMismatch = 500,

            UseTransformNull = 600,
            UseTransformCountZero = 601,
            UseTransformCountMismatch = 602,

            DeformerNull = 700,
            DeformerHashMismatch = 701,
            DeformerVersionMismatch = 702,
            DeformerCountZero = 703,
            DeformerCountMismatch = 704,

            VertexCountZero = 800,
            VertexUseCountZero = 801,
            VertexCountMismatch = 802,

            RootListCountMismatch = 900,

            SelectionDataCountMismatch = 1000,
            SelectionCountZero = 1001,

            CenterTransformNull = 1100,

            SpringDataNull = 1200,
            SpringDataHashMismatch = 1201,
            SpringDataVersionMismatch = 1202,

            TargetObjectNull = 1300,

            SharedMeshNull = 1400,
            SharedMeshCannotRead = 1401,

            MeshOptimizeMismatch = 1500,

            BoneListZero = 1600,
            BoneListNull = 1601,

            // ここからはランタイムエラー(10000～)
        }

        /// <summary>
        /// エラーメッセージを取得する
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static string GetErrorMessage(Error err)
        {
            StringBuilder sb = new StringBuilder(512);

            // 基本エラーコード
            sb.AppendFormat("Error ({0}) : {1}", (int)err, err.ToString());

            // 個別の詳細メッセージ
            switch (err)
            {
                case Error.SharedMeshCannotRead:
                    sb.AppendLine();
                    sb.Append("Please turn On the [Read/Write Enabled] flag of the mesh importer.");
                    break;
            }

            return sb.ToString();

        }
    }
}
