using UnityEngine;
using System.Collections;
using DG.Tweening;

// class for one of the dots on the game board
public class Dot : MonoBehaviour 
{
	public SpriteRenderer foregroundSprite;	// the foreground sprite never changes
	public SpriteRenderer backgroundSprite;	// the background sprite will increase in scale and change alpha when selected
	private int _row;	// row,column for this dot
	private int _col;
	private Color _color; 	// color for this dot
	static float TOUCH_TWEEN_TIME = .5f;	// how fast the touch animation lasts

	public int row
	{
		get { return _row; }
		set { _row = value; }
	}
	public int col
	{
		get { return _col; }
		set { _col = value; }
	}
	public Color color
	{
		get { return _color; }
		set 
		{ 	// for the color, don't just update the value but the acutal color component of each of the SpriteRenderers
			_color = value; 
			foregroundSprite.color = value;
			backgroundSprite.color = value;
		}
	}

	/// <summary>
	/// Starts the animation when a dot is touched (or when a square is created)
	/// </summary>
	public void StartDotTouch()
	{
		// start the scale tween, setting up the callback
		backgroundSprite.gameObject.transform.DOScale( new Vector3( 2f, 2f, 0f ), TOUCH_TWEEN_TIME ).SetEase(Ease.Linear).OnComplete(DotTouchScaleCallback);
		// start the color tween (just adjusts alpha) including the callback
		Color newColor = color;
		newColor.a = 0f;
		backgroundSprite.DOColor( newColor, TOUCH_TWEEN_TIME ).SetEase(Ease.Linear).SetAutoKill(true).OnComplete(DotTouchColorCallback);;
	}

	/// <summary>
	/// Callback when the dot animation has done scaling.  Just go back to identity scale
	/// </summary>
	void DotTouchScaleCallback()
	{
		backgroundSprite.gameObject.transform.localScale = new Vector3(1f,1f,1f);
	}
	/// <summary>
	/// Callback when the dot color has finished tweening.  Go back to original color
	/// </summary>
	void DotTouchColorCallback()
	{
		backgroundSprite.color = this.color;
	}

	/// <summary>
	/// Inits the dot with a tween setup for a dropping animation
	/// </summary>
	/// <param name="color">Color of the dot.</param>
	/// <param name="pos">Position starting position of the dot before drop</param>
	/// <param name="tweenEndLoc">End y value for the drop tween.</param>
	/// <param name="row">row of the dot.</param>
	/// <param name="col">column of the dot.</param>
	public void InitWithTweenDrop( Color color, Vector3 pos, float tweenEndLoc, int row, int col )
	{
		this.color = color;
		this.row = row;
		this.col = col;
		transform.position = pos;
		transform.DOMoveY ( tweenEndLoc, .5f).SetEase(Ease.OutBounce);		
	}
}
