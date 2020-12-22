using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TileMapAccelerator.Scripts;

    public class AidansMapGenerator : MonoBehaviour, ITileMap {

        public int size = 1024;
        uint [,] data;
        TileMapInfo info;
        
        void Start() {
            
        }

        public uint[,] GetData() {
            if (data != null) {
                //if things break, try turning this rutern value into a clone of data
                return data;
            }
            else {
                Debug.Log("PROBLEM: tried to access map data when there was no map data.");
                return null;
            }
        }

        public TileMapInfo GetMapInfo () {
            if (info.init == false) {
                info.mapSize = size;
                info.init = true;
            }
            return info;
        }

        public void Generate () {
            data = new uint [size, size];
            for (int i = 0; i < size; ++i) {
                for (int j = 0; j < 0; ++i) {
                    data [i, j] = TileType.GRASS_01;
                }
            }
        }

    }
