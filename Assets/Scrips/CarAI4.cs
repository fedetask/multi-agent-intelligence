using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI4 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        public float leader_speed;
        public GameObject[] friends;
        public GameObject[] enemies;
        public float acceleration_coefficient;
        private int id; // Identifies the car in the formation
        public GameObject formation_game_object;
        
        private formation formation;

        private void Start()
        {
            
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

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
            Debug.Log("Setting my id as "+id);
        }
        private void FixedUpdate()
        {


            // Execute your path here
            // ...

            Vector3 next_pos = formation.get_next_position(id);

            Debug.DrawLine(transform.position, next_pos, Color.white, 0.1f);
            leader_speed = formation.leader_speed;
            Vector3 direction = (next_pos - transform.position).normalized;
            bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

            float steering = 0f;
            float acceleration = 0;
            Vector3 current_direction = transform.forward.normalized;

            float direction_angle = Vector3.Angle(current_direction, direction) * Mathf.Sign(-current_direction.x * direction.z + current_direction.z * direction.x);

            float brake = 0f;
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
            
                acceleration *= Mathf.Clamp(acceleration_coefficient * Vector3.Distance(transform.position, next_pos), 0, 1);
                //steering = Mathf.Clamp(direction_angle, -m_Car.m_MaximumSteerAngle, m_Car.m_MaximumSteerAngle) * Mathf.Sign(acceleration);
           

            
            if(m_Car.CurrentSpeed>30f+ Vector3.Distance(transform.position,next_pos))
            {
                brake = 1;
            }
           
            if (Mathf.Abs(steering) < 0.2f)
            { steering = 0; }

            //if(Vector3.Dot(transform.position- formation.leader_car.transform.position,formation.leader_orientation)>=0)
            //{
            //    brake = 1;
            //}

           

            // this is how you control the car
            m_Car.Move(steering, acceleration, acceleration, brake);
            //m_Car.Move(0f, -1f, 1f, 0f);


        }
    }
}
