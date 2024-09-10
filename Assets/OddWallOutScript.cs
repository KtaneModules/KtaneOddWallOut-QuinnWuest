using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

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
        _mazeGenerator = new MazeGenerator(_size, new MonoRandom(Rnd.Range(int.MinValue, int.MaxValue)));

        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        for (int i = 0; i < WedgeObjs.Length; i++)
            WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[4];

        _displayOrder = Enumerable.Range(0, 16).ToArray().Shuffle();

        _maze = _mazeGenerator.GenerateMaze();


        var ch = _maze.ToCharArray();
        for (int i = 1; i < ch.Length; i += 2)
            if (ch[i] == '█' && i % 2 == 1)
                ch[i] = (char)(Rnd.Range(0, 4) + '0');

        _innerWallIxs = Enumerable.Range(0, _maze.Length).Where(i =>
            i % (_size * 2 + 1) != 0 &&
            i % (_size * 2 + 1) != (_size * 2) &&
            i / (_size * 2 + 1) != 0 &&
            i / (_size * 2 + 1) != (_size * 2) &&
            i % 2 == 1).ToArray();

        var nonWalls = _innerWallIxs.Where(i => ch[i] != ' ').ToArray();

        _submissionIx = nonWalls.PickRandom();
        ch[_submissionIx] = '?';
        _dummyColors = Enumerable.Range(0, 4).ToArray().Shuffle().Take(2).ToArray();
        _maze = ch.Join("");

        Debug.Log(_maze.Join(""));

        Debug.LogFormat("[Odd Wall Out #{0}] Maze:", _moduleId);
        for (int i = 0; i < 9; i++)
            Debug.LogFormat("[Odd Wall Out #{0}] {1}", _moduleId, _maze.Substring(i * 9, 9));
        Debug.LogFormat("[Odd Wall Out #{0}] The correct button to submit is button #{1}.", _moduleId, Array.IndexOf(_innerWallIxs, _submissionIx) + 1);
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, ButtonSels[btn].transform);
            ButtonSels[btn].AddInteractionPunch(0.3f);

            if (_moduleSolved)
                return false;
            if (btn == _previouslyPressedButton)
            {
                if (_innerWallIxs[btn] == _submissionIx)
                {
                    Debug.LogFormat("[Odd Wall Out #{0}] Correctly submitted inner wall #{1}. Module solved.", _moduleId, btn + 1);
                    _moduleSolved = true;
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    for (int i = 0; i < 4; i++)
                        WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[5];
                    Module.HandlePass();
                }
                else
                {
                    Debug.LogFormat("[Odd Wall Out #{0}] Incorrectly submitted inner wall #{1}. Strike.", _moduleId, btn + 1);
                    Module.HandleStrike();
                }
            }
            else
            {
                _previouslyPressedButton = btn;
                for (int i = 0; i < ButtonObjs.Length; i++)
                {
                    if (_previouslyPressedButton == i)
                        ButtonObjs[i].GetComponent<MeshRenderer>().material = ButtonMats[1];
                    else
                        ButtonObjs[i].GetComponent<MeshRenderer>().material = ButtonMats[0];
                }

                DisplayWalls(_displayOrder[_cycleIx]);
                _cycleIx = (_cycleIx + 1) % 16;
            }
            return false;
        };
    }

    private void DisplayWalls(int pos)
    {
        var p = GetTransformedPosition(pos);
        var arr = new int[] { p - (_size * 2 + 1), p + 1, p + (_size * 2 + 1), p - 1 };
        for (int i = 0; i < arr.Length; i++)
        {
            if (_maze[arr[i]] == '?' && i <= 1)
                WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[_dummyColors[0]];
            else if (_maze[arr[i]] == '?' && i >= 2)
                WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[_dummyColors[1]];
            else if (_maze[arr[i]] >= '0' && _maze[arr[i]] <= '3')
                WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[_maze[arr[i]] - '0'];
            else
                WedgeObjs[i].GetComponent<MeshRenderer>().material = WedgeMats[4];
        }
    }

    private int GetTransformedPosition(int pos)
    {
        return (pos / _size * (_size * 2 + 1) * 2) + (pos % _size * 2) + _size * 2 + 2;
    }
}
