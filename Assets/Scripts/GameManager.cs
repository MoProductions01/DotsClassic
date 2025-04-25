using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

// class that manages the game
public class GameManager : MonoBehaviour {

	// some static variables
	// colors for the dots
	static Color dotsRed = new Color( 232f/255f, 82f/255f, 61f/255f, 1f );
	static Color dotsBlue = new Color( 127f/255f, 180f/255f, 255f/255f, 1f );
	static Color dotsGreen = new Color ( 127f/255f, 229f/255f, 134f/255f, 1f );
	static Color dotsYellow = new Color ( 225f/255f, 213f/255f, 34f/255f, 1f );
	static Color dotsPurple = new Color ( 142f/255f, 83f/255f, 172f/255f, 1f );
	// board size info
	static Vector2 UPPER_LEFT = new Vector2( -2.009f, 2.025f );
	static float DOT_SPACING = .803f;
	static int BOARD_SIZE = 6;

	enum eGameState {NOT_PLAYING, PLAYING} // whether or not the game is active
	eGameState GameState; // current state of the game
	public TMP_Text timerText; // text for the timer
	private float timer; // keeps track of time
	public TMP_Text scoreText; // text for score
	private int score; // current score
	static private float GAME_TIME = 60f;

	// list of colors for random generation
	Color[] dotsColors = { dotsRed, dotsBlue, dotsGreen, dotsYellow, dotsPurple };
	bool isDrawingLines; // keeps track of if we're in line drawing mode	
	public GameObject dotContainer;	// this is just to keep the Hierarchy tidy when the 36 dots show up

	List<DotConnection> dotConnections = new List<DotConnection>();	// list of current connections between dots on the board
	List<DotConnection> squareDotConnections = new List<DotConnection>();	// list of connections that form complete squares
	List<Dot> curLineDots = new List<Dot>();	// dots in the current line

	Dot[,] dotGrid = new Dot[BOARD_SIZE, BOARD_SIZE];	// 2D array representing the game grid
	Dot latestDot;	// the last dot selected
	
	public GameObject startButton; // Start button that shows up before you start playing
	public GameObject dotTemplate;	// GameObject that all the dots are created from
	public LineRenderer connectionLineRenderer;	// LineRenderer for connecting the dots. Used to create instances when connections are made
	public SpriteRenderer squareOverlay;	// overlay when a square is active on the board
	
	void UpdateTimer(float deltaTime)
	{
		timer -= deltaTime;
		if(timer <= 0f)
		{	// game over
			GameState = eGameState.NOT_PLAYING;
			timerText.text = timer.ToString("00");
			startButton.gameObject.SetActive(true);
		}
		else
		{
			timerText.text = Mathf.CeilToInt(timer % GAME_TIME).ToString("00");
		}
	}

	void UpdateScore(int pointsEarned)
	{
		score += pointsEarned;
		scoreText.text = score.ToString();
	}

	public void StartGame()
	{
		GameState = eGameState.PLAYING; // we are now playing
		startButton.gameObject.SetActive(false); // shut off button
		FillBoard ();	// fill up the board with new dots
		score = 0;	// default values
		UpdateScore(0);
		timer = GAME_TIME;
		curLineDots.Clear();
	}

	void Start () 
	{				
		GameState = eGameState.NOT_PLAYING; // Waiting for start button to be clicked
		dotContainer = new GameObject("dotContainer"); // this is just to keep all the board dots tidy in the Hierarchy		
		isDrawingLines = false;	// currently not drawing lines between dots
		// switch off the line renderer and dot template so they're not visible at the start of the game
		connectionLineRenderer.gameObject.SetActive(false);
		dotTemplate.gameObject.SetActive(false);

		// set up the square overlay to be the width of the screen and 80% of the height
		squareOverlay.transform.localScale = new Vector3(1,1,1);
		float width = squareOverlay.sprite.bounds.size.x;
		float height = squareOverlay.sprite.bounds.size.y;
		float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
		float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
		squareOverlay.transform.localScale = new Vector3( worldScreenWidth / width, (worldScreenHeight / height)*.8f, 1f );
		squareOverlay.gameObject.SetActive(false);				
	}				

