using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
namespace UnityStandardAssets.Vehicles.Car
{
    //[RequireComponent(typeof(CarAI2))]
    public class VisibilityGraph : MonoBehaviour
    {

        private float margin = 4f;
        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject game_manager_object;
        GameManager gm;

        // Floyd - Warshal variables
        public Vector3[,] next_node;
        public float[,] min_distances;
        int counter = 0;
        int path_counter = 0;
        // Use this for initialization

        public List<Vector3> visibility_corners;
        public float[,] adjacency_matrix;

        public List<Vector3> tsp_path;
        public List<Vector3> verbose_tsp_path;


        public int[] seen_thus_far { get; private set; }

        public List<Vector3> dominatingSet = new List<Vector3>();
        public Vector3 start_pos;

        public GeneticTSP geneticTSP;




        void Start()
        {
            string scene_name = SceneManager.GetActiveScene().name;

            //int scene_number = Int32.Parse(scene_name.Substring(scene_name.Length - 1));
            int scene_number = 2;
            gm = game_manager_object.GetComponent<GameManager>();

            Debug.Log("scene number " + scene_number);
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            start_pos = terrain_manager.myInfo.start_pos + new Vector3(-2*margin, 0f, -2*margin);
            visibility_corners = new List<Vector3>();
            visibility_corners.Add(start_pos);
            visibility_corners.AddRange(GetCorners());

            if(scene_number==2)
            {
                CorrectionCorners(visibility_corners);
            }
            else
            {
                dominatingSet = get_Turret_Locations(gm);
                visibility_corners.AddRange(dominatingSet);
            }

            
            Debug.Log("starting visibility");
            adjacency_matrix = GetAdjacencyMatrix(visibility_corners);
            seen_thus_far = new int[visibility_corners.Count];
            if(scene_number==2)
            {
                dominatingSet = GreedyDominatingSet(visibility_corners, adjacency_matrix);
            }
           

            foreach (Vector3 v in dominatingSet)
            {
                terrain_manager.DrawCircle(v, 5, 2);
            }

            next_node = floyd_warshal(adjacency_matrix);

            nearest_neighbour_tsp(start_pos);

            geneticTSP = new GeneticTSP(visibility_corners, dominatingSet, min_distances, start_pos, 3);
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            geneticTSP.Optimize(4000);
            stopwatch.Stop();
            Debug.Log("Genetic computation: " + stopwatch.Elapsed.Seconds + "s " + stopwatch.Elapsed.Milliseconds);
            Paths best = geneticTSP.GetBest();
            Debug.Log("Best solution cost : " + best.max_cost);
            DrawMultiAgentPaths(best);

            GameObject[] friends = GameObject.FindGameObjectsWithTag("Player");
            int counter = 0;
            Debug.Log("Friend size " + friends.Length);
            foreach (GameObject obj in friends)
            {
                if(scene_number==2)
                {
                    CarAI2 script = obj.GetComponent<CarAI2>();
                    Debug.Log("Current object " + obj.name);
                    script.index_of_current_player = counter;

                    counter += 1;
                }
                else
                {
                    CarAI3 script = obj.GetComponent<CarAI3>();
                    Debug.Log("Current object " + obj.name);
                    script.index_of_current_player = counter;

                    counter += 1;
                }
                
                
            }
        }


        public float get_margin()
        { return margin; }

        void DrawMultiAgentPaths(Paths paths)
        {
            Color[] colors = new Color[] { Color.yellow, Color.green, Color.blue, Color.white, Color.black };
            for (int path_idx = 0; path_idx < paths.Count(); path_idx++)
            {
                List<int> path = paths.GetPath(path_idx);
                if (path.Count == 0) { continue; }
                Debug.DrawLine(start_pos, visibility_corners[path[0]], colors[path_idx], 100f);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Debug.DrawLine(visibility_corners[path[i]], visibility_corners[path[i + 1]], colors[path_idx], 100f);
                }
            }
        }

        public float[,] get_matrix()
        {
            return adjacency_matrix;
        }

