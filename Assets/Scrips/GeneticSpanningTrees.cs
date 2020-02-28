using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
public partial class GeneticSpanningTrees {

    int nagents;
    System.Random random;
    List<GraphSpanningTrees> population = new List<GraphSpanningTrees>();

    private int population_size = 60;
    private float initial_p_crossover = 0.5f;
    private float initial_p_mutation = 1f;
    private float p_mutation, p_crossover;
    private float selection_survivors = 0.3f;

    public GeneticSpanningTrees(GraphSpanningTrees graph) {
        this.nagents = graph.roots.Length;
        this.random = new System.Random();
        p_mutation = initial_p_mutation;
        p_crossover = initial_p_crossover;
        population.Add(graph);
        graph.ComputeMaxCosts();
        graph.ComputeCosts();
    }

    private List<GraphSpanningTrees> CreateOffspring (int noffspring) {
        List<GraphSpanningTrees> offsprings = new List<GraphSpanningTrees>();
        for (int i = 0; i < noffspring; i++) {
            int pop_idx = random.Next(population.Count);
            GraphSpanningTrees offspring = population[pop_idx].Clone();
            
            // Perform mutation
            int tree = SampleFromCost(offspring.costs, false);
            if (Bernoulli(p_mutation)) {
                PerformMutation(offspring, tree);
            }

            // Perform crossover
            tree = SampleFromCost(offspring.costs, true);
            if (Bernoulli(p_crossover)) {
                PerformCrossover(offspring, tree);
            }

            offsprings.Add(offspring);
        }
        return offsprings;
    }

    private List<GraphSpanningTrees> ApplySelection() {
        int survivors = Mathf.Max((int) (population_size * selection_survivors), 1);
        foreach (GraphSpanningTrees graph in population) {
            graph.ComputeMaxCosts();
            graph.ComputeCosts();
        }
        population = population.OrderBy(g => g.costs.Max()).ToList();
        return population.GetRange(0, survivors);
    }

    public void Optimize(int generations) {
        population.AddRange(CreateOffspring(population_size - 1));
        for (int i = 0; i < generations; i++) {
            population = ApplySelection();
            int offsprings = population_size - population.Count;
            population.AddRange(CreateOffspring(offsprings));
            p_mutation -= initial_p_mutation / generations;
            p_crossover -= initial_p_crossover / generations;
        }
    }

    public GraphSpanningTrees Best() {
        float best_cost = float.MaxValue;
        GraphSpanningTrees best = null;
        foreach (GraphSpanningTrees graph in population) {
            graph.ComputeMaxCosts();
            graph.ComputeCosts();
            if (graph.costs.Max() < best_cost) {
                best_cost = graph.costs.Max();
                best = graph;
            }
        }
        return best;
    }

    private List<Node[]> CrossoverCandidates(GraphSpanningTrees graph, int agent) {
        List<Node[]> candidates = new List<Node[]>();
        Stack<Node> stack = new Stack<Node>();
        stack.Push(graph.roots[agent]);
        while(stack.Count > 0) {
            Node node = stack.Pop();
            // Check neighbors of node and node itself
            int[] offsets = new int[] {0, 1, 0, -1, 0};
            for (int i = 0; i < offsets.Length - 1; i++) {
                int[] cell = new int[] {offsets[i] + node.cell[0], offsets[i+1] + node.cell[1]};
                for (int tree = 0; tree < nagents; tree++) {
                    // If this cell is occupied by a node and the node doesn't belong to this tree
                    if (tree != agent && graph.Contains(cell, tree)) {
                        // Then this node is a candidate
                        Node other = graph.GetNode(cell, tree);
                        if (other.parent != null) { // Roots are not candidate for mutations
                            candidates.Add(new Node[] {node, other});
                        }
                    }
                }
            }
            foreach (Node child in node.childs) {
                stack.Push(child);
            }
        }
        return candidates;
    }

