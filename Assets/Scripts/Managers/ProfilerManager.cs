﻿using Assets.Scripts.Model;
using Assets.Scripts.MonoBehaviours;
using Assets.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class ProfilerManager : GameManager
    {
        static readonly string algorythm = nameof(UpdateAntsMT);
        static readonly string savePath = "/Profiling_" + algorythm + ".csv";
        public List<(int, float)> frames = new List<(int, float)>();

        void Start()
        {
            GameManager.gameState = new GameState();
            isPaused = false;
            StartCoroutine(algorythm);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            GameManager.gameState.gameTime += dt;
            if (GameManager.gameState.gameTime < 2f) return;

            if (dt < 0.05f)
            {
                frames.Add((activeAnts.Count, dt));

                pheroDecayTimer += dt;
                if (pheroDecayTimer > pheroDecayDelay)
                {
                    pheroDecayTimer -= pheroDecayDelay;
                    gameState.pheromonesMap.DecayMarkers();
                }
            }
            else
            {

                FileInfo fileInfo = new FileInfo(Application.persistentDataPath + savePath);
                using (TextWriter writer = new StreamWriter(fileInfo.Open(FileMode.Create)))
                {
                    foreach (var tuple in frames)
                    {
                        writer.WriteLine(tuple.Item1 + ";" + tuple.Item2);
                    }
                }
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }

        public IEnumerator UpdateAntsST()
        {
            while (true)
            {
                foreach (Ant ant in activeAnts)
                {
                    ant.proxy.Init(ant);
                    ant.UpdateSelf();
                }
                yield return null;
            }
        }

        public IEnumerator UpdateAntsMT()
        {
            while (true)
            {
                for (int i = 0; i < activeAnts.Count; i++)
                {
                    var ant = activeAnts[i];
                    ant.proxy.Init(ant);
                }
                ParallelUtils.For(0, activeAnts.Count, delegate (int index) { activeAnts[index].UpdateSelf(); });
                yield return null;
            }
        }

        public IEnumerator UpdateAntsSTQueue()
        {
            while (true)
            {
                foreach (Ant ant in antsToUpdate)
                {
                    ant.proxy.Init(ant);
                    ant.UpdateSelf();
                }
                antsToUpdate.Clear();
                yield return null;
            }
        }
        public IEnumerator UpdateAntsMTQueue()
        {
            while (true)
            {
                foreach(Ant ant in antsToUpdate)
                {
                    ant.proxy.Init(ant);
                }
                ParallelUtils.For(0, antsToUpdate.Count, delegate (int index) { antsToUpdate[index].UpdateSelf(); });
                antsToUpdate.Clear();
                yield return null;
            }
        }

        public IEnumerator UpdateAntsBatchedST()
        {
            int BATCH_NB = 5;
            int batchOffset = 0;
            while (true)
            {
                for (int i = 0; i < activeAnts.Count / BATCH_NB; i++)
                {
                    var ant = activeAnts[i * BATCH_NB + batchOffset];
                    ant.proxy.Init(ant);
                    ant.UpdateSelf();
                }
                batchOffset = Mathf.Min((batchOffset + 1) % BATCH_NB, activeAnts.Count / BATCH_NB);
                yield return null;
            }
        }



        public IEnumerator UpdateAntsBatchedMT()
        {
            int BATCH_NB = 5;
            int batchOffset = 0;
            while (true)
            {
                for (int i = 0; i < activeAnts.Count / BATCH_NB; i++)
                {
                    var ant = activeAnts[i * BATCH_NB + batchOffset];
                    ant.proxy.Init(ant);
                }
                ParallelUtils.For(0, activeAnts.Count / BATCH_NB, delegate (int index) { activeAnts[index * BATCH_NB + batchOffset].UpdateSelf(); });
                batchOffset = Mathf.Min((batchOffset + 1) % BATCH_NB, activeAnts.Count / BATCH_NB);
                yield return null;
            }
        }
    }
}