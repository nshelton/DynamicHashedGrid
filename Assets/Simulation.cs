using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{

    private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    private const int SIZE_PARTICLE = 24;

    private int m_particleCount = 1024 * 1024;
    public Material m_material;
    public ComputeShader m_computeShader;
    public float m_gridSizeScale = 10;

    private int m_cellCount = 64 * 64 * 64;

    GPUBuffer<Particle> m_particleBuffer;
    GPUBuffer<int> m_offsetBuffer;
    GPUBuffer<int> m_sortedIndexBuffer;
    GPUBuffer<Vector3> m_sumBuffer;

    private Dictionary<string, int> m_kernels = new Dictionary<string, int>();

    string[] m_kernelNames = new string[]
    {
        "Integrate",
        "ResetIndices",
        "ZeroOutOffsets",
        "SortBuffer",
        "CalculateOffsets",
        "ParallelSum"
    };

    private const int WARP_SIZE = 256;
    private int m_particleWarpCount;
    private int m_cellWarpCount;

    void InitComputeShaders()
    {
        m_particleWarpCount = Mathf.CeilToInt((float)m_particleCount / WARP_SIZE);
        m_cellWarpCount = Mathf.CeilToInt((float)m_cellCount / WARP_SIZE);
        m_particleBuffer = new GPUBuffer<Particle>(m_particleCount);
        m_sumBuffer = new GPUBuffer<Vector3>(m_particleCount);

        for (int i = 0; i < m_particleCount; i++)
        {
            m_particleBuffer.CPUData[i].position = Random.insideUnitSphere;
            m_particleBuffer.CPUData[i].velocity = Random.insideUnitSphere;
        }

        m_particleBuffer.Upload();

        m_offsetBuffer = new GPUBuffer<int>(m_cellCount);
        m_sortedIndexBuffer = new GPUBuffer<int>(m_particleCount);

        foreach (var kernelName in m_kernelNames)
        {
            m_kernels[kernelName] = m_computeShader.FindKernel(kernelName);
        }

        m_computeShader.SetBuffer(m_kernels["Integrate"], "_particleBuffer", m_particleBuffer.Buffer);

        m_computeShader.SetBuffer(m_kernels["Integrate"], "_sortedIndexBuffer", m_sortedIndexBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["Integrate"], "_cellOffsetBuffer", m_offsetBuffer.Buffer);

        m_computeShader.SetBuffer(m_kernels["ResetIndices"], "_sortedIndexBuffer", m_sortedIndexBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["ZeroOutOffsets"], "_cellOffsetBuffer", m_offsetBuffer.Buffer);

        m_computeShader.SetBuffer(m_kernels["SortBuffer"], "_particleBuffer", m_particleBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["SortBuffer"], "_sortedIndexBuffer", m_sortedIndexBuffer.Buffer);

        m_computeShader.SetBuffer(m_kernels["CalculateOffsets"], "_particleBuffer", m_particleBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["CalculateOffsets"], "_sortedIndexBuffer", m_sortedIndexBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["CalculateOffsets"], "_cellOffsetBuffer", m_offsetBuffer.Buffer);
 


        m_material.SetBuffer("particleBuffer", m_particleBuffer.Buffer);


    }

    void OnRenderObject()
    {
        m_material.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, 1, m_particleCount);
    }

    void OnDestroy()
    {
        m_particleBuffer?.Dispose();
        m_offsetBuffer?.Dispose();
        m_sortedIndexBuffer?.Dispose();
    }

    void RunShader(string name, int warpCount)
    {
        m_computeShader.Dispatch(m_kernels[name], warpCount, 1, 1);
    }

    void Update()
    {
        if (!m_kernels.ContainsKey("Integrate"))
        {
            InitComputeShaders();
        }

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        m_computeShader.SetFloat("_deltaTime", Time.deltaTime);
        m_computeShader.SetFloats("_mousePosition", mousePosition2D);
        m_computeShader.SetFloats("_gridSizeScale", m_gridSizeScale);
        m_computeShader.SetInt("_numParticles", m_particleCount);

        RunShader("ResetIndices", m_particleWarpCount);
        RunShader("ZeroOutOffsets", m_cellWarpCount);
        //Sort
        for (int dim = 2; dim <= m_particleCount; dim <<= 1)
        {
             m_computeShader.SetInt("_dim", dim);
             for (int block = dim >> 1; block > 0; block >>= 1)
             {
                 m_computeShader.SetInt("_block", block);
                 m_computeShader.SetBuffer(m_kernels["SortBuffer"], "_particleBuffer", m_particleBuffer.Buffer);
                 m_computeShader.SetBuffer(m_kernels["SortBuffer"], "_sortedIndexBuffer", m_sortedIndexBuffer.Buffer);
                 m_computeShader.Dispatch(m_kernels["SortBuffer"], m_particleCount, 1, 1);
             }
        }

        RunShader("CalculateOffsets", m_particleWarpCount);
        
        m_computeShader.SetBuffer(m_kernels["Integrate"], "_particleBuffer", m_particleBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["Integrate"], "_sumBuffer", m_sumBuffer.Buffer);
        RunShader("Integrate", m_particleWarpCount);

        m_computeShader.SetBuffer(m_kernels["ParallelSum"], "_particleBuffer", m_particleBuffer.Buffer);
        m_computeShader.SetBuffer(m_kernels["ParallelSum"], "_sumBuffer", m_sumBuffer.Buffer);

        for (int dim = 1; dim <= m_particleCount; dim <<= 1)
        {
            m_computeShader.SetInt("_dim", dim);
            RunShader("ParallelSum", m_particleWarpCount);
        }


        //   m_particleBuffer.Download();
        //  Debug.Log(m_particleBuffer.CPUData[110].position);
    }

    void OnGUI()
    {
        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        mousePos.x = e.mousePosition.x;
        mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
    }

    private void OnDrawGizmos()
    {
        var size = Vector3.one /m_gridSizeScale;
        int dim = 9;
        for (int x = 0; x<dim; x++)
            for(int y = 0; y<dim; y++)
                for(int z = 0; z<dim; z++)
                {
                    var pos = new Vector3(x-dim/2, y-dim/2, z-dim/2);
                    Gizmos.DrawWireCube(Vector3.Scale(pos,size) + size/2, size);
                }

    }

}