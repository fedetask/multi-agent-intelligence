using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VisibilityGraph : MonoBehaviour {

    public float margin = 4; 
    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    
	// Use this for initialization

    List<Vector3> visibility_corners;
    float[,] adjacency_matrix;

    public List<Vector3> dominatingSet = new List<Vector3>();

	void Start () {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        visibility_corners = GetCorners();
        CorrectionCorners(visibility_corners);
        Debug.Log("starting visibility");
        adjacency_matrix = GetAdjacencyMatrix(visibility_corners);
        dominatingSet = GreedyDominatingSet(visibility_corners, adjacency_matrix);
        
        foreach (Vector3 v in dominatingSet) {
            terrain_manager.DrawCircle(v, 5, 2);
        }
    }


    public List<Vector3> GetCorners() {
        TerrainInfo myInfo = terrain_manager.myInfo;
        float[,] pad_traversability = GetPaddedTraversability(myInfo.traversability);
        float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N; 
        float y = myInfo.start_pos.y;
        int rows = pad_traversability.GetLength(0);
        int cols = pad_traversability.GetLength(1);
        List<Vector3> valid_corners = new List<Vector3>();

        for (int r = 1; r < rows -1; r++) {
            for (int c = 1; c < cols - 1; c++) {
                if (pad_traversability[r, c] == 0.0f) { continue; } // Empty cells don't have corners
                // A corner of a cell in a given direction is valid iff
                // - The cell that contains it and the two adjacent cells that also touch the generating cell are free
                // - The cell that contains it is free and the two adjacent cells that also touch the generating cell are full
                int[] corner_steps = new int[] {-1, -1, +1, +1, -1};
                int[] adjacent_steps = new int[] {0, -1, 0, +1, 0, -1};
                for (int i = 0; i < 4; i++) {
                    if (pad_traversability[r + corner_steps[i], c + corner_steps[i + 1]] == 1.0f) { continue; } // Corner is in a full cell
                    // The two adjacent cells that also touch the generating cell are
                    Cell c1 = new Cell(r + adjacent_steps[i], c + adjacent_steps[i + 1]);
                    Cell c2 = new Cell(r + adjacent_steps[i + 1], c + adjacent_steps[i + 2]);
                    if (c1.row == 0 || c1.row == rows-1 || c1.col == 0 || c1.col == cols-1
                        ||c1.row == 0 || c1.row == rows-1 || c1.col == 0 || c1.col == cols-1) {
                            continue;
                        }
                    if ((pad_traversability[c1.row, c1.col] == 0.0f && pad_traversability[c2.row, c2.col] == 0.0f)
                        || (pad_traversability[c1.row, c1.col] == 1.0f && pad_traversability[c2.row, c2.col] == 1.0f)) { // Both free or both full
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

    public List<Vector3> GetPathPoints(Vector3 source, Vector3 destination, List<Vector3> visibility_corners, float[,] adjacency_matrix) {
        // float time = Time.time;
        List<Vector3> path_points = new List<Vector3>();
        List<Vector3> corners = new List<Vector3>();
        //corners.Add(source);
        //corners.Add(destination);
        corners.AddRange(visibility_corners);

        float[, ] adjacencies = GetAdjacencyMatrix(corners);
        List<int> path_indexes = Dijkstra.get_shortest_path(adjacencies);
        foreach (int path_index in path_indexes) {
            path_points.Add(corners[path_index]);
        }
        return path_points;
    }
	
	// Update is called once per frame
	void Update () {
	}

    float[, ] GetAdjacencyMatrix(List<Vector3> corners) {
        float[, ] adjancenies = new float[corners.Count, corners.Count];
        var mask =  (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
        for (int i = 0; i < corners.Count; i++) {
            for (int j = i + 1; j < corners.Count; j++) {
                Vector3 direction = corners[j] - corners[i];
                Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                float step = (margin - 0.1f) / 2;
                int[] signs = new int[] {-1, 0, 1};
                bool free = true;
                foreach (int sign in signs) {
                    if (Physics.Linecast(corners[i] + sign * step * normal, corners[j] + sign * step * normal,mask)) {
                        adjancenies[i, j] = -1;
                        adjancenies[j, i] = -1;
                        free = false;
                    }
                }
                if (free) {
                    float dist = Vector3.Distance(corners[i], corners[j]);
                    //Debug.Log("I am in here");
                    //Debug.DrawLine(corners[i], corners[j], Color.cyan, 100f);
                   
                    adjancenies[i, j] = dist;
                    adjancenies[j, i] = dist;
                }
            }
        }
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
                    if(corners[i].x==corners[j].x || corners[i].z == corners[j].z)
                    {
                        
                        Vector3 middlePoint = (corners[i] + corners[j]) / 2;
                        if (!new_Corners.Contains(middlePoint) && !corners.Contains(middlePoint))
                        {
                            new_Corners.Add(middlePoint);
                        }
                    }
                    
                    //Debug.Log("I am in here");
                    //Debug.DrawLine(corners[i], corners[j], Color.cyan, 100f);
                }
            }
        }
        corners.AddRange(new_Corners);
    }

    private float[,] GetPaddedTraversability(float[,] traversability) {
        float[,] res = new float[traversability.GetLength(0)+2, traversability.GetLength(1)+2];
        for (int r = 0; r < traversability.GetLength(0); r++) {
            for (int c = 0; c < traversability.GetLength(1); c++) {
                res[r + 1, c + 1] = traversability[r, c];
            }
        }
        return res;
    }

    private List<Vector3> GreedyDominatingSet(List<Vector3> visibility_graph, float[,] adjacency_matrix)
    {
        List<Vector3> dominatingSet = new List<Vector3>();
        Dictionary<Vector3, float[]> isDominated = new Dictionary<Vector3, float[]>();
        foreach(Vector3 corner in visibility_graph)
        {
            isDominated.Add(corner, new float[2] { visibility_graph.IndexOf(corner),0 });
        }

        List<Vector3> remaining_Nodes = new List<Vector3>(visibility_graph);
        while (remaining_Nodes.Count>0)
        {
            Vector3 best_Candidate = BestCandidate(adjacency_matrix, isDominated, visibility_graph);

            remaining_Nodes.Remove(best_Candidate);

            for (int i = 0; i < adjacency_matrix.GetLength(0); i++)
            {
                if (adjacency_matrix[(int)isDominated[best_Candidate][0], i] >= 0f)
                {
                    remaining_Nodes.Remove(visibility_graph[i]);
                    if (isDominated[visibility_graph[i]][1] !=1f)
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
        foreach(Vector3 corner in visibility_graph)
        {
            int numberOfNeigbours = 0;
            
            for (int i=0; i<adjacency_matrix.GetLength(0); i++)
            {
                if(adjacency_matrix[(int)isDominated[corner][0], i]>=0f && isDominated[corner][1]==0f)
                {
                    numberOfNeigbours += 1;
                }

            }
            if (numberOfNeigbours>best_value)
            {
                best_value = numberOfNeigbours;
                candidate = corner;
            }
        }
        return candidate;
    }
}
