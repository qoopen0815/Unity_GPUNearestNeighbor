﻿using System.Runtime.InteropServices;
using UnityEngine;
using NearestNeighbor;

namespace NearestNeighborSample.TwoDimension
{

    public struct MyParticle
    {
        public Vector2 pos;
        public Vector3 color;
        public MyParticle(Vector2 pos)
        {
            this.pos = pos;
            this.color = new Vector3(1, 1, 1);
        }
    }

    public class MyParticleSystem : MonoBehaviour
    {

        #region ForParticle
        public ComputeShader ParticleCS;

        public PARTICLE_NUM mode = PARTICLE_NUM.NUM_8K;
        public int dispIdx;
        public Material ParticleRenderMat;

        private int threadGroupSize;
        private ComputeBuffer particlesBufferRead;
        private ComputeBuffer particlesBufferWrite;
        private static readonly int SIMULATION_BLOCK_SIZE = 32;
        private int numParticles;
        #endregion ForParticle

        #region ForGrid
        public Vector2 range = new Vector2(128, 128);
        public Vector2 gridDim = new Vector2(16, 16);
        GridOptimizer2D<MyParticle> gridOptimizer;
        #endregion ForGrid

        #region Accessor
        public ComputeBuffer GetBuffer()
        {
            return particlesBufferRead;
        }

        public int GetParticleNum()
        {
            return numParticles;
        }
        #endregion Accessor

        #region MonoBehaviourFuncs
        void Start()
        {
            numParticles = (int)mode;

            particlesBufferRead = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(MyParticle)));
            particlesBufferWrite = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(MyParticle)));

            InitializeParticle();

            gridOptimizer = new GridOptimizer2D<MyParticle>(numParticles, range, gridDim);
        }

        void Update()
        {

            // ---- Grid Optimization -------------------------------------------------------------------
            gridOptimizer.GridSort(ref particlesBufferRead);    // Pass the buffer you want to optimize  
                                                                // ---- Grid Optimization -------------------------------------------------------------------


            // ---- Your Particle Process -------------------------------------------------------------------
            ParticleCS.SetInt("_NumParticles", numParticles);
            ParticleCS.SetVector("_GridDim", gridDim);
            ParticleCS.SetInt("_DispIdx", (int)(dispIdx * numParticles * 0.001f));
            ParticleCS.SetFloat("_GridH", gridOptimizer.GetGridH());

            int kernel = ParticleCS.FindKernel("Update");
            ParticleCS.SetBuffer(kernel, "_ParticlesBufferRead", particlesBufferRead);
            ParticleCS.SetBuffer(kernel, "_ParticlesBufferWrite", particlesBufferWrite);
            ParticleCS.SetBuffer(kernel, "_GridIndicesBufferRead", gridOptimizer.GetGridIndicesBuffer());   // Get and use a GridIndicesBuffer to find neighbor
            ParticleCS.Dispatch(kernel, threadGroupSize, 1, 1);
            // ---- Your Particle Process -------------------------------------------------------------------

            SwapBuffer(ref particlesBufferRead, ref particlesBufferWrite);
        }

        private void OnRenderObject()
        {
            Material m = ParticleRenderMat;
            m.SetPass(0);
            m.SetBuffer("_Particles", GetBuffer());
            Graphics.DrawProceduralNow(MeshTopology.Points, GetParticleNum());
        }

        void OnDestroy()
        {
            DestroyBuffer(particlesBufferRead);
            DestroyBuffer(particlesBufferWrite);
            gridOptimizer.Release();                // Must
        }
        #endregion MonoBehaviourFuncs

        #region PrivateFuncs

        void InitializeParticle()
        {
            MyParticle[] particles = new MyParticle[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                particles[i] = new MyParticle(new Vector2(Random.Range(1, range.x), Random.Range(1, range.y)));
            }
            threadGroupSize = numParticles / SIMULATION_BLOCK_SIZE;
            particlesBufferRead.SetData(particles);
        }

        void SwapBuffer(ref ComputeBuffer src, ref ComputeBuffer dst)
        {
            ComputeBuffer tmp = src;
            src = dst;
            dst = tmp;
        }

        void DestroyBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        #endregion PrivateFuncs
    }

}