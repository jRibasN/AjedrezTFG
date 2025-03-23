using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public enum SpecialMove{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.6f;
    [SerializeField] private float yOffset = 0.37f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece CurrentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> allTeamPieces = new List<ChessPiece>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    // Multiplayer logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = false;
    private bool computerGame = false;
    private bool[] playerRematch = new bool[2];
    
    private void Start() {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        
        SpawnAllPieces();
        PositionAllPieces();

        isWhiteTurn = true;

        RegisterEvents();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            // Get the indexes of the tile i've hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // If we're hovering a tile after not hovering any tiles
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we were already hovering a tile, change the previous one
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we press down on the mouse
            if (Input.GetMouseButtonDown(0)){
                if (chessPieces[hitPosition.x, hitPosition.y] != null){
                    // Is it our turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || 
                        (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)){
                        CurrentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        // Get a list of where I can go, highlight tiles as well
                        availableMoves = CurrentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        // Get a list of special moves as well
                        specialMove = CurrentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        PreventCheck(CurrentlyDragging.team, CurrentlyDragging, ref availableMoves);
                        HighlightTiles();
                    }
                }
            }

            // If we release the mouse button
            if (CurrentlyDragging != null && Input.GetMouseButtonUp(0)){
                Vector2Int previousPosition = new Vector2Int(CurrentlyDragging.currentX, CurrentlyDragging.currentY);

                if(ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y))){
                    MoveTo(chessPieces, previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y, ref isWhiteTurn, deadWhites, deadBlacks, ref moveList, specialMove);

                    // Net implementation
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = previousPosition.x;
                    mm.originalY = previousPosition.y;
                    mm.destinationX = hitPosition.x;
                    mm.destinationY = hitPosition.y;
                    mm.teamId = currentTeam;
                    Client.Instance.SendToServer(mm);
                }

                else{
                    CurrentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    CurrentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
        }
            
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(CurrentlyDragging && Input.GetMouseButtonUp(0)){
                CurrentlyDragging.SetPosition(GetTileCenter(CurrentlyDragging.currentX, CurrentlyDragging.currentY));
                CurrentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        // If we´re dragging a piece
        if (CurrentlyDragging){
            Plane horizontalPlane = new Plane (Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                CurrentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * 0.2f);
        }

        if (computerGame){
            if ((!isWhiteTurn && currentTeam == 0) || (isWhiteTurn && currentTeam == 1)){
                //SelectRandomMove();
                ComputerV1(1);
            }
        }
    }

    //Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;
        
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x,y] = GenerateSingleTile(tileSize, x, y);
        
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;


        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spawning of the pieces
    private void SpawnAllPieces(){
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //White team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam, 5);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam, 3);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam, 3);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam, 9);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam, 1000);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam, 3);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam, 3);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam, 5);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam, 1);
        }

        //Black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam, 5);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam, 3);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam, 3);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam, 9);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam, 1000);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam, 3);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam, 3);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam, 5);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam, 1);
        }
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team, int value){
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.value = value;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //Positioning
    private void PositionAllPieces(){
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        if (chessPieces[x, y] != null)
        {
            chessPieces[x, y].currentX = x;
            chessPieces[x, y].currentY = y;
            chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
        }
    }

    private Vector3 GetTileCenter(int x, int y){
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // Highlight tiles
    private void HighlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    private void RemoveHighlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        
        availableMoves.Clear();
    }

    // Checkmate
    private void Checkmate(int team){
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam){
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void OnRematchButton(){
        if(localGame || computerGame){
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);
        }

        else{
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);
        }
    }

    public void GameReset(){
        // UI
        rematchButton.interactable = true;
        
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Fields reset

        CurrentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // Clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
        {
            Destroy(deadWhites[i].gameObject);
        }

        for (int i = 0; i < deadBlacks.Count; i++)
        {
            Destroy(deadBlacks[i].gameObject);
        }

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
        if(localGame) currentTeam = 0;
        else{
            currentTeam = (currentTeam == 0) ? 1 : 0;
            if(currentTeam == 0) GameUI.Instance.ChangeCamera(CameraAngle.whiteTeam);
            if(currentTeam == 1) GameUI.Instance.ChangeCamera(CameraAngle.blackTeam);
        } 
    }
    public void OnMenuButton(){
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        Invoke("ShutDownRelay", 0.1f);

        Invoke("GameReset", 0.11f);

        // Reset some values
        Invoke("OnSetMenu", 0.12f);

        GameUI.Instance.Invoke("OnLeaveFromGameMenu", 0.12f);
    }

    // SpecialMoves
    private void ProcessSpecialMove(List<Vector2Int[]> newMoveList, ChessPiece[,] board, SpecialMove specialMove1){
        if (specialMove1 == SpecialMove.EnPassant){
            Vector2Int[] newMove = newMoveList[newMoveList.Count - 1];
            ChessPiece myPawn = board[newMove[1].x, newMove[1].y];
            Vector2Int[] targetPawnPosition = newMoveList[newMoveList.Count - 2];
            ChessPiece enemyPawn = board[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enemyPawn.currentX){
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1){
                    if (enemyPawn.team == 0){
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                        - bounds
                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                        + Vector3.forward * 0.33f * deadWhites.Count);

                    }

                    else{
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                        - bounds
                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                        + Vector3.back * 0.33f * deadBlacks.Count);
                    }

                    board[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if (specialMove1 == SpecialMove.Castling){
            Vector2Int[] lastMove = newMoveList[newMoveList.Count - 1];

            // Left Rook
            if(lastMove[1].x == 2){
                if(lastMove[1].y == 0){ // White side
                    ChessPiece rook = board[0, 0];
                    board[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    board[0, 0] = null;
                }

                else if(lastMove[1].y == 7){ // Black side
                    ChessPiece rook = board[0, 7];
                    board[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    board[0, 7] = null;
                }
            }

            // Right Rook
            if(lastMove[1].x == 6){
                if(lastMove[1].y == 0){ // White side
                    ChessPiece rook = board[7, 0];
                    board[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    board[7, 0] = null;
                }

                else if(lastMove[1].y == 7){ // Black side
                    ChessPiece rook = board[7, 7];
                    board[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    board[7, 7] = null;
                }
            }
        }

        if (specialMove1 == SpecialMove.Promotion){
            Vector2Int[] lastMove = newMoveList[newMoveList.Count - 1];
            ChessPiece targetPawn = board[lastMove[1].x, lastMove[1].y];

            if (targetPawn.team == 0 && lastMove[1].y == 7){
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0, 9);
                newQueen.transform.position = board[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(board[lastMove[1].x, lastMove[1].y].gameObject);
                board[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }

            if (targetPawn.team == 1 && lastMove[1].y == 0){
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1, 9);
                newQueen.transform.position = board[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(board[lastMove[1].x, lastMove[1].y].gameObject);
                board[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }
        }
    
    }

    private void PreventCheck(int team, ChessPiece cp, ref List<Vector2Int> availableMoves){
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        if (chessPieces[x, y].team == team)
                            targetKing = chessPieces[x, y];
        
        // Since we´re sending ref availableMoves, we will be deleting moves that are putting us in check
        SimulateMoveForSinglePiece(cp, availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, List<Vector2Int> moves, ChessPiece targetKing){
        // Save the current values, to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Going through all the moves, simulate them and check if we´re in check
        for (int i = 0; i < moves.Count; i++)
        {
            int  simX = moves[i].x;
            int  simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // Did we simulate the king´s move
            if (cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX, simY);

            // Copy the [,] and not the reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x, y] != null){
                        simulation[x, y] = chessPieces[x, y];
                        if(simulation[x, y].team != cp.team)
                            simAttackingPieces.Add(simulation[x, y]);
                    }
                }
            }

            // Simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            // Did one of the pieces got taken down during our simulation
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
            simAttackingPieces.Remove(deadPiece);

            // Get all the simulated attacking pieces moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }

                // Is the king in trouble? if so, remove the move
                if(ContainsValidMove(ref simMoves, kingPositionThisSim)){
                    movesToRemove.Add(moves[i]);
                }

                // Restore the actual cp data
                cp.currentX = actualX;
                cp.currentY = actualY;
            }
        }

        // Remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }

    private bool CheckForCheckmate(){
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null){
                    if(chessPieces[x, y].team == targetTeam){
                        defendingPieces.Add(chessPieces[x, y]);
                        if(chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else{
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }

        // Is the king being attacked right now?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int> ();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }

        // Are we in check right now?
        if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY))){
            // King is under attack, can we move something to help him?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], defendingMoves, targetKing);

                if(defendingMoves.Count != 0)
                    return false;
            }

            return true; // Checkmate exit
        }

        return false;
    }

    // Operations
    private void SelectRandomMove(){
        Random rnd = new Random();

        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if (chessPieces[x, y] != null){
                    if (chessPieces[x, y].team != currentTeam){
                        availableMoves = chessPieces[x, y].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        specialMove = chessPieces[x, y].GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        PreventCheck((currentTeam == 0) ? 1 : 0, chessPieces[x, y], ref availableMoves);
                        if(availableMoves.Count > 0) allTeamPieces.Add(chessPieces[x, y]);
                        availableMoves.Clear();
                    }
                }
             }
        }

        if(allTeamPieces.Count > 0){
            ChessPiece cp = allTeamPieces[rnd.Next(allTeamPieces.Count)];

            availableMoves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = cp.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
            PreventCheck((currentTeam == 0) ? 1 : 0, cp, ref availableMoves);

            Vector2Int move = availableMoves[rnd.Next(availableMoves.Count)];

            MoveTo(chessPieces, cp.currentX, cp.currentY, move.x, move.y, ref isWhiteTurn, deadWhites, deadBlacks, ref moveList, specialMove);
        }

        allTeamPieces.Clear();
        availableMoves.Clear();
    }

    public void ComputerV1(int depth){
        int bestValueWhite = int.MinValue;
        int bestValueBlack = int.MaxValue;
        ChessPiece bestPiece = null;
        Vector2Int bestMove = Vector2Int.zero;
        SpecialMove bestSpecialMove = SpecialMove.None;
        int iterations = 0;

        // Iterar a través de todas las piezas del equipo del ordenador
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null && piece.team != currentTeam)
                {
                    List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    SpecialMove specialMovee = piece.GetSpecialMoves(ref chessPieces, ref moveList, ref pieceMoves);
                    PreventCheck((currentTeam == 0) ? 1 : 0, piece, ref pieceMoves);

                    // Evaluar cada movimiento
                    foreach (Vector2Int move in pieceMoves)
                    {
                        ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
                        for (int i = 0; i < TILE_COUNT_X; i++)
                        {
                            for (int j = 0; j < TILE_COUNT_Y; j++)
                            {
                                simulation[i, j] = chessPieces[i, j];
                            }
                        }
                        List<Vector2Int[]> simMoveList = new List<Vector2Int[]>(moveList);
                        bool isWhiteTurnSim = isWhiteTurn;
                        iterations++;
                        MoveTo(simulation, piece.currentX, piece.currentY, move.x, move.y, ref isWhiteTurnSim, deadWhites, deadBlacks, ref simMoveList, specialMovee);
                        int moveValue = Minimax(simulation, depth - 1, (currentTeam == 0) ? false : true, simMoveList, ref iterations);

                        if (currentTeam == 0){
                            if (moveValue < bestValueBlack)
                            {
                                bestValueBlack = moveValue;
                                bestPiece = piece;
                                bestMove = move;
                                bestSpecialMove = specialMovee;
                            }
                        }

                        else{
                            if (moveValue > bestValueWhite)
                            {
                                bestValueWhite = moveValue;
                                bestPiece = piece;
                                bestMove = move;
                                bestSpecialMove = specialMovee;
                            }    
                        }
                    }
                }
            }
        }

        if (bestPiece != null)
        {
            MoveTo(chessPieces, bestPiece.currentX, bestPiece.currentY, bestMove.x, bestMove.y, ref isWhiteTurn, deadWhites, deadBlacks, ref moveList, bestSpecialMove);
            Debug.Log("Iterations: " + iterations);
        }
    }

    public int Minimax(ChessPiece[,] position, int depth, bool isMaximizingPlayer, List<Vector2Int[]> simMoveList, ref int iterations){
        bool isWhiteTurnCopy = isWhiteTurn;

        if (depth == 0){
            return EvaluatePosition(position);
        }

        if (isMaximizingPlayer){
            int maxEval = int.MinValue;

            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    List<Vector2Int[]> moveListCopy1 = new List<Vector2Int[]>();
                    for (int i = 0; i < simMoveList.Count; i++)
                    {
                        moveListCopy1.Add(simMoveList[i]);
                    }
                    ChessPiece piece = position[x, y];
                    if (piece != null && piece.team == (isMaximizingPlayer? 0 : 1))
                    {
                        List<Vector2Int> pieceMoves2 = piece.GetAvailableMoves(ref position, TILE_COUNT_X, TILE_COUNT_Y);
                        SpecialMove specialMoveSim = piece.GetSpecialMoves(ref position, ref moveListCopy1, ref pieceMoves2);
                        PreventCheck(isMaximizingPlayer? 0 : 1, piece, ref pieceMoves2);

                        // Evaluar cada movimiento
                        foreach (Vector2Int move in pieceMoves2)
                        {
                            ChessPiece[,] newPosition = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
                            for (int i = 0; i < TILE_COUNT_X; i++)
                            {
                                for (int j = 0; j < TILE_COUNT_Y; j++)
                                {
                                    newPosition[i, j] = position[i, j];
                                }
                            }

                            List<Vector2Int[]> moveListCopy2 = new List<Vector2Int[]>();
                            for (int i = 0; i < moveListCopy1.Count; i++)
                            {
                                moveListCopy2.Add(moveListCopy1[i]);
                            }

                            MoveTo(newPosition, x, y, move.x, move.y, ref isWhiteTurnCopy, deadWhites, deadBlacks, ref moveListCopy2, specialMoveSim);
                            iterations++;
                            int eval = Minimax(newPosition, depth - 1, false, moveListCopy2, ref iterations);
                            maxEval = Math.Max(maxEval, eval);
                            moveListCopy2.Clear();
                            deadBlacks.Clear();
                            deadWhites.Clear();
                            newPosition = null;
                            isWhiteTurnCopy = false;
                        }
                    }
                    moveListCopy1.Clear();
                }
            }

            return maxEval;
            
        }

        else{
            int minEval = int.MaxValue;

            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    List<Vector2Int[]> moveListCopy1 = new List<Vector2Int[]>();
                    for (int i = 0; i < simMoveList.Count; i++)
                    {
                        moveListCopy1.Add(simMoveList[i]);
                    }
                    ChessPiece piece = position[x, y];
                    if (piece != null && piece.team == (isMaximizingPlayer? 0 : 1))
                    {
                        List<Vector2Int> pieceMoves2 = piece.GetAvailableMoves(ref position, TILE_COUNT_X, TILE_COUNT_Y);
                        SpecialMove specialMoveSim = piece.GetSpecialMoves(ref position, ref moveListCopy1, ref pieceMoves2);
                        PreventCheck(isMaximizingPlayer? 0 : 1, piece, ref pieceMoves2);

                        // Evaluar cada movimiento
                        foreach (Vector2Int move in pieceMoves2)
                        {
                            ChessPiece[,] newPosition = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
                            for (int i = 0; i < TILE_COUNT_X; i++)
                            {
                                for (int j = 0; j < TILE_COUNT_Y; j++)
                                {
                                    newPosition[i, j] = position[i, j];
                                }
                            }

                            List<Vector2Int[]> moveListCopy2 = new List<Vector2Int[]>();
                            for (int i = 0; i < moveListCopy1.Count; i++)
                            {
                                moveListCopy2.Add(moveListCopy1[i]);
                            }

                            iterations++;
                            MoveTo(newPosition, x, y, move.x, move.y, ref isWhiteTurnCopy, deadWhites, deadBlacks, ref moveListCopy2, specialMoveSim);
                            int eval = Minimax(newPosition, depth - 1, true, moveListCopy2, ref iterations);
                            minEval = Math.Min(minEval, eval);
                            moveListCopy2.Clear();
                            deadBlacks.Clear();
                            deadWhites.Clear();
                            newPosition = null;
                            isWhiteTurnCopy = true;
                        }
                    }
                    moveListCopy1.Clear();
                }
            }

            return minEval;
        }
    }

    public int EvaluatePosition(ChessPiece[,] position){
        int whiteValue = 0;
        int blackValue = 0;

        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if (position[x, y] != null){
                    if (position[x, y].team == 0)
                        whiteValue += position[x, y].value;
                    else
                        blackValue += position[x, y].value;
                }
            }
        }

        return whiteValue - blackValue;
    }

    public static bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos){
        for (int i = 0; i < moves.Count; i++)
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
            
    }

    public bool IsSquareThreatened(Vector2Int square, int team)
    {
        List<Vector2Int> enemyMoves = new List<Vector2Int>();

        // Iterate through all pieces on the board
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null && piece.team != team)
                {
                    // Get all available moves for the enemy piece
                    List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    enemyMoves.AddRange(pieceMoves);
                }
            }
        }

        // Check if any enemy move can reach the specified square
        return ContainsValidMove(ref enemyMoves, square);
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
                
        return -Vector2Int.one; //Invalid
    }

    private void MoveTo(ChessPiece[,] board, int originalX, int originalY, int x, int y, ref bool isWhiteTurn, List<ChessPiece> deadWhites, List<ChessPiece> deadBlacks, ref List<Vector2Int[]> newMoveList, SpecialMove specialMove1)
    {
        ChessPiece cp = board[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        // Is there another piece on target position?
        if (board[x, y] != null)
        {
            ChessPiece otherCp = board[x, y];

            if (cp.team == otherCp.team) return;

            // If it's from the enemy team
            if (otherCp.team == 0)
            {
                if (otherCp.type == ChessPieceType.King)
                    Checkmate(1);

                deadWhites.Add(otherCp);
                otherCp.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + Vector3.forward * 0.33f * deadWhites.Count);
            }
            else
            {
                if (otherCp.type == ChessPieceType.King)
                    Checkmate(0);

                deadBlacks.Add(otherCp);
                otherCp.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + Vector3.back * 0.33f * deadBlacks.Count);
            }
        }

        board[x, y] = cp;
        board[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        if (localGame) currentTeam = (currentTeam == 0) ? 1 : 0;
        newMoveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        ProcessSpecialMove(newMoveList, board, specialMove1);

        if (CurrentlyDragging)
            CurrentlyDragging = null;

        RemoveHighlightTiles();

        if (CheckForCheckmate())
            Checkmate(cp.team);

        return;
    }

    #region
    private void RegisterEvents(){
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
        GameUI.Instance.SetComputerGame += OnSetComputerGame;
    }

    private void UnregisterEvents(){
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
        GameUI.Instance.SetComputerGame -= OnSetComputerGame;
    }
    

    // Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        // Client has connected, assign a team and return the message back to him
        NetWelcome nw = msg as NetWelcome;

        // Assign a team
        nw.AssignedTeam = ++playerCount;

        // Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        // If full, start the game
        if(playerCount == 1)
            Server.Instance.Broadcast(new NetStartGame());
        
    }

    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove mm = msg as NetMakeMove;

        // Receive, and just broadcast it back
        Server.Instance.Broadcast(mm);
    }

    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    // Client
    private void OnWelcomeClient(NetMessage msg){
        // Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        // Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is {nw.AssignedTeam}");

        if((localGame || computerGame) && currentTeam == 0){
            Server.Instance.Broadcast(new NetStartGame());
        }
    }

    private void OnStartGameClient(NetMessage message)
    {
        // We just need to change the camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }

    private void OnMakeMoveClient(NetMessage message)
    {
        NetMakeMove mm = message as NetMakeMove;

        Debug.Log($"MM : {mm.teamId} : {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY}");

        if(mm.teamId != currentTeam){
            ChessPiece target = chessPieces[mm.originalX, mm.originalY];

            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

            MoveTo(chessPieces, mm.originalX, mm.originalY, mm.destinationX, mm.destinationY, ref isWhiteTurn, deadWhites, deadBlacks, ref moveList, specialMove);
        }
    }

    private void OnRematchClient(NetMessage message)
    {
        // receive the connection message
        NetRematch rm = message as NetRematch;

        // Set the boolean for rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;

        // Activate the piece of UI
        if(rm.teamId != currentTeam){
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if(rm.wantRematch != 1){
                rematchButton.interactable = false;
                rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        // If both players want rematch
        if(playerRematch[0] && playerRematch[1])
            GameReset();
            
    }

    private void ShutDownRelay(){
        Client.Instance.ShutDown();
        Server.Instance.ShutDown();
    }

    private void OnSetLocalGame(bool v)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = v;
    } 

    private void OnSetComputerGame(bool ai)
    {
        playerCount = -1;
        currentTeam = -1;
        computerGame = ai;
    }

    private void OnSetMenu(){
        playerCount = -1;
        currentTeam = -1;
        localGame = false;
        computerGame = false;
    }
    #endregion
}
// private void ComputerV1(int depth)
//     {
//         int bestValue = int.MinValue;
//         ChessPiece bestPiece = null;
//         Vector2Int bestMove = Vector2Int.zero;
//         bool isWhiteTurnSim = isWhiteTurn;

//         // Iterar a través de todas las piezas del equipo del ordenador
//         for (int x = 0; x < TILE_COUNT_X; x++)
//         {
//             for (int y = 0; y < TILE_COUNT_Y; y++)
//             {
//                 ChessPiece piece = chessPieces[x, y];
//                 if (piece != null && piece.team == currentTeam)
//                 {
//                     List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
//                     piece.GetSpecialMoves(ref chessPieces, ref moveList, ref pieceMoves);
//                     PreventCheck(currentTeam, piece, ref pieceMoves);

//                     // Evaluar cada movimiento
//                     foreach (Vector2Int move in pieceMoves)
//                     {
//                         ChessPiece[,] simulation = CopyBoard(chessPieces);
//                         List<Vector2Int[]> simMoveList = new List<Vector2Int[]>(moveList);

//                         MoveTo(simulation, piece.currentX, piece.currentY, move.x, move.y, ref isWhiteTurnSim);
//                         ProcessSpecialMove(simMoveList, simulation);

//                         int moveValue = Minimax(simulation, depth - 1, int.MinValue, int.MaxValue, false, simMoveList, isWhiteTurnSim);

//                         if (moveValue > bestValue)
//                         {
//                             bestValue = moveValue;
//                             bestPiece = piece;
//                             bestMove = move;
//                         }
//                     }
//                 }
//             }
//         }

//         if (bestPiece != null)
//         {
//             MoveTo(chessPieces, bestPiece.currentX, bestPiece.currentY, bestMove.x, bestMove.y, ref isWhiteTurn);
//         }
//     }

//     private ChessPiece[,] CopyBoard(ChessPiece[,] originalBoard)
//     {
//         ChessPiece[,] copy = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
//         for (int x = 0; x < TILE_COUNT_X; x++)
//         {
//             for (int y = 0; y < TILE_COUNT_Y; y++)
//             {
//                 if (originalBoard[x, y] != null)
//                 {
//                     // Copiar la referencia de la pieza existente
//                     copy[x, y] = originalBoard[x, y];
//                 }
//             }
//         }
//         return copy;
//     }

//     private int Minimax(ChessPiece[,] board, int depth, int alpha, int beta, bool isMaximizing, List<Vector2Int[]> simMoveList, bool isWhiteTurnSim)
//     {
//         if (depth == 0)
//         {
//             return EvaluateBoard(board);
//         }

//         if (isMaximizing)
//         {
//             int maxEval = int.MinValue;
//             for (int x = 0; x < TILE_COUNT_X; x++)
//             {
//                 for (int y = 0; y < TILE_COUNT_Y; y++)
//                 {
//                     ChessPiece piece = board[x, y];
//                     if (piece != null && piece.team == currentTeam)
//                     {
//                         List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref board, TILE_COUNT_X, TILE_COUNT_Y);
//                         piece.GetSpecialMoves(ref board, ref simMoveList, ref pieceMoves);
//                         PreventCheck(currentTeam, piece, ref pieceMoves);

//                         foreach (Vector2Int move in pieceMoves)
//                         {
//                             ChessPiece[,] simulation = CopyBoard(board);
//                             List<Vector2Int[]> newSimMoveList = new List<Vector2Int[]>(simMoveList);

//                             MoveTo(simulation, piece.currentX, piece.currentY, move.x, move.y, ref isWhiteTurnSim);
//                             ProcessSpecialMove(newSimMoveList, simulation);

//                             int eval = Minimax(simulation, depth - 1, alpha, beta, false, newSimMoveList, isWhiteTurnSim);
//                             maxEval = Math.Max(maxEval, eval);
//                             alpha = Math.Max(alpha, eval);
//                             if (beta <= alpha)
//                             {
//                                 break;
//                             }
//                         }
//                     }
//                 }
//             }
//             return maxEval;
//         }
//         else
//         {
//             int minEval = int.MaxValue;
//             int opponentTeam = (currentTeam == 0) ? 1 : 0;
//             for (int x = 0; x < TILE_COUNT_X; x++)
//             {
//                 for (int y = 0; y < TILE_COUNT_Y; y++)
//                 {
//                     ChessPiece piece = board[x, y];
//                     if (piece != null && piece.team == opponentTeam)
//                     {
//                         List<Vector2Int> pieceMoves = piece.GetAvailableMoves(ref board, TILE_COUNT_X, TILE_COUNT_Y);
//                         piece.GetSpecialMoves(ref board, ref simMoveList, ref pieceMoves);
//                         PreventCheck(opponentTeam, piece, ref pieceMoves);

//                         foreach (Vector2Int move in pieceMoves)
//                         {
//                             ChessPiece[,] simulation = CopyBoard(board);
//                             List<Vector2Int[]> newSimMoveList = new List<Vector2Int[]>(simMoveList);

//                             MoveTo(simulation, piece.currentX, piece.currentY, move.x, move.y, ref isWhiteTurnSim);
//                             ProcessSpecialMove(newSimMoveList, simulation);

//                             int eval = Minimax(simulation, depth - 1, alpha, beta, true, newSimMoveList, isWhiteTurnSim);
//                             minEval = Math.Min(minEval, eval);
//                             beta = Math.Min(beta, eval);
//                             if (beta <= alpha)
//                             {
//                                 break;
//                             }
//                         }
//                     }
//                 }
//             }
//             return minEval;
//         }
//     }

//     private int EvaluateBoard(ChessPiece[,] board)
//     {
//         int value = 0;
//         for (int x = 0; x < TILE_COUNT_X; x++)
//         {
//             for (int y = 0; y < TILE_COUNT_Y; y++)
//             {
//                 ChessPiece piece = board[x, y];
//                 if (piece != null)
//                 {
//                     value += (piece.team != currentTeam) ? piece.value : -piece.value;
//                 }
//             }
//         }
//         return value;
//     }