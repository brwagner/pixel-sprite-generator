using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using PSG;

public class Example : MonoBehaviour
{

	// Use this for initialization
	void Start ()
	{
		float spritesPerRow = 10;
		for (float i = -spritesPerRow / 2f; i < spritesPerRow / 2f; i++) {
			var go = new GameObject ();
			go.transform.position += Vector3.down * -3f + Vector3.left * (i + 0.5f);
			go.AddComponent<SpriteRenderer> ().sprite = Robot ();

			go = new GameObject ();
			go.transform.position += Vector3.down * -1.5f + Vector3.left * (i + 0.5f);
			go.AddComponent<SpriteRenderer> ().sprite = Dragon ();

			go = new GameObject ();
			go.transform.position += Vector3.left * (i + 0.5f);
			go.AddComponent<SpriteRenderer> ().sprite = Ship ();

			go = new GameObject ();
			go.transform.position += Vector3.down * 1.5f + Vector3.left * (i + 0.5f);
			go.AddComponent<SpriteRenderer> ().sprite = ShipLowSaturation ();

			go = new GameObject ();
			go.transform.position += Vector3.down * 3f + Vector3.left * (i + 0.5f);
			go.AddComponent<SpriteRenderer> ().sprite = ShipColorful ();
		}
	}

	private Sprite Robot ()
	{
		var mask = Mask.FromFile ("Assets/Masks/robot.csv", mirrorX: true, mirrorY: false);
		return new PixelSpriteGenerator (mask, scale: 8, foregroundColor:Color.blue).CreateSprite ();
	}

	private Sprite Ship ()
	{
		var mask = Mask.FromFile ("Assets/Masks/ship.csv", mirrorX: true, mirrorY: false);
		return new PixelSpriteGenerator (mask, scale: 8).CreateSprite ();
	}

	private Sprite Dragon ()
	{
		var mask = Mask.FromFile ("Assets/Masks/dragon.csv", mirrorX: false, mirrorY: false);
		return new PixelSpriteGenerator (mask, scale: 8).CreateSprite ();
	}

	private Sprite ShipLowSaturation ()
	{
		var mask = Mask.FromFile ("Assets/Masks/ship.csv", mirrorX: true, mirrorY: false);
		return new PixelSpriteGenerator (mask, saturation: 0.1f, scale: 8).CreateSprite ();
	}

	private Sprite ShipColorful ()
	{
		var mask = Mask.FromFile ("Assets/Masks/ship.csv", mirrorX: true, mirrorY: false);
		return new PixelSpriteGenerator (mask, colorVariations: 0.9f, saturation: 0.8f, scale: 8).CreateSprite ();
	}
}