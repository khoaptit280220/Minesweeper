using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] state;
    private bool gameover;

    private void OnValidate()
    {
        mineCount = width + height;
        //mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        board = GetComponentInChildren<Board>();
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        state = new Cell[width, height];
        gameover = false;
        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2, height / 2, -10f);

        board.Draw(state);
    }

    private void GenerateCells()
    {
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            var cell = new Cell();
            cell.position = new Vector3Int(i, j, 0);
            cell.type = Cell.Type.Empty;
            state[i, j] = cell;
        }
    }

    private void GenerateMines()
    {
        for (var i = 0; i < mineCount; i++)
        {
            var x = Random.Range(0, width);
            var y = Random.Range(0, height);

            while (state[x, y].type == Cell.Type.Mine)
            {
                x++;

                if (x >= width)
                {
                    x = 0;
                    y++;

                    if (y >= height) y = 0;
                }
            }

            state[x, y].type = Cell.Type.Mine;
            //  state[x, y].revealed = true;
        }
    }

    private void GenerateNumbers()
    {
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            var cell = state[i, j];

            if (cell.type == Cell.Type.Mine) continue;

            cell.number = CountMines(i, j);

            if (cell.number > 0) cell.type = Cell.Type.Number;

            //cell.revealed = true;
            state[i, j] = cell;
        }
    }

    private int CountMines(int cellX, int cellY)
    {
        var count = 0;

        for (var adjacentX = -1; adjacentX <= 1; adjacentX++)
        for (var adjacentY = -1; adjacentY <= 1; adjacentY++)
        {
            if (adjacentX == 0 && adjacentY == 0) continue;
            var x = cellX + adjacentX;
            var y = cellY + adjacentY;

            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            if (GetCell(x, y).type == Cell.Type.Mine) count++;
        }

        return count;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            NewGame();
        }
        else  if (!gameover)
        {
            if (Input.GetMouseButtonDown(1)) Flag();
            else if (Input.GetMouseButtonDown(0)) Reveal();
        }
    }

    private void Flag()
    {
        var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var cellPosition = board.tilemap.WorldToCell(worldPosition);
        var cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed) return;

        cell.flagged = !cell.flagged;
        state[cellPosition.x, cellPosition.y] = cell;
        board.Draw(state);
    }

    private void Reveal()
    {
        var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var cellPosition = board.tilemap.WorldToCell(worldPosition);
        var cell = GetCell(cellPosition.x, cellPosition.y);

        if (cell.type == Cell.Type.Invalid || cell.revealed || cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Mine:
                Explode(cell);
                break;
            case Cell.Type.Empty:
                Flood(cell);
                CheckWinConditiion();
                break;
            default:
                cell.revealed = true;
                state[cellPosition.x, cellPosition.y] = cell;
                CheckWinConditiion();
                break;
        }

        board.Draw(state);
    }

    private void Flood(Cell cell)
    {
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) return;

        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (cell.type == Cell.Type.Empty)
        {
            Flood(GetCell(cell.position.x - 1, cell.position.y));
            Flood(GetCell(cell.position.x + 1, cell.position.y));
            Flood(GetCell(cell.position.x, cell.position.y - 1));
            Flood(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell)
    {
        gameover = true;

        cell.revealed = true;
        cell.exploded = true;

        state[cell.position.x, cell.position.y] = cell;
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            cell = state[i, j];
            if (cell.type == Cell.Type.Mine)
            {
                cell.revealed = true;
                state[i, j] = cell;
            }
        }
    }

    private void CheckWinConditiion()
    {
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            var cell = state[i, j];

            if (cell.type != Cell.Type.Mine && !cell.revealed) return;
        }

        gameover = true;
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            var cell = state[i, j];
            if (cell.type == Cell.Type.Mine)
            {
                cell.flagged = true;
                state[i, j] = cell;
            }
        }
    }

    private Cell GetCell(int x, int y)
    {
        if (IsValue(x, y))
            return state[x, y];
        else
            return new Cell();
    }

    private bool IsValue(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}