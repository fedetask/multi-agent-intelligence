using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI1 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        public float crash_timer = 0f;
        public float crash_timer_threshold = 2f;
        private bool has_crashed = false;
        public float crash_correction_timer = 2f;
        public float current_speed;
        private GameObject[] friends;
        private GameObject[] enemies;
        public GameObject spanning_tree_object;
        private SpanningTree spanning_tree;
        private List<Vector3> visibility_graph;
        public List<Vector3> verbose_path = new List<Vector3>(); //verbose ALL of the nodes of the TSP path
        public List<Vector3> path = new List<Vector3>(); //non verbose only containts the nodes within the TSP path that belongs to the dominating sent

        public bool path_initialized = false;

        private int[] seen_thus_far; //nodes we have seen thus far (indeces of nodes)
        private int next_index = 1; //start at 1, ignore the starting position of the our cars
        public int index_of_current_player;


        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            Time.timeScale = 10;

            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

            //current_speed = m_Car.CurrentSpeed;
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
            current_speed = m_Car.CurrentSpeed;
            for (int i = 0; i < visibility_graph.Count; i++)
            {
                var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
                bool ray = Physics.Linecast(current_pos, visibility_graph[i], mask);
                if (!ray)
                {
                    seen_thus_far[i] = 1;
                }
            }
        }


        private List<Vector3> compute_paths(SpanningTree span)
        {
            List<Vector3> path = span.paths[index_of_current_player];
            path_initialized = true;
            return path;
            
        }

        //Check whether you could steer towards the next point (if the car can drive in a straight line)
        public bool check_for_line(int next)
        {
            var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
            Vector3 current_pos = transform.position;
            Vector3 direction = verbose_path[next] - current_pos;
            Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
            int margin = 4;
            float step = (4 - 0.1f) / 2;
            int[] signs = new int[] { -1, 0, 1 };

            bool free = true;
            //Debug.DrawLine(current_pos, verbose_path[next], Color.yellow, 100f);
            RaycastHit ray;
            foreach (int sign in signs)
            {
                if (Physics.Linecast(current_pos + sign * step * normal, verbose_path[next] + sign * step * normal, out ray, mask))
                {
                    //Debug.DrawLine(current_pos, ray.point, Color.yellow, 100f);
                    free = false;
                }
            }

            return free;
        }

       

        private void FixedUpdate()
        {

            if (!path_initialized)
            {
                spanning_tree = spanning_tree_object.GetComponent<SpanningTree>();
                verbose_path = compute_paths(spanning_tree);
            }

           
            // Execute your path here
            // ...
           

            Vector3 next_pos = verbose_path[next_index];

            

            Vector3 direction = (next_pos - transform.position).normalized;
            current_speed = m_Car.CurrentSpeed;
            bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

            float steering = 0f;
            float acceleration = 0;
            Vector3 current_direction = transform.forward.normalized;

            float direction_angle = Vector3.Angle(current_direction, direction) * Mathf.Sign(-current_direction.x * direction.z + current_direction.z * direction.x);


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

            steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle) * Mathf.Sign(acceleration) ;

            if (Mathf.Abs(steering) < 0.2f)
            { steering = 0; }

            // this is how you control the car
            m_Car.Move(steering, acceleration, acceleration, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);

            if (Vector3.Distance(next_pos, transform.position) < 5f)
            {
                next_index += 1;
                next_pos = verbose_path[next_index];
            }
            /*
            Vector3 current_direction = transform.forward.normalized; //vector facing forward from the car

            Vector3 direction = (next_pos - transform.position).normalized; //vector showcasing desired orientation towards next goal

            Debug.DrawLine(transform.position, next_pos, Color.white, 0.1f);

            //bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            //bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;


            //left or right can be determined by the sign outer producte current_direction x direction
            float direction_angle = Vector3.Angle(current_direction, direction) * Mathf.Sign(-current_direction.x * direction.z + current_direction.z * direction.x);

            float steering; //steering between -1,1 (full left - full right)

            float acceleration; //acceleration between -1,1 (full reverse - full ahead)

            float direction_of_acceleration = Mathf.Clamp(Vector3.Dot(current_direction, direction), -1, 1); //this variable determines whether we reverse or not
                                                                                                             //obtuse angle -> reverse, acute angle->forward 

            if (Mathf.Abs(direction_of_acceleration) < 0.1f) //if we are about ~ 90 degrees angle, prefer going forward
            {
                direction_of_acceleration = 0f;
            }

            if (has_crashed) //If we crashed, time to correct it
            {
                crash_correction_timer -= Time.deltaTime; //start counting down
                direction_of_acceleration = -direction_of_acceleration; //Obviously, what we are doing atm does not work: try going the opposite way!

                if (crash_correction_timer <= 0) //after we corrected for enough time
                {
                    crash_correction_timer = 1f; //reset the correction timer
                    has_crashed = false; //we are no longer in the crashing phase
                    crash_timer = 0f;
                }
            }

            acceleration = Mathf.Sign(direction_of_acceleration); //1 if we go forward, -1 if we wanna reverse

            if (current_speed < 1f) //If we are going really slow, we very likely crashed -> start counting!
            {
                crash_timer += Time.deltaTime; //start counting

                if (crash_timer > crash_timer_threshold) //if we are going slow for a long enough time
                {
                    has_crashed = true; //we crashed
                }

            }
            else
            {
                crash_timer = 0f; //else, all cool/false alarm, reset the timer
            }

            //If we plan on going in reverse (sign of direction of acceleration), then we need to invert our steering
            //Also, clamp it betwee -1 and 1
            steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle) / m_Car.m_MaximumSteerAngle * Mathf.Sign(direction_of_acceleration);

            if (Mathf.Abs(steering) < 0.2f) //We are 'close enough' to the correct movement
            {
                //if it is too small, don't do it
                steering = 0f;
            }

            m_Car.Move(steering, acceleration, acceleration, 0f);
            */

        }
    }
}
