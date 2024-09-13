using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text;

public class OddWallOutScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] ButtonSels;
    public GameObject[] ButtonObjs;
    public GameObject[] WedgeObjs;
    public Material[] WedgeMats;
    public Material[] ButtonMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private const int _size = 4;
    private const int _sttpo = 2 * _size + 1;
    private const int _numColors = 4;
    private MazeGenerator _mazeGenerator;
    private string _maze;

    private int? _previouslyPressedButton = null;
    private int[] _displayOrder;
    private int _submissionIx;
    private int[] _dummyColors;
    private int _cycleIx;
    private int[] _innerWallIxs;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        for (int i = 0; i < WedgeObjs.Length; i++)
            WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[4];

        var seed = Rnd.Range(int.MinValue, int.MaxValue);
        Debug.Log($"[Odd Wall Out #{_moduleId}] Random seed: {seed}");
        var rnd = new MonoRandom(seed);

        tryAgain:
        var gen = new MazeGenerator(_size, rnd);
        var maze = gen.GenerateMaze();
        var walls = Enumerable.Range(0, _sttpo * _sttpo / 2).Select(ix => 2 * ix + 1).Where(ix => ix % _sttpo != _sttpo - 1 && ix / _sttpo != _sttpo - 1 && maze[ix] == '█').ToArray().Shuffle();
        var firstNonEdgeWall = walls.IndexOf(ix => ix % _sttpo != 0 && ix % _sttpo != _sttpo - 1 && ix / _sttpo != 0 && ix / _sttpo != _sttpo - 1);
        if (firstNonEdgeWall != 0)
        {
            var temp = walls[0];
            walls[0] = walls[firstNonEdgeWall];
            walls[firstNonEdgeWall] = temp;
        }

        var wallColors = walls.Select(w => rnd.Next(0, _numColors)).ToArray();
        var specialWallColors = Enumerable.Range(0, _numColors).ToArray().Shuffle().Take(2).ToArray();
        var tiles = Enumerable.Range(0, _size * _size).Select(TransformPosition).Select(cell => new Tile(
                GetColor(cell - _sttpo, false, walls, wallColors, specialWallColors),
                GetColor(cell + 1, false, walls, wallColors, specialWallColors),
                GetColor(cell + _sttpo, true, walls, wallColors, specialWallColors),
                GetColor(cell - 1, true, walls, wallColors, specialWallColors)
            )).ToArray();

        // Make sure the maze is unique
        if (ConstructMaze(new Tile[0], tiles, new int[0][], 0).Take(2).Count() > 1)
            goto tryAgain;

        // Log the intended maze
        Debug.Log($"[Odd Wall Out #{_moduleId}]=svg[Solution maze:]{GenerateSvg(tiles)}");

        // Now randomize the order of the tiles
        tiles.Shuffle();
    }

    private string GenerateSvg(Tile[] tiles)
    {
        var content = new StringBuilder();
        content.Append($"<path class='grid' d='{Enumerable.Range(1, _size - 1).Select(i => $"M{i} 0 v {_size}").Join("")}{Enumerable.Range(1, _size - 1).Select(i => $"M0 {i} h {_size}").Join("")}' />");

        const double th = .1;   // thickness of walls

        for (var i = 0; i < _size * _size; i++)
        {
            int x = i % _size, y = i / _size;

            // Top edge
            if (y == 0 || (tiles[i].top != -1 && tiles[i].top != tiles[Above(i)].bottom))
                content.Append($"<path class='color-{tiles[i].top}' d='M{x} {y} h 1 l {-th} {th} h {-1 + 2 * th} z' />");

            // Left edge
            if (x == 0 || (tiles[i].left != -1 && tiles[i].left != tiles[Left(i)].right))
                content.Append($"<path class='color-{tiles[i].left}' d='M{x} {y + 1} v -1 l {th} {th} v {1 - 2 * th} z' />");

            // Right edge
            if (x == _size - 1 || (tiles[i].right != -1 && tiles[i].right != tiles[Right(i)].left))
                content.Append($"<path class='color-{tiles[i].right}' d='M{x + 1} {y + 1} v -1 l {-th} {th} v {1 - 2 * th} z' />");
            else if (x != _size - 1 && (tiles[i].right != -1 && tiles[i].right == tiles[Right(i)].left))
                content.Append($"<path class='color-{tiles[i].right}' d='M{x + 1} {y} l {th} {th} v {1 - 2 * th} l {-th} {th} {-th} {-th} v {-1 + 2 * th} z' />");

            // Bottom edge
            if (y == _size - 1 || (tiles[i].bottom != -1 && tiles[i].bottom != tiles[Below(i)].top))
                content.Append($"<path class='color-{tiles[i].bottom}' d='M{x + 1} {y + 1} h -1 l {th} {-th} h {1 - 2 * th} z' />");
            else if (y != _size - 1 && (tiles[i].bottom != -1 && tiles[i].bottom == tiles[Below(i)].top))
                content.Append($"<path class='color-{tiles[i].bottom}' d='M{x} {y + 1} l {th} {-th} h {1 - 2 * th} l {th} {th} {-th} {th} h {-1 + 2 * th} z' />");
        }

        return $"<svg class='odd-wall-out' xmlns='http://www.w3.org/2000/svg' viewBox='-.1 -.1 4.2 4.2'>{content}</svg>";
    }

    IEnumerable<bool> ConstructMaze(Tile[] sofar, Tile[] remaining, int[][] disconnectedPieces, int mismatchedWallAlready)
    {
        if (sofar.Length == _size * _size)
        {
            if (disconnectedPieces.Length == 1)
                yield return true;
            yield break;
        }

        var cell = sofar.Length;
        for (var i = 0; i < remaining.Length; i++)
        {
            var newTile = remaining[i];

            // Make sure there is a wall around the edge of the maze
            if ((cell % _size == 0 && newTile.left == -1) ||
                (cell % _size == _size - 1 && newTile.right == -1) ||
                (cell / _size == 0 && newTile.top == -1) ||
                (cell / _size == _size - 1 && newTile.bottom == -1))
                continue;

            // Make sure that walls/non-walls join up with each other ABOVE and LEFT
            if (cell / _size != 0 && (sofar[Above(cell)].bottom != -1) != (newTile.top != -1) || cell % _size != 0 && (sofar[Left(cell)].right != -1) != (newTile.left != -1))
                continue;
            // If toroidal, make sure they join up BELOW and RIGHT
            if ((cell / _size == _size - 1 && (sofar[Below(cell)].top != -1) != (newTile.bottom != -1) || cell % _size == _size - 1 && (sofar[Right(cell)].left != -1) != (newTile.right != -1)))
                continue;

            var conflictsWithAbove = cell / _size != 0 && sofar[Above(cell)].bottom != newTile.top;
            var conflictsWithLeft = cell % _size != 0 && sofar[Left(cell)].right != newTile.left;
            var conflictsWithBelow = cell / _size == _size - 1 && sofar[Below(cell)].top != newTile.bottom;
            var conflictsWithRight = cell % _size == _size - 1 && sofar[Right(cell)].left != newTile.right;

            var newMismatches = mismatchedWallAlready + (conflictsWithAbove ? 1 : 0) + (conflictsWithLeft ? 1 : 0) + (conflictsWithBelow ? 1 : 0) + (conflictsWithRight ? 1 : 0);
            if (newMismatches > 1)
                continue;

            var pieceAbove = newTile.top != -1 ? -1 : disconnectedPieces.IndexOf(dp => dp.Contains(Above(cell)));
            var pieceLeft = newTile.left != -1 ? -1 : disconnectedPieces.IndexOf(dp => dp.Contains(Left(cell)));
            var newDisconnectedPieces =
                (pieceAbove != -1 && pieceLeft == pieceAbove) ? disconnectedPieces.Replace(pieceAbove, disconnectedPieces[pieceAbove].Append(cell)) :
                (pieceAbove != -1 && pieceLeft != -1) ? disconnectedPieces.Remove(Math.Max(pieceLeft, pieceAbove), 1).Remove(Math.Min(pieceLeft, pieceAbove), 1)
                    .Append(disconnectedPieces[pieceAbove].Concat(disconnectedPieces[pieceLeft]).Append(cell)) :
                (pieceAbove != -1) ? disconnectedPieces.Replace(pieceAbove, disconnectedPieces[pieceAbove].Append(cell)) :
                (pieceLeft != -1) ? disconnectedPieces.Replace(pieceLeft, disconnectedPieces[pieceLeft].Append(cell)) :
                disconnectedPieces.Append(new[] { cell });

            foreach (var solution in ConstructMaze(sofar.Append(newTile), remaining.Remove(i, 1), newDisconnectedPieces, newMismatches))
                yield return solution;
        }
    }

    private static int Right(int cell) => (cell % _size + 1) % _size + _size * (cell / _size);
    private static int Below(int cell) => (cell + _size) % (_size * _size);
    private static int Left(int cell) => (cell % _size + _size - 1) % _size + _size * (cell / _size);
    private static int Above(int cell) => (cell - _size + _size * _size) % (_size * _size);

    private int GetColor(int cl, bool sec, int[] walls, int[] wallColors, int[] specialWallColors)
    {
        if (cl == walls[0])
        {
            return specialWallColors[sec ? 1 : 0];
        }
        else
        {
            var p = Array.IndexOf(walls, cl);
            var p2 = Array.IndexOf(walls, Toroidalled(cl));
            return p != -1 ? wallColors[p] : p2 != -1 ? wallColors[p2] : -1;
        }
    }

    private static int TransformPosition(int pos) => (pos / _size * (_size * 2 + 1) * 2) + (pos % _size * 2) + _size * 2 + 2;
    private static int Toroidalled(int cl) => (cl % _sttpo) % (_sttpo - 1) + _sttpo * ((cl / _sttpo) % (_sttpo - 1));

    private static Tile GetTile(Tile[] tiles, int ix) => ix < 0 || ix >= tiles.Length ? new Tile(-1, -1, -1, -1) : tiles[ix];

    //private static Maze VisualizeMaze(Tile[] tiles) =>
    //        Enumerable.Range(0, (_size * 2 + 1) * (_size * 2 + 1)).Select(ix => new { x = ix % (_size * 2 + 1), y = ix / (_size * 2 + 1) })
    //            .Select(c =>
    //            {
    //                if (c.x % 2 == 0 && c.y % 2 == 0)
    //                    return new { String = "██", Colors = new[] { -1, -1 } };
    //                if (c.x % 2 == 1 && c.y % 2 == 1)
    //                    return new { String = "  ", Colors = new[] { -1, -1 } };
    //                if (c.x % 2 == 0)
    //                {
    //                    var rightColor = c.x >= 2 * _size ? -1 : GetTile(tiles, c.x / 2 + _size * (c.y / 2)).left;
    //                    var leftColor = c.x == 0 ? -1 : GetTile(tiles, c.x / 2 - 1 + _size * (c.y / 2)).right;
    //                    if (leftColor == -1 && rightColor == -1)
    //                        return "  ";
    //                    if (leftColor == -1 || rightColor == -1)
    //                        return new { String = "██", Color = new[] { leftColor == -1 ? rightColor : leftColor, leftColor == -1 ? rightColor : leftColor } };
    //                    return new { String = "██", Color = new[] { leftColor, rightColor } };
    //                }
    //                var bottomColor = c.y >= 2 * _size ? -1 : GetTile(tiles, c.x / 2 + _size * (c.y / 2)).top;
    //                var topColor = c.y == 0 ? -1 : GetTile(tiles, c.x / 2 + _size * (c.y / 2 - 1)).bottom;
    //                if (topColor == -1 && bottomColor == -1)
    //                    return "  ";
    //                if (topColor == -1 || bottomColor == -1)
    //                    return new { String = "██", Color = new[] { topColor == -1 ? bottomColor : topColor, topColor == -1 ? bottomColor : topColor } };
    //                return "▀▀".Color(topColor, bottomColor);
    //            })
    //                .Split(2 * _size + 1)
    //                .Select(chunk => chunk.JoinColoredString())
    //                .JoinColoredString("\n");

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, ButtonSels[btn].transform);
            ButtonSels[btn].AddInteractionPunch(0.3f);

            if (_moduleSolved)
                return false;

            return false;
        };
    }
}
