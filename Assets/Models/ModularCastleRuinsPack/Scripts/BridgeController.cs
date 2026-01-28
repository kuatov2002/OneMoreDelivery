using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BridgeController : MonoBehaviour
{

    public List<GameObject> bridge;
    public GameObject support;

    int previowsSize = 0;
    public int size = 0;
    public int height = 1;
    bool updatingSize = false;

    void Start(){
        previowsSize = size;
    }

    public void updateHeight(int increment){
        UnpackPrefab();
        height+=increment;
        foreach(Transform child in transform){
            setHeight(child.gameObject);
        }
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
            updateHeight(0);
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
        GameObject newObj = Instantiate(bridge[Random.Range(0,bridge.Count)], new Vector3(0,((height+1)*3.0f),0),transform.rotation, transform);
        newObj.transform.localPosition = new Vector3(0,((height-1)*3.0f),((straightPiecesCount())*6.0f));
        if(Random.Range(0.0f,1.0f) > 0.5f){
            newObj.transform.localRotation = Quaternion.Euler(0,180,0);
        }
        setHeight(newObj);
    }

    void setHeight(GameObject obj){
        if(obj.transform.childCount-2 != height-1){
            for(int i = obj.transform.childCount - 3; i >= 0; i--){
                if(!obj.transform.GetChild(i).name.Contains("-lod")){
                    DestroyImmediate(obj.transform.GetChild(i).gameObject);
                }
            }
            obj.transform.localPosition = new Vector3(obj.transform.localPosition.x,((height-1)*3),obj.transform.localPosition.z);

            if(height > 1){
                for(int i = 1; i < height; i++){
                    GameObject newObj = Instantiate(support, new Vector3(0,0,0),transform.rotation, obj.transform);
                    newObj.transform.localPosition = new Vector3(0,-(i*3f),0);
                }
            }
        }
    }

    void UnpackPrefab(){
        if(PrefabUtility.IsPartOfAnyPrefab(transform.gameObject)){
            PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
    }
}
