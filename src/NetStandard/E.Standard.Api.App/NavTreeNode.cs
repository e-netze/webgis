using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App;

public class NavTreeNode
{
    public string Title { get; set; }

    public string Description { get; set; }

    public string Prefix { get; set; }

    public string Url { get; set; }

    public List<NavTreeNode> _nodes = new List<NavTreeNode>();
    public IEnumerable<NavTreeNode> Nodes { get { return _nodes; } }

    public void AddNode(NavTreeNode node)
    {
        _nodes.Add(node);
    }

    public string Render()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<div id=" + Guid.NewGuid().ToString("N").ToLower() + " class='treenode' data-url='" + this.Url + "'>");
        sb.Append("<div class='treenode-title' alt='" + this.Description + "' title='" + this.Description + "'>" +
            (_nodes.Count > 0 ? "<span class='treenode-collapse'>[ + ]</span>" : "") +
            "<span class='treenode-title-prefix " + this.Prefix?.ToLower().Replace("#", "sharp") + "'>" + this.Prefix + "</span><span>" + this.Title + "</span></div>");

        if (_nodes.Count > 0)
        {
            sb.Append("<div class='treenode-nodes' style='display:none'>");
            foreach (var node in this.Nodes)
            {
                sb.Append(node.Render());
            }

            sb.Append("</div>");
        }
        sb.Append("</div>");

        return sb.ToString();
    }
}