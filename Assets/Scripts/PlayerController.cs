using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{
    private Vector3 originalPos, targetPos;

    private UndoRedoManager undoRedoManager;
    private GridManager gridManager;
    public bool isMoving = false;

    void Awake()
    {
        undoRedoManager = GetComponent<UndoRedoManager>();
        gridManager = FindObjectOfType<GridManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isMoving)
        {
            // Horizontal Movement
            if (Input.GetButtonDown("Horizontal"))
            {
                if (Input.GetAxisRaw("Horizontal") == 1) // Movement to the Right
                {
                    MovePlayer(Vector3.right);
                }
                else // Movement to the Left
                {
                    MovePlayer(Vector3.left);
                }

            }
            // Vertical Movement
            if (Input.GetButtonDown("Vertical"))
            {
                if (Input.GetAxisRaw("Vertical") == 1) // Movement Up
                {
                    MovePlayer(Vector3.up);

                }
                else // Movement Down
                {
                    MovePlayer(Vector3.down);
                }
            }
            // Reset
            if (Input.GetKeyDown(KeyCode.R))
            {
                gridManager.ResetLevel();
            }
            // Undo
            if (Input.GetKeyDown(KeyCode.Z))
            {
                undoRedoManager.UndoMove();
            }// Redo
            else if (Input.GetKeyDown(KeyCode.X))
            {
                undoRedoManager.RedoMove();
            }
        }
    }

    public Vector3 PlayerPos()
    {
        return transform.position;
    }
    private void MovePlayer(Vector3 direction)
    {
        originalPos = transform.position;
        targetPos = originalPos + direction;
        var cell = gridManager.GetCellAtPosition(targetPos);

        if (cell.tile == null && cell.isPassable)
        {
            // Just the Player Moved
            gridManager.MoveCelltoPosition(transform.gameObject, targetPos);
            // Undo Redo Manager stuff
            undoRedoManager.AddAction(transform.gameObject, originalPos, targetPos);
        }
        else if(cell.tile.tag == "Crate")
        {
            var crateTargetPos = targetPos + direction;

            var secondCell = gridManager.GetCellAtPosition(crateTargetPos);

            // Crate and Player Moved
            if(secondCell.tile == null || secondCell.tile.tag == "Target") 
            {
                gridManager.MoveCelltoPosition(cell.tile, crateTargetPos);
                gridManager.MoveCelltoPosition(transform.gameObject, targetPos);              

                // Check for Win Condition
                if (secondCell.isTarget)
                {
                    gridManager.CheckWinCondition();

                    if (CheckTwobyTwo(crateTargetPos))
                        Debug.Log("Tip: You can't win anymore");
                }
                else
                {
                    if (InvalidBox(crateTargetPos, direction))
                        Debug.Log("Tip: You can't win anymore");
                }
                // Undo Redo Manager stuff
                undoRedoManager.AddAction(transform.gameObject, originalPos, targetPos, cell.tile, targetPos, crateTargetPos);
            }
        }
        //Debug.Log(PlayerPos());
    }
    public bool InvalidBox(Vector3 targetPos, Vector3 direction)
    {
        if (IsInCorner(targetPos))
            return true;
        if (IsInDeadEnd(targetPos, direction))
            return true;
        if (CheckFourCrates(targetPos))
            return true;
        if (CheckTwobyTwo(targetPos))
            return true;

        return false;
    }
    private bool IsInCorner(Vector3 targetPos)
    {
        Vector3 upPos = targetPos + Vector3.up;
        Vector3 rightPos = targetPos + Vector3.right;
        Vector3 leftPos = targetPos + Vector3.left;
        Vector3 downPos = targetPos + Vector3.down;

        // First we check the top. If there is a wall we check right and left. If there aren't any we just return false.If even one is a wall we return true
        // If top is not a wall we check right - we check bot and if bot is a wall we return true else we return false

        if (CheckforWall(upPos))
        {
            if (CheckforWall(rightPos) || CheckforWall(leftPos))
                return true;
            else
                return false;
        }
        if (CheckforWall(rightPos))
        {
            if (CheckforWall(downPos))
                return true;
            else
                return false;
        }
        else
        {
            if (CheckforWall(downPos))
            {
                return CheckforWall(leftPos);
            }
            else
                return false;
        }
    }
    private bool IsInDeadEnd(Vector3 targetPos, Vector3 direction)
    {
        // The question here is how do we detect on which position we have a wall
        // While checking the transform's position and the targetPos we can determine which direction we are headed
        // With the direction we can check one slot ahead to check if there is a wall since corners are already handled
        // If there is a wall now we need a way of going along the other axis.
        // If we move the box along the X-axis and the next position is a wall then we check the Y-axix and vice versa

        if (CheckforWall(targetPos + direction))
        {
            // Has Moved along X axis - Loop over the Y axis for wall
            if (direction.x != 0)
            {
                return CheckWallDeadEnd(targetPos, targetPos + direction, Vector3.up);
            }
            // Has Moved along Y axis - Loop over the X axis for wall
            if (direction.y != 0)
            {
                return CheckWallDeadEnd(targetPos, targetPos + direction, Vector3.left);
            }
            return false;
        }
        else
            return false;
    }
    private bool CheckWallDeadEnd(Vector3 blockPos, Vector3 wallPos, Vector3 axis)
    {
        // Loop in both directions along the specified axis
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 checkPos = blockPos;
            Vector3 checkWallPos = wallPos;

            while (true)
            {

                checkPos += axis * i;
                checkWallPos += axis * i;

                // Check if the wall ends
                if (!CheckforWall(checkWallPos))
                {
                    return false; // Wall doesn't end, not a dead end
                }

                // Check for a target
                if (CheckforTarget(checkPos))
                {
                    return false; // Target found, not a dead end
                }
                // Check for corners
                if (IsInCorner(checkPos))
                {
                    break; // Corner found, break the loop and continue with the other direction
                }
            }
        }

        return true; // Wall ends in corners, dead end
    }
    private bool CheckFourCrates(Vector3 cratePos)
    {
        // The problem here comes with the box placement since we are looking for 4 crates next to each other and once a crate has been moved we need to check
        // all 8 surrounding spaces for crates and potentially count until we reach 3 in a row
        // The other solutions is to directly check 3 spaces at a time 4 times for the 4 possible blocks
        int[] xOffsetOrder = { -1, -1, 0, 1, 1, 1, 0, -1 };
        int[] yOffsetOrder = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int cratecount = 0;
        for (int i = 0; i < xOffsetOrder.Length; i++)
        {
            int xOffset = xOffsetOrder[i];
            int yOffset = yOffsetOrder[i];

            Vector3 checkPos = cratePos + new Vector3(xOffset, yOffset, 0f);
            if (CheckforCrate(checkPos))
                cratecount++;
            else
                cratecount = 0;

            if (cratecount == 3)
                return true;

        }
        return false;
    }
    private bool CheckTwobyTwo(Vector3 cratePos)
    {
        int[] xOffsetOrder = { -1, -1, 0, 1, 1, 1, 0, -1 };
        int[] yOffsetOrder = { 0, 1, 1, 1, 0, -1, -1, -1 };

        for (int i = 0; i < xOffsetOrder.Length; i++)
        {
            switch (i)
            {
                case 0:
                    if (CheckforCrate(cratePos + Vector3.left) && !gridManager.GetCellAtPosition(cratePos + Vector3.left).isTarget)
                    {
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[1], yOffsetOrder[1], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[2], yOffsetOrder[2], 0f)))
                                return true;
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[6], yOffsetOrder[6], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[7], yOffsetOrder[7], 0f)))
                                return true;
                    }
                    break;
                case 2:
                    if (CheckforCrate(cratePos + Vector3.up) && !gridManager.GetCellAtPosition(cratePos + Vector3.up).isTarget)
                    {
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[0], yOffsetOrder[0], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[1], yOffsetOrder[1], 0f)))
                                return true;
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[3], yOffsetOrder[3], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[4], yOffsetOrder[4], 0f)))
                                return true;
                    }
                    break;
                case 4:
                    if (CheckforCrate(cratePos + Vector3.right) && !gridManager.GetCellAtPosition(cratePos + Vector3.right).isTarget)
                    {
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[2], yOffsetOrder[2], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[3], yOffsetOrder[3], 0f)))
                                return true;
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[5], yOffsetOrder[5], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[6], yOffsetOrder[6], 0f)))
                                return true;
                    }
                    break;
                case 6:
                    if (CheckforCrate(cratePos + Vector3.down) && !gridManager.GetCellAtPosition(cratePos + Vector3.down).isTarget)
                    {
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[0], yOffsetOrder[0], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[7], yOffsetOrder[7], 0f)))
                                return true;
                        if (CheckforWall(cratePos + new Vector3(xOffsetOrder[4], yOffsetOrder[4], 0f)))
                            if (CheckforWall(cratePos + new Vector3(xOffsetOrder[5], yOffsetOrder[5], 0f)))
                                return true;
                    }
                    break;
            }
        }
        return false;
    }
    /*
    private bool CheckTwobyTwo(Vector3 cratePos)
    {
        int[] xOffsetOrder = { -1, -1, 0, 1, 1, 1, 0, -1 };
        int[] yOffsetOrder = { 0, 1, 1, 1, 0, -1, -1, -1 };

        int cratecount = 0;
        int wallcount = 0;

        for (int i = 0; i < xOffsetOrder.Length; i++)
        {
            int xOffset = xOffsetOrder[i];
            int yOffset = yOffsetOrder[i];

            Vector3 checkPos = cratePos + new Vector3(xOffset, yOffset, 0f);
            //Debug.Log($"Checking for 2 by 2 at {checkPos}");

            if (CheckforCrate(checkPos))
            {
                Debug.Log($"Found Crate at: {checkPos}");
                if (cratecount == 0)
                    cratecount++;
                else
                    cratecount = 0;
            }
            else if (CheckforWall(checkPos))
            {
                Debug.Log($"Found Wall at: {checkPos}");
                if (wallcount != 2)
                    wallcount++;
                else
                    wallcount = 0;
            }
            else
            {
                wallcount = 0;
                cratecount = 0;
            }

            if (cratecount == 1 && wallcount == 2)
                return true;
        }
        return false;
    }
    */

    private bool CheckforWall(Vector3 targetPos)
    {

        var cell = gridManager.GetCellAtPosition(targetPos).tile;

        if(cell == null) 
            return false;

        return gridManager.GetCellAtPosition(targetPos).tile.CompareTag("Wall");
    }
    private bool CheckforCrate(Vector3 targetPos)
    {

        var cell = gridManager.GetCellAtPosition(targetPos).tile;

        if (cell == null)
            return false;

        return gridManager.GetCellAtPosition(targetPos).tile.CompareTag("Crate");
    }
    private bool CheckforTarget(Vector3 targetPos)
    {
        return gridManager.GetCellAtPosition(targetPos).isTarget;
    }
}
