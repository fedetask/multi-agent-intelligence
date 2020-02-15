using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI2 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;
        public GameObject visibility_game_object;
        public VisibilityGraph visibility;

        public List<Vector3> visibility_graph;
        public List<Vector3> verbose_path; //verbose ALL of the nodes of the TSP path
        public List<Vector3> path; //non verbose only containts the nodes within the TSP path that belongs to the dominating sent

        public int[] seen_thus_far; //nodes we have seen thus far (indeces of nodes)


        public int next_index = 1; //start at 1, ignore the starting position of the our cars

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            visibility = visibility_game_object.GetComponent<VisibilityGraph>();
            visibility_graph = visibility.visibility_corners;
            seen_thus_far = new int[visibility_graph.Count];
            verbose_path = visibility.verbose_tsp_path;
            path = visibility.tsp_path;
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            // Note that you are not allowed to check the positions of the turrets in this problem



            // Plan your path here
            // ...
        }

        //Inspect the world around you. Store all the nodes you have seen
        public void inspect()
        {
            Vector3 current_pos = transform.position;

            for (int i=0; i< visibility_graph.Count; i++)
            {
                var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
                bool ray = Physics.Linecast(current_pos, visibility_graph[i], mask);
                if(!ray)
                {
                    seen_thus_far[i] = 1;
                }
            }
        }

        //Check whether you could steer towards the next point (if the car can drive in a straight line)
        public bool check_for_line(int next)
        {
            var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
            Vector3 current_pos = transform.position;
            Vector3 direction = verbose_path[next] - current_pos;
            Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
            
            float step = (visibility.margin - 0.1f) / 2;
            int[] signs = new int[] { -1, 0, 1 };

            bool free = true;
            Debug.DrawLine(current_pos, verbose_path[next], Color.yellow, 100f);
            RaycastHit ray;
            foreach (int sign in signs)
            {
                if (Physics.Linecast(current_pos + sign * step * normal, verbose_path[next] + sign * step * normal,out ray,mask))
                {
                    //Debug.DrawLine(current_pos, ray.point, Color.yellow, 100f);
                    free = false;
                }
            }

            return free;
        }

        //Check whether you have seen all of the neighbours of the current goal.
        //If you have, you can go to the next node (smoothing)
        public bool move_to_next(int current)
        {
            bool result = true;

            int index_of_current = visibility_graph.IndexOf(verbose_path[current]);

            float[,] adj_matrix = visibility.get_matrix();

            for (int i=0; i<adj_matrix.GetLength(0); i++)
            {
                if(adj_matrix[index_of_current,i]<float.MaxValue/2 && seen_thus_far[i]!=1)
                {
                    result = false;
                    break;
                }
            }

            return result;

        }

        private void FixedUpdate()
        {
            verbose_path = visibility.verbose_tsp_path;

            //update what you have seen thus far
            inspect();

            // Execute your path here
            // ...
            if (move_to_next(next_index) && check_for_line(next_index+1)) //If you seen all, and you can reach the next goal
            {
                next_index += 1;
            }

            
            Vector3 next_pos = verbose_path[next_index];

          
          
            Vector3 direction = (next_pos - transform.position).normalized;

            Debug.DrawLine(transform.position, next_pos, Color.white, 100f);
            bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

            float steering = 0f;
            float acceleration = 0;

            if (is_to_the_right && is_to_the_front)
            {
                steering = 1f;
                acceleration = 1f;
            }
            else if (is_to_the_right && !is_to_the_front)
            {
                steering = -1f;
                acceleration = -1f;
            }
            else if (!is_to_the_right && is_to_the_front)
            {
                steering = -1f;
                acceleration = 1f;
            }
            else if (!is_to_the_right && !is_to_the_front)
            {
                steering = 1f;
                acceleration = -1f;
            }

            m_Car.Move(steering, acceleration, acceleration, 0f);
            

        }
    }
}