    private void MoveRoot(GraphSpanningTrees graph, int agent) {
        Node[] nodes = graph.GetAllNodes(agent).ToArray();
        if (nodes.Length == 1) { return; }
        Node new_root;
        int new_root_idx;
        do {
            new_root_idx = random.Next(nodes.Length);
            new_root = nodes[new_root_idx];
        }while (new_root.parent == null); // Don't select current root
        Node cur = new_root;
        Node prev = null;
        int cont = 0;
        while(cur.parent != null) {
            cur.childs.Add(cur.parent);
            cur.parent.childs.Remove(cur);
            Node parent = cur.parent;
            cur.parent = prev;
            prev = cur;
            cur = parent;
            if (cont++ == graph.nodes.Count) {
                Debug.Log("INF LOOP");
                return;
            }
        }
        graph.roots[agent] = new_root;
    }

    private List<Node[]> MutationCandidates(GraphSpanningTrees graph, int agent) {
        List<Node[]> candidates = new List<Node[]>();
        Stack<Node> stack = new Stack<Node>();
        stack.Push(graph.roots[agent]);
        while(stack.Count > 0) {
            Node node = stack.Pop();
            // Check neighbors of node and node itself
            int[] offsets = new int[] {0, 1, 0, -1, 0};
            for (int i = 0; i < offsets.Length - 1; i++) {
                int[] cell = new int[] {offsets[i] + node.cell[0], offsets[i+1] + node.cell[1]};
                // If this cell is occupied by a node and the node belongs to this tree
                if (graph.Contains(cell, agent)) {
                    // Then this node is a candidate
                    Node other = graph.GetNode(cell, agent);
                    if (other.parent != null && !node.childs.Contains(other)) { // Roots are not candidate for mutations
                        candidates.Add(new Node[] {node, other});
                    }
                }
            }
            foreach (Node child in node.childs) {
                stack.Push(child);
            }
        }
        return candidates;
    }

    private void PerformMutation(GraphSpanningTrees graph, int tree) {
        List<Node[]> mutation_candidates = MutationCandidates(graph, tree);
        if (mutation_candidates.Count == 0) { return; }
        Node[] nodes = mutation_candidates[random.Next(mutation_candidates.Count)];

        bool is_ancestor = IsAncestor(nodes[0], nodes[1]);
        Node parent = is_ancestor ? nodes[0] : nodes[1];
        Node child = is_ancestor ? nodes[1] : nodes[0];

        parent.childs.Add(child);
        child.parent.childs.Remove(child);
        child.parent = parent;
    }

    private bool IsAncestor(Node ancestor, Node b) {
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(ancestor);
        while(queue.Count > 0) {
            Node node = queue.Dequeue();
            if (node.Equals(b)) {
                return true;
            }
            foreach (Node child in node.childs) {
                queue.Enqueue(child);
            }
        }
        return false;
    }

    private void PerformCrossover(GraphSpanningTrees graph, int tree) {
        List<Node[]> crossover_candidates = CrossoverCandidates(graph, tree);
        if (crossover_candidates.Count == 0) { return; }
        Node[] nodes = crossover_candidates[random.Next(crossover_candidates.Count)];
        Node parent = nodes[0];
        Node child = nodes[1];
        graph.RemoveCachedNode(child);
        parent.childs.Add(child);
        int child_idx = child.parent.childs.IndexOf(child);
        child.parent.childs.RemoveAt(child_idx);
        child.parent = parent;
        child.SetAgent(parent.agent);
        graph.AddCachedNode(child);
    }


    private bool Bernoulli(double p) {
        double rand = random.NextDouble();
        return rand < p;
    }

    private int SampleFromCost(float[] costs, bool ascending) {
        float[] distribution = costs.Clone() as float[];
        if (!ascending) {
            for (int i = 0; i < distribution.Length; i++) {
                distribution[i] = costs[costs.Length - 1 - i];
            }
        }
        // Normalize
        for(int i = 0; i < distribution.Length; i++) {
            distribution[i] /= distribution.Sum();
        }
        float rand = (float) random.NextDouble();
        float tot = 0;
        for (int i = 0; i < distribution.Length - 1; i++) {
            if (rand >= tot + distribution[i] && rand < tot + distribution[i+1]) {
                return i;
            }
            tot += distribution[i];
        }
        return distribution.Length - 1;
    }
}