        public List<Vector3> GetCorners()
        {
            TerrainInfo myInfo = terrain_manager.myInfo;
            float[,] pad_traversability = GetPaddedTraversability(myInfo.traversability);
            float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
            float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N;
            float y = myInfo.start_pos.y;
            int rows = pad_traversability.GetLength(0);
            int cols = pad_traversability.GetLength(1);
            List<Vector3> valid_corners = new List<Vector3>();

            for (int r = 1; r < rows - 1; r++)
            {
                for (int c = 1; c < cols - 1; c++)
                {
                    if (pad_traversability[r, c] == 0.0f) { continue; } // Empty cells don't have corners
                                                                        // A corner of a cell in a given direction is valid iff
                                                                        // - The cell that contains it and the two adjacent cells that also touch the generating cell are free
                                                                        // - The cell that contains it is free and the two adjacent cells that also touch the generating cell are full
                    int[] corner_steps = new int[] { -1, -1, +1, +1, -1 };
                    int[] adjacent_steps = new int[] { 0, -1, 0, +1, 0, -1 };
                    for (int i = 0; i < 4; i++)
                    {
                        if (pad_traversability[r + corner_steps[i], c + corner_steps[i + 1]] == 1.0f) { continue; } // Corner is in a full cell
                                                                                                                    // The two adjacent cells that also touch the generating cell are
                        Cell c1 = new Cell(r + adjacent_steps[i], c + adjacent_steps[i + 1]);
                        Cell c2 = new Cell(r + adjacent_steps[i + 1], c + adjacent_steps[i + 2]);
                        if (c1.row == 0 || c1.row == rows - 1 || c1.col == 0 || c1.col == cols - 1
                            || c1.row == 0 || c1.row == rows - 1 || c1.col == 0 || c1.col == cols - 1)
                        {
                            continue;
                        }
                        if ((pad_traversability[c1.row, c1.col] == 0.0f && pad_traversability[c2.row, c2.col] == 0.0f)
                            || (pad_traversability[c1.row, c1.col] == 1.0f && pad_traversability[c2.row, c2.col] == 1.0f))
                        { // Both free or both full
                            Vector3 center = new Vector3(myInfo.x_low + (r - 1 + 0.5f) * x_step, y, myInfo.z_low + (c - 1 + 0.5f) * z_step);
                            float x = center.x + corner_steps[i] * (x_step / 2 + margin / Mathf.Sqrt(2));
                            float z = center.z + corner_steps[i + 1] * (z_step / 2 + margin / Mathf.Sqrt(2));
                            Vector3 corner = new Vector3(x, y, z);
                            valid_corners.Add(corner);
                        }
                    }
                }
            }
            Debug.Log("Finished creating corners");
            Debug.Log("We have this many corners " + valid_corners.Count);

            return valid_corners;
        }

        private List<Vector3> get_Turret_Locations(GameManager gm)
        {
            List<GameObject> turret_list = gm.turret_list;

            List<Vector3> path = new List<Vector3>();

            foreach(GameObject turret in turret_list)
            {
                path.Add(push_from_corner(turret.transform.position));
            }
            Debug.Log("path size " + path.Count);
            return path;
        }


        private Vector3 push_from_corner(Vector3 initial_position)
        {
            Collider[] hitColliders = Physics.OverlapSphere(initial_position, 2*margin);
            if (hitColliders.Length>0)
            {
                Debug.Log("Turret Too close!");
                Collider nearby_wall = hitColliders[0]; //it can only be close enought to one wall
                Vector3 correction_direction =initial_position - nearby_wall.transform.position;
                correction_direction = correction_direction.normalized;
                return initial_position + margin * correction_direction;
            }
            else
            { return initial_position; }
        }
         
        public List<Vector3> GetPathPoints(Vector3 source, Vector3 destination, List<Vector3> visibility_corners, float[,] adjacency_matrix)
        {
            // float time = Time.time;
            List<Vector3> path_points = new List<Vector3>();
            List<Vector3> corners = new List<Vector3>();
            //corners.Add(source);
            //corners.Add(destination);
            corners.AddRange(visibility_corners);

            float[,] adjacencies = GetAdjacencyMatrix(corners);
            List<int> path_indexes = Dijkstra.get_shortest_path(adjacencies);
            foreach (int path_index in path_indexes)
            {
                path_points.Add(corners[path_index]);
            }
            return path_points;
        }

        // Update is called once per frame
        void Update()
        {
        }

