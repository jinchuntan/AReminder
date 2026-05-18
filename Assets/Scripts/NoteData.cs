using System;
using System.Collections.Generic;

[Serializable]
public class NoteData
{
    public string noteId;
    public string title;
    public string content;
    public bool isVisible;
    public List<ChecklistItem> checklistItems;
    public string createdAt;
    public string updatedAt;

    // ---- Custom styling (added for "Custom Styling" assignment requirement)
    // priority is serialised as int because Unity's JsonUtility cannot store
    // enums named directly; the int is mapped to NotePriority on read.
    public int priority;          // 0 = Low, 1 = Medium, 2 = High
    public string colorLabelId;   // matches an entry in NoteStyleCatalog.ColorLabels
    public int icon;              // 0 = None ... see NoteIcon

    public NoteData(string title, string content)
    {
        noteId = Guid.NewGuid().ToString();
        this.title = title;
        this.content = content;
        isVisible = true;
        checklistItems = new List<ChecklistItem>();
        createdAt = DateTime.Now.ToString();
        updatedAt = DateTime.Now.ToString();

        // Sensible neutral defaults: no colour preset, no icon, lowest priority.
        // The user picks these values via the UI before/after creating the note.
        // NoteStyleCatalog.GetColorLabel(null) already falls back gracefully so
        // we do not pre-write any specific colour id here.
        priority = (int)NotePriority.Low;
        colorLabelId = string.Empty;
        icon = (int)NoteIcon.None;
    }

    // Convenience accessors so callers do not have to cast int <-> enum.
    public NotePriority Priority
    {
        get { return (NotePriority)priority; }
        set { priority = (int)value; }
    }

    public NoteIcon Icon
    {
        get { return (NoteIcon)icon; }
        set { icon = (int)value; }
    }
}
