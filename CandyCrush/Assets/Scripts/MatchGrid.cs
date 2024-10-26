using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchGrid : MonoBehaviour
{
    //define the size of the board
    public int width = 8;
    public int height = 8;
    //define some spacing for the board
    public float spacingX;
    public float spacingY;
    //get a reference to our block prefabs
    public GameObject[] blockPrefabs;
    //get a reference to the collection nodes blockBoard + GO
    public Node[,] grid;
    public GameObject gridGameObject;

    public List<GameObject> blocksToDestroy = new();
    public GameObject blockParent;

    [SerializeField]
    private Block selectedBlock;

    [SerializeField]
    private bool isProcessingMove;

    [SerializeField]
    List<Block> blocksToRemove = new();


    //layoutArray
    public ArrayLayout arrayLayout;
    //public static of blockboard
    public static MatchGrid Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeBoard();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject.GetComponent<Block>())
            {
                if (isProcessingMove)
                    return;

                Block block = hit.collider.gameObject.GetComponent<Block>();
                Debug.Log("I have a clicked a block it is: " + block.gameObject);

                SelectBlock(block);
            }
        }
    }

    void InitializeBoard()
    {
        DestroyBlocks();
        grid = new Node[width, height];

        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY);
                if (arrayLayout.rows[y].row[x])
                {
                    grid[x, y] = new Node(null);
                }
                else
                {
                    int randomIndex = Random.Range(0, blockPrefabs.Length);

                    GameObject block = Instantiate(blockPrefabs[randomIndex], position, Quaternion.identity);
                    block.transform.SetParent(blockParent.transform);
                    block.GetComponent<Block>().SetPosition(x, y);
                    grid[x, y] = new Node(block);
                    blocksToDestroy.Add(block);
                }
            }
        }
        
        if (GridCheck())
        {
            Debug.Log("We have matches let's re-create the board");
            InitializeBoard();
        }
        else
        {
            Debug.Log("There are no matches, it's time to start the game!");
        }
    }

    private void DestroyBlocks()
    {
        if (blocksToDestroy != null)
        {
            foreach (GameObject block in blocksToDestroy)
            {
                Destroy(block);
            }
            blocksToDestroy.Clear();
        }
    }

    public bool GridCheck()
    {
        if (GameManager.Instance.isGameOver)
            return false;
        Debug.Log("Checking Board");
        bool hasMatched = false;

        blocksToRemove.Clear();

        foreach(Node node in grid)
        {
            if (node.block != null)
            {
                node.block.GetComponent<Block>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
            
                    //then proceed to get block class in node.
                    Block block = grid[x, y].block.GetComponent<Block>();

                    //ensure its not matched
                    if(!block.isMatched)
                    {
                        //run some matching logic

                        MatchResult matchedBlocks = IsConnected(block);

                        if (matchedBlocks.connectedBlocks.Count >= 3)
                        {
                            MatchResult matchedComboBlocks = ComboMatch(matchedBlocks);

                            blocksToRemove.AddRange(matchedComboBlocks.connectedBlocks);

                            foreach (Block pot in matchedComboBlocks.connectedBlocks)
                                pot.isMatched = true;

                            hasMatched = true;
                        }
                    }
            }
        }

        return hasMatched;
    }

    public IEnumerator ProcessTurnOnMatchedBoard(bool subtractMoves)
    {
        foreach (Block blockToRemove in blocksToRemove)
        {
            blockToRemove.isMatched = false;
        }

        RemoveAndSpawnNewBlocks(blocksToRemove);
        GameManager.Instance.ProcessTurn(blocksToRemove.Count, subtractMoves);
        yield return new WaitForSeconds(0.4f);

        if (GridCheck())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(false));
        }
    }

    private void RemoveAndSpawnNewBlocks(List<Block> blocksToRemove)
    {
        //Removing the block and clearing the board at that location
        foreach (Block block in blocksToRemove)
        {
            //getting it's x and y indicies and storing them
            int xIndex = block.xIndex;
            int yIndex = block.yIndex;

            //Destroy the block
            Destroy(block.gameObject);

            //Create a blank node on the block board.
            grid[xIndex, yIndex] = new Node(null);
        }

        for (int x=0; x < width; x++)
        {
            for (int y=0; y <height; y++)
            {
                if (grid[x, y].block == null)
                {
                    Debug.Log("The location X: " + x + " Y: " + y + " is empty, attempting to refill it.");
                    SpawnNewBlock(x, y);
                }
            }
        }
    }

    private void SpawnNewBlock(int x, int y)
    {
        //y offset
        int vertSpace = 1;

        //while the cell above our current cell is null and we're below the height of the board
        while (y + vertSpace < height && grid[x,y + vertSpace].block == null)
        {
            //increment y offset
            Debug.Log("The block above me is null, Current Offset is: " + vertSpace + " I'm about to add 1.");
            vertSpace++;
        }

        //we've either hit the top of the board or we found a block

        if (y + vertSpace < height && grid[x, y + vertSpace].block != null)
        {
            //we've found a block

            Block hoveringBlock = grid[x, y + vertSpace].block.GetComponent<Block>();

            //Move it to the correct location
            Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, hoveringBlock.transform.position.z);
            Debug.Log("I've found a block when refilling the board and it was in the location: [" + x + "," + (y + vertSpace) + "] we have moved it to the location: [" + x + "," + y + "]");
            //Move to location
            hoveringBlock.MoveToTarget(targetPos);
            //update incidices
            hoveringBlock.SetPosition(x, y);
            //update our blockBoard
            grid[x, y] = grid[x, y + vertSpace];
            //set the location the block came from to null
            grid[x, y + vertSpace] = new Node(null);
        }

        //if we've hit the top of the board without finding a block
        if (y + vertSpace == height)
        {
            Debug.Log("I've reached the top of the board without finding a block");
            SpawnBlockAtTop(x);
        }

    }

    private void SpawnBlockAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);
        int locationToMoveTo = 8 - index;
        Debug.Log("About to spawn a block, ideally i'd like to put it in the index of: " + index);
        //get a random block
        int randomIndex = Random.Range(0, blockPrefabs.Length);
        GameObject newBlock = Instantiate(blockPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
        newBlock.transform.SetParent(blockParent.transform);
        //set indicies
        newBlock.GetComponent<Block>().SetPosition(x, index);
        //set it on the block board
        grid[x, index] = new Node(newBlock);
        //move it to that location
        Vector3 targetPosition = new Vector3(newBlock.transform.position.x, newBlock.transform.position.y - locationToMoveTo, newBlock.transform.position.z);
        newBlock.GetComponent<Block>().MoveToTarget(targetPosition);
    }

    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = 99;
        for (int y = 7; y >= 0; y--)
        {
            if (grid[x,y].block == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }


    #region MatchingLogic
    private MatchResult ComboMatch(MatchResult matchedResults)
    {
        //if we have a horizontal or long horizontal match
        if (matchedResults.direction == MatchDirection.Horizontal || matchedResults.direction == MatchDirection.LongHorizontal)
        {
            //for each block...
            foreach (Block block in matchedResults.connectedBlocks)
            {
                List<Block> extraConnectedBlocks = new();
                //check up
                CheckDirection(block, new Vector2Int(0, 1), extraConnectedBlocks);
                //check down
                CheckDirection(block, new Vector2Int(0, -1), extraConnectedBlocks);

                //do we have 2 or more blocks that have been matched against this current block.
                if (extraConnectedBlocks.Count >= 2)
                {
                    Debug.Log("I have a combo Horizontal Match");
                    extraConnectedBlocks.AddRange(matchedResults.connectedBlocks);

                    //return our combo match
                    return new MatchResult
                    {
                        connectedBlocks = extraConnectedBlocks,
                        direction = MatchDirection.Combo,
                        blockType = matchedResults.blockType
                    };
                }
            }
            //we didn't have a combo match, so return our normal match
            return new MatchResult
            {
                connectedBlocks = matchedResults.connectedBlocks,
                direction = matchedResults.direction,
                blockType = matchedResults.blockType
            };
        }
        else if (matchedResults.direction == MatchDirection.Vertical || matchedResults.direction == MatchDirection.LongVertical)
        {
            //for each block...
            foreach (Block block in matchedResults.connectedBlocks)
            {
                List<Block> extraConnectedBlocks = new();
                //check right
                CheckDirection(block, new Vector2Int(1, 0), extraConnectedBlocks);
                //check left
                CheckDirection(block, new Vector2Int(-1, 0), extraConnectedBlocks);

                //do we have 2 or more blocks that have been matched against this current block.
                if (extraConnectedBlocks.Count >= 2)
                {
                    Debug.Log("I have a combo Vertical Match");
                    extraConnectedBlocks.AddRange(matchedResults.connectedBlocks);
                    //return our combo match
                    return new MatchResult
                    {
                        connectedBlocks = extraConnectedBlocks,
                        direction = MatchDirection.Combo,
                        blockType = matchedResults.blockType
                    };
                }
            }
            //we didn't have a combo match, so return our normal match
            return new MatchResult
            {
                connectedBlocks = matchedResults.connectedBlocks,
                direction = matchedResults.direction,
                blockType = matchedResults.blockType
            };
        }
        return null;
    }

    MatchResult IsConnected(Block block)
    {
        List<Block> connectedBlocks = new();
        BlockType blockType = block.blockType;

        connectedBlocks.Add(block);

        //check right
        CheckDirection(block, new Vector2Int(1, 0), connectedBlocks);
        //check left
        CheckDirection(block, new Vector2Int(-1, 0), connectedBlocks);
        //have we made a 3 match? (Horizontal Match)
        if (connectedBlocks.Count == 3)
        {
            Debug.Log("I have a normal horizontal match, the color of my match is: " + connectedBlocks[0].blockType);

            return new MatchResult
            {
                connectedBlocks = connectedBlocks,
                direction = MatchDirection.Horizontal,
                blockType = blockType
            };
        }
        //checking for more than 3 (Long horizontal Match)
        else if (connectedBlocks.Count > 3)
        {
            Debug.Log("I have a Long horizontal match, the color of my match is: " + connectedBlocks[0].blockType);

            return new MatchResult
            {
                connectedBlocks = connectedBlocks,
                direction = MatchDirection.LongHorizontal,
                blockType = blockType
            };
        }
        //clear out the connectedblocks
        connectedBlocks.Clear();
        //readd our initial block
        connectedBlocks.Add(block);

        //check up
        CheckDirection(block, new Vector2Int(0, 1), connectedBlocks);
        //check down
        CheckDirection(block, new Vector2Int(0,-1), connectedBlocks);

        //have we made a 3 match? (Vertical Match)
        if (connectedBlocks.Count == 3)
        {
            Debug.Log("I have a normal vertical match, the color of my match is: " + connectedBlocks[0].blockType);

            return new MatchResult
            {
                connectedBlocks = connectedBlocks,
                direction = MatchDirection.Vertical,
                blockType = blockType
            };
        }
        //checking for more than 3 (Long Vertical Match)
        else if (connectedBlocks.Count > 3)
        {
            Debug.Log("I have a Long vertical match, the color of my match is: " + connectedBlocks[0].blockType);

            return new MatchResult
            {
                connectedBlocks = connectedBlocks,
                direction = MatchDirection.LongVertical,
                blockType = blockType
            };
        } else
        {
            return new MatchResult
            {
                connectedBlocks = connectedBlocks,
                direction = MatchDirection.None
            };
        }
    }

    void CheckDirection(Block block, Vector2Int direction, List<Block> connectedBlocks)
    {
        BlockType blockType = block.blockType;
        int x = block.xIndex + direction.x;
        int y = block.yIndex + direction.y;

        //check that we're within the boundaries of the board
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            Block neighbourBlock = grid[x, y].block.GetComponent<Block>();

            //does our blockType Match? it must also not be matched
            if(!neighbourBlock.isMatched && neighbourBlock.blockType == blockType)
            {
                connectedBlocks.Add(neighbourBlock);

                x += direction.x;
                y += direction.y;
            }
            else
            {
                break;
            }
        }
    }
    #endregion

    #region Swapping Blocks

    //select block
    public void SelectBlock(Block block)
    {
        // if we don't have a block currently selected, then set the block i just clicked to my selectedblock
        if (selectedBlock == null)
        {
            Debug.Log(block);
            selectedBlock = block;
        }
        // if we select the same block twice, then let's make selectedblock null
        else if (selectedBlock == block)
        {
            selectedBlock = null;
        }
        //if selectedblock is not null and is not the current block, attempt a swap
        //selectedblock back to null
        else if (selectedBlock != block)
        {
            SwapBlock(selectedBlock, block);
            selectedBlock = null;
        }
    }
    //swap block - logic
    private void SwapBlock(Block currentBlock, Block targetBlock)
    {
        if (!IsAdjacent(currentBlock, targetBlock))
        {
            return;
        }

        DoSwap(currentBlock, targetBlock);

        isProcessingMove = true;

        StartCoroutine(ProcessMatches(currentBlock, targetBlock));
    }
    //do swap
    private void DoSwap(Block currentBlock, Block targetBlock)
    {
        GameObject temp = grid[currentBlock.xIndex, currentBlock.yIndex].block;

        grid[currentBlock.xIndex, currentBlock.yIndex].block = grid[targetBlock.xIndex, targetBlock.yIndex].block;
        grid[targetBlock.xIndex, targetBlock.yIndex].block = temp;

        //update indicies.
        int tempXIndex = currentBlock.xIndex;
        int tempYIndex = currentBlock.yIndex;
        currentBlock.xIndex = targetBlock.xIndex;
        currentBlock.yIndex = targetBlock.yIndex;
        targetBlock.xIndex = tempXIndex;
        targetBlock.yIndex = tempYIndex;

        currentBlock.MoveToTarget(grid[targetBlock.xIndex, targetBlock.yIndex].block.transform.position);

        targetBlock.MoveToTarget(grid[currentBlock.xIndex, currentBlock.yIndex].block.transform.position);
    }

    private IEnumerator ProcessMatches(Block currentBlock, Block targetBlock)
    {
        yield return new WaitForSeconds(0.4f);

        if (GridCheck())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        }
        else
        {
            DoSwap(currentBlock, targetBlock);
        }
        isProcessingMove = false;
    }


    //IsAdjacent
    private bool IsAdjacent(Block currentBlock, Block targetBlock)
    {
        return Mathf.Abs(currentBlock.xIndex - targetBlock.xIndex) + Mathf.Abs(currentBlock.yIndex - targetBlock.yIndex) == 1;
    }

    //ProcessMatches

    #endregion

}

public class MatchResult
{
    public List<Block> connectedBlocks;
    public MatchDirection direction;
    public BlockType blockType;
}

public enum MatchDirection
{
    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Combo,
    None
}


