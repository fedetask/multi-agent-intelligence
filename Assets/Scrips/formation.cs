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
        public float max_length = 40;
        public List<Vector3> agent_positions;

        bool initialized = false;
        // Start is called before the first frame update
        void Start()
        {
            leader_orientation = leader_car.transform.forward;
            previous_position = leader_car.transform.position;

            float[] max_dist = max_distances(leader_orientation, previous_position);
            agent_positions = line_positions(leader_orientation, previous_position, max_dist);

           


        }


        public Vector3 get_next_position(int agent_id)
        {
            return agent_positions[agent_id];
        }

        private float[] max_distances(Vector3 leader_orientation, Vector3 leader_position)
        {
            Vector3 perpendicular = Vector3.Cross(Vector3.up, leader_orientation); //right orientation
            RaycastHit hit_right;
            var mask = ~(1 << LayerMask.NameToLayer("Ingore Raycast")); // Take the mask corresponding to the layer with Name Cube Walls
            //var mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Ignore Raycast")));
            Physics.Raycast(leader_position, perpendicular, out hit_right,mask);
            RaycastHit hit_left;
            Physics.Raycast(leader_position, -perpendicular, out hit_left,mask);
            float right = Mathf.Min(max_length, hit_right.distance);
            float left = Mathf.Min(max_length, hit_left.distance);
            return new float[2] { left, right };
        }

        private List<Vector3> line_positions(Vector3 leader_orientation, Vector3 leader_position, float[] distances)
        {
            Vector3 perpendicular = Vector3.Cross(Vector3.up, leader_orientation);
            int number_of_robots_r = number_of_agents / 2; //assume even
            int number_of_robots_l = number_of_agents - number_of_robots_r;
            List<Vector3> positions = new List<Vector3>();
            
            for (int i = 0; i < number_of_robots_r; i++)
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




            // Update is called once per frame
        void Update()
        {
            if(!initialized)
            {
                GameObject[] friends = GameObject.FindGameObjectsWithTag("Player");
                int counter = 0;
                Debug.Log("Friend size " + friends.Length);
                foreach (GameObject obj in friends)
                {

                    CarAI4 script = obj.GetComponent<CarAI4>();
                    Debug.Log("Current object " + obj.name);
                    script.set_id(counter);

                    counter += 1;

                }
                initialized = true;
            }
            leader_orientation = leader_car.transform.forward;
            leader_speed = Vector3.Distance(leader_car.transform.position,previous_position) / Time.deltaTime; 

            previous_position = leader_car.transform.position;
            float[] max_dist = max_distances(leader_orientation, previous_position);
            agent_positions = line_positions(leader_orientation, previous_position, max_dist);

        }
        }
    }