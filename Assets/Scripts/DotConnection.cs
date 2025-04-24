using UnityEngine;
using System.Collections;

// class for keeping track of connections between dots on the board
public class DotConnection 
{
	private Dot _dotA;
	private Dot _dotB;
	private LineRenderer _lineRenderer;
	
	public Dot dotA
	{
		get { return _dotA; }
		set { _dotA = value; }
	}
	public Dot dotB
	{
		get { return _dotB; }
		set { _dotB = value; }
	}
	
	public LineRenderer lineRenderer
	{
		get { return _lineRenderer; }
		set { _lineRenderer = value; }
	}

	/// <summary>
	/// Constructor for this dot connection
	/// </summary>
	/// <param name="dotA">First dot in this connection.</param>
	/// <param name="dotB">Second dot in this connection</param>
	/// <param name="lineRenderer">Line renderer for this connections.</param>
	public DotConnection( Dot dotA, Dot dotB, LineRenderer lineRenderer )
	{
		this.dotA = dotA;
		this.dotB = dotB;
		this.lineRenderer = lineRenderer;
	}

	/// <summary>
	/// Checks whether the two dots passed in match this connection
	/// </summary>
	/// <returns><c>true</c> if this instance is same connection the specified dotA dotB; otherwise, <c>false</c>.</returns>
	/// <param name="dotA">Dot a.</param>
	/// <param name="dotB">Dot b.</param>
	public bool IsSameConnection( Dot dotA, Dot dotB )
	{
		return (( this.dotA == dotA && this.dotB == dotB ) || ( this.dotA == dotB && this.dotB == dotA ) );
	}

	/// <summary>
	/// Trash the line renderer for this connection
	/// </summary>
	public void DestroyLineRenderer()
	{
		GameObject.Destroy( lineRenderer.gameObject );
	}
}