        float[,] GetAdjacencyMatrix(List<Vector3> corners)
        {
            float[,] adjancenies = new float[corners.Count, corners.Count];
            var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
            for (int i = 0; i < corners.Count; i++)
            {
                for (int j = i + 1; j < corners.Count; j++)
                {
                    Vector3 direction = corners[j] - corners[i];
                    Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                    float step = (margin - 0.1f) / 2;
                    int[] signs = new int[] { -1, 0, 1 };
                    bool free = true;
                    foreach (int sign in signs)
                    {
                        if (Physics.Linecast(corners[i] + sign * step * normal, corners[j] + sign * step * normal, mask))
                        {
                            adjancenies[i, j] = float.MaxValue / 2; // -1
                            adjancenies[j, i] = float.MaxValue / 2;
                            free = false;
                        }
                    }
                    if (free)
                    {
                        float dist = Vector3.Distance(corners[i], corners[j]);
                        //Debug.Log("I am in here");
                        //Debug.DrawLine(corners[i], corners[j], Color.cyan, 100f);
                        //Debug.DrawLine(corners[i], corners[j], Color.yellow, 100f);
                        adjancenies[i, j] = dist;
                        adjancenies[j, i] = dist;
                    }
                }
            }
            print(adjancenies[5, 5]);
            return adjancenies;
        }

        private void CorrectionCorners(List<Vector3> corners)
        {
            List<Vector3> new_Corners = new List<Vector3>();
            float[,] adjancenies = new float[corners.Count, corners.Count];
            var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
            for (int i = 0; i < corners.Count; i++)
            {
                for (int j = i + 1; j < corners.Count; j++)
                {
                    Vector3 direction = corners[j] - corners[i];
                    Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                    float step = (margin - 0.1f) / 2;
                    int[] signs = new int[] { -1, 0, 1 };
                    bool free = true;
                    foreach (int sign in signs)
                    {
                        if (Physics.Linecast(corners[i] + sign * step * normal, corners[j] + sign * step * normal, mask))
                        {
                            free = false;
                        }
                    }
                    if (free)
                    {
                        if (corners[i].x == corners[j].x || corners[i].z == corners[j].z)
                        {

                            Vector3 middlePoint = (corners[i] + corners[j]) / 2;
                            if (!new_Corners.Contains(middlePoint) && !corners.Contains(middlePoint))
                            {
                                new_Corners.Add(middlePoint);
                            }
                        }

                        //Debug.Log("I am in here");
                        //Debug.DrawLine(corners[i], corners[j], Color.yellow, 100f);
                    }
                }
            }
            corners.AddRange(new_Corners);
        }

        private float[,] GetPaddedTraversability(float[,] traversability)
        {
            float[,] res = new float[traversability.GetLength(0) + 2, traversability.GetLength(1) + 2];
            for (int r = 0; r < traversability.GetLength(0); r++)
            {
                for (int c = 0; c < traversability.GetLength(1); c++)
                {
                    res[r + 1, c + 1] = traversability[r, c];
                }
            }
            return res;
        }

        private List<Vector3> GreedyDominatingSet(List<Vector3> visibility_graph, float[,] adjacency_matrix)
        {
            List<Vector3> dominatingSet = new List<Vector3>();
            Dictionary<Vector3, float[]> isDominated = new Dictionary<Vector3, float[]>();
            foreach (Vector3 corner in visibility_graph)
            {
                isDominated.Add(corner, new float[2] { visibility_graph.IndexOf(corner), 0 });
            }

            List<Vector3> remaining_Nodes = new List<Vector3>(visibility_graph);
            while (remaining_Nodes.Count > 0)
            {
                Vector3 best_Candidate = BestCandidate(adjacency_matrix, isDominated, visibility_graph);

                remaining_Nodes.Remove(best_Candidate);

                for (int i = 0; i < adjacency_matrix.GetLength(0); i++)
                {
                    if (adjacency_matrix[(int)isDominated[best_Candidate][0], i] < float.MaxValue / 2) //>=0
                    {
                        remaining_Nodes.Remove(visibility_graph[i]);
                        if (isDominated[visibility_graph[i]][1] != 1f)
                        {
                            isDominated[visibility_graph[i]][1] = 0.5f;
                        }

                    }

                }
                isDominated[best_Candidate][1] = 1f;
                dominatingSet.Add(best_Candidate);
            }
            return dominatingSet;


        }

