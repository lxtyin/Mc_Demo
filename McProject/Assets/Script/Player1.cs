using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class Player1 : MonoBehaviour {

    Rigidbody mybody;
    BoxCollider mycollider;
    public Camera myeye;
    public float myHorizontalRotation, myVerticalRotation;//用水平转角+垂直转角表示面向
    public float Vspeed;//垂直速度

    private bool onSky = true;

    public void Start() {
        mybody = GetComponent<Rigidbody>();
        mycollider = GetComponent<BoxCollider>();
        myHorizontalRotation = 0;
        myVerticalRotation = 0;
        mybody.position = Global.BothPosition;
        Global.playerInfo = new UserInfo(Global.playerName, mybody.position, 0);
    }

    void RotationMove() {
        myHorizontalRotation += Input.GetAxis("Mouse X") * 2;
        float myv = myVerticalRotation + Input.GetAxis("Mouse Y") * 2;
        myv = Mathf.Min(90, Mathf.Max(-90, myv));//垂直转角约束
        myVerticalRotation = myv;

        myeye.transform.eulerAngles = new Vector3(-myVerticalRotation, myHorizontalRotation, 0);
    }

    void MoveH(Vector3 dir) {//带碰撞的水平移动
        if(!Physics.BoxCast(mybody.position, mycollider.size / 2, Vector3.right * dir.x, mybody.rotation, 2 * Mathf.Abs(dir.x))) {
            mybody.position += Vector3.right * dir.x;
        }
        if(!Physics.BoxCast(mybody.position, mycollider.size / 2, Vector3.forward * dir.z, mybody.rotation, 2 * Mathf.Abs(dir.z))) {
            mybody.position += Vector3.forward * dir.z;
        }
    }
    void MoveV(Vector3 dir) {//带碰撞的垂直移动
        if(!Physics.BoxCast(mybody.position, mycollider.size / 2, Vector3.up * dir.y, mybody.rotation, 2 * Mathf.Abs(dir.y))) {
            mybody.position += Vector3.up * dir.y;
            if(mybody.position.y < -100) {
                Title.ins.ShowWarn("你掉出了这个世界");
                Title.ins.BackToTitle();
            }
        } else if(dir.y < 0) {
            onSky = false;
            Vspeed = 0;
        }
    }

    private void Update() {
        sendUserInfo();
        if (Global.pause) return;

        RotationMove();
        myeye.transform.position = mybody.position + new Vector3(0, 0.7f, 0);

        Vector3 moveDir = new Vector3(0, 0, 0);
        if(Input.GetKey(KeyCode.W)) moveDir.z = 1;
        if(Input.GetKey(KeyCode.S)) moveDir.z = -1;
        if(Input.GetKey(KeyCode.A)) moveDir.x = -1;
        if(Input.GetKey(KeyCode.D)) moveDir.x = 1;
        if(moveDir.magnitude > 0.1f) {
            moveDir = Math.getHVector(Math.getHRotation(moveDir) + myHorizontalRotation);
            MoveH(moveDir * Time.deltaTime * 6);
        }

        if(Input.GetKeyDown(KeyCode.Space) && !onSky) {
            Vspeed = 7;
            onSky = true;
        }
        if(!Physics.Raycast(mybody.position, Vector3.down, 1)) {//不断检测脚下有无方块
            onSky = true;
        }
        if(onSky) {
            Vspeed -= 17 * Time.deltaTime;//坠落的同时具有重力
            MoveV(Vector3.up * Vspeed * Time.deltaTime);
        }
    }

    float freshTime = 0;
    void sendUserInfo() {
        if (freshTime < 0.05f) {
            freshTime += Time.deltaTime;
        } else {
            freshTime = 0;
            Global.playerInfo = new UserInfo(Global.playerName, mybody.position, myHorizontalRotation);
            UserClient.SendMessage(new Message("UpdateUser", JsonConvert.SerializeObject(Global.playerInfo)));
        }
    }
}
