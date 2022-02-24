using System;
using System.Collections.Generic;
using UnityEngine;

public class ICP
{

    // public Vector3[] Run(Gesture staticG, Gesture dynamicG)
    // {
    public Vector3[] Run(Vector3[] staticG, Vector3[] dynamicG)
    {
        var dynamicMid = ComputeMean(dynamicG);
        var staticMid = ComputeMean(staticG);

        Vector3 qd;
        Vector3 qs;

        float[] U = new float[9];
        float[] w;
        float[] V;

        float[,] uSvd = new float[3, 3];
        float[,] vSvd = new float[3, 3];

        for (int i = 0; i < staticG.Length; i++)
        {
            var staticF = staticG[i];
            // var staticFId = staticF.ToID();
            var dynamicF = dynamicG[i]; //.fingerData.Find(f => f.ToID() == staticFId);

            // Assert.IsNotNull(dynamicF);

            // qd = staticF.GetRelativePosition() - dynamicMid;
            // qs = dynamicF.GetRelativePosition() - staticMid;

            qd = staticF - dynamicMid;
            qs = dynamicF - staticMid;

            w = OuterProduct(qs, qd);
            U = AddMatrix(w, U);
        }

        uSvd = CopyMatToUV(U);
        Dsvd(uSvd, vSvd, 1);
        U = CopyUVtoMat(uSvd);
        V = CopyUVtoMat(vSvd);

        Transpose(V);
        float[] rotationMatrix = MatrixMult(U, V);

        var t = Rotate(dynamicMid, rotationMatrix);
        var translation = staticMid - t;

        Vector3[] results = new Vector3[dynamicG.Length];

        //update the point cloud
        for (int i = 0; i < dynamicG.Length; i++)
        {
            var p = Rotate(dynamicG[i], rotationMatrix);
            var newPos = p + translation;
            results[i] = newPos;
        }

        return results;
    }

    private float[,] CopyMatToUV(float[] mat)
    {
        var result = new float[3, 3];
        result[0, 0] = mat[0];
        result[0, 1] = mat[1];
        result[0, 2] = mat[2];

        result[1, 0] = mat[3];
        result[1, 1] = mat[4];
        result[1, 2] = mat[5];

        result[2, 0] = mat[6];
        result[2, 1] = mat[7];
        result[2, 2] = mat[8];
        return result;
    }

    private Vector3 ComputeMean(Vector3[] cloud)
    {
        var mean = Vector3.zero;

        for (int i = 0; i < cloud.Length; i++)
        {
            mean += cloud[i];
        }

        return mean / cloud.Length;
    }

    private float[] OuterProduct(Vector3 a, Vector3 b)
    {
        float[] matrix = new float[9];
        matrix[0] = a.x * b.x;
        matrix[1] = a.x * b.y;
        matrix[2] = a.x * b.z;

        matrix[3] = a.y * b.x;
        matrix[4] = a.y * b.y;
        matrix[5] = a.y * b.z;

        matrix[6] = a.z * b.x;
        matrix[7] = a.z * b.y;
        matrix[8] = a.z * b.z;
        return matrix;
    }

    private float[] AddMatrix(float[] a, float[] b)
    {
        float[] matrix = new float[9];
        for (int i = 0; i < 9; i++)
        {
            matrix[i] = a[i] + b[i];
        }
        return matrix;
    }


    void Transpose(float[] a)
    {
        float temp;

        temp = a[1];
        a[1] = a[3];
        a[3] = temp;

        temp = a[2];
        a[2] = a[6];
        a[6] = temp;

        temp = a[5];
        a[5] = a[7];
        a[7] = temp;
    }




    private double PYTHAG(double a, double b)
    {
        double at = Math.Abs(a), bt = Math.Abs(b), ct, result;

        if (at > bt) { ct = bt / at; result = at * Math.Sqrt(1.0 + ct * ct); }
        else if (bt > 0.0) { ct = at / bt; result = bt * Math.Sqrt(1.0 + ct * ct); }
        else result = 0.0;
        return result;
    }


