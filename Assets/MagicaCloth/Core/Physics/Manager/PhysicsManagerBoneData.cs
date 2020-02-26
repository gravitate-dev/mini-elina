// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンデータ
    /// </summary>
    public class PhysicsManagerBoneData : PhysicsManagerAccess
    {
        //=========================================================================================
        /// <summary>
        /// 管理ボーンリスト
        /// </summary>
        public FixedTransformAccessArray boneList;

        /// <summary>
        /// ボーンワールド位置リスト
        /// </summary>
        public FixedNativeList<float3> bonePosList;

        /// <summary>
        /// ボーンワールド回転リスト
        /// </summary>
        public FixedNativeList<quaternion> boneRotList;

        /// <summary>
        /// ボーンワールドスケールリスト（現在は初期化時に設定のみ不変）
        /// </summary>
        public FixedNativeList<float3> boneSclList;

        //=========================================================================================
        // ここは復元ボーンごと
        // フラグ
        //public const byte RestoreBone_Flag_RestoreLocalPos = 0x01;
        //public const byte RestoreBone_Flag_RestoreLocalRot = 0x02;

        /// <summary>
        /// 復元ボーンリスト
        /// </summary>
        public FixedTransformAccessArray restoreBoneList;

        /// <summary>
        /// 復元ボーンの復元ローカル座標リスト
        /// </summary>
        public FixedNativeList<float3> restoreBoneLocalPosList;

        /// <summary>
        /// 復元ボーンの復元ローカル回転リスト
        /// </summary>
        public FixedNativeList<quaternion> restoreBoneLocalRotList;

        /// <summary>
        /// 復元ボーンのフラグリスト
        /// </summary>
        //public FixedNativeList<byte> restoreBoneFlagList;

        //=========================================================================================
        // ここはライトボーンごと
        /// <summary>
        /// 書き込みボーンリスト
        /// </summary>
        public FixedTransformAccessArray writeBoneList;

        /// <summary>
        /// 書き込みボーンの参照ボーン姿勢インデックス
        /// </summary>
        public FixedNativeList<int> writeBoneIndexList;

        /// <summary>
        /// 書き込みボーンの対応するパーティクルインデックス
        /// </summary>
        public ExNativeMultiHashMap<int, int> writeBoneParticleIndexMap;

        /// <summary>
        /// 読み込みボーンに対応する書き込みボーンのインデックス辞書
        /// </summary>
        Dictionary<int, int> boneToWriteIndexDict = new Dictionary<int, int>();

        //=========================================================================================
        /// <summary>
        /// ボーンリストに変化が合った場合にtrue
        /// </summary>
        public bool hasBoneChanged { get; private set; }

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            boneList = new FixedTransformAccessArray();
            bonePosList = new FixedNativeList<float3>();
            boneRotList = new FixedNativeList<quaternion>();
            boneSclList = new FixedNativeList<float3>();

            restoreBoneList = new FixedTransformAccessArray();
            restoreBoneLocalPosList = new FixedNativeList<float3>();
            restoreBoneLocalRotList = new FixedNativeList<quaternion>();
            //restoreBoneFlagList = new FixedNativeList<byte>();

            writeBoneList = new FixedTransformAccessArray();
            writeBoneIndexList = new FixedNativeList<int>();
            writeBoneParticleIndexMap = new ExNativeMultiHashMap<int, int>();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (boneList == null)
                return;

            boneList.Dispose();
            bonePosList.Dispose();
            boneRotList.Dispose();
            boneSclList.Dispose();

            restoreBoneList.Dispose();
            restoreBoneLocalPosList.Dispose();
            restoreBoneLocalRotList.Dispose();
            //restoreBoneFlagList.Dispose();

            writeBoneList.Dispose();
            writeBoneIndexList.Dispose();
            writeBoneParticleIndexMap.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// 復元ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="lpos"></param>
        /// <param name="lrot"></param>
        /// <returns></returns>
        //public int AddRestoreBone(Transform target, float3 lpos, quaternion lrot, byte flag)
        public int AddRestoreBone(Transform target, float3 lpos, quaternion lrot)
        {
            int restoreBoneIndex;
            if (restoreBoneList.Exist(target))
            {
                // 参照カウンタ＋
                restoreBoneIndex = restoreBoneList.Add(target);
            }
            else
            {
                // 復元ローカル姿勢も登録
                restoreBoneIndex = restoreBoneList.Add(target);
                restoreBoneLocalPosList.Add(lpos);
                restoreBoneLocalRotList.Add(lrot);
                //restoreBoneFlagList.Add(flag);
                hasBoneChanged = true;
            }

            return restoreBoneIndex;
        }

        /// <summary>
        /// 復元ボーン削除
        /// </summary>
        /// <param name="restoreBoneIndex"></param>
        public void RemoveRestoreBone(int restoreBoneIndex)
        {
            restoreBoneList.Remove(restoreBoneIndex);
            if (restoreBoneList.Exist(restoreBoneIndex) == false)
            {
                // データも削除
                restoreBoneLocalPosList.Remove(restoreBoneIndex);
                restoreBoneLocalRotList.Remove(restoreBoneIndex);
                //restoreBoneFlagList.Remove(restoreBoneIndex);
                hasBoneChanged = true;
            }
        }

        /// <summary>
        /// ボーンの復元カウントを返す
        /// </summary>
        public int RestoreBoneCount
        {
            get
            {
                return restoreBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 利用ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pindex"></param>
        /// <returns></returns>
        public int AddBone(Transform target, int pindex = -1)
        {
            int boneIndex;
            if (boneList.Exist(target))
            {
                // 参照カウンタ＋
                boneIndex = boneList.Add(target);
            }
            else
            {
                // 新規
                boneIndex = boneList.Add(target);
                bonePosList.Add(float3.zero);
                boneRotList.Add(quaternion.identity);
                boneSclList.Add(target.lossyScale);
                hasBoneChanged = true;
            }

            // 書き込み設定
            if (pindex >= 0)
            {
                if (writeBoneList.Exist(target))
                {
                    // 参照カウンタ＋
                    writeBoneList.Add(target);
                }
                else
                {
                    // 新規
                    writeBoneList.Add(target);
                    writeBoneIndexList.Add(boneIndex);
                    hasBoneChanged = true;
                }
                int writeIndex = writeBoneList.GetIndex(target);

                boneToWriteIndexDict.Add(boneIndex, writeIndex);

                // 書き込み姿勢参照パーティクルインデックス登録
                writeBoneParticleIndexMap.Add(writeIndex, pindex);
            }

            return boneIndex;
        }

        /// <summary>
        /// 利用ボーン解除
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <param name="pindex"></param>
        /// <returns></returns>
        public bool RemoveBone(int boneIndex, int pindex = -1)
        {
            bool del = false;
            boneList.Remove(boneIndex);
            if (boneList.Exist(boneIndex) == false)
            {
                // データも削除
                bonePosList.Remove(boneIndex);
                boneRotList.Remove(boneIndex);
                boneSclList.Remove(boneIndex);
                hasBoneChanged = true;
                del = true;
            }

            // 書き込み設定から削除
            if (pindex >= 0)
            {
                int writeIndex = boneToWriteIndexDict[boneIndex];

                writeBoneList.Remove(writeIndex);
                writeBoneIndexList.Remove(writeIndex);
                writeBoneParticleIndexMap.Remove(writeIndex, pindex);
                hasBoneChanged = true;

                if (writeBoneList.Exist(writeIndex) == false)
                {
                    boneToWriteIndexDict.Remove(boneIndex);
                }
            }

            return del;
        }

        /// <summary>
        /// 読み込みボーン数を返す
        /// </summary>
        public int ReadBoneCount
        {
            get
            {
                return boneList.Count;
            }
        }

        /// <summary>
        /// 書き込みボーン数を返す
        /// </summary>
        public int WriteBoneCount
        {
            get
            {
                return writeBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報のリセット
        /// </summary>
        public void ResetBoneFromTransform()
        {
            // ボーン姿勢リセット
            if (RestoreBoneCount > 0)
            {
                var job = new RestoreBoneJob()
                {
                    localPosList = restoreBoneLocalPosList.ToJobArray(),
                    localRotList = restoreBoneLocalRotList.ToJobArray(),
                    //flagList = restoreBoneFlagList.ToJobArray()
                };
                Compute.MasterJob = job.Schedule(restoreBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢の復元
        /// </summary>
        [BurstCompile]
        struct RestoreBoneJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<float3> localPosList;
            [ReadOnly]
            public NativeArray<quaternion> localRotList;
            //[ReadOnly]
            //public NativeArray<byte> flagList;

            // 復元ボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                //byte flag = flagList[index];

                //if ((flag & RestoreBone_Flag_RestoreLocalPos) != 0)
                transform.localPosition = localPosList[index];

                //if ((flag & RestoreBone_Flag_RestoreLocalRot) != 0)
                transform.localRotation = localRotList[index];
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報の読み込み
        /// </summary>
        public void ReadBoneFromTransform()
        {
            // ボーン姿勢読み込み
            if (ReadBoneCount > 0)
            {
                // ボーンから姿勢読み込み（ルートが別れていないとジョブが並列化できないので注意！）
                var job3 = new ReadBoneJob()
                {
                    bonePosList = bonePosList.ToJobArray(),
                    boneRotList = boneRotList.ToJobArray(),
                };
                Compute.MasterJob = job3.Schedule(boneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢の読込み
        /// </summary>
        [BurstCompile]
        struct ReadBoneJob : IJobParallelForTransform
        {
            [WriteOnly]
            public NativeArray<float3> bonePosList;
            [WriteOnly]
            public NativeArray<quaternion> boneRotList;

            // 読み込みボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                boneRotList[index] = transform.rotation;
                bonePosList[index] = transform.position;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        public void WriteBoneToTransform()
        {
            if (WriteBoneCount > 0)
            {
                var job = new WriteBontToTransformJob()
                {
                    writeBoneIndexList = writeBoneIndexList.ToJobArray(),
                    bonePosList = bonePosList.ToJobArray(),
                    boneRotList = boneRotList.ToJobArray()
                };
                Compute.MasterJob = job.Schedule(writeBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        [BurstCompile]
        struct WriteBontToTransformJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<int> writeBoneIndexList;
            [ReadOnly]
            public NativeArray<float3> bonePosList;
            [ReadOnly]
            public NativeArray<quaternion> boneRotList;

            // 書き込みトランスフォームごと
            public void Execute(int index, TransformAccess transform)
            {
                int bindex = writeBoneIndexList[index];
                transform.position = bonePosList[bindex];
                transform.rotation = boneRotList[bindex];
            }
        }
    }
}