        private Vector3 BestCandidate(float[,] adjacency_matrix, Dictionary<Vector3, float[]> isDominated, List<Vector3> visibility_graph)
        {
            float best_value = float.MinValue;
            Vector3 candidate = new Vector3();
            foreach (Vector3 corner in visibility_graph)
            {
                int numberOfNeigbours = 0;

                for (int i = 0; i < adjacency_matrix.GetLength(0); i++)
                {
                    if (adjacency_matrix[(int)isDominated[corner][0], i] < float.MaxValue / 2 && isDominated[corner][1] == 0f) // >=0
                    {
                        numberOfNeigbours += 1;
                    }

                }
                if (numberOfNeigbours > best_value)
                {
                    best_value = numberOfNeigbours;
                    candidate = corner;
                }
            }
            return candidate;
        }


        //Floyd Warshal Part of Code:


        //This method (and print solution) was taked from geeksforgeeks.
        //I adjusted it so we can retrieve the path as well
        public Vector3[,] floyd_warshal(float[,] vis_graph)
        {
            //V: number of nodes in graph
            int V = vis_graph.GetLength(0);
            float[,] dist = new float[V, V];
            int i, j, k;
            Vector3[,] next = new Vector3[V, V];
            // Initialize the solution matrix  
            // same as input graph matrix 
            // Or we can say the initial  
            // values of shortest distances 
            // are based on shortest paths  
            // considering no intermediate 
            // vertex 
            for (i = 0; i < V; i++)
            {
                for (j = 0; j < V; j++)
                {
                    //dist[i, j] = (vis_graph[i, j] < 0f ? float.MaxValue : vis_graph[i, j]);
                    dist[i, j] = vis_graph[i, j];
                    if (vis_graph[i, j] < float.MaxValue / 2) //>=0
                    {
                        next[i, j] = visibility_corners[j]; //If there is a connection between (i,j) then next node from i to j is j
                    }

                }
            }

            for (i = 0; i < V; i++)
            {
                next[i, i] = visibility_corners[i]; //Initialization: If you wanna go from i to i, you use i
            }

            /* Add all vertices one by one to  
            the set of intermediate vertices. 
            ---> Before start of a iteration, 
                 we have shortest distances 
                 between all pairs of vertices 
                 such that the shortest distances 
                 consider only the vertices in 
                 set {0, 1, 2, .. k-1} as  
                 intermediate vertices. 
            ---> After the end of a iteration,  
                 vertex no. k is added 
                 to the set of intermediate 
                 vertices and the set 
                 becomes {0, 1, 2, .. k} */
            for (k = 0; k < V; k++)
            {
                // Pick all vertices as source 
                // one by one 
                for (i = 0; i < V; i++)
                {
                    // Pick all vertices as destination 
                    // for the above picked source 
                    for (j = 0; j < V; j++)
                    {
                        // If vertex k is on the shortest 
                        // path from i to j, then update 
                        // the value of dist[i][j] 
                        if (dist[i, k] + dist[k, j] < dist[i, j])
                        {
                            dist[i, j] = dist[i, k] + dist[k, j];
                            next[i, j] = next[i, k]; // Optimal path goes through k first, so we just need to take the same step towards k first
                        }
                    }
                }
            }

            // Print the shortest distance matrix 
            //printSolution(dist);
            min_distances = dist;
            return next;
        }

        //'Usefull' just for debugging. In the end though, not so useful, since the graph is rather large ^^
        void printSolution(float[,] dist)
        {
            int V = dist.GetLength(0);
            Debug.Log("Following matrix shows the shortest " +
                            "distances between every pair of vertices");
            for (int i = 0; i < V; ++i)
            {
                for (int j = 0; j < V; ++j)
                {
                    if (dist[i, j] < 0)
                    {
                        Debug.Log("INF ");
                    }
                    else
                    {
                        Debug.Log(dist[i, j] + " ");
                    }
                }
                Debug.Log("");
            }
        }