    private int Dsvd(float[,] a, float[,] v, int maxIterations)
    {
        /*
            Matrix a: [1..m,1..n]
                replaced by U.
            vector w: singular values
            matrix v: matrix V [1..n,1..n]
        */
        int m = 3, n = 3;
        int flag, i, its, j, jj, k, l = 0, nm = 0;
        double c, f, h, s, x, y, z;
        double anorm = 0.0, g = 0.0, scale = 0.0;
        double[] rv1 = new double[n];
        float[] w = new float[3];

        /* Householder reduction to bidiagonal form */
        for (i = 0; i < n; i++)
        {
            /* left-hand reduction */
            l = i + 1;
            rv1[i] = scale * g;
            g = s = scale = 0.0;
            if (i < m)
            {
                for (k = i; k < m; k++)
                    scale += Math.Abs((double)a[k, i]);
                if (scale > 0)
                {
                    for (k = i; k < m; k++)
                    {
                        a[k, i] = (float)(a[k, i] / scale);
                        s += a[k, i] * (double)a[k, i];
                    }
                    f = a[i, i];
                    g = -SIGN(Math.Sqrt(s), f);
                    h = f * g - s;
                    a[i, i] = (float)(f - g);
                    if (i != n - 1)
                    {
                        for (j = l; j < n; j++)
                        {
                            for (s = 0.0, k = i; k < m; k++)
                                s += (double)a[k, i] * a[k, j];
                            f = s / h;
                            for (k = i; k < m; k++)
                                a[k, j] += (float)(f * a[k, i]);
                        }
                    }
                    for (k = i; k < m; k++)
                        a[k, i] = (float)(a[k, i] * scale);
                }
            }
            w[i] = (float)(scale * g);

            /* right-hand reduction */
            g = s = scale = 0.0;
            if (i < m && i != n - 1)
            {
                for (k = l; k < n; k++)
                    scale += Math.Abs((double)a[i, k]);
                if (scale > 0)
                {
                    for (k = l; k < n; k++)
                    {
                        a[i, k] = (float)(a[i, k] / scale);
                        s += a[i, k] * (double)a[i, k];
                    }
                    f = a[i, l];
                    g = -SIGN(Math.Sqrt(s), f);
                    h = f * g - s;
                    a[i, l] = (float)(f - g);
                    for (k = l; k < n; k++)
                        rv1[k] = a[i, k] / h;
                    if (i != m - 1)
                    {
                        for (j = l; j < m; j++)
                        {
                            for (s = 0.0, k = l; k < n; k++)
                                s += a[j, k] * (double)a[i, k];
                            for (k = l; k < n; k++)
                                a[j, k] += (float)(s * rv1[k]);
                        }
                    }
                    for (k = l; k < n; k++)
                        a[i, k] = (float)(a[i, k] * scale);
                }
            }
            anorm = Math.Max(anorm, Math.Abs((double)w[i]) + Math.Abs(rv1[i]));
        }

        /* accumulate the right-hand transformation */
        for (i = n - 1; i >= 0; i--)
        {
            if (i < n - 1)
            {
                if (g > 0)
                {
                    for (j = l; j < n; j++)
                        v[j, i] = (float)(a[i, j] / (double)a[i, l] / g);
                    /* double division to avoid underflow */
                    for (j = l; j < n; j++)
                    {
                        for (s = 0.0, k = l; k < n; k++)
                            s += a[i, k] * (double)v[k, j];
                        for (k = l; k < n; k++)
                            v[k, j] += (float)(s * v[k, i]);
                    }
                }
                for (j = l; j < n; j++)
                    v[i, j] = v[j, i] = 0.0f;
            }
            v[i, i] = 1.0f;
            g = rv1[i];
            l = i;
        }

        /* accumulate the left-hand transformation */
        for (i = n - 1; i >= 0; i--)
        {
            l = i + 1;
            g = w[i];
            if (i < n - 1)
                for (j = l; j < n; j++)
                    a[i, j] = 0.0f;
            if (g > 0)
            {
                g = 1.0 / g;
                if (i != n - 1)
                {
                    for (j = l; j < n; j++)
                    {
                        for (s = 0.0, k = l; k < m; k++)
                            s += a[k, i] * (double)a[k, j];
                        f = (s / a[i, i]) * g;
                        for (k = i; k < m; k++)
                            a[k, j] += (float)(f * a[k, i]);
                    }
                }
                for (j = i; j < m; j++)
                    a[j, i] = (float)(a[j, i] * g);
            }
            else
            {
                for (j = i; j < m; j++)
                    a[j, i] = 0.0f;
            }
            ++a[i, i];
        }

        /* diagonalize the bidiagonal form */
        for (k = n - 1; k >= 0; k--)
        {                             /* loop over singular values */
            for (its = 0; its < maxIterations; its++)
            {                         /* loop over allowed iterations */
                flag = 1;
                for (l = k; l >= 0; l--)
                {                     /* test for splitting */
                    nm = l - 1;
                    if (Math.Abs(rv1[l]) + anorm == anorm)
                    {
                        flag = 0;
                        break;
                    }
                    if (Math.Abs((double)w[nm]) + anorm == anorm)
                        break;
                }
                if (flag > 0)
                {
                    c = 0.0;
                    s = 1.0;
                    for (i = l; i <= k; i++)
                    {
                        f = s * rv1[i];
                        if (Math.Abs(f) + anorm != anorm)
                        {
                            g = w[i];
                            h = PYTHAG(f, g);
                            w[i] = (float)h;
                            h = 1.0 / h;
                            c = g * h;
                            s = (-f * h);
                            for (j = 0; j < m; j++)
                            {
                                y = a[j, nm];
                                z = a[j, i];
                                a[j, nm] = (float)(y * c + z * s);
                                a[j, i] = (float)(z * c - y * s);
                            }
                        }
                    }
                }
                z = w[k];
                if (l == k)
                {                  /* convergence */
                    if (z < 0.0)
                    {              /* make singular value nonnegative */
                        w[k] = (float)(-z);
                        for (j = 0; j < n; j++)
                            v[j, k] = (-v[j, k]);
                    }
                    break;
                }
                if (its >= maxIterations)
                {
                    return 0;
                }

                /* shift from bottom 2 x 2 minor */
                x = w[l];
                nm = k - 1;
                y = w[nm];
                g = rv1[nm];
                h = rv1[k];
                f = ((y - z) * (y + z) + (g - h) * (g + h)) / (2.0 * h * y);
                g = PYTHAG(f, 1.0);
                f = ((x - z) * (x + z) + h * ((y / (f + SIGN(g, f))) - h)) / x;

                /* next QR transformation */
                c = s = 1.0;
                for (j = l; j <= nm; j++)
                {
                    i = j + 1;
                    g = rv1[i];
                    y = w[i];
                    h = s * g;
                    g = c * g;
                    z = PYTHAG(f, h);
                    rv1[j] = z;
                    c = f / z;
                    s = h / z;
                    f = x * c + g * s;
                    g = g * c - x * s;
                    h = y * s;
                    y = y * c;
                    for (jj = 0; jj < n; jj++)
                    {
                        x = v[jj, j];
                        z = v[jj, i];
                        v[jj, j] = (float)(x * c + z * s);
                        v[jj, i] = (float)(z * c - x * s);
                    }
                    z = PYTHAG(f, h);
                    w[j] = (float)z;
                    if (z > 0)
                    {
                        z = 1.0 / z;
                        c = f * z;
                        s = h * z;
                    }
                    f = (c * g) + (s * y);
                    x = (c * y) - (s * g);
                    for (jj = 0; jj < m; jj++)
                    {
                        y = a[jj, j];
                        z = a[jj, i];
                        a[jj, j] = (float)(y * c + z * s);
                        a[jj, i] = (float)(z * c - y * s);
                    }
                }
                rv1[l] = 0.0;
                rv1[k] = f;
                w[k] = (float)x;
            }
        }
        return 1;
    }
    double SIGN(double a, double b) => b >= 0.0 ? Math.Abs(a) : -Math.Abs(a);

