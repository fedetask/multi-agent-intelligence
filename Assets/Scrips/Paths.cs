using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
/**
 * This class contains n different paths. Each path is defined by a list of
 * integers, corresponding to the index of points in the domnating set.
 */
public class Paths
{
    List<List<int>> paths = new List<List<int>>();
    public float fitness;

    public Paths() {}

    /**
     * Initialize npaths random paths
     */
    public Paths(List<int> points, int npaths) {
        InitRandom(points, npaths);
    }

    public Paths(List<List<int>> paths) {
        this.paths = paths;
    }

    /**
     * Initialize npaths empty paths
     */
    public Paths(int npaths) {
        for (int i = 0; i < npaths; i++) {
            paths.Add(new List<int>());
        }
    }


    public void AddPath(List<int> path) {
        paths.Add(path);
    }

    public List<int> GetPath(int path_index) {
        return paths[path_index];
    }

    public List<List<int>> GetPaths() {
        return paths;
    }

    public int TotalLength() {
        int tot = 0;
        foreach (List<int> path in paths) {
            tot += path.Count;
        }
        return tot;
    }

    public int Count() { return paths.Count; }
    
    /**
     * Creates the given number of random paths from the given set of points
     */
    private void InitRandom(List<int> points, int npaths) {
        System.Random random = new System.Random();
        int added = 0;

        // Initialize paths with npaths empty arrays
        paths = new List<List<int>>();
        for (int i = 0; i < npaths; i++) {
            paths.Add(new List<int>());
        }

        List<int> remaining_points = new List<int>(points);
        while(remaining_points.Count > 0) {
            int path_index = random.Next(paths.Count);
            int point_index = random.Next(remaining_points.Count);
            int point = remaining_points[point_index];
            paths[path_index].Add(point);
            remaining_points.RemoveAt(point_index);
            added++;
        }
    }

    /**
     * Fitness is defined as 1/max_length, where max_length is the length
     * of the longest path in paths, according to the distances matrix.
     */
    public void ComputeFitness(float[,] distances) {
        float[] costs = GetCosts(distances);
        this.fitness = 1.0f / costs.Max();
    }

    public float[] GetCosts(float[,] distances) {
        float[] costs = new float[paths.Count];
        for (int i = 0; i < paths.Count; i++) {
            List<int> path = paths[i];
            if (path.Count > 0) {
                float length = distances[0, path[0]]; // Distance from start (alwas index 0) to first point of path
                for (int j = 0; j < path.Count - 1 && path.Count > 1; j++) { // Compute length of path
                    length += distances[j, j + 1];
                }
                costs[i] = length;
            }
        }
        return costs;
    }

}