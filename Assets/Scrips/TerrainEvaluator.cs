using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityStandardAssets.Vehicles.Car
{

    
    public class TerrainEvaluator : MonoBehaviour
    {
        public GameObject terrain_Manager_object;
        private TerrainManager terrain_manager;
        private float[,] traversability_matrix;
        public float[,] evaluation_matrix;
        public GameObject game_manager_object;
        private GameManager game_manager;
        public List<Vector3> turret_list_location;
        // Start is called before the first frame update
        void Start()
        {
            terrain_manager = terrain_Manager_object.GetComponent<TerrainManager>();
            traversability_matrix = terrain_manager.myInfo.traversability;
            game_manager = game_manager_object.GetComponent<GameManager>();
            List<GameObject> turret_list = game_manager.turret_list;
            turret_list_location = new List<Vector3>();
            foreach(GameObject tur in turret_list)
            {
                turret_list_location.Add(tur.transform.position);
            }

            evaluation_matrix = evaluate_board(traversability_matrix, turret_list_location);
            for (int i = 0; i < traversability_matrix.GetLength(0); i++)
            {
                for (int j = 0; j < traversability_matrix.GetLength(1); j++)
                {
                    //Debug.Log("At (" + i + ", " + j +") : " +  evaluation_matrix[i,j]);
                }
            }

                   
        }



        // Update is called once per frame
        void Update()
        {

        }

        public float[,] evaluate_board(float[,] traversability_matrix, List<Vector3> turrets)
        {
            float[,] result = new float[traversability_matrix.GetLength(0), traversability_matrix.GetLength(1)];
            for(int i =0; i <traversability_matrix.GetLength(0); i++)
            {
                for (int j=0; j< traversability_matrix.GetLength(1); j++)
                {
                    if (traversability_matrix[i,j]==1f)
                    {
                        result[i,j] = float.MaxValue; // go through walls :)
                    }
                    else
                    {
                        result[i, j] = number_of_turrets(i, j, turrets);
                    }
                }
            }
            return result;
        }

        //if there exists a line of sight, return true.
        public bool line_of_sight(Vector3 current_location, Vector3 turret_location)
        {
           
            float margin = 4f;
            Vector3 direction = turret_location - current_location;
            Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
            float step = (margin - 0.1f) / 2;
            int[] signs = new int[] { -1, 0, 1 };
            bool free = false;
            var mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            foreach (int sign in signs)
            {
                if (!Physics.Linecast(current_location + sign * step * normal, turret_location + sign * step * normal, out RaycastHit rayhit, mask))
                {
                    free = true;
                    
                    break;
                    
                }
                
            }
            return free;
        }

        private float number_of_turrets(int x_coord, int z_coord, List<Vector3> turrets)
        {
            Vector3 current_location = new Vector3(terrain_manager.myInfo.get_x_pos(x_coord), 0f, terrain_manager.myInfo.get_z_pos(z_coord));
            float danger = 0f;
            foreach(Vector3 location in turrets)
            {
               
                if(line_of_sight(current_location,location))
                {
                    if (x_coord == 1 && z_coord == 10) // change indeces here if you wanna check something (draw what it sees)
                    {
                        Debug.DrawLine(current_location, location, Color.cyan, 10f);
                    }
                    danger += 1;
                }
            }
            return Mathf.Pow(2,danger);
        }
    }



}
