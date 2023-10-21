using UnityEngine;

public class GotoMe : MonoBehaviour
{
	private int team;

	private void Start()
	{
		AiActorController[] array = Object.FindObjectsOfType<AiActorController>();
		foreach (AiActorController aiActorController in array)
		{
			if (aiActorController.actor.team == team)
			{
				aiActorController.Goto(base.transform.position);
			}
		}
	}
}
