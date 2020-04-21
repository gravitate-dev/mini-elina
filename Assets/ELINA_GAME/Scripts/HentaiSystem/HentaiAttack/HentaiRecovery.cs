using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HentaiRecovery : MonoBehaviour
{
	[Tooltip("Health percent per second")]
	public float speed;

	[SerializeField]
	private float baseline = -1;
	private HealthSystem healthSystem;
	private HentaiSexCoordinator hentaiSexCoordinator;

	private void Start()
	{
		healthSystem = GetComponent<HealthSystem>();
		hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
	}

	private void Update()
	{
		if (hentaiSexCoordinator.AmIVictim() && !hentaiSexCoordinator.IsSexPlaygroundMode())
		{
			if (baseline<=0)
			{
				baseline = Mathf.Clamp01(healthSystem.CurrentHealth / healthSystem.MaxHealth);
			}

			baseline += (speed * Time.deltaTime);
			baseline = Mathf.Min(baseline, 1);
			//HUDHealth.SetValue(baseline);
			if (Input.GetKeyUp(KeyCode.Space))
			{
				if (hentaiSexCoordinator.CanEscapeRightNow())
				{
					// do the recovery and escape!
					healthSystem.CurrentHealth = healthSystem.MaxHealth * baseline;
					baseline = -1;
					hentaiSexCoordinator.StopAllSexIfAny();
				}
			}
		}
		else
		{
			if (Input.GetKeyUp(KeyCode.Space))
			{
				// stop the sex
				hentaiSexCoordinator.StopAllSexIfAny();
			}
		}
	}
}
