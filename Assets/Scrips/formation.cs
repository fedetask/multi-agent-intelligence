using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public class formation : MonoBehaviour
    {
        public GameObject leader_car;
        public Vector3 leader_orientation;
        public float leader_speed;
        public Vector3 previous_position;
        public int number_of_agents = 4;
        public float max_length = 80;
        public List<Vector3> agent_positions;
        public bool dynamicLeader = false;
        public bool initialized = false;
        AStar aStar;
        public GameObject aStar_game_object;
        public GameObject[] friends;
        public int total_number_of_enemies=8;

        public bool perpendicular_line = true;

        List<Vector3> path;


        // Start is called before the first frame update
        void Start()
        {
            leader_orientation = leader_car.transform.forward;
            previous_position = leader_car.transform.position;

            float[] max_dist = max_distances(leader_orientation, previous_position);
            
            agent_positions = line_positions(leader_orientation, previous_position, max_dist);
            aStar = aStar_game_object.GetComponent<AStar>();
        }

        Vector3 compute_direction(Vector3 target_pos, List<Vector3> path)
        {
            int i;
            Vector3 direction = new Vector3();
            for (i = 0; i < path.Count; i++)
            {
                Vector3 ideal_next_pos = target_pos;
                Vector3 current_pos = leader_car.GetComponent<CarAI5>().path[i];
                float margin = 4f;
                direction = ideal_next_pos - current_pos;
                Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                float step = (margin - 0.1f) / 2;
                int[] signs = new int[] { -1, 0, 1 };
                bool free = true;
                var mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
                foreach (int sign in signs)
                {
                    if (Physics.Linecast(current_pos + sign * step * normal, ideal_next_pos + sign * step * normal, out RaycastHit rayhit, mask))
                    {
                        free = false;
                        if (free == false)
                        {
                            //Debug.Log("We hit a " + rayhit.collider.name);
                        }
                    }
                }

                if (free)
                {
                    break;
                }
            }
            //Debug.DrawLine(leader_car.transform.position, leader_car.transform.position - 10 * direction.normalized, Color.red, 0.1f);
            return -direction.normalized;
            bool is_on_right = Vector3.Dot(direction, path[i] - path[i-1]) > 0f;
            if (is_on_right)
            {
                return -direction.normalized;
            }
            else
            {
                return direction.normalized;
            }
        }
        public Vector3 get_next_position(int agent_id)
        {
            return agent_positions[Mathf.Min(agent_positions.Count-1,agent_id)];
        }

        private float[] max_distances(Vector3 leader_orientation, Vector3 leader_position)
        {
            float margin = 4f;
            Vector3 perpendicular = new Vector3();
            if(perpendicular_line)
            {
                perpendicular = Vector3.Cross(Vector3.up, leader_orientation); //right orientation
            }
            else
            {
                perpendicular = compute_direction(current_target.transform.position, path);
            }
            RaycastHit hit_right;
            var mask = ~(1 << LayerMask.NameToLayer("Inore Raycast")); // Take the mask corresponding to the layer with Name Cube Walls
            //var mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Ignore Raycast")));
            Physics.Raycast(leader_position+ new Vector3(0f,4f,0f), perpendicular, out hit_right,mask);
            RaycastHit hit_left;
            Physics.Raycast(leader_position + new Vector3(0f, 4f, 0f), -perpendicular, out hit_left,mask);
            float right = Mathf.Min(max_length, hit_right.distance-margin);
            float left = Mathf.Min(max_length, hit_left.distance-margin);
            Debug.Log("right " + right);
            Debug.Log("left " + left);
            Debug.DrawLine(leader_position, leader_position + right * perpendicular, Color.white, 0.1f);
            Debug.DrawLine(leader_position, leader_position - left * perpendicular, Color.yellow, 0.1f);

            return new float[2] { left, right };
        }

       
        private List<Vector3> line_positions(Vector3 leader_orientation, Vector3 leader_position, float[] distances)
        {
            Vector3 perpendicular;

            if (perpendicular_line)
            {
                perpendicular = Vector3.Cross(Vector3.up, leader_orientation); //right orientation
            }
            else
            {
                perpendicular = compute_direction(current_target.transform.position, path);
            }
            int number_of_robots_r = number_of_agents / 2; //assume even
            int number_of_robots_l = number_of_agents - number_of_robots_r;
            List<Vector3> positions = new List<Vector3>();
            
            for (int i = 0; i < number_of_robots_l; i++)
            {
                Vector3 position = leader_position - perpendicular.normalized * distances[0] * (number_of_robots_l - i) / number_of_robots_l;
                positions.Add(position);
            }
            for (int i = 0; i < number_of_robots_r; i++)
            {
                Vector3 position = leader_position + perpendicular.normalized * distances[1] * (i + 1) / number_of_robots_r;
                positions.Add(position);
            }


            Debug.DrawLine(positions[0],positions[positions.Count-1],Color.cyan,0.1f);
            return positions;
        }




        GameObject current_target = null;
            // Update is called once per frame
        void Update()
        {
            if(!initialized)
            {
                friends = GameObject.FindGameObjectsWithTag("Player");
                int counter = 0;
                foreach (GameObject obj in friends)
                {
                    CarAI5 script = obj.GetComponent<CarAI5>();
                    
                    script.set_id(counter);
                    if (dynamicLeader && counter == 1)
                    {
                        script.isLeader = true;
                    }
                    if(dynamicLeader && counter==0)
                    {
                        script.is_on_left = true;
                    }
                    if(dynamicLeader && counter==2)
                    {
                        script.is_on_right = true;
                    }
                    counter += 1;

                }
                initialized = true;
            }
            leader_orientation = leader_car.transform.forward;
            leader_speed = Vector3.Distance(leader_car.transform.position,previous_position) / Time.deltaTime;
            //perpendicular_line = false;
            previous_position = leader_car.transform.position;
            

            List<GameObject> enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));

            
            // If enemies doesn't contain current_target it means it was killed
            if (current_target == null || !enemies.Contains(current_target) || enemies.Count<total_number_of_enemies) {
                aStar.update_map(enemies);
                total_number_of_enemies = enemies.Count;
                (current_target, aStar.path) = get_next_target(enemies);
                path = aStar.path;
            }
            float[] max_dist = max_distances(leader_orientation, previous_position);

            agent_positions = line_positions(leader_orientation, previous_position, max_dist);
        }

      

        (GameObject target, List<Vector3> path) get_next_target(List<GameObject> enemies) {
            float min_cost = float.MaxValue;
            GameObject best = null;
            List<Vector3> path = null;
            foreach (GameObject enemy in enemies) {
               
                (float cost, List<Vector3> points) = aStar.GetMinimumCostPath(leader_car.transform.position, enemy.transform.position);
                Debug.Log("initial Cost " + cost);
                cost = aStar.trimPath(points,enemy);
                Debug.Log("final Cost " + cost);
                if (cost < min_cost) {
                    min_cost = cost;
                    best = enemy;
                    path = points;
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], Color.cyan, 30f);
            }
            if(enemies.Count==5)
            {
                //friends[0].GetComponent<CarAI5>().id = 2;
                //friends[2].GetComponent<CarAI5>().id = 0;
            }
            
            int counter = 0;
            foreach (GameObject obj in friends)
            {
                CarAI5 script = obj.GetComponent<CarAI5>();
                script.path_counter = 0;
                if (script.isLeader)
                {
                    script.path=path;
                }
            }

            Debug.Log("Best next enemy" + enemies.IndexOf(best));
                return (best, path);
        }

        }
    }