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
    int population_size = 100000;
    float selection_size = 0.3f;
    float start_mutation_prob = 0.1f;
    float crossover_size_min;
    float crossover_size_max;
    float[,] min_distances;
    System.Random random;
    public GeneticTSP(List<Vector3> visibility_points, List<Vector3> dominating_points, float[,] min_distances, Vector3 start_pos, int nagents) {
        this.dominating_points = dominating_points;
        this.path_points = new List<int>(dominating_points.Count);
        for (int i = 0; i < dominating_points.Count; i++) {
            path_points.Add(visibility_points.IndexOf(dominating_points[i]));
        }
        this.nagents = nagents;
        this.min_distances = min_distances;
        random = new System.Random();
        this.crossover_size_min = dominating_points.Count / (2 * nagents * 100);
        this.crossover_size_max = dominating_points.Count * 2 / (nagents * 100);

        InitGenetic();
    }

    private void InitGenetic() {
        population = new List<Paths>(population_size);
        // Initialize random paths
        for(int i = 0; i < population_size; i++) { 
            population.Add(new Paths(path_points, nagents));
        }
    }

    public Paths Optimize(int generations = 50) {
        InitGenetic();
        for (int i = 0 ; i < generations; i++) {
            ApplySelection();
            GenerateOffspring();
            // Mutate
        }
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
        foreach (Paths path in population) {
            path.ComputeFitness(min_distances);
        }
        population.Sort(new Comparison<Paths>((o1, o2) => (int)(o1.fitness - o2.fitness)));
        // Removing the selection_size proportion from the tail of the population
        for (int i = population.Count - 1; i >= (int) (selection_size * population_size); i--) {
            population.RemoveAt(population.Count - 1);
        }
    }

    private void GenerateOffspring() {
        int offspring_size = (int) selection_size * population_size;
        float[] fitness_scores = FitnessScores(true);
        for (int i = 0; i < offspring_size; i++) {
            // Chose two parents with probability proportional to their fitness
            int parent1 = RandomChoice(fitness_scores);
            int parent2;
            do {
                parent2 = RandomChoice(fitness_scores);
            } while (parent1 == parent2);
            Paths child = Crossover(population[parent1], population[parent2]);
            population.Add(child);
        }
    }

    private float[] FitnessScores(bool normalize = false) {
        float[] fitness_scores = new float[population.Count];
        for (int i = 0; i < population.Count; i++) {
            fitness_scores[i] = population[i].fitness;
            if (normalize) {
                fitness_scores[i] /= population.Count;
            }
        }
        return fitness_scores;
    }

    private Paths Crossover(Paths parent1, Paths parent2) {
        float crossover_size = RandomBetween(crossover_size_min, crossover_size_max);
        int npoints = (int) (crossover_size * parent1.TotalLength());
        List<int> section = Section(parent1, npoints);
        Paths offspring = Merge(section, parent2);
        return offspring;
    }

    /**
     * Return a random "slice" of a Paths object. 
     */
    private List<int> Section(Paths paths, int npoints) {
        List<int> section = new List<int>();
        int start_max = paths.TotalLength() - npoints + 1;
        int start = random.Next(start_max);
        int tot = 0;
        int count = 0;
        foreach (List<int> path in paths.GetPaths()) {
            tot += path.Count;
            if (tot < start) { continue; }
            // TODO: join together paths of different agents? 
            for (int point_idx = 0; point_idx < path.Count(); point_idx++) {
                section.Add(path[point_idx]);
                if (++count == npoints) { return section; }
            }
        }
        return section;
    }

    /**
     * Returns a Paths object resulting from merging the given section (from parent 1)
     * with parent 2.9
     */
    private Paths Merge(List<int> section, Paths parent) {
        List<List<int>> path_sections = new List<List<int>>();
        path_sections.Add(section);
        // Splitting paths of parent in points where they intersect section,
        foreach (List<int> path in parent.GetPaths()) {
            // Check if path intersects with the given section and
            // in that case break it into multiple sections
            List<int> sec = new List<int>();
            for (int i = 0; i < path.Count; i++) {
                if (! section.Contains(path[i])) {
                    sec.Add(path[i]);
                } else if (sec.Count > 0){
                    // Jump point, add section to array
                    path_sections.Add(sec);
                    sec = new List<int>();
                }
            }
            // Add last section
            if (sec.Count > 0) { path_sections.Add(sec); }
        }

        Paths result_paths = new Paths(nagents);
        float[] costs = new float[result_paths.Count()];

        while (path_sections.Count > 0) {
            // Iterate trough all the remaining sections and assign
            // them greedily to the paths by minimizing cost
            float min_cost = float.MaxValue;
            int min_cost_path = -1;
            int min_cost_sec = -1;
            for (int sec_idx = 0; sec_idx < path_sections.Count; sec_idx++) {
                List<int> sec = path_sections[sec_idx];
                float sec_cost = Cost(sec);
                // Find assignment of sec to path that has smallest cost
                for (int i = 0; i < result_paths.GetPaths().Count; i++) {
                    List<int> path = result_paths.GetPath(i);
                    // Try assign sec to this path
                    float tot_cost;
                    if (path.Count > 0) { 
                        tot_cost = costs[i] + min_distances[path.Count - 1, sec[0]] + sec_cost;
                    } else {
                        tot_cost = sec_cost;
                    }
                    if (tot_cost < min_cost) {
                        min_cost = tot_cost;
                        min_cost_path = i;
                        min_cost_sec = sec_idx;
                    }
                }
            }

            result_paths.GetPath(min_cost_path).AddRange(path_sections[min_cost_sec]);
            costs[min_cost_path] = min_cost;
            path_sections.RemoveAt(min_cost_sec);
        }
        return result_paths;
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
        double tot = 0;
        for (int i = 0; i < distribution.Length; i++) {
            if (p >= tot && p < tot + distribution[i]) {
                return i;
            }
            tot += distribution[i];
        }
        return -1;
    }

    private float RandomBetween(float min, float max) {
        return min + (float)random.NextDouble() * (max - min);
    }
}
