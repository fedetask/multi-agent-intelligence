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

    public void ComputeSubtreeCosts() {
        float turn180penalty = 10.0f;
        for (int agent = 0; agent < nagents; agent++) {
            int cont = 0;
            InitCosts(agent);
            HashSet<Node> to_visit = GetAllNodes(agent);
            Node cur = roots[agent];
            bool started = false;
            while (roots[agent].childs.Count > 0 && (!started || cur.parent != null)) {
                started = true;
                bool has_unvisited = false;
                foreach (Node child in cur.childs) {
                    if (to_visit.Contains(child)) {
                        cur = child;
                        has_unvisited = true;
                        to_visit.Remove(child);
                        break;
                    }
                }
                if (!has_unvisited) {
                    int cur_idx = cur.parent.childs.IndexOf(cur);
                    // If the node doesn't have children we have to turn back
                    if (cur.childs.Count == 0) {
                        cur.parent.path_costs[cur_idx] += 1 + turn180penalty;
                    } else {
                        cur.parent.path_costs[cur_idx] += 1 + cur.path_costs.Sum();   
                    }
                    cur = cur.parent;
                }
                cont++; 
            }
        }
    }

    public void InitCosts(int agent) {
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(roots[agent]);
        while (queue.Count > 0) {
            Node node = queue.Dequeue();
            node.path_costs = new List<float>();
            foreach (Node child in node.childs) {
                node.path_costs.Add(1);
                queue.Enqueue(child);
            }  
        }
    }

    public void ComputeCosts() {
        costs = new float[nagents];
        for (int i = 0; i < nagents; i++) {
            costs[i] = PathCost(i);
        }
    }
    public float PathCost(int agent) {
        return roots[agent].path_costs.Sum();
    }

    public List<Vector3> GetFullPath(TerrainInfo info, int agent) {
        List<Vector3> res = new List<Vector3>();
        HashSet<Node> to_visit = GetAllNodes(agent);
        Node cur = roots[agent];
        while (to_visit.Count > 1) {
            res.Add(Node2Vector3(cur, info));
            List<Node> childs_sorted = cur.childs.OrderBy(c => c.MaxCostPath()).ToList();
            int unvisited_childs = 0;
            for(int i = 0; i < childs_sorted.Count; i++) {
                Node child = childs_sorted[i];
                if (to_visit.Contains(child)) {
                    cur = child;
                    unvisited_childs++;
                    to_visit.Remove(child);
                    break;
                }
            }
            if (unvisited_childs == 0) { // Come back
                cur = cur.parent;
            }
        }
        return res;
    }

    private Vector3 Node2Vector3(Node node, TerrainInfo info) {
        float x = info.get_x_pos(node.cell[0]);
        float z = info.get_z_pos(node.cell[1]);
        return new Vector3(x, 0, z);
    }

    public GraphSpanningTrees Clone() {
        GraphSpanningTrees graph = new GraphSpanningTrees();
        graph.nagents = nagents;
        graph.roots = new Node[roots.Length];
        for (int i = 0; i < roots.Length; i++) {
            graph.roots[i] = roots[i].Clone();
        }
        graph.nodes = DictionaryFromGraph(graph.roots);
        graph.costs = costs == null ? null : costs.Clone() as float[];
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
                    Debug.DrawLine(node_coords, child_coords, colors[i], 1000f);
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