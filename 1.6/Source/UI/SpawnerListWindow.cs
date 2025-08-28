namespace Rhynia.Overpower.UI;

public class SpawnerListWindow : Window
{
    private static List<ThingDef> ProductDefs => Building_ThingSpawnerEx.ProductDefs;
    private Dictionary<ThingDef, int> Quantities => _building.Quantities;

    private readonly Building_ThingSpawnerEx _building;
    private Vector2 _scrollPosition = Vector2.zero;
    private string _searchString = "";

    public SpawnerListWindow(Building_ThingSpawnerEx building)
    {
        _building = building;

        doCloseX = true;
        draggable = true;
        resizeable = true;
        forcePause = true;
    }

    private const float RowHeight = 70f;
    private const float IconSize = 40f;
    private const float Padding = 10f;
    private const float SearchBoxHeight = 30f;
    private const float TitleHeight = 40f;
    private const float ScrollBarWidth = 10f;

    public override Vector2 InitialSize => new(800f, 600f);

    public override void DoWindowContents(Rect inRect)
    {
        var curY = 0f;

        // Title area
        var titleRect = new Rect(Padding, curY, inRect.width - 2 * Padding, TitleHeight);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(titleRect, "RhyniaOverpower_ThingSpawnerEx_Window_Title".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        curY += TitleHeight + Padding;

        // Search box
        var searchRect = new Rect(Padding, curY, inRect.width - 2 * Padding, SearchBoxHeight);
        var newSearchString = Widgets.TextField(searchRect, _searchString);
        if (newSearchString != _searchString)
            _searchString = newSearchString;

        curY += SearchBoxHeight + Padding;

        // Filtered item list
        var filteredDefs = GetFilteredDefs();

        // Scroll area
        var scrollRect = new Rect(0f, curY, inRect.width - ScrollBarWidth, inRect.height - curY);
        var viewRect = new Rect(0f, 0f, scrollRect.width - 16f, filteredDefs.Count * RowHeight);

        Widgets.BeginScrollView(scrollRect, ref _scrollPosition, viewRect);

        var itemY = 0f;
        foreach (var def in filteredDefs)
        {
            DrawItemRow(new Rect(Padding, itemY, viewRect.width - 2 * Padding, RowHeight), def);
            itemY += RowHeight;
        }

        Widgets.EndScrollView();
    }

    private void DrawItemRow(Rect rect, ThingDef def)
    {
        if (Mouse.IsOver(rect))
            Widgets.DrawHighlight(rect);

        var curX = Padding;

        // Icon with border
        var iconPadding = 6f;
        var iconTotalSize = IconSize + iconPadding * 2;
        var iconBackgroundRect = new Rect(
            curX,
            rect.y + (rect.height - iconTotalSize) / 2,
            iconTotalSize,
            iconTotalSize
        );
        var iconRect = new Rect(
            curX + iconPadding,
            rect.y + (rect.height - IconSize) / 2,
            IconSize,
            IconSize
        );

        // Icon outline box
        Widgets.DrawBoxSolid(iconBackgroundRect, Color.grey);
        Widgets.DrawBox(iconBackgroundRect);
        Widgets.ThingIcon(iconRect, def);
        curX += iconTotalSize + Padding;

        // Label area
        var labelAreaWidth = rect.width - (curX - rect.x) - 200f; // 200px for controls
        var labelAreaRect = new Rect(curX, rect.y, labelAreaWidth, rect.height);

        // Item label
        var mainLabelHeight = 24f;
        var mainLabelRect = new Rect(
            labelAreaRect.x,
            labelAreaRect.y + 8f,
            labelAreaRect.width,
            mainLabelHeight
        );
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(mainLabelRect, def.LabelCap);

        // Mod info
        var modLabelRect = new Rect(
            labelAreaRect.x,
            mainLabelRect.yMax + 2f,
            labelAreaRect.width,
            16f
        );
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = Color.gray;
        var modName = def.modContentPack?.Name ?? "Unknown Mod";
        Widgets.Label(modLabelRect, $"From: {modName}");
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        // Right controls area
        var toggleWidth = 24f;
        var toggleX = rect.xMax - toggleWidth - 8f;
        var toggleRect = new Rect(
            toggleX,
            rect.y + (rect.height - toggleWidth) / 2,
            toggleWidth,
            toggleWidth
        );

        // Toggle
        var currentQuantity = Quantities.GetValueOrDefault(def, 0);
        var isEnabled = currentQuantity > 0;
        var newEnabled = isEnabled;
        Widgets.Checkbox(toggleRect.position, ref newEnabled);

        if (newEnabled != isEnabled)
        {
            if (newEnabled)
            {
                // Enable as 1
                Quantities[def] = 1;
                OnStateChanged(def, 1);
            }
            else
            {
                // Disable by removing from dictionary
                Quantities.Remove(def);
                OnStateChanged(def, 0);
            }
        }

        if (Quantities.GetValueOrDefault(def, 0) > 0)
        {
            var quantityControlWidth = 120f;
            var quantityX = toggleX - quantityControlWidth - 8f;
            var controlsY = rect.y + (rect.height - 30f) / 2;
            var quantityRect = new Rect(quantityX, controlsY, quantityControlWidth, 30f);
            DrawQuantityControl(quantityRect, def);
        }
    }

    private void DrawQuantityControl(Rect rect, ThingDef def)
    {
        var currentQuantity = Quantities.GetValueOrDefault(def, 0);

        // Minus button
        var buttonWidth = 25f;
        var minusRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
        if (Widgets.ButtonText(minusRect, "-"))
        {
            if (currentQuantity <= 1)
            {
                // If reducing from 1 or less, remove from dictionary
                Quantities.Remove(def);
                OnStateChanged(def, 0);
            }
            else
            {
                var newQty = currentQuantity - 1;
                Quantities[def] = newQty;
                OnStateChanged(def, newQty);
            }
        }

        // Quantity input field
        var inputWidth = rect.width - 2 * buttonWidth;
        var inputRect = new Rect(rect.x + buttonWidth, rect.y, inputWidth, rect.height);
        var quantityString = currentQuantity.ToString();
        var newQuantityString = Widgets.TextField(inputRect, quantityString);

        if (
            newQuantityString != quantityString
            && int.TryParse(newQuantityString, out var newQuantity)
        )
        {
            if (newQuantity <= 0)
            {
                // Remove from dictionary if quantity is 0 or negative
                Quantities.Remove(def);
                OnStateChanged(def, 0);
            }
            else
            {
                Quantities[def] = newQuantity;
                OnStateChanged(def, newQuantity);
            }
        }

        // Add button
        var plusRect = new Rect(
            rect.x + buttonWidth + inputWidth,
            rect.y,
            buttonWidth,
            rect.height
        );
        if (Widgets.ButtonText(plusRect, "+"))
        {
            var newQty = currentQuantity + 1;
            Quantities[def] = newQty;
            OnStateChanged(def, newQty);
        }
    }

    private List<ThingDef> GetFilteredDefs()
    {
        if (string.IsNullOrEmpty(_searchString))
            return ProductDefs;

        return
        [
            .. ProductDefs.Where(def =>
                def.LabelCap.ToString().ToLower().Contains(_searchString.ToLower())
                || def.defName.ToLower().Contains(_searchString.ToLower())
            ),
        ];
    }

    private void OnStateChanged(ThingDef def, int quantity) =>
        _building.Notify_StateChanged(def, quantity);
}