    float[] CopyUVtoMat(float[,] mat)
    {
        return new float[]{
        mat[0, 0],
        mat[0, 1],
        mat[0, 2],

        mat[1, 0],
        mat[1, 1],
        mat[1, 2],

        mat[2, 0],
        mat[2, 1],
        mat[2, 2],
        };
    }

    float[] MatrixMult(float[] a, float[] b)
    {
        float[] result = new float[9];
        result[0] = a[0] * b[0] + a[1] * b[3] + a[2] * b[6];
        result[1] = a[0] * b[1] + a[1] * b[4] + a[2] * b[7];
        result[2] = a[0] * b[2] + a[1] * b[5] + a[2] * b[8];

        result[3] = a[3] * b[0] + a[4] * b[3] + a[5] * b[6];
        result[4] = a[3] * b[1] + a[4] * b[4] + a[5] * b[7];
        result[5] = a[3] * b[2] + a[4] * b[5] + a[5] * b[8];

        result[6] = a[6] * b[0] + a[7] * b[3] + a[8] * b[6];
        result[7] = a[6] * b[1] + a[7] * b[4] + a[8] * b[7];
        result[8] = a[6] * b[2] + a[7] * b[5] + a[8] * b[8];
        return result;
    }

    Vector3 Rotate(Vector3 p, float[] rotationMatrix)
    {
        var result = Vector3.zero;
        result.x = p.x * rotationMatrix[0] + p.y * rotationMatrix[1] + p.z * rotationMatrix[2];
        result.y = p.x * rotationMatrix[3] + p.y * rotationMatrix[4] + p.z * rotationMatrix[5];
        result.z = p.x * rotationMatrix[6] + p.y * rotationMatrix[7] + p.z * rotationMatrix[8];
        return result;
    }

}


