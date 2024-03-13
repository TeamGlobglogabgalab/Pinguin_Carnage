using Godot;
using PinguinCarnage.Prefabs.Vehicles.Wheels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinguinCarnage.Tools
{
    public static class NodeTools
    {
        public static List<T> GetNodesOfType<T>(Node node, bool recursiveSearch = true)
        {
            List<T> result = new();
            if (node is T nodeType) result.Add(nodeType);

            foreach (var c in node.GetChildren())
            {
                if (recursiveSearch && c.GetChildCount() > 0)
                    c.GetChildren().ToList().ForEach(c => result.AddRange(GetNodesOfType<T>(c)));
                
                if (c is T childType) result.Add(childType);
            }
            return result;
        }
    }
}
