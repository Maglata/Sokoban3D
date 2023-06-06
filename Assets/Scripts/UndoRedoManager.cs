using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager : MonoBehaviour
{
    private Stack<MoveAction> undoStack;
    private Stack<MoveAction> redoStack;
    public GridManager gridManager;

    public void Awake()
    {
        undoStack = new Stack<MoveAction>();
        redoStack = new Stack<MoveAction>();
        gridManager = FindObjectOfType<GridManager>();
    }

    public void AddAction(
        GameObject player, Vector3 playerOriginalPosition, Vector3 playerTargetPosition,
        GameObject crate = null, Vector3? crateOriginalPosition = null, Vector3? crateTargetPosition = null
        )
    {
        MoveAction moveAction = new MoveAction(
            player, playerOriginalPosition, playerTargetPosition,
            crate, crateOriginalPosition, crateTargetPosition
            );
        undoStack.Push(moveAction);
        redoStack.Clear(); // Clear redo stack when a new move action is added
    }

    public void UndoMove()
    {
        if (undoStack.Count > 0)
        {
            MoveAction moveAction = undoStack.Pop();
            moveAction.Undo(gridManager);
            redoStack.Push(moveAction);
        }
        else
            Debug.Log("No Undo Action Available");
    }

    public void RedoMove()
    {
        if (redoStack.Count > 0)
        {
            MoveAction moveAction = redoStack.Pop();
            moveAction.Redo(gridManager);
            undoStack.Push(moveAction);
        }
        else
            Debug.Log("No Redo Action Available");
    }
}

public class MoveAction
{
    //Player Action
    private GameObject _player;
    private Vector3 _playerOriginalPosition;
    private Vector3 _playerTargetPosition;

    //Crate Action
    private GameObject _crate = null;
    private Vector3? _crateOriginalPosition;
    private Vector3? _crateTargetPosition;

    public MoveAction(GameObject player, Vector3 playerOriginalPosition, Vector3 playerTargetPosition, 
        GameObject crate = null, Vector3? crateOriginalPosition = null, Vector3? crateTargetPosition = null)
    {
        _player = player;
        _playerOriginalPosition = playerOriginalPosition;
        _playerTargetPosition = playerTargetPosition;

        _crate = crate;
        _crateOriginalPosition = crateOriginalPosition;
        _crateTargetPosition = crateTargetPosition;
    }

    public void Undo(GridManager gridManager)
    {
        // Revert the move by setting the game object's position back to the original position

        var cell = gridManager.GetCellAtPosition(_playerTargetPosition);

        gridManager.MoveCelltoPosition(cell.tile, _playerOriginalPosition);

        if (_crate != null && _crateOriginalPosition.HasValue)
        {
            var cellCrate = gridManager.GetCellAtPosition(_crateTargetPosition.Value);

            gridManager.MoveCelltoPosition(cellCrate.tile, _crateOriginalPosition.Value);
        }
    }

    public void Redo(GridManager gridManager)
    {
        // Reapply the move by setting the game object's position to the target position
        if (_crate != null && _crateTargetPosition.HasValue)
        {
            var cellCrate = gridManager.GetCellAtPosition(_crateOriginalPosition.Value);

            gridManager.MoveCelltoPosition(cellCrate.tile, _crateTargetPosition.Value);
        }
        var cell = gridManager.GetCellAtPosition(_playerOriginalPosition);

        gridManager.MoveCelltoPosition(cell.tile, _playerTargetPosition);       
    }
}
