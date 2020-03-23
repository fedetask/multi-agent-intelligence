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
        public int id;  // Identifies the car in the formation
        public GameObject formation_game_object;
        float time_buffer =2f;
        float timer = 0f;
        public float crash_timer = 0f;
        bool crashed = false;
        private List<Vector3> path_verbose = new List<Vector3>();
        bool initialized = false;
        private formation formation;
        public GameObject A_star_object;
        private AStar a_star;
        public List<Vector3> path = new List<Vector3>();
        public float current_speed;
        public int path_counter = 0;
        public float brake;
        public float acceleration;
        public bool isLeader = false;
        public bool is_on_right;
        public bool is_on_left;
        private float crash_correction_timer = 2f;
        bool leader_mess = false;
        public float offset = 1;
        public float dist;
        private float attempt_timer = 2f;


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
                        //Debug.Log("We hit a " + rayhit.collider.name);
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
            if(a_star.initialized && formation.initialized)
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

                    if (Vector3.Distance(transform.position, next_pos) <7f)
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
                    steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle)*Mathf.Sign(acceleration);

                    GameObject[] friends = GameObject.FindGameObjectsWithTag("Player");
                    float max_distance = float.MinValue;
                    GameObject max_distance_obj = new GameObject();
                    foreach(GameObject obj in friends)
                    {
                        CarAI5 script = obj.GetComponent<CarAI5>();

                        if(script.isLeader)
                        { continue; }

                        int x1 = terrain_manager.myInfo.get_i_index(path[path_counter].x);
                        int z1 = terrain_manager.myInfo.get_i_index(path[path_counter].z);
                        int x2 = terrain_manager.myInfo.get_i_index(path[path_counter + 1].x);
                        int z2 = terrain_manager.myInfo.get_i_index(path[path_counter + 1].z);
                        float[,] evaluation_matrix = a_star.terrain_evaluator_object.GetComponent<TerrainEvaluator>().evaluation_matrix;
                        if (evaluation_matrix[x1, z1] == 0f && evaluation_matrix[x2, z2] == 0f)
                        {
                            script.offset = 1;
                        }
                        else
                        { 
                            script.offset = 0; 
                            if(formation.number_of_agents==4)
                            {
                                script.offset = -1;
                            }
                        }


                        //float distance = Vector3.Distance(transform.position, obj.transform.position);
                        float distance = script.dist;
                        if(distance>max_distance && Vector3.Dot(transform.forward,obj.transform.position - transform.position)<0)
                        {
                            max_distance_obj = obj;
                            max_distance = distance;
                        }

                    }
                    Debug.DrawLine(transform.position, max_distance_obj.transform.position, Color.red, 0.1f);

                   
                    float speed_limit=8f;
                    /*
                    if(formation.total_number_of_enemies==5)
                    {
                        speed_limit = 20f;
                    }
                    else
                    {
                        speed_limit = 10f;
                    }
                    */

                    if (m_Car.CurrentSpeed > speed_limit || max_distance > 7f)
                    {
                        acceleration = 0f;
                        if(max_distance>7f)
                        {
                            crash_timer = 0f;
                        }
                    }
                    else
                    {
                        brake = 0f;
                      
                    }

                    if ((current_speed < 1f) && acceleration != 0) //|| (formation.leader_car.GetComponent<CarAI5>().current_speed<1f && formation.leader_car.GetComponent<CarAI5>().acceleration!=0)) //If we are going really slow, we very likely crashed -> start counting!
                    {

                        crash_timer += Time.deltaTime; //start counting

                        if (crash_timer > time_buffer) //if we are going slow for a long enough time
                        {
                            crashed = true; //we crashed
                        }

                    }
                    else
                    {
                        crash_timer = 0f; //else, all cool/false alarm, reset the timer
                        leader_mess = false;
                    }

                    if (crashed) //If we crashed, time to correct it
                    {
                        crash_correction_timer -= Time.deltaTime; //start counting down
                        acceleration = -acceleration; //Obviously, what we are doing atm does not work: try going the opposite way!
                        steering = 0f;
                        if (crash_correction_timer <= 0) //after we corrected for enough time
                        {
                            crash_correction_timer = 2f; //reset the correction timer
                            crashed = false; //we are no longer in the crashing phase
                            crash_timer = 0f;
                        }
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
                if (Vector3.Distance(formation.get_next_position(id), path[path.Count - 1]) > 5f)
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
            /*
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
            */
            
            acceleration = 1;

            //acceleration *= Mathf.Clamp(acceleration_coefficient * Vector3.Distance(transform.position, next_pos), 0, 1);
            steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle) * Mathf.Sign(acceleration);
            
            float speed_ratio = car_velocity.magnitude / target_velocity.magnitude;
            dist = Vector3.Distance(transform.position, formation.get_next_position(id));
            if (formation.leader_car.transform.InverseTransformDirection(transform.position - formation.leader_car.transform.position).z > 0)
            {
                dist *= -1;
            }
            List<GameObject> enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
            /*
            GameObject target = new GameObject();
            foreach(GameObject enemy in enemies)
            {
                int final_i = terrain_manager.myInfo.get_i_index(formation.leader_car.GetComponent<CarAI5>().path[path.Count - 1].x);
                int final_j = terrain_manager.myInfo.get_j_index(formation.leader_car.GetComponent<CarAI5>().path[path.Count - 1].z);
                if (terrain_manager.myInfo.get_i_index(enemy.transform.position.x) == final_i && terrain_manager.myInfo.get_j_index(enemy.transform.position.z) == final_j)
                {
                    target = enemy;
                    break;
                }
            }
            float offset = find_first_visible(target.transform.position, path);
            */

            float ratio_max = 1.5f;

            float modifier;

            float speed_limit=10f;
            
            if (formation.total_number_of_enemies == 5)
            {
                if(id==0)
                {
                    offset = 8f;
                }
            }
            else
            {
            }
            

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



            float initial_accel = acceleration;
            if (m_Car.CurrentSpeed > speed_limit)
            { acceleration = 0f; }

            //if(formation.leader_car.GetComponent<CarAI5>().crashed==false)
            //{
            if (crashed) //If we crashed, time to correct it
            {
               
                    crash_correction_timer -= Time.deltaTime; //start counting down
                    acceleration = -acceleration; //Obviously, what we are doing atm does not work: try going the opposite way!
                    steering = 0f;
                    if (crash_correction_timer <= 0) //after we corrected for enough time
                    {
                        crash_correction_timer = 10f; //reset the correction timer
                        crashed = false; //we are no longer in the crashing phase
                        crash_timer = 0f;
                    }
                
            }



            if (dist < 0)// && leader_mess == false)
            {
                Vector3 leader_forward = formation.leader_car.transform.forward;
                float angle = Vector3.Angle(transform.forward, leader_forward) * Mathf.Sign(-transform.forward.x * leader_forward.z + transform.forward.z * leader_forward.x);
                steering = angle;
                crash_timer = 0f;
                crashed = false;
                brake = 1f;
            }
            else if (current_speed < 1f && dist- offset >3f && formation.leader_car.GetComponent<CarAI5>().acceleration == 0f)// && formation.leader_car.GetComponent<CarAI5>().acceleration == 0f)) //|| (formation.leader_car.GetComponent<CarAI5>().current_speed<1f && formation.leader_car.GetComponent<CarAI5>().acceleration!=0)) //If we are going really slow, we very likely crashed -> start counting!
            {   
                crash_timer += Time.deltaTime; //start counting
                crashed = false;
                if (crash_timer > time_buffer) //if we are going slow for a long enough time
                    {
                        crashed = true; //we crashed
                    }
            }
            else
            {
                crash_timer = 0f;
            }
           
            if (timer > 1)
            {
                m_Car.Move(steering, acceleration, acceleration, brake);
            }
            
           
        }

       
        private float find_first_visible(Vector3 target_pos, List<Vector3> path)
        {
            int i;
            Vector3 direction = new Vector3() ;
            for (i=0; i<path.Count; i++)
            {
                Vector3 ideal_next_pos = target_pos;
                Vector3 current_pos = formation.leader_car.GetComponent<CarAI5>().path[i];
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

            Debug.DrawLine(formation.leader_car.transform.position, formation.leader_car.transform.position + 10 * direction.normalized, Color.red, 0.1f); //left
            Debug.DrawLine(formation.leader_car.transform.position, formation.leader_car.transform.position - 10 * direction.normalized, Color.red, 0.1f); //right
            float offset = 1f;
            if(id==0) // left
            {
                Vector3 left_curved = formation.leader_car.transform.position + 10 * direction.normalized;
                Vector3 left_perp = formation.agent_positions[0] - formation.leader_car.transform.position;
                float angle = Vector3.Angle(left_curved, left_curved);
                offset = Mathf.Tan(Mathf.Deg2Rad*angle) * left_perp.magnitude;
            }
            else
            {
                Vector3 right_curved = formation.leader_car.transform.position - 10 * direction.normalized;
                Vector3 right_perp = formation.agent_positions[1] - formation.leader_car.transform.position;
                float angle = Vector3.Angle(right_curved, right_curved);
                offset = Mathf.Tan(Mathf.Deg2Rad * angle) * right_perp.magnitude;
            }
            return offset;


            
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
                        //Debug.Log("We hit a " + rayhit.collider.name);
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
                            //Debug.Log("We hit a " + rayhit.collider.name);
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
                        //Debug.Log("We hit a " + rayhit.collider.name);
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
