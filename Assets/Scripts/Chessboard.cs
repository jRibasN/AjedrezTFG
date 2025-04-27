using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private GameObject fog;
    [SerializeField] private float tileSize = 0.6f;
    [SerializeField] private float yOffset = 0.37f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button denyMoveButton;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private AudioClip[] sounds;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece CurrentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> allTeamPieces = new List<ChessPiece>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private Stack<ChessPiece> capturedPieces = new Stack<ChessPiece>();
    private Stack<ChessPiece> promotedPawn = new Stack<ChessPiece>();
    private HashSet<Vector2Int> simulatedPositions = new HashSet<Vector2Int>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private GameObject[,] fogTiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private bool promotion;
    private bool enPassant;
    private bool castle;
    private bool capture;
    private int winnerTeam = -1;
    private bool isComputerTurnInProgress = false;
    private bool waitingAsyncMove = false;
    private Dictionary<string, float> transpositionTable = new Dictionary<string, float>();
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    [SerializeField] private AudioSource audioSource;

    // Multiplayer logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = false;
    private bool computerGame = false;
    private bool asyncGame = false;
    private bool denialGame = false;
    private bool fogOfWar = false;
    private bool[] playerRematch = new bool[2];
    private  Vector2Int[] myAsyncMove = {new Vector2Int(-1, -1), new Vector2Int(-1, -1)};
    private  Vector2Int[] enemyAsyncMove = {new Vector2Int(-1, -1), new Vector2Int(-1, -1)};
    private Vector2Int[] denialMove = {new Vector2Int(-1, -1), new Vector2Int(-1, -1)};
    private bool deniedMove = false;
    
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
                if (chessPieces[hitPosition.x, hitPosition.y] != null && !waitingAsyncMove){
                    // Is it our turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && (asyncGame ? true : isWhiteTurn) && currentTeam == 0) || 
                        (chessPieces[hitPosition.x, hitPosition.y].team == 1 && (asyncGame ? true : !isWhiteTurn) && currentTeam == 1)){
                        CurrentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        // Get a list of where I can go, highlight tiles as well
                        availableMoves = CurrentlyDragging.GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        if(denialGame && deniedMove){
                            availableMoves.Remove(denialMove[1]);
                        }
                        // Get a list of special moves as well
                        PreventCheck(CurrentlyDragging.team, CurrentlyDragging, ref availableMoves);
                        HighlightTiles();
                    }
                }
            }

            // If we release the mouse button
            if (CurrentlyDragging != null && Input.GetMouseButtonUp(0)){
                Vector2Int previousPosition = new Vector2Int(CurrentlyDragging.currentX, CurrentlyDragging.currentY);

                if(ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y))){
                    if(asyncGame){
                        NetMakeMove mm = new NetMakeMove();
                        mm.originalX = previousPosition.x;
                        mm.originalY = previousPosition.y;
                        mm.destinationX = hitPosition.x;
                        mm.destinationY = hitPosition.y;
                        mm.teamId = currentTeam;
                        Client.Instance.SendToServer(mm);

                        CurrentlyDragging.SetPosition(GetTileCenter(CurrentlyDragging.currentX, CurrentlyDragging.currentY));
                        CurrentlyDragging = null;
                        RemoveHighlightTiles();
                        
                        MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);
                    }
                    else{
                        MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                        NetMakeMove mm = new NetMakeMove();
                        mm.originalX = previousPosition.x;
                        mm.originalY = previousPosition.y;
                        mm.destinationX = hitPosition.x;
                        mm.destinationY = hitPosition.y;
                        mm.teamId = currentTeam;
                        Client.Instance.SendToServer(mm);
                    }                    
                }

                else{
                    if(asyncGame && previousPosition != hitPosition){
                        NetMakeMove mm = new NetMakeMove();
                        mm.originalX = previousPosition.x;
                        mm.originalY = previousPosition.y;
                        mm.destinationX = hitPosition.x;
                        mm.destinationY = hitPosition.y;
                        mm.teamId = currentTeam;
                        Client.Instance.SendToServer(mm);
                    }
                    CurrentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    CurrentlyDragging = null;
                    RemoveHighlightTiles();

                    if(asyncGame && previousPosition != hitPosition) MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);
                }
            }
        }
            
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
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
                if (!isComputerTurnInProgress) // Verifica si el turno ya está en progreso
                {
                    isComputerTurnInProgress = true; // Marca el turno como en progreso
                    StartCoroutine(ExecuteComputerMove());
                }
            }
        }
    }

    private IEnumerator ExecuteComputerMove()
    {
        yield return new WaitForSeconds(0.5f); // Espera 1 segundo
        ComputerV1(2); // Llama al método después del retraso
        isComputerTurnInProgress = false;
    }

    //Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        yOffset += transform.position.y;
        bounds = new Vector3(tileCountX / 2 * tileSize, 0, tileCountX / 2 * tileSize) + boardCenter;
        
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

    private void GenerateFogOfWar(){
        fogTiles = new GameObject[TILE_COUNT_X, TILE_COUNT_Y];
        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                fogTiles[x, y] = Instantiate(fog, transform);
                fogTiles[x, y].transform.position = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                fogTiles[x, y].transform.localScale = new Vector3(tileSize, 0.2f, tileSize);
            }
        }
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
                    PositionSinglePiece(x, y, false, true);
    }

    private void PositionSinglePiece(int x, int y, bool simMode, bool force = false)
    {
        if (chessPieces[x, y] != null)
        {
            chessPieces[x, y].currentX = x;
            chessPieces[x, y].currentY = y;
            if(!simMode) chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
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
        if (fogOfWar) FogOfWarVisibility();
    }
    public void OnMenuButton(){
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);
        if(fogTiles != null){
            foreach (GameObject ft in fogTiles){
                Destroy(ft);
            }
        }
        
        Invoke("ShutDownRelay", 0.1f);

        Invoke("GameReset", 0.11f);

        // Reset some values
        Invoke("OnSetMenu", 0.12f);

        GameUI.Instance.Invoke("OnLeaveFromGameMenu", 0.12f);
    }

    // SpecialMoves
    private void ProcessSpecialMove(bool simMode, ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        promotion = false;
        enPassant = false;
        castle = false;

        if(moveList.Count < 2) return;
        Vector2Int[] newMove = moveList[moveList.Count - 1];
        Vector2Int[] prevMove = moveList[moveList.Count - 2];
        if (board[newMove[1].x, newMove[1].y] == null) return;

        if (board[newMove[1].x, newMove[1].y].type == ChessPieceType.Pawn){
            ChessPiece myPawn = board[newMove[1].x, newMove[1].y];
            // En passant
            if (board[prevMove[1].x, prevMove[1].y] != null){
                ChessPiece enemyPawn = board[prevMove[1].x, prevMove[1].y];
                if ((Mathf.Abs(prevMove[0].y - prevMove[1].y) == 2) && (prevMove[0].y == (enemyPawn.team == 0 ? 1 : 6))){
                    if (enemyPawn.type == ChessPieceType.Pawn){
                        if ((myPawn.team == 0 && myPawn.currentY > enemyPawn.currentY) || (myPawn.team == 1 && myPawn.currentY < enemyPawn.currentY)){
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
                                    enPassant = true;
                                    capturedPieces.Push(enemyPawn);
                                }
                            }
                        } 
                    }
                }
            }
            // Promotion
            if (myPawn.team == 0 && newMove[1].y == 7){
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0, 9);
                newQueen.transform.position = board[newMove[1].x, newMove[1].y].transform.position;
                if(simMode) promotedPawn.Push(board[newMove[1].x, newMove[1].y]);
                else Destroy(board[newMove[1].x, newMove[1].y].gameObject);
                board[newMove[1].x, newMove[1].y] = newQueen;
                PositionSinglePiece(newMove[1].x, newMove[1].y, simMode);
                promotion = true;
            }

            if (myPawn.team == 1 && newMove[1].y == 0){
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1, 9);
                newQueen.transform.position = board[newMove[1].x, newMove[1].y].transform.position;
                if(simMode) promotedPawn.Push(board[newMove[1].x, newMove[1].y]);
                else Destroy(board[newMove[1].x, newMove[1].y].gameObject);
                board[newMove[1].x, newMove[1].y] = newQueen;
                PositionSinglePiece(newMove[1].x, newMove[1].y, simMode);
                promotion = true;
            }

        }
        // Castling
        if (board[newMove[1].x, newMove[1].y].type == ChessPieceType.King){
            if(Mathf.Abs(newMove[1].x - newMove[0].x) == 2){
                // White side
                if(newMove[1].y == 0){
                    // Right rook
                    if(newMove[1].x == 2){
                        ChessPiece rook = board[0, 0];
                        board[3, 0] = rook;
                        PositionSinglePiece(3, 0, simMode);
                        board[0, 0] = null;
                    }
                    // Left rook
                    else if(newMove[1].x == 6){
                        ChessPiece rook = board[7, 0];
                        board[5, 0] = rook;
                        PositionSinglePiece(5, 0, simMode);
                        board[7, 0] = null;
                    }
                }
                // Black side
                else if(newMove[1].y == 7){ 
                    // Left rook
                    if(newMove[1].x == 2){
                        ChessPiece rook = board[0, 7];
                        board[3, 7] = rook;
                        PositionSinglePiece(3, 7, simMode);
                        board[0, 7] = null;
                    }
                    // Right rook
                    else if(newMove[1].x == 6){
                        ChessPiece rook = board[7, 7];
                        board[5, 7] = rook;
                        PositionSinglePiece(5, 7, simMode);
                        board[7, 7] = null;
                    }
                }
                castle = true;
                audioSource.clip = sounds[3];
            }
        }
    }

    private void PreventCheck(int team, ChessPiece cp, ref List<Vector2Int> availableMoves, ChessPiece[,] board = null){
        if (fogOfWar) return;
        if (board == null) board = chessPieces;

        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (board[x, y] != null)
                    if (board[x, y].type == ChessPieceType.King)
                        if (board[x, y].team == team)
                            targetKing = board[x, y];
        
        // Since we´re sending ref availableMoves, we will be deleting moves that are putting us in check
        SimulateMoveForSinglePiece(cp, availableMoves, targetKing, board);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, List<Vector2Int> moves, ChessPiece targetKing, ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        // Save the current values, to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Going through all the moves, simulate them and check if we´re in check
        for (int i = 0; i < moves.Count; i++)
        {
            int  simX = moves[i].x;
            int  simY = moves[i].y;

            if (simulatedPositions.Contains(new Vector2Int(simX, simY)))
                return;

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
                    if(board[x, y] != null){
                        simulation[x, y] = board[x, y];
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
                var pieceMoves = simAttackingPieces[a].GetAvailableMoves(simulation, TILE_COUNT_X, TILE_COUNT_Y, moveList);
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
            simulatedPositions.Add(new Vector2Int(simX, simY));
        }

        // Remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
        simulatedPositions.Clear();
    }

    private bool CheckForCheckmate(ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        var lastMove = moveList[moveList.Count - 1];
        if (board[lastMove[1].x, lastMove[1].y] != null){
            int targetTeam = (board[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

            List<ChessPiece> attackingPieces = new List<ChessPiece>();
            List<ChessPiece> defendingPieces = new List<ChessPiece>();
            ChessPiece targetKing = null;
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                    if (board[x, y] != null){
                        if(board[x, y].team == targetTeam){
                            defendingPieces.Add(board[x, y]);
                            if(board[x, y].type == ChessPieceType.King)
                                targetKing = board[x, y];
                        }
                        else{
                            attackingPieces.Add(board[x, y]);
                        }
                    }

            // Is the king being attacked right now?
            List<Vector2Int> currentAvailableMoves = new List<Vector2Int> ();
            for (int i = 0; i < attackingPieces.Count; i++)
            {
                var pieceMoves = attackingPieces[i].GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    currentAvailableMoves.Add(pieceMoves[b]);
                }
            }

            // Are we in check right now?
            if (targetKing != null){
                if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY))){
                    // King is under attack, can we move something to help him?
                    for (int i = 0; i < defendingPieces.Count; i++)
                    {
                        List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        SimulateMoveForSinglePiece(defendingPieces[i], defendingMoves, targetKing, board);

                        if(defendingMoves.Count != 0)
                            return false;
                    }

                    winnerTeam = targetTeam == 0 ? 1 : 0;
                    return true; // Checkmate exit
                }
                else{
                    List<Vector2Int> defendingMoves = new List<Vector2Int>();
                    foreach (ChessPiece piece in defendingPieces)
                    {
                        defendingMoves.AddRange(piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList));

                        if(defendingMoves.Count == 0)
                            return false;
                    }
                }
            }
        }
        return false;
    }

    private bool CheckForOwnCheckmate(ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        var lastMove = moveList[moveList.Count - 2];
        if (board[lastMove[1].x, lastMove[1].y] != null){
            int targetTeam = (board[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

            List<ChessPiece> attackingPieces = new List<ChessPiece>();
            List<ChessPiece> defendingPieces = new List<ChessPiece>();
            ChessPiece targetKing = null;
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                    if (board[x, y] != null){
                        if(board[x, y].team == targetTeam){
                            defendingPieces.Add(board[x, y]);
                            if(board[x, y].type == ChessPieceType.King)
                                targetKing = board[x, y];
                        }
                        else{
                            attackingPieces.Add(board[x, y]);
                        }
                    }

            // Is the king being attacked right now?
            List<Vector2Int> currentAvailableMoves = new List<Vector2Int> ();
            for (int i = 0; i < attackingPieces.Count; i++)
            {
                var pieceMoves = attackingPieces[i].GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    currentAvailableMoves.Add(pieceMoves[b]);
                }
            }

            // Are we in check right now?
            if (targetKing != null){
                if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY))){
                    // King is under attack, can we move something to help him?
                    for (int i = 0; i < defendingPieces.Count; i++)
                    {
                        List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        SimulateMoveForSinglePiece(defendingPieces[i], defendingMoves, targetKing, board);

                        if(defendingMoves.Count != 0)
                            return false;
                    }

                    winnerTeam = targetTeam == 0 ? 1 : 0;
                    return true; // Checkmate exit
                }
                else{
                    List<Vector2Int> defendingMoves = new List<Vector2Int>();
                    foreach (ChessPiece piece in defendingPieces)
                    {
                        defendingMoves.AddRange(piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList));

                        if(defendingMoves.Count == 0)
                            return false;
                    }
                }
            }
        }
        return false;
    }

    private bool CheckForStalemate(ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        var lastMove = moveList[moveList.Count - 1];
        if (board[lastMove[1].x, lastMove[1].y] != null){
            int targetTeam = (board[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

            List<ChessPiece> attackingPieces = new List<ChessPiece>();
            List<ChessPiece> defendingPieces = new List<ChessPiece>();
            List<Vector2Int> defendingMoves = new List<Vector2Int>();
            ChessPiece targetKing = null;
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                    if (board[x, y] != null){
                        if(board[x, y].team == targetTeam){
                            defendingPieces.Add(board[x, y]);
                            if(board[x, y].type == ChessPieceType.King)
                                targetKing = board[x, y];
                        }
                        else{
                            attackingPieces.Add(board[x, y]);
                        }
                    }

            foreach(ChessPiece piece in defendingPieces){
                List<Vector2Int> pieceMoves = piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                PreventCheck(targetTeam, piece, ref pieceMoves, board);

                defendingMoves.AddRange(pieceMoves);
            }
            
            if(defendingMoves.Count == 0 && !IsSquareThreatened(new Vector2Int(targetKing.currentX, targetKing.currentY), targetTeam)){
                return true;

            }
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
                        availableMoves = chessPieces[x, y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        PreventCheck((currentTeam == 0) ? 1 : 0, chessPieces[x, y], ref availableMoves);
                        if(availableMoves.Count > 0) allTeamPieces.Add(chessPieces[x, y]);
                        availableMoves.Clear();
                    }
                }
             }
        }

        if(allTeamPieces.Count > 0){
            ChessPiece cp = allTeamPieces[rnd.Next(allTeamPieces.Count)];

            availableMoves = cp.GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
            PreventCheck((currentTeam == 0) ? 1 : 0, cp, ref availableMoves);

            Vector2Int move = availableMoves[rnd.Next(availableMoves.Count)];

            MoveTo(cp.currentX, cp.currentY, move.x, move.y);
        }

        allTeamPieces.Clear();
        availableMoves.Clear();
    }

    public void ComputerV1(int depth){
        float bestValueWhite = int.MinValue;
        float bestValueBlack = int.MaxValue;
        ChessPiece bestPiece = null;
        Vector2Int bestMove = Vector2Int.zero;
        int iterations = 0;
        ChessPiece[,] simulation = chessPieces;
        
        float alpha = int.MinValue;
        float beta = int.MaxValue;

        // Iterar a través de todas las piezas del equipo del ordenador
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = simulation[x, y];
                if (piece != null && piece.team != currentTeam)
                {
                    List<Vector2Int> pieceMoves = piece.GetAvailableMoves(simulation, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                    PreventCheck((currentTeam == 0) ? 1 : 0, piece, ref pieceMoves, simulation);

                    pieceMoves = OrderMoves(piece, pieceMoves, simulation);

                    // Evaluar cada movimiento
                    foreach (Vector2Int move in pieceMoves)
                    {
                        iterations++;
                        MoveTo(x, y, move.x, move.y, true, simulation);
                        bool captureInstance = capture;
                        bool promotionInstance = promotion;
                        bool enPassantInstance = enPassant;
                        bool castleInstance = castle;
                        float moveValue = Minimax(depth - 1, alpha, beta, (currentTeam == 0) ? true : false, ref iterations, simulation);
                        UndoMove(x, y, move.x, move.y, captureInstance, promotionInstance, enPassantInstance, castleInstance, simulation);

                        if (currentTeam == 0){
                            if (moveValue < bestValueBlack)
                            {
                                bestValueBlack = moveValue;
                                bestPiece = piece;
                                bestMove = move;
                            }
                            beta = Math.Min(beta, moveValue);
                        }

                        else{
                            if (moveValue > bestValueWhite)
                            {
                                bestValueWhite = moveValue;
                                bestPiece = piece;
                                bestMove = move;
                            }   
                            alpha = Math.Max(alpha, moveValue);
                        }
                        // Poda alfa-beta
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                }
            }
        }

        if (bestPiece != null)
        {
            MoveTo(bestPiece.currentX, bestPiece.currentY, bestMove.x, bestMove.y);
            Debug.Log("Iterations: " + iterations);
        }
    }

    public float Minimax(int depth, float alpha, float beta, bool isMaximizingPlayer, ref int iterations, ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        bool isWhiteTurnCopy = isWhiteTurn;

        if (depth == 0){
            if(CheckForCheckmate(board)) return isWhiteTurnCopy ? -1000 : 1000;
            if(CheckForStalemate(board)) return 0;

            return EvaluatePosition(board);
        }

        if (isMaximizingPlayer){
            float maxEval = float.MinValue;

            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    ChessPiece piece = board[x, y];
                    if (piece != null && piece.team == 0)
                    {
                        List<Vector2Int> pieceMoves2 = piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        PreventCheck(0, piece, ref pieceMoves2, board);

                        pieceMoves2 = OrderMoves(piece, pieceMoves2, board);

                        // Evaluar cada movimiento
                        foreach (Vector2Int move in pieceMoves2)
                        {
                            MoveTo(x, y, move.x, move.y, true, board);
                            bool captureInstance = capture;
                            bool promotionInstance = promotion;
                            bool enPassantInstance = enPassant;
                            bool castleInstance = castle;
                            iterations++;
                            float eval = Minimax(depth - 1, alpha, beta, false, ref iterations, board);
                            UndoMove(x, y, move.x, move.y, captureInstance, promotionInstance, enPassantInstance, castleInstance, board); 
                           
                            //Debug.Log("Eval: " + eval);   
                            maxEval = Math.Max(maxEval, eval);
                            alpha = Math.Max(alpha, eval);
                            if (beta <= alpha) break; // Beta cut-off
                        }
                    }
                }
            }

            return maxEval;
            
        }

        else{
            float minEval = float.MaxValue;

            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    ChessPiece piece = board[x, y];
                    if (piece != null && piece.team == 1)
                    {
                        List<Vector2Int> pieceMoves2 = piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        PreventCheck(1, piece, ref pieceMoves2, board);

                        pieceMoves2 = OrderMoves(piece, pieceMoves2, board);

                        // Evaluar cada movimiento
                        foreach (Vector2Int move in pieceMoves2)
                        {
                            iterations++;
                            MoveTo(x, y, move.x, move.y, true, board);
                            bool captureInstance = capture;
                            bool promotionInstance = promotion;
                            bool enPassantInstance = enPassant;
                            bool castleInstance = castle;
                            float eval = Minimax(depth - 1, alpha, beta, true, ref iterations, board);
                            UndoMove(x, y, move.x, move.y, captureInstance, promotionInstance, enPassantInstance, castleInstance, board);
                            
                            minEval = Math.Min(minEval, eval);
                            beta = Math.Min(beta, eval);
                            if (beta <= alpha) break; // Alpha cut-off
                        }
                    }
                }
            }
            return minEval;
        }
    }

    public float EvaluateOpponentStrength(int team, ChessPiece[,] board = null){
        if (board == null) board = chessPieces;

        float value = 0;
        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if (board[x, y] != null && board[x, y].team != team){
                    value += board[x, y].value;
                }
            }
        }
        return value;
    }

    public float EvaluatePosition(ChessPiece[,] board){
        float whiteValue = 0;
        float blackValue = 0;

        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if (board[x, y] != null){
                    if (board[x, y].team == 0)
                        whiteValue += board[x, y].UpdateValue((EvaluateOpponentStrength(board[x, y].team, board) < 1015.5f) ? true : false);
                    else
                        blackValue += board[x, y].UpdateValue((EvaluateOpponentStrength(board[x, y].team, board) < 1015.5f) ? true : false);
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

    private bool isCheckingThreat = false;
    public bool IsSquareThreatened(Vector2Int square, int team, ChessPiece[,] board = null)
    {
        if (board == null) board = chessPieces;

        if (isCheckingThreat) return false; // Evitar recursión
        isCheckingThreat = true;

        List<Vector2Int> enemyMoves = new List<Vector2Int>();

        // Iterar sobre todas las piezas enemigas
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = board[x, y];
                if (piece != null && piece.team != team)
                {
                    // Get all available moves for the enemy piece
                    List<Vector2Int> pieceMoves = piece.GetAvailableMoves(board, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                    enemyMoves.AddRange(pieceMoves);
                }
            }
        }

        isCheckingThreat = false;
        return ContainsValidMove(ref enemyMoves, square);
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
                
        return -Vector2Int.one; //Invalid
    }

    private List<Vector2Int> OrderMoves(ChessPiece piece, List<Vector2Int> moves, ChessPiece[,] board = null)
    {
        if (board == null) board = this.chessPieces;

        // Ordenar los movimientos por el valor de la pieza objetivo
        {
            moves.Sort((move1, move2) =>
            {
                ChessPiece target1 = board[move1.x, move1.y];
                ChessPiece target2 = board[move2.x, move2.y];

                float value1 = (target1 != null) ? target1.value : 0;
                float value2 = (target2 != null) ? target2.value : 0;

                return value2.CompareTo(value1); // Orden descendente por valor
            });

            return moves;
        }
    } 

    private void MoveTo(int originalX, int originalY, int x, int y, bool simMode = false, ChessPiece[,] board = null)
    {
        if (board == null) board = this.chessPieces;

        audioSource.clip = sounds[0];

        if(asyncGame && myAsyncMove[1] == new Vector2Int(-1, -1)){
            waitingAsyncMove = true;
            myAsyncMove[0].x = originalX;
            myAsyncMove[0].y = originalY;
            myAsyncMove[1].x = x;
            myAsyncMove[1].y = y;
            AsyncMove();
            return;
        }
        if(denialGame && !deniedMove){
            denialMove[0].x = originalX;
            denialMove[0].y = originalY;
            denialMove[1].x = x;
            denialMove[1].y = y;
        }

        // Debug.Log("Moving from " + originalX + ", " + originalY + " to " + x + ", " + y);
        ChessPiece cp = board[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);
        capture = false;

        // Is there another piece on target position?
        if (board[x, y] != null && board[originalX, originalY] != null)
        {

            ChessPiece otherCp = board[x, y];

            if (cp.team == otherCp.team) return;
            audioSource.clip = sounds[2];

            // If it's from the enemy team
            if (otherCp.team == 0)
            {
                if (otherCp.type == ChessPieceType.King && !simMode)
                    Checkmate(1);

                deadWhites.Add(otherCp);
                if(!simMode){
                    otherCp.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + Vector3.forward * 0.33f * deadWhites.Count);
                }
            }
            else
            {
                if (otherCp.type == ChessPieceType.King && !simMode)
                    Checkmate(0);

                deadBlacks.Add(otherCp);
                if(!simMode){
                    otherCp.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + Vector3.back * 0.33f * deadBlacks.Count);
                }
            }
            capturedPieces.Push(otherCp);
            capture = true;
        }

        board[x, y] = cp;
        board[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y, simMode);

        isWhiteTurn = !isWhiteTurn;
        if (localGame) currentTeam = (currentTeam == 0) ? 1 : 0;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        ProcessSpecialMove(simMode);

        if (CurrentlyDragging)
            CurrentlyDragging = null;

        RemoveHighlightTiles();

        if (!simMode){
            // Check if we are checking the enemy king
            List<Vector2Int> moves = cp.GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
            ChessPiece targetKing = null;
            for (int i = 0; i < TILE_COUNT_X; i++)
                for (int j = 0; j < TILE_COUNT_Y; j++)
                    if (board[i, j] != null)
                        if (board[i, j].type == ChessPieceType.King)
                            if (board[i, j].team != cp.team)
                                targetKing = board[i, j];
            if (targetKing != null){
                if(ContainsValidMove(ref moves, new Vector2Int(targetKing.currentX, targetKing.currentY))){
                    audioSource.clip = sounds[1];
                }
            }
            else{
                audioSource.clip = sounds[0];
            }
        }
        

        if(!asyncGame){
            if (CheckForCheckmate() && !simMode){
                if(!denialGame){
                    Checkmate(winnerTeam);
                    audioSource.clip = sounds[4];
                }
                else if(denialGame && deniedMove){
                    Checkmate(winnerTeam);
                    audioSource.clip = sounds[4];
                }
            }
                
            if (CheckForStalemate() && !simMode){
                if(!denialGame){
                    Checkmate(2);
                    audioSource.clip = sounds[4];
                }
                else if(denialGame && deniedMove){
                    Checkmate(2);
                    audioSource.clip = sounds[4];
                }
            }
            
            if(deniedMove) deniedMove = false;

            if (fogOfWar) FogOfWarVisibility();
        }
        
        audioSource.Play();
        return;
    }

    private void UndoMove(int originalX, int originalY, int moveX, int moveY, bool captureInstance, bool promotionInstance, bool enPassantInstance, bool castleInstance, ChessPiece[,] board = null, bool simMode = true){
        if (!simMode){
            audioSource.clip = sounds[5];
            audioSource.Play();
        }
        if (board == null) board = this.chessPieces;
        //Debug.Log("Undoing move from " + moveX + ", " + moveY + " to " + originalX + ", " + originalY);
        ChessPiece cp = board[moveX, moveY];
        ChessPiece restoredPiece = null;

        if(captureInstance){
            restoredPiece = capturedPieces.Pop();
            if(cp.team == 0 && deadBlacks.Count > 0){
                deadBlacks.Remove(restoredPiece);
            }
            
            else if(cp.team == 1 && deadWhites.Count > 0){
                deadWhites.Remove(restoredPiece);
            } 
        }

        if(enPassantInstance){
            restoredPiece = capturedPieces.Pop();
            if(cp.team == 0){
                board[moveX, moveY - 1] = restoredPiece;
                PositionSinglePiece(moveX, moveY - 1, simMode);
                deadBlacks.Remove(restoredPiece);
            }
            else{
                board[moveX, moveY + 1] = restoredPiece;
                PositionSinglePiece(moveX, moveY + 1, simMode);
                deadWhites.Remove(restoredPiece);
            }
        }

        if(castleInstance){
            if(moveX == 2 && moveY == 0){
                ChessPiece rook = board[3, 0];
                board[0, 0] = rook;
                PositionSinglePiece(0, 0, simMode);
                board[3, 0] = null;
            }
            else if(moveX == 6 && moveY == 0){
                ChessPiece rook = board[5, 0];
                board[7, 0] = rook;
                PositionSinglePiece(7, 0, simMode);
                board[5, 0] = null;
            }
            else if(moveX == 2 && moveY == 7){
                ChessPiece rook = board[3, 7];
                board[0, 7] = rook;
                PositionSinglePiece(0, 7, simMode);
                board[3, 7] = null;
            }
            else if(moveX == 6 && moveY == 7){
                ChessPiece rook = board[5, 7];
                board[7, 7] = rook;
                PositionSinglePiece(7, 7, simMode);
                board[5, 7] = null;
            }
        }

        board[originalX, originalY] = cp;

        if(promotionInstance){
            ChessPiece pawn = promotedPawn.Pop();
            Destroy(cp.gameObject);
            board[originalX, originalY] = pawn;
        }

        PositionSinglePiece(originalX, originalY, simMode);

        if (restoredPiece != null && captureInstance){
            board[moveX, moveY] = restoredPiece;
            PositionSinglePiece(moveX, moveY, simMode);
        }
        else{
            board[moveX, moveY] = null;
        }

        isWhiteTurn = !isWhiteTurn;
        moveList.RemoveAt(moveList.Count - 1);
    }

    private void AsyncMove()
    {
        List<Vector2Int> myMoves = new List<Vector2Int>();
        List<Vector2Int> enemyMoves = new List<Vector2Int>();

        if(myAsyncMove[1] != new Vector2Int(-1, -1)){
            myMoves = chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
        }
        if(enemyAsyncMove[1] != new Vector2Int(-1, -1)){
            enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
        }
        

        if (enemyAsyncMove[1] != new Vector2Int(-1, -1))
        {
            if (myAsyncMove[1] != new Vector2Int(-1, -1))
            {
                if (myAsyncMove[1] == enemyAsyncMove[1])
                {
                    Debug.Log("Conflict detected: Both players moved to the same tile.");
                    // Ambos jugadores intentan mover a la misma casilla
                    ResolveConflict(myAsyncMove, enemyAsyncMove);
                }
                else
                {
                    if(myAsyncMove[1].x == enemyAsyncMove[0].x && myAsyncMove[1].y == enemyAsyncMove[0].y)
                    {
                        if(enemyAsyncMove[1].x == myAsyncMove[0].x && enemyAsyncMove[1].y == myAsyncMove[0].y)
                        {
                            if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].value == chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].value){
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1]) && ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])){
                                    ChessPiece cp = chessPieces[enemyAsyncMove[1].x, enemyAsyncMove[1].y];
                                    chessPieces[enemyAsyncMove[1].x, enemyAsyncMove[1].y] = null;
                                    if(cp.team == 0){
                                        deadWhites.Add(cp);
                                        cp.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                                        - bounds
                                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                                        + Vector3.forward * 0.33f * deadWhites.Count);
                                    }
                                    else{
                                        deadBlacks.Add(cp);
                                        cp.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                                        - bounds
                                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                                        + Vector3.back * 0.33f * deadBlacks.Count);
                                    }
                                }
                                
                            }
                            else{
                                if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].value < chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].value){
                                    if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                    if(chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y] != null){
                                        myMoves = chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                    }
                                }
                                else{
                                    if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                    if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y] != null){
                                        enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                    }
                                }
                            }
                        }
                        else{
                            if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                            myMoves = chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                            if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                        }
                    }
                    else if(enemyAsyncMove[1].x == myAsyncMove[0].x && enemyAsyncMove[1].y == myAsyncMove[0].y){
                        if(myAsyncMove[1].x == enemyAsyncMove[0].x && myAsyncMove[1].y == enemyAsyncMove[0].y){
                            if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].value == chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].value){
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1]) && ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])){
                                    ChessPiece cp = chessPieces[enemyAsyncMove[1].x, enemyAsyncMove[1].y];
                                    chessPieces[enemyAsyncMove[1].x, enemyAsyncMove[1].y] = null;
                                    if(cp.team == 0){
                                        deadWhites.Add(cp);
                                        cp.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                                        - bounds
                                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                                        + Vector3.forward * 0.33f * deadWhites.Count);
                                    }
                                    else{
                                        deadBlacks.Add(cp);
                                        cp.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                                        - bounds
                                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                                        + Vector3.back * 0.33f * deadBlacks.Count);
                                    }
                                }
                                
                            }
                            else{
                                if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].value < chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].value){
                                    if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                    if(chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y] != null){
                                        myMoves = chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                    }
                                }
                                else{
                                    if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                    if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y] != null){
                                        enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                    }
                                }
                            }
                        }
                        else{
                            if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                            enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                            if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                        }
                    }
                    else{
                        if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].type == ChessPieceType.Pawn && myAsyncMove[0].x != myAsyncMove[1].x){
                            if(chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].type == ChessPieceType.Pawn && enemyAsyncMove[0].x != enemyAsyncMove[1].x){
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                            }
                            else{
                                Debug.Log("Entró");
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                myMoves = chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                            }
                        }
                        else if(chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].type == ChessPieceType.Pawn && enemyAsyncMove[0].x != enemyAsyncMove[1].x){
                            if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].type == ChessPieceType.Pawn && myAsyncMove[0].x != myAsyncMove[1].x){
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                            }
                            else{
                                Debug.Log("Entró");
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                            }
                        }
                        else{
                            if(chessPieces[myAsyncMove[0].x, myAsyncMove[0].y].value < chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].value){
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                            }
                            else{
                                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myAsyncMove[0].x, myAsyncMove[0].y, myAsyncMove[1].x, myAsyncMove[1].y);
                                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyAsyncMove[0].x, enemyAsyncMove[0].y, enemyAsyncMove[1].x, enemyAsyncMove[1].y);
                            }
                        }
                    }
                }

                //Debug.Log(moveList[moveList.Count - 2][1] + " " + moveList[moveList.Count - 1][1]);
                // Reiniciar los movimientos asíncronos
                ResetAsyncMoves();
                Debug.Log("Checkmate check");
                if (CheckForCheckmate()){
                    Debug.Log("Checkmate for: " + winnerTeam);
                    Checkmate(winnerTeam);
                    winnerTeam = -1;
                }

                if (CheckForOwnCheckmate()){
                    Debug.Log("Checkmate for: " + winnerTeam);
                    Checkmate(winnerTeam);
                    winnerTeam = -1;
                }


                if (CheckForStalemate())
                    Checkmate(2);
            }
        }
    }

    private void ResolveConflict(Vector2Int[] myMove, Vector2Int[] enemyMove)
    {
        List<Vector2Int> myMoves = new List<Vector2Int>(chessPieces[myMove[0].x, myMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
        List<Vector2Int> enemyMoves = new List<Vector2Int>(chessPieces[enemyMove[0].x, enemyMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
        
        if(chessPieces[myMove[1].x, myMove[1].y] != null){
            if(chessPieces[myMove[0].x, myMove[0].y].team != chessPieces[myMove[1].x, myMove[1].y].team){
                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
            }
            else{
                if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                myMoves = new List<Vector2Int>(chessPieces[myMove[0].x, myMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
                if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
            }
        }
        else{
            if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.Pawn && myMove[0].x != myMove[1].x){
                if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.Pawn && enemyMove[0].x != enemyMove[1].x){
                    return;
                }
                else{
                    if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                    myMoves = new List<Vector2Int>(chessPieces[myMove[0].x, myMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
                    if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                }
            }
            else if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.Pawn && enemyMove[0].x != enemyMove[1].x){
                if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.Pawn && myMove[0].x != myMove[1].x){
                    return;
                }
                else{
                    if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                    enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                    if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                }
            }
            else{
                if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.Pawn && myMove[0].x == myMove[1].x){
                    if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.Pawn && enemyMove[0].x == enemyMove[1].x){
                        return;
                    }
                    else{
                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                        enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                    }
                }
                else if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.Pawn && enemyMove[0].x == enemyMove[1].x){
                    if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.Pawn && myMove[0].x == myMove[1].x){
                        return;
                    }
                    else{
                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                        myMoves = new List<Vector2Int>(chessPieces[myMove[0].x, myMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                    }
                }
                else if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.King){  
                    if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.King){
                        return;
                    }
                    else{
                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                        enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                    }
                }
                else if(chessPieces[myMove[0].x, myMove[0].y].type == ChessPieceType.King){  
                    if(chessPieces[enemyMove[0].x, enemyMove[0].y].type == ChessPieceType.King){
                        return;
                    }
                    else{
                        if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                        myMoves = new List<Vector2Int>(chessPieces[myMove[0].x, myMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList));
                        if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                    }
                }
                else{
                    if(ContainsValidMove(ref myMoves, myAsyncMove[1])) MoveTo(myMove[0].x, myMove[0].y, myMove[1].x, myMove[1].y);
                    enemyMoves = chessPieces[enemyAsyncMove[0].x, enemyAsyncMove[0].y].GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                    if(ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])) MoveTo(enemyMove[0].x, enemyMove[0].y, enemyMove[1].x, enemyMove[1].y);
                    if(ContainsValidMove(ref myMoves, myAsyncMove[1]) && ContainsValidMove(ref enemyMoves, enemyAsyncMove[1])){
                        ChessPiece cp = chessPieces[enemyMove[1].x, enemyMove[1].y];
                        chessPieces[enemyMove[1].x, enemyMove[1].y] = null;
                        if(cp.team == 0){
                            deadWhites.Add(cp);
                            cp.SetPosition(new Vector3(8.5f * tileSize, 0, -1 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + Vector3.forward * 0.33f * deadWhites.Count);
                        }
                        else{
                            deadBlacks.Add(cp);
                            cp.SetPosition(new Vector3(-1.5f * tileSize, 0, 8 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + Vector3.back * 0.33f * deadBlacks.Count);
                        }
                    }
                }
                
            }
        }
            
        // Restaurar la pieza perdedora a su posición original
        Debug.Log("Conflict resolved");
    }

    private void ResetAsyncMoves()
    {
        myAsyncMove[0] = new Vector2Int(-1, -1);
        myAsyncMove[1] = new Vector2Int(-1, -1);
        enemyAsyncMove[0] = new Vector2Int(-1, -1);
        enemyAsyncMove[1] = new Vector2Int(-1, -1);
        waitingAsyncMove = false;
    }

    public void HandleModeDropdown(int index){
        switch (index){
            case 0:
                Debug.Log("Std game selected");
                asyncGame = false;
                denialGame = false;
                fogOfWar = false;
                break;
            case 1:
                Debug.Log("Async game selected");
                asyncGame = true;
                denialGame = false;
                fogOfWar = false;
                break;
            case 2:
                Debug.Log("Denial game selected");
                //denialGame = true;
                asyncGame = false;
                denialGame = true;
                fogOfWar = false;
                break;
            case 3:
                Debug.Log("Fog of war selected");
                asyncGame = false;
                denialGame = false;
                fogOfWar = true;
                break;
        }
    }

    public void FogOfWarVisibility(){
        List<Vector2Int> teamMoves = new List<Vector2Int>();
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null && piece.team == currentTeam)
                {
                    // Get all available moves for the enemy piece
                    List<Vector2Int> pieceMoves = piece.GetAvailableMoves(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, moveList);
                    teamMoves.AddRange(pieceMoves);
                }
            }
        }
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null && piece.team != currentTeam)
                {
                    if(teamMoves.Contains(new Vector2Int(x, y))){
                        piece.gameObject.SetActive(true);
                    }
                    else{
                        piece.gameObject.SetActive(false);
                    }
                }
                if (!teamMoves.Contains(new Vector2Int(x, y)))
                {
                    if(piece != null && piece.team == currentTeam) fogTiles[x, y].gameObject.SetActive(false);
                    else fogTiles[x, y].SetActive(true);
                }
                else
                {
                    fogTiles[x, y].SetActive(false);
                }   
            }
        }
    }

    public void OnDenyMoveButton(){
        denyMoveButton.interactable = false;
        NetUndoMove um = new NetUndoMove();
        um.originalX = denialMove[0].x;
        um.originalY = denialMove[0].y;
        um.destinationX = denialMove[1].x;
        um.destinationY = denialMove[1].y;
        Client.Instance.SendToServer(um);
        Debug.Log("Undo move sent to server");
    }

    #region
    private void RegisterEvents(){
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;
        NetUtility.S_UNDO_MOVE += OnUndoMoveServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;
        NetUtility.C_UNDO_MOVE += OnUndoMoveClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
        GameUI.Instance.SetComputerGame += OnSetComputerGame;
    }

    private void UnregisterEvents(){
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;
        NetUtility.S_UNDO_MOVE -= OnUndoMoveServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;
        NetUtility.C_UNDO_MOVE -= OnUndoMoveClient;

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

        NetStartGame sg = new NetStartGame();
        if(asyncGame){
            sg.gameMode = 1;
        }
        else if(denialGame){
            sg.gameMode = 2;
        }
        else if(fogOfWar){
            sg.gameMode = 3;
        }
        else{
            sg.gameMode = 0;
        }

        // If full, start the game
        if(playerCount == 1)
            Server.Instance.Broadcast(sg);
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

    private void OnUndoMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetUndoMove um = msg as NetUndoMove;
        Server.Instance.Broadcast(um);
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
        NetStartGame sg = message as NetStartGame;

        if(sg.gameMode == 0){
            // Local game
            asyncGame = false;
            denialGame = false;
            fogOfWar = false;
        }
        else if(sg.gameMode == 1){
            // Asynchronous game
            asyncGame = true;
            denialGame = false;
            fogOfWar = false;
        }
        else if(sg.gameMode == 2){
            // Denial game
            asyncGame = false;
            denialGame = true;
            fogOfWar = false;
            denyMoveButton.gameObject.SetActive(true);
            denyMoveButton.interactable = false;
        }
        else if(sg.gameMode == 3){
            // Fog of war game
            asyncGame = false;
            denialGame = false;
            fogOfWar = true;
            GenerateFogOfWar();
            FogOfWarVisibility();
        }

        // We just need to change the camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }

    private void OnMakeMoveClient(NetMessage message)
    {
        Debug.Log("Received move from server");
        NetMakeMove mm = message as NetMakeMove;

        Debug.Log($"MM : {mm.teamId} : {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY}");
        
        if(asyncGame){
            if(mm.teamId != currentTeam){
                enemyAsyncMove[0] = new Vector2Int(mm.originalX, mm.originalY);
                enemyAsyncMove[1] = new Vector2Int(mm.destinationX, mm.destinationY);
            }
            AsyncMove();
        }
        else if(denialGame && !deniedMove){
            if(mm.teamId != currentTeam){
                denialMove[0] = new Vector2Int(mm.originalX, mm.originalY);
                denialMove[1] = new Vector2Int(mm.destinationX, mm.destinationY);
                MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
                denyMoveButton.interactable = true;
            }
            else if(mm.teamId == currentTeam){
                denyMoveButton.interactable = false;
            }
        }
        else if(mm.teamId != currentTeam){
            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
            deniedMove = false;
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

    private void OnUndoMoveClient(NetMessage message)
    {
        Debug.Log("OnUndoMoveClient called");
        NetUndoMove um = message as NetUndoMove;

        Debug.Log($"Undo move from server: {um.originalX} {um.originalY} -> {um.destinationX} {um.destinationY}");

        UndoMove(um.originalX, um.originalY, um.destinationX, um.destinationY, capture, promotion, enPassant, castle, null, false);

        deniedMove = true;
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