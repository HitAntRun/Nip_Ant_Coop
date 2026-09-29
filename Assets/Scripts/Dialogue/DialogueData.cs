using System.Collections.Generic;


[System.Serializable]
public class DialogueData
{
    public string storyId;
    public string startNodeId;
    public string background;
    public string nextScene;
    public string nextStage;
    public string chapterLabel;
    public int day;
    public string summary;
    public List<DialogueNode> nodes = new List<DialogueNode>();
    public string bgm;
    public bool skipLoading;
}

[System.Serializable]
public class DialogueNode
{
    public string id;
    public string locKey;
    public string speaker;
    public string text;
    public string portrait;
    public float portraitBrightness = 1f;
    public string next;
    public string jumpTo;
    public bool endHere;
    public List<Choice> choices = new List<Choice>();
    public List<ActorState> actors = new List<ActorState>();
    public bool shake;
    public string bgm;
    public string sfx;
    
    // CutScene
    public string mode;
    public string bg;
    public bool fadeBreak;
    public bool wipe;
    public float hold = -1f;
}

[System.Serializable]
public class Choice
{
    public string text;
    public string next;
}

[System.Serializable]
public class ActorState
{
    public string id;
    public string sprite;
    public float brightness = 1f;
    public string slot;
    public bool fadeIn;
    public bool flip;
}