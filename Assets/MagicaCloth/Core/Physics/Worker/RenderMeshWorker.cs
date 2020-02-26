// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// レンダーメッシュワーカー
    /// メッシュの利用頂点のワールド姿勢を求める／書き戻す
    /// </summary>
    public class RenderMeshWorker : PhysicsManagerWorker
    {
        private JobHandle warmupJobHandle;

        private bool isUpdateUseFlag;

        //=========================================================================================
        public override void Create()
        {
        }

        public override void Release()
        {
        }

        public override void RemoveGroup(int group)
        {
        }

        public void SetUpdateUseFlag()
        {
            isUpdateUseFlag = true;
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームリード中に実行する処理
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public override void Warmup()
        {
            if (isUpdateUseFlag == false)
                return;

            if (Manager.Mesh.RenderMeshUseCount == 0)
                return;

            // レンダーメッシュの利用頂点計算
            var job = new CalcUseFlagJob()
            {
                renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),
                sharedRenderMeshInfoList = Manager.Mesh.sharedRenderMeshInfoList.ToJobArray(),

                renderToChildMeshIndexMap = Manager.Mesh.renderToChildMeshIndexMap.Map,
                virtualVertexInfoList = Manager.Mesh.virtualVertexInfoList.ToJobArray(),

                sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
                sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

                sharedRenderVertices = Manager.Mesh.sharedRenderVertices.ToJobArray(),
                sharedRenderNormals = Manager.Mesh.sharedRenderNormals.ToJobArray(),
                sharedRenderTangents = Manager.Mesh.sharedRenderTangents.ToJobArray(),
#if !UNITY_2018
                sharedBonesPerVertexList = Manager.Mesh.sharedBonesPerVertexList.ToJobArray(),
                sharedBonesPerVertexStartList = Manager.Mesh.sharedBonesPerVertexStartList.ToJobArray(),
#endif
                sharedBoneWeightList = Manager.Mesh.sharedBoneWeightList.ToJobArray(),

                renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
                renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
                renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
                renderBoneWeightList = Manager.Mesh.renderBoneWeightList.ToJobArray(),

                renderVertexFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),
            };
            warmupJobHandle = job.Schedule(Manager.Mesh.renderPosList.Length, 128);

            isUpdateUseFlag = false;
        }

        public override void CompleteWarmup()
        {
            warmupJobHandle.Complete();
        }

        /// <summary>
        /// レンダーメッシュの利用頂点状況をリンクする仮想頂点から算出する
        /// </summary>
        [BurstCompile]
        private struct CalcUseFlagJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;
            [ReadOnly]
            public NativeArray<PhysicsManagerMeshData.SharedRenderMeshInfo> sharedRenderMeshInfoList;

            [ReadOnly]
            public NativeMultiHashMap<int, int4> renderToChildMeshIndexMap;

            [ReadOnly]
            public NativeArray<uint> virtualVertexInfoList;

            [ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [ReadOnly]
            public NativeArray<float3> sharedRenderVertices;
            [ReadOnly]
            public NativeArray<float3> sharedRenderNormals;
            [ReadOnly]
            public NativeArray<float4> sharedRenderTangents;
#if UNITY_2018
            [ReadOnly]
            public NativeArray<BoneWeight> sharedBoneWeightList;
#else
            [ReadOnly]
            public NativeArray<byte> sharedBonesPerVertexList;
            [ReadOnly]
            public NativeArray<int> sharedBonesPerVertexStartList;
            [ReadOnly]
            public NativeArray<BoneWeight1> sharedBoneWeightList;
#endif

            [WriteOnly]
            public NativeArray<float3> renderPosList;
            [WriteOnly]
            public NativeArray<float3> renderNormalList;
            [WriteOnly]
            public NativeArray<float4> renderTangentList;
#if UNITY_2018
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<BoneWeight> renderBoneWeightList;
#else
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<BoneWeight1> renderBoneWeightList;
#endif

            public NativeArray<uint> renderVertexFlagList;

            private NativeMultiHashMapIterator<int> iterator;

            // レンダーメッシュ頂点ごと
            public void Execute(int index)
            {
                uint flag = renderVertexFlagList[index];
                int rminfoIndex = (int)(flag & 0xffff);
                if (rminfoIndex == 0)
                    return;

                var r_minfo = renderMeshInfoList[rminfoIndex - 1];
                if (r_minfo.IsUse() == false)
                    return;

                var sr_minfo = sharedRenderMeshInfoList[r_minfo.renderSharedMeshIndex];

                // 頂点使用フラグをリセット
                flag &= 0xffff;

                // 利用頂点計算
                int i = index - r_minfo.vertexChunk.startIndex;
                int4 data;
                uint bit = PhysicsManagerMeshData.RenderVertexFlag_Use;
                if (renderToChildMeshIndexMap.TryGetFirstValue(rminfoIndex - 1, out data, out iterator))
                {
                    do
                    {
                        // data.x = 子共有メッシュの頂点スタートインデックス
                        // data.y = 子共有メッシュのウエイトスタートインデック
                        // data.z = 仮想メッシュの頂点スタートインデックス
                        // data.w = 仮想共有メッシュの頂点スタートインデックス

                        int sc_vindex = data.x + i;
                        int sc_wstart = data.y;
                        int m_vstart = data.z;

                        // ウエイト参照するすべての仮想頂点が利用頂点ならばこのレンダーメッシュ頂点を利用する
                        int usecnt = 0;
                        uint pack = sharedChildVertexInfoList[sc_vindex];
                        int wcnt = DataUtility.Unpack4_28Hi(pack);
                        int wstart = DataUtility.Unpack4_28Low(pack);
                        for (int j = 0; j < wcnt; j++)
                        {
                            // ウエイト０はありえない
                            var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];
                            if ((virtualVertexInfoList[m_vstart + vw.parentIndex] & 0xffff) > 0)
                                usecnt++;
                        }
                        if (wcnt > 0 && wcnt == usecnt)
                        {
                            // 利用する
                            flag |= bit;
                            break;
                        }

                        bit = bit << 1;
                    }
                    while (renderToChildMeshIndexMap.TryGetNextValue(out data, ref iterator));
                }

                // 頂点フラグを再設定
                renderVertexFlagList[index] = flag;

                // 頂点セット
                int si = r_minfo.sharedRenderMeshVertexStartIndex + i;
                if ((flag & 0xffff0000) == 0)
                {
                    // 未使用頂点
                    renderPosList[index] = sharedRenderVertices[si];
                    renderNormalList[index] = sharedRenderNormals[si];
                    renderTangentList[index] = sharedRenderTangents[si];
                }
                else
                {
                    // 使用頂点
                    renderTangentList[index] = sharedRenderTangents[si]; // w成分をコピー
                }

                // ボーンウエイト
                if (sr_minfo.IsSkinning())
                {
#if UNITY_2018
                    if ((flag & 0xffff0000) == 0)
                    {
                        // 未使用頂点
                        renderBoneWeightList[index] = sharedBoneWeightList[si];
                    }
                    else
                    {
                        // 使用頂点
                        int renderBoneIndex = sr_minfo.rendererBoneIndex;
                        BoneWeight bw = new BoneWeight();
                        bw.boneIndex0 = renderBoneIndex;
                        bw.weight0 = 1;
                        renderBoneWeightList[index] = bw;
                    }
#else
                    int svindex = sr_minfo.bonePerVertexChunk.startIndex + i;
                    int wstart = sharedBonesPerVertexStartList[svindex];
                    int windex = r_minfo.boneWeightsChunk.startIndex + wstart;
                    int swindex = sr_minfo.boneWeightsChunk.startIndex + wstart;
                    int renderBoneIndex = sr_minfo.rendererBoneIndex;

                    int cnt = sharedBonesPerVertexList[svindex];
                    if ((flag & 0xffff0000) == 0)
                    {
                        // 未使用頂点
                        for (int j = 0; j < cnt; j++)
                        {
                            renderBoneWeightList[windex + j] = sharedBoneWeightList[swindex + j];
                        }
                    }
                    else
                    {
                        // 使用頂点
                        for (int j = 0; j < cnt; j++)
                        {
                            BoneWeight1 bw = sharedBoneWeightList[swindex + j];
                            bw.boneIndex = renderBoneIndex;
                            renderBoneWeightList[windex + j] = bw;
                        }
                    }
#endif
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PreUpdate(JobHandle jobHandle)
        {
            // 何もなし
            return jobHandle;
        }

        //=========================================================================================
        /// <summary>
        /// 物理更新後処理
        /// 仮想メッシュワールド姿勢をレンダーメッシュのローカル姿勢に変換する
        /// またオプションで法線／接線／バウンディングボックスを再計算する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public override JobHandle PostUpdate(JobHandle jobHandle)
        {
            if (Manager.Mesh.RenderMeshUseCount == 0)
                return jobHandle;

            // レンダーメッシュの頂点座標／法線／接線を接続仮想メッシュから収集する
            // 頂点ごとバージョン
            var job = new CollectLocalPositionNormalTangentJob2()
            {
                renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),
                renderToChildMeshIndexMap = Manager.Mesh.renderToChildMeshIndexMap.Map,

                transformPosList = Manager.Bone.bonePosList.ToJobArray(),
                transformRotList = Manager.Bone.boneRotList.ToJobArray(),
                transformSclList = Manager.Bone.boneSclList.ToJobArray(),

                sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
                sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

                virtualPosList = Manager.Mesh.virtualPosList.ToJobArray(),
                virtualRotList = Manager.Mesh.virtualRotList.ToJobArray(),

                renderFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),
                renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
                renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
                renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
            };
            jobHandle = job.Schedule(Manager.Mesh.renderPosList.Length, 128, jobHandle);

#if false
            // ジョブはレンダーメッシュごとに処理する
            // 様々な検証によりこちらのほうが速度が速い
            //var job = new CollectLocalPositionNormalTangentJob()
            //{
            //    renderMeshInfoList = Manager.Mesh.renderMeshInfoList.ToJobArray(),
            //    renderToChildMeshIndexMap = Manager.Mesh.renderToChildMeshIndexMap.Map,

            //    transformPosList = Manager.Bone.bonePosList.ToJobArray(),
            //    transformRotList = Manager.Bone.boneRotList.ToJobArray(),
            //    transformSclList = Manager.Bone.boneSclList.ToJobArray(),

            //    sharedChildVertexInfoList = Manager.Mesh.sharedChildVertexInfoList.ToJobArray(),
            //    sharedChildVertexWeightList = Manager.Mesh.sharedChildWeightList.ToJobArray(),

            //    virtualPosList = Manager.Mesh.virtualPosList.ToJobArray(),
            //    virtualRotList = Manager.Mesh.virtualRotList.ToJobArray(),

            //    renderFlagList = Manager.Mesh.renderVertexFlagList.ToJobArray(),
            //    renderPosList = Manager.Mesh.renderPosList.ToJobArray(),
            //    renderNormalList = Manager.Mesh.renderNormalList.ToJobArray(),
            //    renderTangentList = Manager.Mesh.renderTangentList.ToJobArray(),
            //};
            //jobHandle = job.Schedule(Manager.Mesh.renderMeshInfoList.Length, 1, jobHandle);
#endif

            return jobHandle;
        }

        [BurstCompile]
        private struct CollectLocalPositionNormalTangentJob2 : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;
            [ReadOnly]
            public NativeMultiHashMap<int, int4> renderToChildMeshIndexMap;

            [ReadOnly]
            public NativeArray<float3> transformPosList;
            [ReadOnly]
            public NativeArray<quaternion> transformRotList;
            [ReadOnly]
            public NativeArray<float3> transformSclList;

            [ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [ReadOnly]
            public NativeArray<float3> virtualPosList;
            [ReadOnly]
            public NativeArray<quaternion> virtualRotList;

            [ReadOnly]
            public NativeArray<uint> renderFlagList;
            [WriteOnly]
            public NativeArray<float3> renderPosList;
            [WriteOnly]
            public NativeArray<float3> renderNormalList;
            [WriteOnly]
            public NativeArray<float4> renderTangentList;

            private NativeMultiHashMapIterator<int> iterator;

            // 頂点ごと
            public void Execute(int vindex)
            {
                uint flag = renderFlagList[vindex];

                // 使用頂点のみ
                //if ((flag & PhysicsManagerMeshData.RenderVertexFlag_Use) == 0)
                if ((flag & 0xffff0000) == 0)
                    return;

                // レンダーメッシュインスタンスインデックス
                int rmindex = DataUtility.Unpack16Low(flag) - 1; // (-1)するので注意！
                var r_minfo = renderMeshInfoList[rmindex];
                if (r_minfo.IsUse() == false)
                    return;

                // レンダラーのローカル座標系に変換する
                int tindex = r_minfo.transformIndex;
                var tpos = transformPosList[tindex];
                var trot = transformRotList[tindex];
                var tscl = transformSclList[tindex];
                quaternion itrot = math.inverse(trot);

                int vcnt = r_minfo.vertexChunk.dataLength;
                int r_vstart = r_minfo.vertexChunk.startIndex;
                int r_vindex = vindex - r_vstart; // レンダーメッシュ内のローカル頂点インデックス

                bool calcNormalTangent = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcNormalTangent);


                // レンダーメッシュは複数の仮想メッシュに接続される場合がある
                int4 data;
                if (renderToChildMeshIndexMap.TryGetFirstValue(rmindex, out data, out iterator))
                {
                    float3 sum_pos = 0;
                    float3 sum_nor = 0;
                    float3 sum_tan = 0;
                    float4 sum_tan4 = 0;
                    sum_tan4.w = -1;

                    int cnt = 0;
                    uint bit = PhysicsManagerMeshData.RenderVertexFlag_Use;

                    do
                    {
                        // data.x = 子共有メッシュの頂点スタートインデックス
                        // data.y = 子共有メッシュのウエイトスタートインデック
                        // data.z = 仮想メッシュの頂点スタートインデックス
                        // data.w = 仮想共有メッシュの頂点スタートインデックス

                        if ((flag & bit) == 0)
                        {
                            bit = bit << 1;
                            continue;
                        }

                        float3 pos = 0;
                        float3 nor = 0;
                        float3 tan = 0;

                        int sc_vindex = data.x + r_vindex;
                        int sc_wstart = data.y;
                        int m_vstart = data.z;

                        // スキニング
                        uint pack = sharedChildVertexInfoList[sc_vindex];
                        int wcnt = DataUtility.Unpack4_28Hi(pack);
                        int wstart = DataUtility.Unpack4_28Low(pack);

                        if (calcNormalTangent)
                        {
                            for (int j = 0; j < wcnt; j++)
                            {
                                var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                                // ウエイト０はありえない
                                var mpos = virtualPosList[m_vstart + vw.parentIndex];
                                var mrot = virtualRotList[m_vstart + vw.parentIndex];

                                // position
                                pos += (mpos + math.mul(mrot, vw.localPos)) * vw.weight;

                                // normal
                                nor += math.mul(mrot, vw.localNor) * vw.weight;

                                // tangent
                                tan += math.mul(mrot, vw.localTan) * vw.weight;
                            }

                            // レンダラーのローカル座標系に変換する
                            pos = math.mul(itrot, (pos - tpos)) / tscl;
                            nor = math.mul(itrot, nor);
                            tan = math.mul(itrot, tan);

                            sum_pos += pos;
                            sum_nor += nor;
                            sum_tan += tan;
                        }
                        else
                        {
                            for (int j = 0; j < wcnt; j++)
                            {
                                var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                                // ウエイト０はありえない
                                var mpos = virtualPosList[m_vstart + vw.parentIndex];
                                var mrot = virtualRotList[m_vstart + vw.parentIndex];

                                // position
                                pos += (mpos + math.mul(mrot, vw.localPos)) * vw.weight;
                            }

                            // レンダラーのローカル座標系に変換する
                            pos = math.mul(itrot, (pos - tpos)) / tscl;

                            sum_pos += pos;
                        }
                        cnt++;
                        bit = bit << 1;
                    }
                    while (renderToChildMeshIndexMap.TryGetNextValue(out data, ref iterator));

                    if (calcNormalTangent)
                    {
                        renderPosList[vindex] = sum_pos / cnt;
                        renderNormalList[vindex] = sum_nor / cnt;
                        sum_tan4.xyz = sum_tan / cnt;
                        renderTangentList[vindex] = sum_tan4;
                    }
                    else
                    {
                        renderPosList[vindex] = sum_pos / cnt;
                    }
                }
            }
        }

