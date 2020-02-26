﻿// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth
{
    /// <summary>
    /// 仮想メッシュ頂点とパーティクルの連動ワーカー
    /// </summary>
    public class MeshParticleWorker : PhysicsManagerWorker
    {
        /// <summary>
        /// 仮想メッシュ頂点が対応するパーティクルインデックスマップ(0=なし)
        /// １頂点に対して複数のパーティクル連動あり。
        /// </summary>
        ExNativeMultiHashMap<int, int> vertexToParticleMap;

        /// <summary>
        /// パーティクル連動している頂点リスト
        /// </summary>
        FixedNativeListWithCount<int> vertexToParticleList;

        /// <summary>
        /// グループごとの作成データ管理
        /// </summary>
        struct CreateData
        {
            public int vertexIndex;
            public int particleIndex;
        }
        Dictionary<int, List<CreateData>> groupCreateDict = new Dictionary<int, List<CreateData>>();

        //=========================================================================================
        public override void Create()
        {
            vertexToParticleMap = new ExNativeMultiHashMap<int, int>();
            vertexToParticleList = new FixedNativeListWithCount<int>();
            vertexToParticleList.SetEmptyElement(-1);
        }

        public override void Release()
        {
            vertexToParticleMap.Dispose();
            vertexToParticleList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// パーティクル連動頂点登録
        /// </summary>
        /// <param name="vindex"></param>
        /// <param name="pindex"></param>
        public void Add(int group, int vindex, int pindex)
        {
            vertexToParticleMap.Add(vindex, pindex);
            vertexToParticleList.Add(vindex);

            if (groupCreateDict.ContainsKey(group) == false)
            {
                groupCreateDict.Add(group, new List<CreateData>());
            }
            groupCreateDict[group].Add(new CreateData() { vertexIndex = vindex, particleIndex = pindex });
        }

        /// <summary>
        /// パーティクル連動頂点解除（グループ単位）
        /// </summary>
        /// <param name="group"></param>
        public override void RemoveGroup(int group)
        {
            if (groupCreateDict.ContainsKey(group))
            {
                var clist = groupCreateDict[group];
                foreach (var cdata in clist)
                {
                    vertexToParticleMap.Remove(cdata.vertexIndex, cdata.particleIndex);
                    vertexToParticleList.Remove(cdata.vertexIndex);
                }
                groupCreateDict.Remove(group);
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
        }

        public override void CompleteWarmup()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// 仮想メッシュ頂点姿勢を連動パーティクルにコピーする(basePos, baseRot)
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            if (vertexToParticleList.Count == 0)
                return jobHandle;

            var job = new VertexToParticleJob()
            {
                vertexToParticleList = vertexToParticleList.ToJobArray(),
                vertexToParticleMap = vertexToParticleMap.Map,

                posList = Manager.Mesh.virtualPosList.ToJobArray(),
                rotList = Manager.Mesh.virtualRotList.ToJobArray(),

                basePosList = Manager.Particle.basePosList.ToJobArray(),
                baseRotList = Manager.Particle.baseRotList.ToJobArray(),
            };
            jobHandle = job.Schedule(vertexToParticleList.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        private struct VertexToParticleJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<int> vertexToParticleList;
            [ReadOnly]
            public NativeMultiHashMap<int, int> vertexToParticleMap;

            [ReadOnly]
            public NativeArray<float3> posList;
            [ReadOnly]
            public NativeArray<quaternion> rotList;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> basePosList;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> baseRotList;

            private NativeMultiHashMapIterator<int> iterator;

            // パーティクル連動頂点ごと
            public void Execute(int index)
            {
                int vindex = vertexToParticleList[index];
                if (vindex < 0)
                    return;

                int pindex;
                if (vertexToParticleMap.TryGetFirstValue(vindex, out pindex, out iterator))
                {
                    // 頂点の姿勢
                    var pos = posList[vindex];
                    var rot = rotList[vindex];

                    // 仮想メッシュは直接スキニングするので恐らく正規化は必要ない
                    //rot = math.normalize(rot); // 正規化しないとエラーになる場合がある

                    do
                    {
                        // base pos
                        basePosList[pindex] = pos;

                        // base rot
                        baseRotList[pindex] = rot;
                    }
                    while (vertexToParticleMap.TryGetNextValue(out pindex, ref iterator));
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// パーティクル姿勢を連動する仮想メッシュ頂点に書き戻す
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (vertexToParticleList.Count == 0)
                return jobHandle;

            var job = new ParticleToVertexJob()
            {
                vertexToParticleList = vertexToParticleList.ToJobArray(),
                vertexToParticleMap = vertexToParticleMap.Map,

                posList = Manager.Mesh.virtualPosList.ToJobArray(),
                rotList = Manager.Mesh.virtualRotList.ToJobArray(),

                particlePosList = Manager.Particle.posList.ToJobArray(),
                particleRotList = Manager.Particle.rotList.ToJobArray(),
            };
            jobHandle = job.Schedule(vertexToParticleList.Length, 64, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        private struct ParticleToVertexJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<int> vertexToParticleList;
            [ReadOnly]
            public NativeMultiHashMap<int, int> vertexToParticleMap;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> posList;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotList;

            [ReadOnly]
            public NativeArray<float3> particlePosList;
            [ReadOnly]
            public NativeArray<quaternion> particleRotList;

            private NativeMultiHashMapIterator<int> iterator;

            // パーティクル連動頂点ごと
            public void Execute(int index)
            {
                int vindex = vertexToParticleList[index];
                if (vindex < 0)
                    return;

                int pindex;
                if (vertexToParticleMap.TryGetFirstValue(vindex, out pindex, out iterator))
                {
                    float3 pos = 0;
                    float3 nor = 0;
                    float3 tan = 0;
                    int cnt = 0;
                    do
                    {
                        // particle
                        float3 ppos = particlePosList[pindex];
                        quaternion prot = particleRotList[pindex];

                        pos += ppos;
                        nor += math.mul(prot, new float3(0, 0, 1));
                        tan += math.mul(prot, new float3(0, 1, 0));
                        cnt++;
                    }
                    while (vertexToParticleMap.TryGetNextValue(out pindex, ref iterator));

                    if (cnt > 0)
                    {
                        pos = pos / cnt;
                        nor = math.normalize(nor);
                        tan = math.normalize(tan);

                        posList[vindex] = pos;
                        rotList[vindex] = quaternion.LookRotation(nor, tan);
                    }
                }
            }
        }
    }
}
