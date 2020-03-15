using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AStar : MonoBehaviour
{

    public float DISTANCE_COST = 0.5f;
    public GameObject terrain_manager_game_object;

    private TerrainManager terrainManager;
    private TerrainInfo terrainInfo;
    GameObject[] enemies;
    GameObject[] friends;
    // Start is called before the first frame update
    void Start()
    {
        terrainManager = terrain_manager_game_object.GetComponent<TerrainManager>();
        terrainInfo = terrainManager.myInfo;
    }

    private bool initialized = false;
    private void FixedUpdate() {
        if (!initialized) {
            initialized = true;
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            friends = GameObject.FindGameObjectsWithTag("Player");

            GameObject example_player = friends[0];
            GameObject example_enemy = enemies[7];

            List<Vector3> path = GetMinimumCostPath(example_player.transform.position, example_enemy.transform.position);
            for (int i = 0; i < path.Count - 1; i++) {
                Debug.DrawLine(path[i], path[i + 1], Color.yellow, 100f);
            }
            Debug.DrawLine(path.Last(), path.Last() + new Vector3(5, 0, 5) ,Color.red, 100f);
        }
    }

    public List<Vector3> GetMinimumCostPath(Vector3 from, Vector3 to) {
        int from_i = terrainInfo.get_i_index(from.x);
        int from_j = terrainInfo.get_j_index(from.z);
        int to_i = terrainInfo.get_i_index(to.x);
        int to_j = terrainInfo.get_j_index(to.z);

        List<Vector2Int> cells = GetMinimumCostPathCells(new Vector2Int(from_i, from_j), new Vector2Int(to_i, to_j), terrainInfo.traversability);
        List<Vector3> path = new List<Vector3>();
        foreach (Vector2Int cell in cells) {
            float x = terrainInfo.get_x_pos(cell.x);
            float z = terrainInfo.get_z_pos(cell.y);
            path.Add(new Vector3(x, 0, z));
        }
        return path;
    }

    private float h(Vector2Int from, Vector2Int goal) {
        return DISTANCE_COST * (Mathf.Abs(goal.x - from.x) + Mathf.Abs(goal.y - from.y));
    }

    private List<Vector2Int> GetMinimumCostPathCells(Vector2Int from, Vector2Int goal, float[,] cost_matrix) {
        int rows = cost_matrix.GetLength(0);
        int cols = cost_matrix.GetLength(1); 

        HashSet<Vector2Int> discovered_nodes = new HashSet<Vector2Int>();
        Vector2Int[, ] came_from = new Vector2Int[rows, cols];
        float[,] g_scores = new float[rows, cols];
        float[,] f_scores = new float[rows, cols];

        // Initialization
        discovered_nodes.Add(from);
        for (int r = 0; r < g_scores.GetLength(0); r++) {
            for (int c = 0; c < g_scores.GetLength(1); c++) {
                g_scores[r, c] = float.MaxValue;
                f_scores[r, c] = float.MaxValue;
                came_from[r, c] = new Vector2Int(-1, -1);
            }
        }
        g_scores[from.x, from.y] = 0;
        f_scores[from.x, from.y] = h(from, goal);

        while (discovered_nodes.Count > 0) {
            Vector2Int current = ArgMin(discovered_nodes, f_scores);
            if (current.Equals(goal)) {
                return ReconstructPath(came_from, goal);
            }

            discovered_nodes.Remove(current);
            foreach (Vector2Int n in GetNeighbors(current, cost_matrix)) {
                float tentative_score = g_scores[current.x, current.y] + DISTANCE_COST + cost_matrix[n.x, n.y];
                if (tentative_score < g_scores[n.x, n.y]) {
                    came_from[n.x, n.y] = current;
                    g_scores[n.x, n.y] = tentative_score;
                    f_scores[n.x, n.y] = g_scores[n.x, n.y] + h(n, goal);
                    if (!discovered_nodes.Contains(n)) {
                        discovered_nodes.Add(n);
                    }
                }
            }
        }

        return null; // Failure
    }

    private Vector2Int ArgMin(HashSet<Vector2Int> discovered_nodes, float[,] f_scores) {
        Vector2Int best = new Vector2Int(-1, -1);
        float min_score = float.MaxValue;
        foreach (Vector2Int point in discovered_nodes) {
            if (f_scores[point.x, point.y] < min_score) {
                min_score = f_scores[point.x, point.y];
                best = point;
            }
        }
        return best;
    }

    private List<Vector2Int> ReconstructPath(Vector2Int[,] came_from, Vector2Int goal) {
        List<Vector2Int> res = new List<Vector2Int>();
        res.Add(goal);
        Vector2Int current = goal;
        while(came_from[current.x, current.y].x != -1) {
            current = came_from[current.x, current.y];
            res.Insert(0, current);
        }
        return res;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int point, float[,] cost_matrix) {
        int rows = cost_matrix.GetLength(0);
        int cols = cost_matrix.GetLength(1);
        List<Vector2Int> neighbors = new List<Vector2Int>();
        neighbors.Add(new Vector2Int(point.x, point.y + 1));
        neighbors.Add(new Vector2Int(point.x + 1, point.y));
        neighbors.Add(new Vector2Int(point.x - 1, point.y));
        neighbors.Add(new Vector2Int(point.x, point.y - 1));

        List<Vector2Int> res = new List<Vector2Int>();
        foreach(Vector2Int n in neighbors) {
            if (n.x < 0 || n.x >= rows || n.y < 0 || n.y >= cols || cost_matrix[n.x, n.y] > float.MaxValue / 2) {
                continue;
            }
            res.Add(n);
        }
        return res;
    }



}
