using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GraphSpanningTrees {

    int nagents;
    public Node[] roots;
    public Dictionary<string, Node> nodes; 
    public float[] costs;
    public GraphSpanningTrees(Dictionary<string, List<int[]>> dict, int[][] roots) {
        nagents = roots.Length;
        this.roots = new Node[nagents];
        for(int agent = 0; agent < nagents; agent++) {
            Node root = new Node(roots[agent], null, agent);
            this.roots[agent] = root;
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0) {
                Node node = queue.Dequeue();
                string key = KeyOf(node.cell, agent);
                foreach (int[] child_cell in dict[key]) {
                    Node child = new Node(child_cell, node, agent);
                    node.childs.Add(child);
                    queue.Enqueue(child);
                }
            }
        }
        this.nodes = DictionaryFromGraph(this.roots); 

        ComputeMaxCosts();
    }

    public GraphSpanningTrees() {}

    public static string KeyOf(int[] cell, int agent) { return cell[0]+","+cell[1]+":"+agent; }
    public bool Contains(int[] cell, int agent) {
        string key = KeyOf(cell, agent);
        return nodes.ContainsKey(key);
    }

    public Node GetNode(int[] cell, int agent) {
        return nodes[KeyOf(cell, agent)];
    }

    public bool RemoveCachedNode(Node node) {
        return nodes.Remove(KeyOf(node.cell, node.agent));
    }

    public void AddCachedNode(Node node) {
        nodes.Add(KeyOf(node.cell, node.agent), node);
    }

    public void ComputeMaxCosts() {
        for (int agent = 0; agent < nagents; agent++) {
            Stack<Node> stack = new Stack<Node>();
            HashSet<Node> to_visit = GetAllNodes(agent);
            stack.Push(roots[agent]);
            while(stack.Count > 0) {
                Node node = stack.Peek();
                int unvisited_childs = 0;
                foreach (Node child in node.childs) {
                    if (to_visit.Contains(child)) {
                        stack.Push(child);
                        unvisited_childs++;
                        to_visit.Remove(child);
                    }
                }
                if (unvisited_childs == 0) {
                    stack.Pop();
                    node.path_costs = new List<float>(node.childs.Count);
                    for (int child_idx = 0; child_idx < node.childs.Count; child_idx++) {
                        node.path_costs.Add(1 + node.childs[child_idx].MaxCostPath());
                    }
                }
            }
        }
    }

    public void ComputeCosts() {
        costs = new float[nagents];
        for (int i = 0; i < nagents; i++) {
            costs[i] = PathCost(i);
        }
    }

    public float MaxPathCost() {
        float max = float.MinValue;
        for (int i = 0; i < roots.Length; i++) {
            float cost = PathCost(i);
            max = Mathf.Max(max, cost);
        }
        return max;
    }
    public float PathCost(int agent) {
        float cost = 0;
        Node root = roots[agent];
        Stack<Node> stack =  new Stack<Node>();
        HashSet<Node> to_visit = GetAllNodes(agent);
        stack.Push(root);
        while (stack.Count > 0) {
            Node node = stack.Peek();
            int unvisited_childs = 0;
            foreach (Node child in node.childs.OrderBy(n => n.MaxCostPath()).ToList()) { // Take minimum
                if (to_visit.Contains(child)) {
                    to_visit.Remove(child);
                    unvisited_childs++;
                    stack.Push(child);
                    cost++;
                }
            }
            if (unvisited_childs == 0) {
                cost ++;
                stack.Pop();
            }
            if (to_visit.Count == 0) {
                break;
            }
        }
        return cost;
    }

    public GraphSpanningTrees Clone() {
        GraphSpanningTrees graph = new GraphSpanningTrees();
        graph.nagents = nagents;
        graph.roots = new Node[roots.Length];
        for (int i = 0; i < roots.Length; i++) {
            graph.roots[i] = roots[i].Clone();
        }
        graph.nodes = DictionaryFromGraph(graph.roots);
        graph.costs = costs.Clone() as float[];
        return graph;
    }

    public void Draw(TerrainInfo info) {
        Color[] colors = new Color[]{ Color.white, Color.yellow, Color.blue };
        for (int i = 0; i < nagents; i++) {
            Node root = roots[i];
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0) {
                Node node = queue.Dequeue();
                Vector3 node_coords = new Vector3(info.get_x_pos(node.cell[0]), 0, info.get_z_pos(node.cell[1]));
                foreach (Node child in node.childs) {
                    queue.Enqueue(child);
                    Vector3 child_coords = new Vector3(info.get_x_pos(child.cell[0]), 0, info.get_z_pos(child.cell[1]));
                    Debug.DrawLine(node_coords, child_coords, colors[i], 100f);
                }
            } 
        }
    }

    private static Dictionary<string, Node> DictionaryFromGraph(Node[] roots) {
        Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        for(int agent = 0; agent < roots.Length; agent++) {
            Node root = roots[agent];
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(root);
            while (queue.Count > 0) {
                Node node = queue.Dequeue();
                string key = KeyOf(node.cell, agent);
                nodes.Add(key, node);
                foreach (Node child in node.childs) {
                    queue.Enqueue(child);
                }
            }
        }
        return nodes;
    }

    public HashSet<Node> GetAllNodes(int agent) {
        HashSet<Node> res = new HashSet<Node>();
        foreach (string key in nodes.Keys) {
            if (nodes[key].agent == agent) {
                res.Add(nodes[key]);
            }
        }
        return res;
    }
}