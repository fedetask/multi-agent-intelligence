using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI5 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        public float leader_speed;
        public GameObject[] friends;
        public GameObject[] enemies;
        public float acceleration_coefficient;
        public int id; // Identifies the car in the formation
        public GameObject formation_game_object;
        float time_buffer = 0.2f;
        float timer = 0f;
        float crash_timer = 0f;
        
        private List<Vector3> path_verbose = new List<Vector3>();
        bool initialized = false;
        private formation formation;
        public GameObject A_star_object;
        private AStar a_star;
        public List<Vector3> path = new List<Vector3>();
        public float current_speed;
        int path_counter = 0;
        public float brake;
        public float acceleration;
        bool crashed = false;
        public bool isLeader = false;
        public bool is_on_right;
        public bool is_on_left;
        private void Start()
        {

            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            a_star = A_star_object.GetComponent<AStar>();
            // Get the formtion object
            formation = formation_game_object.GetComponent<formation>();
            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            // Plan your path here
            // ...
        }

        public void set_id(int agent_id)
        {
            id = agent_id;
            Debug.Log("Setting my id as " + id);
        }



        private bool add_to_path(List<Vector3> verbose, List<Vector3> path)
        {
            Vector3 current_pos = path[path.Count - 1];

            
            Vector3 ideal_next_pos = verbose[verbose.Count - 1];
            float margin = 4f;
            Vector3 direction = ideal_next_pos - current_pos;
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
                        Debug.Log("We hit a " + rayhit.collider.name);
                    }
                }
            }
            return !free;
        }

        Vector3 target_pos;
        Vector3 target_velocity;
        Vector3 car_pos;
        Vector3 car_velocity;


       

        private void FixedUpdate()
        {
            if(a_star.initialized)
            {
                if (!initialized)
                {
                    initialized = true;
                    if (isLeader)
                    { path = a_star.path; }
                }
                if (!isLeader)
                {
                    non_leader_move();
                }
                else
                {

                    current_speed = m_Car.CurrentSpeed;
                    Vector3 next_pos = path[path_counter];

                    if (Vector3.Distance(transform.position, next_pos) < 5f)
                    {
                        path_counter += 1;
                        next_pos = path[path_counter];
                    }

                    Debug.DrawLine(transform.position, next_pos, Color.white, 0.1f);
                    leader_speed = formation.leader_speed;
                    Vector3 direction = (next_pos - transform.position).normalized;
                    bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
                    bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

                    float steering = 0f;
                    acceleration = 0;
                    Vector3 current_direction = transform.forward.normalized;

                    float direction_angle = Vector3.Angle(current_direction, direction) * Mathf.Sign(-current_direction.x * direction.z + current_direction.z * direction.x);


                    brake = 0f;
                    if (is_to_the_right && is_to_the_front)
                    {
                        steering = 1f;
                        acceleration = 1f;
                    }
                    else if (is_to_the_right && !is_to_the_front)
                    {
                        //steering = -1f;
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
                        steering = 1f; //it was 1f
                        acceleration = -1f;
                    }

                    //acceleration *= Mathf.Clamp(acceleration_coefficient * Vector3.Distance(transform.position, next_pos), 0, 1);
                    steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle);// Mathf.Sign(acceleration);

                    if(m_Car.CurrentSpeed>10f)
                    {
                        acceleration = 0f;
                        
                    }
                    else
                    {
                        brake = 0f;
                      
                    }
                    m_Car.Move(steering, acceleration, acceleration, 0);

                }
            }
            
            
        }


        private void non_leader_move()
        {
           
            if (target_pos == null)
            {
                target_pos = formation.get_next_position(id);
                car_pos = transform.position;
            }
            target_velocity = (formation.get_next_position(id) - target_pos) / Time.deltaTime;
            car_velocity = (transform.position - car_pos) / Time.deltaTime;
            target_pos = formation.get_next_position(id);
            car_pos = transform.position;
            current_speed = m_Car.CurrentSpeed;
            timer += Time.deltaTime;
            // Execute your path here
            // ...
            path_verbose.Add(formation.get_next_position(id));
            if (path.Count > 0)
            {
                if (Vector3.Distance(formation.get_next_position(id), path[path.Count - 1]) > 10f)
                {
                    path.Add(formation.get_next_position(id));
                }
            }
            if (path.Count == 0)
            {
                path.Add(formation.get_next_position(id));
            }

            Vector3 next_pos = get_next_on_path();//get_next_position(id); // formation.get_next_position(id);

            Debug.DrawLine(transform.position, next_pos, Color.white, 0.1f);
            leader_speed = formation.leader_speed;
            Vector3 direction = (next_pos - transform.position).normalized;
            bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

            float steering = 0f;
            acceleration = 0;
            Vector3 current_direction = transform.forward.normalized;

            float direction_angle = Vector3.Angle(current_direction, direction) * Mathf.Sign(-current_direction.x * direction.z + current_direction.z * direction.x);


            brake = 0f;
            if (is_to_the_right && is_to_the_front)
            {
                steering = 1f;
                acceleration = 1f;
            }
            else if (is_to_the_right && !is_to_the_front)
            {
                //steering = -1f;
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
                steering = 1f; //it was 1f
                acceleration = -1f;
            }

            //acceleration *= Mathf.Clamp(acceleration_coefficient * Vector3.Distance(transform.position, next_pos), 0, 1);
            steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle);// * Mathf.Sign(acceleration);
            


            //if (m_Car.CurrentSpeed > 20f + Vector3.Distance(transform.position, next_pos))
            //{
            //    brake = 1;
            //}

            //if (Mathf.Abs(steering) < 0.2f)
            //{ steering = 0; }

            //Vector3 relative_pos = formation.leader_car.transform.InverseTransformPoint(transform.position);
            //Vector3 relative_pos = formation.previous_position.InverseTransformPoint(transform.position);
            //if (timer >= time_buffer && Vector3.Dot(transform.position - formation.leader_car.transform.position, formation.leader_orientation) >= 0)
            //{
            //    brake = 1f;
            //    Debug.DrawLine(transform.position, formation.leader_car.transform.position, Color.yellow, 0.1f);
            //    Debug.DrawLine(formation.leader_car.transform.position, formation.leader_car.transform.position + 100f * formation.leader_orientation, Color.yellow, 0.1f);
            //}
            //if(timer >= time_buffer && Vector3.Dot(direction, transform.forward)<0f)
            //if (timer >= time_buffer && relative_pos.z >=0 )

            // this is how you control the car

            //m_Car.Move(0f, -1f, 1f, 0f);

            float speed_ratio = car_velocity.magnitude / target_velocity.magnitude;
            float dist = Vector3.Distance(transform.position, formation.get_next_position(id));
            if (formation.leader_car.transform.InverseTransformDirection(transform.position - formation.leader_car.transform.position).z > 0)
            {
                dist *= -1;
            }
            float offset = 1;
            float ratio_max = 1.5f;

            float modifier;
            if (dist > 0)
            {
                modifier = Mathf.Atan(100 * (dist - offset)) / (0.5f * Mathf.PI);
                //modifier += Mathf.Atan(10 * (-speed_ratio + 2)) / (0.5f * Mathf.PI);
                //modifier /= 1;
                Debug.DrawLine(transform.position, target_pos, Color.black, 0.1f);
            }
            else
            {
                modifier = 0;
            }
            acceleration *= modifier;
            if (m_Car.CurrentSpeed > 15f)
            { acceleration = 0f; }
            if (dist < 0)
            {
                Vector3 leader_forward = formation.leader_car.transform.forward;
                float angle = Vector3.Angle(transform.forward, leader_forward) * Mathf.Sign(-transform.forward.x * leader_forward.z + transform.forward.z * leader_forward.x);
                steering = angle;
                brake = 1f;
            }
            else if(current_speed<0.2f) //we hit a wall
            {

                if (crashed == false)
                {
                    if (has_crashed())
                    {
                        crashed = true;
                        crash_timer = 0f;
                    }
                }
                else
                {
                    crash_timer += Time.deltaTime;
                    Debug.Log("Crashed, oops");
                    Debug.Log(crash_timer);
                    if (crash_timer<time_buffer)
                    {
                        
                        acceleration = -0.2f;
                        steering = 0f;
                        brake = 0;
                        m_Car.Move(steering, acceleration, acceleration, brake);
                    }
                    else
                    {
                        Debug.Log("not anymore!");
                        crashed = false; 
                    }
                }
               
            }
           

            
            if (timer > 1 && crashed==false)
            {
                m_Car.Move(steering, acceleration, acceleration, brake);
            }
            
           
        }

        private bool has_crashed()
        {
            //var mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            var mask = 1 << LayerMask.NameToLayer("CubeWalls");
            Vector3 perpendicular = Vector3.Cross(transform.forward, Vector3.up).normalized;
            Vector3 left = transform.position;// + perpendicular * 2;
            bool ray1 = Physics.Raycast(left + new Vector3(0f, 3f, 0f), transform.forward*4, out RaycastHit hit1,mask);
            Debug.DrawLine(left+new Vector3(0f,3f,0f), left + new Vector3(0f,3f,0f) + transform.forward * 4, Color.cyan, 0.1f);
            if(ray1)
            {
                Debug.Log("Hit a hascrashed " + hit1.collider);
                if (hit1.distance < 5)
                { return true; }
            }
            return false;
        }
        private Vector3 get_next_on_path()
        {
            Vector3 result = new Vector3();
            Vector3 current_pos = transform.position;


            Vector3 ideal_next_pos = formation.get_next_position(id);
            float margin = 4f;
            Vector3 direction = ideal_next_pos - current_pos;
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
                        Debug.Log("We hit a " + rayhit.collider.name);
                    }
                }
            }

            if (free)
            {
                return ideal_next_pos;
            }

            //return path[path.Count - 1];
            for (int i = path.Count - 1; i >= 0; i--)
            {


                ideal_next_pos = path[i];
                margin = 4f;
                direction = ideal_next_pos - current_pos;
                normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                step = (margin - 0.1f) / 2;
                signs = new int[] { -1, 0, 1 };
                free = true;
                mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
                foreach (int sign in signs)
                {
                    if (Physics.Linecast(current_pos + sign * step * normal, ideal_next_pos + sign * step * normal, out RaycastHit rayhit, mask))
                    {
                        free = false;
                        if (free == false)
                        {
                            Debug.Log("We hit a " + rayhit.collider.name);
                        }
                    }
                }

                if (free)
                {
                    result = ideal_next_pos;
                    break;
                }
            }
            return result;
        }
        Vector3 get_next_position(int id)
        {
            Vector3 current_pos = transform.position;

            Vector3 ideal_next_pos = formation.get_next_position(id);
            float margin = 4f;
            Vector3 direction = ideal_next_pos - current_pos;
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
                        Debug.Log("We hit a " + rayhit.collider.name);
                    }
                }
            }
            if (!free)
            {

                return formation.leader_car.transform.position;

            }
            else
            { return ideal_next_pos; }
        }
    }
}
