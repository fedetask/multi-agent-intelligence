using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
public class GeneticTSP
{
    List<Vector3> dominating_points;
    List<int> path_points; // Contains dominating points indexes w.r.t. the visibility graph
    int nagents;
    List<Paths> population;
    int population_size = 50;
    float selection_size = 0.3f;
    float start_mutation_prob = 0.3f;
    float halflife_mutation_prob = 0.01f; // Desired probability of mutation at half of the iterations
    float[,] min_distances;
    System.Random random;
    float mutation_prob;
    public GeneticTSP(List<Vector3> visibility_points, List<Vector3> dominating_points, float[,] min_distances, Vector3 start_pos, int nagents) {
        this.dominating_points = dominating_points;
        this.path_points = new List<int>(dominating_points.Count);
        for (int i = 0; i < dominating_points.Count; i++) {
            path_points.Add(visibility_points.IndexOf(dominating_points[i]));
        }
        this.nagents = nagents;
        this.min_distances = min_distances;
        this.mutation_prob = start_mutation_prob;
        random = new System.Random();
    }

    private void InitGenetic() {
        population = new List<Paths>(population_size);
        // Initialize random paths
        for(int i = 0; i < population_size; i++) { 
            population.Add(new Paths(path_points, nagents, random));
        }
    }

    public Paths Optimize(int generations = 50) {
        float prob_reduction_coeff = Mathf.Pow((halflife_mutation_prob / start_mutation_prob), (2 / (float) generations));
        InitGenetic();
        for (int i = 0 ; i < generations; i++) {
            ComputeFitnesses();
            ApplySelection();
            GenerateOffspring();
            Mutate();
            Migrate();
            mutation_prob *= prob_reduction_coeff;
        }
        ComputeFitnesses();
        return GetBest();
    }

    public Paths GetBest() {
        float max_fit = float.MinValue;
        Paths best = null;
        foreach (Paths path in population) {
            path.ComputeFitness(min_distances);
            if (path.fitness > max_fit) {
                max_fit = path.fitness;
                best = path;
            }
        }
        return best;
    }

    private void ApplySelection() {
        population = population.OrderBy(i => i.fitness).ToList();
        // Removing (1 - selection_size) percent of worst solutions
       while (population.Count > (int) (selection_size * population_size)) {
           population.RemoveAt(0);
       }
    }

    private void ComputeFitnesses() {
        foreach (Paths path in population) {
            path.ComputeFitness(min_distances);
        }
    }

    private void GenerateOffspring() {
        float[] fitness_scores = FitnessScores(true);
        while(population.Count < population_size) {
            // Chose two parents with probability proportional to their fitness
            int parent_idx = RandomChoice(fitness_scores);
            if (parent_idx < 0 || parent_idx > population.Count -1) {
                Debug.Log("ERROR  "+parent_idx);
            }
            Paths child = Crossover(population[parent_idx]);
            population.Add(child);
        }
    }

    private float[] FitnessScores(bool normalize = false) {
        float[] fitness_scores = new float[population.Count];
        float tot_fitness = 0;
        for (int i = 0; i < population.Count; i++) {
            fitness_scores[i] = population[i].fitness;
            tot_fitness += fitness_scores[i];
        }
        if (normalize) {
            for (int i = 0; i < fitness_scores.Length; i++) {
                fitness_scores[i] /= tot_fitness;
            }
        }
        return fitness_scores;
    }

    private Paths Crossover(Paths parent) {
        // Select randomly two subtours of parent
        int subtour1_idx = random.Next(nagents);
        int subtour2_idx;
        do {
            subtour2_idx = random.Next(nagents);
        } while (subtour2_idx == subtour1_idx);

        // Get a copy of the two selected subtours from the parent
        List<int> subtour1 = new List<int>(parent.GetPath(subtour1_idx));
        List<int> subtour2 = new List<int>(parent.GetPath(subtour2_idx));

        // Select a random split point. All items after the
        // split point (included) will be swapped  
        int split1 = random.Next(subtour1.Count);
        int split2 = random.Next(subtour2.Count);

        List<int> slice1 = new List<int>();
        List<int> slice2 = new List<int>();

        // Remove everything from the split points onwards and
        // add to the two slices 
        while (subtour1.Count > split1) {
            slice1.Insert(0, subtour1.Last());
            subtour1.RemoveAt(subtour1.Count - 1);
        }
        while (subtour2.Count > split2) {
            slice2.Insert(0, subtour2.Last());
            subtour2.RemoveAt(subtour2.Count - 1);
        }
        
        // Swapping
        subtour1.AddRange(slice2);
        subtour2.AddRange(slice1);

        List<List<int>> paths = new List<List<int>>();
        paths.Add(subtour1);
        paths.Add(subtour2);
        for (int i = 0; i < parent.Count(); i++) {
            if (i != subtour1_idx && i != subtour2_idx) {
                paths.Add(new List<int>(parent.GetPath(i)));
            }
        }
        
        Paths child = new Paths(paths);
        return child;
    }

    private void Mutate() {
        foreach (Paths paths in population) {
            bool mutate = random.NextDouble() <= mutation_prob;
            if (mutate) {
                DoRandomMutation(paths);
            }
        }
    }

    private void DoRandomMutation(Paths paths) {
        int subtour1_idx = random.Next(paths.Count());
        int subtour2_idx;
        do {
            subtour2_idx = random.Next(paths.Count());
        } while (subtour2_idx == subtour1_idx);

        List<int> path1 = paths.GetPath(subtour1_idx);
        List<int> path2 = paths.GetPath(subtour2_idx);

        if (path1.Count == 0 || path2.Count == 0) { return; }

        int node1_idx = random.Next(path1.Count);
        int node2_idx = random.Next(path2.Count);

        int node1 = path1[node1_idx];
        int node2 = path2[node2_idx];

        path1.RemoveAt(node1_idx);
        path2.RemoveAt(node2_idx);
        path1.Insert(node1_idx, node2);
        path2.Insert(node2_idx, node1);
    }

    private void Migrate() {
        foreach (Paths paths in population) {
            bool mutate = random.NextDouble() <= mutation_prob;
            if (mutate) {
                DoRandomMigration(paths);
            }
        }
    }


    private void DoRandomMigration(Paths paths) {
        int subtour1_idx = random.Next(paths.Count());
        int subtour2_idx;
        do {
            subtour2_idx = random.Next(paths.Count());
        } while (subtour2_idx == subtour1_idx);

        List<int> path1 = paths.GetPath(subtour1_idx);
        List<int> path2 = paths.GetPath(subtour2_idx);

        if (path1.Count == 0 || path2.Count == 0) { return; }

        int node1_idx = random.Next(path1.Count);
        int node2_idx = random.Next(path2.Count);
        
        int node1 = path1[node1_idx];
        path1.RemoveAt(node1_idx);
        path2.Insert(node2_idx, node1);
    }
    private float Cost(List<int> path) {
        if (path.Count < 2) { return 0; }
        float cost = 0;
        for (int i = 0; i < path.Count - 1; i++) { // Compute length of path
            cost += min_distances[i, i + 1];
        }
        return cost;
    }

    /**
     * Returns a random integer from the given distribution
     */
    private int RandomChoice(float[] distribution) {
        double p = random.NextDouble();
        float tot = 0;
        for (int i = 0; i < distribution.Length; i++) {
            if (p >= tot && p < tot + distribution[i]) {
                return i;
            }
            tot += distribution[i];
        }
        return distribution.Length - 1;
    }

    private float RandomBetween(float min, float max) {
        return min + (float)random.NextDouble() * (max - min);
    }
}
