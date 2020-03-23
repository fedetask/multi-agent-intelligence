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
        bool initialized = false;
        AStar aStar;
        public GameObject aStar_game_object;
        public GameObject[] friends;
        private int total_number_of_enemies=8;
        public int leader_index;
        // Start is called before the first frame update
        void Start()
        {
            leader_orientation = leader_car.transform.forward;
            previous_position = leader_car.transform.position;

            float[] max_dist = max_distances(leader_orientation, previous_position);
            
            agent_positions = line_positions(leader_orientation, previous_position, max_dist);
            aStar = aStar_game_object.GetComponent<AStar>();
        }


        public Vector3 get_next_position(int agent_id)
        {
            return agent_positions[Mathf.Min(agent_positions.Count-1,agent_id)];
        }

        private float[] max_distances(Vector3 leader_orientation, Vector3 leader_position)
        {
            float margin = 4f;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, leader_orientation); //right orientation
            RaycastHit hit_right;
            var mask = ~(1 << LayerMask.NameToLayer("Inore Raycast")); // Take the mask corresponding to the layer with Name Cube Walls
            //var mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Ignore Raycast")));
            Physics.Raycast(leader_position, perpendicular, out hit_right,mask);
            RaycastHit hit_left;
            Physics.Raycast(leader_position, -perpendicular, out hit_left,mask);
            float right = Mathf.Min(max_length, hit_right.distance-margin);
            float left = Mathf.Min(max_length, hit_left.distance-margin);
            return new float[2] { left, right };
        }

       
        private List<Vector3> line_positions(Vector3 leader_orientation, Vector3 leader_position, float[] distances)
        {
            Vector3 perpendicular = Vector3.Cross(Vector3.up, leader_orientation);
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


            if (leader_car == null)
            {
                friends = GameObject.FindGameObjectsWithTag("Player");

                CarAI5 script = friends[0].GetComponent<CarAI5>();
                script.path_counter = leader_index;

                script.isLeader = true;
                leader_car = friends[0];
                leader_orientation = leader_car.transform.forward;

            }

            leader_orientation = leader_car.transform.forward;
            leader_speed = Vector3.Distance(leader_car.transform.position,previous_position) / Time.deltaTime; 

            previous_position = leader_car.transform.position;
            float[] max_dist = max_distances(leader_orientation, previous_position);
            agent_positions = line_positions(leader_orientation, previous_position, max_dist);

            List<GameObject> enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
            // If enemies doesn't contain current_target it means it was killed
            if (current_target == null || !enemies.Contains(current_target) || enemies.Count<total_number_of_enemies) {
                aStar.update_map(enemies);
                total_number_of_enemies = enemies.Count;
                (current_target, aStar.path) = get_next_target(enemies);
            }
            



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
            if(enemies.Count==4)
            {
                //friends[0].GetComponent<CarAI5>().id = 2;
                //friends[2].GetComponent<CarAI5>().id = 0;
            }
            friends = GameObject.FindGameObjectsWithTag("Player");

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