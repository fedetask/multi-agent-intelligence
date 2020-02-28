using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Node {
    public int[] cell;
    public Node parent;
    public int agent;
    public List<Node> childs;
    public List<float> path_costs;
    public Node(int[] cell, Node parent, int agent) {
        this.cell = cell;
        this.parent = parent;
        this.agent = agent;
        childs = new List<Node>();
        path_costs = null;
    }
    public Node Clone() {
        Node clone = new Node(new int[]{this.cell[0], this.cell[1]}, null, this.agent);
        List<Node> cloned_childs = new List<Node>();
        foreach (Node child in this.childs) {
            Node cloned_child = child.Clone();
            cloned_child.parent = clone;
            cloned_childs.Add(cloned_child);
        }
        clone.childs = cloned_childs;
        clone.path_costs = path_costs == null ? null : new List<float>(path_costs);
        return clone;
    }
    public override bool Equals(object obj) {
        Node other = obj as Node;
        if (other.cell[0] == cell[0] && other.cell[1] == cell[1] && other.parent == parent) {
            return true;
        } else { return false; }
    }
    public override int GetHashCode() {
        return cell[0] * 10000 + cell[1] * 100 + agent; 
    }
    public bool HasChild(Node child) {
        return childs.Contains(child);
    }

    public float MaxCostPath() {
        if (childs.Count == 0) {
            return 0;
        } else {
            return path_costs.Max();
        }
    }

    public override string ToString() {
        return "("+cell[0]+","+cell[1]+")"+"a"+agent;
    }

    public void SetAgent(int agent) {
        this.agent = agent;
        foreach (Node child in childs) {
            child.SetAgent(agent);
        }
    }
}