        //Utilized for debugging AND to populate the verbose version of the path
        //This method finds (and draws) the path from node u to node v, by iteratively
        //using our next[u,v] Vectror3 Array.
        public void draw_Path_between(Vector3 u, Vector3 v)
        {
            List<Vector3> path = new List<Vector3>();
            path.Add(u); //The path begins with u
            int index_of_v = visibility_corners.IndexOf(v);
            while (u != v) //until we reach v
            {
                int index_of_u = visibility_corners.IndexOf(u);

                u = next_node[index_of_u, index_of_v];
                verbose_tsp_path.Add(u);
                path.Add(u);
            }

            //This is mostly for debugging, you can see the path with alternating colors 
            //(so that you have a better understanding of where the agent is moving
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (counter % 2 == 0)
                {
                    //Debug.DrawLine(path[i], path[i + 1], Color.cyan, 100f);
                }
                else
                {
                    //Debug.DrawLine(path[i], path[i + 1], Color.red, 100f);
                }

            }
            counter += 1;
        }

        //Gives you the next point along the dominating set
        //It ignores the nodes that are seen[node]=0f
        //The next one is that which is nearer based on the distance
        //found in the floyd algorithm
        public (Vector3, int) tsp_next(int from, float[] seen)
        {
            float closest_distance = float.MaxValue;
            Vector3 closest_node = new Vector3();
            int index = 0;
            for (int i = 0; i < visibility_corners.Count; i++)
            {
                if (seen[i] == 1f && min_distances[from, i] < closest_distance)
                {
                    closest_distance = min_distances[from, i];
                    index = i;
                    closest_node = visibility_corners[i];
                }
            }
            return (closest_node, index);
        }

        //Creates the TSP graph, draws it and populates the verbose path
        public void nearest_neighbour_tsp(Vector3 start)
        {
            tsp_path = new List<Vector3>();

            float[] seen = new float[visibility_corners.Count];

            int index_of_u = visibility_corners.IndexOf(start);
            Vector3 from = start;

            //initialize seen. None v is ignored iff v is not in the dominating set, or has already been visited
            for (int i = 0; i < dominatingSet.Count; i++)
            {
                int index_of_this_node = visibility_corners.IndexOf(dominatingSet[i]);
                seen[index_of_this_node] = 1f; // 1: candidate next point, 0: not candidate
            }
            seen[index_of_u] = 0f; //we have just saw it
            tsp_path.Add(start); //add the start to the non verbose path
            verbose_tsp_path.Add(start); //and in the verbose
            while (tsp_path.Count <= dominatingSet.Count) //until we populated the path with all the nodes
            {
                var tsp_info = tsp_next(index_of_u, seen); //give me the next node
                Vector3 best_next = tsp_info.Item1;
                seen[tsp_info.Item2] = 0f;

                from = best_next;
                tsp_path.Add(best_next); //note how we add only the final node, and not the inbetween in the non verbose path
                index_of_u = visibility_corners.IndexOf(from);
            }

            for (int i = 0; i < tsp_path.Count - 1; i++)
            {
                draw_Path_between(tsp_path[i], tsp_path[i + 1]); //Here, we populate the verbose path
            }

            for (int i = 0; i < verbose_tsp_path.Count - 1; i++)
            {
                //draw_Path_between(verbose_tsp_path[i], verbose_tsp_path[i + 1]);
                //Debug.DrawLine(verbose_tsp_path[i], verbose_tsp_path[i + 1], Color.red, 100f); //debugging: see the path
            }
            Debug.Log("path size " + verbose_tsp_path.Count);


        }

        public List<Vector3> succint_to_verbose(List<Vector3> path)
        {
            List<Vector3> verbose_path = new List<Vector3>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                verbose_path.AddRange(populate_path(path[i], path[i + 1]));
            }

            return verbose_path;
        }


        public List<Vector3> populate_path(Vector3 u, Vector3 v)
        {

            List<Vector3> path = new List<Vector3>();
            path.Add(u); //The path begins with u
            int index_of_v = visibility_corners.IndexOf(v);
            while (u != v) //until we reach v
            {
                int index_of_u = visibility_corners.IndexOf(u);

                u = next_node[index_of_u, index_of_v];
                verbose_tsp_path.Add(u);
                if (!path.Contains(u)) //mostly for final node
                {
                    path.Add(u);
                }

            }


            return path;
        }

    }
}
