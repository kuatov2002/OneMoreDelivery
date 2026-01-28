using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WallController : MonoBehaviour
{

    public List<GameObject> wall;
    public List<GameObject> tower;

    int previowsSize = 0;
    public float wallSize = 6.0f;
    public float towerSize = 3.0f;
    public int size = 0;
    bool updatingSize = false;

    void Start(){
        previowsSize = size;
    }

    
    public void setSize(){
        UnpackPrefab();
        if(!updatingSize){
            updatingSize = true;
            if(size > 0){
                do{
                    if(size > straightPiecesCount()){
                        increaseSize(false);
                    }else if(size < straightPiecesCount()){
                        decreaseSize(false);
                    }
                }while(size != straightPiecesCount());
            }
            updatingSize = false;
        }
    }

    public int straightPiecesCount(){
        int count = 0;
        foreach(Transform t in transform){
            count++;
        }
        return count;
    }

    public void increaseSize(bool updateSize){
        updatingSize = false;
        if(updateSize){
            size++;
        }
        
        createStraightPiece();
    }

    public void decreaseSize(bool updateSize){
        updatingSize = false;
        if(straightPiecesCount() > 0){
            if(updateSize){
                size--;
            }
            Transform last = null;
            foreach(Transform t in transform){
                if(last == null){
                    last = t;
                }else{
                    if(t.localPosition.z > last.localPosition.z){
                        last = t;
                    }
                }
            }
            if(last != null){
                DestroyImmediate(last.gameObject);
            }
        }
    }

    void createStraightPiece(){
        GameObject newObj = Instantiate(wall[Random.Range(0,wall.Count)], new Vector3(0,0,0),transform.rotation, transform);
        
        if(tower != null && tower.Count > 0){
            float wallZPos = ((straightPiecesCount())*(wallSize+towerSize));
            newObj.transform.localPosition = new Vector3(0,0,wallZPos);
            GameObject newtower = Instantiate(tower[Random.Range(0,tower.Count)], new Vector3(0,0,0),transform.rotation, newObj.transform);
            newtower.transform.localPosition = new Vector3(0,0,wallSize-(towerSize/2));
        }else{
            newObj.transform.localPosition = new Vector3(0,0,((straightPiecesCount())*wallSize));
        }
    }

    

    void UnpackPrefab(){
        if(PrefabUtility.IsPartOfAnyPrefab(transform.gameObject)){
            PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
    }
}
