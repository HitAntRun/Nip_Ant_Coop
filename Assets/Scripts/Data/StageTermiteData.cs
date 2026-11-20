using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TermiteSpawnInfo
{
    public enum AntType { Termite, BlackAnt }
    public AntType antType = AntType.Termite;
    public Vector3 localPosition;
    public Vector3 surfaceNormal = Vector3.forward;
    public float scale = 1f;
    public bool isReal = true;
    public string containerId = "";
}

[System.Serializable]
public class EndlessRespawnSettings
{
    [Header("Count")]
    public int initialCount = 20;
    public int maxAlive = 30;

    [Header("Respawn")]
    public float respawnDelay = 0.6f;
    public float extraSpawnStagger = 0.25f;
    public float popDuration = 0.25f;
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public float surfaceOffset = 0f;

    [Header("Placement (맵 로컬 단위)")]
    public float minDistFromCaught = 3f;
    public float minDistFromAlive = 1f;
    public float cursorAvoidPixels = 150f;
    public float maxSlopeAngle = 35f;
    public float edgeMargin = 1.5f;
    public float maxEdgeStep = 0.4f;
    public float candidateSpacing = 0.5f;
    public int sampleAttempts = 1500;
    public int maxCandidates = 200;

    [Header("Escalation (타이머 경과 초)")]
    public float escalateStartTime = 15f;
    public float escalateFullTime = 45f;
    public int maxSpawnPerCatch = 3;

    [Header("Termite2 비율")] [Range(0f, 1f)]
    public float termite2Ratio = 0f;
}

[CreateAssetMenu(fileName = "StageTermiteData", menuName = "Termite/Stage Data")]
public class StageTermiteData : ScriptableObject
{
    public int stageIndex;
    public List<TermiteSpawnInfo> termites = new List<TermiteSpawnInfo>();

    [Header("Endless Respawn (Stage5)")]
    public bool endlessRespawn = false;
    public EndlessRespawnSettings endless = new EndlessRespawnSettings();
}