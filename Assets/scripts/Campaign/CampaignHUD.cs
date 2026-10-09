using UnityEngine;

public class CampaignHUD : MonoBehaviour
{
    public CampaignDirector director;
    GUIStyle heading, title, copy, small, button;
    readonly Color ink = new Color(0.045f, 0.068f, 0.071f, 0.97f);
    readonly Color accent = new Color(0.87f, 0.69f, 0.38f);

    void Styles()
    {
        if (heading != null) return;
        heading = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        heading.normal.textColor = accent;
        title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
        copy = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        small.normal.textColor = new Color(0.66f, 0.74f, 0.73f);
        button = new GUIStyle(GUI.skin.button) { fontSize = 16, padding = new RectOffset(15, 15, 12, 12) };
    }

    void Panel(Rect rect)
    {
        Color previous = GUI.color;
        GUI.color = ink;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void OnGUI()
    {
        if (!director || !director.manager || !director.player) return;
        Styles();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        var gm = director.manager;
        if (director.Briefing || gm.HasEnded)
        {
            Panel(new Rect(0, 0, 1280, 720));
            GUI.Label(new Rect(90, 68, 800, 30), "CURIOUS COMPANY    /    RESCUE OPERATIONS", heading);
            GUI.Label(new Rect(90, 120, 1090, 52),
                gm.HasEnded ? (gm.HasWon ? (director.mission == 9 ? "Operation complete" : "Evacuation complete") : "Team lost") : CampaignSession.Titles[director.mission], title);
            GUI.Label(new Rect(90, 190, 730, 80), gm.HasEnded
                ? (gm.HasWon ? "Your survivors made it out. Regroup before the next deployment." : "The player and every survivor must stay alive. Retry the same deployment and find another route.")
                : CampaignSession.Briefings[director.mission], copy);
            GUI.Label(new Rect(90, 287, 900, 30), $"MISSION {director.mission + 1:00} / 10     •     {CampaignSession.SurvivorCount(director.mission)} SURVIVORS     •     RUN {CampaignSession.Seed}", heading);
            GUI.Label(new Rect(90, 327, 850, 45), director.Conditions, small);
            if (director.Briefing)
            {
                GUI.Label(new Rect(90, 381, 700, 28), "DIFFICULTY", heading);
                GUI.enabled = !CampaignSession.Started;
                for (int i = 0; i < 3; i++)
                {
                    GUI.backgroundColor = (int)CampaignSession.Difficulty == i ? accent : Color.gray;
                    if (GUI.Button(new Rect(90 + i * 170, 419, 155, 44), ((CampaignDifficulty)i).ToString(), button))
                        CampaignSession.Difficulty = (CampaignDifficulty)i;
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
                GUI.Label(new Rect(90, 477, 920, 40), "Easy: more health, fewer slower enemies. Hard: more enemies and greater damage. Locked during a run.", small);
                if (GUI.Button(new Rect(90, 545, 245, 52), "Deploy →", button)) director.Begin();
                if (!string.IsNullOrEmpty(director.SetupError)) GUI.Label(new Rect(90, 605, 1000, 60), director.SetupError, copy);
            }
            else
            {
                if (gm.HasWon && director.mission < 9)
                {
                    if (GUI.Button(new Rect(90, 430, 245, 52), "Next mission →", button)) director.Next();
                }
                else if (!gm.HasWon && GUI.Button(new Rect(90, 430, 245, 52), "Retry mission", button)) director.Restart();
                if (GUI.Button(new Rect(360, 430, 245, 52), "New randomized run", button)) director.NewRun();
            }
            if (GUI.Button(new Rect(940, 610, 245, 44), "Main menu", button)) director.Menu();
        }
        else
        {
            Panel(new Rect(24, 24, 355, 116));
            GUI.Label(new Rect(44, 36, 335, 25), $"{director.mission + 1:00}   /   {CampaignSession.Titles[director.mission].ToUpperInvariant()}", heading);
            GUI.Label(new Rect(44, 69, 330, 28), $"Evacuated  {gm.rescued} / {gm.totalRescues}", copy);
            float health = Mathf.Clamp01(director.player.damage.health / director.player.damage.maxHealth);
            GUI.color = new Color(0.18f, 0.24f, 0.23f);
            GUI.DrawTexture(new Rect(44, 112, 310, 6), Texture2D.whiteTexture);
            GUI.color = new Color(0.49f, 0.74f, 0.64f);
            GUI.DrawTexture(new Rect(44, 112, 310 * health, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Panel(new Rect(24, 659, 930, 38));
            GUI.Label(new Rect(40, 668, 900, 27), "WASD  Move    •    Mouse  Aim / Fire    •    Space  Dash    •    Approach survivors to recruit    •    R  Retry", small);
            Panel(new Rect(1030, 24, 225, 70));
            float distance = Vector3.Distance(director.player.transform.position, director.extractionPosition);
            GUI.Label(new Rect(1046, 35, 210, 25), "EXTRACTION", heading);
            GUI.Label(new Rect(1046, 61, 210, 25), $"Green zone  ·  {distance:F0} m", small);
        }
        GUI.matrix = previous;
    }
}
