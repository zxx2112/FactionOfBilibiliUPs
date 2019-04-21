using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using Landfall.TABS;
using UnityEngine;

namespace FactionOfBilibiliUPs
{
    public class Main
    {
        static UnityModManager.ModEntry modeEntry;
        static AssetBundle ab;
        static bool Load(UnityModManager.ModEntry _modEntry) {
            modeEntry = _modEntry;
            //新增一个派系
            _modEntry.Logger.Log("1------------->");
            var database = LandfallUnitDatabase.GetDatabase();
            var factions = database.GetFactions();
            var upsFaction = ScriptableObject.CreateInstance<Faction>();
            upsFaction.Init();
            _modEntry.Logger.Log("2------------->");
            upsFaction.Entity.Name = "Bilibili Ups";
            //必须设置派系图标不然会报错
            _modEntry.Logger.Log("3------------->");
            upsFaction.Entity.Icon = factions[0].Entity.Icon;
            database.AddFactionWithID(upsFaction);
            _modEntry.Logger.Log("添加派系:"+ upsFaction.Entity.Name);
            //加载AssetBundle
            var path = Application.dataPath + "/../Mods/FactionOfBilibiliUPs/res.ab";
            modeEntry.Logger.Log("资源文件加载路径:" + path);
            ab = AssetBundle.LoadFromFile(path);
            if (ab == null) {
                modeEntry.Logger.Log("未能正确加载资源文件");
                return false;
            }
            //Basketball(upsFaction);
            if (!ReverseMaceSpinner(upsFaction)) return false;
            return true;
        }

        static bool Basketball(Faction upsFaction) {
            var database = LandfallUnitDatabase.GetDatabase();
            //添加一个单位
            var customUnitBase = FindBluePrintByName("Stoner");
            var newUnit = new UnitBlueprint(customUnitBase);
            newUnit.Entity.Name = "TestUnit";
            upsFaction.Units = new UnitBlueprint[] { newUnit };
            database.AddUnitWithID(newUnit);
            //定制石头
            //使用巨石为模板实例化一个篮球
            var prefabRoot = new GameObject("PrefabRoot");
            prefabRoot.transform.position = Vector3.zero;
            var basketballWeapon = GameObject.Instantiate(customUnitBase.RightWeapon, prefabRoot.transform);
            //basketballWeapon.SetActive(false);
            basketballWeapon.name = "Thrower_Basketball";
            basketballWeapon.transform.position = new Vector3(0, -100, 0);
            var basketballArmor = GameObject.Instantiate(customUnitBase.RightWeapon.GetComponent<RangeWeapon>().objectToSpawn, prefabRoot.transform);
            //var basketballArmor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            basketballArmor.transform.parent = prefabRoot.transform;
            //basketballArmor.SetActive(false);
            basketballArmor.name = "B_Basketball";
            //basketballArmor.transform.position = new Vector3(0, -100, 0);
            //加载网格
            var basketballModel = ab.LoadAsset<GameObject>("Basketball");
            if (basketballModel == null) {
                modeEntry.Logger.Log("未能正确加载预制体");
                return false;
            }
            //替换mesh
            var rock1 = basketballWeapon.transform.Find("rock");
            rock1.GetComponent<MeshFilter>().mesh = basketballModel.GetComponent<MeshFilter>().mesh;
            rock1.GetComponent<MeshRenderer>().materials = basketballModel.GetComponent<MeshRenderer>().materials;
            //
            var rock2 = basketballArmor.transform.Find("rock");
            rock2.GetComponent<MeshFilter>().mesh = basketballModel.GetComponent<MeshFilter>().mesh;
            rock2.GetComponent<MeshRenderer>().materials = basketballModel.GetComponent<MeshRenderer>().materials;
            GameObject.Destroy(rock2.GetComponent<MeshCollider>());
            rock2.gameObject.AddComponent<SphereCollider>().radius = 0.005565071f;
            //防止销毁
            GameObject.DontDestroyOnLoad(prefabRoot);
            var remover = basketballArmor.GetComponent<RemoveAfterSeconds>();
            basketballArmor.AddComponent<RemoveAdder>().Init(remover);
            GameObject.Destroy(remover);
            //GameObject.Destroy(basketballArmor.GetComponent<RemoveAfterSeconds>());
            newUnit.RightWeapon = basketballWeapon;
            basketballWeapon.GetComponent<RangeWeapon>().objectToSpawn = basketballArmor;
            //
            modeEntry.Logger.Log("添加单位 " + newUnit.Entity.Name + " 到派系 " + upsFaction.Entity.Name);
            return true;
        }
        static bool ReverseMaceSpinner(Faction upsFaction) {
            var database = LandfallUnitDatabase.GetDatabase();
            //添加一个单位
            var customUnitBase = FindBluePrintByName("Brawler");
            if (customUnitBase == null) return false;
            var newUnit = new UnitBlueprint(customUnitBase);
            newUnit.Entity.Name = "TestUnit";
            upsFaction.Units = new UnitBlueprint[] { newUnit };
            database.AddUnitWithID(newUnit);

            //加载网格
            var jingGaiModel = ab.LoadAsset<GameObject>("JingGai");
            if (jingGaiModel == null) {
                modeEntry.Logger.Log("未能正确加载预制体");
                return false;
            }

            //拿到盾牌预制体
            var prefabRoot = new GameObject("PrefabRoot");
            GameObject.DontDestroyOnLoad(prefabRoot);
            prefabRoot.transform.position = Vector3.zero;
            var dunPaiWeapon = GameObject.Instantiate(newUnit.LeftWeapon, prefabRoot.transform);
            dunPaiWeapon.transform.Find("CP_Viking_Shield001").gameObject.SetActive(false);
            //放到盾牌预制体之下
            var jingGai = GameObject.Instantiate(jingGaiModel, dunPaiWeapon.transform);
            jingGai.transform.localScale = Vector3.one;
            newUnit.LeftWeapon = dunPaiWeapon;
            return true;
        }

