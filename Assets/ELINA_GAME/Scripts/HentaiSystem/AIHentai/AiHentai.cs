using System.Collections.Generic;
using UnityEngine;

public class AiHentai : MonoBehaviour
{
    private HentaiSexCoordinator hentaiSexCoordinator;
    
    void Awake()
    {
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        if (hentaiSexCoordinator == null)
        {
            throw new System.Exception("Missing hentai sex coordinator");
        }
    }

    public void FuckTarget(GameObject target)
    {
        int result = hentaiSexCoordinator.TrySexTarget(target);
        if (result == HentaiSexCoordinator.TRY_SEX_RESULT_PICK_MOVE)
        {
            HentaiSexCoordinator victimCoordinator = target.GetComponent<HentaiSexCoordinator>();
            List<HMove> moves = HentaiMoveSystem.INSTANCE.getHMoveForParts(hentaiSexCoordinator.getAvailableParts(), victimCoordinator.getAvailableParts());
            if (moves.Count == 0)
            {
                return;
            }
            victimCoordinator.StartNewSexMove(gameObject,moves[0], false);
        }
    }
}
