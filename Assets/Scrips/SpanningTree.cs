﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SpanningTree : MonoBehaviour
{
    public GameObject terrain_manager_object;
    private TerrainManager terrain_manager;

    private float x_step;
    private float z_step;
    private Vector3 starting_pos;

    public float[,] traversability_matrix;

    private int number_of_agents;

    private GameObject[] agents;


    public int[][] roots;
    private Dictionary<string, List<int[]>> spanning_trees;

    public List<int[]>[] keys;
 
    // Start is called before the first frame update
    void Start()
    {   
        terrain_manager = terrain_manager_object.GetComponent<TerrainManager>();
        TerrainInfo myInfo = terrain_manager.myInfo;
        agents = GameObject.FindGameObjectsWithTag("Player");
        number_of_agents = agents.Length;
        traversability_matrix = myInfo.traversability.Clone() as float[,];
        int number_of_nodes = traversability_matrix.GetLength(0) * traversability_matrix.GetLength(1);
        spanning_trees = new Dictionary<string, List<int[]>>();
        roots = new int[number_of_agents][];
        
        int i = 0;
        keys = new List<int[]>[number_of_agents];
        for (i = 0; i < keys.Length; i++) { keys[i] = new List<int[]>(); }
        i = 0;
        foreach (GameObject car in agents)
        {
            Vector3 position = car.transform.position;
            roots[i] = new int[2];
            roots[i][0] = myInfo.get_i_index(position.x);
            roots[i][1] = myInfo.get_j_index(position.z);
            spanning_trees[array_to_string(roots[i], i)] = new List<int[]>();
            traversability_matrix[roots[i][0], roots[i][1]] = 1f;
            keys[i].Add(roots[i]);
            i++;
        }
        
        x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N;

        starting_pos = myInfo.start_pos;

        generate_Spanning_Trees();
        check_unicity();
        GraphSpanningTrees graph = new GraphSpanningTrees(spanning_trees, roots);
        GeneticSpanningTrees genetic = new GeneticSpanningTrees(graph);
        genetic.Optimize(100);
        GraphSpanningTrees best = genetic.Best();
        Debug.Log("BEST: "+best.costs.Max());
        best.Draw(myInfo);
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    int manhattan_Distance(int[] pos1,int[] pos2)
    {
        return Mathf.Abs(pos1[0] - pos2[0]) + Mathf.Abs(pos1[1] - pos2[1]);
    }

    void generate_Spanning_Trees()
    {
        int filled_blocks = 0;

        for(int i=0; i<traversability_matrix.GetLength(0); i++)
        {
            for (int j=0; j< traversability_matrix.GetLength(1); j++)
            {
                if (traversability_matrix[i,j]==1f)
                {
                    filled_blocks += 1;
                }
                else
                {
                    for (int a = 0; a < number_of_agents; a++) {
                        spanning_trees[array_to_string(new int[] { i, j }, a)] = new List<int[]>();
                    }
                }
            }
        }
        int[][] last_node = new int[number_of_agents][];
        for (int i = 0; i < last_node.Length; i++) {
            last_node[i] = new int[2] { roots[i][0], roots[i][1] };
        }
        
        bool[] part_1_done = new bool[number_of_agents];
        bool[] part_2_done = new bool[number_of_agents];

        while (filled_blocks < traversability_matrix.Length)
        {
            for(int i=0; i<number_of_agents; i++)
            {
                int new_nodes = expand_node(i, last_node);
                filled_blocks += new_nodes;
                part_1_done[i] = (new_nodes == 0);

                if(part_1_done[i] && part_2_done[i]==false) {
                    new_nodes = do_hilling(i);
                    filled_blocks += new_nodes;
                    // If no new nodes were produced, hilling is not possible anymore 
                    part_2_done[i] = (new_nodes == 0);
                }
                if(part_1_done[i] && part_2_done[i]) {
                    filled_blocks += expand_random(i);
                }
            }
        }
    }

    int expand_node(int agent_idx, int[][] last_node) {
        int[] current_position = last_node[agent_idx];
        List<int[]> neighbors = get_Neighbors(current_position);
        
        if(neighbors.Count>0) {
            int[] best_neighbor = find_best_neighbor(neighbors,last_node,agent_idx);
            spanning_trees[array_to_string(last_node[agent_idx], agent_idx)].Add(best_neighbor);
            last_node[agent_idx] = best_neighbor;
            keys[agent_idx].Add(best_neighbor);
            traversability_matrix[best_neighbor[0], best_neighbor[1]] = 1f;
            return 1;
        } else { 
            return 0;
        }
    }

    int[] filled_with_hilling = new int[3];
    int do_hilling(int agent_idx) {
        int[] node_hill = roots[agent_idx];
        int filled_blocks = 0;
        while(spanning_trees[array_to_string(node_hill, agent_idx)].Count>0) {
            int[] next_node = spanning_trees[array_to_string(node_hill, agent_idx)][0];
            bool horizontal = (node_hill[0] == next_node[0]);
            int[] check_coord = horizontal ? new int[] {1, 0} : new int[] {0, 1};
            int[] signs = new int[] {-1, 1};
            foreach (int sign in signs) {
                int[] a = new int[] { node_hill[0] + sign * check_coord[0], node_hill[1] + sign * check_coord[1] };
                int[] b = new int[] { next_node[0] + sign * check_coord[0], next_node[1] + sign * check_coord[1] };
                bool both_free = traversability_matrix[a[0], a[1]] == 0 && traversability_matrix[b[0], b[1]] == 0;
                if (both_free) {
                    bool removed = spanning_trees[array_to_string(node_hill, agent_idx)].Remove(next_node);
                    spanning_trees[array_to_string(node_hill,agent_idx)].Add(a);
                    spanning_trees[array_to_string(a, agent_idx)].Add(b);
                    spanning_trees[array_to_string(b, agent_idx)].Add(next_node);
                    filled_blocks += 2;
                    filled_with_hilling[agent_idx] += 2;
                    traversability_matrix[a[0], a[1]] = 1f;
                    traversability_matrix[b[0], b[1]] = 1f;
                    keys[agent_idx].Add(a);
                    keys[agent_idx].Add(b);
                    //Debug.DrawLine(point2vec(node_hill), point2vec(next_node), Color.red, 100f);
                    break;
                }
            }
            node_hill = next_node;
        }
        return filled_blocks;
    }

    Vector3 point2vec(int[] point) {
        float x = terrain_manager.myInfo.get_x_pos(point[0]);
        float z = terrain_manager.myInfo.get_z_pos(point[1]);
        Vector3 res = new Vector3(x, 0, z);
        return res;
    }

    int expand_random(int agent_idx) {
        int filled_blocks = 0;
        foreach(int[] node in keys[agent_idx]) {
            List<int[]> r_neighbors = get_Neighbors(node);
            if(r_neighbors.Count > 0) {
                int r_index = UnityEngine.Random.Range(0, r_neighbors.Count);
                int[] new_node = r_neighbors[r_index];
                keys[agent_idx].Add(new_node);
                spanning_trees[array_to_string(node, agent_idx)].Add(new_node);
                filled_blocks += 1;
                traversability_matrix[new_node[0],new_node[1]] = 1f;
                break;
            }
        }
        return filled_blocks;
    }

    int[] find_best_neighbor(List<int[]> neighbors, int[][] last_node, int current_robot)
    {
        int[][] distances = new int[neighbors.Count][];
        for(int i =0; i<neighbors.Count; i++) {
            distances[i] = new int[last_node.Length - 1];
            int distance_index = 0;
            for (int j=0; j<last_node.Length; j++)
            {
                if (j==current_robot) { continue; }
                distances[i][distance_index] = manhattan_Distance(neighbors[i], last_node[j]);
                distance_index += 1;
            }
        }

        int[] min_distances = new int[neighbors.Count];
        for(int i=0; i<min_distances.Length;i++)
        {
            min_distances[i] = Mathf.Min(distances[i]);
        }
        int max_value = -1;
        int index = 0;
        for (int i=0; i<min_distances.Length; i++)
        {
            if(min_distances[i]>max_value)
            {
                max_value = min_distances[i];
                index = i;
            }
        }

        return neighbors[index];
    }

    List<int[]> get_Neighbors(int[] current_position) {
        List<int[]> neighbors = new List<int[]>();
        int[] signs = new int[] {0, -1, 0, 1, 0};
        for (int i = 0; i < 4; i++) {
            int row = current_position[0] + signs[i];
            int col = current_position[1] + signs[i + 1];
            if (row >= 0 && row < traversability_matrix.GetLength(0)
                && col >= 0 && col < traversability_matrix.GetLength(1)) {
                    if (traversability_matrix[row, col] == 0f) {
                        neighbors.Add(new int[2]{row, col});
                    }
            }
        }
        return neighbors;
    }

    string array_to_string(int[] array, int agent_idx) {
        return array[0] + "," + array[1]+":"+agent_idx;
    }
    string array_to_string(int[] array) {
        return array[0] + "," + array[1];
    }

    void draw_debug() {
        TerrainInfo myInfo = terrain_manager.myInfo;
        Color[] colors = new Color[] { Color.white, Color.yellow, Color.blue };
        for (int agent = 0; agent < 3; agent++) {
            List<int[]> points = keys[agent];
            foreach(int[] point in points) {
                List<int[]> children = spanning_trees[array_to_string(point, agent)];
                foreach (int[] child in children) {
                    Vector3 from = new Vector3(myInfo.get_x_pos(point[0]), 0, myInfo.get_z_pos(point[1]));
                    Vector3 to = new Vector3(myInfo.get_x_pos(child[0]), 0, myInfo.get_z_pos(child[1]));
                    Debug.DrawLine(from, to, colors[agent], 100f);
                }
            }
        }
    }

    bool check_unicity() {
        Dictionary<string, bool> occupied = new Dictionary<string, bool>();
        for (int i = 0; i < number_of_agents; i++) {
            List<int[]> points = keys[i];
            foreach (int[] point in points) {
                if (occupied.ContainsKey(array_to_string(point, i))) {
                    Debug.LogError("ERROR: point "+point[0]+","+point[1]+" belongs to more than one path");
                    return false;
                }
            }
        }
        return true;
    }
}
