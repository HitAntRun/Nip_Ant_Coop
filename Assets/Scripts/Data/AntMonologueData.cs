using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AntMonologueData", menuName = "Termite/Ant Monologue Data")]
public class AntMonologueData : ScriptableObject
{
    [TextArea] public List<string> lines = new List<string>();
    [TextArea] public List<string> termiteFoundLines = new List<string>();

    [Header("총(가늠좌) 모드일 때 신고당한 흰 개미 대사 (비우면 termiteFoundLines 사용)")]
    [TextArea] public List<string> gunTermiteFoundLines = new List<string>();
}