        static UnitBlueprint FindBluePrintByName(string name) {
            //查找到CustomUnitBase蓝图
            var blueprints = Resources.FindObjectsOfTypeAll<UnitBlueprint>();
            modeEntry.Logger.Log("查找到蓝图数量:" + blueprints.Count());
            var customUnitBase = blueprints
                .Where(unit => {
                    var result = false;
                    if (unit.Name == name) {
                        result = true;
                    } else {
                        result = false;
                    }
                    return result;
                }).FirstOrDefault();
            if (customUnitBase == null) {
                modeEntry.Logger.Log("未找到"+ name + "蓝图");
                return null;
            }
            return customUnitBase;
        }
    }
    //在Awake时候加上remove组件
    public class RemoveAdder : MonoBehaviour
    {
        float extraRandom;
        bool lerpIn;
        GameObject objectToSpawn;
        float seconds;
        bool shrink;
        bool spawnObjectOnMainRig;
        public void Init(RemoveAfterSeconds sampleRemover) {
            extraRandom = sampleRemover.extraRandom;
            lerpIn = sampleRemover.lerpIn;
            objectToSpawn = sampleRemover.objectToSpawn;
            seconds = sampleRemover.seconds;
            shrink = sampleRemover.shrink;
            spawnObjectOnMainRig = sampleRemover.spawnObjectOnMainRig;
        }

        void Start() {
            if(name.Contains("Clone")) {
                if (gameObject.GetComponent<RemoveAfterSeconds>() == null) {
                    var remover = gameObject.AddComponent<RemoveAfterSeconds>();
                    remover.extraRandom = extraRandom;
                    remover.lerpIn = lerpIn;
                    remover.objectToSpawn = objectToSpawn;
                    remover.seconds = 10;
                    remover.shrink = true;
                    remover.spawnObjectOnMainRig = spawnObjectOnMainRig;
                }
            }
        }
    }
}
