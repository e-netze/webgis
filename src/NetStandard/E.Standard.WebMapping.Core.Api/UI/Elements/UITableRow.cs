using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UITableRow : UIElement
{
    public UITableRow(IUIElement[] cellElments, bool isHeader = false, string[] values = null)
        : base("tr")
    {
        List<Cell> cells = new List<Cell>();
        if (cellElments != null)
        {
            for (int i = 0; i < cellElments.Length; i++)
            {
                var cellElement = cellElments[i];
                Cell cell = isHeader ? new HeaderCell() : new Cell();
                cell.elements = new IUIElement[] { cellElement };
                cell.css = cellElement.css;

                if (values != null && values.Length > i)
                {
                    cell.value = values[i];
                }

                cells.Add(cell);
                cellElement.css = null;
            }
        }
        this.elements = cells.ToArray();
    }

    public void AddCell(UIElement element)
    {
        List<IUIElement> list = this.elements == null ? new List<IUIElement>() : new List<IUIElement>(this.elements);
        list.Add(new Cell()
        {
            elements = new IUIElement[] { element },
            css = element.css
        });
        element.css = null;
        this.elements = list.ToArray();
    }

    private class Cell : UIElement
    {
        //public Cell() : base("td") { }
        public Cell(string elementType = "td") : base(elementType) { }
    }

    private class HeaderCell : Cell
    {
        public HeaderCell() : base("th") { }
    }
}
