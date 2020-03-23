using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityStandardAssets.Vehicles.Car
{
    public class AStar : MonoBehaviour
    {

        public float DISTANCE_COST = 0f;
        public GameObject terrain_manager_game_object;

        private TerrainManager terrainManager;
        private TerrainInfo terrainInfo;
        public GameObject terrain_evaluator_object;
        private TerrainEvaluator terrain_evaluator;
        public List<Vector3> path;
        GameObject[] enemies;
        GameObject[] friends;
        // Start is called before the first frame update
        void Start()
        {
            terrainManager = terrain_manager_game_object.GetComponent<TerrainManager>();
            terrainInfo = terrainManager.myInfo;
            terrain_evaluator = terrain_evaluator_object.GetComponent<TerrainEvaluator>();
        }

        public bool initialized = false;
        private void FixedUpdate()
        {
            if (!initialized)
            {
                initialized = true;
                enemies = GameObject.FindGameObjectsWithTag("Enemy");
                friends = GameObject.FindGameObjectsWithTag("Player");
            }
        }

        public float trimPath(List<Vector3> points, GameObject enemy)
        {
            float final_cost = 0;
            Vector3 enemy_location = enemy.transform.position;
            for (int i=0; i<points.Count; i++)
            {
                Vector3 point = points[i];
                var mask = ~(1 << LayerMask.NameToLayer("Inore Raycast"));
                bool ray = Physics.Raycast(point, enemy_location, mask);
                if (!ray)
                { break; }
                final_cost += terrain_evaluator.evaluation_matrix[terrainInfo.get_i_index(point.x), terrainInfo.get_j_index(point.z)];

            }

            return final_cost;
        }

        public (float cost, List<Vector3> points) GetMinimumCostPath(Vector3 from, Vector3 to)
        {
            int from_i = terrainInfo.get_i_index(from.x);
            int from_j = terrainInfo.get_j_index(from.z);
            int to_i = terrainInfo.get_i_index(to.x);
            int to_j = terrainInfo.get_j_index(to.z);



            (float cost, List<Vector2Int> cells) = GetMinimumCostPathCells(new Vector2Int(from_i, from_j),
                                                                            new Vector2Int(to_i, to_j),
                                                                            to,
                                                                            terrain_evaluator.evaluation_matrix);
                        
            List<Vector3> path = new List<Vector3>();
            foreach (Vector2Int cell in cells)
            {
                float x = terrainInfo.get_x_pos(cell.x);
                float z = terrainInfo.get_z_pos(cell.y);
                path.Add(new Vector3(x, 0, z));
            }
            return (cost, path);
        }

        private float h(Vector2Int from, Vector2Int goal)
        {
            return DISTANCE_COST * (Mathf.Abs(goal.x - from.x) + Mathf.Abs(goal.y - from.y));
        }

        public void update_map(List<GameObject> enemies)
        {
            List<Vector3> turret_locations = new List<Vector3>();
            foreach (GameObject enemy in enemies)
            {
                turret_locations.Add(enemy.transform.position);
            }
            terrain_evaluator.evaluation_matrix = terrain_evaluator.evaluate_board(terrainInfo.traversability, turret_locations);
        }

        private (float cost, List<Vector2Int> cells) GetMinimumCostPathCells(Vector2Int from, Vector2Int goal, Vector3 target_pos, float[,] cost_matrix)
        {
            int rows = cost_matrix.GetLength(0);
            int cols = cost_matrix.GetLength(1);

            HashSet<Vector2Int> discovered_nodes = new HashSet<Vector2Int>();
            Vector2Int[,] came_from = new Vector2Int[rows, cols];
            float[,] g_scores = new float[rows, cols];
            float[,] f_scores = new float[rows, cols];

            // Initialization
            discovered_nodes.Add(from);
            for (int r = 0; r < g_scores.GetLength(0); r++)
            {
                for (int c = 0; c < g_scores.GetLength(1); c++)
                {
                    g_scores[r, c] = float.MaxValue;
                    f_scores[r, c] = float.MaxValue;
                    came_from[r, c] = new Vector2Int(-1, -1);
                }
            }
            g_scores[from.x, from.y] = 0;
            f_scores[from.x, from.y] = h(from, goal);

            while (discovered_nodes.Count > 0)
            {
                Vector2Int current = ArgMin(discovered_nodes, f_scores);
                if (current.Equals(goal))
                {
                    return (g_scores[goal.x, goal.y], ReconstructPath(came_from, goal));
                }

                // If from current position we can see the target this is the goal
                if (see_target(current, target_pos)) {
                    return (g_scores[current.x, current.y], ReconstructPath(came_from, current));
                }
                discovered_nodes.Remove(current);

                foreach (Vector2Int n in GetNeighbors(current, cost_matrix))
                {
                    float tentative_score = g_scores[current.x, current.y] + DISTANCE_COST + cost_matrix[n.x, n.y];
                    if (tentative_score < g_scores[n.x, n.y])
                    {
                        came_from[n.x, n.y] = current;
                        g_scores[n.x, n.y] = tentative_score;
                        f_scores[n.x, n.y] = g_scores[n.x, n.y] + h(n, goal);
                        if (!discovered_nodes.Contains(n))
                        {
                            discovered_nodes.Add(n);
                        }
                    }
                }
            }

            Debug.LogError("ERROR NO PATH FOUND");
            return (-1, null); // Failure
        }

        private Vector2Int ArgMin(HashSet<Vector2Int> discovered_nodes, float[,] f_scores)
        {
            Vector2Int best = new Vector2Int(-1, -1);
            float min_score = float.MaxValue;
            foreach (Vector2Int point in discovered_nodes)
            {
                if (f_scores[point.x, point.y] < min_score)
                {
                    min_score = f_scores[point.x, point.y];
                    best = point;
                }
            }
            return best;
        }

        private List<Vector2Int> ReconstructPath(Vector2Int[,] came_from, Vector2Int goal)
        {
            List<Vector2Int> res = new List<Vector2Int>();
            res.Add(goal);
            Vector2Int current = goal;
            while (came_from[current.x, current.y].x != -1)
            {
                current = came_from[current.x, current.y];
                res.Insert(0, current);
            }
            return res;
        }

        private List<Vector2Int> GetNeighbors(Vector2Int point, float[,] cost_matrix)
        {
            int rows = cost_matrix.GetLength(0);
            int cols = cost_matrix.GetLength(1);
            List<Vector2Int> neighbors = new List<Vector2Int>();
            neighbors.Add(new Vector2Int(point.x, point.y + 1));
            neighbors.Add(new Vector2Int(point.x + 1, point.y));
            neighbors.Add(new Vector2Int(point.x - 1, point.y));
            neighbors.Add(new Vector2Int(point.x, point.y - 1));

            List<Vector2Int> res = new List<Vector2Int>();
            foreach (Vector2Int n in neighbors)
            {
                if (n.x < 0 || n.x >= rows || n.y < 0 || n.y >= cols || cost_matrix[n.x, n.y] > float.MaxValue / 2)
                {
                    continue;
                }
                res.Add(n);
            }
            return res;
        }

        private bool see_target(Vector2Int current, Vector3 target) {
            Vector3 current_in_space = new Vector3(terrainInfo.get_x_pos(current.x), 0, terrainInfo.get_z_pos(current.y));
            Vector3 direction = target - current_in_space;
            Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
            var mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            bool free = ! Physics.Linecast(current_in_space, target, mask);

            if (free) { Debug.DrawLine(current_in_space, target, Color.black, 30f); }
            return free;
        }



    }
}
