using Animancer;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothMirror : MonoBehaviour
{

	public GameObject TEMP_ITEM_PREFAB;

	private List<ClothBinder> CurrentClothing = new List<ClothBinder>();

	[Required]
	public Transform childHead;
	[Required]
	public Transform childRoot;

	private Transform selfHead;

	private void Awake()
	{
		HybridAnimancerComponent hybridAnimancer = GetComponent<Animancer.HybridAnimancerComponent>();
		selfHead = hybridAnimancer.Animator.GetBoneTransform(HumanBodyBones.Head);
		if (GetComponent<HentaiRigSwapper>())
		{
			if (childHead==null || childRoot == null)
			{
				Debug.LogError("You must set head and root if you have a rig swapper");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#endif
			}
		}
	}
	// Only non dummy clothes will call link
	public void Link(ClothBinder clothBinder)
	{
		if (clothBinder == null)
		{
			return;
		}
		if (clothBinder.isMirroredInstance)
		{
			Debug.LogError("Fatal, mirrored instance tried to link");
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
			return;
		}
		PruneCloth();
		if (ContainsCloth(clothBinder))
		{
			return;
		}
		// create new instance
		CreateCopyAndPair(clothBinder);
		CurrentClothing.Add(clothBinder);
	}

	private void CreateCopyAndPair(ClothBinder clothBinder)
	{
		if (childHead == null || childRoot==null)
		{
			Debug.LogError("You MUST have child head and child root! on " +gameObject.name);
		}
		GameObject newGo;
		if (clothBinder.bindToHead)
		{
			newGo = Instantiate(clothBinder.gameObject, childHead);
		} else
		{
			newGo = Instantiate(clothBinder.gameObject, childRoot);
		}
		newGo.GetComponent<ClothBinder>().SetClothingPair(clothBinder.gameObject.GetInstanceID());
		
		// color the item
		ColorableItem parentColorableItem = clothBinder.GetComponent<ColorableItem>();
		if (parentColorableItem == null)
		{
			return;
		}
		StartCoroutine(DelayColorizationMirror(newGo, parentColorableItem));
	}

	private IEnumerator DelayColorizationMirror(GameObject newChild, ColorableItem parentClothBinder)
	{
		yield return new WaitForEndOfFrame();
		// colors should be initialized by now
		parentClothBinder.SetMirrorChild(newChild.GetComponent<ColorableItem>());
	}

    #region === CurrentClothing List Management ===
    /// <summary>
    /// Removes nulls
    /// </summary>
    private void PruneCloth()
	{
		if (CurrentClothing.Count == 0)
		{
			return;
		}
		for (int i = CurrentClothing.Count - 1; i >= 0; i--)
		{
			ClothBinder temp = CurrentClothing[i];
			if (temp == null)
			{
				CurrentClothing.RemoveAt(i);
			}
		}
	}

	private void RemoveCloth(ClothBinder clothBinder)
	{
		if (CurrentClothing.Count == 0)
		{
			return;
		}
		for (int i = CurrentClothing.Count - 1; i >= 0; i--)
		{
			ClothBinder temp = CurrentClothing[i];
			if (temp != null && temp.gameObject.GetInstanceID().Equals(clothBinder.gameObject.GetInstanceID()))
			{
				CurrentClothing.RemoveAt(i);
			}
		}
	}

	private bool ContainsCloth(ClothBinder clothBinder)
	{
		foreach(ClothBinder other in CurrentClothing)
		{
			if (other!=null && other.gameObject.GetInstanceID().Equals(clothBinder.gameObject.GetInstanceID()))
			{
				return true;
			}
		}
		return false;
	}

	#endregion


	[Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
	public void TestTempItem()
	{
		AddTempItem(TEMP_ITEM_PREFAB, 5);
	}
	public void AddTempItem(GameObject prefab, float duration)
	{
		ClothBinder clothBinder = gameObject.GetComponent<ClothBinder>();
		GameObject newGo;
		if (clothBinder.bindToHead)
		{
			newGo = Instantiate(prefab,selfHead);
		} else
		{
			newGo = Instantiate(prefab, transform);
		}
		clothBinder.SelfDestructIn(duration);
	}
}
