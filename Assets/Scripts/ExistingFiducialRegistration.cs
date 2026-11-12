using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExistingFiducialRegistration : MonoBehaviour
{
    [Header("CT / Fiducials")]
    public Transform ctRoot;
    public Transform[] ctFiducials; // Pt_1 to Pt_6, order matters

    [Header("Probe")]
    public Transform probeTip;
    public int averageFrames = 60;

    [Header("Debug Visualization")]
    public float markerDiameter = 0.005f; // 5 mm if Unity unit = meter
    public bool showProbeMarkers = true;

    private readonly List<Vector3> ctPointsLocal = new List<Vector3>();
    private readonly List<Vector3> probePointsWorld = new List<Vector3>();
    private readonly List<GameObject> probeMarkers = new List<GameObject>();

    private bool isRecording = false;

    void Start()
    {
        LoadCTFiducials();
    }

    void Update()
    {
        // R = record current probe point
        if (Input.GetKeyDown(KeyCode.R))
        {
            RecordProbePoint();
        }

        // Enter = register
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Register();
        }

        // C = clear recorded probe points
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearProbePoints();
        }

        // T = reset CT mesh transform
        if (Input.GetKeyDown(KeyCode.T))
        {
            ResetCTRoot();
        }

        // L = reload CT fiducials from scene
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadCTFiducials();
        }
    }

    public void LoadCTFiducials()
    {
        ctPointsLocal.Clear();

        if (ctRoot == null)
        {
            Debug.LogError("CTRoot is not assigned.");
            return;
        }

        if (ctFiducials == null || ctFiducials.Length < 3)
        {
            Debug.LogError("Need at least 3 CT fiducials.");
            return;
        }

        for (int i = 0; i < ctFiducials.Length; i++)
        {
            if (ctFiducials[i] == null)
            {
                Debug.LogError($"CT fiducial Pt_{i + 1} is null.");
                return;
            }

            Vector3 localPoint = ctRoot.InverseTransformPoint(ctFiducials[i].position);
            ctPointsLocal.Add(localPoint);

            Debug.Log($"Loaded CT Pt_{i + 1}: local = {localPoint}");
        }

        Debug.Log($"Loaded {ctPointsLocal.Count} CT fiducials.");
    }

    public void RecordProbePoint()
    {
        if (probeTip == null)
        {
            Debug.LogError("ProbeTip is not assigned.");
            return;
        }

        if (ctPointsLocal.Count == 0)
        {
            Debug.LogError("No CT fiducials loaded. Press L or check ctFiducials array.");
            return;
        }

        if (probePointsWorld.Count >= ctPointsLocal.Count)
        {
            Debug.LogWarning("Already recorded all probe points.");
            return;
        }

        if (!isRecording)
        {
            StartCoroutine(RecordProbePointAverage());
        }
    }

    private IEnumerator RecordProbePointAverage()
    {
        isRecording = true;

        int pointIndex = probePointsWorld.Count;
        Debug.Log($"Recording probe Pt_{pointIndex + 1}. Keep probe still...");

        Vector3 sum = Vector3.zero;

        for (int i = 0; i < averageFrames; i++)
        {
            sum += probeTip.position;
            yield return null;
        }

        Vector3 mean = sum / averageFrames;
        probePointsWorld.Add(mean);

        Debug.Log($"Recorded probe Pt_{pointIndex + 1}: world = {mean}");

        if (showProbeMarkers)
        {
            CreateProbeMarker(
                $"Probe_Pt_{pointIndex + 1}",
                mean,
                Color.yellow
            );
        }

        isRecording = false;
    }

    public void Register()
    {
        if (ctRoot == null)
        {
            Debug.LogError("CTRoot is not assigned.");
            return;
        }

        if (ctPointsLocal.Count != probePointsWorld.Count)
        {
            Debug.LogError($"Point count mismatch. CT = {ctPointsLocal.Count}, Probe = {probePointsWorld.Count}");
            return;
        }

        if (ctPointsLocal.Count < 3)
        {
            Debug.LogError("Need at least 3 point pairs.");
            return;
        }

        if (ctRoot.localScale != Vector3.one)
        {
            Debug.LogWarning("CTRoot scale should be 1,1,1. Scale CT_Mesh child instead.");
        }

        ComputeRigidTransform(
            ctPointsLocal,
            probePointsWorld,
            out Quaternion rotation,
            out Vector3 translation
        );

        ctRoot.SetPositionAndRotation(translation, rotation);
        ctRoot.localScale = Vector3.one;

        float fre = ComputeFRE();

        Debug.Log("========== Registration Finished ==========");
        Debug.Log($"FRE = {fre} Unity units");
        Debug.Log($"FRE approx = {fre * 1000.0f} mm if Unity unit = meter");
    }

    private float ComputeFRE()
    {
        float sumSquared = 0.0f;

        for (int i = 0; i < ctPointsLocal.Count; i++)
        {
            Vector3 registeredCT = ctRoot.TransformPoint(ctPointsLocal[i]);
            Vector3 probePoint = probePointsWorld[i];

            float error = Vector3.Distance(registeredCT, probePoint);
            sumSquared += error * error;

            Debug.Log($"Pt_{i + 1} error = {error} Unity units, approx {error * 1000.0f} mm if Unity unit = meter");
        }

        return Mathf.Sqrt(sumSquared / ctPointsLocal.Count);
    }

    public void ClearProbePoints()
    {
        probePointsWorld.Clear();

        foreach (GameObject marker in probeMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        probeMarkers.Clear();

        Debug.Log("Cleared probe points and probe markers.");
    }

    public void ResetCTRoot()
    {
        if (ctRoot == null)
            return;

        ctRoot.position = Vector3.zero;
        ctRoot.rotation = Quaternion.identity;
        ctRoot.localScale = Vector3.one;

        Debug.Log("Reset CTRoot.");
    }

    private void CreateProbeMarker(string name, Vector3 position, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * markerDiameter;

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        Collider col = marker.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        probeMarkers.Add(marker);
    }

    private static void ComputeRigidTransform(
        List<Vector3> source,
        List<Vector3> target,
        out Quaternion rotation,
        out Vector3 translation)
    {
        int n = source.Count;

        Vector3 sourceMean = Vector3.zero;
        Vector3 targetMean = Vector3.zero;

        for (int i = 0; i < n; i++)
        {
            sourceMean += source[i];
            targetMean += target[i];
        }

        sourceMean /= n;
        targetMean /= n;

        double Sxx = 0, Sxy = 0, Sxz = 0;
        double Syx = 0, Syy = 0, Syz = 0;
        double Szx = 0, Szy = 0, Szz = 0;

        for (int i = 0; i < n; i++)
        {
            Vector3 x = source[i] - sourceMean;
            Vector3 y = target[i] - targetMean;

            Sxx += x.x * y.x;
            Sxy += x.x * y.y;
            Sxz += x.x * y.z;

            Syx += x.y * y.x;
            Syy += x.y * y.y;
            Syz += x.y * y.z;

            Szx += x.z * y.x;
            Szy += x.z * y.y;
            Szz += x.z * y.z;
        }

        double[,] N = new double[4, 4];

        N[0, 0] = Sxx + Syy + Szz;
        N[0, 1] = Syz - Szy;
        N[0, 2] = Szx - Sxz;
        N[0, 3] = Sxy - Syx;

        N[1, 0] = Syz - Szy;
        N[1, 1] = Sxx - Syy - Szz;
        N[1, 2] = Sxy + Syx;
        N[1, 3] = Szx + Sxz;

        N[2, 0] = Szx - Sxz;
        N[2, 1] = Sxy + Syx;
        N[2, 2] = -Sxx + Syy - Szz;
        N[2, 3] = Syz + Szy;

        N[3, 0] = Sxy - Syx;
        N[3, 1] = Szx + Sxz;
        N[3, 2] = Syz + Szy;
        N[3, 3] = -Sxx - Syy + Szz;

        double[] q = LargestEigenVector4x4(N);

        // q format is [w, x, y, z]
        Quaternion rot = new Quaternion(
            (float)q[1],
            (float)q[2],
            (float)q[3],
            (float)q[0]
        );

        rot = NormalizeQuaternion(rot);

        rotation = rot;
        translation = targetMean - rotation * sourceMean;
    }

    private static double[] LargestEigenVector4x4(double[,] A)
    {
        double[] v = new double[] { 1, 0, 0, 0 };

        for (int iter = 0; iter < 100; iter++)
        {
            double[] Av = new double[4];

            for (int r = 0; r < 4; r++)
            {
                Av[r] =
                    A[r, 0] * v[0] +
                    A[r, 1] * v[1] +
                    A[r, 2] * v[2] +
                    A[r, 3] * v[3];
            }

            v = Normalize4(Av);
        }

        return v;
    }

    private static double[] Normalize4(double[] q)
    {
        double norm = Math.Sqrt(
            q[0] * q[0] +
            q[1] * q[1] +
            q[2] * q[2] +
            q[3] * q[3]
        );

        if (norm < 1e-12)
        {
            return new double[] { 1, 0, 0, 0 };
        }

        return new double[]
        {
            q[0] / norm,
            q[1] / norm,
            q[2] / norm,
            q[3] / norm
        };
    }

    private static Quaternion NormalizeQuaternion(Quaternion q)
    {
        float norm = Mathf.Sqrt(
            q.x * q.x +
            q.y * q.y +
            q.z * q.z +
            q.w * q.w
        );

        if (norm < 1e-8f)
            return Quaternion.identity;

        return new Quaternion(
            q.x / norm,
            q.y / norm,
            q.z / norm,
            q.w / norm
        );
    }
}