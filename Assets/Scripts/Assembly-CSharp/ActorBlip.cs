using UnityEngine;
using UnityEngine.UI;

public class ActorBlip : MonoBehaviour
{
	private Actor actor;

	private RawImage image;

	private void Awake()
	{
		image = GetComponent<RawImage>();
	}

	public void SetActor(Actor actor)
	{
		this.actor = actor;
		image.color = Color.Lerp(ColorScheme.TeamColor(actor.team), Color.white, 0.5f);
	}

	private void LateUpdate()
	{
		if (actor != null && !actor.dead)
		{
			RectTransform rectTransform = (RectTransform)base.transform;
			Vector3 vector = MinimapCamera.instance.camera.WorldToViewportPoint(actor.Position());
			Vector2 anchorMax = (rectTransform.anchorMin = new Vector2(vector.x, vector.y));
			rectTransform.anchorMax = anchorMax;
			rectTransform.rotation = Quaternion.Euler(0f, 0f, 0f - Quaternion.LookRotation(actor.controller.FacingDirection()).eulerAngles.y);
			image.enabled = true;
		}
		else
		{
			image.enabled = false;
		}
	}
}
