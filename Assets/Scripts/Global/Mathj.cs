using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Mathj {

    /// <summary>
    /// Returns the isolated y axis rotation. Poor name? Yes.
    /// </summary>
    /// <param name="quaternion"></param>
    /// <returns></returns>
    static public float AngleFromQuat(Quaternion quat)
    {
        if (quat.y == 0) return 0;
        Quaternion newquat = new Quaternion();
        float mag = Mathf.Sqrt((quat.w * quat.w) + (quat.y * quat.y));
        newquat.y = quat.y / mag;
        newquat.w = quat.w / mag;
        return newquat.eulerAngles.y;
    }

    /// <summary>
    /// Returns a position along a bezier curve, from point 1 to point 3 with point 2 acting as direction, and time acting as point along the curve, clamped to 0-1.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    static public Vector3 BezierCurve(Vector3 p1, Vector3 p2, Vector3 p3, float time)
    {
        time = Mathf.Clamp01(time);
        // yes, this is basically taken from wikipedia. it's not a very difficult algorithm to understand, though.
        // it simply scales between the two points with respect to time, and also scales the middle point together with them.
        // probably a terrible explanation. just read the dang code.
        //return (1 - time) * ((1 - time) * p1 + time * p2) + time * ((1 - time) * p2 + time * p3);
        // optimized version
        float tmp = 1 - time;
        return tmp * (tmp * p1 + time * p2) + time * (tmp * p2 + time * p3);
        // here's a version that's more simplified in writing, but slower in execution. it simply groups the multiplications with each point, rather than grouping them to be done the least.
        // why did I include this? because i wrote this at 11:30 pm and couldn't do the math in my head as to which was faster.
        //return (tmp * tmp) * p1 + 2 * tmp * time * p2 + time * time * p3;
    }

    /// <summary>
    /// Returns a position along a bezier curve, from point 1 to point 3, going through point 2
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    static public Vector3 BezierCurveThrough(Vector3 p1, Vector3 p2, Vector3 p3, float time)
    {
        Vector3 newvec = p2;
        newvec.x = (2 * p2.x) - p1.x/2 - p3.x/2;
        newvec.y = (2 * p2.y) - p1.y / 2 - p3.y / 2;
        newvec.z = (2 * p2.z) - p1.z / 2 - p3.z / 2;
        return BezierCurve(p1, newvec, p3, time);
    }

    /// <summary>
    /// Int version of mathf.Clamp
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    static public int IntClamp(int value, int min, int max)
    {
        return (value > max) ? max : (value < min) ? min : value;
    }

    /// <summary>
    /// Takes 4 verts, and makes a double sided quad out of them.
    /// </summary>
    /// <param name="verts"></param>
    /// <returns></returns>
    static public int[] MakeQuad(int[] verts)
    {
        int[] tris = new int[12];
        // triangle one
        tris[0] = verts[0];
        tris[1] = verts[2];
        tris[2] = verts[1];
        // triangle two
        tris[3] = verts[2];
        tris[4] = verts[3];
        tris[5] = verts[1];
        // triangle three
        tris[6] = verts[1];
        tris[7] = verts[2];
        tris[8] = verts[0];
        // triangle four
        tris[9] = verts[1];
        tris[10] = verts[3];
        tris[11] = verts[2];
        return tris;
    }

    /// <summary>
    /// Takes 3 points, and generates a triangle normal out of them
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    static public Vector3 TriToNormal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 U = p2 - p1;
        Vector3 V = p3 - p1;
        Vector3 norm = new Vector3
        {
            x = (U.y * V.z) - (U.z * V.y),
            y = (U.z * V.x) - (U.x * V.z),
            z = (U.x * V.y) - (U.y * V.x)
        };
        return norm.normalized;
    }

    /// <summary>
    /// Returns shortest rotation between two degrees
    /// </summary>
    /// <param name="dir1"></param>
    /// <param name="dir2"></param>
    /// <returns></returns>
    public static float AngleBetween(float dir1, float dir2)
    {

        float a = dir2 - dir1;

        a += 180.0f;

        if (a < 0)
        {
            a += 360.0f;
        }

        if (a > 360)
        {
            a -= 360.0f;
        }

        a -= 180.0f;

        return a;
    }

	/// <summary>
	/// Returns y axis from quaternion, relative to the quaternions current up direction
	/// </summary>
	/// <param name="rot"></param>
	/// <returns></returns>
	static public float GenerateDirection(Quaternion rot)
	{
		Vector3 rotvec;
		Vector3 newup = rot * Vector3.up;
		// generate rotation between our current up, and world up, then rotate our forward by that rotation to bring it aligned to the y axis
		if (newup.y >= 0)
		{
			rotvec = Quaternion.FromToRotation(newup, Vector3.up) * (rot * Vector3.forward);
		}
		else
		{
			Vector3 tmpup = newup;
			tmpup.y = -tmpup.y;
			rotvec = Quaternion.FromToRotation(tmpup, Vector3.up) * (rot * Vector3.forward);
		}
		return Mathf.Atan2(rotvec.x, rotvec.z) * Mathf.Rad2Deg;
	}

	/// <summary>
	/// Lerp but a smooth boi
	/// shout outs to Ken Perlin
	/// </summary>
	/// <param name="edge0"></param>
	/// <param name="edge1"></param>
	/// <param name="x"></param>
	/// <returns></returns>
	static public float SmootherStep(float edge0, float edge1, float x)
	{
		// Evaluate polynomial
		return Mathf.Lerp(edge0, edge1, x * x * x * (x * (x * 6f - 15f) + 10f));
	}

	static public Vector3 VecMul(Vector3 a, Vector3 b)
	{
		return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
	}

	/// <summary>
	/// Compresses a 3 dimensional rotation to use fewer bytes
	/// A quaternion will consume 16 bytes, this will consume only 6!
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	static public byte[] CompressRotation(Quaternion input)
	{
		return CompressRotation(input.eulerAngles);
	}

	static public byte[] CompressRotation(Vector3 input)
	{
		// basically convert to BAMS
		byte[] retval = new byte[6];
		// This is the first thing I've programmed since I did a bunch of assembly for like, a month. Wonder if that has any impact on this?
		Array.Copy(BitConverter.GetBytes(FloatToBAMS(input.x)), 0, retval, 0, 2);
		Array.Copy(BitConverter.GetBytes(FloatToBAMS(input.y)), 0, retval, 2, 2);
		Array.Copy(BitConverter.GetBytes(FloatToBAMS(input.z)), 0, retval, 4, 2);
		return retval;
		// Conclusion? It's...short.
	}

	/// <summary>
	/// Rotation, encoded in 16 bits instead of 32!
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	static public short FloatToBAMS(float input)
	{
		return (short)((input / 360f) * 32767f);
	}

	/// <summary>
	/// Decompresses a 3 dimensional rotation from the compressrotation function
	/// </summary>
	/// <param name="input"></param>
	/// <param name="offset"></param>
	/// <returns></returns>
	static public Quaternion DecompressRotation(byte[] input, int offset)
	{
		return Quaternion.Euler(DecompressRotationEuler(input, offset));
	}

	/// <summary>
	/// Decompress rotation function but returns euler angles instead of a quaternion
	/// </summary>
	/// <param name="input"></param>
	/// <param name="offset"></param>
	/// <returns></returns>
	static public Vector3 DecompressRotationEuler(byte[] input, int offset)
	{
		// just convert from BAMS
		Vector3 retval;
		retval.x = BAMSToFloat(BitConverter.ToInt16(input, offset));
		retval.y = BAMSToFloat(BitConverter.ToInt16(input, offset + 2));
		retval.z = BAMSToFloat(BitConverter.ToInt16(input, offset + 4));
		return retval;
	}

	/// <summary>
	/// Decode 16 bit BAMS rotation back to 32 bit float!
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	static public float BAMSToFloat(short input)
	{
		return (((float)input) / 32767f) * 360f;
	}

	/// <summary>
	/// Basically BAMS but more flexible
	/// </summary>
	/// <param name="input"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	static public short CompressFloatToShort(float input, float max)
	{
		return (short)((Mathf.Clamp(input, -max, max) / max) * 32767f);
	}

	/// <summary>
	/// Basically BAMS but more flexible and 8 bit
	/// </summary>
	/// <param name="input"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	static public byte CompressFloatToByte(float input, float max)
	{
		return (byte)((Mathf.Clamp(input, -max, max) / max) * 127f);
	}

	/// <summary>
	/// Decompresses short compressed by CompressFloatToShort
	/// </summary>
	/// <param name="input"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	static public float DecompressShortToFloat(short input, float max)
	{
		return ((float)input / 32767f) * max;
	}

	/// <summary>
	/// Decompresses byte compressed by CompressFloatToByte
	/// </summary>
	/// <param name="input"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	static public float DecompressByteToFloat(byte input, float max)
	{
		return ((float)input / 127f) * max;
	}
}
