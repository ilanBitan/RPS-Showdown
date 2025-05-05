//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class UnityMainThreadDispatcher : MonoBehaviour
//{
//    private static UnityMainThreadDispatcher instance;
//    private readonly Queue<Action> executionQueue = new Queue<Action>();

//    public static UnityMainThreadDispatcher Instance()
//    {
//        if (instance == null)
//        {
//            GameObject go = new GameObject("UnityMainThreadDispatcher");
//            instance = go.AddComponent<UnityMainThreadDispatcher>();
//            DontDestroyOnLoad(go);
//        }
//        return instance;
//    }

//    public void Enqueue(Action action)
//    {
//        lock (executionQueue)
//        {
//            executionQueue.Enqueue(action);
//        }
//    }

//    void Update()
//    {
//        lock (executionQueue)
//        {
//            while (executionQueue.Count > 0)
//            {
//                executionQueue.Dequeue().Invoke();
//            }
//        }
//    }
//}