#if false
        [BurstCompile]
        private struct CollectLocalPositionNormalTangentJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<PhysicsManagerMeshData.RenderMeshInfo> renderMeshInfoList;
            [ReadOnly]
            public NativeMultiHashMap<int, int4> renderToChildMeshIndexMap;

            [ReadOnly]
            public NativeArray<float3> transformPosList;
            [ReadOnly]
            public NativeArray<quaternion> transformRotList;
            [ReadOnly]
            public NativeArray<float3> transformSclList;

            [ReadOnly]
            public NativeArray<uint> sharedChildVertexInfoList;
            [ReadOnly]
            public NativeArray<MeshData.VertexWeight> sharedChildVertexWeightList;

            [ReadOnly]
            public NativeArray<float3> virtualPosList;
            [ReadOnly]
            public NativeArray<quaternion> virtualRotList;

            [ReadOnly]
            public NativeArray<uint> renderFlagList;
            //[WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderPosList;
            //[WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> renderNormalList;
            //[WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> renderTangentList;

            private NativeMultiHashMapIterator<int> iterator;

            // レンダーメッシュごと
            public void Execute(int rmindex)
            {
                var r_minfo = renderMeshInfoList[rmindex]; // (-1)するので注意！
                if (r_minfo.IsUse() == false)
                    return;

                // レンダラーのローカル座標系に変換する
                int tindex = r_minfo.transformIndex;
                var tpos = transformPosList[tindex];
                var trot = transformRotList[tindex];
                var tscl = transformSclList[tindex];
                quaternion itrot = math.inverse(trot);

                int vcnt = r_minfo.vertexChunk.dataLength;
                int r_vstart = r_minfo.vertexChunk.startIndex;
                int r_vend = r_vstart + vcnt;

                bool calcNormalTangent = r_minfo.IsFlag(PhysicsManagerMeshData.Meshflag_CalcNormalTangent);

                float3 pos = 0;
                float3 nor = 0;
                float3 tan = 0;
                float4 tan4 = 0;
                tan4.w = -1;

                // レンダーメッシュは複数の仮想メッシュに接続される場合がある
                int4 data;
                if (renderToChildMeshIndexMap.TryGetFirstValue(rmindex, out data, out iterator))
                {
                    int cnt = 0;

                    do
                    {
                        // data.x = 子共有メッシュの頂点スタートインデックス
                        // data.y = 子共有メッシュのウエイトスタートインデック
                        // data.z = 仮想メッシュの頂点スタートインデックス
                        // data.w = 仮想共有メッシュの頂点スタートインデックス
                        for (int vindex = r_vstart, i = 0; vindex < r_vend; vindex++, i++)
                        {
                            uint flag = renderFlagList[vindex];

                            // 使用頂点のみ
                            if ((flag & PhysicsManagerMeshData.RenderVertexFlag_Use) == 0)
                                continue;

                            pos = 0;
                            nor = 0;
                            tan = 0;
                            cnt = 0;

                            int sc_vindex = data.x + i;
                            int sc_wstart = data.y;
                            int m_vstart = data.z;

                            // スキニング
                            uint pack = sharedChildVertexInfoList[sc_vindex];
                            int wcnt = MeshUtility.Unpack4_28Hi(pack);
                            int wstart = MeshUtility.Unpack4_28Low(pack);

                            if (calcNormalTangent)
                            {
                                for (int j = 0; j < wcnt; j++)
                                {
                                    var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                                    // ウエイト０はありえない
                                    var mpos = virtualPosList[m_vstart + vw.parentIndex];
                                    var mrot = virtualRotList[m_vstart + vw.parentIndex];

                                    // position
                                    pos += (mpos + math.mul(mrot, vw.localPos)) * vw.weight;

                                    // normal
                                    nor += math.mul(mrot, vw.localNor) * vw.weight;

                                    // tangent
                                    tan += math.mul(mrot, vw.localTan) * vw.weight;
                                }

                                // レンダラーのローカル座標系に変換する
                                pos = math.mul(itrot, (pos - tpos)) / tscl;
                                nor = math.mul(itrot, nor);
                                tan = math.mul(itrot, tan);
                                tan4.xyz = tan;

                                if (cnt == 0)
                                {
                                    renderPosList[vindex] = pos;
                                    renderNormalList[vindex] = nor;
                                    renderTangentList[vindex] = tan4;
                                }
                                else
                                {
                                    renderPosList[vindex] += pos;
                                    renderNormalList[vindex] += nor;
                                    renderTangentList[vindex] += tan4;
                                }

                            }
                            else
                            {
                                for (int j = 0; j < wcnt; j++)
                                {
                                    var vw = sharedChildVertexWeightList[sc_wstart + wstart + j];

                                    // ウエイト０はありえない
                                    var mpos = virtualPosList[m_vstart + vw.parentIndex];
                                    var mrot = virtualRotList[m_vstart + vw.parentIndex];

                                    // position
                                    pos += (mpos + math.mul(mrot, vw.localPos)) * vw.weight;
                                }


                                // レンダラーのローカル座標系に変換する
                                pos = math.mul(itrot, (pos - tpos)) / tscl;

                                if (cnt == 0)
                                {
                                    renderPosList[vindex] = pos;
                                }
                                else
                                {
                                    renderPosList[vindex] += pos;
                                }

                            }
                        }
                        cnt++;
                    }
                    while (renderToChildMeshIndexMap.TryGetNextValue(out data, ref iterator));

                    if (cnt > 1)
                    {
                        if (calcNormalTangent)
                        {
                            for (int vindex = r_vstart; vindex < r_vend; vindex++)
                            {
                                // 使用頂点のみ
                                if ((renderFlagList[vindex] & PhysicsManagerMeshData.RenderVertexFlag_Use) == 0)
                                    continue;

                                renderPosList[vindex] /= cnt;
                                renderNormalList[vindex] /= cnt;
                                tan4 = renderTangentList[vindex];
                                tan4.xyz /= cnt;
                                tan4.w = -1;
                                renderTangentList[vindex] = tan4;
                            }
                        }
                        else
                        {
                            for (int vindex = r_vstart; vindex < r_vend; vindex++)
                            {
                                // 使用頂点のみ
                                if ((renderFlagList[vindex] & PhysicsManagerMeshData.RenderVertexFlag_Use) == 0)
                                    continue;

                                renderPosList[vindex] /= cnt;
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}
