using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    List<Vector2Int> excellentSquareWhite = new List<Vector2Int>(){d4, d5, e4, e5, a7, b7, c7, d7, e7, f7, g7, h7};
    List<Vector2Int> goodSquareWhite = new List<Vector2Int>(){c4, c5, f4, f5, d6, e6};
    List<Vector2Int> badSquareWhite = new List<Vector2Int>(){a2, a4, a5, f3, g3, h2, h4, h5};
    List<Vector2Int> horribleSquareWhite = new List<Vector2Int>(){d2, e2};
    List<Vector2Int> excellentSquareBlack = new List<Vector2Int>(){d4, d5, e4, e5, a2, b2, c2, d2, e2, f2, g2, h2};
    List<Vector2Int> goodSquareBlack = new List<Vector2Int>(){c4, c5, f4, f5, d3, e3};
    List<Vector2Int> badSquareBlack = new List<Vector2Int>(){a7, a4, a5, f6, g6, h7, h4, h5};
    List<Vector2Int> horribleSquareBlack = new List<Vector2Int>(){d7, e7};

    public override float UpdateValue(bool isEndgame)
    {
        Vector2Int square = new Vector2Int(currentX, currentY);

        if(team == 0){
            if (excellentSquareWhite.Contains(square))
                value = 1.5f;
            else if (goodSquareWhite.Contains(square))
                value = 1.25f;
            else if (badSquareWhite.Contains(square))
                value = 0.75f;
            else if (horribleSquareWhite.Contains(square))
                value = 0.5f;
        }
        else {
            if (excellentSquareBlack.Contains(square))
                value = 1.5f;
            else if (goodSquareBlack.Contains(square))
                value = 1.25f;
            else if (badSquareBlack.Contains(square))
                value = 0.75f;
            else if (horribleSquareBlack.Contains(square))
                value = 0.5f;
        }        
        return value;
    }

    public override List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, int tileCountX, int tileCountY, List<Vector2Int[]> moveList){
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        if (currentY == 0 || currentY == tileCountY - 1) //If the pawn is on the first or last row, it can't move
            return r;
            
        //One in front
        if (board[currentX, currentY + direction] == null){
                r.Add(new Vector2Int(currentX, currentY + direction));
        }

        //Two in front
        if (board[currentX, currentY + direction] == null){
            //White team
            if(team == 0 && currentY == 1 && board[currentX, currentY +(direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY +(direction * 2)));

            //Black team
            if(team == 1 && currentY == 6 && board[currentX, currentY +(direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY +(direction * 2)));
        }

        //Capture move
        if (currentX != tileCountX - 1)
            if (board[currentX + 1, currentY + direction] != null &&  board[currentX + 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + direction));

        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null &&  board[currentX - 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        
        //En Passant
        if (moveList.Count > 0){
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (board[lastMove[1].x, lastMove[1].y] != null){
                if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn){ //If the last piece moved was a pawn
                    if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2){ //If the last move was a +2 in either direction
                        if (board[lastMove[1].x, lastMove[1].y].team != team){ //If the move was from the other team
                            if (lastMove[1].y == currentY){ //If both pawns are on the same Y
                                if(lastMove[1].x == currentX - 1){ //Landed left
                                    r.Add(new Vector2Int(currentX - 1, currentY + direction));
                                }

                                if(lastMove[1].x == currentX + 1){ //Landed right
                                    r.Add(new Vector2Int(currentX + 1, currentY + direction));
                                }
                            }
                        }
                    }
                }
            }
        }

        //Promotion handled in ProcessSpecialMove() in ChessBoard.cs

        return r;
    }
}