	void Update () 
	{
		//Only do this if we're playing
		if (GameState != eGameState.PLAYING) return;
		UpdateTimer(Time.deltaTime);		

		// check for button press
		if( Input.GetMouseButtonDown(0) )
		{ 
			// check to see if we've selected anything
			RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if( hit.collider != null )
			{
				isDrawingLines = true;
				latestDot = hit.collider.GetComponent<Dot>();	// keep track of the latest selected dot for checks later
				connectionLineRenderer.gameObject.SetActive(true);	// turn on and set up the current line renderer
				connectionLineRenderer.SetColors( latestDot.color, latestDot.color );
				connectionLineRenderer.SetPosition(0, latestDot.transform.position );
				connectionLineRenderer.SetPosition(1, latestDot.transform.position );
				curLineDots.Add( latestDot );	// keep track of the current list of dots for this line
				latestDot.StartDotTouch();	// start the dot touch anim
			}
		}
		// check for mouse drag
		if( Input.GetMouseButton(0) && isDrawingLines == true )
		{
			// update the position of the current line renderer
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mousePos.z = 0f;
			connectionLineRenderer.SetPosition(1, mousePos );
			// check for another dot it
			RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if( hit.collider != null )
			{
				Dot newDot = hit.collider.GetComponent<Dot>();
				if( newDot == latestDot ) return;	// ignore if we're on the same dot
				if( newDot.color != latestDot.color ) return;	// ignore if it's not the correct color
				// only allow connections to dots that are 1 space (not diagonal) away
				if( (Mathf.Abs ( latestDot.row - newDot.row ) + Mathf.Abs ( latestDot.col - newDot.col ) ) > 1 ) return;
				// check to see if we're going back to our previous dot in case we need to break connections
				if( dotConnections.Count > 0 )
				{
					DotConnection lastConnection = dotConnections[dotConnections.Count-1]; 
					if( lastConnection.IsSameConnection( latestDot, newDot ) ) // check if we're trying to create our last connection
					{
						// ok we're going to remove this connection since we backed up to our previous dot
						// if this connection isn't in our list of connections that has created a square then remove the dot from the list				
						if( squareDotConnections.Contains(lastConnection) == false )
						{
							curLineDots.Remove(latestDot);
						}
						else
						{	// ok we just broke a connection that created a square, so remove it from the list
							squareDotConnections.Remove(lastConnection);
							// see if we need to turn the square overlay off
							if( squareDotConnections.Count == 0 ) squareOverlay.gameObject.SetActive(false);
						}
						dotConnections.Remove(lastConnection); 	// remove the last connection from the list
						lastConnection.DestroyLineRenderer();	// trash it's line renderer since it's a MonoBehaviour
						latestDot = newDot;	// update latest dot connected
						connectionLineRenderer.SetPosition(0, hit.collider.transform.position); // updated the current line renderer
						connectionLineRenderer.SetPosition(1, hit.collider.transform.position);
						return;
					}
					// check if we've already got this connection
					foreach( DotConnection con in dotConnections )
					{
						if( con.IsSameConnection( latestDot, newDot ) )
						{
							// already have this connection so ignore and leave
							return;
						}
					}
				}

				// if we've made it this far past all the above checks, then we've created a valid new connection
				newDot.StartDotTouch();
				connectionLineRenderer.SetPosition(1, hit.collider.transform.position);
				// instantiate a copy of the current line renderer since it's already set up
				LineRenderer newLineRenderer = GameObject.Instantiate(connectionLineRenderer).GetComponent<LineRenderer>();
				DotConnection newConTest = new DotConnection( newDot, latestDot, newLineRenderer ); // create a new connection with the dots and new line renderer
				dotConnections.Add(newConTest);	// add the connection to the list
				latestDot = newDot;	// update the latest dot

				// if we're here then check to see if the dot is already in the list. If it is, then we've got a square
				if( curLineDots.Contains(newDot) )
				{	// yup, got a square so go through the board and get all the correctly colored dots animating with their touch animation
					for(int row=0; row<BOARD_SIZE; row++)
					{
						for( int col=0; col<BOARD_SIZE; col++ )
						{
							if( dotGrid[row,col].color == latestDot.color ) dotGrid[row,col].StartDotTouch();
						}
					}
					squareDotConnections.Add( newConTest );	// add the new square connection
					squareOverlay.gameObject.SetActive(true);	// turn on the square overlay and update color
					squareOverlay.color = new Color( latestDot.color.r, latestDot.color.g, latestDot.color.b, .4f );
				}
				else
				{
					curLineDots.Add( latestDot );	// nope, no new connection that creates a square, so it's a new dot for the current line
				}
				// get the current/working line renderer set up in the correct position
				connectionLineRenderer.SetPosition(0, hit.collider.transform.position);
				connectionLineRenderer.SetPosition(1, hit.collider.transform.position);													
			}
		}
		// check for mouse button up
		if( Input.GetMouseButtonUp(0) && isDrawingLines == true )
		{	// lifted the mouse button, so figure out what to do
			isDrawingLines = false;
			connectionLineRenderer.gameObject.SetActive(false);	// switch off current line renderer
			squareOverlay.gameObject.SetActive(false);	// turn off the square overlay
			bool madeSquare = squareDotConnections.Count > 0;	// keep track of if we've made a square
			// Destroy all line renderers in the connections
			foreach( DotConnection con in dotConnections ) 
			{
				con.DestroyLineRenderer();
			}
			dotConnections.Clear();	// clear the connections
			squareDotConnections.Clear ();// clear out any squareConnections

			int[] numDotsToFillPerColumn = new int[BOARD_SIZE]; // keep track of how many replacement dots we need for each column
			latestDot = null;
			if( curLineDots.Count > 1 ) // check if we've got at least 2 dots in our list
			{
				if( madeSquare == true )	// check if we've got a square
				{	// we do, so go through the whole grid and add any dots to our destruction list that match the color
					for( int row=0; row<BOARD_SIZE; row++)
					{
						for( int col = 0; col<BOARD_SIZE; col++)
						{
							Dot curDot = dotGrid[row, col];
							if( (curDot.color == curLineDots[0].color) &&
							     (curLineDots.Contains(curDot) == false ) )
							{
								curLineDots.Add(curDot);
							}
						}
					}
				}
				int points = curLineDots.Count;
				if(madeSquare == true) points *= 4; // if you've made a square then multiple the value of the points				
				UpdateScore(points);
				foreach( Dot dot in curLineDots )	// fill up the array keeping track of how many new dots we need to fill for each column
				{				
					numDotsToFillPerColumn[dot.col]++;
					dotGrid[dot.row, dot.col] = null;
					Destroy (dot.gameObject);	// destroy all the dots on the list
				}
				curLineDots.Clear();	// clear out the list
			}
			else if( curLineDots.Count == 1 )
			{
				// just 1, so clear list and bail
				curLineDots.Clear ();
				return;
			}
			// if we're here then dots were removed from the board so fill the board back up
			float curYTween;
			int rowToFill;
			for( int col=0; col<BOARD_SIZE; col++)	// go through each of the columns
			{
				// move from the bottom row up, filling in and updating the new available spaces based on the empty grid spots
				int row = BOARD_SIZE-1;
				while( row > 0 )
				{
					if(dotGrid[row,col] == null ) // have an empty spot, so find out what will fill it
					{
						rowToFill = row;
						curYTween = UPPER_LEFT.y - DOT_SPACING * (float)row;	// update current tween value
						row--;
						while( row >= 0 && dotGrid[row,col] == null  )	// keep looking for a dot to drop into place
						{
							row--;
						}
						if( row >= 0 )	// fount a dot, so update it's position and ge the tween going
						{
							Tween myTween = dotGrid[row,col].gameObject.transform.DOMoveY (curYTween, .2f);
							myTween.SetEase (Ease.OutBounce);
							dotGrid[rowToFill, col] = dotGrid[row,col];
							dotGrid[rowToFill, col].row = rowToFill;
							dotGrid[row,col] = null;
							if( row != 0 ) row = rowToFill-1;
							curYTween = UPPER_LEFT.y - DOT_SPACING * (float)row;
						}
					}
					else row--;
				}
			}
			// refill the board with new dots based on how many need to be filled for each column
			for( int col=0; col < BOARD_SIZE; col++ )
			{
				for( int row=0; row < numDotsToFillPerColumn[col]; row++ )
				{
					SetUpNewDot( row, col );
				}
			}
		}
	}

