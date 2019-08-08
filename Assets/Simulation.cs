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

    ComputeBuffer m_particleBuffer;
    private Dictionary<string, int> m_kernelIDs = new Dictionary<string, int>();

    string[] m_kernelNames = new string[]
    {
        "Integrate",
        "BufferToGrid",
        "SortBuffer"
    };

    private const int WARP_SIZE = 256;
    private int mWarpCount;



    void InitComputeShaders()
    {

        mWarpCount = Mathf.CeilToInt((float)m_particleCount / WARP_SIZE);

        Particle[] particleArray = new Particle[m_particleCount];

        for (int i = 0; i < m_particleCount; i++)
        {
            particleArray[i].position = Random.insideUnitSphere;
            particleArray[i].velocity = Vector3.zero;
        }

        m_particleBuffer = new ComputeBuffer(m_particleCount, SIZE_PARTICLE);
        m_particleBuffer.SetData(particleArray);

        foreach(var kernelName in m_kernelNames)
        {
            m_kernelIDs[kernelName] = m_computeShader.FindKernel(kernelName);
        }

        m_computeShader.SetBuffer(m_kernelIDs["Integrate"], "particleBuffer", m_particleBuffer);
        m_material.SetBuffer("particleBuffer", m_particleBuffer);
    }

    void OnRenderObject()
    {
        m_material.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, 1, m_particleCount);
    }

    void OnDestroy()
    {
        if (m_particleBuffer != null)
            m_particleBuffer.Release();
    }

    void Update()
    {
        if (!m_kernelIDs.ContainsKey("Integrate"))
        {
            InitComputeShaders();
        }

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        m_computeShader.SetFloat("deltaTime", Time.deltaTime);
        m_computeShader.SetFloats("mousePosition", mousePosition2D);

        m_computeShader.Dispatch(m_kernelIDs["Integrate"], mWarpCount, 1, 1);
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

    void TestSort()
    {
        GPUBuffer<int> randomIntComputeBuffer = new GPUBuffer<int>(1024 * 1024);
        GPUBuffer<int> randomIntComputeBufferKeys = new GPUBuffer<int>(1024 * 1024);

        for (int i = 0; i < m_particleCount; i++)
        {
            randomIntComputeBufferKeys.CPUData[i] = i;
            randomIntComputeBuffer.CPUData[i] = (int)(Random.value * 100000.0);
        }

        Debug.Log("Random Ints");
        Debug.Log(string.Join(",", randomIntComputeBuffer.CPUData));
        Debug.Log(string.Join(",", randomIntComputeBufferKeys.CPUData));
        Debug.Log("Do Sort");

        int count = m_particleCount;

        randomIntComputeBuffer.Upload();
        randomIntComputeBufferKeys.Upload();

        m_computeShader.SetInt("_count", m_particleCount);
        for (var dim = 2; dim <= count; dim <<= 1)
        {
            m_computeShader.SetInt("_dim", dim);
            for (var block = dim >> 1; block > 0; block >>= 1)
            {
                m_computeShader.SetInt("_block", block);
                m_computeShader.SetBuffer(m_kernelIDs["SortBuffer"], "_intBuffer", randomIntComputeBuffer.Buffer);
                m_computeShader.SetBuffer(m_kernelIDs["SortBuffer"], "_sortedIndexBuffer", randomIntComputeBufferKeys.Buffer);
                m_computeShader.Dispatch(m_kernelIDs["SortBuffer"], m_particleCount, 1, 1);
            }
        }

        Debug.Log("Sorted Ints");
        Debug.Log(string.Join(",", randomIntComputeBuffer.Download()));
        Debug.Log(string.Join(",", randomIntComputeBufferKeys.Download()));

        int lastValue = -1;
        for (int i = 0; i < 100; i++)
        {
            int sortedIndex = randomIntComputeBufferKeys.CPUData[i];
            int value = randomIntComputeBuffer.CPUData[sortedIndex];

            if (value > lastValue)
            {
                Debug.Log("good");
            }
            else
            {
                Debug.Log($" {value} {lastValue}");
            }
        }
    }
}