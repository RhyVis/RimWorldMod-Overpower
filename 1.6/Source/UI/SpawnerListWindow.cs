namespace Rhynia.Overpower.UI;

public class SpawnerListWindow : Window
{
    private static List<ThingDef> ProductDefs => Building_ThingSpawnerEx.ProductDefs;
    private Dictionary<ThingDef, int> Quantities => _building.Quantities;
    private readonly List<ThingDef> _sortedProductDefs;

    private readonly Building_ThingSpawnerEx _building;
    private Vector2 _scrollPosition = Vector2.zero;
    private string _searchString = "";

    public SpawnerListWindow(Building_ThingSpawnerEx building)
    {
        _building = building;
        _sortedProductDefs =
        [
            .. ProductDefs.Where(Quantities.ContainsKey).OrderByDescending(def => Quantities[def]),
            .. ProductDefs.Where(def => !Quantities.ContainsKey(def)),
        ];

        doCloseX = true;
        draggable = true;
        forcePause = true;
    }

    private const float RowHeight = 70f;
    private const float IconSize = 40f;
    private const float Padding = 10f;
    private const float SearchBoxHeight = 30f;
    private const float TitleHeight = 40f;
    private const float ScrollBarWidth = 6f;

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

        // Filtered item list with virtualization
        var filteredDefs = GetFilteredDefs();
        DrawVirtualizedList(new Rect(0f, curY, inRect.width, inRect.height - curY), filteredDefs);
    }

    private void DrawVirtualizedList(Rect listRect, List<ThingDef> items)
    {
        var totalHeight = items.Count * RowHeight;
        var scrollRect = new Rect(0f, listRect.y, listRect.width - ScrollBarWidth, listRect.height);
        var viewRect = new Rect(0f, 0f, scrollRect.width - 16f, totalHeight);

        Widgets.BeginScrollView(scrollRect, ref _scrollPosition, viewRect);

        // Calculate visible area
        var visibleTop = _scrollPosition.y;
        var visibleBottom = _scrollPosition.y + scrollRect.height;

        // Calculate item index range to render
        var firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(visibleTop / RowHeight) - 1); // -1 for buffer
        var lastVisibleIndex = Mathf.Min(
            items.Count - 1,
            Mathf.CeilToInt(visibleBottom / RowHeight) + 1
        ); // +1 for buffer

        // Only render visible items
        for (var i = firstVisibleIndex; i <= lastVisibleIndex; i++)
        {
            var itemY = i * RowHeight;
            var itemRect = new Rect(Padding, itemY, viewRect.width - 2 * Padding, RowHeight);
            DrawItemRow(itemRect, items[i]);
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
        Widgets.Label(modLabelRect, $"Mod: {modName}");
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
            return _sortedProductDefs;

        var searchTerm = _searchString.ToLower();
        var (modFilter, nameFilter) = ParseSearchString(searchTerm);

        return
        [
            .. _sortedProductDefs.Where(def =>
            {
                var modMatch =
                    modFilter is null
                    || (def.modContentPack?.Name ?? "Unknown Mod").ToLower().Contains(modFilter);

                var nameMatch =
                    nameFilter is null
                    || def.LabelCap.ToString().ToLower().Contains(nameFilter)
                    || def.defName.ToLower().Contains(nameFilter);

                return modMatch && nameMatch;
            }),
        ];
    }

    private static (string? modFilter, string? nameFilter) ParseSearchString(string searchTerm)
    {
        string? modFilter = null;
        string? nameFilter = null;

        // 1. "@mod" - 仅按模组名搜索
        // 2. "name" - 仅按物品名搜索
        // 3. "@mod:name" - 同时指定模组和物品名
        // 4. "@mod name" - 模组名后跟空格，然后是物品名
        // 5. "name @mod" - 物品名后跟模组名

        if (searchTerm.Contains('@'))
        {
            if (searchTerm.Contains(':'))
            {
                // 模式: "@mod:name"
                var parts = searchTerm.Split(':', 2);
                if (parts[0].StartsWith('@'))
                {
                    modFilter = parts[0][1..].Trim();
                    nameFilter = parts[1].Trim();
                }
            }
            else
            {
                // 处理包含 @ 但没有 : 的情况
                var atIndex = searchTerm.IndexOf('@');

                if (atIndex == 0)
                {
                    // 以 @ 开头: "@mod name" 或 "@mod"
                    var afterAt = searchTerm[1..];
                    var spaceIndex = afterAt.IndexOf(' ');

                    if (spaceIndex > 0)
                    {
                        modFilter = afterAt[..spaceIndex].Trim();
                        nameFilter = afterAt[(spaceIndex + 1)..].Trim();
                    }
                    else
                    {
                        modFilter = afterAt.Trim();
                    }
                }
                else
                {
                    // @ 在中间或末尾: "name @mod"
                    var namePart = searchTerm[..atIndex].Trim();
                    var modPart = searchTerm[(atIndex + 1)..].Trim();

                    nameFilter = namePart;
                    modFilter = modPart;
                }
            }
        }
        else
        {
            // 没有 @，纯物品名搜索
            nameFilter = searchTerm.Trim();
        }

        // 清理空字符串，转换为 null
        modFilter = string.IsNullOrWhiteSpace(modFilter) ? null : modFilter;
        nameFilter = string.IsNullOrWhiteSpace(nameFilter) ? null : nameFilter;

        return (modFilter, nameFilter);
    }

    private void OnStateChanged(ThingDef def, int quantity) =>
        _building.Notify_StateChanged(def, quantity);
}
