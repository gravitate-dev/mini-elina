using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MonoBehaviourを継承するコンポーネント用に各種インターフェースを定義したもの
    /// ・共有データ収集
    /// ・データ検証
    /// ・メッシュ座標取得
    /// ・クロス状態取得
    /// ・データハッシュ作成
    /// ・実行状態管理
    /// </summary>
    public abstract class CoreComponent : MonoBehaviour, IShareDataObject, IDataVerify, IEditorMesh, IEditorCloth, IDataHash
    {
        [SerializeField]
        protected int dataHash;
        [SerializeField]
        protected int dataVersion;

        /// <summary>
        /// 実行状態
        /// </summary>
        protected RuntimeStatus status = new RuntimeStatus();

        public RuntimeStatus Status
        {
            get
            {
                return status;
            }
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public abstract int GetDataHash();

        public int SaveDataHash
        {
            get
            {
                return dataHash;
            }
        }

        public int SaveDataVersion
        {
            get
            {
                return dataVersion;
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
            Init();
        }

        public virtual void OnEnable()
        {
            status.SetEnable(true);
            UpdateStatus();
        }

        public virtual void OnDisable()
        {
            status.SetEnable(false);
            UpdateStatus();
        }

        protected virtual void OnDestroy()
        {
            OnDispose();
            status.SetDispose();
        }

        protected virtual void Update()
        {
            if (status.IsInitSuccess)
            {
                var error = VerifyData() != Define.Error.None;
                status.SetRuntimeError(error);
                UpdateStatus();

                if (status.IsActive)
                    OnUpdate();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// 通常はStart()で呼ぶ
        /// </summary>
        /// <param name="vcnt"></param>
        void Init()
        {
            if (status.IsInitComplete)
                return;
            status.SetInitComplete();

            if (VerifyData() != Define.Error.None)
            {
                status.SetInitError();
                return;
            }

            OnInit();
            if (status.IsInitError)
                return;

            UpdateStatus();
        }

        // 実行状態の更新
        void UpdateStatus()
        {
            if (status.UpdateStatus())
            {
                if (status.IsActive)
                {
                    // 実行状態に入った
                    OnActive();
                }
                else
                {
                    // 実行状態から抜けた
                    OnInactive();
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 初期化
        /// </summary>
        protected abstract void OnInit();

        /// <summary>
        /// 破棄
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// 更新
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 実行状態に入った場合に呼ばれます
        /// </summary>
        protected abstract void OnActive();

        /// <summary>
        /// 実行状態から抜けた場合に呼ばれます
        /// </summary>
        protected abstract void OnInactive();


        //=========================================================================================
        /// <summary>
        /// 共有データの収集
        /// </summary>
        /// <returns></returns>
        public virtual List<ShareDataObject> GetAllShareDataObject()
        {
            return new List<ShareDataObject>();
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract ShareDataObject DuplicateShareDataObject(ShareDataObject source);


        //=========================================================================================
        /// <summary>
        /// ユーザー操作による有効フラグの切り替え(v1.2)
        /// </summary>
        /// <param name="sw"></param>
        protected void SetUserEnable(bool sw)
        {
            if (status.SetUserEnable(sw))
            {
                UpdateStatus();
            }
        }

        //=========================================================================================
        /// <summary>
        /// データバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public abstract int GetVersion();

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public virtual Define.Error VerifyData()
        {
            if (dataHash == 0)
                return Define.Error.InvalidDataHash;
            if (dataVersion != GetVersion())
                return Define.Error.DataVersionMismatch;

            return Define.Error.None;
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public virtual void CreateVerifyData()
        {
            dataHash = GetDataHash();
            dataVersion = GetVersion();
        }

        /// <summary>
        /// データ検証の結果テキストを取得する
        /// </summary>
        /// <returns></returns>
        public virtual string GetInformation()
        {
            return "No information.";
        }

        //=========================================================================================
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す（エディタ用）
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        public virtual int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// メッシュのトライアングルリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetEditorTriangleList()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// メッシュのラインリストを返す（エディタ用）
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetEditorLineList()
        {
            throw new System.NotImplementedException();
        }

        //=========================================================================================
        /// <summary>
        /// 頂点の選択状態をリストにして返す（エディタ用）
        /// 選択状態は ClothSelection.Invalid / ClothSelection.Fixed / ClothSelection.Move
        /// すべてがInvalidならばnullを返す
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetSelectionList()
        {
            return null;
        }

        /// <summary>
        /// 頂点の使用状態をリストにして返す（エディタ用）
        /// 数値が１以上ならば使用中とみなす
        /// すべて使用状態ならばnullを返す
        /// </summary>
        /// <returns></returns>
        public virtual List<int> GetUseList()
        {
            return null;
        }
    }
}
