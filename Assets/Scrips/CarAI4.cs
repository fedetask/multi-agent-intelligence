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

        Vector3 target_pos;
        Vector3 target_velocity;
        Vector3 car_pos;
        Vector3 car_velocity;
        float timer = 0;
        private void FixedUpdate()
        {
            timer += Time.deltaTime;
            if (target_pos == null) {
                target_pos = formation.get_next_position(id);
                car_pos = transform.position;
            }
            target_velocity = (formation.get_next_position(id) - target_pos) / Time.deltaTime;
            car_velocity = (transform.position - car_pos) / Time.deltaTime;
            target_pos = formation.get_next_position(id);
            car_pos = transform.position;

            Debug.DrawLine(transform.position, target_pos, Color.white, 0.1f);
            leader_speed = formation.leader_speed;
            Vector3 direction = (target_pos - transform.position).normalized;
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

            float speed_ratio = car_velocity.magnitude / target_velocity.magnitude;
            float dist = Vector3.Distance(transform.position, formation.get_next_position(id));
            if (formation.leader_car.transform.InverseTransformDirection(transform.position - formation.leader_car.transform.position).z > 0){
                dist *= -1;
            }
            float offset = 20;
            float ratio_max = 1.5f;

            float modifier;
            if (dist > 0) {
                modifier = Mathf.Atan(100 * (dist - offset)) / (0.5f * Mathf.PI);
                Debug.DrawLine(transform.position, target_pos, Color.black, 0.1f);
            } else {
                modifier = 0;
            }
            acceleration *= modifier;
            
            if (dist < 0) {
                Vector3 leader_forward = formation.leader_car.transform.forward;
                float angle = Vector3.Angle(transform.forward, leader_forward) * Mathf.Sign(-transform.forward.x * leader_forward.z + transform.forward.z * leader_forward.x);
                steering = angle;
            }

            Debug.Log("ACC MODIFIER "+modifier);
            if (timer > 2) {
                m_Car.Move(steering, acceleration, acceleration, brake);
            }
        }
    }
}
