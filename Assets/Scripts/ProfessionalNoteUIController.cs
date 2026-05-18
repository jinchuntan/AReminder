using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Drop-in professional UI for the AReminder project.
//
// Usage: attach this script to ANY empty GameObject in the scene
// (or just create one at runtime) and it will build the entire UI
// from code at start. No prefab wiring required. The legacy
// Canvas/NoteUIController still works untouched.
//
// This UI satisfies the "Custom Styling" assignment requirement by
// exposing customizable colour labels, icons, and priority markers
// for every note.
[DisallowMultipleComponent]
public class ProfessionalNoteUIController : MonoBehaviour
{
    // ----- Layout / theme constants ----------------------------------------
    private static readonly Color  ColBackdrop   = new Color(0.07f, 0.08f, 0.10f, 0.35f);
    private static readonly Color  ColPanel      = new Color(0.13f, 0.15f, 0.18f, 0.95f);
    private static readonly Color  ColPanelSoft  = new Color(0.18f, 0.20f, 0.24f, 1f);
    private static readonly Color  ColAccent     = new Color(0.35f, 0.62f, 1.00f, 1f);
    private static readonly Color  ColTextHi     = new Color(0.95f, 0.96f, 0.98f, 1f);
    private static readonly Color  ColTextLo     = new Color(0.70f, 0.74f, 0.80f, 1f);
    private static readonly Color  ColInputBg    = new Color(0.10f, 0.11f, 0.13f, 1f);
    private static readonly Color  ColInputBorder= new Color(0.25f, 0.28f, 0.34f, 1f);
    private static readonly Color  ColDanger     = new Color(0.93f, 0.34f, 0.34f, 1f);
    private static readonly Color  ColSuccess    = new Color(0.30f, 0.72f, 0.42f, 1f);
    private static readonly Color  ColMuted      = new Color(0.55f, 0.58f, 0.65f, 1f);

    // ----- Runtime state ---------------------------------------------------
    private string selectedNoteId;
    private TMP_InputField titleInput;
    private TMP_InputField contentInput;
    private TMP_InputField checklistInput;
    private RectTransform notesListContent;
    private TextMeshProUGUI emptyHint;
    private TextMeshProUGUI selectedNoteCaption;

    // Currently-being-edited style draft. All values start "unset" so the
    // UI does not visually preselect any priority / colour / icon. Users
    // pick these explicitly; if they create a note without picking we fall
    // back to the safe baseline (Low / first colour / no icon).
    private NotePriority? draftPriority = null;
    private string        draftColorId  = string.Empty;
    private NoteIcon?     draftIcon     = null;

    // Buttons that need their selected/idle visual updated
    private readonly List<(Button btn, NotePriority p)> priorityButtons = new List<(Button, NotePriority)>();
    private readonly List<(Button btn, string id)>      colorButtons    = new List<(Button, string)>();
    private readonly List<(Button btn, NoteIcon i)>     iconButtons     = new List<(Button, NoteIcon)>();

