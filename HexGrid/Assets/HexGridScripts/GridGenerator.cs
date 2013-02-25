using UnityEngine;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
// this class generator a map of hex grids in the following pattern
/*
 *   ======== width ========= |
 *   * * * * * * * * * * * *  |
 *    * * * * * * * * * * *   length
 *   * * * * * * * * * * * *  |
 *    * * * * * * * * * * *   |
 * 
 */
// it also initiates a list containing all the hexgrids for use by other modules
public class GridGenerator: MonoBehaviour
{
	//when this is enabled, map will be destroyed and reinitialised everytime play is hit
	public bool m_mapGenDebugMode = false;
	//this will enable testing of preserved data and reference
	public bool m_editorDebugMode = true;
	[SerializeField]
	private int m_hexGridRefTestNum;
	
    public GameObject m_hexPrefab;
	public GameObject m_hexBlueMaskPrefab;
	public GameObject m_hexRedMaskPrefab;
	public GameObject m_hexGreenMaskPrefab;
	public GameObject m_hexOutlineMaskPrefab;
    //instantiate using unity editor
    public int m_gridNumHor = 11; //number of grids in horizontal direction
    public int m_gridNumVer = 11; //number of grids in vertical direction

    //Hexagon tile width and length in game world
    private float m_hexWidth;
    private float m_hexLength;
	private Vector3 m_initPos;
	
	//list of all hexgrids
	[SerializeField]
	//unity cannot serialize nested list.....
	//public List<List<HexGridModel>> grids;
	private List<GameObject> m_grids;
	
	public List<List<GameObject>> GetGridData()
	{
		List<List<GameObject>> grids = new List<List<GameObject>>();
		for(int i=0;i<m_gridNumVer;i++)
			grids.Add(new List<GameObject>());
		int index = 0; //index of grid in m_grids
		for(int row=0;row<m_gridNumVer;row++){
			int numNodes = (row%2 == 0)?m_gridNumHor:m_gridNumHor-1;
			for(int col=0;col<numNodes;col++){
				grids[row].Add(m_grids[index]);
				index ++;
			}
		}
		if(index != m_grids.Count)
			throw new DataMisalignedException("Wrong number of grids in returned data");
		//if something goes wrong in this method, the index will simply not match up
		//the program will throw an exception
		return grids;
	}

    //Method to initialise Hexagon width and length
    private void GetGridSize()
    {
        //renderer component attached to the Hex prefab is used to get the current width and length
        m_hexWidth = m_hexPrefab.renderer.bounds.size.x;
        m_hexLength = m_hexPrefab.renderer.bounds.size.z;
    }
	
	//Method to initliase the list
	private void InitList()
	{
		m_grids = new List<GameObject>();
	}
	
	//Method to initliase testing states
	private void InitDebugStates()
	{
		m_hexGridRefTestNum = 0;
	}

    //Method to calculate the position of the first hexagon tile (negative x, positive z)
    //The center of the hex grid is (0,0,0)
    private void CalcInitPos()
    {
		m_initPos =  new Vector3(m_hexWidth*(-m_gridNumHor+1)/2f, 0, (m_gridNumVer/2)*m_hexLength*3f/4);
    }

    //Method used to convert coordinates in grids to game world coordinates
    private Vector3 CalcWorldCoord(int gridX, int gridY)
    {
        //Every second row is offset by half of the tile width
        float offset = 0;
        if (gridY % 2 != 0)
            offset = m_hexWidth / 2;
        float worldX =  (float)m_initPos.x + offset + gridX * m_hexWidth;
		
        //Every new line is offset in z direction by 3/4 of the hexagon length
        float worldZ = (float)m_initPos.z - gridY * m_hexLength * 0.75f;
        return new Vector3(worldX, 0, worldZ);
    }

    //Method to initialises and positions all the tiles
    private void CreateGrid()
    {
		//random seeder for testing
		System.Random testIntGenerator = new System.Random();
		if(m_mapGenDebugMode)
			GameObject.DestroyImmediate(GameObject.Find("HexGrids"));
        //Game object which is the parent of all the hex tiles
		if(GameObject.Find("HexGrids") == null)
		{
			InitDebugStates();
			InitList();
			//parent object to all the grids
        	GameObject hexGridGroup = new GameObject("HexGrids");
       		for (int y = 0; y < m_gridNumVer; y++)
       		{
				//alternating pattern
				int gridsToDraw = (y%2==0)?m_gridNumHor:m_gridNumHor-1;
				
            	for (int x = 0; x < gridsToDraw; x++)
            	{
                	GameObject hex = (GameObject)Instantiate(m_hexPrefab);
					//get world coordinates of grid
                	hex.transform.position = CalcWorldCoord(x,y);
					//assign parent
                	hex.transform.parent = hexGridGroup.transform;
					Vector2 grid2DPosition = new Vector2(hex.transform.position.x, hex.transform.position.z);
					//initialise model
					hex.GetComponent<HexGridModel>().Initialise(grid2DPosition,m_hexWidth,m_hexLength);
					
					//add mask objects to the grid as children
					GameObject blueMask = (GameObject)Instantiate(m_hexBlueMaskPrefab);
					GameObject redMask = (GameObject)Instantiate(m_hexRedMaskPrefab);
					GameObject greenMask = (GameObject)Instantiate(m_hexGreenMaskPrefab);
					GameObject outlineMask = (GameObject)Instantiate(m_hexOutlineMaskPrefab);
					
					hex.GetComponent<MaskManager>().InitMasks(redMask,greenMask,blueMask,outlineMask);
					
					//set the test bit in terrain component of hexmap to test reference
					if(m_editorDebugMode)
					{
						int testInt = testIntGenerator.Next(0,m_gridNumHor);
						//test case set to 1
						if(testInt == 1)
							m_hexGridRefTestNum ++;
						(hex.GetComponent<TestAttribute>() as TestAttribute).testNum = testInt;
					}
					m_grids.Add(hex);
            	}
        	}
		}
    }
	
	private void TestListSerielisation()
	{
		bool passedTest = true;
		foreach(GameObject e in m_grids){
			if(e == null){
				passedTest = false;
			}
		}
		
		if(!passedTest)
			Debug.Log("Failed TestListSerielisation()");
		
		/*
		foreach(GameObject e in m_grids)
		{
			Debug.Log(e);
		}*/
	}
	
	private void TestReferenceSerielisation()
	{
		int numTestInt = 0;
		//test case set to 1
		foreach(GameObject e in m_grids)
		{
			if((e.GetComponent<TestAttribute>() as TestAttribute).testNum == 1)
				numTestInt ++;
		}
		if(numTestInt!= m_hexGridRefTestNum)
			Debug.Log ("Failed TestReferenceSerielisation()");
	}

    //The grid should be generated on game start
    void Start()
    {
        GetGridSize();
		CalcInitPos();
        CreateGrid();
		
		if(m_editorDebugMode){
			TestListSerielisation();
			TestReferenceSerielisation();
		}
    }
}
