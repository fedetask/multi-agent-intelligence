using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI3 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        public float crash_timer = 0f;
        public float crash_timer_threshold = 2f;
        private bool has_crashed = false;
        public float crash_correction_timer = 3f;
        public float current_speed;
        private GameObject[] friends;
        private GameObject[] enemies;
        public GameObject visibility_game_object;
        private VisibilityGraph visibility;
        private List<Vector3> visibility_graph;
        public List<Vector3> verbose_path = new List<Vector3>(); //verbose ALL of the nodes of the TSP path
        public List<Vector3> path = new List<Vector3>(); //non verbose only containts the nodes within the TSP path that belongs to the dominating sent

        public bool path_initialized = false;

        private int[] seen_thus_far; //nodes we have seen thus far (indeces of nodes)
        private int next_index = 0; //start at 1, ignore the starting position of the our cars
        public int index_of_current_player;


        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();


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


        private List<Vector3> compute_paths(Paths solutions)
        {

            seen_thus_far = visibility.seen_thus_far;
            List<int> path_indeces = solutions.GetPath(index_of_current_player);//visibility.geneticTSP.GetBest().GetPath(index_of_current_player);
            Debug.Log("Path length " + path_indeces.Count);
            path.Add(visibility.start_pos);
            for (int i = 0; i < path_indeces.Count; i++)
            {
                path.Add(visibility_graph[path_indeces[i]]);
            }
            verbose_path = visibility.succint_to_verbose(path);

            for (int i = 0; i < verbose_path.Count - 1; i++)
            {
                //Debug.DrawLine(verbose_path[i], verbose_path[i + 1], Color.red, 100f);
            }
            path_initialized = true;
            return verbose_path;

        }

        //Check whether you could steer towards the next point (if the car can drive in a straight line)
        public bool check_for_line(int next)
        {
            var mask = (1 << LayerMask.NameToLayer("CubeWalls")); // Take the mask corresponding to the layer with Name Cube Walls
            Vector3 current_pos = transform.position;
            Vector3 direction = verbose_path[next] - current_pos;
            Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;

            float step = (visibility.get_margin() - 0.1f) / 2;
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

        //Check whether you have seen all of the neighbours of the current goal.
        //If you have, you can go to the next node (smoothing)
        public bool move_to_next(int current)
        {
            bool result = true;

            if (!visibility.dominatingSet.Contains(verbose_path[current]))
            { return true; }

            int index_of_current = visibility_graph.IndexOf(verbose_path[current]);



            float[,] adj_matrix = visibility.get_matrix();

            for (int i = 0; i < adj_matrix.GetLength(0); i++)
            {
                if (adj_matrix[index_of_current, i] < float.MaxValue / 2 && seen_thus_far[i] != 1)
                {
                    result = false;
                    break;
                }
            }

            return result;

        }


        private bool steering_correction_left(int next)
        {
            Vector3 next_position = verbose_path[next];

            float length = (4.47f-0.1f);
            float width = (2.43f-0.1f);

            Vector3 current_position = transform.position + new Vector3(0f, 4f, 0f);

            Vector3 front_of_car = current_position + (length-0.1f)/2*transform.forward.normalized;

            Vector3 perpendicular_left = Vector3.Cross(transform.forward, transform.up).normalized;
            Vector3 front_left = front_of_car + width/2*perpendicular_left;

            bool ray = Physics.Linecast(front_left, next_position);
            return ray;

        }

        private bool steering_correction_right(int next)
        {
            Vector3 next_position = verbose_path[next];

            float length = (4.47f - 0.1f);
            float width = (2.43f - 0.1f);

            Vector3 current_position = transform.position + new Vector3(0f, 4f, 0f);


            Vector3 front_of_car = current_position + length / 2 * transform.forward.normalized;

            Vector3 perpendicular_right = Vector3.Cross(transform.up, transform.forward).normalized;
            Vector3 front_right = front_of_car + width / 2 * perpendicular_right;

            bool ray = Physics.Linecast(front_right, next_position);
            return ray;

        }

        //The idea of this is to steer towards the corner of the wall that is blocking your way
        //from which both you and the next goal are visible (so that you steer around the wall)
        private Vector3 steer_around(int next_index)
        {
            Vector3 next_point = verbose_path[next_index];

            Vector3 direction = next_point - transform.position;

            Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit);

            Vector3 boxCenter = hit.collider.transform.position;

            Vector3 buffer_point = new Vector3();

            TerrainInfo myInfo = terrain_manager.myInfo;
            float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
            float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N;

            //iterate through corners
            float margin = visibility.get_margin();
            Vector3 sw = new Vector3(boxCenter.x - x_step - margin, 0f, boxCenter.z - z_step - margin);
            Vector3 se = new Vector3(boxCenter.x + x_step + margin, 0f, boxCenter.z - z_step - margin);
            Vector3 nw = new Vector3(boxCenter.x - x_step - margin, 0f, boxCenter.z + z_step + margin);
            Vector3 ne = new Vector3(boxCenter.x + x_step + margin, 0f, boxCenter.z + z_step + margin);

            List<Vector3> check_points = new List<Vector3>();

            check_points.Add(sw);
            check_points.Add(se);
            check_points.Add(nw);
            check_points.Add(ne);

            foreach (Vector3 point in check_points)
            {
                Debug.DrawLine(transform.position, point, Color.cyan,0.1f);
                if (!Physics.Linecast(transform.position,point) && !Physics.Linecast(next_point, point))
                {
                    
                    buffer_point = point;
                    break;
                }
            }

            return buffer_point;
        }

        private float steering_correction() {
            float length = 4.47f;
            Vector3 front_of_car = transform.position + new Vector3(0f,4f,0f) + (length / 2f) * transform.forward.normalized;        
            float[] lidar_rays = new float[] {-45, -30, -15, 15, 30, 45};
            float lidar_range = 20f;
            float tot = 0;
            Debug.Log("Front of car " + front_of_car.ToString());
            foreach (float lidar in lidar_rays) {
                Vector3 dir = Quaternion.Euler(0, lidar, 0) * transform.forward;
                Physics.Raycast(front_of_car, dir.normalized, out RaycastHit hit, lidar_range);
                if (hit.distance>0)
                {
                    tot += hit.distance * Mathf.Sign(lidar);
                }
                else
                {
                    tot += lidar_range * Mathf.Sign(lidar);
                }
                
                Debug.DrawLine(front_of_car, front_of_car + hit.distance * dir.normalized, Color.cyan);
            }
            if (tot == 0) {
                return 0;
            }
            return tot > 0 ? 1f : -1f;
        }

        private void FixedUpdate()
        {

            if (!path_initialized)
            {
                visibility = visibility_game_object.GetComponent<VisibilityGraph>();
                visibility_graph = visibility.visibility_corners;
                compute_paths(visibility.geneticTSP.GetBest());
            }

            //update what you have seen thus far
            inspect();

            

            // Execute your path here
            // ...
            


            Vector3 next_pos = verbose_path[next_index];

            if (Vector3.Distance(next_pos, transform.position) < 20f && check_for_line(next_index+1))
            {
                next_index += 1;
                next_pos = verbose_path[next_index];
            }


            if (steering_correction_left(next_index) || steering_correction_right(next_index))
            {
                //steering += 0.2f;
                //next_pos = steer_around(next_index);
            }

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
                    crash_correction_timer = 3f; //reset the correction timer
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

            /*
            if(steering_correction_left(next_index) || steering_correction_right(next_index))
            {
                //steering += 0.2f;
                steering += steering_correction();
            }
            */
            if (Mathf.Abs(steering) < 0.1f) //We are 'close enough' to the correct movement
            {
                //if it is too small, don't do it
                steering = 0f;
            }

            m_Car.Move(steering, acceleration, acceleration, 0f);

        }
    }
}
