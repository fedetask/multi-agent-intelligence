using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            spanning_trees[array_to_string(roots[i])] = new List<int[]>();
            traversability_matrix[roots[i][0], roots[i][1]] = 1f;
            keys[i].Add(roots[i]);
            //Debug.Log("(" + roots[i][0]+", " + roots[i][1] +")");
            i++;
        }
        //Debug.Log("root number" + roots.Length);
        
        x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N;

        starting_pos = myInfo.start_pos;

        generate_Spanning_Trees();
        Color[] colors = new Color[] { Color.white, Color.yellow, Color.blue };
        /*
        for (i=0; i<3;i++)
        {
            foreach(int[] node in keys[i])
            {
                List<int[]> node_children = spanning_trees[array_to_string(node)];
                Vector3 parent_position = new Vector3(myInfo.get_x_pos(node[0]), 0f, myInfo.get_z_pos(node[1]));
                foreach(int[] child in node_children)
                {
                    Vector3 child_position = new Vector3(myInfo.get_x_pos(child[0]), 0f, myInfo.get_z_pos(child[1]));
                    Debug.DrawLine(parent_position, child_position, colors[i], 100f);
                }
            }
        }
        */

        List<List<Vector3>> paths = new List<List<Vector3>>();
        for(i =0; i<3; i++)
        {
            paths.Add(dfs(roots[i]));
            Debug.Log("Path size " + i + " is " + paths[i].Count);
            for(int j=0; j<paths[i].Count; j++)
            {
                Debug.DrawLine(paths[i][j], paths[i][j], Color.yellow, 100f);
            }
        }

        
        
        

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    int manhattan_Distance(int[] pos1,int[] pos2)
    {
        return Mathf.Abs(pos1[0] - pos2[0]) + Mathf.Abs(pos1[1] - pos2[1]);
    }


    string array_to_string(int[] array)
    {
        return array[0] + "," + array[1];
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
                    spanning_trees[array_to_string(new int[] { i, j })] = new List<int[]>();
                }
            }
        }
        int[][] last_node = new int[number_of_agents][];
        for (int i = 0; i < last_node.Length; i++) {
            last_node[i] = new int[2] { roots[i][0], roots[i][1] };
            Debug.Log("(" + last_node[i][0] + ", " + last_node[i][1] + ")");
        }
        
        bool[] part_1_done = new bool[number_of_agents];
        bool[] part_2_done = new bool[number_of_agents];



        while (filled_blocks < traversability_matrix.Length)
        {
            for(int i=0; i<number_of_agents; i++)
            {
                int[] current_position = last_node[i];

                List<int[]> neighbors = get_Neighbors(current_position);
                
                if(neighbors.Count>0) // Expansion
                {
                    int[] best_neighbor = find_best_neighbor(neighbors,last_node,i);
                    
                    spanning_trees[array_to_string(last_node[i])].Add(best_neighbor);
                    last_node[i] = best_neighbor;
                    keys[i].Add(best_neighbor);
                    Vector3 parent_position = new Vector3(terrain_manager.myInfo.get_x_pos(current_position[0]), 0f, terrain_manager.myInfo.get_z_pos(current_position[1]));
                    Vector3 child_position = new Vector3(terrain_manager.myInfo.get_x_pos(best_neighbor[0]), 0f, terrain_manager.myInfo.get_z_pos(best_neighbor[1]));
                    //Debug.DrawLine(parent_position, child_position, Color.red,100f);

                    traversability_matrix[best_neighbor[0], best_neighbor[1]] = 1f;
                    filled_blocks += 1;
                }
                else { part_1_done[i] = true; }

                if(part_1_done[i] && part_2_done[i]==false)
                {
                    int[] node_hill = roots[i];
                    part_2_done[i] = true;
                    while(spanning_trees[array_to_string(node_hill)].Count>0) //hilling
                    {
                        int[] next_node = spanning_trees[array_to_string(node_hill)][0];
                        bool are_horrizontal = (node_hill[0] == next_node[0]);
                        int[] check_coord;
                        if(!are_horrizontal)
                        {
                            check_coord = new int[2] { 0, 1 };
                        }
                        else
                        {
                            check_coord = new int[2] { 1, 0 };
                        }

                        int[] a_plus = new int[] { node_hill[0] + check_coord[0], node_hill[1] + check_coord[1] };
                        int[] b_plus = new int[] { check_coord[0] + next_node[0], check_coord[1] + next_node[1] };
                        int[] a_minus = new int[] { node_hill[0] - check_coord[0], node_hill[1] - check_coord[1] };
                        int[] b_minus = new int[] { next_node[0]- check_coord[0], next_node[1] - check_coord[1]};

                        bool plus = (traversability_matrix[a_plus[0],a_plus[1]] + traversability_matrix[b_plus[0],b_plus[1]]) < 1f;
                        bool minus = (traversability_matrix[a_minus[0],a_minus[1]] + traversability_matrix[b_minus[0],b_minus[1]]) < 1f;
                        
                        if (plus)
                        {
                            spanning_trees[array_to_string(node_hill)].Remove(next_node);
                            
                            spanning_trees[array_to_string(node_hill)].Add(a_plus);
                            spanning_trees[array_to_string(a_plus)].Add(b_plus);
                            spanning_trees[array_to_string(b_plus)].Add(next_node);
                            part_2_done[i] = false;
                            filled_blocks += 2;
                            traversability_matrix[a_plus[0], a_plus[1]] = 1f;
                            traversability_matrix[b_plus[0], b_plus[1]] = 1f;
                            keys[i].Add(a_plus);
                            keys[i].Add(b_plus);
                            break;
                        }
                        if (minus)
                        {
                            spanning_trees[array_to_string(node_hill)].Remove(next_node);
                            spanning_trees[array_to_string(node_hill)].Add(a_minus);
                            spanning_trees[array_to_string(a_minus)].Add(b_minus);
                            spanning_trees[array_to_string(b_minus)].Add(next_node);
                            part_2_done[i] = false;
                            filled_blocks += 2;
                            traversability_matrix[a_minus[0], a_minus[1]] = 1f;
                            traversability_matrix[b_minus[0], b_minus[1]] = 1f;
                            keys[i].Add(a_minus);
                            keys[i].Add(b_minus);
                            break;
                        }
                        node_hill = next_node;
                    }
                }

                if(part_1_done[i] && part_2_done[i])
                {
                    foreach(int[] node in keys[i])
                    {
                        List<int[]> r_neighbors = get_Neighbors(node);
                        if(r_neighbors.Count>0)
                        {
                            int r_index = UnityEngine.Random.Range(0, r_neighbors.Count);
                            int[] new_node = r_neighbors[r_index];
                            keys[i].Add(new_node);
                            spanning_trees[array_to_string(node)].Add(new_node);
                            filled_blocks += 1;
                            traversability_matrix[new_node[0],new_node[1]] = 1f;
                            break;
                        }
                    }
                }

            }
        }
    }

    int[] find_best_neighbor(List<int[]> neighbors, int[][] last_node, int current_robot)
    {
        int[][] distances = new int[neighbors.Count][];

        
        for(int i =0; i<neighbors.Count; i++)
        {
            distances[i] = new int[last_node.Length - 1];
            int distance_index = 0;
            for (int j=0; j<last_node.Length; j++)
            {
                if (j==current_robot)
                {
                    continue; 
                }
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

    Vector3 coord_to_vec(int[] coord)
    {
       return new Vector3(terrain_manager.myInfo.get_x_pos(coord[0]), 0f, terrain_manager.myInfo.get_z_pos(coord[1]));
    }

    List<Vector3> dfs(int[] root)
    {
        List<Vector3> result = new List<Vector3>();
        result.Add(coord_to_vec(root));
        List<string> stack = new List<string>();
        stack.Add(array_to_string(root));

        while(stack.Count>0)
        {
            string node = stack[0];
            stack.RemoveAt(0);
            List<int[]> children = spanning_trees[node];
            foreach(int[] child in children)
            {
                stack.Insert(0,array_to_string(child));
            }
            if(children.Count>0)
            { result.Add(coord_to_vec(children[0])); }
            
        }
        return result;
    }
}