public class ICP_Tester
{

    public Vector3[] staticPointCloud;
    public Vector3[] dynamicPointCloud;

    Vector3[] CreatePoints()
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(new Vector3(-1.0f, -1.0f, -1.0f));
        points.Add(new Vector3(1.0f, -1.0f, -1.0f));
        points.Add(new Vector3(-1.0f, 1.0f, -1.0f));
        points.Add(new Vector3(1.0f, 1.0f, -1.0f));
        points.Add(new Vector3(-1.0f, -1.0f, 1.0f));
        points.Add(new Vector3(1.0f, -1.0f, 1.0f));
        points.Add(new Vector3(-1.0f, 1.0f, 1.0f));
        points.Add(new Vector3(1.0f, 1.0f, 1.0f));

        return points.ToArray();
    }

    void ApplyAffineTransform(Vector3[] points, float[] rotationMatrix, float[] translation)
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector3()
            {
                x = rotationMatrix[0] * points[i].x + rotationMatrix[1] * points[i].x + rotationMatrix[2] * points[i].x + translation[0],
                y = rotationMatrix[3] * points[i].y + rotationMatrix[4] * points[i].y + rotationMatrix[5] * points[i].y + translation[1],
                z = rotationMatrix[6] * points[i].z + rotationMatrix[7] * points[i].z + rotationMatrix[8] * points[i].z + translation[2]
            };

        }
    }
    public void setup()
    {
        //create a static box point cloud used as a reference.
        staticPointCloud = CreatePoints();

        //create a dynamic box point cloud.
        //this point cloud is transformed to match the static point cloud.
        dynamicPointCloud = CreatePoints();

        //apply an artitrary rotation and translation to the dynamic point cloud to misalign the point cloud.
        float[] rotation = { 1.0f, 0.0f, 0.0f, 0.0f, 0.70710678f, -0.70710678f, 0.0f, 0.70710678f, 0.70710678f };
        float[] translation = { -0.75f, 0.5f, -0.5f };
        ApplyAffineTransform(dynamicPointCloud, rotation, translation);
    }

    public void test()
    {
        //use iterative closest point to transform the dynamic point cloud to best align the static point cloud.
        dynamicPointCloud = new ICP().Run(dynamicPointCloud, staticPointCloud);

        float alignmentError = 0.0f;
        for (int i = 0; i < dynamicPointCloud.Length; i++)
        {
            alignmentError += (float)Math.Pow(dynamicPointCloud[i].x - staticPointCloud[i].x, 2.0f);
            alignmentError += (float)Math.Pow(dynamicPointCloud[i].y - staticPointCloud[i].y, 2.0f);
            alignmentError += (float)Math.Pow(dynamicPointCloud[i].z - staticPointCloud[i].z, 2.0f);
        }

        alignmentError /= dynamicPointCloud.Length;

        Debug.LogFormat("Alignment Error: {0}", alignmentError);
    }
}