    private void Start()
    {
        BuildUI();
        RefreshStyleSelectors();
        RefreshNoteList();

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged += RefreshNoteList;
        }
    }

    private void OnDestroy()
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged -= RefreshNoteList;
        }
    }

    // =======================================================================
    // UI CONSTRUCTION
    // =======================================================================

    private void BuildUI()
    {
        // ---- root canvas -------------------------------------------------
        GameObject canvasGo = new GameObject("ProfessionalNoteCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the legacy canvas

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem if missing
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // dim backdrop so the legacy UI behind us is muted
        Image backdrop = AddImage(canvasGo.transform, "Backdrop", ColBackdrop);
        Stretch(backdrop.rectTransform);

        // ---- top app bar -------------------------------------------------
        Image appBar = AddImage(canvasGo.transform, "AppBar", ColPanel);
        AnchorTopStretch(appBar.rectTransform, 96);

        var titleLabel = AddText(appBar.transform, "Title", "AReminder", 38, FontStyles.Bold, ColTextHi);
        var titleRT = titleLabel.rectTransform;
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(32, 0);
        titleRT.offsetMax = new Vector2(-32, 0);
        titleLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var subtitle = AddText(appBar.transform, "Subtitle", "AR Sticky Notes  ·  Custom Styling", 22, FontStyles.Normal, ColTextLo);
        var subRT = subtitle.rectTransform;
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.offsetMin = new Vector2(32, 0);
        subRT.offsetMax = new Vector2(-32, 0);
        subtitle.alignment = TextAlignmentOptions.MidlineRight;

        // ---- composer card ----------------------------------------------
        Image card = AddImage(canvasGo.transform, "ComposerCard", ColPanel);
        AnchorTopStretch(card.rectTransform, 760, marginTop: 112, marginX: 24);
        AddRoundedFeel(card);

        float padX = 24, y = -24;

        // selected caption
        selectedNoteCaption = AddText(card.transform, "SelectedNote", "New note", 22, FontStyles.Italic, ColMuted);
        PlaceTopLeft(selectedNoteCaption.rectTransform, padX, y, 1000, 28);
        y -= 36;

        // Title input
        titleInput = AddInput(card.transform, "TitleInput", "Title", "Enter note title", isMultiLine: false);
        PlaceTopLeft(titleInput.GetComponent<RectTransform>(), padX, y, -1, 56, stretchX: true, marginRight: padX);
        y -= 70;

        // Content input
        contentInput = AddInput(card.transform, "ContentInput", "Content", "Enter note content / annotations", isMultiLine: true);
        PlaceTopLeft(contentInput.GetComponent<RectTransform>(), padX, y, -1, 110, stretchX: true, marginRight: padX);
        y -= 124;

        // Style chooser: Priority / Color / Icon
        AddSectionHeader(card.transform, "PRIORITY", padX, ref y);
        BuildPrioritySelector(card.transform, padX, ref y);
        y -= 8;

        AddSectionHeader(card.transform, "COLOR LABEL", padX, ref y);
        BuildColorSelector(card.transform, padX, ref y);
        y -= 8;

        AddSectionHeader(card.transform, "ICON", padX, ref y);
        BuildIconSelector(card.transform, padX, ref y);
        y -= 12;

        // Checklist input row
        AddSectionHeader(card.transform, "CHECKLIST", padX, ref y);
        BuildChecklistRow(card.transform, padX, ref y);
        y -= 8;

        // Action buttons row
        BuildActionButtons(card.transform, padX, ref y);

        // ---- note list panel (under composer) ---------------------------
        Image listCard = AddImage(canvasGo.transform, "NotesCard", ColPanel);
        AnchorBottomStretch(listCard.rectTransform, 760, marginBottom: 24, marginX: 24);
        AddRoundedFeel(listCard);

        var listTitle = AddText(listCard.transform, "ListTitle", "Your notes", 26, FontStyles.Bold, ColTextHi);
        PlaceTopLeft(listTitle.rectTransform, 24, -20, 600, 32);

        // empty hint
        emptyHint = AddText(listCard.transform, "EmptyHint",
            "No notes yet. Use the form above to create your first AR sticky note.",
            20, FontStyles.Italic, ColMuted);
        var ehRT = emptyHint.rectTransform;
        ehRT.anchorMin = new Vector2(0, 0); ehRT.anchorMax = new Vector2(1, 1);
        ehRT.offsetMin = new Vector2(24, 24); ehRT.offsetMax = new Vector2(-24, -64);
        emptyHint.alignment = TextAlignmentOptions.Center;

        // scroll view for notes list
        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(listCard.transform, false);
        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.color = new Color(0,0,0,0); // invisible bg, used only as raycast target
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0); scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(16, 16);
        scrollRT.offsetMax = new Vector2(-16, -64);
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;

        // viewport
        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRT = viewportGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        viewportGo.GetComponent<Image>().color = new Color(0,0,0,0.001f); // mask requires graphic
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRT;

        // content
        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 10;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        notesListContent = contentRT;
    }

    // --- Section header text
    private void AddSectionHeader(Transform parent, string text, float padX, ref float y)
    {
        var t = AddText(parent, "Hdr_" + text, text, 18, FontStyles.Bold, ColMuted);
        t.characterSpacing = 8;
        PlaceTopLeft(t.rectTransform, padX, y, 600, 22);
        y -= 28;
    }

    private void BuildPrioritySelector(Transform parent, float padX, ref float y)
    {
        priorityButtons.Clear();
        float btnW = 220, btnH = 56, gap = 12;
        float x = padX;
        NotePriority[] order = { NotePriority.Low, NotePriority.Medium, NotePriority.High };
        foreach (var p in order)
        {
            string label = NoteStyleCatalog.GetPriorityLabel(p) + "  " + NoteStyleCatalog.GetPriorityMarker(p);
            Color tint = NoteStyleCatalog.GetPriorityColor(p);
            Button b = AddPillButton(parent, "Pri_" + p, label, tint, () => SelectPriority(p));
            PlaceTopLeft(b.GetComponent<RectTransform>(), x, y, btnW, btnH);
            priorityButtons.Add((b, p));
            x += btnW + gap;
        }
        y -= btnH + 8;
    }

    private void BuildColorSelector(Transform parent, float padX, ref float y)
    {
        colorButtons.Clear();
        float swatch = 56, gap = 10;
        float x = padX;
        foreach (var label in NoteStyleCatalog.ColorLabels)
        {
            string lid = label.id;
            Button b = AddSwatchButton(parent, "Col_" + lid, label.color, () => SelectColor(lid));
            PlaceTopLeft(b.GetComponent<RectTransform>(), x, y, swatch, swatch);
            colorButtons.Add((b, lid));
            x += swatch + gap;
        }
        y -= swatch + 8;
    }

    private void BuildIconSelector(Transform parent, float padX, ref float y)
    {
        iconButtons.Clear();
        float btn = 56, gap = 10;
        float x = padX;
        NoteIcon[] icons = { NoteIcon.Note, NoteIcon.Star, NoteIcon.Alarm,
                             NoteIcon.Heart, NoteIcon.Shopping, NoteIcon.Work,
                             NoteIcon.Study, NoteIcon.Home };
        foreach (var ic in icons)
        {
            NoteIcon captured = ic;
            Button b = AddIconButton(parent, "Icn_" + ic, NoteStyleCatalog.GetIconGlyph(ic), () => SelectIcon(captured));
            PlaceTopLeft(b.GetComponent<RectTransform>(), x, y, btn, btn);
            iconButtons.Add((b, ic));
            x += btn + gap;
        }
        y -= btn + 8;
    }

    private void BuildChecklistRow(Transform parent, float padX, ref float y)
    {
        // input
        checklistInput = AddInput(parent, "ChecklistInput", "", "Add a checklist item", false);
        PlaceTopLeft(checklistInput.GetComponent<RectTransform>(), padX, y, 700, 56);

        // add button
        Button add = AddSolidButton(parent, "AddChecklistBtn", "+ Add Item", ColAccent, ColTextHi, OnAddChecklist);
        PlaceTopLeft(add.GetComponent<RectTransform>(), padX + 712, y, 220, 56);

        // toggle first
        Button toggle = AddSolidButton(parent, "ToggleChecklistBtn", "Tick / Untick #1", ColPanelSoft, ColTextHi, OnToggleFirst);
        PlaceTopLeft(toggle.GetComponent<RectTransform>(), padX + 712 + 232, y, 220, 56);

        y -= 70;
    }

    private void BuildActionButtons(Transform parent, float padX, ref float y)
    {
        float h = 64, gap = 12;
        float x = padX;

        Button add = AddSolidButton(parent, "AddBtn", "Add Note", ColAccent, ColTextHi, OnAdd);
        PlaceTopLeft(add.GetComponent<RectTransform>(), x, y, 220, h); x += 220 + gap;

        Button edit = AddSolidButton(parent, "EditBtn", "Save Edit", ColSuccess, ColTextHi, OnEdit);
        PlaceTopLeft(edit.GetComponent<RectTransform>(), x, y, 220, h); x += 220 + gap;

        Button hide = AddSolidButton(parent, "HideBtn", "Hide / Show", ColPanelSoft, ColTextHi, OnToggleVisibility);
        PlaceTopLeft(hide.GetComponent<RectTransform>(), x, y, 220, h); x += 220 + gap;

        Button del = AddSolidButton(parent, "DelBtn", "Delete", ColDanger, ColTextHi, OnDelete);
        PlaceTopLeft(del.GetComponent<RectTransform>(), x, y, 220, h);

        y -= h + 4;
    }

    // =======================================================================
    // STYLE SELECTOR LOGIC
    // =======================================================================

    private void SelectPriority(NotePriority p)
    {
        draftPriority = p;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
        {
            NoteManager.Instance.SetNotePriority(selectedNoteId, p);
        }
        RefreshStyleSelectors();
    }

    private void SelectColor(string id)
    {
        draftColorId = id;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
        {
            NoteManager.Instance.SetNoteColor(selectedNoteId, id);
        }
        RefreshStyleSelectors();
    }

    private void SelectIcon(NoteIcon i)
    {
        draftIcon = i;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
        {
            NoteManager.Instance.SetNoteIcon(selectedNoteId, i);
        }
        RefreshStyleSelectors();
    }

    private void RefreshStyleSelectors()
    {
        foreach (var (btn, p) in priorityButtons)
        {
            bool sel = draftPriority.HasValue && (p == draftPriority.Value);
            var img = btn.GetComponent<Image>();
            Color tint = NoteStyleCatalog.GetPriorityColor(p);
            img.color = sel ? tint : new Color(tint.r * 0.4f, tint.g * 0.4f, tint.b * 0.4f, 1f);
            var t = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.color = sel ? Color.white : ColTextHi;
            SetOutline(btn.gameObject, sel ? Color.white : new Color(0,0,0,0));
        }
        foreach (var (btn, id) in colorButtons)
        {
            bool sel = !string.IsNullOrEmpty(draftColorId) && (id == draftColorId);
            SetOutline(btn.gameObject, sel ? ColAccent : new Color(0,0,0,0));
        }
        foreach (var (btn, ic) in iconButtons)
        {
            bool sel = draftIcon.HasValue && (ic == draftIcon.Value);
            var img = btn.GetComponent<Image>();
            img.color = sel ? ColAccent : ColPanelSoft;
        }
    }

    // =======================================================================
    // CRUD ACTIONS
    // =======================================================================

    private void OnAdd()
    {
        if (titleInput == null || NoteManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(titleInput.text))
        {
            Debug.LogWarning("Title cannot be empty.");
            return;
        }

        NoteData note = NoteManager.Instance.AddNote(titleInput.text, contentInput.text);
        // Apply whatever the user explicitly picked. If they did not touch
        // a selector, leave the field unset so we do not invent a preset.
        if (draftPriority.HasValue)
        {
            NoteManager.Instance.SetNotePriority(note.noteId, draftPriority.Value);
        }
        if (!string.IsNullOrEmpty(draftColorId))
        {
            NoteManager.Instance.SetNoteColor(note.noteId, draftColorId);
        }
        if (draftIcon.HasValue)
        {
            NoteManager.Instance.SetNoteIcon(note.noteId, draftIcon.Value);
        }
        SelectNote(note.noteId);
    }

    private void OnEdit()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.EditNote(selectedNoteId, titleInput.text, contentInput.text);
    }

    private void OnDelete()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.DeleteNote(selectedNoteId);
        SelectNote(null);
        titleInput.text = ""; contentInput.text = ""; checklistInput.text = "";
    }

    private void OnToggleVisibility()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.ToggleNoteVisibility(selectedNoteId);
    }

    private void OnAddChecklist()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("Select or create a note before adding checklist items.");
            return;
        }
        if (string.IsNullOrWhiteSpace(checklistInput.text)) return;
        NoteManager.Instance.AddChecklistItem(selectedNoteId, checklistInput.text);
        checklistInput.text = "";
    }

    private void OnToggleFirst()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        var n = NoteManager.Instance.FindNoteById(selectedNoteId);
        if (n == null || n.checklistItems.Count == 0) return;
        NoteManager.Instance.ToggleChecklistItem(selectedNoteId, 0);
    }

    private void SelectNote(string id)
    {
        selectedNoteId = id;
        if (string.IsNullOrEmpty(id))
        {
            selectedNoteCaption.text = "New note";
            // Wipe draft so the selectors return to "no preselection".
            draftPriority = null;
            draftColorId  = string.Empty;
            draftIcon     = null;
            RefreshStyleSelectors();
            return;
        }
        NoteData n = NoteManager.Instance.FindNoteById(id);
        if (n == null)
        {
            selectedNoteCaption.text = "New note";
            return;
        }
        titleInput.text   = n.title;
        contentInput.text = n.content;
        draftPriority = n.Priority;
        draftColorId  = n.colorLabelId;
        draftIcon     = n.Icon;
        selectedNoteCaption.text = "Editing: " + (string.IsNullOrEmpty(n.title) ? "(untitled)" : n.title);
        RefreshStyleSelectors();
    }

    // =======================================================================
    // NOTE LIST
    // =======================================================================

    private void RefreshNoteList()
    {
        if (notesListContent == null) return;

        // clear
        for (int i = notesListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(notesListContent.GetChild(i).gameObject);
        }

        var notes = (NoteManager.Instance != null) ? NoteManager.Instance.GetAllNotes() : null;
        bool empty = (notes == null || notes.Count == 0);
        if (emptyHint != null) emptyHint.gameObject.SetActive(empty);

        if (empty) return;

        for (int i = 0; i < notes.Count; i++)
        {
            BuildNoteRow(notes[i]);
        }
    }

    private void BuildNoteRow(NoteData note)
    {
        Color bg = NoteStyleCatalog.GetColorLabel(note.colorLabelId).color;
        Color fg = NoteStyleCatalog.GetReadableTextColor(bg);
        Color pri = NoteStyleCatalog.GetPriorityColor(note.Priority);

        GameObject row = new GameObject("Note_" + note.noteId, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
        row.transform.SetParent(notesListContent, false);
        var img = row.GetComponent<Image>();
        img.color = bg;
        var le = row.GetComponent<LayoutElement>();
        le.minHeight = 96; le.preferredHeight = 96;
        var btn = row.GetComponent<Button>();
        string capturedId = note.noteId;
        btn.onClick.AddListener(() => SelectNote(capturedId));

        // priority strip on the left
        Image strip = AddImage(row.transform, "Strip", pri);
        var sRT = strip.rectTransform;
        sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(0, 1);
        sRT.pivot = new Vector2(0, 0.5f);
        sRT.anchoredPosition = new Vector2(0, 0);
        sRT.sizeDelta = new Vector2(8, 0);

        // icon
        var icon = AddText(row.transform, "Icon", NoteStyleCatalog.GetIconGlyph(note.Icon), 36, FontStyles.Normal, fg);
        var iRT = icon.rectTransform;
        iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
        iRT.pivot = new Vector2(0, 0.5f);
        iRT.anchoredPosition = new Vector2(24, 0);
        iRT.sizeDelta = new Vector2(48, 48);
        icon.alignment = TextAlignmentOptions.Center;

        // title
        var title = AddText(row.transform, "Title",
            (string.IsNullOrEmpty(note.title) ? "(untitled)" : note.title),
            22, FontStyles.Bold, fg);
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0, 0.5f); tRT.anchorMax = new Vector2(1, 1);
        tRT.offsetMin = new Vector2(80, -2); tRT.offsetMax = new Vector2(-180, -8);
        title.alignment = TextAlignmentOptions.BottomLeft;

        // body line
        string body = note.content;
        if (note.checklistItems != null && note.checklistItems.Count > 0)
        {
            int done = 0; for (int i = 0; i < note.checklistItems.Count; i++) if (note.checklistItems[i].isCompleted) done++;
            body = body + "   ·   " + done + "/" + note.checklistItems.Count + " done";
        }
        var bodyText = AddText(row.transform, "Body", body, 18, FontStyles.Normal, new Color(fg.r, fg.g, fg.b, 0.85f));
        var bRT = bodyText.rectTransform;
        bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 0.5f);
        bRT.offsetMin = new Vector2(80, 8); bRT.offsetMax = new Vector2(-180, 0);
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Ellipsis;

        // priority chip on the right
        Image chip = AddImage(row.transform, "Chip", pri);
        var cRT = chip.rectTransform;
        cRT.anchorMin = new Vector2(1, 0.5f); cRT.anchorMax = new Vector2(1, 0.5f);
        cRT.pivot = new Vector2(1, 0.5f);
        cRT.anchoredPosition = new Vector2(-16, 18);
        cRT.sizeDelta = new Vector2(140, 32);
        var chipText = AddText(chip.transform, "ChipTxt",
            NoteStyleCatalog.GetPriorityLabel(note.Priority) + " " + NoteStyleCatalog.GetPriorityMarker(note.Priority),
            16, FontStyles.Bold, Color.white);
        Stretch(chipText.rectTransform);
        chipText.alignment = TextAlignmentOptions.Center;

        // hidden tag
        if (!note.isVisible)
        {
            var hiddenTag = AddText(row.transform, "HiddenTag", "HIDDEN", 14, FontStyles.Bold, ColMuted);
            var hRT = hiddenTag.rectTransform;
            hRT.anchorMin = new Vector2(1, 0.5f); hRT.anchorMax = new Vector2(1, 0.5f);
            hRT.pivot = new Vector2(1, 0.5f);
            hRT.anchoredPosition = new Vector2(-16, -18);
            hRT.sizeDelta = new Vector2(80, 22);
            hiddenTag.alignment = TextAlignmentOptions.Center;
        }
    }

    // =======================================================================
    // GENERIC UI HELPERS
    // =======================================================================

    private Image AddImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI AddText(Transform parent, string name, string text, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.raycastTarget = false;
        return t;
    }

    private TMP_InputField AddInput(Transform parent, string name, string label, string placeholder, bool isMultiLine)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var bg = go.GetComponent<Image>();
        bg.color = ColInputBg;
        SetOutline(go, ColInputBorder);

        var input = go.AddComponent<TMP_InputField>();
        input.lineType = isMultiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

        // text area
        GameObject ta = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(go.transform, false);
        var taRT = ta.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(16, 8); taRT.offsetMax = new Vector2(-16, -8);

        var ph = AddText(ta.transform, "Placeholder", placeholder, 22, FontStyles.Italic, new Color(0.65f,0.68f,0.74f,1));
        Stretch(ph.rectTransform);
        var txt = AddText(ta.transform, "Text", "", 22, FontStyles.Normal, ColTextHi);
        Stretch(txt.rectTransform);

        input.textViewport = taRT;
        input.textComponent = txt;
        input.placeholder = ph;

        return input;
    }

    private Button AddSolidButton(Transform parent, string name, string text, Color bg, Color fg, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        var label = AddText(go.transform, "Label", text, 22, FontStyles.Bold, fg);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        return b;
    }

    private Button AddPillButton(Transform parent, string name, string text, Color bg, System.Action onClick)
    {
        Button b = AddSolidButton(parent, name, text, bg, Color.white, onClick);
        return b;
    }

    private Button AddSwatchButton(Transform parent, string name, Color color, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        SetOutline(go, new Color(0,0,0,0));
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        return b;
    }

    private Button AddIconButton(Transform parent, string name, string glyph, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = ColPanelSoft;
        var label = AddText(go.transform, "Glyph", glyph, 28, FontStyles.Normal, ColTextHi);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        return b;
    }

    private void SetOutline(GameObject go, Color color)
    {
        var ol = go.GetComponent<Outline>();
        if (color.a == 0)
        {
            if (ol != null) ol.enabled = false;
            return;
        }
        if (ol == null) ol = go.AddComponent<Outline>();
        ol.enabled = true;
        ol.effectColor = color;
        ol.effectDistance = new Vector2(2, -2);
    }

    private void AddRoundedFeel(Image img)
    {
        // Without per-corner radius support in built-in Image we fake a card
        // feel with a shadow and a slightly lighter outline.
        var shadow = img.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.45f);
        shadow.effectDistance = new Vector2(0, -6);
        SetOutline(img.gameObject, new Color(1, 1, 1, 0.05f));
    }

    // ----- placement helpers ----------------------------------------------

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private void AnchorTopStretch(RectTransform rt, float height, float marginTop = 0, float marginX = 0)
    {
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -marginTop);
        rt.offsetMin = new Vector2(marginX, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-marginX, rt.offsetMax.y);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private void AnchorBottomStretch(RectTransform rt, float height, float marginBottom = 0, float marginX = 0)
    {
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, marginBottom);
        rt.offsetMin = new Vector2(marginX, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-marginX, rt.offsetMax.y);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h, bool stretchX = false, float marginRight = 0)
    {
        rt.pivot = new Vector2(0, 1);
        if (stretchX)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(x, 0);
            rt.offsetMax = new Vector2(-marginRight, 0);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