	/// <summary>
	/// Creates a new dot and gets it's drop tween going
	/// </summary>
	/// <param name="row">row for new dot</param>
	/// <param name="col">column for new dot</param>
	void SetUpNewDot( int row, int col )
	{
		GameObject obj = GameObject.Instantiate(dotTemplate);	// instantiate a new dot 
		obj.name = row + "," + col;
		obj.SetActive(true); 	// make sure it's turned on
		Dot dotObject = obj.GetComponent<Dot>();
		// init the new dot object with a random color and the correct row, column and tween values for the drop
		dotObject.InitWithTweenDrop( dotsColors[Random.Range (0, dotsColors.Length)],  //(int)eColors.NUM_DOTS_COLORS)], 
		                            new Vector3( UPPER_LEFT.x + DOT_SPACING * (float)col, UPPER_LEFT.y + DOT_SPACING * (float)(BOARD_SIZE-row-1), 0f ), 
		                            UPPER_LEFT.y - DOT_SPACING * (float)row, row, col );
		dotObject.transform.parent = dotContainer.transform;	// put it in the dot container for tidyness
		dotGrid[row,col] = dotObject;	// update the game grid
		//dotObject.color = dotsYellow;
	}

	/// <summary>
	/// Fills the board up with dots at the start of the game
	/// </summary>
	void FillBoard()
	{
		// destroy the old board
		//Debug.Log("Num children: " + dotContainer.transform.childCount);
		if(dotContainer.transform.childCount > 36)
		{
		//	Debug.Log("WTF");
		}
		if(dotContainer.transform.childCount > 0)
		{	
			int i=0;
			foreach( Transform dot in dotContainer.transform)
			{
				i++;
				Destroy(dot.gameObject);
			}
			//Debug.Log("i:" + i);
			for( int row=0; row<BOARD_SIZE; row++)
			{
				for( int col = 0; col<BOARD_SIZE; col++)
				{							
					dotGrid[row,col] = null;
				}
			}
		}

		for( int row=0; row < BOARD_SIZE; row++ )
		{
			for(int col=0; col < BOARD_SIZE; col++ )
			{
				SetUpNewDot(row, col);
			}
		}
		// Below is example debug code to force a certain shape on the board
		/*dotGrid[0,0].color = dotsBlue;
		dotGrid[0,1].color = dotsBlue;
		dotGrid[1,0].color = dotsBlue;	
		dotGrid[1,1].color = dotsBlue;		
		
		
		dotGrid[3,4].color = dotsRed;
		dotGrid[3,5].color = dotsRed;
		dotGrid[4,4].color = dotsRed;
		dotGrid[4,5].color = dotsRed;
		
		dotGrid[5,2].color = dotsPurple;
		dotGrid[5,3].color = dotsPurple;
		
		dotGrid[2,2].color = dotsGreen;
		dotGrid[3,2].color = dotsGreen;
		dotGrid[4,2].color = dotsGreen;*/
	}
}
//1,0
//0,0
//0,1